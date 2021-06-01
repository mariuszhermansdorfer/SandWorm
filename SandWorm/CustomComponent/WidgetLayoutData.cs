using Grasshopper.GUI.Canvas;

namespace SandWorm
{
	public abstract class WidgetLayoutData
	{
		public GH_PaletteStyle Style
		{
			get;
			private set;
		}

		protected GH_Palette Palette
		{
			get;
			private set;
		}
	}
}