using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hedera.SimplifyCSharp
{
    public abstract class BaseSimplifier<T>
    {
        private class Range
        {
            public int First { get; }
            public int Last { get; }

            public Range(int first, int last)
            {
                First = first;
                Last = last;
            }
        }

        protected BaseSimplifier(Func<T, T, Boolean> equalityChecker)
        {
            _equalityChecker = equalityChecker;
        }

        Func<T, T, Boolean> _equalityChecker;

        /// <summary>
        /// Simplified data points
        /// </summary>
        /// <param name="points">Points to be simplified</param>
        /// <param name="tolerance">Amount of wiggle to be tolerated between coordinates.</param>
        /// <param name="highestQuality">
        /// True for Douglas-Peucker. 
        /// False for Radial-Distance before Douglas-Peucker (Runs Faster)
        /// </param>
        /// <returns>Simplified points</returns>
        public IList<T> Simplify(IList<T> points,
                            float tolerance,
                            bool highestQuality)
        {

            if (points == null || points.Count <= 2)
            {
                return points;
            }

            float sqTolerance = tolerance * tolerance;

            if (!highestQuality)
            {
                points = SimplifyRadialDistance(points, sqTolerance);
            }

            points = SimplifyDouglasPeucker(points, sqTolerance);

            return points;
        }

        IList<T> SimplifyRadialDistance(IList<T> points, float sqTolerance)
        {
            T point = default(T);
            T prevPoint = points[0];

            IList<T> newPoints = new List<T>();
            newPoints.Add(prevPoint);

            for (int i = 1; i < points.Count; ++i)
            {
                point = points[i];

                if (GetSquareDistance(point, prevPoint) > sqTolerance)
                {
                    newPoints.Add(point);
                    prevPoint = point;
                }
            }

            if (!_equalityChecker(prevPoint, point))
            {
                newPoints.Add(point);
            }

            return newPoints.ToArray();
        }

        IList<T> SimplifyDouglasPeucker(IList<T> points, float sqTolerance)
        {

            BitArray bitArray = new BitArray(points.Count);
            bitArray.Set(0, true);
            bitArray.Set(points.Count - 1, true);

            Stack<Range> stack = new Stack<Range>();
            stack.Push(new Range(0, points.Count - 1));

            while (stack.Count > 0)
            {
                Range range = stack.Pop();

                int index = -1;
                float maxSqDist = 0f;

                // Find index of point with maximum square distance from first and last point
                for (int i = range.First + 1; i < range.Last; ++i)
                {
                    float sqDist = GetSquareSegmentDistance(
                        points[i], points[range.First], points[range.Last]);

                    if (sqDist > maxSqDist)
                    {
                        index = i;
                        maxSqDist = sqDist;
                    }
                }

                if (maxSqDist > sqTolerance)
                {
                    bitArray.Set(index, true);

                    stack.Push(new Range(range.First, index));
                    stack.Push(new Range(index, range.Last));
                }
            }

            List<T> newPoints = new List<T>(CountNumberOfSetBits(bitArray));

            for (int i = 0; i < bitArray.Count; i++)
            {
                if (bitArray[i])
                {
                    newPoints.Add(points[i]);
                }
            }

            return newPoints.ToArray();
        }

        int CountNumberOfSetBits(BitArray bitArray)
        {
            int counter = 0;
            for (int i = 0; i < bitArray.Length; i++)
            {
                if (bitArray[i])
                {
                    counter++;
                }
            }
            return counter;
        }

        protected abstract float GetSquareDistance(T p1, T p2);
        protected abstract float GetSquareSegmentDistance(T p0, T p1, T p2);
    }
}
