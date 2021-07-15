using System;
using System.Drawing;
using System.Threading.Tasks;
using Rhino.Display;

namespace SandWorm.Analytics
{
    public class Aspect : Analysis.MeshColorAnalysis
    {
        public Aspect() : base("Visualise Aspect")
        {
        }

        private Color GetColorForAspect(short aspectValue)
        {
            if (aspectValue >= lookupTable.Length)
                return lookupTable[lookupTable.Length - 1];
            if (aspectValue < 0)
                return lookupTable[0];
            return lookupTable[aspectValue];
        }

        public Color[] GetColorCloudForAnalysis(double[] pixelArray, int width, int height, double gradientRange)
        {
            if (lookupTable == null)
            {
                ComputeLookupTableForAnalysis(0.0, gradientRange);
            }
            var vertexColors = new Color[pixelArray.Length];

            // The approach below finds the difference in elevation along the X and Y axis
            // i.e. a difference of -10 in the X indicates a tilt to the left/west
            // These x/y tilts are treated as a vector that is thus the average orientation in X/Y 
            // This vector's angle is then measured relative to the up direction 
            // The tilts within diagonal axes are halved and added equally to each X/Y component
            double deltaLR, deltaTB, deltaTLBR, deltaTRBL, deltaX, deltaY, radianAngle, normalisedDegreeAngle;

            // First pixel NW
            deltaLR = pixelArray[1]; // E 
            deltaTB = pixelArray[width]; // S
            deltaTLBR = pixelArray[width + 1]; // SE;

            deltaX = deltaLR - (deltaTLBR * 0.5);
            deltaY = deltaTB - (deltaTLBR * 0.5);
            radianAngle = (Math.Atan2(deltaX, deltaY) - Math.Atan2(0, 1));
            normalisedDegreeAngle = (radianAngle * (180 / Math.PI)) + 180;
            vertexColors[0] = GetColorForAspect((short)normalisedDegreeAngle);

            // Last pixel NE
            deltaLR = pixelArray[width - 2]; // W
            deltaTB = pixelArray[2 * width - 1]; // S
            deltaTRBL = pixelArray[2 * width - 1]; // SW

            deltaX = deltaLR - (deltaTRBL * 0.5);
            deltaY = deltaTB - (deltaTRBL * 0.5);
            radianAngle = (Math.Atan2(deltaX, deltaY) - Math.Atan2(0, 1));
            normalisedDegreeAngle = (radianAngle * (180 / Math.PI)) + 180;
            vertexColors[width - 1] = GetColorForAspect((short)normalisedDegreeAngle);

            // First pixel SW
            deltaLR = pixelArray[(height - 1) * width + 1]; // E 
            deltaTB = pixelArray[(height - 2) * width]; // N 
            deltaTRBL = pixelArray[(height - 2) * width + 1]; // NE 

            deltaX = deltaLR - (deltaTRBL * 0.5);
            deltaY = deltaTB - (deltaTRBL * 0.5);
            radianAngle = (Math.Atan2(deltaX, deltaY) - Math.Atan2(0, 1));
            normalisedDegreeAngle = (radianAngle * (180 / Math.PI)) + 180;
            vertexColors[(height - 1) * width] = GetColorForAspect((short)normalisedDegreeAngle);

            // Last pixel SE
            deltaLR = pixelArray[height * width - 2]; // W
            deltaTB = pixelArray[(height - 1) * width - 1]; // N
            deltaTLBR = pixelArray[(height - 1) * width - 2]; // NW 

            deltaX = deltaLR - (deltaTLBR * 0.5);
            deltaY = deltaTB - (deltaTLBR * 0.5);
            radianAngle = (Math.Atan2(deltaX, deltaY) - Math.Atan2(0, 1));
            normalisedDegreeAngle = (radianAngle * (180 / Math.PI)) + 180;
            vertexColors[height * width - 1] = GetColorForAspect((short)normalisedDegreeAngle);

            // First row
            for (int x = 1; x < width - 1; x++)
            {
                deltaLR = pixelArray[x + 1] - pixelArray[x - 1]; // E - W
                deltaTB = pixelArray[x + width]; // S
                deltaTLBR = pixelArray[x + width - 1]; // SW 
                deltaTRBL = pixelArray[x + width + 1]; // SE 

                deltaX = deltaLR - (deltaTLBR * 0.5) - (deltaTRBL * 0.5);
                deltaY = deltaTB - (deltaTLBR * 0.5) - (deltaTRBL * 0.5);
                radianAngle = (Math.Atan2(deltaX, deltaY) - Math.Atan2(0, 1));
                normalisedDegreeAngle = (radianAngle * (180 / Math.PI)) + 180;
                vertexColors[x] = GetColorForAspect((short)normalisedDegreeAngle);
            }

            // Last row
            for (int x = (height - 1) * width + 1; x < height * width - 1; x++)
            {
                deltaLR = pixelArray[x + 1] - pixelArray[x - 1]; // E - W
                deltaTB = pixelArray[x - width]; // N
                deltaTLBR = pixelArray[x - width - 1]; // NW 
                deltaTRBL = pixelArray[x - width + 1]; // NE 

                deltaX = deltaLR - (deltaTLBR * 0.5) - (deltaTRBL * 0.5);
                deltaY = deltaTB - (deltaTLBR * 0.5) - (deltaTRBL * 0.5);
                radianAngle = (Math.Atan2(deltaX, deltaY) - Math.Atan2(0, 1));
                normalisedDegreeAngle = (radianAngle * (180 / Math.PI)) + 180;
                vertexColors[x] = GetColorForAspect((short)normalisedDegreeAngle);
            }

            // First column
            for (int x = width; x < (height - 1) * width; x += width)
            {
                deltaLR = pixelArray[x + 1]; // E 
                deltaTB = pixelArray[x - width] - pixelArray[x + width]; // N - S
                deltaTLBR = pixelArray[x + width + 1]; // SE
                deltaTRBL = pixelArray[x - width + 1]; // NE

                deltaX = deltaLR - (deltaTLBR * 0.5) - (deltaTRBL * 0.5);
                deltaY = deltaTB - (deltaTLBR * 0.5) - (deltaTRBL * 0.5);
                radianAngle = (Math.Atan2(deltaX, deltaY) - Math.Atan2(0, 1));
                normalisedDegreeAngle = (radianAngle * (180 / Math.PI)) + 180;
                vertexColors[x] = GetColorForAspect((short)normalisedDegreeAngle);
            }

            // Last column
            for (int x = 2 * width - 1; x < height * width - 1; x += width)
            {
                deltaLR = pixelArray[x - 1]; // W
                deltaTB = pixelArray[x - width] - pixelArray[x + width]; // N - S
                deltaTLBR = pixelArray[x - width - 1]; // NW 
                deltaTRBL = pixelArray[x + width - 1]; // SW

                deltaX = deltaLR - (deltaTLBR * 0.5) - (deltaTRBL * 0.5);
                deltaY = deltaTB - (deltaTLBR * 0.5) - (deltaTRBL * 0.5);
                radianAngle = (Math.Atan2(deltaX, deltaY) - Math.Atan2(0, 1));
                normalisedDegreeAngle = (radianAngle * (180 / Math.PI)) + 180;
                vertexColors[x] = GetColorForAspect((short)normalisedDegreeAngle);
            }

            // Rest of the array
            Parallel.For(1, height - 1, rows => // Iterate over y dimension
            {
                for (var columns = 1; columns < width - 1; columns++) // Iterate over x dimension
                {
                    var h = (rows - 1) * width + columns;
                    var i = rows * width + columns;
                    var j = (rows + 1) * width + columns;

                    // Note using inline variable prefix to localise references in parallel ops
                    var inlineDeltaLR = pixelArray[i + 1] - pixelArray[i - 1]; // E - W
                    var inlineDeltaTB = pixelArray[h] - pixelArray[j]; // N - S
                    var inlineDeltaTLBR = pixelArray[h - 1] - pixelArray[j + 1]; // NW - SE
                    var inlineDeltaTRBL = pixelArray[h + 1] - pixelArray[j - 1]; // NE - SW

                    var inlineDeltaX = inlineDeltaLR - (inlineDeltaTLBR * 0.5) - (inlineDeltaTRBL * 0.5);
                    var inlineDeltaY = inlineDeltaTB - (inlineDeltaTLBR * 0.5) - (inlineDeltaTRBL * 0.5);
                    var inlineRadians = (Math.Atan2(inlineDeltaLR, inlineDeltaTB) - Math.Atan2(0, 1));
                    var inlineNormalisedDegrees = (inlineRadians * (180 / Math.PI)) + 180;
                    vertexColors[i] = GetColorForAspect((short)inlineNormalisedDegrees);
                }
            });

            return vertexColors;
        }

        public override void ComputeLookupTableForAnalysis(double sensorElevation, double gradientRange)
        {
            var north = new ColorHSL(0.7, 1, 0.90); // North = Blue
            var east = new ColorHSL(0.9, 1, 0.5); // East = Pink
            var south = new ColorHSL(0.5, 0, 0.10); // South = Black
            var west = new ColorHSL(0.5, 1, 0.5); // West = Teal

            // The angle range coming from the calculation is -180 to +180; which then has 180 added to normalise as 0-360
            // So our color span starts at South (-180 > 0) and proceeds clockwise to North (0 > 180) and then South (180 > 360)
            var NWAspect = new Analysis.VisualisationRangeWithColor
            {
                ValueSpan = 90, ColorStart = south, ColorEnd = east 
            };
            var SWAspect = new Analysis.VisualisationRangeWithColor
            {
                ValueSpan = 90, ColorStart = east, ColorEnd = north
            };
            var SEAspect = new Analysis.VisualisationRangeWithColor
            {
                ValueSpan = 90, ColorStart = north, ColorEnd = west
            };
            var NEAspect = new Analysis.VisualisationRangeWithColor
            {
                ValueSpan = 90, ColorStart = west, ColorEnd = south
            };
            ComputeLinearRanges(NWAspect, SWAspect, SEAspect, NEAspect);
        }
    }
}