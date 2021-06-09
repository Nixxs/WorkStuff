using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoToCoordinate
{
    class Coordinate
    {
        private double _y;
        private double _x;

        public Coordinate(double y, double x)
        {
            _y = y;
            _x = x;
        }

        public double Y
        {
            get { return _y; }
        }

        public double X
        {
            get { return _x; }
        }
    }
}
