using System;
using System.CodeDom;
using System.Linq;
using System.Threading;
using Microsoft.Azure.Kinect.Sensor;

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
                    return DepthMode.NFOV_Unbinned;
                case Core.KinectTypes.KinectForAzureWide:
                    return DepthMode.WFOV_Unbinned;
                default:
                    throw new System.ArgumentException("Invalid Kinect Type provided", "original"); ;
            }
        }

        private static void CreateCameraConfig(Core.KinectTypes k4AConfig)
        {
            deviceConfig = new DeviceConfiguration
            {
                CameraFPS = FPS.FPS15, // TODO: set this based on tick rate?
                DepthMode = GetDepthMode(k4AConfig),
                ColorResolution = ColorResolution.Off,
                SynchronizedImagesOnly = false // Color and depth images can be out of sync
            };
        }

        // Prototype function to try and capture a single frame
        private static void CaptureFrame()
        {
            //sensor.GetCalibration(deviceConfig.DepthMode, deviceConfig.ColorResolution, out calibration);

            if (sensor == null)
            {
                return; // Occurs during initial load?
            }

            var capture = sensor.GetCapture();
            if (capture != null)
            {
                using (capture)
                {
                    if (capture.Depth != null)
                    {


                        //using (var transformation = calibration.CreateTransformation())
                        //{
                        //    var depthImage = capture.Depth;
                        //    depthFrameData = transformation.DepthImageToColorCamera(depthImage).GetPixels<ushort>().ToArray();
                        //    depthHeight = depthImage.HeightPixels;
                        //    depthWidth = depthImage.WidthPixels;
                        //}

                        //WORKING SOL//
                        var depthImage = capture.Depth;
                        depthHeight = depthImage.HeightPixels; 
                        depthWidth = depthImage.WidthPixels;
                        depthFrameData = depthImage.GetPixels<ushort>().ToArray(); //Works
                        //END WORKING SOL//


                        //capture.Dispose();


                        //int sum = depthFrameData.Select(r => (int)r).Sum();  //this is updating and changing correctly which means data is coming through larger number means further obstacles
                        //var xyzImageBuffer = new short[depthImage.WidthPixels * depthImage.HeightPixels * 3];  //Short or UShort?
                        //var xyzImageStride = depthImage.WidthPixels * sizeof(short) * 3;


                        //using (var transformation = calibration.CreateTransformation())
                        //{
                        //var output = transformation.DepthImageToPointCloud(depthImage);
                        //var test = output;
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
            }
            catch (Exception ex)
            {
                message = ex.ToString();
            }

            CaptureFrame();
        }
    }
}