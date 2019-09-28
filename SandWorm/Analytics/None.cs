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
        public Color[] GetColorCloudForAnalysis()
        {
            var vertexColors = new Color[0];
            return vertexColors; // Send back an empty array so mesh is transparent/uncolored
        }

        public override void ComputeLookupTableForAnalysis(double sensorElevation)
        {
            return; // No lookup table necessary
        }
    }
}