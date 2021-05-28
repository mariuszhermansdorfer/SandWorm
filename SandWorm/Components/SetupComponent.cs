using System;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino;
using SandWorm.Components;
using SandWorm.Properties;

namespace SandWorm
{
    public class SetupComponent : BaseKinectComponent
    {
        private double _averagedSensorElevation;
        private bool _calibrateSandworm;
        private double[] _elevationArray;
        private int _frameCount = 0; // Number of frames to average the calibration across

        private System.Collections.Generic.List<Grasshopper.Kernel.Types.GH_Point> points;

        //private System.Numerics.Vector3?[] translationMatrix;
        private K4AdotNet.Float3?[] translationMatrix;
        private short[] depthFrameShort;

        public SetupComponent() : base("Setup Component", "SWSetup",
            "This component takes care of all the setup & calibration of your sandbox.", "Utility")
        {
        }

        protected override Bitmap Icon => Resources.icons_setup;

        public override Guid ComponentGuid => new Guid("9ee53381-c269-4fff-9d45-8a2dbefc243c");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("CalibrateSandworm", "CS",
                "Set to true to initiate the calibration process.", GH_ParamAccess.item, _calibrateSandworm);
            pManager.AddNumberParameter("SensorHeight", "SH",
                "The height (in document units) of the sensor above your model.", GH_ParamAccess.item, sensorElevation);
            pManager.AddIntegerParameter("LeftColumns", "LC",
                "Number of columns to trim from the left.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("RightColumns", "RC",
                "Number of columns to trim from the right.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("TopRows", "TR",
                "Number of rows to trim from the top.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("BottomRows", "BR",
                "Number of rows to trim from the bottom.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("TickRate", "TR",
                "The time interval, in milliseconds, to update geometry from the Kinect. Set as 0 to disable automatic updates.",
                GH_ParamAccess.item, tickRate);
            pManager.AddIntegerParameter("KeepFrames", "KF",
                "Output a running list of frame updates rather than just the current frame. Set to 1 or 0 to disable.",
                GH_ParamAccess.item, keepFrames);
            pManager.AddIntegerParameter("Kinect Type", "KT",
                "Leave as 0 for Kinect for Windows; set 1 for Kinect for Azure in Near-FOV; set 2 for Kinect for Azure in Wide-FOV.",
                GH_ParamAccess.item, (int)kinectType);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
            pManager[8].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Options", "O", "SandWorm Options", GH_ParamAccess.item);
            pManager.AddTextParameter("Info", "I", "Component info", GH_ParamAccess.list); //debugging
            pManager.AddPointParameter("po", "po", "", GH_ParamAccess.list);
        }

        private void ManipulateSlider(GH_Document doc)
        {
            var input = Params.Input[1].Sources[0]; // Get the first thing connected to the second input of this component

            if (input is GH_NumberSlider slider)
            {
                slider.Slider.RaiseEvents = false;
                slider.Slider.DecimalPlaces = 4;
                slider.SetSliderValue((decimal) _averagedSensorElevation);
                slider.ExpireSolution(false);
                slider.Slider.RaiseEvents = true;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "Calibration measurement finished; sensor elevation measured and set as " + _averagedSensorElevation);
            }
        }

        protected override void SandwormSolveInstance(IGH_DataAccess DA)
        {
            SetupLogging();
            int tempKinectType = 0; // Can't cast to the enum within GetData ref

            DA.GetData(0, ref _calibrateSandworm);
            DA.GetData(1, ref sensorElevation);
            DA.GetData(2, ref leftColumns);
            DA.GetData(3, ref rightColumns);
            DA.GetData(4, ref topRows);
            DA.GetData(5, ref bottomRows);
            DA.GetData(6, ref tickRate);
            DA.GetData(7, ref keepFrames);
            DA.GetData(8, ref tempKinectType);

            if ((int)tempKinectType > 2)
            {
                ShowComponentError("Invalid KinectType provided. Must be 0, 1, or 2.");
                return;
            }
            else
            {
                kinectType = (Core.KinectTypes)tempKinectType;
            }

            // Initialize all arrays
            points = new System.Collections.Generic.List<Grasshopper.Kernel.Types.GH_Point>();

            Core.GetTrimmedDimensions(kinectType, ref trimmedWidth, ref trimmedHeight, ref _elevationArray, 
                                      topRows, bottomRows, leftColumns, rightColumns);
            var depthFrameDataInt = new int[trimmedWidth * trimmedHeight];
            var averagedDepthFrameData = new double[trimmedWidth * trimmedHeight];

            _averagedSensorElevation = sensorElevation;
            var unitsMultiplier = Core.ConvertDrawingUnits(RhinoDoc.ActiveDoc.ModelUnitSystem);

            var active_Height = 0;
            var active_Width = 0;
            ushort[] depthFrameData;
            if (kinectType == Core.KinectTypes.KinectForWindows)
            {
                depthFrameData = KinectController.depthFrameData;
                active_Height = KinectController.depthHeight;
                active_Width = KinectController.depthWidth;
            }
            else
            {
                var errorMessage = "";
                KinectAzureController.SetupSensor(kinectType, ref errorMessage); //neededfor the following to work the first time round.
                KinectAzureController.Initialize(kinectType); //this should be stoping active cameras and updating the settings for the new one
                KinectAzureController.CaptureFrame(); //this gets a frame so the variables below have some values.
                translationMatrix = KinectAzureController.translationMatrix;
                depthFrameShort = KinectAzureController.depthFrameShort;
                depthFrameData = KinectAzureController.depthFrameData;
                active_Height = KinectAzureController.depthHeight;
                active_Width = KinectAzureController.depthWidth;
            }

            // Trim the depth array and cast ushort values to int //BUG Attempted to write protected data

            if (_calibrateSandworm) _frameCount = 60; // Start calibration 


            if (_frameCount > 1) // Iterate a pre-set number of times
            {
                output.Add("Reading frame: " + _frameCount); // Debug Info

                // Trim the depth array and cast ushort values to int
                Core.CopyAsIntArray(depthFrameData, depthFrameDataInt,
                                    leftColumns, rightColumns, topRows, bottomRows,
                                    active_Height, active_Width);

                renderBuffer.AddLast(depthFrameDataInt);
                for (var pixel = 0; pixel < depthFrameDataInt.Length; pixel++)
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
                            // Replace the zero value from the depth array with the one from the neighboring pixel
                            renderBuffer.Last.Value[pixel] = depthFrameDataInt[pixel - 1]; 
                        }
                        else // Pixel is invalid and it is the first one in the list. (No neighbor on the left hand side, so we set it to the lowest point on the table)
                        {
                            runningSum[pixel] += (int) sensorElevation;
                            renderBuffer.Last.Value[pixel] = (int) sensorElevation;
                        }
                    }
                    averagedDepthFrameData[pixel] = runningSum[pixel] / renderBuffer.Count; // Calculate average values
                }

                _frameCount--;
                if (_frameCount == 1) // All frames have been collected, we can save the results
                {
                    // Measure sensor elevation by averaging over a grid of 20x20 pixels in the center of the table
                    var counter = 0;
                    _averagedSensorElevation = 0;

                    for (var y = trimmedHeight / 2 - 10; y < trimmedHeight / 2 + 10; y++) // Iterate over y dimension
                    for (var x = trimmedWidth / 2 - 10; x < trimmedWidth / 2 + 10; x++) // Iterate over x dimension
                    {
                        var i = y * trimmedWidth + x;
                        _averagedSensorElevation += averagedDepthFrameData[i];
                        counter++;
                    }

                    _averagedSensorElevation /= counter;

                    // Counter for Kinect inaccuracies and potential hardware misalignment by storing differences between the averaged sensor elevation and individual pixels.
                    for (var i = 0; i < averagedDepthFrameData.Length; i++)
                        _elevationArray[i] = averagedDepthFrameData[i] - _averagedSensorElevation;

                    _averagedSensorElevation *= unitsMultiplier;

                    renderBuffer.Clear();
                    Array.Clear(runningSum, 0, runningSum.Length);
                }

                if (_frameCount > 1)
                    ScheduleSolve(); // Schedule another solution to get more data from Kinect
                else
                    OnPingDocument().ScheduleSolution(5, ManipulateSlider);
            }

            output.Add("Parameter-Provided Sensor Elevation: " + sensorElevation); // Debug Info
            output.Add("Measured-Average Sensor Elevation: " + _averagedSensorElevation); // Debug Info
            output.Add("Output Sensor Elevation: " + _averagedSensorElevation); // Debug Info

            var outputOptions = new SetupOptions
            {
                SensorElevation = _averagedSensorElevation,
                LeftColumns = leftColumns,
                RightColumns = rightColumns,
                TopRows = topRows,
                BottomRows = bottomRows,
                TickRate = tickRate,
                KeepFrames = keepFrames,
                ElevationArray = _elevationArray,
                KinectType = kinectType,
            };
            

            Rhino.Geometry.Point3d pt = new Rhino.Geometry.Point3d();
            System.Collections.Generic.List<Rhino.Geometry.Point3d> _points = new System.Collections.Generic.List<Rhino.Geometry.Point3d>();
            /*
            for (int y = 0, i = 0; y < active_Height; y++)
            {
                
                for (int x = 0; x < active_Width; x++, i++)
                {

                    int indx = x * 3 + y * active_Width * 3;
                    pt.X = KinectAzureController.xyzImageBuffer[indx];
                    pt.Y = KinectAzureController.xyzImageBuffer[indx + 1];
                    pt.Z = KinectAzureController.xyzImageBuffer[indx + 2];

                    //_points.Add(pt);
                    points.Add(new Grasshopper.Kernel.Types.GH_Point(pt));
                }
            }
            */
            /*
            for (int i = 0; i < KinectAzureController.depthFrameShort.Length; i++)
            {
                pt.X = Math.Round(KinectAzureController.depthFrameShort[i] * translationMatrix[i].Value.X, 1);
                pt.Y = Math.Round(KinectAzureController.depthFrameShort[i] * translationMatrix[i].Value.Y, 1);
                pt.Z = KinectAzureController.depthFrameShort[i];

                points.Add(new Grasshopper.Kernel.Types.GH_Point(pt));
            }
            */
            /*
            for (int i = 0; i < _points.Count; i++)
            {
                pt.X = _points[i].X * translationMatrix[i].Value.X * 100;
                pt.Y = _points[i].Y * translationMatrix[i].Value.Y * 100;
                pt.Z = _points[i].Z;

                points.Add(new Grasshopper.Kernel.Types.GH_Point(pt));
            }
            */
            /*
            for (int y = 0, i = 0; y < active_Height; y++)
            {
                pt.Y = 100 * translationMatrix[i].Value.Y;
                for (int x = 0; x < active_Width; x++, i++)
                {
                    pt.X = 100 * translationMatrix[i].Value.X;
                    pt.Z = 100;
                    points.Add(new Grasshopper.Kernel.Types.GH_Point(pt));
                }
            }
            
            */
            /*
            var te = KinectAzureController.test;

            var byteArray = KinectAzureController.test.Memory.ToArray();



                for (int x = 0; x < active_Width * active_Height * 6; x+=3)
                {
                    //int indx = x * 3 + y * active_Width * 3;

                    pt.X = byteArray[x];
                    pt.Y = byteArray[x + 1];
                    pt.Z = byteArray[x + 2];

                    points.Add(new Grasshopper.Kernel.Types.GH_Point(pt));
                }
            

            int o = active_Width / 2 * 3 + active_Height / 2 * active_Width * 3;
            RhinoApp.WriteLine($"x ={byteArray[o]}, y ={byteArray[o + 1]}, z ={byteArray[o + 2]}");
            */

            Core.LogTiming(ref output, timer, "Setup completion"); // Debug Info
            DA.SetData(0, outputOptions);
            DA.SetDataList(1, output); // For logging/debugging
            DA.SetDataList(2, KinectAzureController.points);
        }
    }
}