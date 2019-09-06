using System;
using System.Drawing;
using Rhino.Display;
using Grasshopper.Kernel;
using System.Windows.Forms;

namespace SandWorm
{
    public static class Analysis
    {        
        abstract class MeshVisualisation // For all analysis options that can be enabled
        {
            public string menuName { get; } // Name used in the toggle menu
            public bool menuExclusive { get; } // Whether this can be applied indepedently of other analysis

            public MeshVisualisation(string name, bool exclusive)
            {
                menuName = name; 
                menuExclusive = exclusive;
            }

            public ToolStripDropDown AddToMenu(ToolStripDropDown menu)
            {
                if (menuExclusive)
                {
                    // Create separator item
                }
                // Append menu item
                return menu;
            }
        }

        abstract class MeshAnalysis : MeshVisualisation // For analysis that affects the entire mesh and is mutually exclusive
        {
            public bool showContours;
            public int contourInterval;
            public bool showWaterLevel;
            private Color[] lookupTable;

            public MeshAnalysis(string name, bool exclusive, bool contours, bool water, int sensorHeight, int interval)
                : base(name, exclusive) 
            {
                showContours = contours; // Not exclusive with the mesh gradient
                showWaterLevel = water; // Not exclusive with the mesh gradient
                lookupTable = new Color[GetLookupTableSize(sensorHeight)];
                contourInterval = interval;
            }

            public abstract Color GetPixelColor();
            public abstract int GetLookupTableSize(int sensorHeight);
            public abstract void ComputeLookupTableForAnalysis(ref Color[] lookupTable, int startIndex);

            public void ComputeLookupTable(int waterLevel, int contourInterval, int sensorHeight)
            {
                int j = 0;
                if (showWaterLevel) // Color all pixels below water level; pass on remaining items
                {
                    for (int i = waterLevel; i < lookupTable.Length; i++) 
                    {
                        lookupTable[i] = new ColorHSL(0.6, 0.6, 0.60 - (j * 0.02)).ToArgbColor();
                        j++;
                    }
                }

                ComputeLookupTableForAnalysis(ref lookupTable, j);

                if (showContours) // Override earlier pixels as needed to draw contours
                {
                    for (int i = 0; i < lookupTable.Length; i++)
                    {
                        // Check if within specified contour interval
                    }

                }
            }
        }

        class NoAnalysis : MeshAnalysis
        {
            public NoAnalysis(bool contours, bool water, int sensorHeight, int contourInterval) 
                : base("No Analysis", false, contours, water, sensorHeight, contourInterval) { }
            public override Color GetPixelColor()
            {
                return Color.White;
            }
            public override int GetLookupTableSize(int sensorHeight)
            {
                return 0;
            }
            public override void ComputeLookupTableForAnalysis(ref Color[] lookupTable, int startIndex)
            {
                // Do nothing? Or just return a transparent color?
            }
        }

        class ElevationAnalysis : MeshAnalysis
        {
            public ElevationAnalysis(bool contours, bool water, int sensorHeight, int contourInterval) 
                : base("Elevation Analysis", false, contours, water, sensorHeight, contourInterval) { }
            public override Color GetPixelColor()
            {
                return Color.White;
            }
            public override int GetLookupTableSize(int sensorHeight)
            {
                return sensorHeight;
            }

            public override void ComputeLookupTableForAnalysis(ref Color[] lookupTable, int startIndex)
            {
                j = 0;
                for (int i = startIndex; i > 0; i--) 
                {
                    lookupTable[i] = new ColorHSL(0.01 + (j * 0.01), 1.0, 0.5).ToArgbColor();
                    j++;
                }
            }
        }

        class SlopeAnalysis : MeshAnalysis
        {
            public SlopeAnalysis(bool contours, bool water, int sensorHeight, int contourInterval) 
                : base("Slope Analysis", false, contours, water, sensorHeight, contourInterval) { }
            public override Color GetPixelColor()
            {
                return Color.White;
            }
            public override int GetLookupTableSize(int sensorHeight)
            {
                return 90; // Slope ranges
            }
            public override void ComputeLookupTableForAnalysis(ref Color[] lookupTable, int startIndex)
            {
                // Provide a lookup table as per a reasonable range of slope values
            }
        }
        class AspectAnalysis : MeshAnalysis
        {
            public AspectAnalysis(bool contours, bool water, int sensorHeight, int contourInterval)
                : base("Aspect Analysis", false, contours, water, sensorHeight, contourInterval) { }
            public override Color GetPixelColor()
            {
                return Color.White;
            }
            public override int GetLookupTableSize(int sensorHeight)
            {
                return 359; // Aspect ranges
            }
            public override void ComputeLookupTableForAnalysis(ref Color[] lookupTable, int startIndex)
            {
                // Provide a lookup table as per a reasonable range of aspect values
            }
        }        
    }
}
