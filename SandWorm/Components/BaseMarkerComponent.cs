using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Microsoft.Kinect;
using Rhino;
using Rhino.Geometry;

namespace SandWorm.Components
{
    // Provides common functions across the components that classify markers within the Kinect stream
    public abstract class BaseMarkerComponent : BaseKinectComponent
    {
        protected List<Color> markerColors;
        protected double colorFuzz;
        protected Color[] allPixels;

        public BaseMarkerComponent(string name, string nickname, string description)
            : base(name, nickname, description)
        {
        }

        protected void GenerateColorImage()
        {
            if (KinectController.colorFrameData == null)
            {
                ShowComponentError("No color frame data provided by the Kinect.");
                return;
            }

            allPixels = new Color[trimmedWidth * trimmedHeight];
            var colorFrameData = KinectController.colorFrameData;
            var bytesForPixelColor = KinectController.colorFrameDescription.BytesPerPixel;

            // Create and return an image that EMGU CV will like

            Core.LogTiming(ref output, timer, "Color image generation"); // Debug Info
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddColourParameter("MarkerColor", "Color", "The color, or colors, of the markers this component should track", GH_ParamAccess.list);
            pManager.AddNumberParameter("ColorFuzz", "Fuzz", "The amount of leeway to use when matching the color. A higher value will identify more similar colors",
                GH_ParamAccess.item);
            pManager.AddNumberParameter("SandwormOptions", "SWO", "Setup & Calibration options", GH_ParamAccess.list);
            pManager[0].Optional = false;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }
    }
}