using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

using C5;

using Coords = VoxelMash.Grids.ChunkSpaceCoordinates;

namespace VoxelMash.Grids
{
    public class SparseChunkOctree : ChunkOctree
    {
        private readonly TreeDictionary<Coords, ushort> FTerminals;

        public SparseChunkOctree()
        {
            this.FTerminals = new TreeDictionary<Coords, ushort>(Comparer<Coords>.Default);
        }

        protected void Expand(Coords ACoords)
        {
            byte bIgnore = ACoords.GetPath();
            while (ACoords.StepUp())
            {
                ushort nGet;
                if (this.FTerminals.Find(ref ACoords, out nGet))
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

            Coords cLastChildPlusOne = ACoords.GetLastChildPlusOne();
            foreach (C5.KeyValuePair<Coords, ushort> kvpPair in
                this.FTerminals.RangeFromTo(ACoords, cLastChildPlusOne))
            {
                if (kvpPair.Value == AValue)
                    ABalance -= kvpPair.Key.Volume;
            }

            this.FTerminals.RemoveRangeFromTo(ACoords, cLastChildPlusOne);

            if (AValue == ChunkOctree.C_EmptyMaterial)
                this.FTerminals.Remove(ACoords);
            else
                this.FTerminals.UpdateOrAdd(ACoords, AValue);
        }
        protected bool Collapse(Coords ACoords, byte AIgnore, ushort AValue)
        {
            for (byte bPath = 0; bPath < 8; bPath++)
                if (bPath != AIgnore)
                {
                    Coords cChild = ACoords.GetChild(bPath);
                    ushort nGet;
                    if (!this.FTerminals.Find(ref cChild, out nGet))
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
                }

            Coords cLastChildPlusOne = ACoords.GetLastChildPlusOne();           
            this.FTerminals.RemoveRangeFromTo(ACoords, cLastChildPlusOne);

            if (AValue == ChunkOctree.C_EmptyMaterial)
                this.FTerminals.Remove(ACoords);
            else
                this.FTerminals.UpdateOrAdd(ACoords, AValue);

            byte bIgnore = ACoords.GetPath();
            if (ACoords.StepUp())
                this.Collapse(ACoords, bIgnore, AValue);
            return true;
        }

        public override void Get(ref Coords ACoords, out ushort AValue)
        {
            while (!this.FTerminals.Find(ref ACoords, out AValue))
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

        [Pure]
        public bool IsLeaf(Coords ACoords)
        {
            return this.FTerminals.Contains(ACoords);
        }

        public int TerminalCount
        {
            get { return this.FTerminals.Count; }
        }
    }
}