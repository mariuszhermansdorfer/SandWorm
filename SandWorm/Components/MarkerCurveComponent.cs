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
    public class MarkerAreaComponent : BaseMarkerComponent
    {
        public MarkerAreaComponent() : base("Sandworm Area Markers", "SW PMarks",
            "Track color markers from the Kinect camera stream and output them as areas")
        {
        }

        protected override Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("41b279b6-643e-4d22-bc45-b47efa264ffb");
        
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Marker Areas", "A", "The areas of different color; as a Grasshopper curve", GH_ParamAccess.list);
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
            
            ScheduleSolve();
        }
    }
}