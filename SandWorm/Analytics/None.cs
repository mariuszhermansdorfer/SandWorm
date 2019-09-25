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

        public override int GetPixelIndexForAnalysis(Point3d vertex, List<Point3d> analysisPts)
        {
            return 0; // Should never be called (see below)
        }

        public override void ComputeLookupTableForAnalysis(double sensorElevation)
        {
            lookupTable = new Color[0]; // Empty color table allows pixel loop to skip lookup
        }
    }
}