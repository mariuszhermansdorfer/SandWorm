using System;
using System.Collections.Generic;
using System.Diagnostics; //debugging
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Microsoft.Kinect;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace SandWorm
{
    public class SandWorm : GH_Component
    {
        private KinectSensor kinectSensor = null;
        private List<Point3d> pointCloud = null;
        private Mesh triangulatedMesh = null;
        private List<Mesh> outputMesh = null;
        private List<Curve> contours = null;

        private List<String> output = null;//debugging
        private List<System.Drawing.Color> pointCloudColor = null;
        public int resolution = 1;
        public double depth = 8.00;
        public Point3d origin;
        public Rectangle3d trimRectangle;
        public double smooth = 0.1;
        public int iterations = 1;
        public Point3d[] renderBuffer = Enumerable.Range(1, 217088).Select(i => new Point3d()).ToArray(); //initialize the array with 217088 points, each for every pixel of Kinect's depth camera
        
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
            pManager.AddPointParameter("Origin of Pointcloud", "O", "Origin of pointcoud", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Resolution of Pointcloud", "R", "Resolution of pointcoud", GH_ParamAccess.item);
            pManager.AddNumberParameter("Depth of Pointcloud", "D", "Depth of pointcoud", GH_ParamAccess.item);
            pManager.AddRectangleParameter("Trim Rectangle", "Tr", "Trim Rectangle", GH_ParamAccess.item);
            pManager.AddNumberParameter("Smoothing Factor", "Sf", "Smoothing Factor", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Smoothing Iterations", "Si", "Smoothing Iterations", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Pointcloud", "PC", "Pointcloud", GH_ParamAccess.list);
            pManager.AddColourParameter("Color of Pointcloud", "C", "Color of Pointcloud", GH_ParamAccess.list);
            pManager.AddMeshParameter("Mesh", "M", "Resulting Mesh", GH_ParamAccess.list);
            pManager.AddCurveParameter("Contours", "Co", "Contours", GH_ParamAccess.list);
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
            DA.GetData<Point3d>(0, ref origin);
            DA.GetData<int>(1, ref resolution);
            DA.GetData<double>(2, ref depth);
            DA.GetData<Rectangle3d>(3, ref trimRectangle);
            DA.GetData<double>(4, ref smooth);
            DA.GetData<int>(5, ref iterations);


            int res;
            double dep;
            Point3d org = origin;
            Point3d min = (trimRectangle.Corner(0) + org);
            Point3d max = (trimRectangle.Corner(2) + org);
            //Kinect's Y axis is flipped. Reversing values here for ease of further comparison
            double minY = max.Y * -1;
            double maxY = min.Y * -1;

            Stopwatch timer = Stopwatch.StartNew(); //debugging

            if (resolution < 1)
            {
                res = 1;
            }
            else
            {
                res = resolution;
            }

            if (depth < 0)
            {
                dep = 8.00;
            }
            else
            {
                dep = depth;
            }

            if (this.kinectSensor == null)
            {
                KinectController.AddRef();
                this.kinectSensor = KinectController.sensor;
                KinectController.kinectGHC = this;
            }


            if (this.kinectSensor != null)
            {
                if (KinectController.cameraSpacePoints != null && KinectController.colorSpacePoints != null)
                {
                    pointCloud = new List<Point3d>();
                    pointCloudColor = new List<System.Drawing.Color>();
                    Point3d renderPoint = new Point3d();
                    outputMesh = new List<Mesh>();
                    triangulatedMesh = new Mesh();

                    output = new List<String>(); //debugging


                    int pixelCount = KinectController.cameraSpacePoints.Length;
                    for (int i = 0; i < pixelCount; i += res)
                    {
                        CameraSpacePoint p = KinectController.cameraSpacePoints[i];

                        if (p.Z > dep || p.Z < 1.0) //remove flying pixels
                        {
                            continue;
                        }

                        else //if (p.Z <= dep)
                        {
                            if (min.X > p.X || p.X > max.X || minY > p.Y || p.Y > maxY) //Taking only points enclosed by the trimming rectangle 
                            {
                                continue;
                            }

                            else
                            {
                                ColorSpacePoint colPt = KinectController.colorSpacePoints[i];

                                int colorX = (int)Math.Floor(colPt.X + 0.5);
                                int colorY = (int)Math.Floor(colPt.Y + 0.5);


                                if ((colorX >= 0) && (colorX < KinectController.colorWidth) && (colorY >= 0) && (colorY < KinectController.colorHeight))
                                {
                                    int colorIndex = ((colorY * KinectController.colorWidth) + colorX) * KinectController.bytesPerPixel;
                                    Byte b = 0; Byte g = 0; Byte r = 0;

                                    b = KinectController.colorFrameData[colorIndex++];
                                    g = KinectController.colorFrameData[colorIndex++];
                                    r = KinectController.colorFrameData[colorIndex++];

                                    System.Drawing.Color color = System.Drawing.Color.FromArgb(r, g, b);


                                    //Only add new points if delta between ticks is greater than 10mm to remove jitter
                                    if (0.010 < Math.Abs(renderBuffer[i].Z - p.Z))
                                    {
                                        renderBuffer[i].X = p.X;
                                        renderBuffer[i].Y = p.Y;
                                        renderBuffer[i].Z = p.Z;
                                    }

                                    //transformation matrix to draw on screen
                                    renderPoint.X = renderBuffer[i].X + org.X;
                                    renderPoint.Y = renderBuffer[i].Y * -1 - org.Y;
                                    renderPoint.Z = renderBuffer[i].Z * -1 - org.Z;

                                    pointCloud.Add(renderPoint);
                                    pointCloudColor.Add(color);

                                }
                            }
                        }
                    }
                    //debugging
                    timer.Stop();
                    output.Add("Point Cloud generation: " + timer.ElapsedMilliseconds.ToString() + " ms");

                }

                timer.Restart(); //debugging
                if (pointCloud != null)
                {
                    triangulatedMesh = Mesh.CreateFromTessellation(pointCloud, null, Plane.WorldXY, false);

                    timer.Stop(); //debugging
                    output.Add("Meshing: " + timer.ElapsedMilliseconds.ToString() + " ms");

                    timer.Restart(); //debugging
                    for (int i = 0; i < iterations; i++)
                    {
                        triangulatedMesh.Smooth(smooth, false, false, true, false, SmoothingCoordinateSystem.World);
                    }

                    timer.Stop(); //debugging
                    output.Add("Mesh Smoothing: " + iterations + " iterations. " + timer.ElapsedMilliseconds.ToString() + " ms"); //debugging

                    timer.Restart(); //debugging

                    Point3d lowerBound = new Point3d(0.0, 0.0, -1.4);
                    Point3d upperBound = new Point3d(0.0, 0.0, -1.0);
                    contours = new List<Curve>(Mesh.CreateContourCurves(triangulatedMesh, lowerBound, upperBound, 0.005));

                    timer.Stop(); //debugging
                    output.Add("Contours: " + timer.ElapsedMilliseconds.ToString() + " ms"); //debugging

                    outputMesh.Add(triangulatedMesh);
                }


                DA.SetDataList(0, pointCloud);
                DA.SetDataList(1, pointCloudColor);
                DA.SetDataList(2, outputMesh);
                DA.SetDataList(3, contours);
                DA.SetDataList(4, output); //debugging
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
