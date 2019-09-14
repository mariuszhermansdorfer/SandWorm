using System;
using System.Drawing;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Rhino.Geometry;
using SandWorm;

namespace SandwormBenchmarks
{
    public class RunColorBenchmarks
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<MeshColorBenchmarks>();

            MeshColorBenchmarks.TestA();
            MeshColorBenchmarks.TestB();

            // Prevent console from auto exiting
            Console.Write("Finished Benchmarks. Press any key to exit.");
            var wait = Console.ReadLine();
        }
    }
    public class MeshColorBenchmarks : MockMesh
    {
        [Benchmark(Baseline = true)]
        public static void TestA()
        {
            var pointCloud = new Point3d[trimmedWidth * trimmedHeight];
            vertexColors = new Color[trimmedWidth * trimmedHeight];
            var arrayIndex = 0;
            for (var rows = 0; rows < trimmedHeight; rows++)
            for (var columns = 0; columns < trimmedWidth; columns++)
            {
                tempPoint.X = columns * -unitsMultiplier * depthPixelSize.x;
                tempPoint.Y = rows * -unitsMultiplier * depthPixelSize.y;

                depthPoint = averagedDepthFrameData[arrayIndex];
                tempPoint.Z = (depthPoint - sensorElevation) * -unitsMultiplier;

                pointCloud[arrayIndex] = tempPoint;

                if (vertexColors.Length > 0) // Proxy for whether a mesh-coloring visualisation has been enabled
                    vertexColors[arrayIndex] = Analysis.AnalysisManager.GetPixelColor((int) depthPoint);

                arrayIndex++;
            }
        }

        [Benchmark]
        public static void TestB()
        {
            var pointCloud = new Point3d[trimmedWidth * trimmedHeight];
            vertexColors = new Color[trimmedWidth * trimmedHeight];
            var arrayIndex = 0;
            for (var rows = 0; rows < trimmedHeight; rows++)
            for (var columns = 0; columns < trimmedWidth; columns++)
            {
                tempPoint.X = columns * -unitsMultiplier * depthPixelSize.x;
                tempPoint.Y = rows * -unitsMultiplier * depthPixelSize.y;

                depthPoint = averagedDepthFrameData[arrayIndex];
                tempPoint.Z = (depthPoint - sensorElevation) * -unitsMultiplier;

                pointCloud[arrayIndex] = tempPoint;

                if (vertexColors.Length > 0) // Proxy for whether a mesh-coloring visualisation has been enabled
                    vertexColors[arrayIndex] = Analysis.AnalysisManager.GetPixelColor((int) depthPoint);

                arrayIndex++;
            }
        }
    }
}