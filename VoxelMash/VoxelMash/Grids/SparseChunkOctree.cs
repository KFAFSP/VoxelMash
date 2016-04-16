using System.Collections.Generic;
using System.Linq;

using Coords = VoxelMash.Grids.ChunkSpaceCoordinates;

namespace VoxelMash.Grids
{
    public class SparseChunkOctree : ChunkOctree
    {
        private readonly SortedDictionary<Coords, ushort> FTerminals;

        public SparseChunkOctree()
        {
            this.FTerminals = new SortedDictionary<Coords, ushort>();
        }

        protected void Expand(Coords ACoords)
        {
            byte bIgnore = ACoords.GetPath();
            while (ACoords.StepUp())
            {
                ushort nGet;
                if (this.FTerminals.TryGetValue(ACoords, out nGet))
                {
                    
                    this.FTerminals.Remove(ACoords);

                    for (byte bPath = 0; bPath < 8; bPath++)
                        if (bPath != bIgnore)
                            this.FTerminals[ACoords.GetChild(bPath)] = nGet;
                }
                else
                    return;

                bIgnore = ACoords.GetPath();
            }
        }
        protected void Terminate(Coords ACoords, ushort AValue, ref int ABalance)
        {
            ABalance += ACoords.Volume;

            List<Coords> lRemove = new List<Coords>();
            foreach (KeyValuePair<Coords, ushort> kvpPair in this.FTerminals
                .SkipWhile(APair => APair.Key <= ACoords)
                .TakeWhile(APair => ACoords.IsParentOf(APair.Key)))
            {
                if (kvpPair.Value == AValue)
                    ABalance -= kvpPair.Key.Volume;

                lRemove.Add(kvpPair.Key);
            }

            lRemove.ForEach(AKey => this.FTerminals.Remove(AKey));
            if (AValue == ChunkOctree.C_EmptyMaterial)
                this.FTerminals.Remove(ACoords);
            else
                this.FTerminals[ACoords] = AValue;
        }
        protected bool Collapse(Coords ACoords, byte AIgnore, ushort AValue)
        {
            List<Coords> lChildren = new List<Coords>(8);
            if (AIgnore != ChunkOctree.C_EmptyMaterial)
                lChildren.Add(ACoords.GetChild(AIgnore));

            for (byte bPath = 0; bPath < 8; bPath++)
                if (bPath != AIgnore)
                {
                    Coords cChild = ACoords.GetChild(bPath);
                    ushort nGet;
                    if (!this.FTerminals.TryGetValue(cChild, out nGet))
                    {
                        if (AValue == ChunkOctree.C_EmptyMaterial)
                        {
                            if (this.FTerminals.Keys.Any(cChild.IsParentOf))
                                return false;

                            nGet = ChunkOctree.C_EmptyMaterial;
                        }
                        else
                            return false;
                    }

                    if (nGet != AValue)
                        return false;

                    if (nGet != ChunkOctree.C_EmptyMaterial)
                        lChildren.Add(cChild);
                }

            lChildren.ForEach(AKey => this.FTerminals.Remove(AKey));
            if (AValue != ChunkOctree.C_EmptyMaterial)
                this.FTerminals[ACoords] = AValue;

            byte bIgnore = ACoords.GetPath();
            if (ACoords.StepUp())
                this.Collapse(ACoords, bIgnore, AValue);
            return true;
        }

        public override void Get(ref Coords ACoords, out ushort AValue)
        {
            while (!this.FTerminals.TryGetValue(ACoords, out AValue))
                if (!ACoords.StepUp())
                {
                    AValue = ChunkOctree.C_EmptyMaterial;
                    return;
                }
        }
        public override void Set(Coords ACoords, ushort AValue, ref int ABalance)
        {
            Coords cDefined = ACoords;
            ushort nCurrent;
            this.Get(ref cDefined, out nCurrent);
            if (nCurrent == AValue)
                return;

            this.Expand(ACoords);
            this.Terminate(ACoords, AValue, ref ABalance);

            byte bIgnore = ACoords.GetPath();
            if (ACoords.StepUp())
                this.Collapse(ACoords, bIgnore, AValue);
        }

        public override void Clear()
        {
            this.FTerminals.Clear();
        }

        public bool IsLeaf(Coords ACoords)
        {
            return this.FTerminals.ContainsKey(ACoords);
        }

        public int TerminalCount
        {
            get { return this.FTerminals.Count; }
        }
    }
}