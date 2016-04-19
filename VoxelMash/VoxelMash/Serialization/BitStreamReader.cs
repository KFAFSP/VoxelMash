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
            this.Reset();
        }

        public void Reset()
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
                    this.FShift = 8;
                    this.FBuffer = this.FBaseStream.SafeReadByte();
                    iRead++;
                }

                ACount--;
                this.FShift--;
                ABits |= ((this.FBuffer >> this.FShift) & 0x1) << ACount;
            }

            return iRead;
        }

        public byte BitPos
        {
            get { return (byte)(7 - this.FShift); }
        }
    }
}
