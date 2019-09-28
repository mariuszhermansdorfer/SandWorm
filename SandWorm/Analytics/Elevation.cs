using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino.Display;
using Rhino.Geometry;

namespace SandWorm.Analytics
{
    public class Elevation : Analysis.MeshColorAnalysis
    {
        private int _lastSensorElevation; // Keep track of prior values to recalculate only as needed

        public Elevation() : base("Visualise Elevation")
        {
        }

        public Color[] GetColorCloudForAnalysis(double[] pixelArray, double sensorElevation)
        {
            var sensorElevationRounded = (int)sensorElevation; // Convert once as it is done often
            if (lookupTable == null || sensorElevationRounded != _lastSensorElevation)
            {
                ComputeLookupTableForAnalysis(sensorElevation);
            }

            // Lookup elevation value in color table
            var vertexColors = new Color[pixelArray.Length];
            for (int i = 0; i < pixelArray.Length; i++)
            {
                var pixelDepthNormalised = sensorElevationRounded - (int)pixelArray[i];
                if (pixelDepthNormalised < 0)
                    pixelDepthNormalised = 0; // Account for negative depths
                if (pixelDepthNormalised >= lookupTable.Length)
                    pixelDepthNormalised = lookupTable.Length - 1; // Account for big height

                vertexColors[i] = lookupTable[pixelDepthNormalised]; // Lookup z value in color table
            }
            return vertexColors;
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
            _lastSensorElevation = (int)sensorElevation;
        }
    }
}