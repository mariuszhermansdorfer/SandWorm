using Rhino.Geometry;
using System.Drawing;
using System;
using System.Runtime.CompilerServices;

namespace SandWorm
{
    public static class Core
    {
        public static Mesh CreateQuadMesh(Mesh mesh, Point3f[] vertices, Color[] colors, int xStride, int yStride)
        {
            int xd = xStride;       // The x-dimension of the data
            int yd = yStride;       // They y-dimension of the data


            if (mesh.Faces.Count != (xStride - 2) * (yStride - 2))
            {
                SandWorm.output.Add("Face remeshing");
                mesh = new Mesh();
                mesh.Vertices.Capacity = vertices.Length;      // Don't resize array
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

            if (colors.Length > 0) // Colors only provided if the mesh style permits
            {
                mesh.VertexColors.SetColors(colors); 
            }
            return mesh;
        }

        public struct PixelSize // Unfortunately no nice tuples in this version of C# :(
        {
            public float x;
            public float y;
        }

        public static PixelSize GetDepthPixelSpacing(float sensorHeight)
        {
            float kinect2FOVForX = 70.6F; 
            float kinect2FOVForY = 60.0F;
            float kinect2ResolutionForX = 512F;
            float kinect2ResolutionForY = 424F;

            PixelSize pixelsForHeight = new PixelSize
            {
                x = GetDepthPixelSizeInDimension(kinect2FOVForX, kinect2ResolutionForX, sensorHeight),
                y = GetDepthPixelSizeInDimension(kinect2FOVForY, kinect2ResolutionForY, sensorHeight)
            };
            return pixelsForHeight;
        }

        private static float GetDepthPixelSizeInDimension(float fovAngle, float resolution, float height)
        {
            float fovInRadians = ((float)Math.PI / 180) * fovAngle;
            float dimensionSpan = 2 * height * (float)Math.Tan(fovInRadians / 2);
            return dimensionSpan / resolution;
        }

        public static void CopyAsIntArray(ushort[] source, int[] destination, int leftColumns, int rightColumns, int topRows, int bottomRows, int height, int width)
        {
            ref ushort ru0 = ref source[0];
            ref int ri0 = ref destination[0];
            int j = 0;

            for (int rows = topRows; rows < height - bottomRows; rows++)
            {
                for (int columns = rightColumns; columns < width - leftColumns; columns++)
                {
                    int i = rows * width + columns;
                    Unsafe.Add(ref ri0, j) = Unsafe.Add(ref ru0, i);
                    j++;
                }
            }
                
        }
    }
}