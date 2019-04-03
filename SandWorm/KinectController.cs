using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Microsoft.Kinect;
using Grasshopper;


namespace SandWorm
{
    static class KinectController
    {
        private const int MapDepthToByte = 8000 / 256;
        public static KinectSensor sensor = null;
        public static CoordinateMapper coordinateMapper;
        public static ColorSpacePoint[] colorSpacePoints;
        public static CameraSpacePoint[] cameraSpacePoints;
        public static int colorHeight = 0;
        public static int colorWidth = 0;
        public static int depthHeight = 0;
        public static int depthWidth = 0;
        //public static bool gotFrame = false;
        public static MultiSourceFrameReader multiFrameReader = null;
        public static FrameDescription depthFrameDescription = null;
        public static FrameDescription colorFrameDescription = null;
        public static int refc = 0;
        public static ushort[] depthFrameData = null;
        public static byte[] colorFrameData = null;
        public static SandWorm kinectGHC = null;
        public static int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

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

        public static void RemoveRef()
        {
            refc--;
            if((sensor!=null) && (refc==0))
            {
                multiFrameReader.MultiSourceFrameArrived -= Reader_FrameArrived;
                sensor.Close();
                sensor = null;
            }
        }

        public static void Initialize()
        {
            sensor = KinectSensor.GetDefault();

            coordinateMapper = sensor.CoordinateMapper;
            multiFrameReader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Color);
            multiFrameReader.MultiSourceFrameArrived += new EventHandler<MultiSourceFrameArrivedEventArgs>(KinectController.Reader_FrameArrived);

            sensor.Open();
        }

        private static void Reader_FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            if (e.FrameReference != null)
            {
                MultiSourceFrame multiFrame = e.FrameReference.AcquireFrame();
                if (multiFrame.ColorFrameReference != null && multiFrame.DepthFrameReference != null)
                {
                    try
                    {
                        using (DepthFrame depthFrame = multiFrame.DepthFrameReference.AcquireFrame())
                        {
                            using(ColorFrame colorFrame = multiFrame.ColorFrameReference.AcquireFrame())
                            {
                                if (depthFrame != null && colorFrame != null)
                                {
                                    colorFrameDescription = colorFrame.FrameDescription;
                                    colorWidth = colorFrameDescription.Width;
                                    colorHeight = colorFrameDescription.Height;
                                    colorFrameData = new byte[colorWidth * colorHeight * bytesPerPixel];

                                    if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                                    {
                                        colorFrame.CopyRawFrameDataToArray(colorFrameData);
                                    }
                                    else
                                    {
                                        colorFrame.CopyConvertedFrameDataToArray(colorFrameData, ColorImageFormat.Bgra);
                                    }

                                    using (KinectBuffer buffer = depthFrame.LockImageBuffer())
                                    {
                                        depthFrameDescription = depthFrame.FrameDescription;
                                        depthWidth = depthFrame.FrameDescription.Width;
                                        depthHeight = depthFrame.FrameDescription.Height;
                                        depthFrameData = new ushort[depthWidth * depthHeight];

                                        cameraSpacePoints = new CameraSpacePoint[depthWidth * depthHeight];
                                        colorSpacePoints = new ColorSpacePoint[depthWidth * depthHeight];

                                        depthFrame.CopyFrameDataToArray(depthFrameData);

                                        //coordinateMapper.MapDepthFrameToColorSpace(depthFrameData, colorSpacePoints);
                                        //coordinateMapper.MapDepthFrameToCameraSpace(depthFrameData, cameraSpacePoints);
                                        coordinateMapper.MapDepthFrameToColorSpaceUsingIntPtr(buffer.UnderlyingBuffer, buffer.Size, colorSpacePoints);
                                        coordinateMapper.MapDepthFrameToCameraSpaceUsingIntPtr(buffer.UnderlyingBuffer, buffer.Size, cameraSpacePoints);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception) { return; }
                }
            }
        }

            //kinectGHC.CallExpireSolution();


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