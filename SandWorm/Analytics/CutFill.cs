using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino.Display;
using Rhino.Geometry;

namespace SandWorm.Analytics
{
    public class CutFill : Analysis.MeshColorAnalysis
    {
        readonly ushort maximumCutFill = 200;
        public CutFill() : base("Visualise difference between meshes")
        {
        }
        private Color getColorForCutFill(int cutFillValue)
        {
            if (cutFillValue > maximumCutFill)
                return lookupTable[lookupTable.Length - 1];
            else
                return lookupTable[cutFillValue];
        }

        public void GetColorCloudForAnalysis(ref Color[] vertexColors, double[] pixelArray, CutFillResults referenceMeshElevations)
        {
            if (lookupTable == null)
            {
                ComputeLookupTableForAnalysis(0.0);
            }

            vertexColors = new Color[pixelArray.Length];


        }

        public override void ComputeLookupTableForAnalysis(double sensorElevation)
        {
            var cut = new Analysis.VisualisationRangeWithColor
            {
                ValueSpan = 100,
                ColorStart = new ColorHSL(1.0, 1.0, 0.3), // Dark Red
                ColorEnd = new ColorHSL(1.0, 1.0, 1.0) // White
            };
            var fill = new Analysis.VisualisationRangeWithColor
            {
                ValueSpan = 100, 
                ColorStart = new ColorHSL(1.0, 1.0, 1.0), // White
                ColorEnd = new ColorHSL(0.3, 1.0, 0.3) // Dark Green
            };
            ComputeLinearRanges(cut, fill);
        }
    }
}