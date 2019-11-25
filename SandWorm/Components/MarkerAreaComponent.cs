using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Microsoft.Kinect;
using Rhino.Geometry;
using SandWorm.Components;
using SandWorm.Properties;

namespace SandWorm
{
    public class MarkerAreaComponent : BaseMarkerComponent
    {
        private List<Curve> markerAreas;
        public MarkerAreaComponent() : base("Sandworm Area Markers", "SW PMarks",
            "Track color markers from the Kinect's' camera and output as areas")
        {
        }

        protected override Bitmap Icon => Resources.icons_marker_areas;

        public override Guid ComponentGuid => new Guid("41b279b6-643e-4d22-bc45-b47efa264ffb");
        
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Marker Areas", "A", "The areas of different color; as a Grasshopper curve", GH_ParamAccess.list);
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

                // Translate identified areas back into Grasshopper geometry
                markerAreas = new List<Curve>();

                // TODO: translation

                DA.SetDataList(0, markerAreas);
            }
            else
            {
                // TODO: add warning?
            }
            binaryImage.Dispose();

            Core.LogTiming(ref output, timer, "Image processing"); // Debug Info
            DA.SetDataList(1, output); // For logging/debugging
            ScheduleSolve();
        }
    }
}