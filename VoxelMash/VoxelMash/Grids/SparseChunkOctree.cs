using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

using C5;

using VoxelMash.Serialization;

using Coords = VoxelMash.Grids.ChunkSpaceCoordinates;
using Terminal = C5.KeyValuePair<VoxelMash.Grids.ChunkSpaceCoordinates, ushort>;

namespace VoxelMash.Grids
{
    public class SparseChunkOctree : ChunkOctree
    {
        public class SerializationHandler
        {
            public int Read(
                Stream AInput,
                SparseChunkOctree AOctree)
            {
                AOctree.Clear();

                int iRead = 0;

                using (BitStreamReader bsrReader = new BitStreamReader(AInput))
                {
                    Coords cPointer;
                    iRead += Coords.PackedHandler.Read(AInput, out cPointer);

                    int iBuffer;

                    if (cPointer == ChunkSpaceCoordinates.OutOfRange)
                        return iRead;

                    iRead += bsrReader.ReadBits(2, out iBuffer);
                    iRead += bsrReader.ReadBits((byte)((iBuffer + 1) * 4), out iBuffer);

                    AOctree.FTerminals.Add(cPointer, (ushort)iBuffer);
                    cPointer.MoveRight();

                    while (cPointer != ChunkSpaceCoordinates.OutOfRange)
                    {
                        iRead += bsrReader.ReadBits(1, out iBuffer);
                        if (iBuffer == 0x1)
                        {
                            iRead += bsrReader.ReadBits(2, out iBuffer);
                            iRead += bsrReader.ReadBits((byte)((iBuffer + 1) * 4), out iBuffer);

                            AOctree.FTerminals.Add(cPointer, (ushort)iBuffer);
                            cPointer.MoveRight();
                            continue;
                        }

                        iRead += bsrReader.ReadBits(1, out iBuffer);
                        if (iBuffer == 0x1)
                        {
                            iRead += bsrReader.ReadBits(3, out iBuffer);
                            cPointer.StepDown((byte)iBuffer);
                            continue;
                        }

                        iRead += bsrReader.ReadBits(1, out iBuffer);
                        if (iBuffer == 0x1)
                        {
                            cPointer.MoveRight();
                        }
                        else
                        {
                            cPointer.StepUp();
                        }
                    }
                }

                return iRead;
            }
            public void Write(
                Stream AOutput,
                SparseChunkOctree AOctree)
            {
                using (BitStreamWriter bswWriter = new BitStreamWriter(AOutput))
                {
                    IEnumerator<Terminal> ieTerminals = AOctree.FTerminals.OrderBy(ATerminal => ATerminal.Key).GetEnumerator();

                    if (!ieTerminals.MoveNext())
                    {
                        Coords.PackedHandler.Write(AOutput, Coords.OutOfRange);
                        return;
                    }

                    Coords.PackedHandler.Write(AOutput, ieTerminals.Current.Key);
                    ushort nValue = ieTerminals.Current.Value;
                    if (nValue > 0xFFF)
                    {
                        bswWriter.WriteBits(0x3, 2);
                        bswWriter.WriteBits(nValue, 16);
                    }
                    else if (nValue > 0xFF)
                    {
                        bswWriter.WriteBits(0x2, 2);
                        bswWriter.WriteBits(nValue, 12);
                    }
                    else if (nValue > 0xF)
                    {
                        bswWriter.WriteBits(0x1, 2);
                        bswWriter.WriteBits(nValue, 8);
                    }
                    else
                    {
                        bswWriter.WriteBits(0x0, 2);
                        bswWriter.WriteBits(nValue, 4);
                    }

                    Coords cPointer = ieTerminals.Current.Key;

                    while (ieTerminals.MoveNext())
                    {
                        Coords cCurrent = ieTerminals.Current.Key;
                        cPointer.MoveRight();

                        while (cPointer != cCurrent && cPointer.Shift <= cCurrent.Shift + 1 && !cPointer.IsParentOf(cCurrent))
                        {
                            bswWriter.WriteBits(0x1, 3); // 001
                            cPointer.MoveRight();
                        }

                        while (cPointer != cCurrent && !cPointer.IsParentOf(cCurrent))
                        {
                            bswWriter.WriteBits(0x0, 3); // 000
                            cPointer.StepUp();
                        }

                        // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
                        while (cPointer != cCurrent)
                        {
                            byte bPath = cCurrent.GetPath((byte)(cPointer.Shift - cCurrent.Shift - 1));
                            cPointer.StepDown(bPath);
                            bswWriter.WriteBits(0x1, 2); // 01
                            bswWriter.WriteBits(bPath, 3);
                        }

                        bswWriter.WriteBits(0x1, 1); // 1
                        nValue = ieTerminals.Current.Value;
                        if (nValue > 0xFFF)
                        {
                            bswWriter.WriteBits(0x3, 2);
                            bswWriter.WriteBits(nValue, 16);
                        }
                        else if (nValue > 0xFF)
                        {
                            bswWriter.WriteBits(0x2, 2);
                            bswWriter.WriteBits(nValue, 12);
                        }
                        else if (nValue > 0xF)
                        {
                            bswWriter.WriteBits(0x1, 2);
                            bswWriter.WriteBits(nValue, 8);
                        }
                        else
                        {
                            bswWriter.WriteBits(0x0, 2);
                            bswWriter.WriteBits(nValue, 4);
                        }
                    }

                    while (cPointer != ChunkSpaceCoordinates.OutOfRange)
                    {
                        bswWriter.WriteBits(0x1, 3); //001
                        cPointer.MoveRight();
                    }
                }
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
            foreach (Terminal tTerminal in this.FTerminals.RangeFromTo(ACoords, cLastChildPlusOne))
            {
                if (tTerminal.Value == AValue)
                    ABalance -= tTerminal.Key.Volume;
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