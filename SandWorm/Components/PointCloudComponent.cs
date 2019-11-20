using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Microsoft.Kinect;
using Rhino.Geometry;
using SandWorm.Components;

namespace SandWorm
{
    public class PointCloudComponent : BaseKinectComponent
    {
        private bool _showPixelColor = true;
        private bool _writeOutPoints = false;
        private double _pixelSize = 1.0;
        // Outputs
        private PointCloud _pointCloud;
        private List<Point3d> _outputPoints;

        public PointCloudComponent() : base("Sandworm Point Cloud", "SW PCloud",
            "Visualise live data from the Kinect as a point cloud")
        {
        }

        protected override Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("be133406-49aa-4b8d-a622-23f77161b03f");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("ShowColor", "PC", "Whether pixels are colored or not.", GH_ParamAccess.item,
                _showPixelColor);
            pManager.AddBooleanParameter("PointsList", "PL", "Write out a list of points (will be slow).",
                GH_ParamAccess.item, _writeOutPoints);
            pManager.AddNumberParameter("PointSize", "PS", "The size of the points within the point cloud, as displayed by this component.",
                GH_ParamAccess.item, _pixelSize);
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
            pManager[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point Cloud", "PC", "Point Cloud", GH_ParamAccess.list);
            pManager.AddPointParameter("Points", "P", "Points", GH_ParamAccess.list);
            pManager.AddTextParameter("Output", "O", "Output", GH_ParamAccess.list); //debugging
        }

        protected override void SandwormSolveInstance(IGH_DataAccess DA)
        {
            SetupLogging();
            DA.GetData(0, ref _showPixelColor);
            DA.GetData(1, ref _writeOutPoints);
            DA.GetData(2, ref _pixelSize);
            GetSandwormOptions(DA, 5, 3, 4);
            SetupKinect();

            var depthFrameDataInt = new int[trimmedWidth * trimmedHeight];
            var averagedDepthFrameData = new double[trimmedWidth * trimmedHeight];

            // Initialize outputs
            if (keepFrames <= 1 || _outputPoints == null)
            {
                // Don't replace prior frames (by clearing array) if using keepFrames
                _outputPoints = new List<Point3d>();
            }

            SetupRenderBuffer(depthFrameDataInt, null);
            Core.LogTiming(ref output, timer, "Initial setup"); // Debug Info

            AverageAndBlurPixels(depthFrameDataInt, ref averagedDepthFrameData);

            GeneratePointCloud(averagedDepthFrameData);

            // Assign colors and pixels to the point cloud
            _pointCloud = new PointCloud(); // TODO intelligently manipulate the point cloud
            // TODO: actually color pixels by their RGB values and/or using analytic methods
            if (allPoints.Length > 0)
            {
                for (int i = 0; i < allPoints.Length; i++) // allPoints.Length
                {
                    if (_showPixelColor)
                    {
                        _pointCloud.Add(allPoints[i], Color.Aqua);
                    }
                    else
                    {
                        _pointCloud.Add(allPoints[i]);
                    }
                }
                Core.LogTiming(ref output, timer, "Point cloud construction"); // Debug Info
            }

            //DA.SetDataList(0, ); // TODO: pointcloud output
            if (_writeOutPoints)
            {
                // Cast to GH_Point for performance reasons
                DA.SetDataList(0, allPoints.Select(x => new GH_Point(x)));
            }
            DA.SetDataList(1, output); // For logging/debugging
            ScheduleSolve();
        }

        //Draw all points in this method.
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (_pointCloud == null)
                return;
            else
                args.Display.DrawPointCloud(_pointCloud, (float)_pixelSize);
        }
    }
}