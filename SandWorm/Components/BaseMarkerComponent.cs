using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Microsoft.Kinect;
using Rhino;
using Rhino.Geometry;
using OpenCvSharp;

namespace SandWorm.Components
{
    // Provides common functions across the components that classify markers within the Kinect stream
    public abstract class BaseMarkerComponent : BaseKinectComponent
    {
        protected List<Color> markerColors;
        protected double colorFuzz;
        protected Color[] allPixels;

        public BaseMarkerComponent(string name, string nickname, string description)
            : base(name, nickname, description, "Markers")
        {
        }

        protected Mat GenerateColorImage()
        {
            if (KinectForWindows.colorFrameData == null)
            {
                ShowComponentError("No color frame data provided by the Kinect.");
                return null;
            }

            // Convert the kinect pixel byte array to a opencv mat file for processing
            var height = KinectForWindows.colorHeight;
            var width = KinectForWindows.colorWidth;
            var mat = new Mat(height, width, MatType.CV_8UC4, KinectForWindows.colorFrameData);
            Core.LogTiming(ref output, timer, "Color image generation"); // Debug Info
            return mat;
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddColourParameter("MarkerColor", "Color", "The color, or colors, of the markers this component should track", GH_ParamAccess.list);
            pManager.AddNumberParameter("ColorFuzz", "Fuzz", "The amount of leeway to use when matching the color. A higher value will identify more similar colors",
                GH_ParamAccess.item);
            pManager.AddGenericParameter("SandwormOptions", "SWO", "Setup & Calibration options", GH_ParamAccess.item);
            pManager[0].Optional = false;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }
    }
}