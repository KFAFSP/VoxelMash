using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

using C5;

using VoxelMash.Serialization;

using Coords = VoxelMash.Grids.ChunkSpaceCoordinates;

namespace VoxelMash.Grids
{
    public class SparseChunkOctree : ChunkOctree
    {
        public sealed class SerializationHandler : StreamAdapter
        {
            private readonly Coords.SerializationHandler FCoordHandler;

            public SerializationHandler(
                Stream ABaseStream,
                Coords.SerializationHandler ACoordHandler,
                bool APropagateDispose = false)
                : base(ABaseStream, APropagateDispose)
            {
                if (ACoordHandler == null)
                    throw new ArgumentNullException("ACoordHandler");

                this.FCoordHandler = ACoordHandler;
            }
            public SerializationHandler(
                Stream ABaseStream,
                bool AAllowPacking = true)
                : this(ABaseStream, new Coords.SerializationHandler(ABaseStream, AAllowPacking))
            { }

            public int Read(SparseChunkOctree AOctree)
            {
                AOctree.Clear();

                int iTerminals = this.FBaseStream.ReadInt32();
                int iCount = 4;
                while (iTerminals > 0)
                {
                    iTerminals--;
                    Coords cCoords;
                    iCount += this.FCoordHandler.Read(out cCoords);
                    ushort nValue = this.FBaseStream.ReadUInt16();
                    iCount += 2;
                    AOctree.FTerminals.Add(cCoords, nValue);
                }

                return iCount;
            }
            public int Write(SparseChunkOctree AOctree)
            {
                /* Start with first terminal in sort order.
                 * 1. Emit coords + material.
                 * 2. for each next node
                 *   - express path to next via horizontal (and vertical move)
                 *   - emit material
                 */

                this.FBaseStream.WriteInt32(AOctree.TerminalCount);
                int iCount = 4;
                foreach (C5.KeyValuePair<Coords, ushort> kvpPair in AOctree.FTerminals)
                {
                    iCount += this.FCoordHandler.Write(kvpPair.Key);
                    this.FBaseStream.WriteUInt16(kvpPair.Value);
                    iCount += 2;
                }

                return iCount;
            }

            public Coords.SerializationHandler CoordHandler
            {
                get { return this.FCoordHandler; }
            }
        }

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