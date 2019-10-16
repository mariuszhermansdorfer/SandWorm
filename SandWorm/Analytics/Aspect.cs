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

        public Color[] GetColorCloudForAnalysis(double[] pixelArray, int width, int height)
        {
            if (lookupTable == null)
            {
                ComputeLookupTableForAnalysis(0.0);
            }
            var vertexColors = new Color[pixelArray.Length];

            // The approach below finds the difference in elevation along the X and Y axis
            // i.e. a difference of -10 in the X indicates a tilt to the left/west
            // These x/y tilts are treated as a vector that is thus the average orientation in X/Y 
            // This vector's angle is then measured relative to the up direction 
            // The tilts within diagonal axes are halved and added equally to each X/Y component
                       
            // first row
            for (int x = 1; x < width - 1; x++)
            {
                // vertexColors[i] = GetColorForAspect((short)angle);
            }

            // last row
            for (int x = (height - 1) * width + 1; x < height * width - 1; x++)
            {
                // vertexColors[i] = GetColorForAspect((short)angle);
            }

            // first column
            for (int x = width; x < (height - 1) * width; x += width)
            {
                // vertexColors[i] = GetColorForAspect((short)angle);
            }

            // last column
            for (int x = 2 * width - 1; x < height * width - 1; x += width)
            {
                // vertexColors[i] = GetColorForAspect((short)angle);
            }


            // Rest of the array
            Parallel.For(1, height - 1, rows => // Iterate over y dimension
            {
                for (var columns = 1; columns < width - 1; columns++) // Iterate over x dimension
                {
                    var h = (rows - 1) * width + columns;
                    var i = rows * width + columns;
                    var j = (rows + 1) * width + columns;

                    var deltaLR = pixelArray[i + 1] - pixelArray[i - 1]; // E - W
                    var deltaTB = pixelArray[h] - pixelArray[j]; // N - S
                    var deltaTLBR = pixelArray[h - 1] - pixelArray[j + 1]; // NW - SE
                    var deltaTRBL = pixelArray[h + 1] - pixelArray[j - 1]; // NE - SW

                    var deltaX = deltaLR - (deltaTLBR * 0.5) - (deltaTRBL * 0.5);
                    var deltaY = deltaTB - (deltaTLBR * 0.5) - (deltaTRBL * 0.5);
                    var angle = (Math.Atan2(deltaX, 1 - deltaY) * (180 / Math.PI) * -1) + 180;
                    vertexColors[i] = GetColorForAspect((short) angle);
                }
            });

            return vertexColors;
        }

        public override void ComputeLookupTableForAnalysis(double sensorElevation)
        {
            // The angle range coming from the calculation is -180 to +180; which then has 180 added to normalise as 0-360
            // So our color span starts at South (-180 > 0) and proceeds clockwise to North (0 > 180) and then South (180 > 360)

            var SWAspect = new Analysis.VisualisationRangeWithColor
            {
                ValueSpan = 90, // For the other side of the aspect we loop back to the 0 value
                ColorStart = new ColorHSL(1.0, 1.0, 0.0), // South = Black
                ColorEnd = new ColorHSL(0.5, 1.0, 0.5) // West = Blue
            };
            var NWAspect = new Analysis.VisualisationRangeWithColor
            {
                ValueSpan = 90, // For the other side of the aspect we loop back to the 0 value
                ColorStart = new ColorHSL(0.5, 1.0, 0.5), // West = Blue
                ColorEnd = new ColorHSL(1.0, 1.0, 1.0) // North = White
            };
            var NEAspect = new Analysis.VisualisationRangeWithColor
            {
                ValueSpan = 90,
                ColorStart = new ColorHSL(1.0, 1.0, 1.0), // North = White
                ColorEnd = new ColorHSL(1.0, 1.0, 0.5) // East = Red
            };
            var SEAspect = new Analysis.VisualisationRangeWithColor
            {
                ValueSpan = 90,
                ColorStart = new ColorHSL(1.0, 1.0, 0.5), // East = Red
                ColorEnd = new ColorHSL(1.0, 1.0, 0.0) // South = Black
            };
            ComputeLinearRanges(SWAspect, NWAspect, NEAspect, SEAspect);
        }
    }
}