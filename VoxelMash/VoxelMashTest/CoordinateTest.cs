﻿using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VoxelMash.Grids;

namespace VoxelMashTest
{
    [TestClass]
    public class CoordinateTest
    {
        private ChunkSpaceCoords Coords(
            ChunkSpaceLevel ALevel,
            byte AX, byte AY, byte AZ)
        {
            return new ChunkSpaceCoords(ALevel, AX, AY, AZ);
        }
        private ChunkSpaceCoords Coords(byte ALevel, byte AX, byte AY, byte AZ)
        {
            return this.Coords((ChunkSpaceLevel)ALevel, AX, AY, AZ);
        }

        private void BytesAssert(byte[] AExpected, byte[] AActual)
        {
            Assert.IsTrue(AExpected.SequenceEqual(AActual), String.Format("Expected: {{{0}}}, Actual: {{{1}}}", String.Join(", ", AExpected), String.Join(", ", AActual)));
        }

        [TestMethod]
        public void NormalAddition()
        {
            // (8, 2|3|4) + (8, 3|2|1) = (8, 5|5|5)
            Assert.AreEqual(this.Coords(8, 5, 5, 5), this.Coords(8, 2, 3, 4) + this.Coords(8, 3, 2, 1));
        }

        [TestMethod]
        public void StepDownAddition()
        {
            // (0, 0|0|0) + (8, 1|1|1) = (8, 1|1|1)
            Assert.AreEqual(this.Coords(8, 1, 1, 1), this.Coords((byte)0, 0, 0, 0) + this.Coords(8, 1, 1, 1));

            // (7, 1|1|1) + (8, 1|1|1) = (8, 3|3|3)
            Assert.AreEqual(this.Coords(8, 3, 3, 3), this.Coords(7, 1, 1, 1) + this.Coords(8, 1, 1, 1));
        }

        [TestMethod]
        public void StepDown()
        {
            // (7, 1|1|1) down 1|1|1 = (7, 1|1|1) + (8, 1|1|1)
            Assert.AreEqual(this.Coords(7, 1, 1, 1) + this.Coords(8, 1, 1, 1), this.Coords(7, 1, 1, 1).StepDown(0x07));

            // (0, 0|0|0) down 0|0|0 down 1|1|1 down 0|1|0 down 1|0|1 = (4, 5, 6, 5)
            Assert.AreEqual(this.Coords(4, 5, 6, 5), this.Coords((byte)0, 0, 0, 0).StepDown(new byte[] { 0x00, 0x07, 0x02, 0x05 }));
        }

        [TestMethod]
        public void StepUp()
        {
            // (4, 5|6|5) up = (3, 2|3|2)
            Assert.AreEqual(this.Coords(3, 2, 3, 2), this.Coords(4, 5, 6, 5).StepUp());

            // (0, 0|0|0) up = (0, 0|0|0)
            Assert.AreEqual(this.Coords((byte)0, 0, 0, 0), this.Coords((byte)0, 0, 0, 0).StepUp());
        }

        [TestMethod]
        public void Serialization()
        {
            // Example : (4, 12|11|2)
            byte[] aShort = {0x42, 0xBC};
            ChunkSpaceCoords cscShort = this.Coords(4, 12, 11, 2);
            this.BytesAssert(aShort, ChunkSpaceCoords.ToBytes(cscShort));
            Assert.AreEqual(cscShort, ChunkSpaceCoords.FromBytes(aShort));

            // Example : (5, 2|4|23)
            byte[] aMedium = {0xA5, 0xC2, 0x02};
            ChunkSpaceCoords cscMedium = this.Coords(5, 2, 4, 23);
            this.BytesAssert(aMedium, ChunkSpaceCoords.ToBytes(cscMedium));
            Assert.AreEqual(cscMedium, ChunkSpaceCoords.FromBytes(aMedium));
            
            // Example : (8, 200|123|199)
            byte[] aLong = {8, 199, 123, 200};
            ChunkSpaceCoords cscLong = this.Coords(8, 200, 123, 199);
            this.BytesAssert(aLong, ChunkSpaceCoords.ToBytes(cscLong));
            Assert.AreEqual(cscLong, ChunkSpaceCoords.FromBytes(aLong));

            Assert.AreEqual(this.Coords(4, 12, 11, 2), ChunkSpaceCoords.FromCanonic("(4, 12|11|2)"));
        }

        [TestMethod]
        public void Ordering()
        {
            // (1, x|x|x) < (2, x|x|x)
            Assert.IsTrue(this.Coords(1, 0, 0, 0) < this.Coords(2, 0, 0, 0));

            // (4, 0|1|0) < (4, 1|1|0)
            Assert.IsTrue(this.Coords(4, 0, 1, 0) < this.Coords(4, 1, 1, 0));
        }

        [TestMethod]
        public void ParentChildRelation()
        {
            // (0, 0|0|0) is parent of anything
            Assert.IsTrue(this.Coords((byte)0, 0, 0, 0).IsParentOf(this.Coords(8, 1, 2, 3)));

            // (2, 1|1|1) is parent of (3, 2|3|2)
            Assert.IsTrue(this.Coords(2, 1, 1, 1).IsParentOf(this.Coords(3, 2, 3, 2)));

            // (2, 1|1|1) is not parent of (3, 6|1|2)
            Assert.IsFalse(this.Coords(2, 1, 1, 1).IsParentOf(this.Coords(3, 6, 1, 2)));

            // (2, 1|1|1) is parent of (3, 2|3|3)
            Assert.AreEqual(this.Coords(2, 1, 1, 1), this.Coords(3, 2, 3, 3).Parent);
        }

        [TestMethod]
        public void Paths()
        {
            // (0, 0|0|0) path is empty
            Assert.IsFalse(this.Coords((byte)0, 0, 0, 0).RootPath.Any());

            // (4, 5|6|5) path is { 0x00, 0x07, 0x02, 0x05 }
            this.BytesAssert(new byte[] { 0x00, 0x07, 0x02, 0x05 }, this.Coords(4, 5, 6, 5).RootPath.ToArray());
        }
    }
}
