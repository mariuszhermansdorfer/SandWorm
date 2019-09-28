using System;
using System.Drawing;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Rhino.Geometry;
using SandWorm;

namespace SandwormBenchmarks
{
    public class RunColorBenchmarks : MockMesh
    {
        public static void Benchmarks()
        {
            BenchmarkRunner.Run<MeshElevationBenchmarks>();
            MeshElevationBenchmarks.TestCurrentElevationImplementation();
            MeshElevationBenchmarks.TestProposedElevationImplementation();

            BenchmarkRunner.Run<MeshSlopeBenchmarks>();
            MeshSlopeBenchmarks.TestCurrentSlopeImplementation();
            MeshSlopeBenchmarks.TestProposedSlopeImplementation();
        }
    }

    public class MeshElevationBenchmarks : MockMesh
    {
        static readonly SandWorm.Analytics.Elevation elevationAnalysis;
        static MeshElevationBenchmarks()
        {
            elevationAnalysis = SandWorm.Analysis.AnalysisManager.options[3] as SandWorm.Analytics.Elevation;
            elevationAnalysis.ComputeLookupTableForAnalysis(sensorElevation);
        }

        [Benchmark(Baseline = true)]
        public static void TestCurrentElevationImplementation()
        {
            var vertexColors = new Color[averagedDepthFrameData.Length];
            elevationAnalysis.GetColorCloudForAnalysis(ref vertexColors, averagedDepthFrameData, sensorElevation);
        }

        [Benchmark]
        public static void TestProposedElevationImplementation()
        {
            var vertexColors = new Color[averagedDepthFrameData.Length];
            elevationAnalysis.GetColorCloudForAnalysis(ref vertexColors, averagedDepthFrameData, sensorElevation);
        }
    }

    public class MeshSlopeBenchmarks : MockMesh
    {
        static readonly SandWorm.Analytics.Slope slopeAnalysis;
        static MeshSlopeBenchmarks()
        {
            slopeAnalysis = SandWorm.Analysis.AnalysisManager.options[4] as SandWorm.Analytics.Slope;
            slopeAnalysis.ComputeLookupTableForAnalysis(sensorElevation);
        }

        [Benchmark(Baseline = true)]
        public static void TestCurrentSlopeImplementation()
        {
            var vertexColors = slopeAnalysis.GetColorCloudForAnalysis(averagedDepthFrameData,
                trimmedWidth, trimmedHeight, depthPixelSize.x, depthPixelSize.y);
        }

        [Benchmark]
        public static void TestProposedSlopeImplementation()
        {
            var vertexColors = slopeAnalysis.GetColorCloudForAnalysis(averagedDepthFrameData, 
                trimmedWidth, trimmedHeight, depthPixelSize.x, depthPixelSize.y);
        }
    }
}
