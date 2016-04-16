using System.Collections.Generic;
using System.Linq;

namespace VoxelMash.Grids
{
    public class OverrideGridChunk : GridChunk
    {
        protected class CounterDict
        {
            private bool FAbsoluteMaximum;
            private ushort FMaximumKey;
            private int FMaximumValue;

            private readonly Dictionary<ushort, int> FCounts;

            public CounterDict()
            {
                this.FCounts = new Dictionary<ushort, int>();
                this.Reset();
            }

            public void Reset()
            {
                this.FCounts.Clear();

                this.FAbsoluteMaximum = false;
                this.FMaximumKey = 0;
                this.FMaximumValue = int.MinValue;
            }

            public void Increment(ushort AKey, int AAmount)
            {
                int iCurrent;
                if (!this.FCounts.TryGetValue(AKey, out iCurrent))
                    iCurrent = 0;

                this.Set(AKey, iCurrent + AAmount);
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
                if (AKey == this.FMaximumKey && AValue < this.FMaximumValue)
                {
                    this.FMaximumValue = AValue;

                    this.FCounts[AKey] = AValue;
                    this.FCounts.ForEach(APair =>
                    {
                        if (APair.Value > this.FMaximumValue)
                        {
                            this.FAbsoluteMaximum = true;
                            this.FMaximumValue = APair.Value;
                            this.FMaximumKey = APair.Key;
                        }
                        else if (APair.Value == this.FMaximumValue && APair.Key != this.FMaximumKey)
                            this.FAbsoluteMaximum = false;
                    });

                    return;
                }
                
                if (this.FMaximumValue < AValue)
                {
                    this.FAbsoluteMaximum = true;
                    this.FMaximumValue = AValue;
                    this.FMaximumKey = AKey;
                }
                else if (this.FMaximumValue == AValue)
                    this.FAbsoluteMaximum = false;

                this.FCounts[AKey] = AValue;
            }

            public int this[ushort AKey]
            {
                get { return this.Get(AKey); }
                set { this.Set(AKey, value); }
            }

            public bool AbsoluteMaximum
            {
                get { return this.FAbsoluteMaximum; }
            }
            public KeyValuePair<ushort, int> Maximum
            {
                get { return new KeyValuePair<ushort, int>(this.FMaximumKey, this.FMaximumValue); }
            }

            public IEnumerable<KeyValuePair<ushort, int>> NonZero
            {
                get { return this.FCounts.Where(APair => APair.Value != 0); }
            }
        }

        protected void Count(
            ushort AExpect,
            ChunkSpaceCoords ACoords,
            CounterDict AResult)
        {
            if (ACoords.Level == ChunkSpaceLevel.Voxel)
                return;

            for (byte bPath = 0; bPath < 8; bPath++)
            {
                ChunkSpaceCoords cscChild = ACoords;
                cscChild.StepDown(bPath);
                int iVolume = cscChild.Volume;

                ushort nValue;
                if (!this.FTerminals.TryGetValue(cscChild, out nValue))
                    nValue = AExpect;

                if (nValue != AExpect)
                {
                    AResult.Increment(AExpect, -iVolume);
                    AResult.Increment(nValue, iVolume);
                }

                this.Count(nValue, cscChild, AResult);
            }
        }
        protected void ReplaceChildren(
            ChunkSpaceCoords ACoords,
            ushort AOldValue,
            ushort ANewValue)
        {
            if (ACoords.Level == ChunkSpaceLevel.Voxel)
                return;

            HashSet<ChunkSpaceCoords> hsAdd = new HashSet<ChunkSpaceCoords>();
            for (byte bPath = 0; bPath < 8; bPath++)
            {
                ChunkSpaceCoords cscChild = ACoords;
                cscChild.StepDown(bPath);
                hsAdd.Add(cscChild);
            }

            ChunkSpaceCoords cscLast = ACoords.LastChild;
            List<ChunkSpaceCoords> lRemove = new List<ChunkSpaceCoords>();

            this.FTerminals
                .SkipWhile(APair => APair.Key <= ACoords)
                .TakeWhile(APair => APair.Key <= cscLast)
                .ForEach(APair =>
                {
                    if (APair.Value == ANewValue)
                    {
                        hsAdd.Remove(APair.Key);
                        lRemove.Add(APair.Key);
                    }

                    if (hsAdd.RemoveWhere(ACandidate => ACandidate.IsParentOf(APair.Key)) == 1)
                        hsAdd.Add(APair.Key);
                });

            lRemove.ForEach(AKey => this.FTerminals.Remove(AKey));
            hsAdd.ForEach(AKey => this.FTerminals[AKey] = AOldValue);
        }
        protected bool SmartCollapse(
            ChunkSpaceCoords ANode)
        {
            if (ANode.Level == ChunkSpaceLevel.Voxel)
                return false;

            ushort nOldValue = this.Get(ANode);
            CounterDict cdCount = new CounterDict();
            cdCount.Increment(nOldValue, ANode.Volume);
            this.Count(nOldValue, ANode, cdCount);

            bool bCollapsed = false;
            do
            {
                if (!cdCount.AbsoluteMaximum)
                {
                    if (bCollapsed)
                        return true;

                    if (ANode.Level == ChunkSpaceLevel.Chunk)
                        return false;

                    byte bSkip = ANode.Path;
                    ANode.StepUp();
                    ushort nExpect = this.Get(ANode);
                    cdCount.Increment(nExpect, ANode.Volume);
                    if (nOldValue != nExpect)
                        cdCount.Increment(nExpect, -ANode.GetChild().Volume);
                    nOldValue = nExpect;

                    for (byte bPath = 0; bPath < 8; bPath++)
                        if (bPath != bSkip)
                        {
                            ChunkSpaceCoords cscChild = ANode;
                            cscChild.StepDown(bSkip);
                            this.Count(nExpect, cscChild, cdCount);
                        }
                }
                else
                {
                    ushort nNewValue = cdCount.Maximum.Key;
                    if (nNewValue == nOldValue)
                        return bCollapsed;

                    this.ReplaceChildren(ANode, nOldValue, nNewValue);
                    this.FTerminals[ANode] = nNewValue;

                    if (ANode.Level == ChunkSpaceLevel.Chunk)
                        return true;

                    byte bSkip = ANode.Path;
                    ANode.StepUp();
                    ushort nExpect = this.Get(ANode);
                    cdCount.Increment(nExpect, ANode.Volume);
                    if (nOldValue != nExpect)
                        cdCount.Increment(nExpect, -ANode.GetChild().Volume);
                    nOldValue = nExpect;

                    for (byte bPath = 0; bPath < 8; bPath++)
                        if (bPath != bSkip)
                        {
                            ChunkSpaceCoords cscChild = ANode;
                            cscChild.StepDown(bSkip);
                            this.Count(nExpect, cscChild, cdCount);
                        }

                    bCollapsed = true;
                }
            } while (true);
        }

        protected override void ExpandToHere(
            ChunkSpaceCoords ANode,
            ushort AValue)
        { }
        protected override bool CollapseThis(
            ChunkSpaceCoords ANode,
            ushort AValue)
        {
            return this.SmartCollapse(ANode);
        }
    }
}