using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VoxelMash;
using Coords = VoxelMash.Grids.ChunkSpaceCoordinates;

namespace VoxelMashTest
{
    [TestClass]
    public class CoordinateConstraintsTest
    {
        private static void AreSequencesEqual<T>(
            IEnumerable<T> AExpect,
            IEnumerable<T> AActual)
        {
            T[] aExpect = AExpect.ToArray();
            T[] aActual = AActual != null ? AActual.ToArray() : null;

            if (AActual == null || !aActual.SequenceEqual(aExpect))
            {
                string sActual = AActual != null
                    ? String.Join(", ", aActual)
                    : "null";

                throw new AssertFailedException(String.Format("Expected: {0}, Actually: {1}",
                    String.Join(", ", aExpect),
                    sActual));
            }
        }

        [TestMethod]
        public void Constructor()
        {
            // Constraint 1 : shift must be in range.
            Trace.WriteLine("Constraint 1 : shift must be in range.");
            try
            {
                new Coords(120, 1, 42, 4).StepUp();
                Assert.Fail("Failed to throw exception.");
            }
            catch (ArgumentException) { }
        }

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

            // Constraint 3 : step-up without changes yields false.
            Trace.WriteLine("Constraint 3 : step-up without changes yields false.");
            Assert.IsFalse(new Coords(8, 0, 0, 0).StepUp());

            // Constraint 4 : step-up with changes yields true.
            Trace.WriteLine("Constraint 4 : step-up with changes yields true.");
            Assert.IsTrue(new Coords(4, 0, 0, 0).StepUp());
        }

        [TestMethod]
        public void MoveNext()
        {
            // Constraint 1 : voxels should return their siblings.
            Trace.WriteLine("Constraint 1 : moving between siblings.");
            Assert.AreEqual(
                new Coords(0, 1, 0, 0),
                new Coords(0, 0, 0, 0).GetSuccessor());

            // Constraint 2 : upwards propagation.
            Trace.WriteLine("Constraint 2 : upward propagation.");
            Assert.AreEqual(
                new Coords(1, 1, 0, 0),
                new Coords(0, 1, 1, 1).GetSuccessor());

            // Constraint 3 : last voxel should return out of range.
            Trace.WriteLine("Constraint 3 : out of range.");
            Assert.AreEqual(
                Coords.OutOfRange,
                Coords.LastVoxel.GetSuccessor());
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

        [TestMethod]
        public void GetOffset()
        {
            // Constraint 1 : (0, x|y|z) remains unchanged.
            Trace.WriteLine("Constraint 1 : voxel constance.");
            Assert.AreEqual(
                new Coords(0, 10, 11, 12),
                new Coords(0, 10, 11, 12).GetOffset());

            // Constraint 2 : correctness.
            Trace.WriteLine("Constraint 2 : correctness.");

            // Example : trivial solution.
            Trace.WriteLine("Example 1 : trivial solution.");
            Assert.AreEqual(
                new Coords(0, 0, 0, 0),
                new Coords(8, 0, 0, 0).GetOffset());

            // Example : (4, 7|9|11) has its first child at (0, 112|144|176)
            Trace.WriteLine("Example 2 : multi step-down.");
            Assert.AreEqual(
                new Coords(0, 112, 144, 176),
                new Coords(4, 7, 9, 11).GetOffset());
        }

        [TestMethod]
        public void GetPath()
        {
            // Constraint 1 : (8, 0|0|0) results in 0x0.
            Trace.WriteLine("Constraint 1 : root path is zero.");
            Assert.AreEqual(
                0x0,
                new Coords(8, 0, 0, 0).GetPath());

            // Constraint 2 : correctness.
            Trace.WriteLine("Constraint 2 : correctness.");

            // Example : (4, 9|3|1) is parent of (3, 18|7|3) via (0, 1, 1)
            Trace.WriteLine("Example 1 : single step path.");
            Assert.AreEqual(
                0x0 | 0x2 | 0x4,
                new Coords(3, 18, 7, 3).GetPath());

            // Example : (0, 200|24|1) is child of (4, 12|1|0) [4 levels down]
            Trace.WriteLine("Example 2 : multi step path (downward).");
            CoordinateConstraintsTest.AreSequencesEqual(
                new byte[]{0x3, 0x0, 0x0, 0x4},
                new Coords(0, 200, 24, 1).GetPath(false, 4));

            // Example : (0, 200|24|1) is child of (4, 12|1|0) [4 levels down]
            Trace.WriteLine("Example 3 : multi step path (upward).");
            CoordinateConstraintsTest.AreSequencesEqual(
                new byte[] { 0x3, 0x0, 0x0, 0x4 }.Reverse(),
                new Coords(0, 200, 24, 1).GetPath(true, 4));
        }

        [TestMethod]
        public void GetIndex()
        {
            // Constraint 1 : (0, 0|0|0) results in 0x00000000.
            Trace.WriteLine("Contraint 1 : first voxel has smallest index.");
            Assert.AreEqual(
                0x00000000,
                new Coords(0, 0, 0, 0).GetIndex());

            // Constraint 2 : (8, 0|0|0) results in 0x08000000.
            Trace.WriteLine("Contraint 2 : root has highest index.");
            Assert.AreEqual(
                0x08000000,
                new Coords(8, 0, 0, 0).GetIndex());

            // Constraint 3 : Siblings are sorted in the cartesian coordinate system l-r, b-t, f-b.
            Trace.WriteLine("Contraint 3 : sibling cartesian sorting.");

            // Example : index(4, 0|0|0) < index(4, 1|1|0)
            Trace.WriteLine("Example 1");
            Assert.IsTrue(new Coords(4, 0, 0, 0).GetIndex() < new Coords(4, 1, 1, 0).GetIndex());

            // Example : index(4, 1|1|0) < index(4, 1|1|1)
            Trace.WriteLine("Example 2");
            Assert.IsTrue(new Coords(4, 1, 1, 0).GetIndex() < new Coords(4, 1, 1, 1).GetIndex());

            // Constraint 4 : Smaller volumes have lower indices.
            Trace.WriteLine("Constraint 4 : smaller volumes have lower indices.");
            Assert.IsTrue(new Coords(1, 9, 3, 7).GetIndex() < new Coords(4, 3, 2, 6).GetIndex());
        }

        [TestMethod]
        public void GetLastChild()
        {
            // Constraint 1 : (0, x|y|z) results in (0, x|y|z).
            Trace.WriteLine("Contraint 1 : constance of voxel nodes.");
            Assert.AreEqual(
                new Coords(0, 12, 1, 9),
                new Coords(0, 12, 1, 9).GetLastChild());

            // Constraint 2 : (8, 0|0|0) results in (0, 255|255|255).
            Trace.WriteLine("Constraint 2 : root node results in highest voxel.");
            Assert.AreEqual(
                new Coords(0, 255, 255, 255),
                new Coords(8, 0, 0, 0).GetLastChild());

            // Constraint 3 : correctness.
            Trace.WriteLine("Constraint 3 : correctness.");

            // Example : (1, 0|0|0) has last child (0, 1|1|1) [1 level down].
            Trace.WriteLine("Example 1 : single step-down.");
            Assert.AreEqual(
                new Coords(0, 1, 1, 1),
                new Coords(1, 0, 0, 0).GetLastChild());

            // Example : (3, 0|0|0) has last child (0, 7|7|7) [3 levels down].
            Trace.WriteLine("Example 2 : mutli step-down.");
            Assert.AreEqual(
                new Coords(0, 7, 7, 7),
                new Coords(3, 0, 0, 0).GetLastChild());

            // Constraint 4 : (8, 1|0|0) is greater than anything.
            Trace.WriteLine("Constraint 4 : largest node with impossible value.");
            Assert.IsTrue(
                Coords.LastVoxel.GetLastChildPlusOne() > Coords.LastVoxel);
        }

        [TestMethod]
        public void CompareTo()
        {
            // Constraint 1 : root comes before anything.
            Trace.WriteLine("Constraint 1 : root is smallest element.");
            Assert.IsTrue(new Coords(8, 0, 0, 0) < new Coords(4, 1, 7, 3));

            // Constraint 2 : last voxel comes after anything.
            Trace.WriteLine("Constraint 2 : last voxel is largest element.");
            Assert.IsTrue(new Coords(0, 255, 255, 255) > new Coords(4, 1, 7, 3));

            // Constraint 3 : parent comes before child.
            Trace.WriteLine("Constraint 3 : parent before child.");

            // Example : (4, 1|1|1) < (3, 2|2|2)
            Trace.WriteLine("Example 1 : single step.");
            Assert.IsTrue(new Coords(4, 1, 1, 1) < new Coords(3, 2, 2, 2));

            // Example : (5, 0|0|0) < (3, 2|2|2)
            Trace.WriteLine("Example 2 : multi step.");
            Assert.IsTrue(new Coords(5, 0, 0, 0) < new Coords(3, 2, 2, 2));

            // Constraint 4 : hierarchical sorting.
            Trace.WriteLine("Constraint 4 : hierarchical sorting.");
            List<Coords> lSorted = new List<Coords>
            {
                new Coords(4, 0, 0, 0),
                new Coords(4, 0, 0, 0),
                    new Coords(3, 0, 0, 0),
                new Coords(4, 1, 0, 0),
                    new Coords(3, 2, 0, 0),
                    new Coords(3, 3, 1, 0),
                        new Coords(2, 6, 2, 0),
                    new Coords(3, 2, 1, 1),
                        new Coords(2, 4, 2, 3),
                new Coords(4, 0, 0, 1)
            };
            List<Coords> lTest = lSorted.ToList();
            lTest.Shuffle(new Random());
            lTest.Sort();
            CoordinateConstraintsTest.AreSequencesEqual(
                lSorted,
                lTest);

            // Constraint 5 : parent grouping.
            Trace.WriteLine("Constraint 5 : parent grouping.");
            lSorted = new List<Coords>
            {
                new Coords(0, 0, 0, 0),
                new Coords(0, 1, 0, 0),
                new Coords(0, 0, 1, 0),
                new Coords(0, 1, 1, 0),

                new Coords(0, 3, 0, 0),
                new Coords(0, 0, 3, 0),
                new Coords(0, 3, 2, 0),

                new Coords(0, 4, 4, 0),
                new Coords(0, 5, 4, 0),
                new Coords(0, 4, 5, 0)
            };
            lTest = lSorted.ToList();
            lTest.Shuffle(new Random());
            lTest.Sort();
            CoordinateConstraintsTest.AreSequencesEqual(
                lSorted,
                lTest);
        }
    }
}
