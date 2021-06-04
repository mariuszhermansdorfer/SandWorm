using System;
using System.Numerics;
using System.Collections.Generic;
using Microsoft.Azure.Kinect.Sensor;
using Rhino.Geometry;

namespace SandWorm
{

    static class KinectAzureController
    {
        // Shared across devices
        public static int depthHeight = 0;
        public static int depthWidth = 0;
        public static int colorHeight = 0;
        public static int colorWidth = 0;
        public static ushort[] depthFrameData = null;
        public static byte[] colorFrameData = null;

        // Kinect for Azure specific
        public static Device sensor;
        private static DeviceConfiguration deviceConfig;
        private static Calibration calibration;

        public const int depthWidthNear = 640; // Assuming low FPS mode
        public const int depthHeightNear = 576;

        public const int depthWidthWide = 1024; // Assuming low FPS mode
        public const int depthHeightWide = 1024;

        public static Vector3?[] undistortMatrix;
        public static Vector2[] idealXYCoordinates;
        public static double[] verticalTiltCorrectionMatrix;

        public const double sin6 = 0.10452846326;


        public static void SetupSensor(Core.KinectTypes fieldOfViewMode, double sensorElevation, ref string errorMessage)
        {
            if (sensor == null)
            {
                try
                {
                    sensor = Device.Open();
                    Initialize(fieldOfViewMode, sensorElevation);
                }
                catch (Exception exc)
                {
                    sensor?.Dispose();
                    errorMessage = exc.Message; // Returned to BaseKinectComponent
                }
            }
        }
        public static DepthMode GetDepthMode(Core.KinectTypes type)
        {
            switch (type)
            {
                case Core.KinectTypes.KinectAzureNear:
                    depthWidth = depthWidthNear;
                    depthHeight = depthHeightNear;
                    return DepthMode.NFOV_Unbinned;

                case Core.KinectTypes.KinectAzureWide:
                    depthWidth = depthWidthWide;
                    depthHeight = depthHeightWide;
                    return DepthMode.WFOV_Unbinned;

                default:
                    throw new ArgumentException("Invalid Kinect Type provided", "original"); ;
            }
        }

        private static void CreateCameraConfig(Core.KinectTypes fieldOfViewMode)
        {
            deviceConfig = new DeviceConfiguration
            {
                CameraFPS = FPS.FPS15,
                DepthMode = GetDepthMode(fieldOfViewMode),
                ColorResolution = ColorResolution.R1536p,
                SynchronizedImagesOnly = false // Color and depth images can be out of sync
            };

            if (fieldOfViewMode == Core.KinectTypes.KinectAzureNear) // We can have 30 FPS in the narrow field of view
                deviceConfig.CameraFPS = FPS.FPS30;
        }

        // Capture a single frame
        public static void CaptureFrame()
        {
            using (var capture = sensor.GetCapture())
            {
                if (capture.Depth != null)
                    depthFrameData = capture.Depth.GetPixels<ushort>().ToArray();
            }
        }


        public static void Initialize(Core.KinectTypes fieldOfViewMode, double sensorElevation)
        {
            string message;
            CreateCameraConfig(fieldOfViewMode); // Apply user options from Sandworm Options
            
            try
            {
                sensor.StopCameras();
            }
            catch (Exception ex)
            {
                message = ex.ToString();
            }

            try
            {
                sensor.StartCameras(deviceConfig);
                calibration = sensor.GetCalibration(deviceConfig.DepthMode, deviceConfig.ColorResolution);

                Vector2 depthPixel = new Vector2();
                Vector3? translationVector;

                // Lookup tables to correct for depth camera distortion in XY plane and its vertical tilt
                undistortMatrix = new Vector3?[depthHeight * depthWidth];
                verticalTiltCorrectionMatrix = new double[depthHeight * depthWidth];

                for (int y = 0, i = 0; y < depthHeight; y++)
                {
                    depthPixel.Y = (float)y;
                    for (int x = 0; x < depthWidth; x++, i++)
                    {
                        depthPixel.X = (float)x;
                        translationVector = calibration.TransformTo3D(depthPixel, 1f, CalibrationDeviceType.Depth, CalibrationDeviceType.Depth);
                        undistortMatrix[i] = translationVector;
                        
                        verticalTiltCorrectionMatrix[i] = translationVector.Value.Y * sin6;
                    }
                }

                // Create synthetic depth values emulating our sensor elevation and obtain corresponding idealized XY coordinates
                double syntheticDepthValue;
                idealXYCoordinates = new Vector2[depthWidth * depthHeight];
                for (int i = 0; i < depthWidth * depthHeight; i++)
                {
                    syntheticDepthValue = sensorElevation / (1 - verticalTiltCorrectionMatrix[i]);
                    idealXYCoordinates[i] = new Vector2((float)Math.Round(syntheticDepthValue * undistortMatrix[i].Value.X, 1), (float)Math.Round(syntheticDepthValue * undistortMatrix[i].Value.Y, 1));
                }
            }
            catch (Exception ex)
            {
                message = ex.ToString();
            }
        }
    }
}