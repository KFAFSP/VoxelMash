using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace VoxelMash.Grids
{
    public struct ChunkSpaceCoordinates :
        IEquatable<ChunkSpaceCoordinates>,
        IComparable<ChunkSpaceCoordinates>
    {
        public static ChunkSpaceCoordinates Root { get { return new ChunkSpaceCoordinates(8, 0, 0, 0); } }

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
        public ChunkSpaceCoordinates GetParent(byte AOrder = 0x1)
        {
            ChunkSpaceCoordinates cscParent = this;
            cscParent.StepUp(AOrder);
            return cscParent;
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
            int iThis = (this.FZ << 16 | this.FY << 8 | this.FX) << this.FShift;
            int iOther = (AOther.FZ << 16 | AOther.FY << 8 | AOther.FX) << AOther.FShift;
            int iMask = 0x00808080;

            int iLevelDiff = this.FShift - AOther.FShift;
            byte bMax = (byte)(8 - (iLevelDiff > 0 ? this.FShift : AOther.FShift));

            for (byte bShift = 0; bShift < bMax; bShift++)
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
