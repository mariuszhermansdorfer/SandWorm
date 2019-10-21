using System;
using System.Collections.Generic;
using Rhino.Geometry.Intersect;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace SandWorm
{
    public class CompareMeshes
    {
        public CompareMeshes()
        {
        }

        private double[] _meshElevationPoints;

        public double[] MeshElevationPoints
        {
            get { return _meshElevationPoints; }
            set { _meshElevationPoints = value; }
        }
    }
    public class MeshCompareComponent : GH_Component
    {
        public List<GeometryBase> outputSurface;
        private Curve inputRectangle;
        private double scaleFactor = 1;
        private double sensorElevation;
        public int leftColumns = 0;
        public int rightColumns = 0;
        public int topRows = 0;
        public int bottomRows = 0;
        public SetupOptions options; // List of options coming from the SWSetup component
        public CompareMeshes results; // Array of resulting elevation points 
        public List<string> output;
        public Mesh inputMesh;

        public MeshCompareComponent()
          : base("MeshCompare", "MeshCompare",
              "Visualizes elevation differences between meshes.",
              "Sandworm", "Sandbox")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Rectangle", "RC", "Rectangle", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh", "M", "Mesh to be compared to the Kinect's scan", GH_ParamAccess.item);
            pManager.AddNumberParameter("Scale Factor. 1 : ", "SF", "Scale factor for the referenced terrain.", GH_ParamAccess.item, scaleFactor);
            pManager.AddGenericParameter("SandWormOptions", "SWO", "Setup & Calibration options", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = false;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Surface", "S", "Sandbox surface in real-world scale", GH_ParamAccess.list);
            pManager.AddPointParameter("Points", "P", "Additional mesh analysis", GH_ParamAccess.list);
            pManager.AddGenericParameter("MeshCompareOptions", "MCO", "Resulting array of elevation points", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            options = new SetupOptions();
            results = new CompareMeshes();

            DA.GetData<Curve>(0, ref inputRectangle);
            DA.GetData<Mesh>(1, ref inputMesh);
            DA.GetData<double>(2, ref scaleFactor);
            DA.GetData<SetupOptions>(3, ref options);

            if (options.SensorElevation != 0) sensorElevation = options.SensorElevation; 
            if (scaleFactor <= 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Scale Factor must be greater than 0.");
            if (options.LeftColumns != 0) leftColumns = options.LeftColumns;
            if (options.RightColumns != 0) rightColumns = options.RightColumns;
            if (options.TopRows != 0) topRows = options.TopRows;
            if (options.BottomRows != 0) bottomRows = options.BottomRows;


            // Shared variables
            var unitsMultiplier = Core.ConvertDrawingUnits(Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem);
            sensorElevation /= unitsMultiplier; // Standardise to mm to match sensor units
            Core.PixelSize depthPixelSize = Core.GetDepthPixelSpacing(sensorElevation);
            var trimmedWidth = (512 - leftColumns - rightColumns) * depthPixelSize.x * unitsMultiplier * scaleFactor;
            var trimmedHeight = (424 - topRows - bottomRows) * depthPixelSize.y * unitsMultiplier * scaleFactor;

            // Initialize all the outputs
            output = new List<string>();
            outputSurface = new List<GeometryBase>();
            results.MeshElevationPoints = new double[(512 - leftColumns - rightColumns) * (424 - topRows - bottomRows)];
            List<Mesh> inputMeshes = new List<Mesh>{ inputMesh };

            PlaneSurface surface; // Used to create the point grid that matches the vertex grid of the live mesh
            if (inputRectangle == default(Curve))
            {
                surface = createPlaneFromMesh(inputMeshes, trimmedWidth, trimmedHeight);
            }
            else
            {
                surface = createPlaneFromCrop(inputRectangle, trimmedWidth, trimmedHeight);
            }
            outputSurface.Add(surface);

            // Place a point at a grid, corresponding to Kinect's depth map
            var points = new List<Point3d>();
            for (int i = 0; i < 424 - topRows - bottomRows; i++)
            {
                for (int j = 0; j < 512 - leftColumns - rightColumns; j++)
                {
                    var point = surface.PointAt(surface.Domain(0).Length / (512 - leftColumns - rightColumns) * j, surface.Domain(1).Length / (424 - topRows - bottomRows) * i);
                    points.Add(point);
                }
            }

            // Project all points onto the underlying mesh     
            var projectedPoints = Intersection.ProjectPointsToMeshes(inputMeshes, points, new Vector3d(0, 0, -1), 0.000001); // Need to use very high accuraccy, otherwise the function generates duplicate points
            if (projectedPoints.Length == 0) // Projection fails if there is no overlap
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No overlap between your cropping rectangle and comparison mesh found. " +
                                                                "Ensure that the rectangle is positioned above the comparison mesh.");
                return;
            }

            double min = (projectedPoints[0].Z / scaleFactor) / unitsMultiplier;

            // Prevent accessing projectedPoint indices that don't exist (e.g. when rectangle area is cropped small)
            int viablePixels;
            if (results.MeshElevationPoints.Length > projectedPoints.Length)
            {
                viablePixels = projectedPoints.Length;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "There were fewer projected pixels than mesh points and " +
                    "the results of the cut/fill analysis may be truncated or appear streaky.");
            }
            else
            {
                viablePixels = results.MeshElevationPoints.Length;
            }

            // Populate the mesh elevations array
            for (int i = 0; i < viablePixels; i++)
            {
                results.MeshElevationPoints[i] = (projectedPoints[i].Z / scaleFactor) / unitsMultiplier;
                if (results.MeshElevationPoints[i] < min)
                    min = results.MeshElevationPoints[i];
            }

            // Convert to Kinect's sensor coordinate system
            int bottomMargin = 0; // Set the lowest point of the mesh slightly above the table so that users still have some sand to play with
            for (int i = 0; i < results.MeshElevationPoints.Length; i++)
            {
                results.MeshElevationPoints[i] = sensorElevation - bottomMargin - results.MeshElevationPoints[i] + min;
            }

            // Output data
            DA.SetData(0, surface);
            DA.SetDataList(1, projectedPoints);
            DA.SetData(2, results);
        }
        
        private PlaneSurface createPlaneFromCrop(Curve inputRectangle, double trimmedWidth, double trimmedHeight) 
        {
            // Convert the input curve to polyline and construct a surface based on its segments
            var polyCurve = inputRectangle.ToPolyline(Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, Rhino.RhinoDoc.ActiveDoc.ModelAngleToleranceDegrees, 0.01, 100000);
            var polyLine = polyCurve.ToPolyline();
            var segments = polyLine.GetSegments();
            var plane = new Plane(segments[0].PointAt(0), segments[0].PointAt(1), segments[3].PointAt(0));
            return new PlaneSurface(plane, new Interval(0, trimmedWidth), new Interval(0, trimmedHeight));
        }

        private PlaneSurface createPlaneFromMesh(List<Mesh> inputMeshes, double trimmedWidth, double trimmedHeight)
        {
            var boundsPts = new List<Point3d>();
            // Getting the BoundingBox for the whole mesh is inaccurate; use its vertices instead
            foreach (Mesh mesh in inputMeshes)
            {
                boundsPts.AddRange(mesh.Vertices.ToPoint3dArray());
            }
            
            var boxPoints = new BoundingBox(boundsPts).GetCorners(); // Use top corners of the box for the surface plane
            var plane = new Plane(boxPoints[4], boxPoints[5], boxPoints[6]);
            return new PlaneSurface(plane, new Interval(0, trimmedWidth), new Interval(0, trimmedHeight));
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