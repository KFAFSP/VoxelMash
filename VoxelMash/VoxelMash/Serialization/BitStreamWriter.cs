using System;
using System.IO;

namespace VoxelMash.Serialization
{
    public class BitStreamWriter : StreamAdapter
    {
        private byte FBuffer;
        private byte FShift;

        public BitStreamWriter(
            Stream ABaseStream,
            bool APropagateDispose = false)
            : base(ABaseStream, APropagateDispose)
        {
            this.Reset();
        }

        protected override void Dispose(bool ADisposing)
        {
            if (ADisposing)
                this.FinalizeByte();

            base.Dispose(ADisposing);
        }

        public void Reset()
        {
            this.FBuffer = 0x00;
            this.FShift = 8;
        }
        public void FinalizeByte()
        {
            if (this.FShift < 8)
            {
                this.FBaseStream.WriteByte(this.FBuffer);
                this.Reset();
            }
        }

        public int WriteBits(int ABits, byte ACount)
        {
            if (ACount > 32)
                throw new ArgumentOutOfRangeException("ACount");

            int iWritten = 0;
            while (ACount > 0)
            {
                ACount--;
                this.FShift--;
                this.FBuffer |= (byte)(((ABits >> ACount) & 0x1) << this.FShift);

                if (this.FShift == 0)
                {
                    this.FBaseStream.WriteByte(this.FBuffer);
                    iWritten++;
                    this.FBuffer = 0x00;
                    this.FShift = 8;
                }
            }

            return iWritten;
        }

        public void WriteByte(byte AByte)
        {
            this.WriteBits(AByte, 8);
        }

        public byte BitPos
        {
            get { return (byte)(8 - this.FShift); }
        }
    }
}