using System;
using System.Collections.Generic;
using System.Linq;

namespace VoxelMash.Grids
{
    public struct ChunkSpaceCoords :
        IEquatable<ChunkSpaceCoords>,
        IComparable<ChunkSpaceCoords>,
        IFormattable
    {
        #region Unchecked coordinate math functions
        private static ChunkSpaceCoords Unchecked_Add(
            ChunkSpaceCoords ALeft,
            ChunkSpaceCoords ARight)
        {
            unchecked
            {
                byte bDiff = (byte)((byte)ARight.Level - (byte)ALeft.Level);

                return new ChunkSpaceCoords(
                    (ChunkSpaceLevel)Math.Max((byte)ALeft.FLevel, (byte)ARight.Level),
                    (byte)(ALeft.FX << bDiff + ARight.FX),
                    (byte)(ALeft.FY << bDiff + ARight.FY),
                    (byte)(ALeft.FZ << bDiff + ARight.FZ));
            }
        }
        private static ChunkSpaceCoords Unchecked_StepUp(
            ChunkSpaceCoords ACoords,
            byte ASteps)
        {
            if (ASteps == 0)
                return ACoords;

            unchecked
            {
                return new ChunkSpaceCoords(
                    (ChunkSpaceLevel)Math.Max(0, (byte)ACoords.Level - ASteps),
                    (byte)(ACoords.FX >> ASteps),
                    (byte)(ACoords.FY >> ASteps),
                    (byte)(ACoords.FZ >> ASteps));
            }
        }
        private static ChunkSpaceCoords Unchecked_StepDown(
            ChunkSpaceCoords ACoords,
            byte APath)
        {
            unchecked
            {
                return new ChunkSpaceCoords(
                    (ChunkSpaceLevel)((byte)ACoords.Level + 1),
                    (byte)(ACoords.FX << 1 | APath & 0x01),
                    (byte)(ACoords.FY << 1 | APath & 0x02),
                    (byte)(ACoords.FZ << 1 | APath & 0x04));
            }
        }
        #endregion

        #region Coordinate math functions
        public static ChunkSpaceCoords Add(
            ChunkSpaceCoords ALeft, ChunkSpaceCoords ARight)
        {
            return ALeft.Level < ARight.Level
                ? ChunkSpaceCoords.Unchecked_Add(ARight, ALeft)
                : ChunkSpaceCoords.Unchecked_Add(ALeft, ARight);
        }
        public static ChunkSpaceCoords StepUp(
            ChunkSpaceCoords ACoords,
            byte ASteps = 1)
        {
            return ChunkSpaceCoords.Unchecked_StepUp(ACoords, Math.Min((byte)ACoords.Level, ASteps));
        }
        public static ChunkSpaceCoords StepDown(
            ChunkSpaceCoords ACoords,
            byte APath = 0x00)
        {
            return ACoords.Level == ChunkSpaceLevel.Voxel
                ? ACoords
                : ChunkSpaceCoords.StepDown(ACoords, APath);
        }
        public static ChunkSpaceCoords StepDown(
            ChunkSpaceCoords ACoords,
            IEnumerable<byte> APaths)
        {
            return APaths.Aggregate(ACoords, ChunkSpaceCoords.StepDown);
        }
        #endregion

        private readonly ChunkSpaceLevel FLevel;

        private readonly byte FX;
        private readonly byte FY;
        private readonly byte FZ;

        public ChunkSpaceCoords(
            ChunkSpaceLevel ALevel,
            byte AX, byte AY, byte AZ)
        {
            this.FLevel = ALevel;

            this.FX = AX;
            this.FY = AY;
            this.FZ = AZ;
        }

        #region System.Object overrides
        public override bool Equals(object AOther)
        {
            if (AOther is ChunkSpaceCoords)
                return this.Equals((ChunkSpaceCoords)AOther);

            return false;
        }
        public override int GetHashCode()
        {
            return this.AsInt32;
        }
        public override string ToString()
        {
            return this.ToString(null, null);
        }
        #endregion

        #region IEquatable<ChunkSpaceCoords>
        public bool Equals(ChunkSpaceCoords AOther)
        {
            return this.FLevel == AOther.FLevel
                   && this.FX == AOther.FX
                   && this.FY == AOther.FY
                   && this.FZ == AOther.FZ;
        }
        #endregion

        #region IComparable<ChunkSpaceCoords>
        public int CompareTo(ChunkSpaceCoords AOther)
        {
            if (this.FLevel != AOther.FLevel)
                return this.FLevel - AOther.FLevel;
            if (this.FZ != AOther.FZ)
                return this.FZ - AOther.FZ;
            if (this.FY != AOther.FY)
                return this.FY - AOther.FY;

            return this.FX - AOther.FX;
        }
        #endregion

        #region IFormattable
        public string ToString(string AFormat, IFormatProvider AFormatProvider)
        {
            AFormat = AFormat != null ? AFormat.ToUpperInvariant() : "G";

            switch (AFormat)
            {
                case "G":
                    return String.Format(AFormatProvider, "({0}, {1}|{2}|{3})", (byte)this.FLevel, this.FX, this.FY, this.FZ);

                default:
                    throw new FormatException(String.Format("Invalid format specifier \"{0}\".", AFormat));
            }
        }
        #endregion

        public ChunkSpaceLevel Level
        {
            get { return this.FLevel; }
        }

        public Int32 AsInt32
        {
            get 
            {
                return this.FX
                       | this.FY << 8
                       | this.FZ << 16
                       | (byte)this.FLevel << 24;
            }
        }

        public byte X
        {
            get { return this.FX; }
        }
        public byte Y
        {
            get { return this.FY; }
        }
        public byte Z
        {
            get { return this.FZ; }
        }
    
        #region Static operator overloads
        public static ChunkSpaceCoords operator +(
            ChunkSpaceCoords ALeft,
            ChunkSpaceCoords ARight)
        {
            return ChunkSpaceCoords.Add(ALeft, ARight);
        }
        public static ChunkSpaceCoords operator +(
            ChunkSpaceCoords ALeft,
            byte ARight)
        {
            return ChunkSpaceCoords.StepDown(ALeft, ARight);
        }

        public static bool operator ==(
            ChunkSpaceCoords ALeft,
            ChunkSpaceCoords ARight)
        {
            return ALeft.Equals(ARight);
        }
        public static bool operator !=(
            ChunkSpaceCoords ALeft,
            ChunkSpaceCoords ARight)
        {
            return !ALeft.Equals(ARight);
        }

        public static bool operator <(
            ChunkSpaceCoords ALeft,
            ChunkSpaceCoords ARight)
        {
            return ALeft.CompareTo(ARight) < 0;
        }
        public static bool operator <=(
            ChunkSpaceCoords ALeft,
            ChunkSpaceCoords ARight)
        {
            return ALeft.CompareTo(ARight) <= 0;
        }
        public static bool operator >(
            ChunkSpaceCoords ALeft,
            ChunkSpaceCoords ARight)
        {
            return ALeft.CompareTo(ARight) > 0;
        }
        public static bool operator >=(
            ChunkSpaceCoords ALeft,
            ChunkSpaceCoords ARight)
        {
            return ALeft.CompareTo(ARight) >= 0;
        }
        #endregion
    }
}