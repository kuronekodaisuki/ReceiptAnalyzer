using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;

namespace EmguTest
{
    public class Segment
    {
        private int _x, _y;
        private int _width;
        private int _height;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public Segment(int x, int y, int width, int height)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        /// <summary>
        /// Get bound rectangle
        /// </summary>
        /// <returns></returns>
        public Rectangle bound()
        {
            return new Rectangle(_x, _y, _width, _height);
        }
    }
}
