using System;
using System.CodeDom;
using System.Linq;
using System.Threading;
//using Microsoft.Azure.Kinect.Sensor;
using K4AdotNet.Sensor;

namespace SandWorm
{
    // Reference: https://github.com/bibigone/k4a.net/issues/15
    // Reference: https://github.com/windperson/K4aColorCameraDemo/blob/70816a7e9fd479a12da6b4470385b56516ebd106/K4aColorCameraDemo/MainWindow.xaml.cs

    static class K4AController
    {
        // Shared in Kinect for Windows Controller
        public static int depthHeight = 0;
        public static int depthWidth = 0;
        public static int colorHeight = 0;
        public static int colorWidth = 0;
        public static ushort[] depthFrameData = null;
        public static byte[] colorFrameData = null;

        // Kinect for Azure specific
        public static Device sensor;

        // Kinect for Azure Details; see https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
        // Near FOV unbinned
        public static double K4ANFOVForX = 75.0;
        public static double K4ANFOVForY = 65.0;
        public static int K4ANResolutionForX = 640; // Assuming low FPS mode
        public static int K4ANResolutionForY = 576;

        // Wide FOV unbinned
        public static double K4AWFOVForX = 120.0;
        public static double K4AWFOVForY = 120.0;
        public static int K4AWResolutionForX = 1024; // Assuming low FPS mode
        public static int K4AWResolutionForY = 1024;

        private static DeviceConfiguration deviceConfig;
        private static Calibration calibration;

        public static void SetupSensor(Core.KinectTypes k4AConfig, ref string errorMessage)
        {
            if (sensor == null)
            {
                try
                {
                    sensor = Device.Open();
                    Initialize(k4AConfig);
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
                    return DepthMode.NarrowViewUnbinned;
                case Core.KinectTypes.KinectForAzureWide:
                    return DepthMode.WideViewUnbinned;
                default:
                    throw new System.ArgumentException("Invalid Kinect Type provided", "original"); ;
            }
        }

        private static void CreateCameraConfig(Core.KinectTypes k4AConfig)
        {
            deviceConfig = new DeviceConfiguration
            {
                
                CameraFps = FrameRate.Fifteen, // TODO: set this based on tick rate?
                DepthMode = GetDepthMode(k4AConfig),
                ColorResolution = ColorResolution.Off,
                SynchronizedImagesOnly = false // Color and depth images can be out of sync
            };
        }

        // Prototype function to try and capture a single frame
        private static void CaptureFrame()
        {
            

            if (sensor == null)
            {
                return; // Occurs during initial load?
            }

            var capture = sensor.GetCapture();
            if (capture != null)
            {
                using (capture)
                {
                    if (capture.DepthImage != null)
                    {



                        //WORKING SOL//
                        var depthImage = capture.DepthImage;
                        depthHeight = depthImage.HeightPixels; 
                        depthWidth = depthImage.WidthPixels;
                        //depthFrameData = depthImage.GetPixels<ushort>().ToArray(); //Works
                        //END WORKING SOL//


                        short[] xyzImageBuffer = new short[depthImage.WidthPixels * depthImage.HeightPixels * 3];
                        int xyzImageStride = depthImage.WidthPixels * sizeof(short) * 3;
                        using (var transformation = calibration.CreateTransformation())
                        {
                            using (var xyzImage = K4AdotNet.Sensor.Image.CreateFromArray(xyzImageBuffer, K4AdotNet.Sensor.ImageFormat.Custom, depthImage.WidthPixels, depthImage.HeightPixels, xyzImageStride))
                            {
                                transformation.DepthImageToPointCloud(depthImage, CalibrationGeometry.Depth, xyzImage);
                            }
                        }

                        short x3Dmillimeters = xyzImageBuffer[10000];
                        short y3Dmillimeters = xyzImageBuffer[1];
                        short z3Dmillimeters = xyzImageBuffer[2];
                    }
                }
            }
            else
            {
                Thread.Sleep(1);
            }
        }

        public static void UpdateFrame()
        {
            CaptureFrame(); //CaptureFrame Once each time public function is called
        }

        public static void Initialize(Core.KinectTypes k4AConfig)
        {
            string message;
            CreateCameraConfig(k4AConfig); // Apply the user options from Sandworm Options
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
                sensor.GetCalibration(deviceConfig.DepthMode, deviceConfig.ColorResolution, out calibration);
                //sensor.GetCalibration(DepthMode.)
            }
            catch (Exception ex)
            {
                message = ex.ToString();
            }

            CaptureFrame();
        }
    }
}