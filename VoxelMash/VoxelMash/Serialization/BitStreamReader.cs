using System;
using System.IO;

namespace VoxelMash.Serialization
{
    public class BitStreamReader : StreamAdapter
    {
        private byte FBuffer;
        private byte FShift;

        public BitStreamReader(
            Stream ABaseStream,
            bool APropagateDispose = false)
            : base(ABaseStream, APropagateDispose)
        {
            this.ResetBitPos();
        }

        protected void ResetBitPos()
        {
            this.FBuffer = 0x00;
            this.FShift = 0;
        }

        public int ReadBits(byte ACount, out int ABits)
        {
            if (ACount > 32)
                throw new ArgumentOutOfRangeException("ACount");

            ABits = 0x00000000;
            int iRead = 0;

            while (ACount > 0)
            {
                if (this.FShift == 0)
                {
                    int iNew = this.FBaseStream.ReadByte();
                    if (iNew == -1)
                        return iRead;

                    this.FShift = 8;
                    this.FBuffer = (byte)iNew;
                    iRead++;
                }

                ACount--;
                this.FShift--;
                ABits |= ((this.FBuffer >> this.FShift) & 0x1) << ACount;
            }

            return iRead;
        }

        #region Stream
        public override int Read(byte[] ABuffer, int AOffset, int ACount)
        {
            for (int I = 0; I < ACount; I++)
            {
                int iBits;
                if (this.ReadBits(8, out iBits) != 8)
                    return I;
                ABuffer[AOffset + I] = (byte)(iBits & 0xFF);
            }

            return ACount;
        }
        public override void Write(byte[] ABuffer, int AOffset, int ACount)
        {
            throw new NotSupportedException();
        }
        public override long Seek(long AOffset, SeekOrigin AOrigin)
        {
            long iPos = this.FBaseStream.Seek(AOffset, AOrigin);
            this.ResetBitPos();
            return iPos;
        }
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override bool CanWrite
        {
            get { return false; }
        }
        #endregion

        public byte BitPos
        {
            get { return (byte)(7 - this.FShift); }
        }
    }
}
