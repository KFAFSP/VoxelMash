using System;
using System.IO;

namespace VoxelMash.Serialization
{
    public abstract class StreamAdapter : IDisposable
    {
        private bool FDisposed;
        private bool FPropagateDispose;
        protected readonly Stream FBaseStream;

        public StreamAdapter(
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
        protected virtual void Dispose(bool ADisposing)
        {
            if (this.FDisposed)
                return;

            this.FDisposed = true;
            if (this.FPropagateDispose)
                this.FBaseStream.Dispose();
        }
        public void Dispose()
        {
            this.Dispose(true);
        }

        public bool IsDisposed
        {
            get { return this.FDisposed; }
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