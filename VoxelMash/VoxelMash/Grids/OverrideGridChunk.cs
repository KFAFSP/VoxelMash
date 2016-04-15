using System.Collections.Generic;
using System.Linq;

namespace VoxelMash.Grids
{
    public class OverrideGridChunk : GridChunk
    {
        protected class CounterDict
        {
            private bool FSoleMax;
            private ushort FMaxKey;
            private int FMaxValue;

            private readonly Dictionary<ushort, int> FCounts;

            public CounterDict()
            {
                this.FSoleMax = true;
                this.FMaxKey = 0;
                this.FMaxValue = int.MinValue;

                this.FCounts = new Dictionary<ushort, int>();
            }
            public CounterDict(ushort AKey, int AValue)
                : this()
            {
                this.FCounts[AKey] = AValue;
            }

            public void Increment(ushort AKey, int AAmount)
            {
                int iCurrent;
                if (!this.FCounts.TryGetValue(AKey, out iCurrent))
                    iCurrent = 0;

                iCurrent += AAmount;

                if (this.FMaxValue < iCurrent)
                {
                    this.FSoleMax = true;
                    this.FMaxValue = iCurrent;
                    this.FMaxKey = AKey;
                }
                else if (this.FMaxValue == iCurrent)
                    this.FSoleMax = false;

                this.FCounts[AKey] = iCurrent;
            }
            public void Increment(CounterDict ABy)
            {
                ABy.NonZero.ForEach(APair => this.Increment(APair.Key, APair.Value));
            }

            public int Get(ushort AKey)
            {
                int iValue;
                if (!this.FCounts.TryGetValue(AKey, out iValue))
                    iValue = 0;

                return iValue;
            }
            public void Set(ushort AKey, int AValue)
            {
                if (this.FMaxValue < AValue)
                {
                    this.FSoleMax = true;
                    this.FMaxValue = AValue;
                    this.FMaxKey = AKey;
                }
                else if (this.FMaxValue == AValue)
                    this.FSoleMax = false;

                this.FCounts[AKey] = AValue;
            }

            public int this[ushort AKey]
            {
                get { return this.Get(AKey); }
                set { this.Set(AKey, value); }
            }

            public bool SoleMax
            {
                get { return this.FSoleMax; }
            }
            public ushort MaxKey
            {
                get { return this.FMaxKey; }
            }
            public int MaxValue
            {
                get { return this.FMaxValue; }
            }

            public IEnumerable<KeyValuePair<ushort, int>> NonZero
            {
                get { return this.FCounts.Where(APair => APair.Value != 0); }
            }
        }

        protected readonly Dictionary<ChunkSpaceCoords, CounterDict> FCounterCache;

        public OverrideGridChunk()
        {
            this.FCounterCache = new Dictionary<ChunkSpaceCoords, CounterDict>();
        }

        protected virtual CounterDict GetCount(ChunkSpaceCoords ACoords)
        {
            CounterDict cdResult;
            if (this.FCounterCache.TryGetValue(ACoords, out cdResult))
                return cdResult;

            cdResult = new CounterDict();
            this.FCounterCache[ACoords] = cdResult;

            if (ACoords.Level == ChunkSpaceLevel.Voxel)
                cdResult.Increment(this.Get(ACoords), 1);
            else
                for (byte bPath = 0; bPath < 8; bPath++)
                    cdResult.Increment(this.GetCount(ACoords.GetChild(bPath)));

            return cdResult;
        }

        protected override void ExpandToHere(
            ChunkSpaceCoords ANode,
            ushort AValue)
        { }
        protected override bool CollapseThis(
            ChunkSpaceCoords ANode,
            ushort AValue)
        {
            if (ANode.Level == ChunkSpaceLevel.Voxel)
                return false;

            CounterDict cdThis = this.GetCount(ANode);
            if (cdThis.SoleMax)
            {
                ChunkSpaceCoords cscLast = ANode.LastChild;
                List<ChunkSpaceCoords> lRemove = this.FTerminals
                    .SkipWhile(APair => APair.Key <= ANode)
                    .TakeWhile(APair => APair.Key <= cscLast)
                    .Where(APair => APair.Value == cdThis.MaxKey)
                    .Select(APair => APair.Key)
                    .ToList();

                if (lRemove.Count > 0)
                {
                    lRemove.ForEach(AKey => this.FTerminals.Remove(AKey));
                    if (cdThis.MaxKey == 0)
                        this.FTerminals.Remove(ANode);
                    else
                        this.FTerminals[ANode] = cdThis.MaxKey;

                    if (ANode.Level == ChunkSpaceLevel.Chunk)
                        return true;

                    ANode.StepUp();
                    this.CollapseThis(ANode, AValue);
                    return true;
                }

                return false;
            }

            if (ANode.Level == ChunkSpaceLevel.Chunk)
                return false;

            ANode.StepUp();
            return this.CollapseThis(ANode, AValue);
        }

        public override int Set(
            ChunkSpaceCoords ACoords,
            ushort AValue)
        {
            this.FCounterCache.Clear();
            int iResult = base.Set(ACoords, AValue);
            this.FCounterCache.Clear();

            return iResult;
        }
    }
}
