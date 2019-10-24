using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace SandWorm
{
    public class SetupComponent : GH_Component
    {
        public int bottomRows;
        public int keepFrames = 1; // In ms
        public int leftColumns;

        public double[] options = new double[7];
        public int rightColumns;

        public double sensorElevation = 1000; // Arbitrary default value (must be >0)
        public int tickRate = 33; // In ms
        public int topRows;

        /// <summary>
        ///     Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public SetupComponent()
            : base("Setup Component", "SWSetup",
                "This component takes care of all the setup & calibration of your sandbox.",
                "Sandworm", "Configuration")
        {
        }

        /// <summary>
        ///     Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon => null;

        /// <summary>
        ///     Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("9ee53381-c269-4fff-9d45-8a2dbefc243c");

        /// <summary>
        ///     Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("SensorHeight", "SH",
                "The height (in document units) of the sensor above your model", GH_ParamAccess.item, sensorElevation);
            pManager.AddIntegerParameter("LeftColumns", "LC", "Number of columns to trim from the left",
                GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("RightColumns", "RC", "Number of columns to trim from the right",
                GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("TopRows", "TR", "Number of rows to trim from the top", GH_ParamAccess.item,
                0);
            pManager.AddIntegerParameter("BottomRows", "BR", "Number of rows to trim from the bottom",
                GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("TickRate", "TR",
                "The time interval, in milliseconds, to update geometry from the Kinect. Set as 0 to disable automatic updates.",
                GH_ParamAccess.item, tickRate);
            pManager.AddIntegerParameter("KeepFrames", "KF",
                "Output a running list of a frame updates rather than just the current frame. Set to 1 or 0 to disable.",
                GH_ParamAccess.item, keepFrames);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        /// <summary>
        ///     Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Options", "O", "Sandworm oOptions", GH_ParamAccess.list); //debugging
        }

        /// <summary>
        ///     This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(0, ref sensorElevation);
            DA.GetData(1, ref leftColumns);
            DA.GetData(2, ref rightColumns);
            DA.GetData(3, ref topRows);
            DA.GetData(4, ref bottomRows);
            DA.GetData(5, ref tickRate);
            DA.GetData(6, ref keepFrames);

            options[0] = sensorElevation;
            options[1] = leftColumns;
            options[2] = rightColumns;
            options[3] = topRows;
            options[4] = bottomRows;
            options[5] = tickRate;
            options[6] = keepFrames;

            DA.SetDataList(0, options);
        }
    }
}