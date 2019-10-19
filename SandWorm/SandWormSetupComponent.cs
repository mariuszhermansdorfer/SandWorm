using System;
using System.Collections.Generic;
using Microsoft.Kinect;
using Grasshopper.Kernel;
using System.Linq;

namespace SandWorm

{
    public class SandWormSetupComponent : GH_Component
    {
        public bool calibrateSandworm;
        public double sensorElevation = 1000; // Arbitrary default value (must be >0)
        public int leftColumns = 0;
        public int rightColumns = 0;
        public int topRows = 0;
        public int bottomRows = 0;
        public int tickRate = 33; // In ms
        public int keepFrames = 1; // In ms

        public int frameCount; // Number of frames to average the calibration across

        private LinkedList<int[]> renderBuffer = new LinkedList<int[]>();
        public int[] runningSum = Enumerable.Range(1, 217088).Select(i => new int()).ToArray();

        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public SandWormSetupComponent()
          : base("SandWormSetup", "SWSetup",
              "This component takes care of all the setup & calibration of your sandbox.",
              "Sandworm", "Sandbox")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("CalibrateSandworm", "CS", "Set to true to initiate the calibration process.", GH_ParamAccess.item, calibrateSandworm);
            pManager.AddNumberParameter("SensorHeight", "SH", "The height (in document units) of the sensor above your model.", GH_ParamAccess.item, sensorElevation);
            pManager.AddIntegerParameter("LeftColumns", "LC", "Number of columns to trim from the left.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("RightColumns", "RC", "Number of columns to trim from the right.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("TopRows", "TR", "Number of rows to trim from the top.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("BottomRows", "BR", "Number of rows to trim from the bottom.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("TickRate", "TR", "The time interval, in milliseconds, to update geometry from the Kinect. Set as 0 to disable automatic updates.", GH_ParamAccess.item, tickRate);
            pManager.AddIntegerParameter("KeepFrames", "KF", "Output a running list of frame updates rather than just the current frame. Set to 1 or 0 to disable.", GH_ParamAccess.item, keepFrames);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Options", "O", "SandWorm Options", GH_ParamAccess.item); 
            pManager.AddTextParameter("Info", "I", "Component info", GH_ParamAccess.list); //debugging
        }

        private void ScheduleDelegate(GH_Document doc)
        {
            ExpireSolution(false);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData<bool>(0, ref calibrateSandworm);
            DA.GetData<double>(1, ref sensorElevation);
            DA.GetData<int>(2, ref leftColumns);
            DA.GetData<int>(3, ref rightColumns);
            DA.GetData<int>(4, ref topRows);
            DA.GetData<int>(5, ref bottomRows);
            DA.GetData<int>(6, ref tickRate);
            DA.GetData<int>(7, ref keepFrames);
            // Initialize all arrays
            int trimmedWidth = KinectController.depthWidth - leftColumns - rightColumns;
            int trimmedHeight = KinectController.depthHeight - topRows - bottomRows;

            int[] depthFrameDataInt = new int[trimmedWidth * trimmedHeight];
            double[] averagedDepthFrameData = new double[trimmedWidth * trimmedHeight];
            double[] elevationArray = new double[trimmedWidth * trimmedHeight];

            double averagedSensorElevation = sensorElevation;
            var unitsMultiplier = Core.ConvertDrawingUnits(Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem);
            string info = "";

            if (calibrateSandworm) frameCount = 60; // Start calibration 

            if (frameCount > 1) // Iterate a pre-set number of times
            {
                info += "Reading frame: " + frameCount.ToString();
                // Trim the depth array and cast ushort values to int
                Core.CopyAsIntArray(KinectController.depthFrameData, depthFrameDataInt, leftColumns, rightColumns, topRows, bottomRows, KinectController.depthHeight, KinectController.depthWidth);

                renderBuffer.AddLast(depthFrameDataInt);
                for (int pixel = 0; pixel < depthFrameDataInt.Length; pixel++)
                {
                    if (depthFrameDataInt[pixel] > 200) // We have a valid pixel. 
                    {
                        runningSum[pixel] += depthFrameDataInt[pixel];
                    }
                    else
                    {
                        if (pixel > 0) // Pixel is invalid and we have a neighbor to steal information from
                        {
                            runningSum[pixel] += depthFrameDataInt[pixel - 1];
                            renderBuffer.Last.Value[pixel] = depthFrameDataInt[pixel - 1]; // Replace the zero value from the depth array with the one from the neighboring pixel
                        }
                        else // Pixel is invalid and it is the first one in the list. (No neighbor on the left hand side, so we set it to the lowest point on the table)
                        {
                            runningSum[pixel] += (int)sensorElevation;
                            renderBuffer.Last.Value[pixel] = (int)sensorElevation;
                        }
                    }
                    averagedDepthFrameData[pixel] = runningSum[pixel] / renderBuffer.Count; // Calculate average values
                }
                frameCount--;

                if (frameCount == 1) // All frames have been collected, we can save the results
                {
                    // Measure sensor elevation by averaging over a grid of 20x20 pixels in the center of the table
                    int counter = 0;
                    averagedSensorElevation = 0;

                    for (int y = (trimmedHeight / 2) - 10; y < (trimmedHeight / 2) + 10; y++)       // Iterate over y dimension
                    {
                        for (int x = (trimmedWidth / 2) - 10; x < (trimmedWidth / 2) + 10; x++)       // Iterate over x dimension
                        {
                            int i = y * trimmedWidth + x;

                            averagedSensorElevation += averagedDepthFrameData[i];
                            
                            counter++;
                        }
                    }
                    averagedSensorElevation /= counter;
                    

                    // Counter for Kinect inaccuracies and potential hardware misalignment by storing differences between the averaged sensor elevation and individual pixels.
                    for (int i = 0; i < averagedDepthFrameData.Length; i++)
                    {
                        elevationArray[i] = averagedDepthFrameData[i] - averagedSensorElevation;
                    }

                    averagedSensorElevation *= unitsMultiplier;

                    renderBuffer.Clear();
                    Array.Clear(runningSum, 0, runningSum.Length);
                }

                if (frameCount > 1)
                {
                    ScheduleSolve(); // Schedule another solution to get more data from Kinect
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Calibration measurement finished; sensor elevation measured and set as " + averagedSensorElevation.ToString());
                    info = ""; // Reset the frame-measuring messages (otherwise it looks like its stuck/paused?)
                }
            }
            
            info += "\nParameter-Provided Sensor Elevation: " + sensorElevation.ToString();
            info += "\nMeasured-Average Sensor Elevation: " + averagedSensorElevation.ToString();
            info += "\nOutput Sensor Elevation: " + averagedSensorElevation.ToString();

            var options = new SetupOptions
            {
                SensorElevation = averagedSensorElevation,
                LeftColumns = leftColumns,
                RightColumns = rightColumns,
                TopRows = topRows,
                BottomRows = bottomRows,
                TickRate = tickRate,
                KeepFrames = keepFrames,
                ElevationArray = elevationArray
            };

            DA.SetData(0, options);
            DA.SetData(1, info);
        }

        private void ScheduleSolve()
        {
            base.OnPingDocument().ScheduleSolution(33, new GH_Document.GH_ScheduleDelegate(ScheduleDelegate));
        }
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9ee53381-c269-4fff-9d45-8a2dbefc243c"); }
        }
    }
}