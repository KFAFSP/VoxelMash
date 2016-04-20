using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VoxelMash.Grids;
using VoxelMash.Serialization;

using Coords = VoxelMash.Grids.ChunkSpaceCoordinates;

namespace VoxelMashTest
{
    [TestClass]
    public class McChunkComparisonTest
    {
        [TestMethod]
        public void Convert()
        {
            int iLevel = 0;
            do
            {
                string sPrefix = String.Format(@".\world_chunks\y{0}_", iLevel);
                if (!File.Exists(sPrefix + "blocks"))
                    break;

                SparseChunkOctree scoThis = new SparseChunkOctree();

                FileStream fsMcCompressed = new FileStream(sPrefix + "mcall.gzip", FileMode.Create, FileAccess.Write);
                GZipStream gszMcCompress = new GZipStream(fsMcCompressed, CompressionMode.Compress, true);

                FileStream fsBlocks = new FileStream(sPrefix + "blocks", FileMode.Open, FileAccess.Read);
                FileStream fsMeta = new FileStream(sPrefix + "meta", FileMode.Open, FileAccess.Read);
                FileStream fsAdd = null;
                if (File.Exists(sPrefix + "add"))
                    fsAdd = new FileStream(sPrefix + "add", FileMode.Open, FileAccess.Read);

                byte bAddBuff = 0x00;
                byte bMetaBuff = 0x00;
                bool bAdvSupplements = true;

                int iBalance = 0;
                for (byte bX = 0; bX < 16; bX++)
                    for (byte bZ = 0; bZ < 16; bZ++)
                        for (byte bY = 0; bY < 16; bY++)
                        {
                            byte bBlockId = fsBlocks.SafeReadByte();
                            gszMcCompress.WriteByte(bBlockId);
                            byte bMetaData;
                            byte bAddData;

                            if (bAdvSupplements)
                            {
                                bAdvSupplements = false;
                                bAddBuff = fsAdd == null ? (byte)0x00 : fsAdd.SafeReadByte();
                                bMetaBuff = fsMeta.SafeReadByte();

                                gszMcCompress.WriteByte(bAddBuff);
                                gszMcCompress.WriteByte(bMetaBuff);

                                bMetaData = (byte)(bMetaBuff >> 4);
                                bAddData = (byte)(bAddBuff >> 4);
                            }
                            else
                            {
                                bMetaData = (byte)(bMetaBuff & 0x0F);
                                bAddData = (byte)(bAddBuff & 0x0F);
                            }

                            ushort nMaterial = (ushort)(bMetaData | (bBlockId << 4) | (bAddData << 12));
                            scoThis.Set(new Coords(4, bX, bY, bZ), nMaterial, ref iBalance);
                        }

                Trace.WriteLine(String.Format("Loaded slice {0}, Balance was {1}, {2} Terminals.", iLevel, iBalance, scoThis.TerminalCount));

                long nMcSize = fsBlocks.Position + fsMeta.Position + (fsAdd != null ? fsAdd.Position : 0);
                gszMcCompress.Close();
                long nMcCompressed = fsMcCompressed.Length;
                fsMcCompressed.Close();

                fsBlocks.Close();
                fsMeta.Close();
                if (fsAdd != null)
                    fsAdd.Close();

                FileStream fsWrite = new FileStream(sPrefix + "materials.sco.gzip", FileMode.Create, FileAccess.Write);
                GZipStream gzsScoCompressed = new GZipStream(fsWrite, CompressionMode.Compress, true);
                SparseChunkOctree.SerializationHandler shHandler = new SparseChunkOctree.SerializationHandler();
                shHandler.Write(gszMcCompress, scoThis);
                gzsScoCompressed.Close();
                long nScoSize = fsWrite.Length;
                fsWrite.Close();

                Trace.WriteLine(String.Format("MC has {0} Bytes total, {1} Bytes compressed , SCO has {2} Bytes compressed.", nMcSize, nMcCompressed, nScoSize));

                iLevel++;
            } while (true);

            Assert.IsTrue(iLevel > 0);
        }
    }
}
