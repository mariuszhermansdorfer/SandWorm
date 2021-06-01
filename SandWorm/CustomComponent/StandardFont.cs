using System.Drawing;
using Grasshopper.Kernel;

namespace SandWorm
{
	public class StandardFont
	{
		public static Font Font()
		{
			return GH_FontServer.StandardAdjusted;
		}

		public static Font LargeFont()
		{
			return GH_FontServer.LargeAdjusted;
		}
	}
}
