using Grasshopper.Kernel;

namespace SandWorm
{
	public class ExtendedPlug
	{
		private bool isMenu;
        private EvaluationUnit unit;

        public IGH_Param Parameter { get; }
        public bool IsMenu
		{
			get
			{
				return isMenu;
			}
			set
			{
				isMenu = value;
			}
		}

		public EvaluationUnit Unit
		{
			get
			{
				return unit;
			}
			set
			{
				unit = value;
			}
		}

		public ExtendedPlug(IGH_Param parameter)
		{
			this.Parameter = parameter;
		}
	}
}