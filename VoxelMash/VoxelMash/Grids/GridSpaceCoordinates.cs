namespace VoxelMash.Grids
{
    public struct GridSpaceCoordinates
    {
        /* Coordinate ranges:
         * 
         *  LSB                                 MSB
         *  0000 0000 0000 0000 0000 0000 0000 0000
         *  VVVV BBBB CCCC CRRR RRRR RRRR RRRR RRRR
         *  16   16   32    32.768             16
         *  
         *  S0      Voxel
         *  S1-S3   BlockSpace
         *  S4      Block
         *  S5-S7   ChunkSpace
         *  S8      Chunk
         *  S9-S12  RegionSpace      
         *  S13     Region
         *  S14     Grid
         *  S15     OutOfRange
         *  
         *  ChunkSpacePart  : byte (UNSIGNED)
         *  RegionSpacePart : byte (UNSIGNED)
         *  GridSpacePart   : int (SIGNED)
         */

        private byte FShift;
        private int FX;
        private int FY;
        private int FZ;
    }
}
