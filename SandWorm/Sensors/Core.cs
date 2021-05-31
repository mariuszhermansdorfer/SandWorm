using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
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

        public enum KinectTypes
        {
            KinectForWindows,
            KinectForAzureNear,
            KinectForAzureWide,
        }

        public struct PixelSize // Unfortunately no nice tuples in this version of C# :(
        {
            public double x;
            public double y;
        }

        public static void GetTrimmedDimensions(KinectTypes kinectType, ref int trimmedWidth, ref int trimmedHeight, ref double[] elevationArray,
                                                int topRows, int bottomRows, int leftColumns, int rightColumns)
        {
            int _x;
            int _y;
            switch (kinectType)
            {
                case KinectTypes.KinectForWindows:
                    _x = KinectController.kinect2ResolutionForX;
                    _y = KinectController.kinect2ResolutionForY;
                    break;
                case KinectTypes.KinectForAzureNear:
                    _x = KinectAzureController.K4ANResolutionForX;
                    _y = KinectAzureController.K4ANResolutionForY;
                    break;
                case KinectTypes.KinectForAzureWide:
                    _x = KinectAzureController.K4AWResolutionForX;
                    _y = KinectAzureController.K4AWResolutionForY;
                    break;
                default:
                    throw new System.ArgumentException("Invalid Kinect Type", "original"); ;
            }

            trimmedWidth = _x - leftColumns - rightColumns;
            trimmedHeight = _y - topRows - bottomRows;
            // Only create a new elevation array when user resizes the mesh
            if (elevationArray == null || elevationArray.Length != trimmedWidth * trimmedHeight)
                elevationArray = new double[trimmedWidth * trimmedHeight];
        }


        public static PixelSize GetDepthPixelSpacing(double sensorHeight)  //TODO rereference for 
        {
            PixelSize pixelsForHeight = new PixelSize
            {
                x = GetDepthPixelSizeInDimension(KinectController.kinect2FOVForX, KinectController.kinect2ResolutionForX, sensorHeight),
                y = GetDepthPixelSizeInDimension(KinectController.kinect2FOVForY, KinectController.kinect2ResolutionForX, sensorHeight)
            };
            return pixelsForHeight;
        }

        private static double GetDepthPixelSizeInDimension(double fovAngle, double resolution, double height)
        {
            double fovInRadians = (Math.PI / 180) * fovAngle;
            double dimensionSpan = 2 * height * Math.Tan(fovInRadians / 2);
            return dimensionSpan / resolution;
        }

        public static void CopyAsIntArray(ushort[] source, int[] destination, Vector2[] sourceXY, Vector2[] destinationXY, int leftColumns, int rightColumns, int topRows, int bottomRows, int height, int width) //Takes the feed and trims and casts from ushort m to int
        {
            if (source == null)
            {
                return; // Triggers on initial setup
            }

            int j = 0;


            ref ushort ru0 = ref source[0];
            ref int ri0 = ref destination[0];

            ref Vector2 rv0 = ref sourceXY[0];
            ref Vector2 rd0 = ref destinationXY[0];

            for (int rows = topRows; rows < height - bottomRows; rows++)
            {
                for (int columns = rightColumns; columns < width - leftColumns; columns++)
                {
                    int i = rows * width + columns;
                    Unsafe.Add(ref ri0, j) = Unsafe.Add(ref ru0, i); // Depth array
                    
                    if (sourceXY != null)
                        Unsafe.Add(ref rd0, j) = Unsafe.Add(ref rv0, i); //XY lookup table

                    j++;
                }
            }
        }


        public static double ConvertDrawingUnits(Rhino.UnitSystem units)
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

        public static void LogTiming(ref List<string> output, Stopwatch timer, string eventDescription)
        {
            var logInfo = eventDescription + ": ";
            timer.Stop();
            output.Add(logInfo.PadRight(28, ' ') + timer.ElapsedMilliseconds.ToString() + " ms");
            timer.Restart();
        }
    }
}