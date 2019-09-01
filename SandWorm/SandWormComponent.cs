using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics; //debugging
using Grasshopper.Kernel;
using Rhino.Geometry;
using Microsoft.Kinect;
// comment 
// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

//Test comment
namespace SandWorm
{
    public class SandWorm : GH_Component
    {
        private KinectSensor kinectSensor = null;
        private List<Point3f> pointCloud = null;
        private Mesh quadMesh = null;
        private List<Mesh> outputMesh = null;
        private List<String> output = null;//debugging

        public List<Color> vertexColors;
        public Mesh m = new Mesh();

        public double minEl;
        public double maxEl;
        public Interval hueRange = new Interval(0, 0.333333333333);
        public double waterLevel;
        public double sensorElevation = 1.06; //to do - fix hard wiring
        public int leftColumns = 0;
        public int rightColumns = 0;
        public int topRows = 0;
        public int bottomRows = 0;

        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SandWorm()
          : base("SandWorm", "SandWorm",
              "Kinect v2 Augmented Reality Sandbox",
              "Sandworm", "Sandbox")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("WaterLevel", "WL", "WaterLevel", GH_ParamAccess.item, 0.00f);
            pManager.AddNumberParameter("Minimum Elevation", "minEl", "Minimum Elevation", GH_ParamAccess.item, -0.02f);
            pManager.AddNumberParameter("Maximum Elevation", "maxEl", "Maximum Elevation", GH_ParamAccess.item, 0.05f);
            pManager.AddIntegerParameter("LeftColumns", "LC", "Number of columns to trim from the left", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("RightColumns", "RC", "Number of columns to trim from the right", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("TopRows", "TR", "Number of rows to trim from the top", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("BottomRows", "BR", "Number of rows to trim from the bottom", GH_ParamAccess.item, 0);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Resulting Mesh", GH_ParamAccess.list);
            pManager.AddTextParameter("Output", "O", "Output", GH_ParamAccess.list); //debugging
        }

        private void ScheduleDelegate(GH_Document doc)
        {
            ExpireSolution(false);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData<double>(0, ref waterLevel);
            DA.GetData<double>(1, ref minEl);
            DA.GetData<double>(2, ref maxEl);
            DA.GetData<int>(3, ref leftColumns);
            DA.GetData<int>(4, ref rightColumns);
            DA.GetData<int>(5, ref topRows);
            DA.GetData<int>(6, ref bottomRows);


            Stopwatch timer = Stopwatch.StartNew(); //debugging

            if (this.kinectSensor == null)
            {
                KinectController.AddRef();
                this.kinectSensor = KinectController.sensor;
                //KinectController.kinectGHC = this;
            }


            if (this.kinectSensor != null)
            {
                if (KinectController.cameraSpacePoints != null)
                {
                    pointCloud = new List<Point3f>();
                    Point3f tempPoint = new Point3f();
                    outputMesh = new List<Mesh>();
                    output = new List<String>(); //debugging
                    vertexColors = new List<Color>();

                    for (int rows = topRows; rows < KinectController.depthHeight - bottomRows; rows++)

                    {
                        for (int columns = rightColumns; columns < KinectController.depthWidth - leftColumns; columns++)
                        {

                            int i = rows * KinectController.depthWidth + columns;
                            CameraSpacePoint p = KinectController.cameraSpacePoints[i];

                            tempPoint.X = (float)Math.Round(p.X * -1, 3);
                            tempPoint.Y = (float)Math.Round(p.Y, 3);
                            tempPoint.Z = (float)Math.Round(p.Z * -1 + sensorElevation, 3);

                            vertexColors.Add(Core.ColorizeVertex(tempPoint.Z, maxEl, minEl, waterLevel, hueRange));
                            pointCloud.Add(tempPoint);
                        }
                    };
                    //debugging
                    timer.Stop();
                    output.Add("Point Cloud generation: " + timer.ElapsedMilliseconds.ToString() + " ms");

                    timer.Restart(); //debugging


                    quadMesh = Core.CreateQuadMesh(m, pointCloud, vertexColors, KinectController.depthWidth - leftColumns - rightColumns, KinectController.depthHeight - topRows - bottomRows);
                    outputMesh.Add(quadMesh);

                    timer.Stop(); //debugging
                    output.Add("Meshing: " + timer.ElapsedMilliseconds.ToString() + " ms");
                }

                DA.SetDataList(0, outputMesh);
                DA.SetDataList(1, output); //debugging
            }
            base.OnPingDocument().ScheduleSolution(20, new GH_Document.GH_ScheduleDelegate(ScheduleDelegate));
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("f923f24d-86a0-4b7a-9373-23c6b7d2e162"); }
        }
    }
}
