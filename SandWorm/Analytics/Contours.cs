using System.Collections.Generic;
using Rhino.Geometry;

namespace SandWorm.Analytics
{
    public class Contours : Analysis.MeshGeometryAnalysis
    {
        public Contours() : base("Show Contour Lines")
        {
        }

        public override void GetGeometryForAnalysis(ref List<GeometryBase> outputGeometry, int wl,
            int contourInterval, Mesh mesh)
        {
            var bounds = mesh.GetBoundingBox(false);
            var originStart = new Point3d(0, 0, bounds.Min.Z);
            var originEnd = new Point3d(0, 0, bounds.Max.Z);
            var contours = Mesh.CreateContourCurves(mesh, originStart, originEnd, contourInterval);
            outputGeometry.AddRange(contours);
        }
    }
}