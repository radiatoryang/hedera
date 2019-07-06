using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hedera.SimplifyCSharp
{
    public class SimplificationHelpers
    {
        public static IList<T> Simplify<T>(
            IList<T> points,
            Func<T, T, Boolean> equalityChecker,
            Func<T, float> xExtractor, 
            Func<T, float> yExtractor, 
            Func<T, float> zExtractor,
            float tolerance = 1.0f,
            bool highestQuality = false)
        {
            var simplifier3D = new Simplifier3D<T>(equalityChecker, xExtractor, yExtractor, zExtractor);
            return simplifier3D.Simplify(points, tolerance, highestQuality);
        }

        public static IList<T> Simplify<T>(
            IList<T> points,
            Func<T, T, Boolean> equalityChecker,
            Func<T, float> xExtractor,
            Func<T, float> yExtractor,
            float tolerance = 1.0f,
            bool highestQuality = false)
        {
            var simplifier2D = new Simplifier2D<T>(equalityChecker, xExtractor, yExtractor);
            return simplifier2D.Simplify(points, tolerance, highestQuality);
        }
    }
}
