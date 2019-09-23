﻿using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics; //debugging
using Grasshopper.Kernel;
using Rhino.Geometry;
using Microsoft.Kinect;
using System.Windows.Forms;
using System.Linq;
using SandWorm.Properties;

// comment 
// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

//Test comment
namespace SandWorm
{
    public class SandWorm : GH_Component
    {
        private KinectSensor kinectSensor = null;
        private Point3d[] pointCloud;
        private List<Mesh> outputMesh = null;
        public static List<string> output = null;//debugging
        private LinkedList<int[]> renderBuffer = new LinkedList<int[]>();
        public int[] runningSum = Enumerable.Range(1, 217088).Select(i => new int()).ToArray();

        public double depthPoint;
        public static Color[] lookupTable = new Color[1500]; //to do - fix arbitrary value assuming 1500 mm as max distance from the kinect sensor
        public Color[] vertexColors;
        public Mesh quadMesh = new Mesh();

        public double sensorElevation = 1000; // Arbitrary default value (must be >0)
        public int leftColumns = 0;
        public int rightColumns = 0;
        public int topRows = 0;
        public int bottomRows = 0;
        public int tickRate = 20; // In ms
        public int averageFrames = 1;
        public int blurRadius = 1;
        public static Rhino.UnitSystem units = Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem;
        public static double unitsMultiplier;

        // Analysis state
        private int waterLevel = 1000;

        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SandWorm()
          : base("SandWorm", "SandWorm",
              "Kinect v2 Augmented Reality Sandbox",
              "Sandworm", "Sandbox")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("SensorHeight", "SH", "The height (in document units) of the sensor above your model", GH_ParamAccess.item, sensorElevation);
            pManager.AddIntegerParameter("WaterLevel", "WL", "WaterLevel", GH_ParamAccess.item, 1000);
            pManager.AddIntegerParameter("LeftColumns", "LC", "Number of columns to trim from the left", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("RightColumns", "RC", "Number of columns to trim from the right", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("TopRows", "TR", "Number of rows to trim from the top", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("BottomRows", "BR", "Number of rows to trim from the bottom", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("TickRate", "TR", "The time interval, in milliseconds, to update geometry from the Kinect. Set as 0 to disable automatic updates.", GH_ParamAccess.item, tickRate);
            pManager.AddIntegerParameter("AverageFrames", "AF", "Amount of depth frames to average across. This number has to be greater than zero.", GH_ParamAccess.item, averageFrames);
            pManager.AddIntegerParameter("BlurRadius", "BR", "Radius for Gaussian blur.", GH_ParamAccess.item, blurRadius);
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

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Resulting Mesh", GH_ParamAccess.list);
            pManager.AddTextParameter("Output", "O", "Output", GH_ParamAccess.list); //debugging
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            foreach (Analysis.MeshAnalysis option in Analysis.AnalysisManager.options) // Add analysis items to menu
            {
                Menu_AppendItem(menu, option.Name, SetMeshVisualisation, true, option.IsEnabled);
                // Create reference to the menu item in the analysis class
                option.MenuItem = (ToolStripMenuItem)menu.Items[menu.Items.Count - 1];
                if (!option.IsExclusive)
                    Menu_AppendSeparator(menu);
            }
        }

        private void SetMeshVisualisation(object sender, EventArgs e)
        {
            Analysis.AnalysisManager.SetEnabledOptions((ToolStripMenuItem)sender);
            Analysis.AnalysisManager.ComputeLookupTables(sensorElevation);
            quadMesh.VertexColors.Clear(); // Must flush mesh colors to properly updated display
            ExpireSolution(true);
        }

        private void ScheduleDelegate(GH_Document doc)
        {
            ExpireSolution(false);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData<double>(0, ref sensorElevation);
            DA.GetData<int>(1, ref waterLevel);
            DA.GetData<int>(2, ref leftColumns);
            DA.GetData<int>(3, ref rightColumns);
            DA.GetData<int>(4, ref topRows);
            DA.GetData<int>(5, ref bottomRows);
            DA.GetData<int>(6, ref tickRate);
            DA.GetData<int>(7, ref averageFrames);
            DA.GetData<int>(8, ref blurRadius);

            switch (units.ToString())
            {
                case "Kilometers":
                    unitsMultiplier = 0.0001;
                    break;

                case "Meters":
                    unitsMultiplier = 0.001;
                    break;

                case "Decimeters":
                    unitsMultiplier = 0.01;
                    break;

                case "Centimeters":
                    unitsMultiplier = 0.1;
                    break;

                case "Millimeters":
                    unitsMultiplier = 1;
                    break;

                case "Inches":
                    unitsMultiplier = 0.0393701;
                    break;

                case "Feet":
                    unitsMultiplier = 0.0328084;
                    break;
            }
            sensorElevation /= unitsMultiplier; // Standardise to mm to match sensor units 

            Stopwatch timer = Stopwatch.StartNew(); //debugging

            if (this.kinectSensor == null)
            {
                KinectController.AddRef();
                this.kinectSensor = KinectController.sensor;
            }

            if (this.kinectSensor != null)
            {
                if (KinectController.depthFrameData != null)
                {
                    int trimmedWidth = KinectController.depthWidth - leftColumns - rightColumns;
                    int trimmedHeight = KinectController.depthHeight - topRows - bottomRows;
                    
                    // initialize all arrays
                    pointCloud = new Point3d[trimmedWidth * trimmedHeight];
                    int[] depthFrameDataInt = new int[trimmedWidth * trimmedHeight];
                    double[] averagedDepthFrameData = new double[trimmedWidth * trimmedHeight];

                    // initialize outputs
                    outputMesh = new List<Mesh>();
                    output = new List<string>(); //debugging

                    Point3d tempPoint = new Point3d();
                    Core.PixelSize depthPixelSize = Core.GetDepthPixelSpacing(sensorElevation);

                    // trim the depth array and cast ushort values to int
                    Core.CopyAsIntArray(KinectController.depthFrameData, depthFrameDataInt, leftColumns, rightColumns, topRows, bottomRows, KinectController.depthHeight, KinectController.depthWidth);

                    averageFrames = averageFrames < 1 ? 1 : averageFrames; //make sure there is at least one frame in the render buffer
                    
                    // reset everything when resizing Kinect's field of view or changing the amounts of frame to average across
                    if (renderBuffer.Count > averageFrames || quadMesh.Faces.Count != (trimmedWidth - 2) * (trimmedHeight - 2))
                    {
                        renderBuffer.Clear();
                        Array.Clear(runningSum, 0, runningSum.Length);
                        renderBuffer.AddLast(depthFrameDataInt);
                    }
                    else
                        renderBuffer.AddLast(depthFrameDataInt);

                    output.Add("Render buffer length:".PadRight(30, ' ') + renderBuffer.Count + " frames"); //debugging

                    // average across multiple frames
                    for (int pixel = 0; pixel < depthFrameDataInt.Length; pixel++)
                    {
                        if (depthFrameDataInt[pixel] > 200 && depthFrameDataInt[pixel] <= lookupTable.Length) //We have a valid pixel. TODO remove reference to the lookup table
                            runningSum[pixel] += depthFrameDataInt[pixel];
                        else
                        {
                            if (pixel > 0) //pixel is invalid and we have a neighbor to steal information from
                            {
                                runningSum[pixel] += depthFrameDataInt[pixel - 1];
                                renderBuffer.Last.Value[pixel] = depthFrameDataInt[pixel - 1]; //replace the zero value from the depth array with the one from the neighboring pixel
                            }
                            else //pixel is invalid and it is the first one in the list. (No neighbor on the left hand side, so we set it to the lowest point on the table)
                            {
                                runningSum[pixel] += (int)sensorElevation;
                                renderBuffer.Last.Value[pixel] = (int)sensorElevation;
                            }
                        }
                            
                        averagedDepthFrameData[pixel] = runningSum[pixel] / renderBuffer.Count; //calculate average values

                        if (renderBuffer.Count >= averageFrames) 
                            runningSum[pixel] -= renderBuffer.First.Value[pixel]; //subtract the oldest value from the sum 
                    }

                    timer.Stop();
                    output.Add("Frames averaging: ".PadRight(30, ' ') + timer.ElapsedMilliseconds.ToString() + " ms");
                    timer.Restart(); //debugging

                    if (blurRadius > 1) //apply gaussian blur
                    {
                        GaussianBlurProcessor gaussianBlurProcessor = new GaussianBlurProcessor(blurRadius, trimmedWidth, trimmedHeight);
                        gaussianBlurProcessor.Apply(averagedDepthFrameData);
                        timer.Stop();
                        output.Add("Gaussian blurring: ".PadRight(30, ' ') + timer.ElapsedMilliseconds.ToString() + " ms");
                        timer.Restart(); //debugging
                    }

                    // Setup variables for the coloring process
                    Analysis.AnalysisManager.ComputeLookupTables(sensorElevation); // First-run computing of tables
                    var enabledMeshColoring = Analysis.AnalysisManager.GetEnabledMeshColoring();
                    var enabledColorTable = enabledMeshColoring.lookupTable;
                    var hasColorTable = enabledColorTable.Length > 0; // Setting the 'no analysis' option == empty table  
                    vertexColors = new Color[hasColorTable ? trimmedWidth * trimmedHeight : 0]; // A 0-length array wont be used in meshing
                    var pixelsForAnalysis = new Point3d[4];

                    // Setup variables for per-pixel loop
                    pointCloud = new Point3d[trimmedWidth * trimmedHeight];
                    int arrayIndex = 0;
                    for (int rows = 0; rows < trimmedHeight; rows++)
                    {
                        for (int columns = 0; columns < trimmedWidth; columns++)
                        {
                            depthPoint = averagedDepthFrameData[arrayIndex];
                            tempPoint.X = columns * -unitsMultiplier * depthPixelSize.x;
                            tempPoint.Y = rows * -unitsMultiplier * depthPixelSize.y;
                            tempPoint.Z = (depthPoint - sensorElevation) * -unitsMultiplier;

                            pointCloud[arrayIndex] = tempPoint; // Add new point to point cloud itself
                            if (hasColorTable) // Perform analysis as needed and lookup result in table
                            {
                                var pixelIndex = enabledMeshColoring.GetPixelIndexForAnalysis(tempPoint, pixelsForAnalysis);
                                vertexColors[arrayIndex] = enabledColorTable[pixelIndex];
                            }
                            arrayIndex++;
                        }
                    }
                   
                    //keep only the desired amount of frames in the buffer
                    while (renderBuffer.Count >= averageFrames)
                    {
                        renderBuffer.RemoveFirst();
                    }

                    //debugging
                    timer.Stop();
                    output.Add("Point cloud generation/color: ".PadRight(30, ' ') + timer.ElapsedMilliseconds.ToString() + " ms");
                    timer.Restart(); //debugging

                    quadMesh = Core.CreateQuadMesh(quadMesh, pointCloud, vertexColors, trimmedWidth, trimmedHeight);
                    outputMesh.Add(quadMesh);

                    timer.Stop(); //debugging
                    output.Add("Meshing: ".PadRight(30, ' ') + timer.ElapsedMilliseconds.ToString() + " ms");
                }

                DA.SetDataList(0, outputMesh);
                DA.SetDataList(1, output); //debugging
            }

            if (tickRate > 0) // Allow users to force manual recalculation
            {
                base.OnPingDocument().ScheduleSolution(tickRate, new GH_Document.GH_ScheduleDelegate(ScheduleDelegate));
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override Bitmap Icon => Resources.icon_mesh;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("f923f24d-86a0-4b7a-9373-23c6b7d2e162"); }
        }
    }
}
