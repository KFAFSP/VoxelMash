using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Coords = VoxelMash.Grids.ChunkSpaceCoordinates;

namespace VoxelMashTest.Grids
{
    [TestClass]
    public class CoordinateSerializationTest
    {
        [TestMethod]
        public void Packed()
        {
            MemoryStream msTest = new MemoryStream();
            // ReSharper disable once RedundantArgumentDefaultValue
            Coords.SerializationHandler shHandler = new Coords.SerializationHandler(true);

            for (byte bShift = 0; bShift <= 8; bShift++)
                shHandler.Write(msTest, new Coords(bShift, 123, 213, 17));

            shHandler.Write(msTest, Coords.OutOfRange);
            Assert.AreEqual(
                22,
                msTest.Position);

            msTest.Seek(0, SeekOrigin.Begin);

            Coords cRead;
            Assert.AreEqual(4, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(0, 123, 213, 17), cRead);

            Assert.AreEqual(3, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(1, 123, 213, 17), cRead);
            Assert.AreEqual(3, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(2, 123, 213, 17), cRead);
            Assert.AreEqual(3, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(3, 123, 213, 17), cRead);

            Assert.AreEqual(2, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(4, 123, 213, 17), cRead);
            Assert.AreEqual(2, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(5, 123, 213, 17), cRead);
            Assert.AreEqual(2, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(6, 123, 213, 17), cRead);

            Assert.AreEqual(1, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(7, 123, 213, 17), cRead);

            Assert.AreEqual(1, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(8, 123, 213, 17), cRead);

            Assert.AreEqual(1, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(Coords.OutOfRange, cRead);

            msTest.Dispose();
        }

        [TestMethod]
        public void Unpacked()
        {
            MemoryStream msTest = new MemoryStream();
            Coords.SerializationHandler shHandler = new Coords.SerializationHandler(false);

            for (byte bShift = 0; bShift <= 8; bShift++)
                shHandler.Write(msTest, new Coords(bShift, 123, 213, 17));

            shHandler.Write(msTest, Coords.OutOfRange);
            Assert.AreEqual(
                4 * 10,
                msTest.Position);

            msTest.Seek(0, SeekOrigin.Begin);

            Coords cRead;
            Assert.AreEqual(4, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(0, 123, 213, 17), cRead);

            Assert.AreEqual(4, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(1, 123, 213, 17), cRead);
            Assert.AreEqual(4, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(2, 123, 213, 17), cRead);
            Assert.AreEqual(4, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(3, 123, 213, 17), cRead);

            Assert.AreEqual(4, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(4, 123, 213, 17), cRead);
            Assert.AreEqual(4, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(5, 123, 213, 17), cRead);
            Assert.AreEqual(4, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(6, 123, 213, 17), cRead);

            Assert.AreEqual(4, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(7, 123, 213, 17), cRead);

            Assert.AreEqual(4, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(new Coords(8, 123, 213, 17), cRead);

            Assert.AreEqual(4, shHandler.Read(msTest, out cRead));
            Assert.AreEqual(Coords.OutOfRange, cRead);

            msTest.Dispose();
        }
    }
}
