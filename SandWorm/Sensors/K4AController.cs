using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using K4AdotNet.Sensor;

namespace SandWorm
{
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
        public static Device sensor = null;
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


        public static void SetupSensor(ref string errorMessage)
        {
            if (sensor == null)
            {
                try
                {
                    sensor = Device.Open();
                }
                catch (Exception exc)
                {
                    errorMessage = exc.Message;
                }
            }

            if (K4AController.depthFrameData == null)
                errorMessage = "No depth frame data provided by the Kinect for Azure.";
        }
    }
}
