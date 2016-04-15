using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VoxelMash.Grids;

namespace VoxelMashTest
{
    [TestClass]
    public class CoordinateTest
    {
        [TestMethod]
        public void NormalAddition()
        {
            // (8, 2|3|4) + (8, 3|2|1) = (8, 5|5|5)
            Assert.AreEqual(
                TestHelpers.ChunkCoords(8, 5, 5, 5),
                TestHelpers.ChunkCoords(8, 2, 3, 4) + TestHelpers.ChunkCoords(8, 3, 2, 1));
        }

        [TestMethod]
        public void StepDownAddition()
        {
            // (0, 0|0|0) + (8, 1|1|1) = (8, 1|1|1)
            Assert.AreEqual(
                TestHelpers.ChunkCoords(8, 1, 1, 1),
                TestHelpers.ChunkCoords((byte)0, 0, 0, 0) + TestHelpers.ChunkCoords(8, 1, 1, 1));

            // (7, 1|1|1) + (8, 1|1|1) = (8, 3|3|3)
            Assert.AreEqual(
                TestHelpers.ChunkCoords(8, 3, 3, 3),
                TestHelpers.ChunkCoords(7, 1, 1, 1) + TestHelpers.ChunkCoords(8, 1, 1, 1));
        }

        [TestMethod]
        public void StepDown()
        {
            // (7, 1|1|1) down 1|1|1 = (7, 1|1|1) + (8, 1|1|1)
            Assert.AreEqual(
                TestHelpers.ChunkCoords(7, 1, 1, 1) + TestHelpers.ChunkCoords(8, 1, 1, 1),
                TestHelpers.ChunkCoords(7, 1, 1, 1).StepDown(0x07));

            // (0, 0|0|0) down 0|0|0 down 1|1|1 down 0|1|0 down 1|0|1 = (4, 5, 6, 5)
            Assert.AreEqual(
                TestHelpers.ChunkCoords(4, 5, 6, 5),
                TestHelpers.ChunkCoords((byte)0, 0, 0, 0).StepDown(new byte[] { 0x00, 0x07, 0x02, 0x05 }));
        }

        [TestMethod]
        public void StepUp()
        {
            // (4, 5|6|5) up = (3, 2|3|2)
            Assert.AreEqual(
                TestHelpers.ChunkCoords(3, 2, 3, 2),
                TestHelpers.ChunkCoords(4, 5, 6, 5).StepUp());

            // (0, 0|0|0) up = (0, 0|0|0)
            Assert.AreEqual(
                TestHelpers.ChunkCoords((byte)0, 0, 0, 0),
                TestHelpers.ChunkCoords((byte)0, 0, 0, 0).StepUp());
        }

        [TestMethod]
        public void Serialization()
        {
            // Example : (4, 12|11|2)
            byte[] aShort = {0x42, 0xBC};
            ChunkSpaceCoords cscShort = TestHelpers.ChunkCoords(4, 12, 11, 2);
            TestHelpers.BytesAssert(aShort, ChunkSpaceCoords.ToBytes(cscShort));
            Assert.AreEqual(cscShort, ChunkSpaceCoords.FromBytes(aShort));

            // Example : (5, 2|4|23)
            byte[] aMedium = {0xA5, 0xC2, 0x02};
            ChunkSpaceCoords cscMedium = TestHelpers.ChunkCoords(5, 2, 4, 23);
            TestHelpers.BytesAssert(aMedium, ChunkSpaceCoords.ToBytes(cscMedium));
            Assert.AreEqual(cscMedium, ChunkSpaceCoords.FromBytes(aMedium));
            
            // Example : (8, 200|123|199)
            byte[] aLong = {8, 199, 123, 200};
            ChunkSpaceCoords cscLong = TestHelpers.ChunkCoords(8, 200, 123, 199);
            TestHelpers.BytesAssert(aLong, ChunkSpaceCoords.ToBytes(cscLong));
            Assert.AreEqual(cscLong, ChunkSpaceCoords.FromBytes(aLong));

            string sCanonic = "(4, 12|11|2)";
            Assert.AreEqual(
                TestHelpers.ChunkCoords(4, 12, 11, 2),
                ChunkSpaceCoords.FromCanonic(sCanonic));
            Assert.AreEqual(sCanonic, ChunkSpaceCoords.ToCanonic(TestHelpers.ChunkCoords(4, 12, 11, 2)));
        }

        [TestMethod]
        public void Ordering()
        {
            // (1, x|x|x) < (2, x|x|x)
            Assert.IsTrue(TestHelpers.ChunkCoords(1, 0, 0, 0) < TestHelpers.ChunkCoords(2, 0, 0, 0));

            // (4, 0|1|0) < (4, 1|1|0)
            Assert.IsTrue(TestHelpers.ChunkCoords(4, 0, 1, 0) < TestHelpers.ChunkCoords(4, 1, 1, 0));
        }

        [TestMethod]
        public void ParentChildRelation()
        {
            // (0, 0|0|0) is parent of anything
            Assert.IsTrue(
                TestHelpers.ChunkCoords((byte)0, 0, 0, 0).IsParentOf(TestHelpers.ChunkCoords(8, 1, 2, 3)));

            // (2, 1|1|1) is parent of (3, 2|3|2)
            Assert.IsTrue(
                TestHelpers.ChunkCoords(2, 1, 1, 1).IsParentOf(TestHelpers.ChunkCoords(3, 2, 3, 2)));

            // (2, 1|1|1) is not parent of (3, 6|1|2)
            Assert.IsFalse(
                TestHelpers.ChunkCoords(2, 1, 1, 1).IsParentOf(TestHelpers.ChunkCoords(3, 6, 1, 2)));

            // (2, 1|1|1) is parent of (3, 2|3|3)
            Assert.AreEqual(
                TestHelpers.ChunkCoords(2, 1, 1, 1),
                TestHelpers.ChunkCoords(3, 2, 3, 3).Parent);
        }

        [TestMethod]
        public void Paths()
        {
            // (0, 0|0|0) path is empty
            Assert.IsFalse(TestHelpers.ChunkCoords((byte)0, 0, 0, 0).RootPath.Any());

            // (4, 5|6|5) path is { 0x00, 0x07, 0x02, 0x05 }
            TestHelpers.BytesAssert(
                new byte[] { 0x00, 0x07, 0x02, 0x05 },
                TestHelpers.ChunkCoords(4, 5, 6, 5).RootPath.ToArray());
        }

        [TestMethod]
        public void StepDownPerformance()
        {
            Random rRandom = new Random();

            for (int I = 0; I < 1000000; I++)
            {
                ChunkSpaceCoords cscCoords = new ChunkSpaceCoords(ChunkSpaceLevel.Chunk, 0, 0, 0);

                for (int J = 0; J < 8; J++)
                    cscCoords = cscCoords.StepDown((byte)rRandom.Next(0, 7));

                Assert.IsTrue(cscCoords.Level == ChunkSpaceLevel.Voxel);
            }
        }
    }
}
