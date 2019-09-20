using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino.Display;
using Rhino.Geometry;

namespace SandWorm
{
    class Analytics
    {
        /// <summary>Implementations of particular analysis options</summary>

        public class None : Analysis.MeshColorAnalysis
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

        public class Elevation : Analysis.MeshColorAnalysis
        {
            public Elevation() : base("Visualise Elevation") { }

            public override int GetPixelIndexForAnalysis(Point3d vertex, params Point3d[] analysisPts)
            {
                return (int)vertex.Z;
            }

            public override void ComputeLookupTableForAnalysis(double sensorElevation)
            {
                var normalElevationRange = new Analysis.VisualisationRangeWithColor
                {
                    ValueStart = 0,
                    ValueEnd = (int)sensorElevation - 201,
                    ColorStart = new ColorHSL(0.20, 0.35, 0.02),
                    ColorEnd = new ColorHSL(0.50, 0.85, 0.85)
                }; // A clear gradient for pixels inside the expected normal model height 

                var extraElevationRange = new Analysis.VisualisationRangeWithColor
                {
                    ValueStart = (int)sensorElevation - 200,
                    ValueEnd = (int)sensorElevation + 1,
                    ColorStart = new ColorHSL(1.00, 0.85, 0.76),
                    ColorEnd = new ColorHSL(0.50, 0.85, 0.99)
                }; // A fallback gradiend for those outside (TODO: set sensible colors here)
                ComputeLinearRanges(normalElevationRange, extraElevationRange);
            }
        }

        public class Slope : Analysis.MeshColorAnalysis
        {
            public Slope() : base("Visualise Slope") { }

            public override int GetPixelIndexForAnalysis(Point3d vertex, params Point3d[] neighbours)
            {
                // Loop over the neighbouring pixels; calculate slopes relative to vertex
                double slopeSum = 0;
                for (int i = 0; i < neighbours.Length; i++)
                {
                    double rise = vertex.Z - neighbours[i].Z;
                    double run = Math.Sqrt(Math.Pow(vertex.X - neighbours[i].X, 2) + Math.Pow(vertex.Y - neighbours[i].Y, 2));
                    slopeSum += rise / run;
                }
                double slopeAverage = Math.Abs(slopeSum / neighbours.Length);
                double slopeAsPercent = slopeAverage * 100; // Array is keyed as 0 - 100
                return (int)slopeAsPercent; // Cast to int as its cross-referenced to the lookup 
            }

            public override void ComputeLookupTableForAnalysis(double sensorElevation)
            {
                var slopeRange = new Analysis.VisualisationRangeWithColor
                {
                    ValueStart = 0,
                    ValueEnd = 100,
                    ColorStart = new ColorHSL(1.0, 1.0, 1.0), // White
                    ColorEnd = new ColorHSL(1.0, 1.0, 0.3) // Dark Red
                };
                ComputeLinearRanges(slopeRange);
            }
        }

        public class Aspect : Analysis.MeshColorAnalysis
        {
            public Aspect() : base("Visualise Aspect") { }

            public override int GetPixelIndexForAnalysis(Point3d vertex, params Point3d[] analysisPts)
            {
                return 44;
            }

            public override void ComputeLookupTableForAnalysis(double sensorElevation)
            {
                var rightAspect = new Analysis.VisualisationRangeWithColor
                {
                    ValueStart = 0,
                    ValueEnd = 180,
                    ColorStart = new ColorHSL(1.0, 1.0, 1.0), // White
                    ColorEnd = new ColorHSL(1.0, 1.0, 0.3) // Dark Red
                };
                var leftAspect = new Analysis.VisualisationRangeWithColor
                {
                    ValueStart = 180, // For the other side of the aspect we loop back to the 0 value
                    ValueEnd = 359,
                    ColorStart = new ColorHSL(1.0, 1.0, 0.3), // Dark Red
                    ColorEnd = new ColorHSL(1.0, 1.0, 1.0) // White
                };
                ComputeLinearRanges(rightAspect, leftAspect);
            }
        }

        public class Contours : Analysis.MeshGeometryAnalysis
        {
            public Contours() : base("Show Contour Lines") { }

            public override void GetGeometryForAnalysis(ref List<GeometryBase> outputGeometry, double wl)
            {
                var dummyLine = new Line(new Point3d(0, 0, 0), new Point3d(1, 1, 1));
                var contour = NurbsCurve.CreateFromLine(dummyLine);
                outputGeometry.Add(contour); // TODO: actual implementation
            }
        }

        public class Water : Analysis.MeshGeometryAnalysis
        {
            public Water() : base("Show Water Level") { }

            public override void GetGeometryForAnalysis(ref List<GeometryBase> outputGeometry, double wl)
            {
                var waterPlane = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));
                var xInterval = new Interval(0, 100);
                var yInterval = new Interval(0, 100);
                var waterSrf = new PlaneSurface(waterPlane, xInterval, yInterval);
                outputGeometry.Add(waterSrf); // TODO: actual implementation
            }
        }
    }
}
