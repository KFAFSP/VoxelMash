using System.Collections.Generic;
using System.Linq;

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

        protected abstract void ExpandToHere(
            ChunkSpaceCoords ACoords,
            ushort AValue);
        protected abstract bool CollapseThis(
            ChunkSpaceCoords ACoords,
            ushort AValue);

        public virtual ushort Get(ChunkSpaceCoords ACoords)
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
        public virtual int Set(ChunkSpaceCoords ACoords, ushort AValue)
        {
            int iBalance = 0;
            if (ACoords.Level != ChunkSpaceLevel.Voxel)
            {
                List<ChunkSpaceCoords> lRemove = new List<ChunkSpaceCoords>();
                this.FTerminals
                    .SkipWhile(APair => APair.Key <= ACoords)
                    .TakeWhile(APair => APair.Key.IsChildOf(ACoords))
                    .ForEach(APair =>
                    {
                        if (APair.Value == AValue)
                            // ReSharper disable once AccessToModifiedClosure
                            iBalance -= APair.Key.Volume;

                        lRemove.Add(APair.Key);
                    });

                lRemove.ForEach(AKey => this.FTerminals.Remove(AKey));
            }

            if (this.Get(ACoords) == AValue)
                return 0;

            this.ExpandToHere(ACoords, AValue);

            this.FTerminals[ACoords] = AValue;
            iBalance += ACoords.Volume;

            ACoords.StepUp();
            this.CollapseThis(ACoords, AValue);

            return iBalance;
        }

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
