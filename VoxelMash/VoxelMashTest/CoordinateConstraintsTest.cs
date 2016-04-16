using System;
using System.Diagnostics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Coords = VoxelMash.Grids.ChunkSpaceCoordinates;

namespace VoxelMashTest
{
    [TestClass]
    public class CoordinateConstraintsTest
    {
        [TestMethod]
        public void StepDown()
        {
            // Constraint 1 : (0, x|y|z) cannot be stepped-down.
            Trace.WriteLine("Contraint 1 : leaf node step-down is invalid.");
            try
            {
                new Coords(0, 1, 2, 3).StepDown(0x0);
                Assert.Fail("Failed to throw expection.");
            }
            catch (InvalidOperationException) { }

            // Constraint 2 : parent-child relationship respected.
            Trace.WriteLine("Constraint 2 : correctness.");

            // Example : (0, 2|0|0) is child of (1, 1|0|0)
            Trace.WriteLine("Example 1 : single step-down.");
            Assert.AreEqual(
                new Coords(0, 2, 0, 0),
                new Coords(1, 1, 0, 0).GetChild(0x0));

            // Example : (0, 200|24|1) is child of (4, 12|1|0) [4 levels down]
            Trace.WriteLine("Example 2 : multi step-down.");
            Assert.AreEqual(
                new Coords(0, 200, 24, 1),
                new Coords(4, 12, 1, 0)
                    .GetChild(0x1 | 0x2 | 0x0)
                    .GetChild(0x0 | 0x0 | 0x0)
                    .GetChild(0x0 | 0x0 | 0x0)
                    .GetChild(0x0 | 0x0 | 0x4));
        }

        [TestMethod]
        public void StepUp()
        {
            // Constraint 1 : (8, 0|0|0) remains unchanged.
            Trace.WriteLine("Contraint 1 : root node constance.");
            Assert.AreEqual(
                new Coords(8, 0, 0, 0),
                new Coords(8, 0, 0, 0).GetParent());

            // Constraint 2 : parent-child relationship respected.
            Trace.WriteLine("Contraint 2 : correctness.");

            // Example : (0, 2|0|0) is child of (1, 1|0|0)
            Trace.WriteLine("Example 1 : single step-up.");
            Assert.AreEqual(
                new Coords(1, 1, 0, 0),
                new Coords(0, 2, 0 ,0).GetParent());

            // Example : (0, 200|24|1) is child of (4, 12|1|0) [4 levels down]
            Trace.WriteLine("Example 2 : multi step-up.");
            Assert.AreEqual(
                new Coords(4, 12, 1, 0),
                new Coords(0, 200, 24, 1).GetParent(4));
        }

        [TestMethod]
        public void IsParentOf()
        {
            // Constraint 1 : (8, 0|0|0) is parent of anything.
            Trace.WriteLine("Contraint 1 : root node prefixes everything.");
            Assert.IsTrue(
                new Coords(8, 0, 0, 0).IsParentOf(new Coords(1, 64, 122, 90)));

            // Constraint 2 : (0, x|y|z) is parent of nothing.
            Trace.WriteLine("Constraint 2 : leaf node prefixes nothing.");
            Assert.IsFalse(
                new Coords(0, 1, 10, 4).IsParentOf(new Coords(1, 1, 10, 4)));

            // Constraint 3 : (l, x|y|z) does not prefix istelf.
            Trace.WriteLine("Constraint 3 : antisymmetry.");
            Assert.IsFalse(
                new Coords(1, 1, 10, 4).IsParentOf(new Coords(1, 1, 10, 4)));

            // Constraint 4 : parent-child relationship respected.
            Trace.WriteLine("Constraint 4 : correctness.");

            // Example : (0, 2|0|0) is child of (1, 1|0|0)
            Trace.WriteLine("Example 1 : immediate parent.");
            Assert.IsTrue(
                new Coords(1, 1, 0, 0).IsParentOf(new Coords(0, 2, 0, 0)));

            // Example : (0, 200|24|1) is child of (4, 12|1|0) [4 levels down]
            Trace.WriteLine("Example 2 : non-immediate parent.");
            Assert.IsTrue(
                new Coords(4, 12, 1, 0).IsParentOf(new Coords(0, 200, 24, 1)));
        }
    }
}
