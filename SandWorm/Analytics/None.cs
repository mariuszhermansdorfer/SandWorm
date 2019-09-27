using System.Collections.Generic;
using System.Drawing;
using Rhino.Geometry;

namespace SandWorm.Analytics
{
    public class None : Analysis.MeshColorAnalysis
    {
        public None() : base("No Visualisation")
        {
        }
        public void GetColorCloudForAnalysis(ref Color[] vertexColors)
        {
            vertexColors = new Color[0]; // Send back an empty array so mesh is transparent/uncolored
        }

        public override void ComputeLookupTableForAnalysis(double sensorElevation)
        {
            return; // No lookup table necessary
        }
    }
}