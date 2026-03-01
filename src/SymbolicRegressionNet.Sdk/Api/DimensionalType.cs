using System;

namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// Represents the powers of fundamental physical dimensions (SI base units).
    /// Used to enforce dimensional constraints during symbolic regression searches.
    /// </summary>
    public struct DimensionalType : IEquatable<DimensionalType>
    {
        public sbyte Length { get; }
        public sbyte Mass { get; }
        public sbyte Time { get; }

        public static readonly DimensionalType Dimensionless = new DimensionalType(0, 0, 0);
        public static readonly DimensionalType Unknown = new DimensionalType(sbyte.MinValue, sbyte.MinValue, sbyte.MinValue);

        public DimensionalType(sbyte length, sbyte mass, sbyte time)
        {
            Length = length;
            Mass = mass;
            Time = time;
        }

        public static DimensionalType operator *(DimensionalType a, DimensionalType b)
        {
            if (a.Equals(Unknown) || b.Equals(Unknown)) return Unknown;
            return new DimensionalType((sbyte)(a.Length + b.Length), (sbyte)(a.Mass + b.Mass), (sbyte)(a.Time + b.Time));
        }

        public static DimensionalType operator /(DimensionalType a, DimensionalType b)
        {
            if (a.Equals(Unknown) || b.Equals(Unknown)) return Unknown;
            return new DimensionalType((sbyte)(a.Length - b.Length), (sbyte)(a.Mass - b.Mass), (sbyte)(a.Time - b.Time));
        }

        public bool Equals(DimensionalType other)
        {
            return Length == other.Length && Mass == other.Mass && Time == other.Time;
        }

        public override bool Equals(object obj) => obj is DimensionalType other && Equals(other);
        
        public override int GetHashCode() => HashCode.Combine(Length, Mass, Time);
    }
}
