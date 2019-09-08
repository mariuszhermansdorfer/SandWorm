using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics; //debugging
using Grasshopper.Kernel;
using Rhino.Geometry;
using Microsoft.Kinect;
using System.Windows.Forms;
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
        private Point3f[] pointCloud;
        private List<Mesh> outputMesh = null;
        public static List<String> output = null;//debugging
        private readonly Queue<ushort[]> renderBuffer = new Queue<ushort[]>();


        public static int depthPoint;
        public static Color[] lookupTable = new Color[1500]; //to do - fix arbitrary value assuming 1500 mm as max distance from the kinect sensor
        enum MeshColorStyle { noColor, byElevation };
        private MeshColorStyle selectedColorStyle = MeshColorStyle.byElevation; // Must be private to be less accessible than enum type
        public Color[] vertexColors;
        public Mesh quadMesh = new Mesh();

        public int waterLevel = 1000;
        public int colorRampSpan = 300; // Arbitrary defalut value (must be >0)
        public double sensorElevation = 1000; // Arbitrary default value (must be >0)
        public int leftColumns = 0;
        public int rightColumns = 0;
        public int topRows = 0;
        public int bottomRows = 0;
        public int tickRate = 20; // In ms
        public int averageFrames = 1;
        public int blurRadius = 1;
        public static Rhino.UnitSystem units = Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem;
        public static double unitsMultiplier;
        public Color startColor = Color.FromArgb(255, 128, 128, 128);
        public Color endColor = Color.FromArgb(255, 255, 255, 255);


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
            pManager.AddNumberParameter("SensorHeight", "SH", "The height (in document units) of the sensor above your model", GH_ParamAccess.item, sensorElevation);
            pManager.AddIntegerParameter("WaterLevel", "WL", "WaterLevel", GH_ParamAccess.item, waterLevel);
            pManager.AddIntegerParameter("LeftColumns", "LC", "Number of columns to trim from the left", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("RightColumns", "RC", "Number of columns to trim from the right", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("TopRows", "TR", "Number of rows to trim from the top", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("BottomRows", "BR", "Number of rows to trim from the bottom", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("TickRate", "TR", "The time interval, in milliseconds, to update geometry from the Kinect. Set as 0 to disable automatic updates.", GH_ParamAccess.item, tickRate);
            pManager.AddIntegerParameter("AverageFrames", "AF", "Amount of depth frames to average across. This number has to be greater than zero.", GH_ParamAccess.item, averageFrames);
            pManager.AddIntegerParameter("BlurRadius", "BR", "Radius for the gaussian blur.", GH_ParamAccess.item, blurRadius);
            pManager.AddIntegerParameter("ColorRampSpan", "CS", "Color Ramp span", GH_ParamAccess.item, colorRampSpan);
            pManager.AddColourParameter("StartColor", "SC", "Start Color", GH_ParamAccess.item, startColor);
            pManager.AddColourParameter("EndColor", "EC", "End Color", GH_ParamAccess.item, endColor);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
            pManager[8].Optional = true;
            pManager[9].Optional = true;
            pManager[10].Optional = true;
            pManager[11].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Resulting Mesh", GH_ParamAccess.list);
            pManager.AddTextParameter("Output", "O", "Output", GH_ParamAccess.list); //debugging
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Color Mesh by Elevation", SetMeshColorStyle, true, selectedColorStyle == MeshColorStyle.byElevation);
            menu.Items[menu.Items.Count - 1].Tag = MeshColorStyle.byElevation;
            Menu_AppendItem(menu, "Do Not Color Mesh", SetMeshColorStyle, true, selectedColorStyle == MeshColorStyle.noColor);
            menu.Items[menu.Items.Count - 1].Tag = MeshColorStyle.noColor;
        }

        private void SetMeshColorStyle(object sender, EventArgs e)
        {
            ToolStripMenuItem selectedItem = (ToolStripMenuItem)sender;
            ToolStrip parentMenu = selectedItem.Owner as ToolStrip;
            if ((MeshColorStyle)selectedItem.Tag != selectedColorStyle) // Update style if it was changed
            {
                selectedColorStyle = (MeshColorStyle)selectedItem.Tag;
                ExpireSolution(true);
                quadMesh.VertexColors.Clear(); // Must flush mesh colors to properly updated display
            }
            for (int i = 0; i < parentMenu.Items.Count; i++) // Easier than foreach as types differ
            {
                if (parentMenu.Items[i] is ToolStripMenuItem && parentMenu.Items[i].Tag != null)
                {
                    ToolStripMenuItem menuItem = parentMenu.Items[i] as ToolStripMenuItem;
                    menuItem.Checked = true; // Toggle state of menu items
                }
            }
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
            DA.GetData<double>(0, ref sensorElevation);
            DA.GetData<int>(1, ref waterLevel);
            DA.GetData<int>(2, ref leftColumns);
            DA.GetData<int>(3, ref rightColumns);
            DA.GetData<int>(4, ref topRows);
            DA.GetData<int>(5, ref bottomRows);
            DA.GetData<int>(6, ref tickRate);
            DA.GetData<int>(7, ref averageFrames);
            DA.GetData<int>(8, ref blurRadius);
            DA.GetData<int>(9, ref colorRampSpan);
            DA.GetData<Color>(10, ref startColor);
            DA.GetData<Color>(11, ref endColor);

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
            sensorElevation /= unitsMultiplier; // Standardise to mm to match sensor units

            Stopwatch timer = Stopwatch.StartNew(); //debugging

            if (selectedColorStyle == MeshColorStyle.byElevation)
            {
                Core.ComputeLookupTable(waterLevel, startColor, endColor, colorRampSpan, lookupTable); //precompute all vertex colors
            }

            if (this.kinectSensor == null)
            {
                KinectController.AddRef();
                this.kinectSensor = KinectController.sensor;
            }


            if (this.kinectSensor != null)
            {
                if (KinectController.depthFrameData != null)
                {
                    pointCloud = new Point3f[(KinectController.depthHeight - topRows - bottomRows) * (KinectController.depthWidth - leftColumns - rightColumns)];
                    Point3f tempPoint = new Point3f();
                    outputMesh = new List<Mesh>();
                    output = new List<String>(); //debugging
                    Core.PixelSize depthPixelSize = Core.GetDepthPixelSpacing(sensorElevation);
                    vertexColors = new Color[(KinectController.depthHeight - topRows - bottomRows) * (KinectController.depthWidth - leftColumns - rightColumns)];


                    if (blurRadius > 1)
                    {
                        var gaussianBlur = new GaussianBlur(KinectController.depthFrameData);
                        var blurredFrame = gaussianBlur.Process(blurRadius, KinectController.depthWidth, KinectController.depthHeight);

                        renderBuffer.Enqueue(blurredFrame);
                    }
                    else
                    {
                        renderBuffer.Enqueue(KinectController.depthFrameData);
                    }

                    int arrayIndex = 0;
                    for (int rows = topRows; rows < KinectController.depthHeight - bottomRows; rows++)

                    {
                        for (int columns = rightColumns; columns < KinectController.depthWidth - leftColumns; columns++)
                        {

                            int i = rows * KinectController.depthWidth + columns;
                            //int arrayIndex = i - ((topRows * KinectController.depthWidth) + rightColumns) - ((rows - topRows) * (leftColumns + rightColumns)); //get index in the trimmed array

                            tempPoint.X = (float)(columns * -unitsMultiplier * depthPixelSize.x); 
                            tempPoint.Y = (float)(rows * -unitsMultiplier * depthPixelSize.y);

                            if (averageFrames > 1)
                            {
                                int depthPointRunningSum = 0;
                                foreach (var frame in renderBuffer)
                                {
                                    depthPointRunningSum += frame[i];
                                }
                                depthPoint = depthPointRunningSum / renderBuffer.Count;
                            }
                            else
                            {
                                depthPoint = KinectController.depthFrameData[i];
                            }

                            if (depthPoint == 0 || depthPoint >= lookupTable.Length) //check for invalid pixels
                            {
                                depthPoint = (int)sensorElevation;
                            }


                            tempPoint.Z = (float)((depthPoint - sensorElevation) * -unitsMultiplier);
                            if (selectedColorStyle == MeshColorStyle.byElevation)
                            {
                                vertexColors[arrayIndex] = (lookupTable[depthPoint]);
                            }

                            pointCloud[arrayIndex] = tempPoint;

                            arrayIndex++;
                        }
                    };

                    //keep only the desired amount of frames in the buffer
                    while (renderBuffer.Count >= averageFrames && averageFrames > 0)
                    {
                        renderBuffer.Dequeue();
                    }

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
