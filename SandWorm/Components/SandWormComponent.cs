using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;

using Rhino;
using Rhino.Geometry;

using Grasshopper.Kernel;

using SandWorm.Analytics;
using static SandWorm.Core;
using static SandWorm.Kinect2Helpers;
using static SandWorm.Structs;
using static SandWorm.SandWormComponentUI;


namespace SandWorm
{
    public class SandWormComponent : GH_ExtendableComponent
    {
        #region Class Variables

        // Units & dimensions
        private Vector2 depthPixelSize;
        private double unitsMultiplier;

        private int active_Height = 0;
        private int active_Width = 0;
        private int trimmedHeight;
        private int trimmedWidth;

        // Data arrays
        private Point3f[] allPoints;
        private Color[] _vertexColors;
        private Mesh _quadMesh;

        private readonly LinkedList<int[]> renderBuffer = new LinkedList<int[]>();
        private int[] runningSum;
        private double[] elevationArray; // Array of elevation values for every pixel scanned during the calibration process
        private Vector2[] trimmedXYLookupTable;

        // Outputs
        private List<GeometryBase> _outputGeometry;
        private List<Mesh> _outputMesh;

        // Debugging
        public static List<string> output; 
        protected Stopwatch timer;

        // Boolean controls
        private bool calibrate;
        private bool reset;

        #endregion

        public SandWormComponent()
          : base("Sandworm Mesh", "SW Mesh",
            "Visualise Kinect depth data as a mesh", "SandWorm", "Visualisation")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Calibrate", "calibrate", "", GH_ParamAccess.item, calibrate);
            pManager.AddBooleanParameter("Reset", "reset", "", GH_ParamAccess.item, reset);
            pManager.AddColourParameter("Color Gradient", "color gradient", "", GH_ParamAccess.list);
            pManager.AddGenericParameter("Mesh", "mesh", "", GH_ParamAccess.item);

            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "geometry", "", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Options", "options", "", GH_ParamAccess.item);
        }

        protected override void Setup(GH_ExtendableComponentAttributes attr)
        {
            MainComponentUI(attr);
        }

        protected override void OnComponentLoaded()
        {
            base.OnComponentLoaded();
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(1, ref reset);
            if (reset)
            {
                KinectAzureController.sensor.Dispose();
                KinectAzureController.sensor = null;
                _quadMesh = null;
            }
                
            GeneralHelpers.SetupLogging(ref timer, ref output);
            unitsMultiplier = GeneralHelpers.ConvertDrawingUnits(RhinoDoc.ActiveDoc.ModelUnitSystem);

            // Trim 
            GetTrimmedDimensions((KinectTypes)_sensorType.Value, ref trimmedWidth, ref trimmedHeight, ref elevationArray, runningSum,
                                  _bottomRows.Value, _topRows.Value, _leftColumns.Value, _rightColumns.Value);

            // Setup sensor
            if ((KinectTypes)_sensorType.Value == KinectTypes.KinectForWindows)
            {
                KinectForWindows.SetupSensor();
                active_Height = KinectForWindows.depthHeight;
                active_Width = KinectForWindows.depthWidth;
                depthPixelSize = Kinect2Helpers.GetDepthPixelSpacing(_sensorElevation.Value);
            }
            else
            {
                KinectAzureController.SetupSensor((KinectTypes)_sensorType.Value, _sensorElevation.Value);
                KinectAzureController.CaptureFrame(); // Get a frame so the variables below have some values.
                active_Height = KinectAzureController.depthHeight;
                active_Width = KinectAzureController.depthWidth;

                trimmedXYLookupTable = new Vector2[trimmedWidth * trimmedHeight];
                Core.TrimXYLookupTable(KinectAzureController.idealXYCoordinates, trimmedXYLookupTable, KinectAzureController.verticalTiltCorrectionMatrix,
                                    _leftColumns.Value, _rightColumns.Value, _bottomRows.Value, _topRows.Value,
                                    active_Height, active_Width, unitsMultiplier);
            }
                


            // Initialize
            int[] depthFrameDataInt = new int[trimmedWidth * trimmedHeight]; 
            double[] averagedDepthFrameData = new double[trimmedWidth * trimmedHeight];

            if (runningSum == null || runningSum.Length < elevationArray.Length)
                runningSum = Enumerable.Range(1, elevationArray.Length).Select(i => new int()).ToArray();
            _outputMesh = new List<Mesh>();


            SetupRenderBuffer(depthFrameDataInt, (KinectTypes)_sensorType.Value,
                _leftColumns.Value, _rightColumns.Value, _bottomRows.Value, _topRows.Value, _quadMesh, trimmedWidth, trimmedHeight, _averagedFrames.Value,
                runningSum, renderBuffer);

            GeneralHelpers.LogTiming(ref output, timer, "Initial setup"); // Debug Info

            AverageAndBlurPixels(depthFrameDataInt, ref averagedDepthFrameData, runningSum, renderBuffer,
                _sensorElevation.Value, elevationArray, _averagedFrames.Value, _blurRadius.Value, trimmedWidth, trimmedHeight);

            allPoints = new Point3f[trimmedWidth * trimmedHeight];
            GeneratePointCloud(averagedDepthFrameData, trimmedXYLookupTable, KinectAzureController.verticalTiltCorrectionMatrix, allPoints, 
                renderBuffer, trimmedWidth, trimmedHeight, _sensorElevation.Value, unitsMultiplier, _averagedFrames.Value);

            // Produce 1st type of analysis that acts on the pixel array and assigns vertex colors
            GenerateMeshColors(ref _vertexColors, _analysisType.Value, averagedDepthFrameData, depthPixelSize, _colorGradientRange.Value,
                _sensorElevation.Value, trimmedWidth, trimmedHeight);

            GeneralHelpers.LogTiming(ref output, timer, "Point cloud analysis"); // Debug Info

            // Generate the mesh itself
            _quadMesh = CreateQuadMesh(_quadMesh, allPoints, _vertexColors, trimmedWidth, trimmedHeight);
            _outputMesh.Add(_quadMesh);

            GeneralHelpers.LogTiming(ref output, timer, "Meshing"); // Debug Info

            // Produce 2nd type of analysis that acts on the mesh and creates new geometry
            _outputGeometry = new List<GeometryBase>();

            if (_contourIntervalRange.Value > 0)
                new Contours().GetGeometryForAnalysis(ref _outputGeometry, _contourIntervalRange.Value, _quadMesh);

            if (_waterLevel.Value > 0)
                new WaterLevel().GetGeometryForAnalysis(ref _outputGeometry, _waterLevel.Value, _quadMesh);

            GeneralHelpers.LogTiming(ref output, timer, "Mesh analysis"); // Debug Info


            DA.SetDataList(0, _outputMesh);
            DA.SetDataList(1, output);

            ScheduleSolve();

        }

        protected override Bitmap Icon => Properties.Resources.Icons_Main;
        public override Guid ComponentGuid => new Guid("{53fefb98-1cec-4134-b707-0c366072af2c}");


        public override void AddedToDocument(GH_Document document)
        {
            if(Params.Input[0].SourceCount == 0)
            {
                List<IGH_DocumentObject> componentList = new List<IGH_DocumentObject>();
                PointF pivot;
                pivot = Attributes.Pivot;

                var calibrate = new Grasshopper.Kernel.Special.GH_ButtonObject();
                calibrate.CreateAttributes();
                calibrate.NickName = "calibrate";
                calibrate.Attributes.Pivot = new PointF(pivot.X - 250, pivot.Y - 46);
                calibrate.Attributes.ExpireLayout();
                calibrate.Attributes.PerformLayout();
                componentList.Add(calibrate);

                Params.Input[0].AddSource(calibrate);

                var reset = new Grasshopper.Kernel.Special.GH_ButtonObject();
                reset.CreateAttributes();
                reset.NickName = "reset";
                reset.Attributes.Pivot = new PointF(pivot.X - 250, pivot.Y - 21);
                reset.Attributes.ExpireLayout();
                reset.Attributes.PerformLayout();
                componentList.Add(reset);

                Params.Input[1].AddSource(reset);


                foreach (var component in componentList)
                    document.AddObject(component, false);


                document.UndoUtil.RecordAddObjectEvent("Add buttons", componentList);
            }
            
        }

        protected void ScheduleSolve()
        {
            OnPingDocument().ScheduleSolution(GeneralHelpers.ConvertFPStoMilliseconds(_refreshRate.Value), ScheduleDelegate);
        }
        protected void ScheduleDelegate(GH_Document doc)
        {
            ExpireSolution(false);
        }

    }
}