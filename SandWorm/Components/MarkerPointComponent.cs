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
    public class MarkerPointComponent : BaseMarkerComponent
    {

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

            GenerateColorImage();

            // Search image for the color and identify/classify

            ScheduleSolve();
        }
    }
}