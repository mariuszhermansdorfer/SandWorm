using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using Rhino.Geometry;

namespace SandWorm
{
    public static class Core
    {
        public static Mesh CreateQuadMesh(Mesh mesh, Point3f[] vertices, Color[] colors, int xStride, int yStride)
        {
            int xd = xStride;       // The x-dimension of the data
            int yd = yStride;       // They y-dimension of the data


            if (mesh.Faces.Count != (xStride - 2) * (yStride - 2))
            {
                mesh = new Mesh();
                mesh.Vertices.Capacity = vertices.Length;      // Don't resize array
                mesh.Vertices.UseDoublePrecisionVertices = false;
                mesh.Vertices.AddVertices(vertices);       

                for (int y = 1; y < yd - 1; y++)       // Iterate over y dimension
                {
                    for (int x = 1; x < xd - 1; x++)       // Iterate over x dimension
                    {
                        int i = y * xd + x;
                        int j = (y - 1) * xd + x;

                        mesh.Faces.AddFace(j - 1, j, i, i - 1);
                    }
                }
            }
            else
            {
                mesh.Vertices.UseDoublePrecisionVertices = false; 

                unsafe
                {
                    using (var meshAccess = mesh.GetUnsafeLock(true))
                    {
                        int arrayLength;
                        Point3f* points = meshAccess.VertexPoint3fArray(out arrayLength);
                        for (int i = 0; i < arrayLength; i++)
                        {
                            points->Z = vertices[i].Z;
                            points++;
                        }
                        mesh.ReleaseUnsafeLock(meshAccess);
                    }  
                }
            }

            if (colors.Length > 0) // Colors only provided if the mesh style permits
            {
                mesh.VertexColors.SetColors(colors);
            }

            return mesh;
        }

        public struct PixelSize // Unfortunately no nice tuples in this version of C# :(
        {
            public double x;
            public double y;
        }

        public static PixelSize GetDepthPixelSpacing(double sensorHeight)
        {
            double kinect2FOVForX = 70.6; 
            double kinect2FOVForY = 60.0;
            double kinect2ResolutionForX = 512;
            double kinect2ResolutionForY = 424;

            PixelSize pixelsForHeight = new PixelSize
            {
                x = GetDepthPixelSizeInDimension(kinect2FOVForX, kinect2ResolutionForX, sensorHeight),
                y = GetDepthPixelSizeInDimension(kinect2FOVForY, kinect2ResolutionForY, sensorHeight)
            };
            return pixelsForHeight;
        }

        private static double GetDepthPixelSizeInDimension(double fovAngle, double resolution, double height)
        {
            double fovInRadians = (Math.PI / 180) * fovAngle;
            double dimensionSpan = 2 * height * Math.Tan(fovInRadians / 2);
            return dimensionSpan / resolution;
        }

        public static void CopyAsIntArray(ushort[] source, int[] destination, int leftColumns, int rightColumns, int topRows, int bottomRows, int height, int width)
        {
            if (source == null)
            {
                return; // Triggers on initial setup
            }

            ref ushort ru0 = ref source[0];
            ref int ri0 = ref destination[0];
            int j = 0;

            for (int rows = topRows; rows < height - bottomRows; rows++)
            {
                for (int columns = rightColumns; columns < width - leftColumns; columns++)
                {
                    int i = rows * width + columns;
                    Unsafe.Add(ref ri0, j) = Unsafe.Add(ref ru0, i);
                    j++;
                }
            }
                
        }

        public static double ConvertDrawingUnits (Rhino.UnitSystem units)
        {
            double unitsMultiplier = 1.0;

            switch (units.ToString())
            {
                case "Kilometers":
                    unitsMultiplier = 0.0001;
                    break;

                case "Meters":
                    unitsMultiplier = 0.001;
                    break;

                case "Decimeters":
                    unitsMultiplier = 0.01;
                    break;

                case "Centimeters":
                    unitsMultiplier = 0.1;
                    break;

                case "Millimeters":
                    unitsMultiplier = 1.0;
                    break;

                case "Inches":
                    unitsMultiplier = 0.0393701;
                    break;

                case "Feet":
                    unitsMultiplier = 0.0328084;
                    break;
            }
            return unitsMultiplier;
        }

        public static void LogTiming(ref List<string>  output, Stopwatch timer, string eventDescription)
        {
            var logInfo = eventDescription + ": ";
            timer.Stop();
            output.Add(logInfo.PadRight(28, ' ') + timer.ElapsedMilliseconds.ToString() + " ms");
            timer.Restart();
        }
    }
}