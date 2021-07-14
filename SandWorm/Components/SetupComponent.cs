using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino;


namespace SandWorm
{
    public class SetupComponent 
    {
        public static int sensorElevation;
        private double _averagedSensorElevation;
        private bool _calibrateSandworm;
        private double[] _elevationArray;
        private int _frameCount = 0; // Number of frames to average the calibration across



        private short[] depthFrameShort;



        void SandwormSolveInstance(IGH_DataAccess DA)
        {

            //_averagedSensorElevation = sensorElevation;



            /*

            if (_calibrateSandworm) _frameCount = 60; // Start calibration 


            if (_frameCount > 1) // Iterate a pre-set number of times
            {
                output.Add("Reading frame: " + _frameCount); // Debug Info

                // Trim the depth array and cast ushort values to int
                Core.CopyAsIntArray(depthFrameData, depthFrameDataInt,
                                    leftColumns, rightColumns, topRows, bottomRows,
                                    active_Height, active_Width);


                renderBuffer.AddLast(depthFrameDataInt);
                for (var pixel = 0; pixel < depthFrameDataInt.Length; pixel++)
                {
                    if (depthFrameDataInt[pixel] > 200) // We have a valid pixel. 
                    {
                        runningSum[pixel] += depthFrameDataInt[pixel];
                    }
                    else
                    {
                        if (pixel > 0) // Pixel is invalid and we have a neighbor to steal information from
                        {
                            runningSum[pixel] += depthFrameDataInt[pixel - 1];
                            // Replace the zero value from the depth array with the one from the neighboring pixel
                            renderBuffer.Last.Value[pixel] = depthFrameDataInt[pixel - 1]; 
                        }
                        else // Pixel is invalid and it is the first one in the list. (No neighbor on the left hand side, so we set it to the lowest point on the table)
                        {
                            runningSum[pixel] += (int) sensorElevation;
                            renderBuffer.Last.Value[pixel] = (int) sensorElevation;
                        }
                    }
                    averagedDepthFrameData[pixel] = runningSum[pixel] / renderBuffer.Count; // Calculate average values
                }

                _frameCount--;
                if (_frameCount == 1) // All frames have been collected, we can save the results
                {
                    // Measure sensor elevation by averaging over a grid of 20x20 pixels in the center of the table
                    int counter = 0;
                    _averagedSensorElevation = 0;

                    for (var y = trimmedHeight / 2 - 10; y < trimmedHeight / 2 + 10; y++) // Iterate over y dimension
                    for (var x = trimmedWidth / 2 - 10; x < trimmedWidth / 2 + 10; x++) // Iterate over x dimension
                    {
                        var i = y * trimmedWidth + x;
                        _averagedSensorElevation += averagedDepthFrameData[i];
                        counter++;
                    }

                    _averagedSensorElevation /= counter;

                    // Counter for Kinect inaccuracies and potential hardware misalignment by storing differences between the averaged sensor elevation and individual pixels.
                    for (int i = 0; i < averagedDepthFrameData.Length; i++)
                        _elevationArray[i] = averagedDepthFrameData[i] - _averagedSensorElevation;

                    _averagedSensorElevation *= unitsMultiplier;

                    renderBuffer.Clear();
                    Array.Clear(runningSum, 0, runningSum.Length);
                }

                if (_frameCount > 1)
                    ScheduleSolve(); // Schedule another solution to get more data from Kinect
                else
                    OnPingDocument().ScheduleSolution(5, ManipulateSlider);
            }



            var outputOptions = new SetupOptions
            {
                SensorElevation = _averagedSensorElevation,
                LeftColumns = leftColumns,
                RightColumns = rightColumns,
                TopRows = topRows,
                BottomRows = bottomRows,
                TickRate = tickRate,
                KeepFrames = keepFrames,
                ElevationArray = _elevationArray,
                IdealXYCoordinates = trimmedXYLookupTable,
                VerticalTiltCorrectionLookupTable = verticalTiltCorrectionLookupTable,
                KinectType = kinectType,
            };
            
            */
        }
    }
}