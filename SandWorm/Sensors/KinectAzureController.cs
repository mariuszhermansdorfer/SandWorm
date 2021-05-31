using System;
using System.Numerics;
using System.Collections.Generic;
using Microsoft.Azure.Kinect.Sensor;
using Rhino.Geometry;

namespace SandWorm
{
    // Reference: https://github.com/bibigone/k4a.net/issues/15
    // Reference: https://github.com/windperson/K4aColorCameraDemo/blob/70816a7e9fd479a12da6b4470385b56516ebd106/K4aColorCameraDemo/MainWindow.xaml.cs

    static class KinectAzureController
    {
        // Shared in Kinect for Windows Controller
        public static int depthHeight = 576;
        public static int depthWidth = 640;
        public static int colorHeight = 0;
        public static int colorWidth = 0;
        public static ushort[] depthFrameData = null;
        public static int[] depthFrameInt = null;
        public static byte[] colorFrameData = null;
        private static double sensorElevation = 515;

        // Kinect for Azure specific
        public static Device sensor;

        public static double K4ANFOVForX = 75.0;
        public static double K4ANFOVForY = 65.0;
        public static int K4ANResolutionForX = 640; // Assuming low FPS mode
        public static int K4ANResolutionForY = 576;

        public static double K4AWFOVForX = 120.0;
        public static double K4AWFOVForY = 120.0;
        public static int K4AWResolutionForX = 1024; // Assuming low FPS mode
        public static int K4AWResolutionForY = 1024;

        private static DeviceConfiguration deviceConfig;
        private static Calibration calibration;

        public static Vector3?[] undistortMatrix;
        public static Vector2[] idealXYCoordinates;
        public static double[] verticalTiltCorrectionMatrix;
        private const double sin6 = 0.10452846326;
        


        public static void SetupSensor(Core.KinectTypes k4AConfig, ref string errorMessage)
        {
            if (sensor == null)
            {
                try
                {
                    sensor = Device.Open();
                    Initialize(k4AConfig, sensorElevation);
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
                case Core.KinectTypes.KinectForAzureNear:
                    return DepthMode.NFOV_Unbinned;
                case Core.KinectTypes.KinectForAzureWide:
                    return DepthMode.WFOV_Unbinned;
                default:
                    throw new ArgumentException("Invalid Kinect Type provided", "original"); ;
            }
        }

        private static void CreateCameraConfig(Core.KinectTypes k4AConfig)
        {
            deviceConfig = new DeviceConfiguration
            {
                CameraFPS = FPS.FPS30, // TODO: set this based on tick rate?
                DepthMode = GetDepthMode(k4AConfig),
                ColorResolution = ColorResolution.R1536p,
                SynchronizedImagesOnly = false // Color and depth images can be out of sync
            };
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


        public static void Initialize(Core.KinectTypes kinectAzureConfig, double sensorElevation)
        {
            string message;
            CreateCameraConfig(kinectAzureConfig); // Apply user options from Sandworm Options
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