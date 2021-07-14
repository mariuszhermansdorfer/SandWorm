using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SandWorm
{
    public static class GeneralHelpers
    {
        public static void SetupLogging(Stopwatch timer, List<string> output)
        {
            timer = Stopwatch.StartNew(); // Setup timer used for debugging
            output = new List<string>(); // For the debugging log lines
        }
        public static void LogTiming(ref List<string> output, Stopwatch timer, string eventDescription)
        {
            var logInfo = eventDescription + ": ";
            timer.Stop();
            output.Add(logInfo.PadRight(28, ' ') + timer.ElapsedMilliseconds.ToString() + " ms");
            timer.Restart();
        }

        public static double ConvertDrawingUnits(Rhino.UnitSystem units)
        {
            double unitsMultiplier = 1.0;

            switch (units.ToString())
            {
                case "Kilometers":
                    unitsMultiplier = 0.0001;
                    break;

                case "Meters":
                    unitsMultiplier = 0.001;
                    break;

                case "Decimeters":
                    unitsMultiplier = 0.01;
                    break;

                case "Centimeters":
                    unitsMultiplier = 0.1;
                    break;

                case "Millimeters":
                    unitsMultiplier = 1.0;
                    break;

                case "Inches":
                    unitsMultiplier = 0.0393701;
                    break;

                case "Feet":
                    unitsMultiplier = 0.0328084;
                    break;
            }
            return unitsMultiplier;
        }


        // Multiply two int[] arrays using SIMD instructions
        public static int[] SimdVectorProd(int[] a, int[] b)
        {
            if (a.Length != b.Length) throw new ArgumentException();
            if (a.Length == 0) return Array.Empty<int>();

            int[] result = new int[a.Length];

            // Get a reference to the first value in all 3 arrays
            ref int ra = ref a[0];
            ref int rb = ref b[0];
            ref int rr = ref result[0];
            int length = a.Length;
            int i = 0;


            /* Calculate the maximum offset we can work on with SIMD instructions.
             * Eg. if each SIMD register can hold 4 int values, and our input
             * arrays have 10 values, we can use SIMD instructions to sum the
             * first two groups of 4 integers, leaving the last 2 out. */
            int end = length - Vector<int>.Count;

            for (; i <= end; i += Vector<int>.Count)
            {
                // Get the reference into a and b at the current position
                ref int rai = ref Unsafe.Add(ref ra, i);
                ref int rbi = ref Unsafe.Add(ref rb, i);

                /* Reinterpret those references as Vector<int>, which effectively
                 * means reading a Vector<int> value starting from the memory
                 * locations those two references are pointing to.
                 * The JIT compiler will make sure that our Vector<int>
                 * variables are stored in exactly one SIMD register each.
                 * Once we have them, we can multiply those together, which will
                 * use a single special SIMD instruction in assembly. */

                // va = { a[i], a[i + 1], ..., a[i + Vector<int>.Count - 1] }
                Vector<int> va = Unsafe.As<int, Vector<int>>(ref rai);

                // vb = { b[i], b[i + 1], ..., b[i + Vector<int>.Count - 1] }
                Vector<int> vb = Unsafe.As<int, Vector<int>>(ref rbi);

                /* vr =
                 * {
                 *     a[i] * b[i],
                 *     a[i + 1] * b[i + 1],
                 *     ...,
                 *     a[i + Vector<int>.Count - 1] * b[i + Vector<int>.Count - 1]
                 * } */
                Vector<int> vr = va * vb;

                // Get the reference into the target array
                ref int rri = ref Unsafe.Add(ref rr, i);

                // Store the resulting vector at the right position
                Unsafe.As<int, Vector<int>>(ref rri) = vr;
            }


            // Multiply the remaining values
            for (; i < a.Length; i++)
            {
                result[i] = a[i] * b[i];
            }

            return result;
        }
    }
}
