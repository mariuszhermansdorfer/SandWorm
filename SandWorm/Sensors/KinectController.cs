using System;
using System.Windows.Media;
using Microsoft.Kinect;


namespace SandWorm
{
    static class KinectController
    {
        // Shared in K4Azure Controller
        public static int depthHeight = 0;
        public static int depthWidth = 0;
        public static int colorHeight = 0;
        public static int colorWidth = 0;
        public static ushort[] depthFrameData = null;
        public static byte[] colorFrameData = null;
        // Kinect for Windows specific
        public static KinectSensor sensor = null;
        public static MultiSourceFrameReader multiFrameReader = null;
        public static FrameDescription depthFrameDescription = null;
        public static FrameDescription colorFrameDescription = null;
        public static int refc = 0;
        public static int bytesForPixelColor = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        // Kinect for Windows Details
        public static double kinect2FOVForX = 70.6;
        public static double kinect2FOVForY = 60.0;
        public static int kinect2ResolutionForX = 512;
        public static int kinect2ResolutionForY = 424;

        public static void AddRef()
        {
            if (sensor == null)
            {
                Initialize();
            }
            if (sensor != null)
            {
                refc++;
            }
        }

        public static void SetupSensor(ref string errorMessage)
        {
            if (sensor == null)
            {
                KinectController.AddRef();
                //sensor = KinectController.sensor;
            }
            if (KinectController.depthFrameData == null)
                errorMessage = "No depth frame data provided by the Kinect for Windows.";
        }

        public static void RemoveRef()
        {
            refc--;
            if ((sensor != null) && (refc == 0))
            {
                multiFrameReader.MultiSourceFrameArrived -= Reader_FrameArrived;
                sensor.Close();
                sensor = null;
            }
        }

        public static void Initialize()
        {
            sensor = KinectSensor.GetDefault();
            // TODO: switch based on component type?
            multiFrameReader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color);
            multiFrameReader.MultiSourceFrameArrived += new EventHandler<MultiSourceFrameArrivedEventArgs>(KinectController.Reader_FrameArrived);

            sensor.Open();
        }

        private static void Reader_FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            if (e.FrameReference != null)
            {
                MultiSourceFrame multiFrame = e.FrameReference.AcquireFrame();

                if (multiFrame.DepthFrameReference != null)
                {
                    try
                    {
                        using (DepthFrame depthFrame = multiFrame.DepthFrameReference.AcquireFrame())
                        {
                            if (depthFrame != null)
                            {
                                using (KinectBuffer buffer = depthFrame.LockImageBuffer())
                                {
                                    depthFrameDescription = depthFrame.FrameDescription;
                                    depthWidth = depthFrameDescription.Width;
                                    depthHeight = depthFrameDescription.Height;
                                    depthFrameData = new ushort[depthWidth * depthHeight];
                                    depthFrame.CopyFrameDataToArray(depthFrameData);
                                }
                            }
                        }
                    }
                    catch (Exception) { return; }
                }

                if (multiFrame.ColorFrameReference != null)
                {
                    try
                    {
                        using (ColorFrame colorFrame = multiFrame.ColorFrameReference.AcquireFrame())
                        {
                            if (colorFrame != null)
                            {
                                colorFrameDescription = colorFrame.FrameDescription;
                                colorWidth = colorFrameDescription.Width;
                                colorHeight = colorFrameDescription.Height;
                                colorFrameData = new byte[colorWidth * colorHeight * bytesForPixelColor]; // 4 == bytes per color

                                using (KinectBuffer buffer = colorFrame.LockRawImageBuffer())
                                {
                                    if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                                    {
                                        colorFrame.CopyRawFrameDataToArray(colorFrameData);
                                    }
                                    else
                                    {
                                        colorFrame.CopyConvertedFrameDataToArray(colorFrameData, ColorImageFormat.Bgra);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception) { return; }
                }
            }
        }

        public static KinectSensor Sensor
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