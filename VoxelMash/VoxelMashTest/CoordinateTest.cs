using System;
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
            Assert.AreEqual(this.Coords(4, 5, 6, 5), this.Coords((byte)0, 0, 0, 0).StepDown(new byte[]{0x00, 0x07, 0x02, 0x05}));
        }

        [TestMethod]
        public void StepUp()
        {
            // (4, 5|6|5) up = (3, 2|3|2)
            Assert.AreEqual(this.Coords(3, 2, 3, 2), this.Coords(4, 5, 6, 5).StepUp());

            // (0, 0|0|0) up = (0, 0|0|0)
            Assert.AreEqual(this.Coords((byte)0, 0, 0, 0), this.Coords((byte)0, 0, 0, 0).StepUp());
        }
    }
}
