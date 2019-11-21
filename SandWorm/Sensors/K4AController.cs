using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SandWorm
{
    static class K4AController
    {
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
    }
}
