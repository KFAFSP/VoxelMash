using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VoxelMash;
using VoxelMash.Grids;

using Coords = VoxelMash.Grids.ChunkSpaceCoordinates;

namespace VoxelMashTest
{
    [TestClass]
    public class SparseChunkSerializationTest
    {
        private const int C_Complexity = 3000;

        private static readonly Random _FRandom = new Random();
        private static readonly List<Coords> _FRandomBlocks = new List<Coords>();

        private static Coords GetRandomBlock()
        {
            return new Coords(
                4,
                (byte)SparseChunkSerializationTest._FRandom.Next(0, 15),
                (byte)SparseChunkSerializationTest._FRandom.Next(0, 15),
                (byte)SparseChunkSerializationTest._FRandom.Next(0, 15));
        }

        [TestInitialize]
        public void Randomize()
        {
            for (int I = 0; I < SparseChunkSerializationTest.C_Complexity; I++)
                SparseChunkSerializationTest._FRandomBlocks.Add(SparseChunkSerializationTest.GetRandomBlock());

            SparseChunkSerializationTest._FRandomBlocks.Shuffle(SparseChunkSerializationTest._FRandom);
        }

        [TestMethod]
        public void Serialize()
        {
            SparseChunkOctree scoOctree = new SparseChunkOctree();

            int iDiscard = 0;
            foreach (Coords cBlock in SparseChunkSerializationTest._FRandomBlocks)
                scoOctree.Set(cBlock, 1, ref iDiscard);

            MemoryStream msTest = new MemoryStream();
            SparseChunkOctree.SerializationHandler shHandler = new SparseChunkOctree.SerializationHandler();
            shHandler.Write(msTest, scoOctree);

            msTest.Seek(0, SeekOrigin.Begin);
            shHandler.Read(msTest, scoOctree);

            foreach (Coords cBlock in SparseChunkSerializationTest._FRandomBlocks)
            {
                Coords cCopy = cBlock;
                ushort nValue;
                scoOctree.Get(ref cCopy, out nValue);
                Assert.AreEqual(1, nValue);
            }

            msTest.Dispose();
        }
    }
}
