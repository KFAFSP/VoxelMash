using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VoxelMash.Grids;
using VoxelMash.Serialization;

using Coords = VoxelMash.Grids.ChunkSpaceCoordinates;

namespace VoxelMashTest.Grids
{
    [TestClass]
    public class McChunkComparisonTest
    {
        [TestMethod]
        public void Convert()
        {
            for (int iSlice = 0; iSlice < 6; iSlice++)
            {
                string sPrefix = String.Format(@".\world_chunks\y{0}_", iSlice);

                SparseChunkOctree scoOctree = new SparseChunkOctree();
                int iBalance = 0;

                FileStream fsCompressedMats = new FileStream(sPrefix + "mats.gzip", FileMode.Create, FileAccess.Write);
                GZipStream gzsCompressedMats = new GZipStream(fsCompressedMats, CompressionLevel.Fastest, true);

                using (FileStream fsBlockIDs = new FileStream(sPrefix + "blocks", FileMode.Open, FileAccess.Read))
                using (FileStream fsMetaData = new FileStream(sPrefix + "meta", FileMode.Open, FileAccess.Read))
                {
                    FileStream fsBlockAdd = null;
                    if (File.Exists(sPrefix + "add"))
                        fsBlockAdd = new FileStream(sPrefix + "add", FileMode.Open, FileAccess.Read);

                    byte bCarryMeta = 0x0;
                    byte bCarryAdd = 0x0;
                    for (int I = 0; I < 0xFFF; I++)
                    {
                        byte bBlock = fsBlockIDs.SafeReadByte();
                        byte bMeta, bAdd;

                        if (I%2 == 0)
                        {
                            bCarryMeta = fsMetaData.SafeReadByte();
                            bCarryAdd = fsBlockAdd != null ? fsBlockAdd.SafeReadByte() : (byte)0x00;

                            bMeta = (byte)(bCarryMeta >> 4);
                            bAdd = (byte)(bCarryAdd >> 4);
                        }
                        else
                        {
                            bMeta = bCarryMeta;
                            bAdd = bCarryAdd;
                        }

                        byte bY = (byte)(I%16);
                        byte bZ = (byte)((I/16)%16);
                        byte bX = (byte)((I/256)%16);

                        ushort nMaterial = (ushort)((bAdd << 12) | (bMeta << 8) | bBlock);
                        gzsCompressedMats.WriteUInt16(nMaterial);
                        scoOctree.Set(new Coords(4, bX, bY, bZ), nMaterial, ref iBalance);
                    }
                }

                gzsCompressedMats.Close();

                Debug.WriteLine("Converted slice {0}.", iSlice);
                Debug.WriteLine("Amounted to {0} terminals with volume {1}.", scoOctree.TerminalCount, iBalance);
                Debug.WriteLine("Materials compressed to {0}B ({1:F2}% gzip compression).",
                    fsCompressedMats.Length,
                    100.0d - ((double)fsCompressedMats.Length / 8192.0d * 100.0d));

                fsCompressedMats.Close();

                FileStream fsCompressedOctree = new FileStream(sPrefix + "octree.gzip", FileMode.Create, FileAccess.Write);
                GZipStream gzsCompressedOctree = new GZipStream(fsCompressedOctree, CompressionLevel.Fastest, true);

                MemoryStream msUncompressedOctree = new MemoryStream();

                SparseChunkOctree.SerializationHandler shHandler = new SparseChunkOctree.SerializationHandler();
                shHandler.Write(msUncompressedOctree, scoOctree);
                shHandler.Write(gzsCompressedOctree, scoOctree);

                gzsCompressedOctree.Close();

                Debug.WriteLine("Octree compressed to {0}B ({1:F2}% gzip compression).",
                    fsCompressedOctree.Length,
                    100.0d - ((double)fsCompressedOctree.Length / (double)msUncompressedOctree.Length * 100.0d));

                msUncompressedOctree.Dispose();
                fsCompressedOctree.Close();
            }
        }
    }
}
