using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using SandWorm;
using Rhino.Geometry;

namespace SandwormBenchmarks
{
    class MeshColoring : MockMesh
    {
        public MeshColoring() : base() { }

        static void Main(string[] args)
        {
            List <Action> tests = new List<Action> { TestA, TestB }; // Specify tests to run
            List<Stopwatch> timers = new List<Stopwatch>();
            foreach (Action test in tests)
                timers.Add(new Stopwatch());

            // To try and avoid CPU timing issues use second core and bump process priority
            Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(2); 
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            foreach (Action test in tests)
                test(); // Warm up functions

            Console.Write("Starting Benchmarks; running over {0} iterations\n", runs);
            for (int i = 0; i < runs; i++) // Alternate running each method; otherwise first=fastest?
            {
                for (int j = 0; j < tests.Count; j++)
                { 
                    RunTest(timers[j], tests[j]);
                }
            }

            for (int i = 0; i < tests.Count; i++)
            {
                var result = string.Format("{0:0000.0}", timers[i].ElapsedMilliseconds / (float)runs);
                Console.WriteLine("{0}:\t {1}ms avg execution", tests[i].Method.Name, result);
            }

            // Prevent console from auto exiting
            Console.Write("Finished Benchmarks. Press any key to exit.");
            var wait = Console.ReadLine();
        }

        static void RunTest(Stopwatch timer, Action method)
        {
            // Clean up to try and avoid JIT/GC influence times
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            // Time method
            timer.Start();
            method();
            timer.Stop();
        }

        static void TestA()
        {
            Point3d[] pointCloud = new Point3d[trimmedWidth * trimmedHeight];
            vertexColors = new Color[trimmedWidth * trimmedHeight];
            int arrayIndex = 0;
            for (int rows = 0; rows < trimmedHeight; rows++)
            {
                for (int columns = 0; columns < trimmedWidth; columns++)
                {
                    tempPoint.X = columns * -unitsMultiplier * depthPixelSize.x;
                    tempPoint.Y = rows * -unitsMultiplier * depthPixelSize.y;

                    depthPoint = averagedDepthFrameData[arrayIndex];
                    tempPoint.Z = (depthPoint - sensorElevation) * -unitsMultiplier;

                    pointCloud[arrayIndex] = tempPoint;

                    if (vertexColors.Length > 0) // Proxy for whether a mesh-coloring visualisation has been enabled
                        vertexColors[arrayIndex] = Analysis.AnalysisManager.GetPixelColor((int)depthPoint);

                    arrayIndex++;
                }
            }
        }

        static void TestB()
        {
            Point3d[] pointCloud = new Point3d[trimmedWidth * trimmedHeight];
            vertexColors = new Color[trimmedWidth * trimmedHeight];
            int arrayIndex = 0;
            for (int rows = 0; rows < trimmedHeight; rows++)
            {
                for (int columns = 0; columns < trimmedWidth; columns++)
                {
                    tempPoint.X = columns * -unitsMultiplier * depthPixelSize.x;
                    tempPoint.Y = rows * -unitsMultiplier * depthPixelSize.y;

                    depthPoint = averagedDepthFrameData[arrayIndex];
                    tempPoint.Z = (depthPoint - sensorElevation) * -unitsMultiplier;

                    pointCloud[arrayIndex] = tempPoint;

                    if (vertexColors.Length > 0) // Proxy for whether a mesh-coloring visualisation has been enabled
                        vertexColors[arrayIndex] = Analysis.AnalysisManager.GetPixelColor((int)depthPoint);

                    arrayIndex++;
                }
            }
        }
    }
}
