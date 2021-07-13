using System.Collections.Generic;
using Rhino.Geometry;

namespace SandWorm.Analytics
{
    public class Contours : Analysis.MeshGeometryAnalysis
    {
        public Contours() : base("Show Contour Lines")
        {
        }

        public override void GetGeometryForAnalysis(ref List<GeometryBase> outputGeometry, double contourInterval, Mesh mesh)
        {
            var bounds = mesh.GetBoundingBox(false);

            var zVector = new Vector3d(0, 0, 1); // Reference vector

            // For relative planes starting from Bounds.Min,
            // this can also be absolute and fixed relative to the sensor distance.
            List<Rhino.Geometry.Plane> planes = new List<Rhino.Geometry.Plane>(); 

            for (var i = 0; i * contourInterval <= bounds.Max.Z; i++)
            {
                var currentPoint = new Point3d(0, 0, bounds.Min.Z + i * contourInterval);
                var currentPlane = new Plane(currentPoint, zVector);
                planes.Add(currentPlane);
            }

            var tempContours = Rhino.Geometry.Intersect.Intersection.MeshPlane(mesh, planes);
            for (var i = 0; i < tempContours.Length; i++) // Convert all polylines to Polyline Curves.
            {
                outputGeometry.Add(tempContours[i].ToPolylineCurve());
            }

        }
    }
}