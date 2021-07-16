using System.Collections.Generic;
using Rhino.Geometry;

namespace SandWorm.Analytics
{
    public static class WaterLevel
    {
        public static void GetGeometryForAnalysis(ref List<GeometryBase> outputGeometry, double waterLevel, Point3d[] pointArray, int trimmedWidth)
        {
            List<Point3d> waterCorners = new List<Point3d>();

            waterCorners.Add(pointArray[0]);
            waterCorners.Add(pointArray[0 + trimmedWidth - 1]);
            waterCorners.Add(pointArray[pointArray.Length - 1]);
            waterCorners.Add(pointArray[pointArray.Length - trimmedWidth + 1]);
            waterCorners.Add(pointArray[0]); // Close polyline

            for (int i = 0; i < waterCorners.Count; i++)
                waterCorners[i] = new Point3d(waterCorners[i].X, waterCorners[i].Y, waterLevel);

            Polyline waterBoundary = new Polyline(waterCorners);
            Mesh waterPlane = Mesh.CreateFromClosedPolyline(waterBoundary);
            outputGeometry.Add(waterPlane);
        }
    }
}