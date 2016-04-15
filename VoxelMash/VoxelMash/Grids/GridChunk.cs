using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelMash.Grids
{
    public class GridChunk
    {
        public const ushort C_EmptyMaterial = 0;

        private readonly SortedDictionary<ChunkSpaceCoords, ushort> FTerminals;

        public GridChunk()
        {
            this.FTerminals = new SortedDictionary<ChunkSpaceCoords, ushort>();
        }

        public ushort Get(ChunkSpaceCoords ACoords)
        {
            if (this.FTerminals.Count == 0)
                return GridChunk.C_EmptyMaterial;

            ChunkSpaceCoords cscCheck = ACoords;
            do
            {
                ushort nValue;

                if (this.FTerminals.TryGetValue(cscCheck, out nValue))
                    return nValue;

                if (ACoords.Level == ChunkSpaceLevel.Chunk)
                    return GridChunk.C_EmptyMaterial;

                ACoords = ACoords.StepUp();
            } while (true);
        }

        public bool StrictCollapseThis(
            ChunkSpaceCoords ANode,
            ushort AValue)
        {
            List<ChunkSpaceCoords> lRemove = new List<ChunkSpaceCoords>();

            for (byte bPath = 0; bPath < 8; bPath++)
            {
                ChunkSpaceCoords cscChild = ANode.StepDown(bPath);
                ushort nChild;
                if (!this.FTerminals.TryGetValue(cscChild, out nChild))
                {
                    if (AValue == GridChunk.C_EmptyMaterial)
                    {
                        ChunkSpaceCoords cscLast = cscChild.LastChild;
                        if (this.FTerminals.Keys
                            .SkipWhile(AKey => AKey <= cscChild)
                            .TakeWhile(AKey => AKey <= cscLast)
                            .Any())
                            return false;
                    }

                    return false;
                }

                if (nChild != AValue)
                    return false;

                lRemove.Add(cscChild);
            }

            lRemove.ForEach(AKey => this.FTerminals.Remove(AKey));
            this.FTerminals[ANode] = AValue;
            return true;
        }
    }
}
