using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VoxelMash.Grids;

namespace VoxelMashTest
{
    [TestClass]
    public class ChunkTest
    {
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
            Random rRandom = new Random();

            List<ChunkSpaceCoords> lAll = ChunkSpaceCoords.Root.Children
                .Where(AChild => AChild.Level == ChunkSpaceLevel.Block)
                .OrderBy(AChild => rRandom.Next())
                .ToList();

            GridChunk gcChunk = new StrictGridChunk();

            lAll.ForEach(ABlock => gcChunk.Set(ABlock, 1));
            Assert.AreEqual(1, gcChunk.TerminalCount);
        }

        [TestMethod]
        public void OverrideBlockPerformance()
        {
            Random rRandom = new Random();

            List<ChunkSpaceCoords> lAll = ChunkSpaceCoords.Root.Children
                .Where(AChild => AChild.Level == ChunkSpaceLevel.Block)
                .OrderBy(AChild => rRandom.Next())
                .ToList();

            GridChunk gcChunk = new OverrideGridChunk();

            lAll.ForEach(ABlock => gcChunk.Set(ABlock, 1));
            Assert.AreEqual(1, gcChunk.TerminalCount);
        }
    }
}
