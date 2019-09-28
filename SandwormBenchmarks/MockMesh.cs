using System;
using System.Drawing;
using SandWorm;
using Rhino.Geometry;

namespace SandwormBenchmarks
{
    public class MockMesh
    {
        public static int trimmedHeight = 424;
        public static int trimmedWidth = 512;
        public static int sensorElevation = 1000;
        public static Core.PixelSize depthPixelSize = new Core.PixelSize { x = 3.0, y = 3.0 };
        public static int unitsMultiplier = 1;
        public static double[] averagedDepthFrameData;
        public static Mesh quadMesh;
        // Temp

        static MockMesh()
        {
            // Make a random arrangement of depths
            averagedDepthFrameData = new double[trimmedWidth * trimmedHeight];
            Random randNum = new Random();
            for (int i = 0; i < averagedDepthFrameData.Length; i++)
            {
                averagedDepthFrameData[i] = randNum.Next(sensorElevation - 25, sensorElevation);
            }

            // Make a mesh TODO: load a valid mesh object (from XML?) as RhinoCommon doesn't work within CLI
            // quadMesh = Core.CreateQuadMesh(quadMesh, pointCloud, null, trimmedWidth, trimmedHeight);
        }
    }
}
