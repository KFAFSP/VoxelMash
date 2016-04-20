using System;
using System.IO;

namespace VoxelMash.Serialization
{
    public abstract class StreamAdapter : Stream
    {
        private bool FDisposed;
        private bool FPropagateDispose;
        protected readonly Stream FBaseStream;

        protected StreamAdapter(
            Stream ABaseStream,
            bool APropagateDispose = false)
        {
            this.FDisposed = false;
            this.FPropagateDispose = APropagateDispose;
            this.FBaseStream = ABaseStream;
        }
        ~StreamAdapter()
        {
            this.Dispose(false);
        }

        #region IDisposable
        protected override void Dispose(bool ADisposing)
        {
            if (this.FDisposed)
                throw new ObjectDisposedException("StreamAdapter");

            base.Dispose(ADisposing);

            this.FDisposed = true;
            if (this.FPropagateDispose)
                this.FBaseStream.Dispose();
        }
        
        public bool IsDisposed
        {
            get { return this.FDisposed; }
        }
        #endregion

        #region Stream
        public override bool CanRead
        {
            get { return this.FBaseStream.CanRead; }
        }
        public override bool CanSeek
        {
            get { return this.FBaseStream.CanSeek; }
        }
        public override bool CanWrite
        {
            get { return this.FBaseStream.CanWrite; }
        }

        public override long Length
        {
            get { return this.FBaseStream.Length; }
        }
        public override long Position
        {
            get { return this.FBaseStream.Position; }
            set { this.Seek(value, SeekOrigin.Begin); }
        }

        public override void SetLength(long AValue)
        {
            this.FBaseStream.SetLength(AValue);
        }
        #endregion

        public bool PropagateDispose
        {
            get { return this.FPropagateDispose; }
            set { this.FPropagateDispose = value; }
        }
        public Stream BaseStream
        {
            get { return this.FBaseStream; }
        }
    }
}