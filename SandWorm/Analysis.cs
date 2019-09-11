using System;
using System.Drawing;
using System.Collections.Generic;
using Rhino.Display;
using Rhino.Geometry;
using Grasshopper.Kernel;
using System.Windows.Forms;

namespace SandWorm
{
    public static class Analysis
    {
        public static class AnalysisManager
        {
            /// <summary>Stories copies of each analysis option and intefaces their use with components.</summary>
            public static List<MeshVisualisation> options;

            static AnalysisManager()
            {
                // Note their order in array determines their priority; i.e. which color 'wins'
                // Also needs to manually arrange the exclusive options together
                options = new List<Analysis.MeshVisualisation> {
                    new Analysis.Water(), new Analysis.Contours(),
                    new Analysis.None(),
                    new Analysis.Elevation(), new Analysis.Slope(), new Analysis.Aspect(),
                };
                // Default to showing elevation analysis
                options[3].IsEnabled = true;
            }

            public static void SetEnabledOptions(ToolStripMenuItem selectedMenuItem)
            {
                MeshVisualisation selectedOption = options.Find(x => x.MenuItem == selectedMenuItem);
                if (selectedOption.IsExclusive)
                    foreach (MeshVisualisation option in AnalysisManager.options)
                        option.IsEnabled = selectedOption == option; // Tick a toggle on or off
                else
                    selectedOption.IsEnabled = !selectedOption.IsEnabled; // Simple toggle for independent items
            }

            private static List<MeshVisualisation> GetEnabledOptions()
            {
                return options.FindAll(x => x.IsEnabled);
            }

            public static void ComputeLookupTables(double sensorElevation, double waterLevel)
            {
                foreach (Analysis.MeshVisualisation option in GetEnabledOptions())
                    option.ComputeLookupTableForAnalysis((int)sensorElevation, (int)waterLevel);
            }

            public static Color? GetPixelColor(int depthPoint) // Get color for pixel given enabled options
            {
                foreach (Analysis.MeshVisualisation option in GetEnabledOptions())
                {
                    Color? analysisColor = option.GetPixelColorForAnalysis(depthPoint);
                    if (analysisColor.HasValue)
                        return analysisColor.Value;
                }
                return null;
            }
        }

        public class VisualisationRangeWithColor 
        {
            /// <summary>Describes a numeric range (e.g. elevation/slope values) and color range to visualise it.</summary>
            public int ValueStart { get; set; }
            public int ValueEnd { get; set; }
            public ColorHSL ColorStart { get; set; }
            public ColorHSL ColorEnd { get; set; }

            public ColorHSL InterpolateColor(double progress) // Progress is assumed to be a % value of 0.0 - 1.0
            {
                return new ColorHSL( 
                    ColorStart.H + ((ColorEnd.H - ColorStart.H) * progress),
                    ColorStart.S + ((ColorEnd.S - ColorStart.S) * progress),
                    ColorStart.L + ((ColorEnd.L - ColorStart.L) * progress)
                );
            }
        }

        public abstract class MeshVisualisation 
        {
            /// <summary>Inherited by all possible analysis options (even if not coloring the mesh).</summary>
            public string Name { get; } // Name used in the toggle menu
            public bool IsExclusive { get; } // Whether this can be applied indepedently of other analysis
            public bool IsEnabled { get; set; } // Whether to apply the analysis
            public ToolStripMenuItem MenuItem { get; set; } 
            public Dictionary<int, Color> lookupTable; // Dictionary of integers that map to color values

            public MeshVisualisation(string menuName, bool exclusive)
            {
                Name = menuName; 
                IsExclusive = exclusive;
                IsEnabled = false;
            }

            public void ComputeLinearRanges(params VisualisationRangeWithColor[] lookUpRanges)
            {
                int lookupTableMaximumSize = 0;
                foreach (VisualisationRangeWithColor range in lookUpRanges)
                {
                    lookupTableMaximumSize += range.ValueEnd - range.ValueStart;
                }

                if (lookupTableMaximumSize == 0)
                    return; // Can occur e.g. if the waterLevel is greater than the sensor height
                else
                    lookupTable = new Dictionary<int, Color>(lookupTableMaximumSize); // Init dict with needed size

                // Populate dict values by interpolating colors within each of the lookup ranges
                foreach (VisualisationRangeWithColor range in lookUpRanges) 
                {
                    for (int i = range.ValueStart; i < range.ValueEnd; i++)
                    {
                        double progress = ((double)i - range.ValueStart) / (range.ValueEnd - range.ValueStart);
                        lookupTable[i] = range.InterpolateColor(progress);
                    }
                }
            }
            
            public abstract Color? GetPixelColorForAnalysis(int elevation);
            public abstract void ComputeLookupTableForAnalysis(int sensorElevation, int waterLevel);
        }

        public class None : MeshVisualisation
        {
            public None() : base("No Visualisation", true) { }

            public override Color? GetPixelColorForAnalysis(int elevation) {
                return null; 
            }

            public override void ComputeLookupTableForAnalysis(int sensorElevation, int waterLevel) { }
        }

        public class Water : MeshVisualisation
        {
            public Water() : base("Show Water Level", false) { }

            public override Color? GetPixelColorForAnalysis(int elevation)
            {
                if (lookupTable.ContainsKey(elevation))
                    return lookupTable[elevation]; // If the elevation is within the water level
                else
                    return null;
            }

            public override void ComputeLookupTableForAnalysis(int sensorElevation, int waterLevel)
            {
                VisualisationRangeWithColor waterRange = new VisualisationRangeWithColor
                {
                    // From the sensor's perspective water is between specified level and max height (i.e. upside down)
                    ValueStart = sensorElevation - waterLevel, 
                    ValueEnd = sensorElevation,
                    ColorStart = new ColorHSL(0.55, 0.85, 0.25), 
                    ColorEnd = new ColorHSL(0.61, 0.65, 0.65)
                };
                ComputeLinearRanges(new VisualisationRangeWithColor[] { waterRange }); 
            }
        }

        public class Elevation : MeshVisualisation
        {
            public Elevation() : base("Visualise Elevation", true) { }

            public override Color? GetPixelColorForAnalysis(int elevation)
            {
                if (lookupTable.ContainsKey(elevation))
                    return lookupTable[elevation];
                else
                    return null;
            }

            public override void ComputeLookupTableForAnalysis(int sensorElevation, int waterLevel)
            {
                VisualisationRangeWithColor elevationRange = new VisualisationRangeWithColor
                {
                    ValueStart = sensorElevation - 750, // TODO: don't assume maximum value here
                    ValueEnd = sensorElevation,
                    ColorStart = new ColorHSL(0.00, 0.25, 0.05),
                    ColorEnd = new ColorHSL(0.50, 0.85, 0.75)
                };
                ComputeLinearRanges(new VisualisationRangeWithColor[] { elevationRange });

            }
        }
        
        class Slope : MeshVisualisation
        {
            public Slope() : base("Visualise Slope", true) { }

            public override Color? GetPixelColorForAnalysis(int slopeValue)
            {
                return null; // TODO
            }

            public override void ComputeLookupTableForAnalysis(int sensorElevation, int waterLevel)
            {
                VisualisationRangeWithColor slopeRange = new VisualisationRangeWithColor
                {
                    ValueStart = 0, 
                    ValueEnd = 90,
                    ColorStart = new ColorHSL(1.0, 1.0, 1.0), // White
                    ColorEnd = new ColorHSL(1.0, 1.0, 0.3) // Dark Red
                };
                ComputeLinearRanges(new VisualisationRangeWithColor[] { slopeRange });
            }
        }

        class Aspect : MeshVisualisation
        {
            public Aspect() : base("Visualise Aspect", true) { }

            public override Color? GetPixelColorForAnalysis(int aspectValue)
            {
                return null; // TODO
            }

            public override void ComputeLookupTableForAnalysis(int sensorElevation, int waterLevel)
            {
                VisualisationRangeWithColor rightAspect = new VisualisationRangeWithColor
                {
                    ValueStart = 0, 
                    ValueEnd = 180,
                    ColorStart = new ColorHSL(1.0, 1.0, 1.0), // White
                    ColorEnd = new ColorHSL(1.0, 1.0, 0.3) // Dark Red
                };
                VisualisationRangeWithColor leftAspect = new VisualisationRangeWithColor
                {
                    ValueStart = 180, // For the other side of the aspect we loop back to the 0 value
                    ValueEnd = 359,
                    ColorStart = new ColorHSL(1.0, 1.0, 0.3), // Dark Red
                    ColorEnd = new ColorHSL(1.0, 1.0, 1.0) // White
                };
                ComputeLinearRanges(new VisualisationRangeWithColor[] { rightAspect, leftAspect });
            }
        }


        public class Contours : MeshVisualisation
        {
            public Contours() : base("Show Contour Lines", false) { }

            public override Color? GetPixelColorForAnalysis(int elevation)
            {
                return null; 
            }

            public override void ComputeLookupTableForAnalysis(int sensorElevation, int waterLevel) { }
        }

    }
}
