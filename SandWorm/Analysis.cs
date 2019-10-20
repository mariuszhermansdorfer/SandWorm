﻿using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Rhino.Display;
using Rhino.Geometry;

namespace SandWorm
{
    public static class Analysis
    {
        /// <summary>Abstractions for managing analysis state.</summary>
        public static class AnalysisManager
        {
            /// <summary>Stories copies of each analysis option and interfaces their use with components.</summary>
            public static List<MeshAnalysis> options;

            static AnalysisManager() // Note that the order of items here determines their menu order
            {
                options = new List<MeshAnalysis>
                {
                    new Analytics.WaterLevel(), new Analytics.Contours(),
                    new Analytics.None(),
                    new Analytics.Elevation(), new Analytics.Slope(), new Analytics.Aspect(), new Analytics.CutFill()
                };
                // Default to showing elevation analysis
                options[3].isEnabled = true;
            }

            public static List<MeshAnalysis> GetEnabledAnalyses()
            {
                return options.FindAll(x => x.isEnabled);
            }

            public static MeshColorAnalysis GetEnabledMeshColoring()
            {
                foreach (var enabledOption in GetEnabledAnalyses())
                    if (enabledOption.GetType().IsSubclassOf(typeof(MeshColorAnalysis)))
                        return enabledOption as MeshColorAnalysis;
                return null; // Shouldn't happen; a mesh coloring option (even no color) is always set
            }

            public static List<MeshGeometryAnalysis> GetEnabledMeshAnalytics()
            {
                var enabledGeometryAnalysis = new List<MeshGeometryAnalysis>();
                foreach (var enabledOption in GetEnabledAnalyses())
                {
                    // Testing inheritance with generics is not going to work; so just check if the option is not a color one 
                    if (enabledOption.GetType().IsSubclassOf(typeof(MeshGeometryAnalysis)))
                        enabledGeometryAnalysis.Add(enabledOption as MeshGeometryAnalysis);
                }
                return enabledGeometryAnalysis; 
            }

            public static void SetEnabledOptions(ToolStripMenuItem selectedMenuItem)
            {
                var selectedOption = options.Find(x => x.MenuItem == selectedMenuItem);
                if (selectedOption.IsExclusive)
                    foreach (var exclusiveOption in options.FindAll(x => x.IsExclusive))
                        exclusiveOption.isEnabled =
                            selectedOption == exclusiveOption; // Toggle selected item; untoggle other exclusive items
                else
                    selectedOption.isEnabled = !selectedOption.isEnabled; // Simple toggle for independent items
            }
        }

        public class VisualisationRangeWithColor
        {
            /// <summary>Describes a numeric range (e.g. elevation or slope values) and color range to visualise it.</summary>
            public int ValueSpan { get; set; }
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
            public bool isEnabled; // Whether to apply the analysis

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
            public MeshGeometryAnalysis(string menuName) : base(menuName, false)
            {
            } // Note: not mutually exclusive

            // Note that the use of <GeometryBase> may potentially exclude some geometric types as returnable
            // Note also the need to hard-code params useful to any of the analytics; operator overloading wont work :(
            public abstract void GetGeometryForAnalysis(ref List<GeometryBase> outputGeometry, double controlParameter, Mesh mesh);
        }

        public abstract class MeshColorAnalysis : MeshAnalysis
        {
            /// <summary>A form of analysis that colors the vertices of the entire mesh</summary>
            public Color[] lookupTable; // Dictionary of integers that map to color values

            public MeshColorAnalysis(string menuName) : base(menuName, true)
            {
            } // Note: is mutually exclusive
            
            public abstract void ComputeLookupTableForAnalysis(double sensorElevation);

            public void ComputeLinearRanges(params VisualisationRangeWithColor[] lookUpRanges)
            {
                var lookupTableMaximumSize = lookUpRanges.Length;
                foreach (var range in lookUpRanges) lookupTableMaximumSize += range.ValueSpan;
                lookupTable = new Color[lookupTableMaximumSize];

                // Populate dict values by interpolating colors within each of the lookup ranges
                var index = 0;
                foreach (var range in lookUpRanges)
                    for (var i = 0; i <= range.ValueSpan; i++)
                    {
                        var progress = (double) i / range.ValueSpan;
                        lookupTable[index] = range.InterpolateColor(progress);
                        index++;
                    }
            }
        }
    }
}