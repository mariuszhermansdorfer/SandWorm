using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public double sensorElevation
        {
            get { return _sensorElevation; }
            set { _sensorElevation = value; }
        }
        public int leftColumns
        {
            get { return _leftColumns; }
            set { _leftColumns = value; }
        }

        public int rightColumns
        {
            get { return _rightColumns; }
            set { _rightColumns = value; }
        }

        public int topRows
        {
            get { return _topRows; }
            set { _topRows = value; }
        }

        public int bottomRows
        {
            get { return _bottomRows; }
            set { _bottomRows = value; }
        }

        public int tickRate
        {
            get { return _tickRate; }
            set { _tickRate = value; }
        }
    }
}
