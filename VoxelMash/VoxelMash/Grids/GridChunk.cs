using System.Collections.Generic;

namespace VoxelMash.Grids
{
    public abstract class GridChunk
    {
        public const ushort C_EmptyMaterial = 0;

        protected readonly SortedDictionary<ChunkSpaceCoords, ushort> FTerminals;

        public GridChunk()
        {
            this.FTerminals = new SortedDictionary<ChunkSpaceCoords, ushort>();
        }

        public abstract ushort Get(ChunkSpaceCoords ACoords);
        public abstract int Set(ChunkSpaceCoords ACoords, ushort AValue);

        public void Clear()
        {
            this.FTerminals.Clear();
        }

        public int TerminalCount
        {
            get { return this.FTerminals.Count; }
        }
    }
}
