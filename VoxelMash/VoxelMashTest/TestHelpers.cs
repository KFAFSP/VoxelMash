using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VoxelMash.Grids;

namespace VoxelMashTest
{
    public static class TestHelpers
    {
        public static ChunkSpaceCoords ChunkCoords(
            ChunkSpaceLevel ALevel,
            byte AX, byte AY, byte AZ)
        {
            return new ChunkSpaceCoords(ALevel, AX, AY, AZ);
        }
        public static ChunkSpaceCoords ChunkCoords(byte ALevel, byte AX, byte AY, byte AZ)
        {
            return TestHelpers.ChunkCoords((ChunkSpaceLevel)ALevel, AX, AY, AZ);
        }

        public static void BytesAssert(byte[] AExpected, byte[] AActual)
        {
            Assert.IsTrue(
                AExpected.SequenceEqual(AActual),
                String.Format(
                    "Expected: {{{0}}}, Actual: {{{1}}}",
                    String.Join(", ", AExpected),
                    String.Join(", ", AActual)));
        }
    }
}
