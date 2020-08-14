﻿using System;
using System.Diagnostics;
using System.Threading;
using K4AdotNet.Sensor;
using K4AdotNet.Record;

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
            if (K4AController.depthFrameData == null)
                errorMessage = "No depth frame data provided by the Kinect Azure.";
        }



        //Check if needed to flatten array of Image (for workaround)
        static int[] To1DArray(int[,] input)
        {
            // Step 1: get total size of 2D array, and allocate 1D array.
            int size = input.Length;
            int[] result = new int[size];

            // Step 2: copy 2D array elements into a 1D array.
            int write = 0;
            for (int i = 0; i <= input.GetUpperBound(0); i++)
            {
                for (int z = 0; z <= input.GetUpperBound(1); z++)
                {
                    result[write++] = input[i, z];
                }
            }
            // Step 3: return the new array.
            return result;
        }

        //public event EventHandler<CaptureReadyEventArgs> CaptureReady;

        private static DeviceConfiguration CreateCameraConfig()
        {
            var config = new DeviceConfiguration
            {
                CameraFps = FrameRate.Fifteen,
                ColorResolution = ColorResolution.R720p,   //Off
                DepthMode = DepthMode.WideViewUnbinned,     //Passive IR, //WideViewUnbinned
                SynchronizedImagesOnly = false //false =  Color and depth images can be out of sync
            };
            return config;
        }


        // Prototype function to try and capture a single frame
        private static async void CaptureFrame() //private static async void 
        {
            sensor.GetCalibration(DepthMode.WideViewUnbinned, ColorResolution.Off, out calibration); //deviceConfig.DepthMode deviceConfig.ColorResolution,

            var res = sensor.TryGetCapture(out var capture);
            if (res)
            {
                using (capture)
                {
                    if (capture.DepthImage != null)
                    {

                        //CaptureReady?.Invoke(object sender, new CaptureReadyEventArgs(capture));
                        var depthImage = capture.DepthImage;
                        //var depthImageData = new ushort[depthImage.WidthPixels * depthImage.HeightPixels];
                        //var shorts = Array.ConvertAll(depthImage, b => new Converter<byte, ushort>(b));  //b => (ushort)b System.Convert.ToUInt16
                        //depthImage.CopyTo(depthImageData);

                        var xyzImageBuffer = new ushort[depthImage.WidthPixels * depthImage.HeightPixels * 3]; //CHANGED was shor, changed to ushort
                        var xyzImageStride = depthImage.WidthPixels * sizeof(short) * 3; //Can be also 0//correct
                        var xyzImage = Image.CreateFromArray(xyzImageBuffer, ImageFormat.Custom,
                                depthImage.WidthPixels,  //should this be *3 or does stride take care of that?
                                depthImage.HeightPixels, xyzImageStride);

                        using (var transformation = calibration.CreateTransformation())
                        {

                            using (xyzImage)
                            {
                                //for color space mapping use CalibrationGeometry.Color
                                transformation.DepthImageToPointCloud(depthImage, CalibrationGeometry.Depth, xyzImage); //BUG DepthImage must have 0 width but is shaped 1024 x 1024 so is xyzImage since its based of its height and with.
                                //https://github.com/sotanmochi/AzureKinect4Unity/blob/master/Assets/AzureKinect4Unity/Scripts/AzureKinectSensor.cs
                            }
                            
                            //depthImage.CopyFrameDataToArray(depthFrameData)
                        }
                        
                        //Update depthFrameData array....
                        depthFrameData = new ushort[depthImage.WidthPixels * depthImage.HeightPixels]; //Setup empty array as ushort, TODO imidiately assign every %3 element as the depthframe shaped as 1024x1024 
                        for (int y = 0; y < depthImage.HeightPixels; y++)
                        {
                            for (int x = 0; x < depthImage.WidthPixels; x++)
                            {
                                var indx = x * 3 + y * depthImage.WidthPixels * 3;
                                depthFrameData[indx] = xyzImageBuffer[indx + 2]; 
                            }
                        }
                        // How to access 3D coordinates of pixel with (x,y) 2D coordinates
                        //var x = 400;
                        //var y = 400;
                        //var indx = x * 3 + y * depthImage.WidthPixels * 3;
                        //var x3Dmillimeters = xyzImageBuffer[indx];
                        //var y3Dmillimeters = xyzImageBuffer[indx + 1];
                        //var z3Dmillimeters = xyzImageBuffer[indx + 2];


                    }
                    //if (capture.ColorImage != null)
                    //{

                        //var colorImage = capture.ColorImage;

                    //}
                }
                capture.Dispose(); //Must be called in the end to free the capture otherwise capture is null - CHECK THIS
            }
            else
            {
                if (!sensor.IsConnected)
                    throw new DeviceConnectionLostException(sensor.DeviceIndex);
                Thread.Sleep(1);
            }
        }

        public static void Initialize()
        {

            var deviceConfig = CreateCameraConfig();
            string message;
            if (!deviceConfig.IsValid(out message)) message += "Configuration Valid.";

            try
            {
                sensor.StartCameras(deviceConfig);
                
                //sensor = Device.GetDefault();
                // TODO: switch based on component type?
                //multiFrameReader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color);
                //multiFrameReader.MultiSourceFrameArrived += new EventHandler<MultiSourceFrameArrivedEventArgs>(KinectController.Reader_FrameArrived);

                //sensor.Open();
            }
            catch (Exception ex)
            {
                message = ex.ToString();
            }

            CaptureFrame(); //CaptureFrame Once on Initialize
        }


        public static void UpdateFrame()
        {
            CaptureFrame(); //CaptureFrame Once each time public function is called
        }
        
        //KANE - Mimics Kinect Controller Architecture - DOES NOTHING ATM

        public static Device Sensor
        {
            get
            {
                if (sensor == null)
                {
                    Initialize();
                }
                return sensor;
            }
            set
            {
                sensor = value;
            }
        }
    }
}