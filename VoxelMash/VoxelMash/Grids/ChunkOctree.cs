using System.Diagnostics.Contracts;

using Coords = VoxelMash.Grids.ChunkSpaceCoordinates;

namespace VoxelMash.Grids
{
    public abstract class ChunkOctree
    {
        public const ushort C_EmptyMaterial = 0;

        [Pure]
        public abstract void Get(ref Coords ACoords, out ushort AValue);
        public abstract void Set(Coords ACoords, ushort AValue, ref int ABalance);

        public abstract void Clear();
    }
}
