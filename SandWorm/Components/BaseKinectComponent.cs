using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Microsoft.Azure.Kinect;
using Microsoft.Kinect;
using Rhino;
using Rhino.Geometry;

namespace SandWorm.Components
{
    // Provides common functions across the components that read from the Kinect stream
    public abstract class BaseKinectComponent : BaseComponent
    {
        // Common Input Parameters
        protected int averageFrames = 1;
        protected int blurRadius = 1;
        protected int keepFrames = 1;
        // Sandworm Options
        protected SetupOptions options; // List of options coming from the SetupComponent
        protected int topRows = 0;
        protected int rightColumns = 0;
        protected int bottomRows = 0;
        protected int leftColumns = 0;
        protected double sensorElevation = 1000; // Arbitrary default value (must be >0)
        protected int tickRate = 33; // In ms
        protected Core.KinectTypes kinectType = Core.KinectTypes.KinectForWindows;
        private double[] elevationArray;
        // Derived
        protected Core.PixelSize depthPixelSize;
        protected static double unitsMultiplier;
        protected Point3f[] allPoints;
        protected int trimmedHeight;
        protected int trimmedWidth;
        protected readonly LinkedList<int[]> renderBuffer = new LinkedList<int[]>();
        protected int[] runningSum;

        public BaseKinectComponent(string name, string nickname, string description, string subCategory)
            : base(name, nickname, description, subCategory)
        {
        }

        protected void GetSandwormOptions(IGH_DataAccess DA, int optionsIndex, int framesIndex, int blurIndex)
        {
            // Loads standard options provided by the setup component
            options = new SetupOptions();
            DA.GetData<SetupOptions>(optionsIndex, ref options);

            if (options.SensorElevation != 0) sensorElevation = options.SensorElevation;
            if (options.LeftColumns != 0) leftColumns = options.LeftColumns;
            if (options.RightColumns != 0) rightColumns = options.RightColumns;
            if (options.TopRows != 0) topRows = options.TopRows;
            if (options.BottomRows != 0) bottomRows = options.BottomRows;
            if (options.TickRate != 0) tickRate = options.TickRate;
            if (options.KeepFrames != 0) keepFrames = options.KeepFrames;
            if (options.ElevationArray != null && options.ElevationArray.Length != 0) elevationArray = options.ElevationArray;
            else elevationArray = new double[0];
            if ((int)options.KinectType <= 2) kinectType = options.KinectType;

            // Pick the correct multiplier based on the drawing units. Shouldn't be a class variable; gets 'stuck'.
            unitsMultiplier = Core.ConvertDrawingUnits(RhinoDoc.ActiveDoc.ModelUnitSystem);
            sensorElevation /= unitsMultiplier; // Standardise to mm to match sensor units

            // Technically not provided by setup; but common to all Kinect-accessing components
            if (framesIndex > 0)
            {
                DA.GetData(framesIndex, ref averageFrames);
                // Make sure there is at least one frame in the render buffer
                averageFrames = averageFrames < 1 ? 1 : averageFrames;
            }
            if (blurIndex > 0)
                DA.GetData(blurIndex, ref blurRadius);

            depthPixelSize = Core.GetDepthPixelSpacing(sensorElevation);
        }

        protected void SetupKinect()
        {
            var errorMessage = "";
            if (kinectType == Core.KinectTypes.KinectForWindows)
                KinectController.SetupSensor(ref errorMessage);
            else
                K4AController.SetupSensor(ref errorMessage);

            if (errorMessage != "")
                ShowComponentError(errorMessage);

            Core.GetTrimmedDimensions(kinectType, ref trimmedWidth, ref trimmedHeight, ref elevationArray,
                                      topRows, bottomRows, leftColumns, rightColumns);
            if (runningSum == null || runningSum.Length < elevationArray.Length)
                runningSum = Enumerable.Range(1, elevationArray.Length).Select(i => new int()).ToArray();
        }

        protected void SetupRenderBuffer(int[] depthFrameDataInt, Mesh quadMesh)
        {
            var active_Height = 0;
            var active_Width = 0;
            ushort[] depthFrameData;
            if (kinectType == Core.KinectTypes.KinectForWindows)
            { 
                depthFrameData = KinectController.depthFrameData;
                active_Height = KinectController.depthHeight;
                active_Width = KinectController.depthWidth;
            }
            else
            { 
                K4AController.UpdateFrame();
                depthFrameData = K4AController.depthFrameData;
                active_Height = K4AController.depthHeight;
                active_Width = K4AController.depthWidth;
            }

            // Trim the depth array and cast ushort values to int //BUG Attempted to write protected data
            Core.CopyAsIntArray(depthFrameData, depthFrameDataInt,
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

        protected void AverageAndBlurPixels(int[] depthFrameDataInt, ref double[] averagedDepthFrameData)
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
                if (elevationArray.Length > 0) averagedDepthFrameData[pixel] -= elevationArray[pixel]; // Correct for Kinect's inacurracies using input from the calibration component

                if (renderBuffer.Count >= averageFrames)
                    runningSum[pixel] -= renderBuffer.First.Value[pixel]; // Subtract the oldest value from the sum 
            }

            Core.LogTiming(ref output, timer, "Frames averaging"); // Debug Info

            if (blurRadius > 1) // Apply gaussian blur
            {
                var gaussianBlurProcessor = new GaussianBlurProcessor(blurRadius, trimmedWidth, trimmedHeight);
                gaussianBlurProcessor.Apply(averagedDepthFrameData);
                Core.LogTiming(ref output, timer, "Gaussian blurring"); // Debug Info
            }
        }

        protected void GeneratePointCloud(double[] averagedDepthFrameData)
        {
            double depthPoint; 
            // Setup variables for per-pixel loop
            allPoints = new Point3f[trimmedWidth * trimmedHeight];
            var tempPoint = new Point3f();
            var arrayIndex = 0;
            for (var rows = 0; rows < trimmedHeight; rows++)
            for (var columns = 0; columns < trimmedWidth; columns++)
            {
                depthPoint = averagedDepthFrameData[arrayIndex];
                tempPoint.X = (float)(columns * -unitsMultiplier * depthPixelSize.x);
                tempPoint.Y = (float)(rows * -unitsMultiplier * depthPixelSize.y);
                tempPoint.Z = (float)((depthPoint - sensorElevation) * -unitsMultiplier);
                allPoints[arrayIndex] = tempPoint; // Add new point to point cloud itself
                arrayIndex++;
            }

            // Keep only the desired amount of frames in the buffer
            while (renderBuffer.Count >= averageFrames) renderBuffer.RemoveFirst();

            Core.LogTiming(ref output, timer, "Point cloud generation"); // Debug Info
        }

        protected void ShowComponentError(string errorMessage)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, errorMessage);
            ScheduleSolve(); // Ensure a future solve is scheduled despite an early return to SolveInstance()
        }

        protected void ScheduleDelegate(GH_Document doc)
        {
            ExpireSolution(false);
        }

        protected void ScheduleSolve()
        {
            if (tickRate > 0) // Allow users to force manual recalculation
                OnPingDocument().ScheduleSolution(tickRate, ScheduleDelegate);
        }
    }
}