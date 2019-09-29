using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace SandWorm
{
    public class CutFill : GH_Component
    {
        public List<GeometryBase> outputRectangle;
        private Curve inputRectangle;
        private double scaleFactor;
        private double sensorElevation;
        public int leftColumns = 0;
        public int rightColumns = 0;
        public int topRows = 0;
        public int bottomRows = 0;
        public int tickRate = 33; // In ms
        public List<double> options; // List of options coming from the SWSetup component
        public List<string> output;

        public CutFill()
          : base("CutFill", "CutFill",
              "Displays elevation differences between meshes.",
              "Sandworm", "Sandbox")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Rectangle", "RC", "Rectangle", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh", "M", "Mesh to be sampled", GH_ParamAccess.item);
            pManager.AddNumberParameter("Scale Factor. 1 : ", "SF", "Scale factor for the referenced terrain.", GH_ParamAccess.item);
            pManager.AddNumberParameter("SandWormOptions", "SWO", "Setup & Calibration options", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Analysis", "A", "Additional mesh analysis", GH_ParamAccess.list);
            pManager.AddPointParameter("Points", "P", "Additional mesh analysis", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            options = new List<double>();
            DA.GetData<Curve>(0, ref inputRectangle);
            DA.GetData<double>(2, ref scaleFactor);
            DA.GetDataList<double>(3, options);

            if (options.Count != 0) // TODO add more robust checking whether all the options have been provided by the user
            {
                sensorElevation = options[0];
                leftColumns = (int)options[1];
                rightColumns = (int)options[2];
                topRows = (int)options[3];
                bottomRows = (int)options[4];
                tickRate = (int)options[5];
            }
            
            output = new List<string>();
            outputRectangle = new List<GeometryBase>();

            var unitsMultiplier = Core.ConvertDrawingUnits(Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem);
            sensorElevation /= unitsMultiplier; // Standardise to mm to match sensor units
            Core.PixelSize depthPixelSize = Core.GetDepthPixelSpacing(sensorElevation);
            var trimmedWidth = (512 - leftColumns - rightColumns) * depthPixelSize.x * unitsMultiplier * scaleFactor;
            var trimmedHeight = (424 - topRows - bottomRows) * depthPixelSize.y * unitsMultiplier * scaleFactor;

            /*
            Point3d[] corners;
            Rhino.Input.RhinoGet.GetRectangle(out corners);
            var plane = new Plane(corners[0], corners[1], corners[3]);
            */

            var polyCurve = inputRectangle.ToPolyline(1, 1, 0.01, 100000);
            var polyLine = polyCurve.ToPolyline();
            var segments = polyLine.GetSegments();
            var plane = new Plane(segments[0].PointAt(0), segments[0].PointAt(1), segments[3].PointAt(0));

            var rectangle = new Rectangle3d(plane, trimmedWidth, trimmedHeight).ToNurbsCurve();


            var surface = new PlaneSurface(plane, new Interval(0, trimmedWidth), new Interval(0, trimmedHeight));

            var points = new List<Point3d>();
            for (int i = 0; i < 512 - leftColumns - rightColumns; i++)
            {
                for (int j = 0; j < 424 - topRows - bottomRows; j++)
                {
                    var point = surface.PointAt(surface.Domain(0).Length / (512 - leftColumns - rightColumns) * i, surface.Domain(1).Length / (424 - topRows - bottomRows) * j);
                    points.Add(point);
                }
            }



            outputRectangle.Add(rectangle);
            outputRectangle.Add(surface);

            DA.SetDataList(0, outputRectangle);
            DA.SetDataList(1, points);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("daa5e88f-e0d2-47a5-928f-9f2ecbd43036"); }
        }
    }
}