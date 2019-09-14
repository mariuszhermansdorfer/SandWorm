using System;
using System.Drawing;
using SandWorm;
using Rhino.Geometry;

namespace SandwormBenchmarks
{
    public class MockMesh
    {
        public static int runs = 100;
        public static int trimmedRuns = 25; // Remove the top N and bottom N results
        public static int trimmedHeight = 424;
        public static int trimmedWidth = 512;
        public static int sensorElevation = 1000;
        public static int waterLevel = 50;
        public static int unitsMultiplier = 1;
        public static Core.PixelSize depthPixelSize = new Core.PixelSize { x = 3.0, y = 3.0 };
        public static double depthPoint;
        public static Point3d tempPoint = new Point3d();
        public static Color[] vertexColors;
        public static double[] averagedDepthFrameData;

        static MockMesh()
        {
            // Make a random arrangement of depths
            averagedDepthFrameData = new double[trimmedWidth * trimmedHeight];
            Random randNum = new Random();
            for (int i = 0; i < averagedDepthFrameData.Length; i++)
            {
                averagedDepthFrameData[i] = randNum.Next(800, 1000);
            }
            Analysis.AnalysisManager.ComputeLookupTables(sensorElevation, waterLevel);
        }
    }
}
