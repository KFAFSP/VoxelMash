using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Coords = VoxelMash.Grids.ChunkSpaceCoordinates;

namespace VoxelMashTest
{
    [TestClass]
    public class CoordinatePerformaceTest
    {
        private const int C_Complexity = 100000;

        private static readonly Random _FRandom = new Random();
        private static readonly List<Coords> _FRandomCoords = new List<Coords>();

        private static Coords GetRandom()
        {
            return new Coords(
                (byte)CoordinatePerformaceTest._FRandom.Next(0, 8),
                (byte)CoordinatePerformaceTest._FRandom.Next(0, 255),
                (byte)CoordinatePerformaceTest._FRandom.Next(0, 255),
                (byte)CoordinatePerformaceTest._FRandom.Next(0, 255));
        }

        [TestInitialize]
        public void Randomize()
        {
            for (int I = 0; I < CoordinatePerformaceTest.C_Complexity; I++)
                CoordinatePerformaceTest._FRandomCoords.Add(CoordinatePerformaceTest.GetRandom());
        }

        [TestMethod]
        public void Constructor()
        {
            for (int I = 0; I < CoordinatePerformaceTest.C_Complexity; I++)
            {
                Coords cTest = new Coords(0, 1, 2, 3);
                Assert.AreEqual(1, cTest.Volume);
            }
        }

        [TestMethod]
        public void StepDown()
        {
            foreach (Coords cCoords in CoordinatePerformaceTest._FRandomCoords)
            {
                if (cCoords.Shift != 0)
                    cCoords.StepDown((byte)CoordinatePerformaceTest._FRandom.Next(0, 7));
            }
        }

        [TestMethod]
        public void StepUp()
        {
            foreach (Coords cCoords in CoordinatePerformaceTest._FRandomCoords)
                cCoords.StepUp();
        }

        [TestMethod]
        public void IsParentOf()
        {
            for (int I = 0; I < CoordinatePerformaceTest._FRandomCoords.Count - 1; I++)
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                CoordinatePerformaceTest._FRandomCoords[I].IsParentOf(
                    CoordinatePerformaceTest._FRandomCoords[CoordinatePerformaceTest._FRandomCoords.Count - I - 1]);
        }

        [TestMethod]
        public void GetOffset()
        {
            foreach (Coords cCoords in CoordinatePerformaceTest._FRandomCoords)
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                cCoords.GetOffset();
        }

        [TestMethod]
        public void GetPath()
        {
            foreach (Coords cCoords in CoordinatePerformaceTest._FRandomCoords)
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                cCoords.GetPath();
        }

        [TestMethod]
        public void GetIndex()
        {
            foreach (Coords cCoords in CoordinatePerformaceTest._FRandomCoords)
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                cCoords.GetIndex();
        }

        [TestMethod]
        public void CompareTo()
        {
            for (int I = 0; I < CoordinatePerformaceTest._FRandomCoords.Count - 1; I++)
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                CoordinatePerformaceTest._FRandomCoords[I].CompareTo(
                    CoordinatePerformaceTest._FRandomCoords[CoordinatePerformaceTest._FRandomCoords.Count - I - 1]);
        }
    }
}
