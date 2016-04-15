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

        public static ChunkSpaceCoords Root
        {
            get { return new ChunkSpaceCoords(ChunkSpaceLevel.Chunk, 0, 0, 0); }
        }

        #region Unchecked coordinate math functions
        private static void Unchecked_Increment(
            ref ChunkSpaceCoords ALeft,
            ChunkSpaceCoords ARight)
        {
            unchecked
            {
                byte bDiff = (byte)((byte)ALeft.FLevel - (byte)ARight.FLevel);

                ALeft.FLevel = (ChunkSpaceLevel)Math.Max((byte)ALeft.FLevel, (byte)ARight.FLevel);
                ALeft.FX += (byte)(ARight.FX << bDiff);
                ALeft.FY += (byte)(ARight.FY << bDiff);
                ALeft.FZ += (byte)(ARight.FZ << bDiff);
            }
        }
        private static void Unchecked_StepUp(
            ref ChunkSpaceCoords ACoords,
            byte ASteps)
        {
            if (ASteps == 0)
                return;

            unchecked
            {
                ACoords.FLevel = (ChunkSpaceLevel)Math.Max(0, (byte)ACoords.FLevel - ASteps);
                ACoords.FX >>= ASteps;
                ACoords.FY >>= ASteps;
                ACoords.FZ >>= ASteps;
            }
        }
        private static void Unchecked_StepDown(
            ref ChunkSpaceCoords ACoords,
            byte APath)
        {
            unchecked
            {
                ACoords.FLevel = (ChunkSpaceLevel)Math.Min((byte)ACoords.FLevel + 1, 8);
                ACoords.FX = (byte)((ACoords.FX << 1) | APath & 0x01);
                ACoords.FY = (byte)((ACoords.FY << 1) | ((APath & 0x02) >> 1));
                ACoords.FZ = (byte)((ACoords.FZ << 1) | ((APath & 0x04) >> 2));
            }
        }
        #endregion

        #region Coordinate math functions
        public static void Increment(
            ref ChunkSpaceCoords ALeft,
            ChunkSpaceCoords ARight)
        {
            if (ALeft.FLevel < ARight.FLevel)
            {
                ChunkSpaceCoords cscSwap = ALeft;
                ALeft = ARight;
                ARight = cscSwap;
            }
            
            ChunkSpaceCoords.Unchecked_Increment(ref ALeft, ARight);
        }
        public static void StepUp(
            ref ChunkSpaceCoords ACoords,
            byte ASteps = 1)
        {
            ChunkSpaceCoords.Unchecked_StepUp(ref ACoords, Math.Min((byte)ACoords.FLevel, ASteps));
        }
        public static void StepDown(
            ref ChunkSpaceCoords ACoords,
            byte APath = 0x00)
        {
            if (ACoords.FLevel == ChunkSpaceLevel.Voxel)
                return;

            ChunkSpaceCoords.Unchecked_StepDown(ref ACoords, APath);
        }
        public static void StepDown(
            ref ChunkSpaceCoords ACoords,
            IEnumerable<byte> APaths)
        {
            if (APaths == null)
                throw new ArgumentNullException("APaths");

            foreach (byte bPath in APaths)
                ChunkSpaceCoords.StepDown(ref ACoords, bPath);
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
                .Select(APath => ACoords.GetChild((byte)APath))
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
                int iLength = ACoords.ByteSize;

                switch (iLength)
                {
                    case 2:
                        return new[]
                        {
                            (byte)(((byte)ACoords.FLevel << 4) | ACoords.FZ),
                            (byte)((ACoords.FY << 4) | ACoords.FX)
                        };

                    case 3:
                        return new[]
                        {
                            (byte)(((byte)ACoords.FLevel << 5) | (ACoords.FZ >> 2)),
                            (byte)(((ACoords.FZ & 0x03) << 6) | (ACoords.FY >> 1)),
                            (byte)(((ACoords.FY & 0x01) << 7) | ACoords.FX)
                        };

                    case 4:
                        byte[] aBytes = BitConverter.GetBytes(ACoords.AsInt32);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(aBytes);

                        return aBytes;

                    default:
                        return null;
                }
            }
        }
        public static string ToCanonic(ChunkSpaceCoords ACoords)
        {
            return ACoords.ToString("C", CultureInfo.InvariantCulture);
        }

        private static int GetLengthType(byte AFirst)
        {
            if (AFirst == 0x08)
                return 4;

            if (AFirst >> 7 == 1)
                return 3;

            return 2;
        }

        public static ChunkSpaceCoords FromBytes(
            byte[] ABytes,
            int AOffset = 0)
        {
            if (ABytes == null)
                throw new ArgumentNullException("ABytes");

            unchecked
            {
                int iLength = ChunkSpaceCoords.GetLengthType(ABytes[0]);

                switch (iLength)
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

        private ChunkSpaceLevel FLevel;

        private byte FX;
        private byte FY;
        private byte FZ;

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

        #region Impure operator shortcuts
        public void Add(ChunkSpaceCoords AOther)
        {
            ChunkSpaceCoords.Increment(ref this, AOther);
        }

        public void StepUp(byte ASteps = 1)
        {
            ChunkSpaceCoords.StepUp(ref this, ASteps);
        }
        public void StepDown(byte APath = 0x00)
        {
            ChunkSpaceCoords.StepDown(ref this, APath);
        }
        public void StepDown(IEnumerable<byte> APath)
        {
            ChunkSpaceCoords.StepDown(ref this, APath);
        }
        #endregion

        #region Pure operator shortcuts
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
        public ChunkSpaceCoords GetParent()
        {
            ChunkSpaceCoords cscResult = this;
            cscResult.StepUp();
            return cscResult;
        }
        [Pure]
        public ChunkSpaceCoords GetChild(byte APath = 0x00)
        {
            ChunkSpaceCoords cscResult = this;
            cscResult.StepDown(APath);
            return cscResult;
        }
        [Pure]
        public ChunkSpaceCoords GetChild(byte[] APaths)
        {
            ChunkSpaceCoords cscResult = this;
            cscResult.StepDown(APaths);
            return cscResult;
        }
        [Pure]
        public IEnumerable<ChunkSpaceCoords> GetChildren(
            ChunkSpaceLevel AToLevel = ChunkSpaceLevel.Voxel)
        {
            return ChunkSpaceCoords.EnumerateChildren(this, AToLevel);
        }

        [Pure]
        public ChunkSpaceCoords GetFirstChild(
            ChunkSpaceLevel AToLevel = ChunkSpaceLevel.Voxel)
        {
            ChunkSpaceCoords cscChild = this;
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (cscChild.FLevel < AToLevel)
                cscChild.StepDown(0x00);

            return cscChild;
        }
        [Pure]
        public ChunkSpaceCoords GetLastChild(
            ChunkSpaceLevel AToLevel = ChunkSpaceLevel.Voxel)
        {
            ChunkSpaceCoords cscChild = this;
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (cscChild.FLevel < AToLevel)
                cscChild.StepDown(0x07);

            return cscChild;
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
        public int ByteSize
        {
            get
            {
                if (this.FLevel == ChunkSpaceLevel.Level8)
                    return 4;
                if (this.FLevel > ChunkSpaceLevel.Level4)
                    return 3;
                return 2;
            }
        }
        public int Volume
        {
            get { return 1 << ((8 - (byte)this.FLevel) * 3); }
        }

        public ChunkSpaceCoords Parent
        {
            get { return this.GetParent(); }
        }
        
        public IEnumerable<ChunkSpaceCoords> Children
        {
            get { return this.GetChildren(); }
        }
        public ChunkSpaceCoords FirstChild
        {
            get { return this.GetFirstChild(); }
        }
        public ChunkSpaceCoords LastChild
        {
            get { return this.GetLastChild(); }
        }

        public byte Path
        {
            get
            {
                return (byte)(this.FX & 0x1
                              | (((this.FY >> 1) & 0x1) << 1)
                              | (((this.FZ >> 1) & 0x1) << 2));
            }
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
            ChunkSpaceCoords cscResult = ALeft;
            cscResult.Add(ARight);
            return cscResult;
        }
        public static ChunkSpaceCoords operator +(
            ChunkSpaceCoords ALeft,
            byte ARight)
        {
            ChunkSpaceCoords cscResult = ALeft;
            cscResult.StepDown(ARight);
            return cscResult;
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