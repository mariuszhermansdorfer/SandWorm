using System;
using Grasshopper.Kernel;

namespace SandWorm.Components
{
    // Provides common functions across the various Sandworm components
    public abstract class BaseComponent : GH_Component
    {
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
    }
}
