using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Rhino.Display;

namespace SandWorm
{
    public static class ColorLookupTables
    {
        

        public static Color[] ComputeLookupTable(int waterLevel, Color[] lookupTable)
        {
            //precompute all vertex colors
            int j = 0;
            for (int i = waterLevel; i < lookupTable.Length; i++) //below water level
            {
                lookupTable[i] = new ColorHSL(0.6, 0.6, 0.60 - (j * 0.02)).ToArgbColor();
                j++;
            }

            j = 0;
            for (int i = waterLevel; i > 0; i--) //above water level
            {
                lookupTable[i] = new ColorHSL(0.01 + (j * 0.01), 1.0, 0.5).ToArgbColor();
                j++;
            }
            return lookupTable;
        }

        //Color.FromArgb(128, 128, 128);
    }
}
