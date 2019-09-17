using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using Rhino.Display;
using Rhino.Geometry;

namespace SandWorm
{
    public static class Analysis
    {
        public static class AnalysisManager
        {
            /// <summary>Stories copies of each analysis option and interfaces their use with components.</summary>
            public static List<MeshAnalysis> options;

            static AnalysisManager() // Note that the order of items here determines their menu order
            {
                options = new List<MeshAnalysis>
                {
                    new Water(), new Contours(),
                    new None(),
                    new Elevation(), new Slope(), new Aspect()
                };
                // Default to showing elevation analysis
                options[3].IsEnabled = true;
            }

            public static List<MeshAnalysis> GetEnabledAnalyses() => options.FindAll(x => x.IsEnabled);

            public static MeshColorAnalysis GetEnabledMeshColoring()
            {
                foreach (var enabledOption in GetEnabledAnalyses())
                {
                    var optionType = enabledOption.GetType();
                    var optionTest = optionType.IsSubclassOf(typeof(MeshColorAnalysis));
                    if (enabledOption.GetType().IsSubclassOf(typeof(MeshColorAnalysis)))
                        return enabledOption as MeshColorAnalysis;
                }
                return null; // Shouldn't happen; a mesh color (even no color) is always set
            }

            public static void SetEnabledOptions(ToolStripMenuItem selectedMenuItem)
            {
                var selectedOption = options.Find(x => x.MenuItem == selectedMenuItem);
                if (selectedOption.IsExclusive)
                    foreach (var exclusiveOption in options.FindAll(x => x.IsExclusive))
                        exclusiveOption.IsEnabled =
                            selectedOption == exclusiveOption; // Toggle selected item; untoggle other exclusive items
                else
                    selectedOption.IsEnabled = !selectedOption.IsEnabled; // Simple toggle for independent items
            }

            public static void ComputeLookupTables(double sensorElevation)
            {
                GetEnabledMeshColoring().ComputeLookupTableForAnalysis(sensorElevation);
            }
        }

        public class VisualisationRangeWithColor
        {
            /// <summary>Describes a numeric range (e.g. elevation or slope values) and color range to visualise it.</summary>
            public int ValueStart { get; set; }
            public int ValueEnd { get; set; }
            public ColorHSL ColorStart { get; set; }
            public ColorHSL ColorEnd { get; set; }

            public ColorHSL InterpolateColor(double progress) // Progress is assumed to be a % value of 0.0 - 1.0
            {
                return new ColorHSL(
                    ColorStart.H + (ColorEnd.H - ColorStart.H) * progress,
                    ColorStart.S + (ColorEnd.S - ColorStart.S) * progress,
                    ColorStart.L + (ColorEnd.L - ColorStart.L) * progress
                );
            }
        }

        public abstract class MeshAnalysis
        {
            /// <summary>Some form of analysis that applies, or derives from, the mesh.</summary>

            public bool IsEnabled; // Whether to apply the analysis

            public MeshAnalysis(string menuName, bool exclusive)
            {
                Name = menuName;
                IsExclusive = exclusive; // Any analysis that applies to the mesh as a whole is mutually exclusive
            }

            /// <summary>Inherited by all possible analysis options (even if not coloring the mesh).</summary>
            public string Name { get; } // Name used in the toggle menu

            public bool IsExclusive { get; set; } // Whether the analysis can be applied independent of other options
            public ToolStripMenuItem MenuItem { get; set; }
        }

        public abstract class MeshGeometryAnalysis : MeshAnalysis
        {
            /// <summary>A form of analysis that outputs geometry (i.e. contours) based on the mesh</summary>
            public MeshGeometryAnalysis(string menuName) : base(menuName, false) { } // Note: not mutually exclusive
        }

        public abstract class MeshColorAnalysis : MeshAnalysis
        {
            /// <summary>A form of analysis that colors the vertices of the entire mesh</summary>
            public Color[] lookupTable; // Dictionary of integers that map to color values

            public MeshColorAnalysis(string menuName) : base(menuName, true) { } // Note: is mutually exclusive

            public abstract int GetPixelIndexForAnalysis(Point3d vertex, params Point3d[] analysisPts);

            public abstract void ComputeLookupTableForAnalysis(double sensorElevation);

            public void ComputeLinearRanges(params VisualisationRangeWithColor[] lookUpRanges)
            {
                var lookupTableMaximumSize = 1;
                foreach (var range in lookUpRanges) lookupTableMaximumSize += range.ValueEnd - range.ValueStart;
                lookupTable = new Color[lookupTableMaximumSize]; 

                // Populate dict values by interpolating colors within each of the lookup ranges
                foreach (var range in lookUpRanges)
                    for (var i = range.ValueStart; i < range.ValueEnd; i++)
                    {
                        var progress = ((double)i - range.ValueStart) / (range.ValueEnd - range.ValueStart);
                        lookupTable[i] = range.InterpolateColor(progress);
                    }
            }
        }

        public class None : MeshColorAnalysis
        {
            public None() : base("No Visualisation") { }

            public override int GetPixelIndexForAnalysis(Point3d vertex, params Point3d[] analysisPts)
            {
                return 0; // Should never be called (see below)
            }

            public override void ComputeLookupTableForAnalysis(double sensorElevation)
            {
                lookupTable = new Color[0]; // Empty color table allows pixel loop to skip lookup
            }
        }

        public class Elevation : MeshColorAnalysis
        {
            public Elevation() : base("Visualise Elevation") { }

            public override int GetPixelIndexForAnalysis(Point3d vertex, params Point3d[] analysisPts)
            {
                return (int)vertex.Z;
            }

            public override void ComputeLookupTableForAnalysis(double sensorElevation)
            {
                var normalElevationRange = new VisualisationRangeWithColor
                {
                    ValueStart = 0,
                    ValueEnd = (int)sensorElevation - 201,
                    ColorStart = new ColorHSL(0.20, 0.35, 0.02),
                    ColorEnd = new ColorHSL(0.50, 0.85, 0.85)
                }; // A clear gradient for pixels inside the expected normal model height 

                var extraElevationRange = new VisualisationRangeWithColor
                {
                    ValueStart = (int)sensorElevation - 200,
                    ValueEnd = (int)sensorElevation + 1,
                    ColorStart = new ColorHSL(1.00, 0.85, 0.76),
                    ColorEnd = new ColorHSL(0.50, 0.85, 0.99)
                }; // A fallback gradiend for those outside (TODO: set sensible colors here)
                ComputeLinearRanges(normalElevationRange, extraElevationRange);
            }
        }

        private class Slope : MeshColorAnalysis
        {
            public Slope() : base("Visualise Slope") { }

            public override int GetPixelIndexForAnalysis(Point3d vertex, params Point3d[] neighbours)
            {
                return 22; // TODO: benchmark different methods for passing pixels before enabling a real calculation
                // Loop over the neighbouring pixels; calculate slopes relative to vertex
                //double slopeSum = 0;
                //for (int i = 0; i < neighbours.Length; i++)
                //{
                //    double rise = vertex.Z - neighbours[i].Z;
                //    double run = Math.Sqrt(Math.Pow(vertex.X - neighbours[i].X, 2) + Math.Pow(vertex.Y - neighbours[i].Y, 2));
                //    slopeSum += rise / run;
                //}
                //double slopeAverage = Math.Abs(slopeSum / neighbours.Length);
                //double slopeAsPercent = slopeAverage * 100; // Array is keyed as 0 - 100
                //return (int)slopeAsPercent; // Cast to int as its cross-referenced to the lookup 
            }

            public override void ComputeLookupTableForAnalysis(double sensorElevation)
            {
                var slopeRange = new VisualisationRangeWithColor
                {
                    ValueStart = 0,
                    ValueEnd = 100,
                    ColorStart = new ColorHSL(1.0, 1.0, 1.0), // White
                    ColorEnd = new ColorHSL(1.0, 1.0, 0.3) // Dark Red
                };
                ComputeLinearRanges(slopeRange);
            }
        }

        private class Aspect : MeshColorAnalysis
        {
            public Aspect() : base("Visualise Aspect") { }

            public override int GetPixelIndexForAnalysis(Point3d vertex, params Point3d[] analysisPts)
            {
                return 44;
            }

            public override void ComputeLookupTableForAnalysis(double sensorElevation)
            {
                var rightAspect = new VisualisationRangeWithColor
                {
                    ValueStart = 0,
                    ValueEnd = 180,
                    ColorStart = new ColorHSL(1.0, 1.0, 1.0), // White
                    ColorEnd = new ColorHSL(1.0, 1.0, 0.3) // Dark Red
                };
                var leftAspect = new VisualisationRangeWithColor
                {
                    ValueStart = 180, // For the other side of the aspect we loop back to the 0 value
                    ValueEnd = 359,
                    ColorStart = new ColorHSL(1.0, 1.0, 0.3), // Dark Red
                    ColorEnd = new ColorHSL(1.0, 1.0, 1.0) // White
                };
                ComputeLinearRanges(rightAspect, leftAspect);
            }
        }


        public class Contours : MeshGeometryAnalysis
        {
            public Contours() : base("Show Contour Lines") { }
        }

        public class Water : MeshGeometryAnalysis
        {
            public Water() : base("Show Water Level") { }
        }
    }
}