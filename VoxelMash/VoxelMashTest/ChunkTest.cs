using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VoxelMash;
using VoxelMash.Grids;

namespace VoxelMashTest
{
    [TestClass]
    public class ChunkTest
    {
        private static readonly List<ChunkSpaceCoords> _FRandomBlocks = new List<ChunkSpaceCoords>(4096);

        [TestInitialize]
        public void Randomize()
        {
            Random rRandom = new Random();

            for (byte Z = 0; Z < 16; Z++)
                for (byte Y = 0; Y < 16; Y++)
                    for (byte X = 0; X < 16; X++)
                        ChunkTest._FRandomBlocks.Add(new ChunkSpaceCoords(ChunkSpaceLevel.Block, X, Y, Z));

            ChunkTest._FRandomBlocks.Shuffle(rRandom);
        }

        [TestMethod]
        public void StrictAccess()
        {
            GridChunk gcChunk = new StrictGridChunk();

            Assert.AreEqual(1, gcChunk.Set(TestHelpers.ChunkCoords(8, 1, 1, 1), 10));
            Assert.AreEqual(1, gcChunk.TerminalCount);
            Assert.AreEqual(10, gcChunk.Get(TestHelpers.ChunkCoords(8, 1, 1, 1)));

            for (byte bPath = 0; bPath < 7; bPath++)
                Assert.AreEqual(1, gcChunk.Set(TestHelpers.ChunkCoords(7, 0, 0, 0).GetChild(bPath), 10));

            Assert.AreEqual(1, gcChunk.TerminalCount);
            Assert.AreEqual(10, gcChunk.Get(TestHelpers.ChunkCoords(8, 1, 1, 1)));

            Assert.AreEqual(1, gcChunk.Set(TestHelpers.ChunkCoords(8, 1, 1, 1), 9));
            Assert.AreEqual(8, gcChunk.TerminalCount);

            gcChunk.Clear();

            for (byte bPath = 0; bPath < 4; bPath++)
                Assert.AreEqual(1, gcChunk.Set(TestHelpers.ChunkCoords(7, 0, 0, 0).GetChild(bPath), 10));
            Assert.AreEqual(4, gcChunk.Set(TestHelpers.ChunkCoords(7, 0, 0, 0), 10));
        }

        [TestMethod]
        public void OverrideAccess()
        {
            GridChunk gcChunk = new OverrideGridChunk();

            Assert.AreEqual(1, gcChunk.Set(TestHelpers.ChunkCoords(8, 1, 1, 1), 10));
            Assert.AreEqual(1, gcChunk.TerminalCount);
            Assert.AreEqual(10, gcChunk.Get(TestHelpers.ChunkCoords(8, 1, 1, 1)));

            for (byte bPath = 0; bPath < 7; bPath++)
                Assert.AreEqual(1, gcChunk.Set(TestHelpers.ChunkCoords(7, 0, 0, 0).GetChild(bPath), 10));

            Assert.AreEqual(1, gcChunk.TerminalCount);
            Assert.AreEqual(10, gcChunk.Get(TestHelpers.ChunkCoords(8, 1, 1, 1)));

            Assert.AreEqual(1, gcChunk.Set(TestHelpers.ChunkCoords(8, 1, 1, 1), 9));
            Assert.AreEqual(2, gcChunk.TerminalCount);

            gcChunk.Clear();

            for (byte bPath = 0; bPath < 4; bPath++)
                Assert.AreEqual(1, gcChunk.Set(TestHelpers.ChunkCoords(7, 0, 0, 0).GetChild(bPath), 10));
            Assert.AreEqual(4, gcChunk.Set(TestHelpers.ChunkCoords(7, 0, 0, 0), 10));
        }

        [TestMethod]
        public void StrictBlockPerformance()
        {
            GridChunk gcChunk = new StrictGridChunk();

            ChunkTest._FRandomBlocks.ForEach(ABlock => gcChunk.Set(ABlock, 1));
            Assert.AreEqual(1, gcChunk.TerminalCount);
        }

        [TestMethod]
        public void OverrideBlockPerformance()
        {
            GridChunk gcChunk = new OverrideGridChunk();

            ChunkTest._FRandomBlocks.Take(100).ForEach(ABlock => gcChunk.Set(ABlock, 1));
            Assert.AreEqual(100, gcChunk.TerminalCount);
        }
    }
}
