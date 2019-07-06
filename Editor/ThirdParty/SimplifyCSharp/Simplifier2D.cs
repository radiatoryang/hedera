using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hedera.SimplifyCSharp
{
    public class Simplifier2D<T> : BaseSimplifier<T>
    {
        readonly Func<T, float> _xExtractor;
        readonly Func<T, float> _yExtractor;

        public Simplifier2D(Func<T, T, Boolean> equalityChecker,
            Func<T, float> xExtractor, Func<T, float> yExtractor) :
            base(equalityChecker)
        {
            _xExtractor = xExtractor;
            _yExtractor = yExtractor;
        }

        protected override float GetSquareDistance(T p1, T p2)
        {
            float dx = _xExtractor(p1) - _xExtractor(p2);
            float dy = _yExtractor(p1) - _yExtractor(p2);

            return dx * dx + dy * dy;
        }

        protected override float GetSquareSegmentDistance(T p0, T p1, T p2)
        {
            float x1 = _xExtractor(p1);
            float y1 = _yExtractor(p1);
            float x2 = _xExtractor(p2);
            float y2 = _yExtractor(p2);
            float x0 = _xExtractor(p0);
            float y0 = _yExtractor(p0);

            float dx = x2 - x1;
            float dy = y2 - y1;

            float t;

            if (dx != 0f|| dy != 0f)
            {
                t = ((x0 - x1) * dx + (y0 - y1) * dy)
                        / (dx * dx + dy * dy);

                if (t > 1.0f)
                {
                    x1 = x2;
                    y1 = y2;
                }
                else if (t > 0f)
                {
                    x1 += dx * t;
                    y1 += dy * t;
                }
            }

            dx = x0 - x1;
            dy = y0 - y1;

            return dx * dx + dy * dy;
        }
    }
}
