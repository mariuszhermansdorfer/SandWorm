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
            trimmedWidth = Core.GetDepthPixelXResolution(kinectType) - leftColumns - rightColumns;
            trimmedHeight = Core.GetDepthPixelYResolution(kinectType) - topRows - bottomRows;
            // Only create a new elevation array when user resizes the mesh
            if (elevationArray == null || elevationArray.Length != trimmedWidth * trimmedHeight)
                elevationArray = new double[trimmedWidth * trimmedHeight];
        }

        public static int GetDepthPixelXResolution(KinectTypes type)
        {
            switch (type)
            {
                case KinectTypes.KinectForWindows:
                    return KinectController.kinect2ResolutionForX;
                case KinectTypes.KinectForAzureNear:
                    return K4AController.K4ANResolutionForX;
                case KinectTypes.KinectForAzureWide:
                    return K4AController.K4AWResolutionForX;
                default:
                    throw new System.ArgumentException("Invalid Kinect Type", "original"); ;
            }
        }

        public static int GetDepthPixelYResolution(KinectTypes type)
        {
            switch (type)
            {
                case KinectTypes.KinectForWindows:
                    return KinectController.kinect2ResolutionForY;
                case KinectTypes.KinectForAzureNear:
                    return K4AController.K4ANResolutionForY;
                case KinectTypes.KinectForAzureWide:
                    return K4AController.K4AWResolutionForY;
                default:
                    throw new System.ArgumentException("Invalid Kinect Type", "original"); ;
            }
        }

        public static double GetFOVForY(KinectTypes type)
        {
            switch (type)
            {
                case KinectTypes.KinectForWindows:
                    return KinectController.kinect2FOVForY;
                case KinectTypes.KinectForAzureNear:
                    return K4AController.K4ANFOVForY;
                case KinectTypes.KinectForAzureWide:
                    return K4AController.K4AWFOVForY;
                default:
                    throw new System.ArgumentException("Invalid Kinect Type", "original"); ;
            }
        }

        public static double GetFOVForX(KinectTypes type)
        {
            switch (type)
            {
                case KinectTypes.KinectForWindows:
                    return KinectController.kinect2FOVForX;
                case KinectTypes.KinectForAzureNear:
                    return K4AController.K4ANFOVForX;
                case KinectTypes.KinectForAzureWide:
                    return K4AController.K4AWFOVForX;
                default:
                    throw new System.ArgumentException("Invalid Kinect Type", "original"); ;
            }
        }

        public static PixelSize GetDepthPixelSpacing(double sensorHeight, KinectTypes kinectType)  //TODO rereference for 
        {
            PixelSize pixelsForHeight = new PixelSize
            {
                x = GetDepthPixelSizeInDimension(GetFOVForX(kinectType), GetDepthPixelXResolution(kinectType), sensorHeight), //KinectController.kinect2FOVForX, KinectController.kinect2ResolutionForX
                y = GetDepthPixelSizeInDimension(GetFOVForY(kinectType), GetDepthPixelYResolution(kinectType), sensorHeight) //KinectController.kinect2FOVForY, KinectController.kinect2ResolutionForY
            };
            return pixelsForHeight;
        }

        private static double GetDepthPixelSizeInDimension(double fovAngle, double resolution, double height)
        {
            double fovInRadians = (Math.PI / 180) * fovAngle;
            double dimensionSpan = 2 * height * Math.Tan(fovInRadians / 2);
            return dimensionSpan / resolution;
        }

        public static void CopyAsIntArray(ushort[] source, int[] destination, int leftColumns, int rightColumns, int topRows, int bottomRows, int height, int width) //Takes the feed and trims and casts from ushort m to int
        {
            if (source == null)
            {
                return; // Triggers on initial setup
            }

            int j = 0;
            //for (int rows = topRows; rows < height - bottomRows; rows++)
            //{
            //    for (int columns = rightColumns; columns < width - leftColumns; columns++)
            //    {
            //        int i = rows * width + columns;
            //       destination[j] = (int)source[i];
            //       j++;
            //    }
            //}

            ref ushort ru0 = ref source[0];
            ref int ri0 = ref destination[0];
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