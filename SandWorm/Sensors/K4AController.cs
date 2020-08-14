using System;
using System.Linq;
using System.Threading;
using Microsoft.Azure.Kinect.Sensor;

namespace SandWorm
{
    // Reference: https://github.com/bibigone/k4a.net/issues/15
    // Reference: https://github.com/windperson/K4aColorCameraDemo/blob/70816a7e9fd479a12da6b4470385b56516ebd106/K4aColorCameraDemo/MainWindow.xaml.cs

    internal static class K4AController
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

        public static void SetupSensor(ref string errorMessage)
        {
            if (sensor == null)
                try
                {
                    sensor = Device.Open();
                    Initialize();
                }
                catch (Exception exc)
                {
                    sensor?.Dispose();
                    errorMessage = exc.Message; // Returned to BaseKinectComponent
                }
        }

        private static DeviceConfiguration CreateCameraConfig()
        {
            var config = new DeviceConfiguration
            {
                CameraFPS = FPS.FPS15,
                ColorResolution = ColorResolution.Off,
                DepthMode = DepthMode.WFOV_Unbinned,
                SynchronizedImagesOnly = false // Color and depth images can be out of sync
            };
            return config;
        }

        // Prototype function to try and capture a single frame
        private static async void CaptureFrame()
        {
            //sensor.GetCalibration(deviceConfig.DepthMode, deviceConfig.ColorResolution, out calibration);

            var capture = sensor.GetCapture();
            if (capture != null)
            {
                using (capture)
                {
                    if (capture.Depth != null)
                    {
                        var depthImage = capture.Depth;
                        depthHeight = depthImage.HeightPixels;
                        depthWidth = depthImage.WidthPixels;
                        depthFrameData = depthImage.GetPixels<ushort>().ToArray();
                        //int sum = depthFrameData.Select(r => (int)r).Sum();  //this is updating and changing correctly which means data is coming through larger number means further obstacles
                        //var xyzImageBuffer = new short[depthImage.WidthPixels * depthImage.HeightPixels * 3];  //Short or UShort?
                        //var xyzImageStride = depthImage.WidthPixels * sizeof(short) * 3;
                        //using (var transformation = calibration.CreateTransformation())
                        //{
                        //    var output = transformation.DepthImageToPointCloud(depthImage);
                        //    var test = output;
                        //}

                        // How to access 3D coordinates of pixel with (x,y) 2D coordinates
                        //var x = 400;
                        //var y = 400;
                        //var indx = x * 3 + y * depthImage.WidthPixels * 3;
                        //var x3Dmillimeters = xyzImageBuffer[indx];
                        //var y3Dmillimeters = xyzImageBuffer[indx + 1];
                        //var z3Dmillimeters = xyzImageBuffer[indx + 2];
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

        public static void Initialize()
        {
            var deviceConfig = CreateCameraConfig();
            string message;

            try
            {
                sensor.StartCameras(deviceConfig);
            }
            catch (Exception ex)
            {
                message = ex.ToString();
            }

            CaptureFrame();
        }
    }
}