using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VoxelMash.Serialization;

namespace VoxelMashTest
{
    [TestClass]
    public class BitStreamTest
    {
        [TestMethod]
        public void Read()
        {
            //    2 D       7E
            // 0010 1101 0111 1110
            MemoryStream msTest = new MemoryStream(new byte[]{0x2D, 0x7E});
            BitStreamReader bsrReader = new BitStreamReader(msTest, true);

            int iBits;
            Assert.AreEqual(2, bsrReader.ReadBits(16, out iBits));
            Assert.AreEqual(0x2D7E, iBits);

            bsrReader.Dispose();
        }

        [TestMethod]
        public void Write()
        {
            //    2 D       7E
            // 0010 1101 0111 1110
            MemoryStream msTest = new MemoryStream();
            BitStreamWriter bsrReader = new BitStreamWriter(msTest, true);

            Assert.AreEqual(2, bsrReader.WriteBits(0x2D7E, 16));
            Assert.AreEqual(0x2D, msTest.GetBuffer()[0]);
            Assert.AreEqual(0x7E, msTest.GetBuffer()[1]);

            bsrReader.Dispose();
        }
    }
}
