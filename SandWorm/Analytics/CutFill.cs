using System.Drawing;
using Rhino.Display;


namespace SandWorm.Analytics
{
    public class CutFill : Analysis.MeshColorAnalysis
    {
        readonly ushort maximumCutFill = 100; // Limit the range to allow for better visualization of differences
        public CutFill() : base("Visualise difference between meshes")
        {
        }
        private Color GetColorForCutFill(int cutFillValue)
        {
            if (cutFillValue < 0)
                return lookupTable[0];
            else if (cutFillValue > maximumCutFill)
                return lookupTable[lookupTable.Length - 1];
            else 
                return lookupTable[cutFillValue];

        }

        public Color[] GetColorCloudForAnalysis(double[] pixelArray, CompareMeshes referenceMeshElevations)
        {
            if (lookupTable == null)
            {
                ComputeLookupTableForAnalysis(0.0);
            }

            var vertexColors = new Color[pixelArray.Length];

            for (int i = 0; i < referenceMeshElevations.MeshElevationPoints.Length; i++)
            {
                vertexColors[i] = GetColorForCutFill((int)(pixelArray[i] - referenceMeshElevations.MeshElevationPoints[i]) + maximumCutFill); 
            }
            return vertexColors;
        }

        public override void ComputeLookupTableForAnalysis(double sensorElevation)
        {
            var cut = new Analysis.VisualisationRangeWithColor
            {
                ValueSpan = maximumCutFill,
                ColorStart = new ColorHSL(1.0, 1.0, 0.3), // Dark Red
                ColorEnd = new ColorHSL(1.0, 1.0, 1.0) // White
            };
            var fill = new Analysis.VisualisationRangeWithColor
            {
                ValueSpan = maximumCutFill, 
                ColorStart = new ColorHSL(1.0, 1.0, 1.0), // White
                ColorEnd = new ColorHSL(0.3, 1.0, 0.3) // Dark Green
            };
            ComputeLinearRanges(cut, fill);
        }
    }
}