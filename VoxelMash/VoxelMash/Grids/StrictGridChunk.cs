using System.Collections.Generic;
using System.Linq;

namespace VoxelMash.Grids
{
    public class StrictGridChunk : GridChunk
    {
        protected override void ExpandToHere(
            ChunkSpaceCoords ANode,
            ushort AValue)
        {
            if (ANode.Level == ChunkSpaceLevel.Chunk)
                return;

            byte[] aPath = ANode.GetRootPath().ToArray();
            ChunkSpaceCoords cscCurrent = ChunkSpaceCoords.Root;

            int I = 0;
            do
            {
                ushort nValue;
                if (this.FTerminals.TryGetValue(cscCurrent, out nValue))
                {
                    this.FTerminals.Remove(cscCurrent);
                    for (byte bPath = 0; bPath < 8; bPath++)
                        if (bPath != aPath[I])
                            this.FTerminals[cscCurrent.GetChild(bPath)] = nValue;
                }

                cscCurrent.StepDown(aPath[I]);
                I++;
            } while (I < aPath.Length);
        }
        protected override bool CollapseThis(
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
            this.CollapseThis(ANode, AValue);
            return true;
        }
    }
}