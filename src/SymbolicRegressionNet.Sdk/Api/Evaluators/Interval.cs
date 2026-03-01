using System;

namespace SymbolicRegressionNet.Sdk.Api.Evaluators
{
    /// <summary>
    /// Represents a mathematical interval [Min, Max] for reliable execution bounds checking.
    /// </summary>
    public readonly struct Interval
    {
        public double Min { get; }
        public double Max { get; }

        public Interval(double min, double max)
        {
            Min = min;
            Max = max;
        }

        public static Interval Add(Interval a, Interval b) => new Interval(a.Min + b.Min, a.Max + b.Max);
        public static Interval Sub(Interval a, Interval b) => new Interval(a.Min - b.Max, a.Max - b.Min);

        public static Interval Mul(Interval a, Interval b)
        {
            double v1 = a.Min * b.Min;
            double v2 = a.Min * b.Max;
            double v3 = a.Max * b.Min;
            double v4 = a.Max * b.Max;
            return new Interval(
                Math.Min(Math.Min(v1, v2), Math.Min(v3, v4)),
                Math.Max(Math.Max(v1, v2), Math.Max(v3, v4))
            );
        }

        public static Interval Div(Interval a, Interval b)
        {
            if (b.Min <= 0 && b.Max >= 0)
            {
                // Div by zero interval -> covers infinity
                return new Interval(double.NegativeInfinity, double.PositiveInfinity);
            }
            return Mul(a, new Interval(1.0 / b.Max, 1.0 / b.Min));
        }

        public static Interval Sin(Interval a)
        {
            if (a.Max - a.Min >= 2 * Math.PI) return new Interval(-1.0, 1.0);
            // Simplification: tight bounding of sine is complex due to periodicity.
            // For rough screening, return [-1, 1] as a conservative bound.
            return new Interval(-1.0, 1.0);
        }

        public bool IsValid()
        {
            return !double.IsNaN(Min) && !double.IsNaN(Max) && Min <= Max;
        }
    }
}
