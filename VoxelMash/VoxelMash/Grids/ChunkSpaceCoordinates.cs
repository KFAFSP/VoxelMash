using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace VoxelMash.Grids
{
    public struct ChunkSpaceCoords :
        IEquatable<ChunkSpaceCoords>,
        IComparable<ChunkSpaceCoords>,
        IFormattable
    {
        private static readonly Regex _FCanonicRegex = new Regex(@"^\((?<Level>[0-8]), (?<X>[0-9]+)\|(?<Y>[0-9]+)\|(?<Z>[0-9]+)\)$");

        #region Unchecked coordinate math functions
        private static ChunkSpaceCoords Unchecked_Add(
            ChunkSpaceCoords ALeft,
            ChunkSpaceCoords ARight)
        {
            unchecked
            {
                byte bDiff = (byte)((byte)ALeft.FLevel - (byte)ARight.FLevel);

                return new ChunkSpaceCoords(
                    (ChunkSpaceLevel)Math.Max((byte)ALeft.FLevel, (byte)ARight.Level),
                    (byte)((ARight.FX << bDiff) + ALeft.FX),
                    (byte)((ARight.FY << bDiff) + ALeft.FY),
                    (byte)((ARight.FZ << bDiff) + ALeft.FZ));
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
                    (byte)((ACoords.FX << 1) | APath & 0x01),
                    (byte)((ACoords.FY << 1) | ((APath & 0x02) >> 1)),
                    (byte)((ACoords.FZ << 1) | ((APath & 0x04) >> 2)));
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
                : ChunkSpaceCoords.Unchecked_StepDown(ACoords, APath);
        }
        public static ChunkSpaceCoords StepDown(
            ChunkSpaceCoords ACoords,
            IEnumerable<byte> APaths)
        {
            if (APaths == null)
                throw new ArgumentNullException("APaths");

            return APaths.Aggregate(ACoords, ChunkSpaceCoords.StepDown);
        }
        #endregion

        #region Tree operations
        public static bool IsParent(
            ChunkSpaceCoords AParent,
            ChunkSpaceCoords AChild)
        {
            if (AParent.FLevel >= AChild.FLevel)
                return false;

            unchecked
            {
                byte bDiff = (byte)((byte)AChild.FLevel - (byte)AParent.FLevel);

                return AChild.FX >> bDiff == AParent.FX
                       && AChild.FY >> bDiff == AParent.FY
                       && AChild.FZ >> bDiff == AParent.FZ;
            }
        }

        public static IEnumerable<ChunkSpaceCoords> EnumerateChildren(
            ChunkSpaceCoords ACoords,
            ChunkSpaceLevel AToLevel = ChunkSpaceLevel.Level8)
        {
            if (ACoords.FLevel >= ChunkSpaceLevel.Level8)
                return Enumerable.Empty<ChunkSpaceCoords>();

            ChunkSpaceCoords[] aChildren = Enumerable.Range(0, 8)
                .Select(APath => ACoords.StepDown((byte)APath))
                .ToArray();

            return aChildren
                .Concat(aChildren
                    .SelectMany(AChild => ChunkSpaceCoords.EnumerateChildren(AChild, AToLevel)));
        }

        public static IEnumerable<byte> GetRootPath(
            ChunkSpaceCoords ACoords)
        {
            if (ACoords.FLevel == ChunkSpaceLevel.Level0)
                yield break;

            unchecked
            {
                for (int iLevel = (byte)ACoords.FLevel - 1; iLevel >= 0; iLevel--)
                {
                    yield return (byte)(((ACoords.FX >> iLevel) & 0x01)
                                        | (((ACoords.FY >> iLevel) & 0x01) << 1)
                                        | (((ACoords.FZ >> iLevel) & 0x01) << 2));
                }
            }
        }
        #endregion

        #region Serialization functions
        public static byte[] ToBytes(ChunkSpaceCoords ACoords)
        {
            unchecked
            {
                if (ACoords.FLevel == ChunkSpaceLevel.Level8)
                {
                    byte[] aBytes = BitConverter.GetBytes(ACoords.AsInt32);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(aBytes);

                    return aBytes;
                }

                if (ACoords.FLevel > ChunkSpaceLevel.Level4)
                {
                    return new []
                    {
                        (byte)(((byte)ACoords.FLevel << 5) | (ACoords.FZ >> 2)),
                        (byte)(((ACoords.FZ & 0x03) << 6) | (ACoords.FY >> 1)),
                        (byte)(((ACoords.FY & 0x01) << 7) | ACoords.FX)
                    };
                }

                return new []
                {
                    (byte)(((byte)ACoords.FLevel << 4) | ACoords.FZ),
                    (byte)((ACoords.FY << 4) | ACoords.FX)
                };
            }
        }
        public static string ToCanonic(ChunkSpaceCoords ACoords)
        {
            return ACoords.ToString("C", CultureInfo.InvariantCulture);
        }

        public static ChunkSpaceCoords FromBytes(byte[] ABytes)
        {
            if (ABytes == null)
                throw new ArgumentNullException("ABytes");

            unchecked
            {
                switch (ABytes.Length)
                {
                    case 2:
                        return new ChunkSpaceCoords(
                            (ChunkSpaceLevel)(ABytes[0] >> 4),
                            (byte)(ABytes[1] & 0x0F),
                            (byte)(ABytes[1] >> 4),
                            (byte)(ABytes[0] & 0x0F));
                    case 3:
                        return new ChunkSpaceCoords(
                            (ChunkSpaceLevel)(ABytes[0] >> 5),
                            (byte)(ABytes[2] & 0x7F),
                            (byte)((ABytes[2] >> 7) | ((ABytes[1] & 0x3F) << 1)),
                            (byte)((ABytes[1] >> 6) | ((ABytes[0] & 0x1F) << 2)));

                    case 4:
                        return new ChunkSpaceCoords(
                            (ChunkSpaceLevel)ABytes[0],
                            ABytes[3],
                            ABytes[2],
                            ABytes[1]);

                    default:
                        throw new FormatException("Invalid coordinate byte format.");
                }
            }
        }
        public static ChunkSpaceCoords FromCanonic(string ACanonic)
        {
            if (ACanonic == null)
                throw new ArgumentNullException("ACanonic");

            Match mMatch = ChunkSpaceCoords._FCanonicRegex.Match(ACanonic);
            if (!mMatch.Success)
                throw new FormatException("Input string is not canonic.");

            try
            {
                return new ChunkSpaceCoords(
                    (ChunkSpaceLevel)Byte.Parse(mMatch.Groups["Level"].Value),
                    Byte.Parse(mMatch.Groups["X"].Value),
                    Byte.Parse(mMatch.Groups["Y"].Value),
                    Byte.Parse(mMatch.Groups["Z"].Value));
            }
            catch
            {
                throw new FormatException("Invalid canonic chunk space coordinate string.");
            }
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
            byte bMask = unchecked((byte)(0xFF >> (byte)(8 - ALevel)));

            this.FX = (byte)(AX & bMask);
            this.FY = (byte)(AY & bMask);
            this.FZ = (byte)(AZ & bMask);
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
            return this.ToString(null);
        }
        #endregion

        #region IEquatable<ChunkSpaceCoords>
        [Pure]
        public bool Equals(ChunkSpaceCoords AOther)
        {
            return this.FLevel == AOther.FLevel
                   && this.FX == AOther.FX
                   && this.FY == AOther.FY
                   && this.FZ == AOther.FZ;
        }
        #endregion

        #region IComparable<ChunkSpaceCoords>
        [Pure]
        public int CompareTo(ChunkSpaceCoords AOther)
        {
            if (this.FLevel != AOther.FLevel)
                return (int)this.FLevel - (int)AOther.FLevel;
            if (this.FZ != AOther.FZ)
                return this.FZ - AOther.FZ;
            if (this.FY != AOther.FY)
                return this.FY - AOther.FY;

            return this.FX - AOther.FX;
        }
        #endregion

        #region IFormattable
        [Pure]
        public string ToString(string AFormat, IFormatProvider AFormatProvider = null)
        {
            AFormat = AFormat != null ? AFormat.ToUpperInvariant() : "G";

            switch (AFormat)
            {
                case "C":
                case "G":
                    return String.Format(AFormatProvider, "({0}, {1}|{2}|{3})", (byte)this.FLevel, this.FX, this.FY, this.FZ);

                case "L":
                    return String.Format(AFormat, "{0}", (byte)this.FLevel);
                case "LN":
                    return String.Format(AFormat, "{0}", this.FLevel);
                case "X":
                    return String.Format(AFormat, "{0}", this.FX);
                case "Y":
                    return String.Format(AFormat, "{0}", this.FY);
                case "Z":
                    return String.Format(AFormat, "{0}", this.FZ);

                default:
                    throw new FormatException(String.Format("Invalid format specifier \"{0}\".", AFormat));
            }
        }
        #endregion

        #region Operator shortcuts
        [Pure]
        public ChunkSpaceCoords Add(ChunkSpaceCoords AOther)
        {
            return ChunkSpaceCoords.Add(this, AOther);
        }

        [Pure]
        public ChunkSpaceCoords StepUp(byte ASteps = 1)
        {
            return ChunkSpaceCoords.StepUp(this, ASteps);
        }
        [Pure]
        public ChunkSpaceCoords StepDown(byte APath = 0x00)
        {
            return ChunkSpaceCoords.StepDown(this, APath);
        }
        [Pure]
        public ChunkSpaceCoords StepDown(IEnumerable<byte> APath)
        {
            return ChunkSpaceCoords.StepDown(this, APath);
        }

        [Pure]
        public bool IsParentOf(ChunkSpaceCoords AChild)
        {
            return ChunkSpaceCoords.IsParent(this, AChild);
        }
        [Pure]
        public bool IsChildOf(ChunkSpaceCoords AParent)
        {
            return ChunkSpaceCoords.IsParent(AParent, this);
        }

        [Pure]
        public IEnumerable<ChunkSpaceCoords> GetChildren(
            ChunkSpaceLevel AToLevel = ChunkSpaceLevel.Voxel)
        {
            return ChunkSpaceCoords.EnumerateChildren(this, AToLevel);
        }
        [Pure]
        public IEnumerable<byte> GetRootPath()
        {
            return ChunkSpaceCoords.GetRootPath(this);
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

        public ChunkSpaceCoords Parent
        {
            get { return this.StepUp(); }
        }
        public IEnumerable<ChunkSpaceCoords> Children
        {
            get { return this.GetChildren(); }
        }
        public IEnumerable<byte> RootPath
        {
            get { return this.GetRootPath(); }
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