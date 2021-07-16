using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Rhino.Geometry;
using SandWorm.Analytics;

namespace SandWorm
{
    public static class Core
    {
        public static Mesh CreateQuadMesh(Mesh mesh, Point3d[] vertices, Color[] colors, int xStride, int yStride)
        {
            int xd = xStride;       // The x-dimension of the data
            int yd = yStride;       // They y-dimension of the data

            if (mesh == null || mesh.Faces.Count != (xStride - 2) * (yStride - 2))
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
                mesh.Vertices.UseDoublePrecisionVertices = true;

                unsafe
                {
                    using (var meshAccess = mesh.GetUnsafeLock(true))
                    {
                        int arrayLength;
                        Point3d* points = meshAccess.VertexPoint3dArray(out arrayLength);
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
                mesh.VertexColors.SetColors(colors);
            else
                mesh.VertexColors.Clear();

            return mesh;
        }


        public static void GetTrimmedDimensions(Structs.KinectTypes kinectType, ref int trimmedWidth, ref int trimmedHeight, ref double[] elevationArray, int[] runningSum,
                                                double topRows, double bottomRows, double leftColumns, double rightColumns)
        {
            int _x;
            int _y;
            switch (kinectType)
            {
                case Structs.KinectTypes.KinectForWindows:
                    _x = KinectForWindows.depthWidth;
                    _y = KinectForWindows.depthHeight;
                    break;
                case Structs.KinectTypes.KinectAzureNear:
                    _x = KinectAzureController.depthWidthNear;
                    _y = KinectAzureController.depthHeightNear;
                    break;
                case Structs.KinectTypes.KinectAzureWide:
                    _x = KinectAzureController.depthWidthWide;
                    _y = KinectAzureController.depthHeightWide;
                    break;
                default:
                    throw new System.ArgumentException("Invalid Kinect Type", "original"); ;
            }

            runningSum = Enumerable.Range(1, _x * _y).Select(i => new int()).ToArray();

            trimmedWidth = (int)(_x - leftColumns - rightColumns);
            trimmedHeight = (int)(_y - topRows - bottomRows);
            // Only create a new elevation array when user resizes the mesh
            if (elevationArray == null || elevationArray.Length != trimmedWidth * trimmedHeight)
                elevationArray = new double[trimmedWidth * trimmedHeight];
        }

       
        public static void TrimXYLookupTable(Vector2[] sourceXY, Vector2[] destinationXY, double[] verticalTiltCorrectionLookupTable, 
            double leftColumns, double rightColumns, double topRows, double bottomRows, int height, int width, double unitsMultiplier) //Takes the feed and trims and casts from ushort m to int
        {
            ref Vector2 rv0 = ref sourceXY[0];
            ref Vector2 rd0 = ref destinationXY[0];
            float _units = (float)unitsMultiplier;

            for (int rows = (int)topRows, j = 0; rows < height - bottomRows; rows++)
            {
                for (int columns = (int)rightColumns; columns < width - leftColumns; columns++, j++)
                {
                    int i = rows * width + columns;
                    Unsafe.Add(ref rd0, j).X = Unsafe.Add(ref rv0, i).X * -_units;
                    Unsafe.Add(ref rd0, j).Y = Unsafe.Add(ref rv0, i).Y * _units;
                    verticalTiltCorrectionLookupTable[j] = sourceXY[i].Y * KinectAzureController.sin6;
                }
            }
        }


        public static void SetupRenderBuffer(int[] depthFrameDataInt, Structs.KinectTypes kinectType,
            double leftColumns, double rightColumns, double topRows, double bottomRows,
            Mesh quadMesh, int trimmedWidth, int trimmedHeight, double averageFrames, int[] runningSum, LinkedList<int[]> renderBuffer)
        {
            var active_Height = 0;
            var active_Width = 0;
            ushort[] depthFrameData;
            Vector2[] idealXYCoordinates = null;

            if (kinectType == Structs.KinectTypes.KinectForWindows)
            {
                depthFrameData = KinectForWindows.depthFrameData;
                active_Height = KinectForWindows.depthHeight;
                active_Width = KinectForWindows.depthWidth;
            }
            else
            {
                KinectAzureController.CaptureFrame();
                depthFrameData = KinectAzureController.depthFrameData;
                active_Height = KinectAzureController.depthHeight;
                active_Width = KinectAzureController.depthWidth;
                idealXYCoordinates = KinectAzureController.idealXYCoordinates;
            }

            // Trim the depth array and cast ushort values to int 
            CopyAsIntArray(depthFrameData, depthFrameDataInt,
                leftColumns, rightColumns, topRows, bottomRows,
                active_Height, active_Width);

            // Reset everything when resizing Kinect's field of view or changing the amounts of frame to average across
            if (renderBuffer.Count > averageFrames || (quadMesh != null && quadMesh.Faces.Count != (trimmedWidth - 2) * (trimmedHeight - 2)))
            {
                renderBuffer.Clear();
                Array.Clear(runningSum, 0, runningSum.Length);
                renderBuffer.AddLast(depthFrameDataInt);
            }
            else
            {
                renderBuffer.AddLast(depthFrameDataInt);
            }

        }

        public static void AverageAndBlurPixels(int[] depthFrameDataInt, ref double[] averagedDepthFrameData, int[] runningSum, LinkedList<int[]> renderBuffer,
            double sensorElevation, double[] elevationArray, double averageFrames,
            double blurRadius, int trimmedWidth, int trimmedHeight)
        {
            // Average across multiple frames
            for (var pixel = 0; pixel < depthFrameDataInt.Length; pixel++)
            {
                if (depthFrameDataInt[pixel] > 200) // We have a valid pixel. 
                {
                    runningSum[pixel] += depthFrameDataInt[pixel];
                }
                else
                {
                    if (pixel > 0) // Pixel is invalid and we have a neighbor to steal information from
                    {
                        //D1 Method
                        runningSum[pixel] += depthFrameDataInt[pixel - 1];

                        // Replace the zero value from the depth array with the one from the neighboring pixel
                        renderBuffer.Last.Value[pixel] = depthFrameDataInt[pixel - 1];
                    }
                    else // Pixel is invalid and it is the first one in the list. (No neighbor on the left hand side, so we set it to the lowest point on the table)
                    {
                        runningSum[pixel] += (int)sensorElevation;
                        renderBuffer.Last.Value[pixel] = (int)sensorElevation;
                    }
                }

                averagedDepthFrameData[pixel] = runningSum[pixel] / renderBuffer.Count; // Calculate average values
                
                if (elevationArray.Length > 0) 
                    averagedDepthFrameData[pixel] -= elevationArray[pixel]; // Correct for Kinect's inacurracies using input from the calibration component

                if (renderBuffer.Count >= averageFrames)
                    runningSum[pixel] -= renderBuffer.First.Value[pixel]; // Subtract the oldest value from the sum 
            }


            if (blurRadius > 1) // Apply gaussian blur
            {
                var gaussianBlurProcessor = new GaussianBlurProcessor((int)blurRadius, trimmedWidth, trimmedHeight);
                gaussianBlurProcessor.Apply(averagedDepthFrameData);
            }
        }

        public static void GeneratePointCloud(double[] averagedDepthFrameData, Vector2[] trimmedXYLookupTable, double[] verticalTiltCorrectionLookupTable, 
            Point3d[] allPoints, LinkedList<int[]> renderBuffer, int trimmedWidth, int trimmedHeight, double sensorElevation, double unitsMultiplier, double averageFrames)
        {

            // Setup variables for per-pixel loop
            
            Point3d tempPoint = new Point3d();
            double correctedElevation = 0.0;
            for (int rows = 0, i = 0; rows < trimmedHeight; rows++)
                for (int columns = 0; columns < trimmedWidth; columns++, i++)
                {
                    //TODO add lookup table for Kinect for Windows as well
                    //tempPoint.X = (float)(columns * -unitsMultiplier * depthPixelSize.X); // Flip direction of the X axis
                    //tempPoint.Y = (float)(rows * unitsMultiplier * depthPixelSize.Y);

                    tempPoint.X = trimmedXYLookupTable[i].X;
                    tempPoint.Y = trimmedXYLookupTable[i].Y;

                    correctedElevation = averagedDepthFrameData[i] - verticalTiltCorrectionLookupTable[i];
                    tempPoint.Z = (correctedElevation - sensorElevation) * -unitsMultiplier;
                    averagedDepthFrameData[i] = correctedElevation;

                    allPoints[i] = tempPoint; // Add new point to point cloud
                }

            // Keep only the desired amount of frames in the buffer
            while (renderBuffer.Count >= averageFrames) renderBuffer.RemoveFirst();
        }
        
        public static void GenerateMeshColors(ref Color[] vertexColors, int analysisType, double[] averagedDepthFrameData, 
            Vector2 depthPixelSize, double gradientRange,
            double sensorElevation, int trimmedWidth, int trimmedHeight)
        {
            switch (analysisType)
            {
                case 0: // None
                    vertexColors = new None().GetColorCloudForAnalysis();
                    break;

                case 1: // TODO: RGB
                    break;

                case 2: // Elevation
                    vertexColors = new Elevation().GetColorCloudForAnalysis(averagedDepthFrameData, sensorElevation, gradientRange);
                    break;

                case 3: // Slope
                    vertexColors = new Slope().GetColorCloudForAnalysis(averagedDepthFrameData,
                        trimmedWidth, trimmedHeight, depthPixelSize.X, depthPixelSize.Y, gradientRange);
                    break;

                case 4: // Aspect
                    vertexColors = new Aspect().GetColorCloudForAnalysis(averagedDepthFrameData,
                        trimmedWidth, trimmedHeight, gradientRange);
                    break;

                case 5: // TODO: Cut & Fill
                    break;
            }
        }

        public static void CopyAsIntArray(ushort[] source, int[] destination, double leftColumns, double rightColumns, double topRows, double bottomRows, int height, int width) //Takes the feed and trims and casts from ushort to int
        {
            if (source == null)
                return; // Triggers on initial setup

            ref ushort ru0 = ref source[0];
            ref int ri0 = ref destination[0];

            for (int rows = (int)topRows, j = 0; rows < height - bottomRows; rows++)
            {
                for (int columns = (int)rightColumns; columns < width - leftColumns; columns++, j++)
                {
                    int i = rows * width + columns;
                    Unsafe.Add(ref ri0, j) = Unsafe.Add(ref ru0, i);
                }
            }
        }

    }
}