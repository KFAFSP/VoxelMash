using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;

using VoxelMash.Serialization;

namespace VoxelMash.Grids
{
    public struct ChunkSpaceCoordinates :
        IEquatable<ChunkSpaceCoordinates>,
        IComparable<ChunkSpaceCoordinates>
    {
        public sealed class SerializationHandler
        {
            private readonly Stream FBaseStream;
            private bool FAllowPacking;

            public SerializationHandler(
                Stream ABaseStream,
                bool AAllowPacking = true)
            {
                if (ABaseStream == null)
                    throw new ArgumentNullException("ABaseStream");

                this.FBaseStream = ABaseStream;
                this.FAllowPacking = AAllowPacking;
            }

            public int Read(out ChunkSpaceCoordinates ACoords)
            {
                byte bFirst = this.FBaseStream.SafeReadByte();

                if ((bFirst & 0xF0) == 0x80)
                {
                    // Full length.
                    ACoords.FShift = (byte)(bFirst & 0x0F);                   
                    ACoords.FZ = this.FBaseStream.SafeReadByte();
                    ACoords.FY = this.FBaseStream.SafeReadByte();
                    ACoords.FX = this.FBaseStream.SafeReadByte();
                    return 4;
                }
                
                if (!this.FAllowPacking)                   
                    throw new FormatException("Packed data is not supported in this context.");

                if (bFirst == 0x00)
                {
                    // Chunk.
                    ACoords.FShift = 8;
                    ACoords.FX = 0;
                    ACoords.FY = 0;
                    ACoords.FZ = 0;
                    return 1;
                }

                if (bFirst == 0x60)
                {
                    // Out of range.
                    ACoords.FShift = 9;
                    ACoords.FX = 0;
                    ACoords.FY = 0;
                    ACoords.FZ = 0;
                    return 1;
                }

                byte bMagic = (byte)(bFirst & 0xE0);
                if (bMagic == 0x00)
                {
                    // Large block group.
                    ACoords.FShift = 7;
                    ACoords.FX = (byte)(bFirst & 0x1);
                    ACoords.FY = (byte)((bFirst >> 1) & 0x1);
                    ACoords.FZ = (byte)((bFirst >> 2) & 0x1);
                    return 1;
                }

                byte bSecond = this.FBaseStream.SafeReadByte();
                if (bMagic <= 0x40)
                {
                    // Up to blocks.
                    ACoords.FShift = (byte)(8 - (bFirst >> 4));
                    ACoords.FZ = (byte)(bFirst & 0x0F);
                    ACoords.FY = (byte)(bSecond >> 4);
                    ACoords.FX = (byte)(bSecond & 0x0F);
                    return 2;
                }

                // Excluding voxel.
                byte bThird = this.FBaseStream.SafeReadByte();
                ACoords.FShift = (byte)(8 - (bFirst >> 5));
                ACoords.FZ = (byte)(((bFirst & 0x1F) << 2) | (bSecond >> 6));
                ACoords.FY = (byte)(((bSecond & 0x3F) << 1) | (bThird >> 7));
                ACoords.FX = (byte)(bThird & 0x7F);
                return 3;
            }
            public int Write(ChunkSpaceCoordinates ACoords)
            {
                if (this.FAllowPacking && ACoords.FShift != 0)
                {
                    switch (ACoords.Shift)
                    {
                        case 1:
                        case 2:
                        case 3:
                            // Excluding voxels.
                            this.FBaseStream.WriteByte(
                                (byte)(((8 - ACoords.FShift) << 5)
                                       | (ACoords.FZ >> 2)));
                            this.FBaseStream.WriteByte(
                                (byte)(((ACoords.FZ & 0x3) << 6)
                                       | (ACoords.FY >> 1)));
                            this.FBaseStream.WriteByte(
                                (byte)(((ACoords.FY & 0x1) << 7)
                                       | ACoords.FX));
                            return 3;

                        case 4:
                        case 5:
                        case 6:
                            // Up to blocks.
                            this.FBaseStream.WriteByte(
                                (byte)(((8 - ACoords.FShift) << 4)
                                       | ACoords.FZ));
                            this.FBaseStream.WriteByte(
                                (byte)((ACoords.FY << 4)
                                       | ACoords.FX));
                            return 2;

                        case 7:
                            // Large block group.
                            this.FBaseStream.WriteByte(
                                (byte)(0x1
                                       | (ACoords.FZ << 2)
                                       | (ACoords.FY << 1)
                                       | ACoords.FX));
                            return 1;

                        case 8:
                            // Chunk.
                            this.FBaseStream.WriteByte(0x00);
                            return 1;

                        default:
                            // Out of range.
                            this.FBaseStream.WriteByte(0x60);
                            return 1;
                    }
                }

                // Anything.
                this.FBaseStream.WriteByte((byte)(0x80 | ACoords.FShift));
                this.FBaseStream.WriteByte(ACoords.FZ);
                this.FBaseStream.WriteByte(ACoords.FY);
                this.FBaseStream.WriteByte(ACoords.FX);
                return 4;
            }

            public Stream BaseStream
            {
                get { return this.FBaseStream; }
            }
            public bool AllowPacking
            {
                get { return this.FAllowPacking; }
                set { this.FAllowPacking = value; }
            }
        }

        #region Special coordinate constants
        public static ChunkSpaceCoordinates Root
        {
            get { return new ChunkSpaceCoordinates(8, 0, 0, 0); }
        }
        public static ChunkSpaceCoordinates FirstVoxel
        {
            get { return new ChunkSpaceCoordinates(0, 0, 0, 0); }
        }
        public static ChunkSpaceCoordinates LastVoxel
        {
            get { return new ChunkSpaceCoordinates(0, 255, 255, 255); }
        }
        public static ChunkSpaceCoordinates OutOfRange
        {
            get { return ChunkSpaceCoordinates.LastVoxel.GetLastChildPlusOne(); }
        }
        #endregion

        private byte FShift;
        private byte FX;
        private byte FY;
        private byte FZ;

        public ChunkSpaceCoordinates(
            byte AShift,
            byte AX, byte AY, byte AZ)
        {
            if (AShift > 8)
                throw new ArgumentException("Shift must be less or equal to 8.");

            this.FShift = AShift;
            byte bMask = (byte)(0xFF >> this.FShift);

            this.FX = (byte)(AX & bMask);
            this.FY = (byte)(AY & bMask);
            this.FZ = (byte)(AZ & bMask);
        }

        public void StepDown(byte APath)
        {
            if (this.FShift == 0)
                throw new InvalidOperationException("Cannot step down voxel address volume.");

            this.FShift--;
            this.FX = (byte)((this.FX << 1) | (APath & 0x1));
            this.FY = (byte)((this.FY << 1) | ((APath & 0x2) >> 1));
            this.FZ = (byte)((this.FZ << 1) | ((APath & 0x4) >> 2));
        }
        public bool StepUp(byte AAmount = 0x1)
        {
            AAmount = (byte)Math.Min(AAmount, 8 - this.FShift);
            if (AAmount == 0)
                return false;

            this.FShift += AAmount;
            this.FX >>= AAmount;
            this.FY >>= AAmount;
            this.FZ >>= AAmount;
            return true;
        }

        public void SetPath(byte APath)
        {
            if ((APath & 0x1) == 0x1)
                this.FX |= 0x01;
            else
                this.FX &= 0xFE;
            
            if ((APath & 0x2) == 0x2)
                this.FY |= 0x01;
            else
                this.FY &= 0xFE;

            if ((APath & 0x4) == 0x4)
                this.FZ |= 0x01;
            else
                this.FZ &= 0xFE;
        }

        public void MoveRight()
        {
            do
            {
                if (this.FShift == 8)
                {
                    this.FShift = 9;
                    return;
                }

                byte bPath = this.GetPath();
                if (bPath < 0x7)
                {
                    this.SetPath((byte)(bPath + 1));
                    return;
                }

                this.StepUp();
            } while (true);
        }
        public void MoveLeft()
        {
            do
            {
                if (this.FShift == 8)
                {
                    this.FShift = 9;
                    return;
                }

                byte bPath = this.GetPath();
                if (bPath > 0x0)
                {
                    this.SetPath((byte)(bPath - 1));
                    return;
                }

                this.StepUp();
            } while (true);
        }

        #region Pure methods
        [Pure]
        public bool IsParentOf(ChunkSpaceCoordinates AOther)
        {
            int iDiff = this.FShift - AOther.FShift;
            if (iDiff <= 0)
                return false;

            return AOther.FX >> iDiff == this.FX
                   && AOther.FY >> iDiff == this.FY
                   && AOther.FZ >> iDiff == this.FZ;
        }

        [Pure]
        public ChunkSpaceCoordinates GetOffset()
        {
            return new ChunkSpaceCoordinates(
                0,
                (byte)(this.FX << this.FShift),
                (byte)(this.FY << this.FShift),
                (byte)(this.FZ << this.FShift));
        }
        [Pure]
        public byte GetPath(byte AShift = 0x0)
        {
            return (byte)(((this.FX >> AShift) & 0x1)
                          | (((this.FY >> AShift) & 0x1) << 1)
                          | (((this.FZ >> AShift) & 0x1) << 2));
        }
        [Pure]
        public IEnumerable<byte> GetPath(bool AUpward, byte AStopShift = 8)
        {
            int iLimit = Math.Max(AStopShift - this.FShift, 0);
            for (byte bShift = 0; bShift < iLimit; bShift++)
                yield return this.GetPath(AUpward ? bShift : (byte)(iLimit - bShift - 1));
        }
        [Pure]
        public int GetIndex()
        {
            return this.FX
                   | (this.FY << 8)
                   | (this.FZ << 16)
                   | (this.FShift << 24);
        }
        [Pure]
        public ChunkSpaceCoordinates GetSibling(byte APath)
        {
            ChunkSpaceCoordinates cscSibling = this;
            cscSibling.SetPath(APath);
            return cscSibling;
        }

        [Pure]
        public ChunkSpaceCoordinates GetChild(byte APath)
        {
            ChunkSpaceCoordinates cscChild = this;
            cscChild.StepDown(APath);
            return cscChild;
        }
        [Pure]
        public ChunkSpaceCoordinates GetLastChild()
        {
            byte bMask = (byte)(0xFF >> (8 - this.FShift));
            return new ChunkSpaceCoordinates(
                0,
                (byte)(this.FX | bMask),
                (byte)(this.FY | bMask),
                (byte)(this.FZ | bMask));
        }
        [Pure]
        public ChunkSpaceCoordinates GetLastChildPlusOne()
        {
            ChunkSpaceCoordinates cscResult = this;
            do
            {
                byte bPath = cscResult.GetPath();
                if (bPath < 0x7)
                {
                    if (cscResult.FShift == 8)
                    {
                        cscResult.FShift = 9;
                        return cscResult;
                    }

                    cscResult.SetPath((byte)(bPath + 1));
                    return cscResult;
                }

                cscResult.StepUp();
            } while (true);
        }
        [Pure]
        public ChunkSpaceCoordinates GetParent(byte AOrder = 0x1)
        {
            ChunkSpaceCoordinates cscParent = this;
            cscParent.StepUp(AOrder);
            return cscParent;
        }

        [Pure]
        public ChunkSpaceCoordinates GetRight()
        {
            ChunkSpaceCoordinates cscRight = this;
            cscRight.MoveRight();
            return cscRight;
        }
        [Pure]
        public ChunkSpaceCoordinates GetLeft()
        {
            ChunkSpaceCoordinates cscLeft = this;
            cscLeft.MoveLeft();
            return cscLeft;
        }
        #endregion

        #region System.Object overrides
        public override bool Equals(object AOther)
        {
            if (AOther is ChunkSpaceCoordinates)
                return this.Equals((ChunkSpaceCoordinates)AOther);

            return false;
        }
        public override int GetHashCode()
        {
            return this.GetIndex();
        }
        public override string ToString()
        {
            return String.Format("({0}, {1}|{2}|{3})", this.FShift, this.FX, this.FY, this.FZ);
        }
        #endregion

        #region IEquatable<ChunkSpaceCoordinates>
        public bool Equals(ChunkSpaceCoordinates AOther)
        {
            return this.FShift == AOther.FShift
                   && this.FX == AOther.FX
                   && this.FY == AOther.FY
                   && this.FZ == AOther.FZ;
        }
        #endregion

        #region IComparable<ChunkSpaceCoordinates>
        public int CompareTo(ChunkSpaceCoordinates AOther)
        {
            int iOutOfRange = (this.FShift > 8 ? 1 : 0) - (AOther.FShift > 8 ? 1 : 0);
            if (iOutOfRange != 0)
                return iOutOfRange;

            int iThis = (this.FZ << 16 | this.FY << 8 | this.FX) << this.FShift;
            int iOther = (AOther.FZ << 16 | AOther.FY << 8 | AOther.FX) << AOther.FShift;
            int iMask = 0x00808080;

            int iLevelDiff = this.FShift - AOther.FShift;
            byte bMax = (byte)(8 - (iLevelDiff > 0 ? this.FShift : AOther.FShift));

            for (byte bShift = 0; bShift <= bMax; bShift++)
            {
                int iDiff = (iThis & (iMask >> bShift))
                            - (iOther & (iMask >> bShift));

                if (iDiff != 0)
                    return iDiff;
            }

            return -iLevelDiff;
        }
        #endregion

        public byte Mask
        {
            get { return (byte)(0xFF >> this.FShift); }
        }
        public byte Level
        {
            get { return (byte)(8 - this.FShift); }
        }
        public int Volume
        {
            get { return 1 << (this.FShift * 3); }
        }

        public byte Shift
        {
            get { return this.FShift; }
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

        public bool IsOutOfRange
        {
            get { return this.FShift > 8; }
        }
        public bool IsRoot
        {
            get { return this.FShift == 8; }
        }
        public bool IsBlock
        {
            get { return this.FShift == 4; }
        }
        public bool IsVoxel
        {
            get { return this.FShift == 0; }
        }

        #region Operator overloads
        public static bool operator ==(
            ChunkSpaceCoordinates ALeft,
            ChunkSpaceCoordinates ARight)
        {
            return ALeft.Equals(ARight);
        }
        public static bool operator !=(
            ChunkSpaceCoordinates ALeft,
            ChunkSpaceCoordinates ARight)
        {
            return !ALeft.Equals(ARight);
        }

        public static ChunkSpaceCoordinates operator +(
            ChunkSpaceCoordinates ALeft,
            byte ARight)
        {
            ALeft.StepDown(ARight);
            return ALeft;
        }
        public static ChunkSpaceCoordinates operator -(
            ChunkSpaceCoordinates ALeft,
            byte ARight)
        {
            ALeft.StepUp(ARight);
            return ALeft;
        }
        public static ChunkSpaceCoordinates operator |(
            ChunkSpaceCoordinates ALeft,
            byte APath)
        {
            ALeft.SetPath(APath);
            return ALeft;
        }

        public static ChunkSpaceCoordinates operator --(ChunkSpaceCoordinates ALeft)
        {
            ALeft.StepUp();
            return ALeft;
        }

        public static bool operator >(
            ChunkSpaceCoordinates ALeft,
            ChunkSpaceCoordinates ARight)
        {
            return ALeft.CompareTo(ARight) > 0;
        }
        public static bool operator <(
            ChunkSpaceCoordinates ALeft,
            ChunkSpaceCoordinates ARight)
        {
            return ALeft.CompareTo(ARight) < 0;
        }
        public static bool operator >=(
            ChunkSpaceCoordinates ALeft,
            ChunkSpaceCoordinates ARight)
        {
            return ALeft.CompareTo(ARight) >= 0;
        }
        public static bool operator <=(
            ChunkSpaceCoordinates ALeft,
            ChunkSpaceCoordinates ARight)
        {
            return ALeft.CompareTo(ARight) <= 0;
        }
        #endregion
    }
}
