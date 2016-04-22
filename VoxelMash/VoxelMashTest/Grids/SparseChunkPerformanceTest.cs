using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VoxelMash;
using VoxelMash.Grids;

using Coords = VoxelMash.Grids.ChunkSpaceCoordinates;

namespace VoxelMashTest.Grids
{
    [TestClass]
    public class SparseChunkPerformanceTest
    {
        private const int C_Complexity = 100000;

        private static readonly Random _FRandom = new Random();
        private static readonly List<Coords> _FRandomCoords = new List<Coords>();
        private static readonly List<Coords> _FAllBlocks = new List<Coords>();

        private static Coords GetRandom()
        {
            return new Coords(
                (byte)SparseChunkPerformanceTest._FRandom.Next(0, 8),
                (byte)SparseChunkPerformanceTest._FRandom.Next(0, 255),
                (byte)SparseChunkPerformanceTest._FRandom.Next(0, 255),
                (byte)SparseChunkPerformanceTest._FRandom.Next(0, 255));
        }

        [TestInitialize]
        public void Randomize()
        {
            for (int I = 0; I < SparseChunkPerformanceTest.C_Complexity; I++)
                SparseChunkPerformanceTest._FRandomCoords.Add(SparseChunkPerformanceTest.GetRandom());

            for (int iPos = 0; iPos <= 0xFFF; iPos++)
                SparseChunkPerformanceTest._FAllBlocks.Add(
                    new Coords(
                        4,
                        (byte)(iPos & 0xF),
                        (byte)((iPos >> 4) & 0xF),
                        (byte)((iPos >> 8) & 0xF)));

            SparseChunkPerformanceTest._FAllBlocks.Shuffle(SparseChunkPerformanceTest._FRandom);
        }

        [TestMethod]
        public void RandomFill()
        {
            SparseChunkOctree scoOctree = new SparseChunkOctree();

            int iBalance = 0;
            foreach (Coords cCoord in SparseChunkPerformanceTest._FRandomCoords)
                scoOctree.Set(cCoord, 1, ref iBalance);

            Assert.IsTrue(iBalance > 0);
        }

        [TestMethod]
        public void SequentialBlockFill()
        {
            SparseChunkOctree scoOctree = new SparseChunkOctree();

            List<Coords> lSorted = SparseChunkPerformanceTest._FAllBlocks.ToList();
            lSorted.Sort();

            int iBalance = 0;
            foreach (Coords cCoord in lSorted)
                scoOctree.Set(cCoord, 1, ref iBalance);

            Assert.AreEqual(1, scoOctree.TerminalCount);
            Assert.IsTrue(scoOctree.IsLeaf(new Coords(8, 0, 0, 0)));
            Assert.AreEqual(256 * 256 * 256, iBalance);
        }

        [TestMethod]
        public void RandomBlockFill()
        {
            SparseChunkOctree scoOctree = new SparseChunkOctree();

            int iBalance = 0;
            foreach (Coords cCoord in SparseChunkPerformanceTest._FAllBlocks)
                scoOctree.Set(cCoord, 1, ref iBalance);

            Assert.AreEqual(1, scoOctree.TerminalCount);
            Assert.IsTrue(scoOctree.IsLeaf(new Coords(8, 0, 0, 0)));
            Assert.AreEqual(256 * 256 * 256, iBalance);
        }
    }
}
