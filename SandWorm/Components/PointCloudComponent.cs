using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Microsoft.Kinect;


namespace SandWorm
{
    public class PointCloudComponent : Components.BaseKinectComponent
    {
        public PointCloudComponent() : base("Sandworm Point Cloud", "SW Points", "Visualise live data from the Kinect as Points") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SandwormSolveInstance(IGH_DataAccess DA)
        {
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("be133406-49aa-4b8d-a622-23f77161b03f");
    }
}
