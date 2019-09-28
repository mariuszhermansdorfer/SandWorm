using System;
using System.Collections.Generic;
using System.Drawing;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Rhino.Geometry;
using SandWorm;

namespace SandwormBenchmarks
{
    public class RunAnalysisBenchmarks: MockMesh
    {
        public static void Benchmarks()
        {
            BenchmarkRunner.Run<MeshContourBenchmarks>();
            MeshContourBenchmarks.TestCurrentContouringImplementation();
            MeshContourBenchmarks.TestProposedContouringImplementation();
        }
    }
    public class MeshContourBenchmarks : MockMesh
    {
        static readonly int contourInterval = 50;
        static readonly SandWorm.Analytics.Contours contourAnalysis;
        static MeshContourBenchmarks()
        {
            contourAnalysis = SandWorm.Analysis.AnalysisManager.options[1] as SandWorm.Analytics.Contours;
        }

        [Benchmark(Baseline = true)]
        public static void TestCurrentContouringImplementation()
        {
            var outputGeometry = new List<Rhino.Geometry.GeometryBase>();
            contourAnalysis.GetGeometryForAnalysis(ref outputGeometry, contourInterval, quadMesh);
        }

        [Benchmark]
        public static void TestProposedContouringImplementation()
        {
            var outputGeometry = new List<Rhino.Geometry.GeometryBase>();
            contourAnalysis.GetGeometryForAnalysis(ref outputGeometry, contourInterval, quadMesh);
        }
    }
}
