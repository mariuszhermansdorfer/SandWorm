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
        public static void Main()
        {
            BenchmarkRunner.Run<MeshColorBenchmarks>();

            MeshColorBenchmarks.TestA();
            MeshColorBenchmarks.TestB();

            // Prevent console from auto exiting
            Console.Write("Finished Benchmarks. Press any key to exit.");
            Console.ReadLine();
        }
    }
    public class MeshColorBenchmarks : MockMesh
    {
        [Benchmark(Baseline = true)]
        public static void TestA()
        {
        }

        [Benchmark]
        public static void TestB()
        {
        }
    }
}
