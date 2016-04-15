using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VoxelMash.Grids
{
    public static class GridChunkTerminal
    {
        private static readonly Regex _FCanonicRegex = new Regex(@"^\(\((?<Level>[0-8]), (?<X>[0-9]+)\|(?<Y>[0-9]+)\|(?<Z>[0-9]+)\), (?<Material>[0-9]+)\)$");

        #region Serialization functions
        public static byte[] ToBytes(
            ChunkSpaceCoords ACoords,
            ushort AMaterial)
        {
            byte[] aMaterial = BitConverter.GetBytes(AMaterial);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(aMaterial);

            return ChunkSpaceCoords.ToBytes(ACoords)
                .Concat(aMaterial)
                .ToArray();
        }
        public static string ToCanonic(
            ChunkSpaceCoords ACoords,
            ushort AMaterial)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0}, {1})", ACoords.ToString("C"), AMaterial);
        }

        public static void FromBytes(
            byte[] ABytes,
            out ChunkSpaceCoords ACoords,
            out ushort AMaterial,
            int AOffset = 0)
        {
            if (ABytes == null)
                throw new ArgumentNullException("ABytes");

            ACoords = ChunkSpaceCoords.FromBytes(ABytes, AOffset);
            int iLength = ACoords.ByteSize;

            if (!BitConverter.IsLittleEndian)
            {
                byte[] aMaterial = new byte[] { ABytes[AOffset + iLength + 1], ABytes[AOffset + iLength] };
                AMaterial = BitConverter.ToUInt16(aMaterial, 0);
                return;
            }

            AMaterial = BitConverter.ToUInt16(ABytes, AOffset + iLength);
        }
        public static void FromCanonic(
            string ACanonic,
            out ChunkSpaceCoords ACoords,
            out ushort AMaterial)
        {
            if (ACanonic == null)
                throw new ArgumentNullException("ACanonic");

            Match mMatch = GridChunkTerminal._FCanonicRegex.Match(ACanonic);
            if (!mMatch.Success)
                throw new FormatException("Input string is not canonic.");

            try
            {
                ACoords = new ChunkSpaceCoords(
                    (ChunkSpaceLevel)Byte.Parse(mMatch.Groups["Level"].Value),
                    Byte.Parse(mMatch.Groups["X"].Value),
                    Byte.Parse(mMatch.Groups["Y"].Value),
                    Byte.Parse(mMatch.Groups["Z"].Value));

                AMaterial = UInt16.Parse(mMatch.Groups["Material"].Value);
            }
            catch
            {
                throw new FormatException("Invalid canonic terminal string.");
            }
        }
        #endregion
    }
}
