using System.Collections.Generic;
using System.Diagnostics;
using Grasshopper.Kernel;

namespace SandWorm.Components
{
    // Provides common functions across the various Sandworm components
    public abstract class BaseComponent : GH_Component
    {
        public static List<string> output; // Debugging
        protected Stopwatch timer;

        // Pass the constructor parameters up to the main GH_Component abstract class
        protected BaseComponent(string name, string nickname, string description, string subCategory)
            : base(name, nickname, description, "Sandworm", subCategory)
        {
        }

        // Components must implement a solve instance using SandwormSolveInstance()
        protected abstract void SandwormSolveInstance(IGH_DataAccess DA);

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SandwormSolveInstance(DA);
        }

        protected void SetupLogging()
        {
            timer = Stopwatch.StartNew(); // Setup timer used for debugging
            output = new List<string>(); // For the debugging log lines
        }
    }
}