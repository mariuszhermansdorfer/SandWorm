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
                mesh.Vertices.UseDoublePrecisionVertices = true;
                mesh.Vertices.AddVertices(vertices);       

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
                mesh.Vertices.AddVertices(vertices);       
            }

            mesh.VertexColors.SetColors(colors.ToArray());
            return mesh;
        }

        public static Color[] ComputeLookupTable(int waterLevel, Color[] lookupTable)
        {
            //precompute all vertex colors
            int j = 0;
            for (int i = waterLevel; i < lookupTable.Length; i++) //below water level
            {
                lookupTable[i] = new ColorHSL(0.6, 0.6, 0.60 - (j * 0.02)).ToArgbColor();
                j++;
            }

            j = 0;
            for (int i = waterLevel; i > 0; i--) //above water level
            {
                lookupTable[i] = new ColorHSL(0.01 + (j * 0.01), 1.0, 0.5).ToArgbColor();
                j++;
            }
            return lookupTable;
        }
    }
}
