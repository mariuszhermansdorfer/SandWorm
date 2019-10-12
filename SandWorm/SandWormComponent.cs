using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Microsoft.Kinect;


namespace SandWorm
{
    public class SandWorm : GH_Component
    {
        private KinectSensor kinectSensor = null;
        private Point3f[] pointCloud;
        private List<Mesh> outputMesh = null;
        private List<Rhino.Geometry.GeometryBase> outputGeometry = null;
        public static List<string> output = null;//debugging
        private LinkedList<int[]> renderBuffer = new LinkedList<int[]>();
        public int[] runningSum = Enumerable.Range(1, 217088).Select(i => new int()).ToArray();

        public double depthPoint;
        public static Color[] lookupTable = new Color[1500]; //to do - fix arbitrary value assuming 1500 mm as max distance from the kinect sensor
        public Color[] vertexColors;
        public Mesh quadMesh = new Mesh();
        public PointCloud _cloud;

        public SetupOptions options; // List of options coming from the SWSetup component

        public double sensorElevation = 1000; // Arbitrary default value (must be >0)
        public int leftColumns = 0;
        public int rightColumns = 0;
        public int topRows = 0;
        public int bottomRows = 0;
        public int tickRate = 33; // In ms
        public int keepFrames = 1;
        public int averageFrames = 1;
        public int blurRadius = 1;
        public static double unitsMultiplier;

        // Analysis state
        private double waterLevel = 50;
        private double contourInterval = 10;

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
            pManager.AddNumberParameter("WaterLevel", "WL", "WaterLevel", GH_ParamAccess.item, waterLevel);
            pManager.AddNumberParameter("ContourInterval", "CI", "The interval (if this analysis is enabled)", GH_ParamAccess.item, contourInterval);
            pManager.AddIntegerParameter("AverageFrames", "AF", "Amount of depth frames to average across. This number has to be greater than zero.", GH_ParamAccess.item, averageFrames);
            pManager.AddIntegerParameter("BlurRadius", "BR", "Radius for Gaussian blur.", GH_ParamAccess.item, blurRadius);
            pManager.AddGenericParameter("SandWormOptions", "SWO", "Setup & Calibration options", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Resulting mesh", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Analysis", "A", "Additional mesh analysis", GH_ParamAccess.list);
            pManager.AddTextParameter("Output", "O", "Output", GH_ParamAccess.list); //debugging
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            foreach (Analysis.MeshAnalysis option in Analysis.AnalysisManager.options) // Add analysis items to menu
            {
                Menu_AppendItem(menu, option.Name, SetMeshVisualisation, true, option.isEnabled);
                // Create reference to the menu item in the analysis class
                option.MenuItem = (ToolStripMenuItem)menu.Items[menu.Items.Count - 1];
                if (!option.IsExclusive)
                    Menu_AppendSeparator(menu);
            }
        }

        private void SetMeshVisualisation(object sender, EventArgs e)
        {
            Analysis.AnalysisManager.SetEnabledOptions((ToolStripMenuItem)sender);
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
            options = new SetupOptions();
            DA.GetData<double>(0, ref waterLevel);
            DA.GetData<double>(1, ref contourInterval);
            DA.GetData<int>(2, ref averageFrames);
            DA.GetData<int>(3, ref blurRadius);
            DA.GetData<SetupOptions>(4, ref options);

            if (options.sensorElevation != 0) sensorElevation = options.sensorElevation;
            if (options.leftColumns != 0) leftColumns = options.leftColumns;
            if (options.rightColumns != 0) rightColumns = options.rightColumns;
            if (options.topRows != 0) topRows = options.topRows;
            if (options.bottomRows != 0) bottomRows = options.bottomRows;
            if (options.tickRate != 0) tickRate = options.tickRate;

            // Pick the correct multiplier based on the drawing units. Shouldn't be a class variable; gets 'stuck'.
            unitsMultiplier = Core.ConvertDrawingUnits(Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem);
            sensorElevation /= unitsMultiplier; // Standardise to mm to match sensor units

            Stopwatch timer = Stopwatch.StartNew(); // Setup timer used for debugging
            output = new List<string>(); // For the debugging log lines

            if (this.kinectSensor == null)
            {
                KinectController.AddRef();
                this.kinectSensor = KinectController.sensor;
            }
            if (KinectController.depthFrameData == null)
            {
                ShowComponentError("No depth frame data provided by the Kinect.");
                return;
            }

            // Initialize all arrays
            int trimmedWidth = KinectController.depthWidth - leftColumns - rightColumns;
            int trimmedHeight = KinectController.depthHeight - topRows - bottomRows;

            int[] depthFrameDataInt = new int[trimmedWidth * trimmedHeight];
            double[] averagedDepthFrameData = new double[trimmedWidth * trimmedHeight];

            // Initialize outputs
            if (keepFrames <= 1 || outputMesh == null)
                outputMesh = new List<Mesh>(); // Don't replace prior frames (by clearing array) if using keepFrames

            Point3f tempPoint = new Point3f();
            Core.PixelSize depthPixelSize = Core.GetDepthPixelSpacing(sensorElevation);

            // Trim the depth array and cast ushort values to int
            Core.CopyAsIntArray(KinectController.depthFrameData, depthFrameDataInt, leftColumns, rightColumns, topRows, bottomRows, KinectController.depthHeight, KinectController.depthWidth);

            averageFrames = averageFrames < 1 ? 1 : averageFrames; // Make sure there is at least one frame in the render buffer

            // Reset everything when resizing Kinect's field of view or changing the amounts of frame to average across
            if (renderBuffer.Count > averageFrames || quadMesh.Faces.Count != (trimmedWidth - 2) * (trimmedHeight - 2))
            {
                renderBuffer.Clear();
                Array.Clear(runningSum, 0, runningSum.Length);
                renderBuffer.AddLast(depthFrameDataInt);
            }
            else
            {
                renderBuffer.AddLast(depthFrameDataInt);
            }

            Core.LogTiming(ref output, timer, $"Initial setup"); // Debug Info

            // Average across multiple frames
            for (int pixel = 0; pixel < depthFrameDataInt.Length; pixel++)
            {
                if (depthFrameDataInt[pixel] > 200 && depthFrameDataInt[pixel] <= lookupTable.Length) // We have a valid pixel. TODO remove reference to the lookup table
                    runningSum[pixel] += depthFrameDataInt[pixel];
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

                if (renderBuffer.Count >= averageFrames)
                    runningSum[pixel] -= renderBuffer.First.Value[pixel]; // Subtract the oldest value from the sum 
            }
            Core.LogTiming(ref output, timer, "Frames averaging"); // Debug Info

            if (blurRadius > 1) // Apply gaussian blur
            {
                GaussianBlurProcessor gaussianBlurProcessor = new GaussianBlurProcessor(blurRadius, trimmedWidth, trimmedHeight);
                gaussianBlurProcessor.Apply(averagedDepthFrameData);
                Core.LogTiming(ref output, timer, "Gaussian blurring"); // Debug Info
            }

            // Setup variables for per-pixel loop
            pointCloud = new Point3f[trimmedWidth * trimmedHeight];
            int arrayIndex = 0;
            for (int rows = 0; rows < trimmedHeight; rows++)
            {
                for (int columns = 0; columns < trimmedWidth; columns++)
                {
                    depthPoint = averagedDepthFrameData[arrayIndex];
                    tempPoint.X = (float) (columns * -unitsMultiplier * depthPixelSize.x);
                    tempPoint.Y = (float) (rows * -unitsMultiplier * depthPixelSize.y);
                    tempPoint.Z = (float) ((depthPoint - sensorElevation) * -unitsMultiplier);
                    pointCloud[arrayIndex] = tempPoint; // Add new point to point cloud itself
                    arrayIndex++;
                }
            }
            Core.LogTiming(ref output, timer, "Point cloud generation"); // Debug Info
            
            // First type of analysis that acts on the pixel array and produces vertex colors
            
            switch (Analysis.AnalysisManager.GetEnabledMeshColoring())
            {
                case Analytics.None analysis:
                    vertexColors = analysis.GetColorCloudForAnalysis();
                    break;
                case Analytics.Elevation analysis:
                    vertexColors = analysis.GetColorCloudForAnalysis(averagedDepthFrameData, sensorElevation);
                    break;
                case Analytics.Slope analysis:
                    vertexColors = analysis.GetColorCloudForAnalysis(averagedDepthFrameData,
                        trimmedWidth, trimmedHeight, depthPixelSize.x, depthPixelSize.y);
                    break;
                case Analytics.Aspect analysis:
                    // TODO: implementation
                    break;
                default:
                    break;
            }
    Core.LogTiming(ref output, timer, "Point cloud analysis"); // Debug Info
            
            // Keep only the desired amount of frames in the buffer
            while (renderBuffer.Count >= averageFrames)
            {
                renderBuffer.RemoveFirst();
            }

quadMesh = Core.CreateQuadMesh(quadMesh, pointCloud, vertexColors, trimmedWidth, trimmedHeight);
            if (keepFrames > 1)
                outputMesh.Insert(0, quadMesh.DuplicateMesh()); // Clone and prepend if keeping frames
            else
                outputMesh.Add(quadMesh);

            Core.LogTiming(ref output, timer, "Meshing"); // Debug Info

            // Second type of analysis that acts on the mesh and produces new geometry
            outputGeometry = new List<Rhino.Geometry.GeometryBase>();
            foreach (var enabledAnalysis in Analysis.AnalysisManager.GetEnabledMeshAnalytics())
            {
                switch (enabledAnalysis)
                {
                    case Analytics.Contours analysis:
                        analysis.GetGeometryForAnalysis(ref outputGeometry, contourInterval, quadMesh);
                        break;
                    case Analytics.WaterLevel analysis:
                        analysis.GetGeometryForAnalysis(ref outputGeometry, waterLevel, quadMesh);
                        break;
                    default:
                        break;
                }
            }
            Core.LogTiming(ref output, timer, "Mesh analysis"); // Debug Info

            // Trim the outputMesh List to length specified in keepFrames
            if (keepFrames > 1 && keepFrames<outputMesh.Count)
            {
                int framesToRemove = outputMesh.Count - keepFrames;
outputMesh.RemoveRange(keepFrames, framesToRemove > 0 ? framesToRemove : 0);
            }

            DA.SetDataList(0, outputMesh);
            DA.SetDataList(1, outputGeometry);
            DA.SetDataList(2, output); // For logging/debugging

            ScheduleSolve();
        }


        private void ShowComponentError(string errorMessage)
{
    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, errorMessage);
    ScheduleSolve(); // Ensure a future solve is scheduled despite an early return to SolveInstance()
}

private void ScheduleSolve()
{
    if (tickRate > 0) // Allow users to force manual recalculation
        base.OnPingDocument().ScheduleSolution(tickRate, new GH_Document.GH_ScheduleDelegate(ScheduleDelegate));
}

/// <summary>
/// Provides an Icon for every component that will be visible in the User Interface.
/// Icons need to be 24x24 pixels.
/// </summary>
protected override System.Drawing.Bitmap Icon => null;

/// <summary>
/// Each component must have a unique Guid to identify it. 
/// It is vital this Guid doesn't change otherwise old ghx files 
/// that use the old ID will partially fail during loading.
/// </summary>
public override Guid ComponentGuid => new Guid("f923f24d-86a0-4b7a-9373-23c6b7d2e162");
    }
}
