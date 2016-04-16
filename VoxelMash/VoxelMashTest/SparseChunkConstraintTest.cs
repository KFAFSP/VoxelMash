using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using VoxelMash.Grids;

using Coords = VoxelMash.Grids.ChunkSpaceCoordinates;

namespace VoxelMashTest
{
    [TestClass]
    public class SparseChunkConstraintTest
    {
        [TestMethod]
        public void SingleVoxel()
        {
            SparseChunkOctree scoOctree = new SparseChunkOctree();

            int iDiscard = 0;
            scoOctree.Set(new Coords(0, 0, 0, 0), 1, ref iDiscard);

            ushort nGet;
            Coords cGet = new Coords(0, 0, 0, 0);
            scoOctree.Get(ref cGet, out nGet);
            Assert.AreEqual(cGet, new Coords(0, 0, 0, 0));
            Assert.AreEqual(1, nGet);
        }

        [TestMethod]
        public void Balance()
        {
            SparseChunkOctree scoOctree = new SparseChunkOctree();

            int iBalance = 0;
            Coords cSet = new Coords(0, 0, 0, 0);
            for (byte bPath = 0; bPath < 8; bPath++)
            {
                int iOld = iBalance;
                cSet.SetPath(bPath);
                scoOctree.Set(cSet, 1, ref iBalance);
                Assert.AreEqual(iOld + 1, iBalance);
            }

            iBalance = 0;
            scoOctree.Set(new Coords(1, 0, 0, 0), 0, ref iBalance);
            Assert.AreEqual(8, iBalance);

            scoOctree.Set(new Coords(0, 0, 0, 0), 1, ref iBalance);
            scoOctree.Set(new Coords(0, 1, 0, 0), 1, ref iBalance);
            scoOctree.Set(new Coords(0, 0, 1, 0), 1, ref iBalance);
            scoOctree.Set(new Coords(0, 1, 1, 0), 1, ref iBalance);

            iBalance = 0;
            scoOctree.Set(new Coords(1, 0, 0, 0), 1, ref iBalance);
            Assert.AreEqual(4, iBalance);
        }

        [TestMethod]
        public void VoxelCollapse()
        {
            SparseChunkOctree scoOctree = new SparseChunkOctree();
            
            int iDiscard = 0;
            Coords cSet = new Coords(0, 0, 0, 0);
            for (byte bPath = 0; bPath < 7; bPath++)
            {
                int iOld = iDiscard;
                cSet.SetPath(bPath);
                scoOctree.Set(cSet, 1, ref iDiscard);
                Assert.AreEqual(iOld + 1, iDiscard);
            }

            Assert.AreEqual(7, scoOctree.TerminalCount);
            cSet.SetPath(0x7);
            scoOctree.Set(cSet, 1, ref iDiscard);
            Assert.AreEqual(1, scoOctree.TerminalCount);
            Assert.IsTrue(scoOctree.IsLeaf(new Coords(1, 0, 0, 0)));
        }

        [TestMethod]
        public void VoxelExpand()
        {
            SparseChunkOctree scoOctree = new SparseChunkOctree();

            int iDiscard = 0;
            scoOctree.Set(new Coords(1, 0, 0, 0), 1, ref iDiscard);
            Assert.AreEqual(1, scoOctree.TerminalCount);

            scoOctree.Set(new Coords(0, 0, 0, 0), 2, ref iDiscard);
            Assert.AreEqual(8, scoOctree.TerminalCount);
            
            for (byte bPath = 1; bPath < 8; bPath++)
                Assert.IsTrue(scoOctree.IsLeaf(new Coords(0, 0, 0, 0).GetSibling(bPath)));
        }

        [TestMethod]
        public void ZeroCollapse()
        {
            SparseChunkOctree scoOctree = new SparseChunkOctree();

            int iDiscard = 0;
            scoOctree.Set(new Coords(1, 0, 0, 0), 1, ref iDiscard);
            Assert.AreEqual(1, scoOctree.TerminalCount);

            Coords cSet = new Coords(0, 0, 0, 0);
            for (byte bPath = 0; bPath < 8; bPath++)
            {
                cSet.SetPath(bPath);
                scoOctree.Set(cSet, 0, ref iDiscard);
            }
            Assert.AreEqual(0, scoOctree.TerminalCount);
        }
    }
}
