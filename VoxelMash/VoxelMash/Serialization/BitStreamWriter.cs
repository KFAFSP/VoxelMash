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
            this.ResetBitPos();
        }

        #region IDisposable
        protected override void Dispose(bool ADisposing)
        {
            if (ADisposing)
                this.FinalizeByte();

            base.Dispose(ADisposing);
        }
        #endregion

        protected void ResetBitPos()
        {
            this.FBuffer = 0x00;
            this.FShift = 8;
        }
        protected void FinalizeByte()
        {
            if (this.FShift < 8)
            {
                this.FBaseStream.WriteByte(this.FBuffer);
                this.ResetBitPos();
            }
        }

        public void WriteBits(int ABits, byte ACount)
        {
            if (ACount > 32)
                throw new ArgumentOutOfRangeException("ACount");

            while (ACount > 0)
            {
                ACount--;
                this.FShift--;
                this.FBuffer |= (byte)(((ABits >> ACount) & 0x1) << this.FShift);

                if (this.FShift == 0)
                {
                    this.FBaseStream.WriteByte(this.FBuffer);
                    this.FBuffer = 0x00;
                    this.FShift = 8;
                }
            }
        }

        #region Stream
        public override int Read(byte[] ABuffer, int AOffset, int ACount)
        {
            throw new NotSupportedException();
        }
        public override void Write(byte[] ABuffer, int AOffset, int ACount)
        {
            for (int I = 0; I < ACount; I++)
            {
                int iWrite = 0x00000000 | ABuffer[AOffset + I];
                this.WriteBits(iWrite, 8);
            }
        }
        public override long Seek(long AOffset, SeekOrigin AOrigin)
        {
            long iPos = this.FBaseStream.Seek(AOffset, AOrigin);
            this.ResetBitPos();
            return iPos;
        }
        public override void Flush()
        {
            this.FinalizeByte();
            this.FBaseStream.Flush();
        }

        public override bool CanRead
        {
            get { return false; }
        }
        #endregion

        public byte BitPos
        {
            get { return (byte)(8 - this.FShift); }
        }
    }
}