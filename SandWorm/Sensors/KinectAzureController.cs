using System;
using K4AdotNet.Sensor;

namespace SandWorm
{
    // Reference: https://github.com/bibigone/k4a.net/issues/15
    // Reference: https://github.com/windperson/K4aColorCameraDemo/blob/70816a7e9fd479a12da6b4470385b56516ebd106/K4aColorCameraDemo/MainWindow.xaml.cs

    static class KinectAzureController
    {
        // Shared in Kinect for Windows Controller
        public static int depthHeight = 0;
        public static int depthWidth = 0;
        public static int colorHeight = 0;
        public static int colorWidth = 0;
        public static ushort[] depthFrameData = null;
        public static int[] depthFrameInt = null;
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
        private static Transformation _transformation;
        private static Calibration calibration;
        private static int _colourWidth;
        private static int _colourHeight;
        //public static System.Numerics.Vector3?[] translationMatrix;
        public static K4AdotNet.Float3?[] translationMatrix;
        public static short[] depthFrameShort;
        public static short[] xyzImageBuffer;
        public static System.Collections.Generic.List<Rhino.Geometry.Point3d> points;
        public static double[] verticalTiltCorrectionMatrix;
        private const double sin6 = 0.10452846326;

        private static double sensorElevation = 515;

        public static Image test;

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
                ColorResolution = ColorResolution.R1536p,
                SynchronizedImagesOnly = false // Color and depth images can be out of sync
            };
        }

        // Prototype function to try and capture a single frame
        public static void CaptureFrame()
        {
            using (var capture = sensor.GetCapture())
            {
                if (capture.DepthImage != null)
                {

                    var depthImage = capture.DepthImage;

                    depthFrameShort = new short[depthImage.WidthPixels * depthImage.HeightPixels];
                    var depthFrameDouble = new double[depthImage.WidthPixels * depthImage.HeightPixels];

                    Rhino.Geometry.Point3d pt = new Rhino.Geometry.Point3d();
                    points = new System.Collections.Generic.List<Rhino.Geometry.Point3d>();

                    //depthImage.CopyTo(depthFrameShort);

                    for (int i = 0; i < depthImage.WidthPixels * depthImage.HeightPixels; i++)
                    {
                        depthFrameDouble[i] = (sensorElevation / (1 - verticalTiltCorrectionMatrix[i]));
                    }

                    for (int i = depthWidth/2; i < depthFrameShort.Length; i += 1)
                    {
                        pt.X = Math.Round(depthFrameDouble[i] * translationMatrix[i].Value.X, 1);
                        pt.Y = Math.Round(depthFrameDouble[i] * translationMatrix[i].Value.Y, 1);
                        pt.Z = Math.Ceiling(depthFrameDouble[i] - (short)pt.Y * sin6);

                        points.Add(pt);

                    }
                    /*
                    for (int i = 0; i < 100; i++)
                    {
                        p3d.X = -1000;
                        p3d.Y = i;
                        p3d.Z = 1000;

                        var p = calibration.Convert3DTo3D(p3d, CalibrationGeometry.Depth, CalibrationGeometry.Color);
                        pt.X = p.X;
                        pt.Y = p.Y;
                        pt.Z = p.Z;
                        
                        points.Add(pt);
                    }
                    */
                    //depthFrameData = depthImage.GetPixels<ushort>().ToArray(); //Works
                    /*

                    _colourWidth = calibration.ColorCameraCalibration.ResolutionWidth;
                    _colourHeight = calibration.ColorCameraCalibration.ResolutionHeight;

                    Image transformedDepth = new Image(ImageFormat.Depth16, _colourWidth, _colourHeight, _colourWidth * sizeof(UInt16));
                    
                        // Transform the depth image to the colour capera perspective.
                        _transformation.DepthImageToColorCamera(depthImage, transformedDepth);

                    
                    

                    xyzImageBuffer = new short[depthWidth * depthHeight* 3];
                    int xyzImageStride = depthWidth * sizeof(short) * 3;
                    using (var transformation = calibration.CreateTransformation())
                    {
                        using (var xyzImage = Image.CreateFromArray(xyzImageBuffer, ImageFormat.Custom, depthWidth, depthHeight, xyzImageStride))
                        {
                            
                            transformation.DepthImageToPointCloud(depthImage, CalibrationGeometry.Depth, xyzImage);
                        }
                    }
                    */

                }
            }

        }



        public static void Initialize(Core.KinectTypes k4AConfig)
        {
            string message;
            CreateCameraConfig(k4AConfig); // Apply user options from Sandworm Options
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
                _transformation = calibration.CreateTransformation();


                K4AdotNet.Float2 depthPixel = new K4AdotNet.Float2();
                K4AdotNet.Float3? translationVector;

                //System.Numerics.Vector2 depthPixel = new System.Numerics.Vector2();
                //System.Numerics.Vector3? translationVector;

                //translationMatrix = new System.Numerics.Vector3?[depthHeight * depthWidth];
                translationMatrix = new K4AdotNet.Float3?[depthHeight * depthWidth];
                verticalTiltCorrectionMatrix = new double[depthHeight * depthWidth];


                for (int y = 0, i = 0; y < depthHeight; y++)
                {
                    depthPixel.Y = (float)y;
                    for (int x = 0; x < depthWidth; x++, i++)
                    {
                        depthPixel.X = (float)x;
                        translationVector = calibration.Convert2DTo3D(depthPixel, 1f, CalibrationGeometry.Depth, CalibrationGeometry.Depth);
                        translationMatrix[i] = translationVector;
                        
                        verticalTiltCorrectionMatrix[i] = translationVector.Value.Y * sin6;

                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.ToString();
            }

            CaptureFrame();
        }
    }
}