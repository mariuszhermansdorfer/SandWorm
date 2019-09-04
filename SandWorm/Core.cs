using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Rhino.Display;
using System.Drawing;

namespace SandWorm
{
    public class Core
    {
        public static Mesh CreateQuadMesh(Mesh mesh, List<Point3f> vertices, List<Color> colors, int xStride, int yStride)
        {
            int xd = xStride;       // The x-dimension of the data
            int yd = yStride;       // They y-dimension of the data


            if (mesh.Faces.Count() != (xStride - 2) * (yStride - 2))
            {
                SandWorm.output.Add("Face remeshing");
                mesh = new Mesh();
                mesh.Vertices.Capacity = vertices.Count();      // Don't resize array
                mesh.Vertices.UseDoublePrecisionVertices = true;       // Save memory
                mesh.Vertices.AddVertices(vertices);       // Add all points to vertex list

                for (int y = 1; y < yd - 1; y++)       // Iterate over y dimension
                {
                    for (int x = 1; x < xd - 1; x++)       // Iterate over x dimension
                    {
                        int i = y * xd + x;
                        int j = (y - 1) * xd + x;

                        mesh.Faces.AddFace(j - 1, j, i, i - 1);
                    }
                }
            }
            else
            {
                mesh.Vertices.Clear();
                mesh.Vertices.UseDoublePrecisionVertices = true;
                mesh.Vertices.AddVertices(vertices);       // Add all points to vertex list
            }

            mesh.VertexColors.SetColors(colors.ToArray());
            return mesh;
        }

        public static Color ColorizeVertex(double z, double maxEl, double minEl, double waterLevel, Interval hueRange)
        {

            if (z > waterLevel)
            {
                double c = MapValue(minEl, maxEl, 0, 1, z);
                double hue = hueRange.ParameterAt(c);
                ColorHSL hsl = new ColorHSL(hue, 1.0, 0.5);
                return hsl.ToArgbColor();
            }
            else
            {
                double hue = 0.6;
                ColorHSL hsl = new ColorHSL(hue, 1.0, 0.5);
                return hsl.ToArgbColor();
            }
        }


        public static double MapValue(double a0, double a1, double b0, double b1, double a)
        {
            return b0 + (b1 - b0) * ((a - a0) / (a1 - a0));
        }
    }
}
