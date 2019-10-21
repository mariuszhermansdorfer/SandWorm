using System;
using Grasshopper.Kernel;

namespace SandWorm.Components
{
    // Provides common functions across the components that read from the Kinect stream
    public abstract class BaseKinectComponent : BaseComponent
    {
        public int tickRate = 33; // In ms

        public BaseKinectComponent(string name, string nickname, string description) 
            : base(name, nickname, description, "Kinect Visualisation")
        { }

        protected void ShowComponentError(string errorMessage)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, errorMessage);
            ScheduleSolve(); // Ensure a future solve is scheduled despite an early return to SolveInstance()
        }

        protected void ScheduleDelegate(GH_Document doc)
        {
            ExpireSolution(false);
        }

        protected void ScheduleSolve()
        {
            if (tickRate > 0) // Allow users to force manual recalculation
                base.OnPingDocument().ScheduleSolution(tickRate, new GH_Document.GH_ScheduleDelegate(ScheduleDelegate));
        }

    }
}
