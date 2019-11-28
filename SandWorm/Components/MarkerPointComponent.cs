using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Microsoft.Kinect;
using Rhino.Geometry;
using SandWorm.Components;
using OpenCvSharp;
using SandWorm.Properties;
using Point3d = Rhino.Geometry.Point3d;

namespace SandWorm
{
    public class MarkerPointComponent : BaseMarkerComponent
    {
        private List<Point3d> markerPoints;
        public MarkerPointComponent() : base("Sandworm Point Markers", "SW Markers",
            "Track color markers from the Kinect's' camera and output as points")
        {
        }

        protected override Bitmap Icon => Resources.icons_marker_points;

        public override Guid ComponentGuid => new Guid("e4752964-5214-47b8-a7db-954166ba5eee");
        
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Marker Locations", "L", "The centers of different color areas; as a Grasshopper point", GH_ParamAccess.list);
            pManager.AddTextParameter("Output", "O", "Output", GH_ParamAccess.list); //debugging
        }

        protected override void SandwormSolveInstance(IGH_DataAccess DA)
        {
            SetupLogging();
            markerColors = new List<Color>();
            DA.GetDataList(0, markerColors);
            DA.GetData(1, ref colorFuzz);
            GetSandwormOptions(DA, 2, 0, 0);
            SetupKinect();
            Core.LogTiming(ref output, timer, "Initial setup"); // Debug Info

            var binaryImage = GenerateColorImage();
            Core.LogTiming(ref output, timer, "Image generation"); // Debug Info
            if (binaryImage != null)
            {
                // Search image for the color and identify/classify
                var keyPoints = new List<KeyPoint>();
                var detectorParameters = new SimpleBlobDetector.Params
                {
                    FilterByArea = true,
                    FilterByColor = true, // If it doesn't work; pre-filter the image
                    MinDistBetweenBlobs = 1,
                    MinArea = 10,
                    MaxArea = 20
                };
                Core.LogTiming(ref output, timer, "Detector setup"); // Debug Info

                foreach (Color markerColor in markerColors)
                {
                    var blobDetector = SimpleBlobDetector.Create(detectorParameters);
                    keyPoints.AddRange(blobDetector.Detect(binaryImage));
                    blobDetector.Dispose();
                }
                Core.LogTiming(ref output, timer, "Image blob detection"); // Debug Info

                // Translate identified points back into Grasshopper geometry
                markerPoints = new List<Point3d>();
                foreach (KeyPoint keyPoint in keyPoints)
                {
                    var x = keyPoint.Pt.X;
                    var y = keyPoint.Pt.Y;
                    markerPoints.Add(new Point3d(x, y, 0));
                }
                DA.SetDataList(0, markerPoints);
                Core.LogTiming(ref output, timer, "Blob output"); // Debug Info
            }
            else
            {
                // TODO: add warning?
            }
            binaryImage.Dispose();

            DA.SetDataList(1, output); // For logging/debugging
            ScheduleSolve();
        }
    }
}