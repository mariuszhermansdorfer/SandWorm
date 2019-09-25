using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino.Display;
using Rhino.Geometry;

namespace SandWorm.Analytics
{
    public class Elevation : Analysis.MeshColorAnalysis
    {
        public Elevation() : base("Visualise Elevation")
        {
        }
        public void getColorCloudForAnalysis(ref Color[] vertexColors, double[] pixelArray)
        {

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
}