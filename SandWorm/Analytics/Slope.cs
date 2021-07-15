using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Rhino.Display;
using Rhino.Geometry;

namespace SandWorm.Analytics
{
    public class Slope : Analysis.MeshColorAnalysis
    {
        readonly ushort maximumSlope = 1000; // Needs to be some form of cutoff to keep lookup table small; this = ~84%

        public Slope() : base("Visualise Slope")
        {
        }

        private Color GetColorForSlope(ushort slopeValue)
        {
            if (slopeValue > maximumSlope)
                return lookupTable[lookupTable.Length - 1];
            else
                return lookupTable[slopeValue];
        }

        public Color[] GetColorCloudForAnalysis(double[] pixelArray, int width, int height, double deltaX, double deltaY, double gradientRange)
        {
            if (lookupTable == null)
            {
                ComputeLookupTableForAnalysis(0.0, gradientRange);
            }
            var vertexColors = new Color[pixelArray.Length];

            // Calculate slope values
            double deltaXY = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
            double slope = 0.0;

            // first pixel NW
            slope += Math.Abs(pixelArray[1] - pixelArray[0]) / deltaX; // E Pixel
            slope += Math.Abs(pixelArray[width] - pixelArray[0]) / deltaY; // S Pixel
            slope += Math.Abs(pixelArray[width + 1] - pixelArray[0]) / deltaXY; // SE Pixel

            vertexColors[0] = GetColorForSlope((ushort)(slope * 33.33)); // Divide by 3 multiply by 100 => 33.33

            // last pixel NE
            slope = 0.0;
            slope += Math.Abs(pixelArray[width - 2] - pixelArray[width - 1]) / deltaX; // W Pixel
            slope += Math.Abs(pixelArray[2 * width - 1] - pixelArray[width - 1]) / deltaY; // S Pixel
            slope += Math.Abs(pixelArray[2 * width - 2] - pixelArray[width - 1]) / deltaXY; // SW Pixel

            vertexColors[width - 1] = GetColorForSlope((ushort)(slope * 33.33)); // Divide by 3 multiply by 100 => 33.33

            // first pixel SW
            slope = 0.0;
            slope += Math.Abs(pixelArray[(height - 1) * width + 1] - pixelArray[(height - 1) * width]) / deltaX; // E Pixel
            slope += Math.Abs(pixelArray[(height - 2) * width] - pixelArray[(height - 1) * width]) / deltaY; // N Pixel
            slope += Math.Abs(pixelArray[(height - 2) * width + 1] - pixelArray[(height - 1) * width]) / deltaXY; //NE Pixel

            vertexColors[(height - 1) * width] = GetColorForSlope((ushort)(slope * 33.33)); // Divide by 3 multiply by 100 => 33.33

            // last pixel SE
            slope = 0.0;
            slope += Math.Abs(pixelArray[height * width - 2] - pixelArray[height * width - 1]) / deltaX; // W Pixel
            slope += Math.Abs(pixelArray[(height - 1) * width - 1] - pixelArray[height * width - 1]) / deltaY; // N Pixel
            slope += Math.Abs(pixelArray[(height - 1) * width - 2] - pixelArray[height * width - 1]) / deltaXY; //NW Pixel

            vertexColors[height * width - 1] = GetColorForSlope((ushort)(slope * 33.33)); // Divide by 3 multiply by 100 => 33.33

            // first row
            for (int x = 1; x < width - 1; x++)
            {
                slope = 0.0;
                slope += Math.Abs(pixelArray[x - 1] - pixelArray[x]) / deltaX; // W Pixel
                slope += Math.Abs(pixelArray[x + 1] - pixelArray[x]) / deltaX; // E Pixel
                slope += Math.Abs(pixelArray[x + width - 1] - pixelArray[x]) / deltaXY; // SW Pixel
                slope += Math.Abs(pixelArray[x + width] - pixelArray[x]) / deltaY; // S Pixel
                slope += Math.Abs(pixelArray[x + width + 1] - pixelArray[x]) / deltaXY; // SE Pixel

                vertexColors[x] = GetColorForSlope((ushort)(slope * 20.0)); // Divide by 5 multiply by 100 => 20.0
            }

            // last row
            for (int x = (height - 1) * width + 1; x < height * width - 1; x++)
            {
                slope = 0.0;
                slope += Math.Abs(pixelArray[x - 1] - pixelArray[x]) / deltaX; // W Pixel
                slope += Math.Abs(pixelArray[x + 1] - pixelArray[x]) / deltaX; // E Pixel
                slope += Math.Abs(pixelArray[x - width - 1] - pixelArray[x]) / deltaXY; // NW Pixel
                slope += Math.Abs(pixelArray[x - width] - pixelArray[x]) / deltaY; // N Pixel
                slope += Math.Abs(pixelArray[x - width + 1] - pixelArray[x]) / deltaXY; // NE Pixel

                vertexColors[x] = GetColorForSlope((ushort)(slope * 20.0)); // Divide by 5 multiply by 100 => 20.0
            }

            // first column
            for (int x = width; x < (height - 1) * width; x += width)
            {
                slope = 0.0;
                slope += Math.Abs(pixelArray[x - width] - pixelArray[x]) / deltaY; // N Pixel
                slope += Math.Abs(pixelArray[x + width] - pixelArray[x]) / deltaY; // S Pixel
                slope += Math.Abs(pixelArray[x - width + 1] - pixelArray[x]) / deltaXY; // NE Pixel
                slope += Math.Abs(pixelArray[x + 1] - pixelArray[x]) / deltaX; // E Pixel
                slope += Math.Abs(pixelArray[x + width + 1] - pixelArray[x]) / deltaXY; // SE Pixel

                vertexColors[x] = GetColorForSlope((ushort)(slope * 20.0)); // Divide by 5 multiply by 100 => 20.0
            }

            // last column
            for (int x = 2 * width - 1; x < height * width - 1; x += width)
            {
                slope = 0.0;
                slope += Math.Abs(pixelArray[x - width] - pixelArray[x]) / deltaY; // N Pixel
                slope += Math.Abs(pixelArray[x + width] - pixelArray[x]) / deltaY; // S Pixel
                slope += Math.Abs(pixelArray[x - width - 1] - pixelArray[x]) / deltaXY; // NW Pixel
                slope += Math.Abs(pixelArray[x - 1] - pixelArray[x]) / deltaX; // W Pixel
                slope += Math.Abs(pixelArray[x + width - 1] - pixelArray[x]) / deltaXY; // SW Pixel

                vertexColors[x] = GetColorForSlope((ushort)(slope * 20.0)); // Divide by 5 multiply by 100 => 20.0
            }

            // rest of the array
            Parallel.For(1, height - 1, rows =>         // Iterate over y dimension
            {
                for (int columns = 1; columns < width - 1; columns++)             // Iterate over x dimension
                {
                    int h = (rows - 1) * width + columns;
                    int i = rows * width + columns;
                    int j = (rows + 1) * width + columns;

                    double parallelSlope = 0.0; // Declare a local variable in the parallel loop for performance reasons
                    parallelSlope += Math.Abs((pixelArray[h - 1] - pixelArray[i])) / deltaXY; // NW pixel
                    parallelSlope += Math.Abs((pixelArray[h] - pixelArray[i])) / deltaY; //N pixel
                    parallelSlope += Math.Abs((pixelArray[h + 1] - pixelArray[i])) / deltaXY; //NE pixel
                    parallelSlope += Math.Abs((pixelArray[i - 1] - pixelArray[i])) / deltaX; //W pixel
                    parallelSlope += Math.Abs((pixelArray[i + 1] - pixelArray[i])) / deltaX; //E pixel
                    parallelSlope += Math.Abs((pixelArray[j - 1] - pixelArray[i])) / deltaXY; //SW pixel
                    parallelSlope += Math.Abs((pixelArray[j] - pixelArray[i])) / deltaY; //S pixel
                    parallelSlope += Math.Abs((pixelArray[j + 1] - pixelArray[i])) / deltaXY; //SE pixel

                    vertexColors[i] = GetColorForSlope((ushort)(parallelSlope * 12.5)); // Divide by 8 multiply by 100 => 12.5
                }
            });

            return vertexColors;
        }

        public override void ComputeLookupTableForAnalysis(double sensorElevation, double gradientRange)
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
                ValueSpan = maximumSlope - slightSlopeRange.ValueSpan - moderateSlopeRange.ValueSpan,
                ColorStart = new ColorHSL(0.0, 1.0, 0.5), // Red
                ColorEnd = new ColorHSL(0.0, 1.0, 0.0) // Black
            };
            ComputeLinearRanges(slightSlopeRange, moderateSlopeRange, extremeSlopeRange);
        }
    }
}