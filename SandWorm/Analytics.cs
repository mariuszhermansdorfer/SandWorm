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

            public override int GetPixelIndexForAnalysis(Point3d vertex, List<Point3d> analysisPts)
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

            public override int GetPixelIndexForAnalysis(Point3d vertex, List<Point3d> analysisPts)
            {
                if (vertex.Z > 0)
                    return (int)vertex.Z;
                return 0; // Usually occurs when sensor height is configured incorrectly
            }

            public override void ComputeLookupTableForAnalysis(double sensorElevation)
            {
                var sElevationRange = new Analysis.VisualisationRangeWithColor
                {
                    ValueSpan = 50,
                    ColorStart = new ColorHSL(0.40, 0.35, 0.3), // Dark Green
                    ColorEnd = new ColorHSL(0.30, 0.85, 0.4) // Green
                };
                var mElevationRange = new Analysis.VisualisationRangeWithColor
                {
                    ValueSpan = 50,
                    ColorStart = new ColorHSL(0.30, 0.85, 0.4), // Green
                    ColorEnd = new ColorHSL(0.20, 0.85, 0.5) // Yellow
                };
                var lElevationRange = new Analysis.VisualisationRangeWithColor
                {
                    ValueSpan = 50,
                    ColorStart = new ColorHSL(0.20, 0.85, 0.5), // Yellow
                    ColorEnd = new ColorHSL(0.10, 0.85, 0.6) // Orange
                };
                var xlElevationRange = new Analysis.VisualisationRangeWithColor
                {
                    ValueSpan = 50,
                    ColorStart = new ColorHSL(0.10, 1, 0.6), // Orange
                    ColorEnd = new ColorHSL(0.00, 1, 0.7) // Red
                };
                ComputeLinearRanges(sElevationRange, mElevationRange, lElevationRange, xlElevationRange);
            }
        }

        public class Slope : Analysis.MeshColorAnalysis
        {
            public Slope() : base("Visualise Slope") { }
            private int slopeCap = 500; // As measuring in % need an uppper limit on the value

            public override int GetPixelIndexForAnalysis(Point3d vertex, List<Point3d> neighbours)
            {
                // Loop over the neighbouring pixels; calculate slopes relative to vertex
                double slopeSum = 0;
                for (int i = 0; i < neighbours.Count; i++)
                {
                    double rise = vertex.Z - neighbours[i].Z;
                    double run = Math.Sqrt(Math.Pow(vertex.X - neighbours[i].X, 2) + Math.Pow(vertex.Y - neighbours[i].Y, 2));
                    slopeSum += Math.Abs(rise / run);
                }
                double slopeAverage = slopeSum / neighbours.Count;
                double slopeAsPercent = slopeAverage * 100; // Array is keyed as 0 - 100

                if (slopeAsPercent >= slopeCap)
                    return slopeCap;
                else
                    return (int)slopeAsPercent; // Cast to int as its then cross-referenced to the lookup 
            }

            public override void ComputeLookupTableForAnalysis(double sensorElevation)
            {
                var slightSlopeRange = new Analysis.VisualisationRangeWithColor
                {
                    ValueSpan = 30,
                    ColorStart = new ColorHSL(0.30, 1.0, 0.5), // Green
                    ColorEnd = new ColorHSL(0.15, 1.0, 0.5) // Yellow
                };
                var moderateSlopeRange = new Analysis.VisualisationRangeWithColor
                {
                    ValueSpan = 30,
                    ColorStart = new ColorHSL(0.15, 1.0, 0.5), // Green
                    ColorEnd = new ColorHSL(0.0, 1.0, 0.5) // Red
                };
                var extremeSlopeRange = new Analysis.VisualisationRangeWithColor
                {
                    ValueSpan = 200,
                    ColorStart = new ColorHSL(0.0, 1.0, 0.5), // Red
                    ColorEnd = new ColorHSL(0.0, 1.0, 0.0) // Black
                };
                ComputeLinearRanges(slightSlopeRange, moderateSlopeRange, extremeSlopeRange);
            }
        }

        public class Aspect : Analysis.MeshColorAnalysis
        {
            public Aspect() : base("Visualise Aspect") { }

            public override int GetPixelIndexForAnalysis(Point3d vertex, List<Point3d> analysisPts)
            {
                return 44;
            }

            public override void ComputeLookupTableForAnalysis(double sensorElevation)
            {
                var rightAspect = new Analysis.VisualisationRangeWithColor
                {
                    ValueSpan = 180,
                    ColorStart = new ColorHSL(1.0, 1.0, 1.0), // White
                    ColorEnd = new ColorHSL(1.0, 1.0, 0.3) // Dark Red
                };
                var leftAspect = new Analysis.VisualisationRangeWithColor
                {
                    ValueSpan = 180, // For the other side of the aspect we loop back to the 0 value
                    ColorStart = new ColorHSL(1.0, 1.0, 0.3), // Dark Red
                    ColorEnd = new ColorHSL(1.0, 1.0, 1.0) // White
                };
                ComputeLinearRanges(rightAspect, leftAspect);
            }
        }

        public class Contours : Analysis.MeshGeometryAnalysis
        {
            public Contours() : base("Show Contour Lines") { }

            public override void GetGeometryForAnalysis(ref List<GeometryBase> outputGeometry, int wl, int contourInterval, Mesh mesh)
            {
                var bounds = mesh.GetBoundingBox(false);
                var originStart = new Point3d(0, 0, bounds.Min.Z);
                var originEnd = new Point3d(0, 0, bounds.Max.Z);
                var contours = Mesh.CreateContourCurves(mesh, originStart, originEnd, contourInterval);
                outputGeometry.AddRange(contours);
;            }
        }

        public class Water : Analysis.MeshGeometryAnalysis
        {
            public Water() : base("Show Water Level") { }

            public override void GetGeometryForAnalysis(ref List<GeometryBase> outputGeometry, int waterLevel, int ci, Mesh mesh)
            {
                var bounds = mesh.GetBoundingBox(false);
                var waterPlane = new Plane(new Point3d(bounds.Max.X, bounds.Max.Y, waterLevel), new Vector3d(0, 0, 1));
                var test = new Interval(bounds.Max.X, bounds.Min.X);
                var waterSrf = new PlaneSurface(waterPlane,
                    new Interval(bounds.Min.X, bounds.Max.X),
                    new Interval(bounds.Min.Y, bounds.Max.Y)
                );
                outputGeometry.Add(waterSrf); 
            }
        }
    }
}
