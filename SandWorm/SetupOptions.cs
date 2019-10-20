﻿
namespace SandWorm
{
    public class SetupOptions
    {
        public SetupOptions()
        {
        }

        private double _sensorElevation;
        private int _leftColumns;
        private int _rightColumns;
        private int _topRows;
        private int _bottomRows;
        private int _tickRate;
        private int _keepFrames;
        private double[] _elevationArray; // Store all deltas between desired and measured distance values from the sensor to the table for each pixel.

        public double SensorElevation
        {
            get { return _sensorElevation; }
            set { _sensorElevation = value; }
        }
        public int LeftColumns
        {
            get { return _leftColumns; }
            set { _leftColumns = value; }
        }

        public int RightColumns
        {
            get { return _rightColumns; }
            set { _rightColumns = value; }
        }

        public int TopRows
        {
            get { return _topRows; }
            set { _topRows = value; }
        }

        public int BottomRows
        {
            get { return _bottomRows; }
            set { _bottomRows = value; }
        }

        public int TickRate
        {
            get { return _tickRate; }
            set { _tickRate = value; }
        }

        public int KeepFrames
        {
            get { return _keepFrames; }
            set { _keepFrames = value; }
        }
        public double[] ElevationArray
        {
            get { return _elevationArray; }
            set { _elevationArray = value; }
        }

    }
}
