using System;
using System.Drawing;
using SandWorm;

namespace SandwormBenchmarks
{
    public class RunAllBenchmarks
    {
        public static void Main()
        {
            SandwormBenchmarks.RunColorBenchmarks.Benchmarks(); // Slope, Elevation etc
            // NOTE: below is disabled; see comment in MockMesh.cs
            // SandwormBenchmarks.RunAnalysisBenchmarks.Benchmarks(); // Contours, etc
            // Prevent console from auto exiting
            Console.Write("Note: Benchmark summaries saved to SandwormBenchmarks\\bin\\Release\\BenchmarkDotNet.Artifacts\\results.");
            Console.Write("Finished Benchmarks. Press any key to exit.");
            Console.ReadLine();
        }
    }
}
