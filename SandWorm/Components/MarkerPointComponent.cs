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
using Point3d = Rhino.Geometry.Point3d;

namespace SandWorm
{
    public class MarkerPointComponent : BaseMarkerComponent
    {
        private List<Point3d> markerPoints;
        public MarkerPointComponent() : base("Sandworm Point Markers", "SW Markers",
            "Track color markers from the Kinect camera stream and output them as points")
        {
        }

        protected override Bitmap Icon => null;

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

            var binaryImage = GenerateColorImage();
            if (binaryImage != null)
            {
                // Search image for the color and identify/classify
                var keyPoints = new List<KeyPoint>();
                var detectorParameters = new SimpleBlobDetector.Params();
                detectorParameters.FilterByArea = true;
                detectorParameters.FilterByColor = true; // If it doesn't work; pre-filter the image
                detectorParameters.MinDistBetweenBlobs = 1;
                detectorParameters.MinArea = 10;
                detectorParameters.MaxArea = 20;

                foreach (Color markerColor in markerColors)
                {
                    var blobDetector = SimpleBlobDetector.Create(detectorParameters);
                    keyPoints.AddRange(blobDetector.Detect(binaryImage));
                }

                // Translate identified points back into
                markerPoints = new List<Point3d>();
                foreach (KeyPoint keyPoint in keyPoints)
                {
                    var x = keyPoint.Pt.X;
                    var y = keyPoint.Pt.Y;
                    markerPoints.Add(new Point3d(x, y, 0));
                }
            }
            else
            {
                // TODO: add warning?
            }



            ScheduleSolve();
        }
    }
}