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


namespace SandWorm
{
    public class SandWormComponent : GH_ExtendableComponent
    {
        #region UI variables

        private MenuDropDown _sensorType;
        private MenuSlider _sensorElevation;
        private MenuSlider _leftColumns;
        private MenuSlider _rightColumns;
        private MenuSlider _topRows;
        private MenuSlider _bottomRows;

        private MenuDropDown _outputType;
        private MenuDropDown _analysisType;
        private MenuSlider _colorGradientRange;
        private MenuSlider _contourIntervalRange;
        private MenuSlider _waterLevel;
        private MenuSlider _rainDensity;

        private MenuSlider _averagedFrames;
        private MenuSlider _blurRadius;
        #endregion

        private Color[] _vertexColors;
        private Mesh _quadMesh = new Mesh();
        public static List<string> output; // Debugging
        protected Stopwatch timer;

        // Outputs
        private List<GeometryBase> _outputGeometry;
        private List<Mesh> _outputMesh;


        private double[] elevationArray; // Array of elevation values for every pixel scanned during the calibration process
        private Vector2[] trimmedXYLookupTable;
        private double[] verticalTiltCorrectionLookupTable;
        // Derived
        private Vector2 depthPixelSize;
        private double unitsMultiplier;
        private double sensorElevation;
        private Point3f[] allPoints;
        private int trimmedHeight;
        private int trimmedWidth;
        private readonly LinkedList<int[]> renderBuffer = new LinkedList<int[]>();
        private int[] runningSum;
        private int active_Height = 0;
        private int active_Width = 0;
        private bool calibrate;
        private bool reset;


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
            #region Sensor Type
            MenuPanel optionsMenuPanel = new MenuPanel(0, "panel_options")
            {
                Name = "Options for the SandWorm component.", // <- mouse over header for the entire "options" fold-out.
                Header = "Define custom parameters here." // <- mouse over description
            };

            GH_ExtendableMenu optionsMenu = new GH_ExtendableMenu(0, "menu_options")
            {
                Name = "Sensor", // <- Foldable header
                Header = "Setup the Kinect Sensor." // <- Foldable mouseOver
            };

            MenuStaticText sensorTypeHeader = new MenuStaticText("Sensor type", "Choose which Kinect Version you have.");
            _sensorType = new MenuDropDown(0, "Sensor type", "Choose Kinect Version");
            _sensorType.AddItem("Kinect Azure Narrow", "Kinect Azure Narrow");
            _sensorType.AddItem("Kinect Azure Wide", "Kinect Azure Wide");
            _sensorType.AddItem("Kinect for Windows", "Kinect for Windows");

            MenuStaticText sensorElevationHeader = new MenuStaticText("Sensor elevation", "Distance between the sensor and the table. \nInput should be in drawing units.");
            _sensorElevation = new MenuSlider(sensorElevationHeader, 1, 250, 1500, 700, 0);

            MenuStaticText leftColumnsHeader = new MenuStaticText("Left columns", "Number of pixels to trim from the left.");
            _leftColumns = new MenuSlider(leftColumnsHeader, 2, 0, 200, 0, 0);

            MenuStaticText rightColumnsHeader = new MenuStaticText("Right columns", "Number of pixels to trim from the right.");
            _rightColumns = new MenuSlider(rightColumnsHeader, 3, 0, 200, 0, 0);

            MenuStaticText topRowsHeader = new MenuStaticText("Top rows", "Number of pixels to trim from the top.");
            _topRows = new MenuSlider(topRowsHeader, 4, 0, 200, 0, 0);

            MenuStaticText bottomRowsHeader = new MenuStaticText("Bottom rows", "Number of pixels to trim from the bottom.");
            _bottomRows = new MenuSlider(bottomRowsHeader, 5, 0, 200, 0, 0);

            optionsMenu.AddControl(optionsMenuPanel);
            attr.AddMenu(optionsMenu);

            optionsMenuPanel.AddControl(sensorTypeHeader);
            optionsMenuPanel.AddControl(_sensorType);
            optionsMenuPanel.AddControl(sensorElevationHeader);
            optionsMenuPanel.AddControl(_sensorElevation);
            optionsMenuPanel.AddControl(leftColumnsHeader);
            optionsMenuPanel.AddControl(_leftColumns);
            optionsMenuPanel.AddControl(rightColumnsHeader);
            optionsMenuPanel.AddControl(_rightColumns);
            optionsMenuPanel.AddControl(topRowsHeader);
            optionsMenuPanel.AddControl(_topRows);
            optionsMenuPanel.AddControl(bottomRowsHeader);
            optionsMenuPanel.AddControl(_bottomRows);

            #endregion

            #region Analysis
            MenuPanel analysisPanel = new MenuPanel(20, "panel_analysis")
            {
                Name = "Analysis",
                Header = "Define custom analysis parameters."
            };
            GH_ExtendableMenu analysisMenu = new GH_ExtendableMenu(21, "menu_analysis")
            {
                Name = "Analysis",
                Header = "Define custom analysis parameters."
            };

            MenuStaticText outputTypeHeader = new MenuStaticText("Output type", "Choose which type of geometry to output.");
            _outputType = new MenuDropDown(22, "Ouput type", "Choose type of geometry to output.");
            _outputType.AddItem("Mesh", "Mesh");
            _outputType.AddItem("Point Cloud", "Point Cloud");

            MenuStaticText analysisTypeHeader = new MenuStaticText("Analysis type", "Choose which type of analysis to perform on scanned geometry.");
            _analysisType = new MenuDropDown(23, "Ouput type", "Choose type of geometry to output.");
            _analysisType.AddItem("None", "None");
            _analysisType.AddItem("RGB", "RGB");
            _analysisType.AddItem("Elevation", "Elevation");
            _analysisType.AddItem("Slope", "Slope");
            _analysisType.AddItem("Aspect", "Aspect");
            _analysisType.AddItem("Cut Fill", "Cut & Fill");

            MenuStaticText colorGradientHeader = new MenuStaticText("Color gradient range", "Maximum value for elevation analysis. \nInput should be in drawing units.");
            _colorGradientRange = new MenuSlider(colorGradientHeader, 24, 1, 500, 250, 0);

            MenuStaticText contourIntervalHeader = new MenuStaticText("Contour interval", "Define spacing between contours. \nInput should be in drawing units.");
            _contourIntervalRange = new MenuSlider(contourIntervalHeader, 25, 0, 30, 0, 0);

            MenuStaticText waterLevelHeader = new MenuStaticText("Water level", "Define distance between the table and a simulated water surface. \nInput should be in drawing units.");
            _waterLevel = new MenuSlider(contourIntervalHeader, 26, 0, 300, 0, 0);

            MenuStaticText rainDensityHeader = new MenuStaticText("Rain density", "Define spacing between simulated rain drops. \nInput should be in drawing units.");
            _rainDensity = new MenuSlider(contourIntervalHeader, 27, 1, 300, 50, 0);

            analysisMenu.AddControl(analysisPanel);
            attr.AddMenu(analysisMenu);

            analysisPanel.AddControl(outputTypeHeader);
            analysisPanel.AddControl(_outputType);
            analysisPanel.AddControl(analysisTypeHeader);
            analysisPanel.AddControl(_analysisType);
            analysisPanel.AddControl(colorGradientHeader);
            analysisPanel.AddControl(_colorGradientRange);
            analysisPanel.AddControl(contourIntervalHeader);
            analysisPanel.AddControl(_contourIntervalRange);
            analysisPanel.AddControl(waterLevelHeader);
            analysisPanel.AddControl(_waterLevel);
            analysisPanel.AddControl(rainDensityHeader);
            analysisPanel.AddControl(_rainDensity);

            #endregion

            #region Post processing

            MenuPanel postProcessingPanel = new MenuPanel(40, "panel_analysis")
            {
                Name = "Post Processing",
                Header = "Define custom post processing parameters."
            };
            GH_ExtendableMenu postProcessingMenu = new GH_ExtendableMenu(41, "menu_analysis")
            {
                Name = "Post Processing",
                Header = "Define custom post processing parameters."
            };

            MenuStaticText averagedFramesHeader = new MenuStaticText("Averaged frames", "Number of frames to average across.");
            _averagedFrames = new MenuSlider(averagedFramesHeader, 42, 1, 30, 1, 0);

            MenuStaticText blurRadiusHeader = new MenuStaticText("Blur Radius", "Define the extent of gaussian blurring.");
            _blurRadius = new MenuSlider(blurRadiusHeader, 43, 0, 15, 1, 0);

            postProcessingMenu.AddControl(postProcessingPanel);
            attr.AddMenu(postProcessingMenu);

            postProcessingPanel.AddControl(averagedFramesHeader);
            postProcessingPanel.AddControl(_averagedFrames);
            postProcessingPanel.AddControl(blurRadiusHeader);
            postProcessingPanel.AddControl(_blurRadius);

            #endregion
        }

        protected override void OnComponentLoaded()
        {
            base.OnComponentLoaded();
        }

        public override void AddedToDocument(GH_Document document)
        {
            GH_Document grasshopperDocument = OnPingDocument();
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
                grasshopperDocument.AddObject(component, false);

            
            grasshopperDocument.UndoUtil.RecordAddObjectEvent("Add buttons", componentList);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(1, ref reset);
            if (reset)
            {
                KinectAzureController.sensor.Dispose();
                KinectAzureController.sensor = null;
            }
                
            //GeneralHelpers.SetupLogging(timer, output);
            unitsMultiplier = GeneralHelpers.ConvertDrawingUnits(RhinoDoc.ActiveDoc.ModelUnitSystem);
            sensorElevation = _sensorElevation.Value / unitsMultiplier; // Standardise to mm to match sensor units

            // Trim 
            GetTrimmedDimensions((KinectTypes)_sensorType.Value, ref trimmedWidth, ref trimmedHeight, ref elevationArray, runningSum,
                                  _bottomRows.Value, _topRows.Value, _leftColumns.Value, _rightColumns.Value);

            // Setup sensor
            if ((KinectTypes)_sensorType.Value == KinectTypes.KinectForWindows)
            {
                KinectForWindows.SetupSensor();
                active_Height = KinectForWindows.depthHeight;
                active_Width = KinectForWindows.depthWidth;
                depthPixelSize = Kinect2Helpers.GetDepthPixelSpacing(sensorElevation);
            }
            else
            {
                KinectAzureController.SetupSensor((KinectTypes)_sensorType.Value, sensorElevation);
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

            //GeneralHelpers.LogTiming(ref output, timer, "Initial setup"); // Debug Info

            AverageAndBlurPixels(depthFrameDataInt, ref averagedDepthFrameData, runningSum, renderBuffer,
                sensorElevation, elevationArray, _averagedFrames.Value, _blurRadius.Value, trimmedWidth, trimmedHeight);

            allPoints = new Point3f[trimmedWidth * trimmedHeight];
            GeneratePointCloud(averagedDepthFrameData, trimmedXYLookupTable, KinectAzureController.verticalTiltCorrectionMatrix, allPoints, 
                renderBuffer, trimmedWidth, trimmedHeight, sensorElevation, unitsMultiplier, _averagedFrames.Value);



            // Produce 1st type of analysis that acts on the pixel array and assigns vertex colors
            switch (_analysisType.Value)
            {
                case 0: // None
                    _vertexColors = new None().GetColorCloudForAnalysis();
                    break;

                case 1: // TODO: RGB
                    break;

                case 2: // Elevation
                    _vertexColors = new Elevation().GetColorCloudForAnalysis(averagedDepthFrameData, _sensorElevation.Value);
                    break;

                case 3: // Slope
                    _vertexColors = new Slope().GetColorCloudForAnalysis(averagedDepthFrameData,
                        trimmedWidth, trimmedHeight, depthPixelSize.X, depthPixelSize.Y);
                    break;

                case 4: // Aspect
                    _vertexColors = new Aspect().GetColorCloudForAnalysis(averagedDepthFrameData,
                        trimmedWidth, trimmedHeight);
                    break;

                case 5: // TODO: Cut & Fill
                    break;
            }
            //GeneralHelpers.LogTiming(ref output, timer, "Point cloud analysis"); // Debug Info

            // Generate the mesh itself
            _quadMesh = CreateQuadMesh(_quadMesh, allPoints, _vertexColors, trimmedWidth, trimmedHeight);
            _outputMesh.Add(_quadMesh);

            //GeneralHelpers.LogTiming(ref output, timer, "Meshing"); // Debug Info

            // Produce 2nd type of analysis that acts on the mesh and creates new geometry
            _outputGeometry = new List<GeometryBase>();

            if (_contourIntervalRange.Value > 0)
                new Contours().GetGeometryForAnalysis(ref _outputGeometry, _contourIntervalRange.Value, _quadMesh);

            if (_waterLevel.Value > 0)
                new WaterLevel().GetGeometryForAnalysis(ref _outputGeometry, _waterLevel.Value, _quadMesh);

            //GeneralHelpers.LogTiming(ref output, timer, "Mesh analysis"); // Debug Info


            DA.SetDataList(0, _outputMesh);
            DA.SetDataList(1, _outputGeometry);

            ScheduleSolve();

        }

        protected override Bitmap Icon => Properties.Resources.icons_mesh;
        public override Guid ComponentGuid => new Guid("{53fefb98-1cec-4134-b707-0c366072af2c}");

        protected void ScheduleDelegate(GH_Document doc)
        {
            ExpireSolution(false);
        }

        protected void ScheduleSolve()
        {
            OnPingDocument().ScheduleSolution(30, ScheduleDelegate);
        }

    }
}