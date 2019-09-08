using System;
using System.Drawing;
using Rhino.Display;
using Rhino.Geometry;
using Grasshopper.Kernel;
using System.Windows.Forms;

namespace SandWorm
{
    public static class Analysis
    {        
        public abstract class MeshVisualisation // For all analysis options that can be enabled
        {
            public string name { get; } // Name used in the toggle menu
            public bool isExclusive { get; } // Whether this can be applied indepedently of other analysis
            public bool isEnabled { get; set; } // Whether to apply the analysis
            public Color[] lookupTable;

            public MeshVisualisation(string menuName, bool exclusive)
            {
                name = menuName; 
                isExclusive = exclusive;
                isEnabled = false;
            }

            public void ComputeLookupTable(double sensorElevation)
            {
                ComputeLookupTableForAnalysis(sensorElevation); 
            }

            public void ComputeLinearRange(int startIndex, int endIndex)
            {
                lookupTable = new Color[endIndex];
                for (int i = startIndex; i > endIndex; i--)
                {
                    lookupTable[i] = new ColorHSL(0.01 + (i * 0.01), 1.0, 0.5).ToArgbColor();
                }
            }

            public void ComputeBlockRange(int startIndex, int endIndex, Color color)
            {
                lookupTable = new Color[endIndex];
                for (int i = startIndex; i > endIndex; i--)
                {
                    lookupTable[i] = color;
                }
            }

            public abstract Color GetPixelColor(int elevation); // TODO: accept other inputs
            public abstract void ComputeLookupTableForAnalysis(double sensorElevation);
        }

        public class None : MeshVisualisation
        {
            public None() : base("No Analysis", false) { }

            public override Color GetPixelColor(int elevation) {
                return Color.Transparent; // Never called as its lookup table has no items
            }

            public override void ComputeLookupTableForAnalysis(double sensorElevation) { }
        }

        public class Water : MeshVisualisation
        {
            public Water() : base("Water Level", false) { }

            public override Color GetPixelColor(int elevation)
            {
                return lookupTable[elevation];
            }

            public override void ComputeLookupTableForAnalysis(double waterLevel)
            {
                ComputeBlockRange(0, (int)waterLevel, Color.Blue); 
            }
        }

        public class Contours : MeshVisualisation
        {
            public Contours() : base("Contour Lines", false) { }

            public override Color GetPixelColor(int elevation)
            {
                return Color.White; // TODO
            }

            public override void ComputeLookupTableForAnalysis(double sensorElevation)
            {
                // TODO - how to define a range of inputs? set the others to null?
            }
        }


        public class Elevation : MeshVisualisation
        {
            public Elevation() : base("Elevation Analysis", false) { }

            public override Color GetPixelColor(int elevation)
            {
                return lookupTable[elevation];
            }

            public override void ComputeLookupTableForAnalysis(double sensorElevation)
            {
                ComputeLinearRange(0, (int)sensorElevation); 
            }
        }

        class Slope : MeshVisualisation
        {
            public Slope() : base("Slope Analysis", false) { }

            public override Color GetPixelColor(int elevation)
            {
                return Color.White; // TODO
            }

            public override void ComputeLookupTableForAnalysis(double sensorElevation)
            {
                ComputeLinearRange(0, 90);
            }
        }

        class Aspect : MeshVisualisation
        {
            public Aspect() : base("Aspect Analysis", false) { }

            public override Color GetPixelColor(int elevation)
            {
                return Color.White; // TODO
            }

            public override void ComputeLookupTableForAnalysis(double sensorElevation)
            {
                ComputeLinearRange(0, 359);
            }
        }        
    }
}
