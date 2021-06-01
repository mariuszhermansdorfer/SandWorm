using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace SandWorm
{
    public class SandWormComponent : GH_ExtendableComponent
    {
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

        public SandWormComponent()
          : base("Sandworm Mesh", "SW Mesh",
            "Visualise Kinect depth data as a mesh", "SandWorm", "Visualisation")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Calibrate", "calibrate", "", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Reset", "reset", "", GH_ParamAccess.item, false);
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
            _sensorType.AddItem("Kinect for Windows", "Kinect for Windows");
            _sensorType.AddItem("Kinect Azure Narrow", "Kinect Azure Narrow");
            _sensorType.AddItem("Kinect Azure Wide", "Kinect Azure Wide");

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
            _rainDensity = new MenuSlider(contourIntervalHeader, 27, 0, 300, 0, 0);

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
            _blurRadius = new MenuSlider(blurRadiusHeader, 43, 0, 15, 0, 0);

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

        protected override void SolveInstance(IGH_DataAccess DA)
        {


        }

        protected override Bitmap Icon => Properties.Resources.icons_mesh;
        public override Guid ComponentGuid => new Guid("{53fefb98-1cec-4134-b707-0c366072af2c}");

    }
}