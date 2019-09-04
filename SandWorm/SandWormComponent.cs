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
        private List<Mesh> outputMesh = null;
        public static List<String> output = null;//debugging

        public List<Color> vertexColors;
        public Mesh quadMesh = new Mesh();

        public double minEl;
        public double maxEl;
        public Interval hueRange = new Interval(0, 0.333333333333);
        public double waterLevel;
        public double sensorElevation = 1060; //to do - fix hard wiring
        public int leftColumns = 0;
        public int rightColumns = 0;
        public int topRows = 0;
        public int bottomRows = 0;
        public int tickRate = 20; // In ms
        public static Rhino.UnitSystem units = Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem;
        public static double unitsMultiplier;

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
            pManager.AddIntegerParameter("TickRate", "TR", "The time interval, in milliseconds, to update geometry from the Kinect. Set as 0 to disable automatic updates.", GH_ParamAccess.item, tickRate);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
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
            DA.GetData<int>(7, ref tickRate);

            switch (units.ToString())
            {
                case "Kilometers":
                    unitsMultiplier = 0.0001;
                    break;

                case "Meters":
                    unitsMultiplier = 0.001;
                    break;

                case "Decimeters":
                    unitsMultiplier = 0.01;
                    break;

                case "Centimeters":
                    unitsMultiplier = 0.1;
                    break;

                case "Millimeters":
                    unitsMultiplier = 1;
                    break;

                case "Inches":
                    unitsMultiplier = 0.0393701;
                    break;

                case "Feet":
                    unitsMultiplier = 0.0328084;
                    break;
            }

            Stopwatch timer = Stopwatch.StartNew(); //debugging

            if (this.kinectSensor == null)
            {
                KinectController.AddRef();
                this.kinectSensor = KinectController.sensor;
                //KinectController.kinectGHC = this;
            }


            if (this.kinectSensor != null)
            {
                if (KinectController.depthFrameData != null)
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

                            tempPoint.X = (float)(columns * -unitsMultiplier * 3); //to do - fix arbitrary grid size of 3mm
                            tempPoint.Y = (float)(rows * -unitsMultiplier * 3); //to do - fix arbitrary grid size of 3mm

                            if (KinectController.depthFrameData[i] == 0) //check for invalid pixels
                            {
                                tempPoint.Z = (float)((KinectController.depthFrameData[i-1] - sensorElevation) * -unitsMultiplier);
                            }
                            else
                            {
                                tempPoint.Z = (float)((KinectController.depthFrameData[i] - sensorElevation) * -unitsMultiplier);
                            }
                            
                            vertexColors.Add(Core.ColorizeVertex(tempPoint.Z, maxEl, minEl, waterLevel, hueRange));
                            pointCloud.Add(tempPoint);
                        }
                    };
                    //debugging
                    timer.Stop();
                    output.Add("Point Cloud generation: " + timer.ElapsedMilliseconds.ToString() + " ms");

                    timer.Restart(); //debugging


                    quadMesh = Core.CreateQuadMesh(quadMesh, pointCloud, vertexColors, KinectController.depthWidth - leftColumns - rightColumns, KinectController.depthHeight - topRows - bottomRows);
                    outputMesh.Add(quadMesh);

                    timer.Stop(); //debugging
                    output.Add("Meshing: " + timer.ElapsedMilliseconds.ToString() + " ms");
                }

                DA.SetDataList(0, outputMesh);
                DA.SetDataList(1, output); //debugging
            }

            if (tickRate > 0) // Allow users to force manual recalculation
            {
                base.OnPingDocument().ScheduleSolution(tickRate, new GH_Document.GH_ScheduleDelegate(ScheduleDelegate));
            }
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
