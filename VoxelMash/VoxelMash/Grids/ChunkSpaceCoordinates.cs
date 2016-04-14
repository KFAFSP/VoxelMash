using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace VoxelMash.Grids
{
    interface IChunkSpaceCoords
    {
        ChunkSpaceLevel Level { get; }

        byte X { get; }
        byte Y { get; }
        byte Z { get; }
    }

    struct ChunkSpaceCoords : IChunkSpaceCoords
    {
        private readonly ChunkSpaceLevel FLevel;

        private readonly byte FX;
        private readonly byte FY;
        private readonly byte FZ;

        public ChunkSpaceCoords(
            ChunkSpaceLevel ALevel,
            byte AX, byte AY, byte AZ)
        {
            this.FLevel = ALevel;

            this.FX = AX;
            this.FY = AY;
            this.FZ = AZ;
        }

        public ChunkSpaceLevel Level
        {
            get { return this.FLevel; }
        }

        public byte X
        {
            get { return this.FX; }
        }
        public byte Y
        {
            get { return this.FY; }
        }
        public byte Z
        {
            get { return this.FZ; }
        }
    }
}