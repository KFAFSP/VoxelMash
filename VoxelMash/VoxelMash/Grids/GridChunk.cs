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

            do
            {
                ushort nValue;

                if (this.FTerminals.TryGetValue(ACoords, out nValue))
                    return nValue;

                if (ACoords.Level == ChunkSpaceLevel.Chunk)
                    return GridChunk.C_EmptyMaterial;

                ACoords.StepUp();
            } while (true);
        }

        public void StrictExpandHere(ChunkSpaceCoords ANode)
        {
            if (ANode.Level == ChunkSpaceLevel.Chunk)
                return;

            byte[] aPath = ANode.GetRootPath().ToArray();
            ChunkSpaceCoords cscCurrent = ChunkSpaceCoords.Root;

            for (int I = 0; I < aPath.Length - 1; I++)
            {
                ushort nValue;
                if (this.FTerminals.TryGetValue(cscCurrent, out nValue))
                {
                    this.FTerminals.Remove(cscCurrent);
                    for (byte bPath = 0; bPath < 8; bPath++)
                        if (bPath != aPath[I + 1])
                            this.FTerminals[cscCurrent.GetChild(bPath)] = nValue;
                }
            }
        }

        public bool StrictCollapseThis(
            ChunkSpaceCoords ANode,
            ushort AValue)
        {
            if (ANode.Level == ChunkSpaceLevel.Voxel)
                return false;

            List<ChunkSpaceCoords> lRemove = new List<ChunkSpaceCoords>();

            for (byte bPath = 0; bPath < 8; bPath++)
            {
                ChunkSpaceCoords cscChild = ANode.GetChild(bPath);
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
            ANode.StepUp();
            this.StrictCollapseThis(ANode, AValue);
            return true;
        }
    }
}
