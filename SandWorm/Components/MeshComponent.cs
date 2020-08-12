using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;
using SandWorm.Analytics;
using SandWorm.Components;
using SandWorm.Properties;

namespace SandWorm
{
    public class MeshComponent : BaseKinectComponent
    {
        private Color[] _vertexColors;
        private Mesh _quadMesh = new Mesh();
        // Input Parameters
        private double _waterLevel = 50;
        private double _contourInterval = 10;
        // Outputs
        private List<GeometryBase> _outputGeometry;
        private List<Mesh> _outputMesh;

        public MeshComponent() : base("Sandworm Mesh", "SW Mesh", 
            "Visualise Kinect depth data as a mesh", "Visualisation")
        {
        }

        protected override Bitmap Icon => Resources.icons_mesh;

        public override Guid ComponentGuid => new Guid("f923f24d-86a0-4b7a-9373-23c6b7d2e162");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("WaterLevel", "WL", "WaterLevel", GH_ParamAccess.item, _waterLevel);
            pManager.AddNumberParameter("ContourInterval", "CI", "The interval (if this analysis is enabled)",
                GH_ParamAccess.item, _contourInterval);
            pManager.AddIntegerParameter("AverageFrames", "AF",
                "Amount of depth frames to average across. This number has to be greater than zero.",
                GH_ParamAccess.item, averageFrames);
            pManager.AddIntegerParameter("BlurRadius", "BR", "Radius for Gaussian blur.", GH_ParamAccess.item,
                blurRadius);
            pManager.AddGenericParameter("SandwormOptions", "SWO", "Setup & Calibration options", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Resulting mesh", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Analysis", "A", "Additional mesh analysis", GH_ParamAccess.list);
            pManager.AddTextParameter("Output", "O", "Output", GH_ParamAccess.list); //debugging
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            foreach (var option in Analysis.AnalysisManager.options) // Add analysis items to menu
            {
                Menu_AppendItem(menu, option.Name, SetMeshVisualisation, true, option.isEnabled);
                // Create reference to the menu item in the analysis class
                option.MenuItem = (ToolStripMenuItem) menu.Items[menu.Items.Count - 1];
                if (!option.IsExclusive)
                    Menu_AppendSeparator(menu);
            }
        }

        private void SetMeshVisualisation(object sender, EventArgs e)
        {
            Analysis.AnalysisManager.SetEnabledOptions((ToolStripMenuItem) sender);
            _quadMesh.VertexColors.Clear(); // Must flush mesh colors to properly updated display
            ExpireSolution(true);
        }

        protected override void SandwormSolveInstance(IGH_DataAccess DA)
        {
            SetupLogging();
            DA.GetData(0, ref _waterLevel);
            DA.GetData(1, ref _contourInterval);
            GetSandwormOptions(DA, 4, 2, 3);

            SetupKinect();
            var depthFrameDataInt = new int[trimmedWidth * trimmedHeight];
            var averagedDepthFrameData = new double[trimmedWidth * trimmedHeight];

            // Initialize outputs
            if (keepFrames <= 1 || _outputMesh == null)
                _outputMesh = new List<Mesh>(); // Don't replace prior frames (by clearing array) if using keepFrames

            
            //If kinect type Azure K4AController.CaptureFrame() else do nothing

            SetupRenderBuffer(depthFrameDataInt, _quadMesh);
            Core.LogTiming(ref output, timer, "Initial setup"); // Debug Info

            AverageAndBlurPixels(depthFrameDataInt, ref averagedDepthFrameData);

            GeneratePointCloud(averagedDepthFrameData);

            // Produce 1st type of analysis that acts on the pixel array and assigns vertex colors
            switch (Analysis.AnalysisManager.GetEnabledMeshColoring())
            {
                case None analysis:
                    _vertexColors = analysis.GetColorCloudForAnalysis();
                    break;
                case Elevation analysis:
                    _vertexColors = analysis.GetColorCloudForAnalysis(averagedDepthFrameData, sensorElevation);
                    break;
                case Slope analysis:
                    _vertexColors = analysis.GetColorCloudForAnalysis(averagedDepthFrameData,
                        trimmedWidth, trimmedHeight, depthPixelSize.x, depthPixelSize.y);
                    break;
                case Aspect analysis:
                    _vertexColors = analysis.GetColorCloudForAnalysis(averagedDepthFrameData,
                        trimmedWidth, trimmedHeight);
                    break;
            }
            Core.LogTiming(ref output, timer, "Point cloud analysis"); // Debug Info

            // Generate the mesh itself
            _quadMesh = Core.CreateQuadMesh(_quadMesh, allPoints, _vertexColors, trimmedWidth, trimmedHeight);
            if (keepFrames > 1)
                _outputMesh.Insert(0, _quadMesh.DuplicateMesh()); // Clone and prepend if keeping frames
            else
                _outputMesh.Add(_quadMesh);

            Core.LogTiming(ref output, timer, "Meshing"); // Debug Info

            // Produce 2nd type of analysis that acts on the mesh and creates new geometry
            _outputGeometry = new List<GeometryBase>();
            foreach (var enabledAnalysis in Analysis.AnalysisManager.GetEnabledMeshAnalytics())
                switch (enabledAnalysis)
                {
                    case Contours analysis:
                        analysis.GetGeometryForAnalysis(ref _outputGeometry, _contourInterval, _quadMesh);
                        break;
                    case WaterLevel analysis:
                        analysis.GetGeometryForAnalysis(ref _outputGeometry, _waterLevel, _quadMesh);
                        break;
                }
            Core.LogTiming(ref output, timer, "Mesh analysis"); // Debug Info

            // Trim the _outputMesh List to length specified in keepFrames
            if (keepFrames > 1 && keepFrames < _outputMesh.Count)
            {
                var framesToRemove = _outputMesh.Count - keepFrames;
                _outputMesh.RemoveRange(keepFrames, framesToRemove > 0 ? framesToRemove : 0);
            }

            DA.SetDataList(0, _outputMesh);
            DA.SetDataList(1, _outputGeometry);
            DA.SetDataList(2, output); // For logging/debugging

            ScheduleSolve();
        }
    }
}