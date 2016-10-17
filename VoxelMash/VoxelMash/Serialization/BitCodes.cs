using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace VoxelMash.Serialization
{
    public struct BitSequence : IEnumerable<bool>
    {
        public class BitSequenceEnumerator : IEnumerator<bool>
        {
            private readonly BitSequence FSequence;
            private readonly int FStartOffset;
            private readonly bool FReverse;

            private int FOffset;
            private bool FCurrent;

            public BitSequenceEnumerator(
                BitSequence ASequence,
                int AStartOffset,
                bool AReverse)
            {
                this.FSequence = ASequence;
                this.FStartOffset = AStartOffset;
                this.FReverse = AReverse;
            }

            public void Dispose()
            { }
            
            public bool MoveNext()
            {
                this.FOffset += this.FReverse ? -1 : 1;
                if (this.FOffset < 0 || this.FOffset >= this.FSequence.FLength)
                    return false;

                this.FCurrent = this.FSequence.Get(this.FOffset);
                return true;
            }
            public void Reset()
            {
                this.FCurrent = false;
                this.FOffset = this.FStartOffset + (this.FReverse ? -1 : 1);
            }

            public bool Current
            {
                get { return this.FCurrent; }
            }
            object IEnumerator.Current
            {
                get { return this.Current; }
            }
        }

        private uint FBits;
        private byte FLength;

        public BitSequence(
            byte ALength,
            uint ABits = 0x00000000)
        {
            if (ALength > 32)
                throw new ArgumentOutOfRangeException("ALength");

            this.FBits = ABits & (0xFFFFFFFF >> (32 - ALength));
            this.FLength = ALength;
        }

        public void Append(BitSequence ASequence)
        {
            if (this.FLength + ASequence.FLength > 32)
                throw new ArgumentException("Sequence is too long.");

            this.FLength += ASequence.FLength;
            this.FBits <<= ASequence.FLength;
            this.FBits |= ASequence.FBits;
        }
        public BitSequence Drop(byte ACount)
        {
            if (ACount > this.FLength)
                throw new ArgumentOutOfRangeException("ACount");

            this.FLength -= ACount;
            BitSequence bsDrop = new BitSequence(ACount, this.FBits);
            this.FBits >>= ACount;
            return bsDrop;
        }

        [Pure]
        public bool Get(int AOffset)
        {
            int iShift = AOffset > 0 ? this.FLength - AOffset : AOffset;
            if (iShift < 0 || iShift >= this.FLength)
                throw new ArgumentOutOfRangeException("AOffset");

            return (this.FBits & (1 << iShift)) > 0;
        }
        public void Set(int AOffset, bool AValue)
        {
            int iShift = AOffset > 0 ? this.FLength - AOffset : AOffset;
            if (iShift < 0 || iShift >= this.FLength)
                throw new ArgumentOutOfRangeException("AOffset");

            uint iMask = (uint)(1 << iShift);
            this.FBits = AValue ? this.FBits | iMask : this.FBits & ~iMask;
        }
        public void Flip(int AOffset)
        {
            this.Set(AOffset, !this.Get(AOffset));
        }

        public uint Bits
        {
            get { return this.FBits; }
        }
        public byte Length
        {
            get { return this.FLength; }
        }

        #region IEnumerable<bool>
        public IEnumerator<bool> GetEnumerator()
        {
            return new BitSequenceEnumerator(this, 0, false);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        #endregion

        public bool this[int AOffset]
        {
            get { return this.Get(AOffset); }
            set { this.Set(AOffset, value); }
        }

        public static implicit operator uint(BitSequence ASequence)
        {
            return ASequence.FBits;
        }
    }
}
