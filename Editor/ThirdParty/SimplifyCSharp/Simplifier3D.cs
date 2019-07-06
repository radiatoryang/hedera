using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hedera.SimplifyCSharp
{
    public class Simplifier3D<T>: BaseSimplifier<T>
    {
        readonly Func<T, float> _xExtractor;
        readonly Func<T, float> _yExtractor;
        readonly Func<T, float> _zExtractor;

        public Simplifier3D(Func<T, T, Boolean> equalityChecker,
            Func<T, float> xExtractor, Func<T, float> yExtractor, Func<T, float> zExtractor) :
            base(equalityChecker)
        {
            _xExtractor = xExtractor;
            _yExtractor = yExtractor;
            _zExtractor = zExtractor;
        }

        protected override float GetSquareDistance(T p1, T p2)
        {
            float dx = _xExtractor(p1) - _xExtractor(p2);
            float dy = _yExtractor(p1) - _yExtractor(p2);
            float dz = _zExtractor(p1) - _zExtractor(p2);

            return dx * dx + dy * dy + dz * dz;
        }

        protected override float GetSquareSegmentDistance(T p0, T p1, T p2)
        {
            float x0, y0, z0, x1, y1, z1, x2, y2, z2, dx, dy, dz, t;

            x1 = _xExtractor(p1);
            y1 = _yExtractor(p1);
            z1 = _zExtractor(p1);
            x2 = _xExtractor(p2);
            y2 = _yExtractor(p2);
            z2 = _zExtractor(p2);
            x0 = _xExtractor(p0);
            y0 = _yExtractor(p0);
            z0 = _zExtractor(p0);

            dx = x2 - x1;
            dy = y2 - y1;
            dz = z2 - z1;

            if (dx != 0.0f || dy != 0.0f || dz != 0.0f)
            {
                t = ((x0 - x1) * dx + (y0 - y1) * dy + (z0 - z1) * dz)
                        / (dx * dx + dy * dy + dz * dz);

                if (t > 1.0f)
                {
                    x1 = x2;
                    y1 = y2;
                    z1 = z2;
                }
                else if (t > 0.0f)
                {
                    x1 += dx * t;
                    y1 += dy * t;
                    z1 += dz * t;
                }
            }

            dx = x0 - x1;
            dy = y0 - y1;
            dz = z0 - z1;

            return dx * dx + dy * dy + dz * dz;
        }
    }
}
