using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SandWorm
{
    /// <summary>
    /// Applies Gaussian blur processing to a <see cref="float"/> image
    /// </summary>
    public sealed class GaussianBlurProcessor
    {
        /// <summary>
        /// The width of the target image
        /// </summary>
        private readonly int Width;

        /// <summary>
        /// The height of the target image
        /// </summary>
        private readonly int Height;

        /// <summary>
        /// The 1D kernel to apply
        /// </summary>
        private readonly float[] Kernel;

        /// <summary>
        /// The array to store the results of the first convolution pass
        /// </summary>
        private readonly float[] FirstPassBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="GaussianBlurProcessor"/> class
        /// </summary>
        /// <param name="radius">The blur radius to use</param>
        /// <param name="width">The width of the image to apply the blur to</param>
        /// <param name="height">The height of the image to apply the blur to</param>
        public GaussianBlurProcessor(int radius, int width, int height)
        {
            int kernelSize = radius * 2 + 1;
            Kernel = CreateGaussianBlurKernel(kernelSize, radius / 3f);
            FirstPassBuffer = new float[width * height];
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Creates a 1 dimensional Gaussian kernel using the Gaussian G(x) function
        /// </summary>
        [Pure]
        private static float[] CreateGaussianBlurKernel(int size, float weight)
        {
            float[] kernel = new float[size];
            ref float rKernel = ref kernel[0];

            float sum = 0F;
            float midpoint = (size - 1) / 2F;

            for (int i = 0; i < size; i++)
            {
                float x = i - midpoint;
                float gx = Gaussian(x, weight);
                sum += gx;
                Unsafe.Add(ref rKernel, i) = gx;
            }

            // Normalize kernel so that the sum of all weights equals 1
            for (int i = 0; i < size; i++)
            {
                Unsafe.Add(ref rKernel, i) /= sum;
            }

            return kernel;
        }

        /// <summary>
        /// Implementation of 1D Gaussian G(x) function
        /// </summary>
        /// <param name="x">The x provided to G(x)</param>
        /// <param name="sigma">The spread of the blur</param>
        /// <returns>The Gaussian G(x)</returns>
        [Pure]
        private static float Gaussian(float x, float sigma)
        {
            const float Numerator = 1.0f;
            float denominator = (float)Math.Sqrt(2 * Math.PI) * sigma;

            float exponentNumerator = -x * x;
            float exponentDenominator = 2 * sigma * sigma;

            float left = Numerator / denominator;
            float right = (float)Math.Exp(exponentNumerator / exponentDenominator);

            return left * right;
        }

        /// <summary>
        /// Applies the current effect to a target array
        /// </summary>
        /// <param name="source">The source and destination array</param>
        public void Apply(float[] source)
        {
            ApplyVerticalConvolution(source, FirstPassBuffer, Kernel);
            ApplyHorizontalConvolution(FirstPassBuffer, source, Kernel);
        }

        /// <summary>
        /// Performs a vertical 1D complex convolution with the specified parameters
        /// </summary>
        /// <param name="source">The source array to read data from</param>
        /// <param name="target">The target array to write the results to</param>
        /// <param name="kernel">The array with the values for the current complex kernel</param>
        private void ApplyVerticalConvolution(float[] source, float[] target, float[] kernel)
        {
            int width = Width;
            int height = Height;
            int maxY = Height - 1;
            int maxX = Width - 1;
            int kernelLength = kernel.Length;

            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    float result = 0;
                    int radiusY = kernelLength >> 1;
                    int sourceOffsetColumnBase = x;

                    for (int i = 0; i < kernelLength; i++)
                    {
                        int offsetY = Clamp(y + i - radiusY, 0, maxY);
                        int offsetX = Clamp(sourceOffsetColumnBase, 0, maxX);
                        float value = source[offsetY * width + offsetX];

                        result += kernel[i] * value;
                    }

                    int offsetXY = y * width + x;
                    target[offsetXY] = result;
                }
            });
        }

        /// <summary>
        /// Performs an horizontal 1D complex convolution with the specified parameters
        /// </summary>
        /// <param name="source">The source array to read data from</param>
        /// <param name="target">The target array to write the results to</param>
        /// <param name="kernel">The array with the values for the current complex kernel</param>
        private void ApplyHorizontalConvolution(float[] source, float[] target, float[] kernel)
        {
            int height = Height;
            int width = Width;
            int maxY = height - 1;
            int maxX = width - 1;
            int kernelLength = kernel.Length;

            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    float result = 0;
                    int radiusX = kernelLength >> 1;
                    int sourceOffsetColumnBase = x;
                    int offsetY = Clamp(y, 0, maxY);
                    int offsetXY;

                    for (int i = 0; i < kernelLength; i++)
                    {
                        int offsetX = Clamp(sourceOffsetColumnBase + i - radiusX, 0, maxX);
                        offsetXY = offsetY * width + offsetX;
                        float value = source[offsetXY];

                        result += kernel[i] * value;
                    }

                    offsetXY = y * width + x;
                    target[offsetXY] = result;
                }
            });
        }

        /// <summary>
        /// Clamps the input value in a specified interval
        /// </summary>
        /// <param name="x">The input value</param>
        /// <param name="min">The minimum interval value</param>
        /// <param name="max">The maximum interval value</param>
        [Pure]
        private static int Clamp(int x, int min, int max)
        {
            if (x < min) return min;
            if (x > max) return max;
            return x;
        }
    }
}
