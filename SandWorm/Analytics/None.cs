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

        public override void ComputeLookupTableForAnalysis(double sensorElevation)
        {
            lookupTable = new Color[0]; // Empty color table allows pixel loop to skip lookup
        }
    }
}