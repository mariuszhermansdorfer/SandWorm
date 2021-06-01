using Grasshopper.Kernel;

namespace SandWorm
{
    public abstract class SubComponent
    {
        public abstract string Name();
        public abstract string Display_name();
        public abstract void RegisterEvaluationUnits(EvaluationUnitManager mngr);
        public abstract void SolveInstance(IGH_DataAccess DA, out string msg, out GH_RuntimeMessageLevel level);
        public virtual void OnComponentLoaded()
        {
        }
    }
}