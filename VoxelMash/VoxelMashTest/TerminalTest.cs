using Microsoft.VisualStudio.TestTools.UnitTesting;

using VoxelMash.Grids;

namespace VoxelMashTest
{
    [TestClass]
    public class TerminalTest
    {
        [TestMethod]
        public void Serialization()
        {
            // Example : ((5, 2|4|23), 15222)
            byte[] aMedium = { 0xA5, 0xC2, 0x02, 0x76, 0x3B };
            ChunkSpaceCoords cscTest = TestHelpers.ChunkCoords(5, 2, 4, 23);
            ushort nMaterial = 15222;
            TestHelpers.BytesAssert(aMedium, GridChunkTerminal.ToBytes(cscTest, nMaterial));
            GridChunkTerminal.FromBytes(aMedium, out cscTest, out nMaterial);
            Assert.AreEqual(TestHelpers.ChunkCoords(5, 2, 4, 23), cscTest);
            Assert.AreEqual(15222, nMaterial);

            string sCanonic = "((7, 1|18|29), 43)";
            GridChunkTerminal.FromCanonic(sCanonic, out cscTest, out nMaterial);
            Assert.AreEqual(TestHelpers.ChunkCoords(7, 1, 18, 29), cscTest);
            Assert.AreEqual(43, nMaterial);
            Assert.AreEqual(sCanonic, GridChunkTerminal.ToCanonic(cscTest, nMaterial));
        }
    }
}
