using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino.Display;
using Rhino.Geometry;

namespace SandWorm.Analytics
{
    public class Elevation : Analysis.MeshColorAnalysis
    {
        readonly int maximumElevation = 1000; // Needs to be high to account for weird sensorElevation parameters
        private int _lastSensorElevation; // Keep track of prior values to recalculate only as needed

        public Elevation() : base("Visualise Elevation")
        {
        }

        public void GetColorCloudForAnalysis(ref Color[] vertexColors, double[] pixelArray, double sensorElevation)
        {
            var sensorElevationRounded = (int)sensorElevation; // Convert once as it is done often
            if (lookupTable == null || sensorElevationRounded != _lastSensorElevation)
            {
                ComputeLookupTableForAnalysis(sensorElevation);
            }

            // Lookup elevation value in color table
            vertexColors = new Color[pixelArray.Length];
            for (int i = 0; i < pixelArray.Length; i++)
            {
                var pixelDepthNormalised = sensorElevationRounded - (int)pixelArray[i];
                if (pixelDepthNormalised < 0)
                    pixelDepthNormalised = 0; // Account for negative depths
                vertexColors[i] = lookupTable[pixelDepthNormalised]; // Lookup z value in color table
            }
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
            var weirdElevationRange = new Analysis.VisualisationRangeWithColor
            {
                ValueSpan = maximumElevation,
                ColorStart = new ColorHSL(0.10, 1, 0.8), // Gray
                ColorEnd = new ColorHSL(0.10, 1, 0.9) // Black
            };
            ComputeLinearRanges(sElevationRange, mElevationRange, lElevationRange, xlElevationRange, weirdElevationRange);
            _lastSensorElevation = (int)sensorElevation;
        }
    }
}