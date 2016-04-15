using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelMash.Grids
{
    public class GridChunk
    {
        public const ushort C_Empty = 0;

        private readonly SortedDictionary<ChunkSpaceCoords, ushort> FTerminals;

        public GridChunk()
        {
            this.FTerminals = new SortedDictionary<ChunkSpaceCoords, ushort>();
        }

        public ushort Get(ChunkSpaceCoords ACoords)
        {
            if (this.FTerminals.Count == 0)
                return GridChunk.C_Empty;

            ChunkSpaceCoords cscCheck = ACoords;
            do
            {
                ushort nValue;

                if (this.FTerminals.TryGetValue(cscCheck, out nValue))
                    return nValue;

                if (ACoords.Level == ChunkSpaceLevel.Chunk)
                    return GridChunk.C_Empty;

                ACoords = ACoords.StepUp();
            } while (true);
        }
    }
}
