using System.Collections.Generic;
using Rhino.Geometry;

namespace SandWorm.Analytics
{
    public class WaterLevel : Analysis.MeshGeometryAnalysis
    {
        public WaterLevel() : base("Show Water Level")
        {
        }

        public override void GetGeometryForAnalysis(ref List<GeometryBase> outputGeometry, int waterLevel, int ci,
            Mesh mesh)
        {
            var bounds = mesh.GetBoundingBox(false);
            var waterPlane = new Plane(new Point3d(bounds.Max.X, bounds.Max.Y, waterLevel), new Vector3d(0, 0, 1));
            var waterSrf = new PlaneSurface(waterPlane,
                new Interval(bounds.Min.X, bounds.Max.X),
                new Interval(bounds.Min.Y, bounds.Max.Y)
            );
            outputGeometry.Add(waterSrf);
        }
    }
}