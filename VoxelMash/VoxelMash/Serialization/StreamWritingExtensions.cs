using System;
using System.IO;
using System.Linq;
using System.Text;

namespace VoxelMash.Serialization
{
    public static class StreamWritingExtensions
    {
        public static void WriteBytes(
            this Stream AByteStream,
            byte[] ABytes)
        {
            if (AByteStream == null)
                throw new ArgumentNullException("AByteStream");

            if (ABytes == null)
                return;

            for (uint I = 0; I < ABytes.LongLength; I++)
                AByteStream.WriteByte(ABytes[I]);
        }

        public static void WriteUInt16(
            this Stream AByteStream,
            ushort AUInt16,
            bool ALittleEndian = true)
        {
            if (AByteStream == null)
                throw new ArgumentNullException("AByteStream");

            byte[] aWrite = BitConverter.GetBytes(AUInt16);

            if (BitConverter.IsLittleEndian != ALittleEndian)
                Array.Reverse(aWrite);

            AByteStream.WriteBytes(aWrite);
        }
        public static void WriteUInt32(
            this Stream AByteStream,
            uint AUInt32,
            bool ALittleEndian = true)
        {
            if (AByteStream == null)
                throw new ArgumentNullException("AByteStream");

            byte[] aWrite = BitConverter.GetBytes(AUInt32);

            if (BitConverter.IsLittleEndian != ALittleEndian)
                Array.Reverse(aWrite);

            AByteStream.WriteBytes(aWrite);
        }
        public static void WriteUInt64(
            this Stream AByteStream,
            ulong AUInt64,
            bool ALittleEndian = true)
        {
            if (AByteStream == null)
                throw new ArgumentNullException("AByteStream");

            byte[] aWrite = BitConverter.GetBytes(AUInt64);

            if (BitConverter.IsLittleEndian != ALittleEndian)
                Array.Reverse(aWrite);

            AByteStream.WriteBytes(aWrite);
        }

        public static void WriteInt16(
            this Stream AByteStream,
            short AInt16,
            bool ALittleEndian = true)
        {
            if (AByteStream == null)
                throw new ArgumentNullException("AByteStream");

            byte[] aWrite = BitConverter.GetBytes(AInt16);

            if (BitConverter.IsLittleEndian != ALittleEndian)
                Array.Reverse(aWrite);

            AByteStream.WriteBytes(aWrite);
        }
        public static void WriteInt32(
            this Stream AByteStream,
            int AInt32,
            bool ALittleEndian = true)
        {
            if (AByteStream == null)
                throw new ArgumentNullException("AByteStream");

            byte[] aWrite = BitConverter.GetBytes(AInt32);

            if (BitConverter.IsLittleEndian != ALittleEndian)
                Array.Reverse(aWrite);

            AByteStream.WriteBytes(aWrite);
        }
        public static void WriteInt64(
            this Stream AByteStream,
            long AInt64,
            bool ALittleEndian = true)
        {
            if (AByteStream == null)
                throw new ArgumentNullException("AByteStream");

            byte[] aWrite = BitConverter.GetBytes(AInt64);

            if (BitConverter.IsLittleEndian != ALittleEndian)
                Array.Reverse(aWrite);

            AByteStream.WriteBytes(aWrite);
        }

        public static void WriteFloat(
            this Stream AByteStream,
            float AFloat,
            bool ALittleEndian = true)
        {
            if (AByteStream == null)
                throw new ArgumentNullException("AByteStream");

            byte[] aWrite = BitConverter.GetBytes(AFloat);

            if (BitConverter.IsLittleEndian != ALittleEndian)
                Array.Reverse(aWrite);

            AByteStream.WriteBytes(aWrite);
        }
        public static void WriteDouble(
            this Stream AByteStream,
            double ADouble,
            bool ALittleEndian = true)
        {
            if (AByteStream == null)
                throw new ArgumentNullException("AByteStream");

            byte[] aWrite = BitConverter.GetBytes(ADouble);

            if (BitConverter.IsLittleEndian != ALittleEndian)
                Array.Reverse(aWrite);

            AByteStream.WriteBytes(aWrite);
        }

        public static void WriteUtf8Zt(
            this Stream AByteStream,
            string AString)
        {
            if (AByteStream == null)
                throw new ArgumentNullException("AByteStream");
            if (AString == null)
                throw new ArgumentNullException("AString");

            if (AString.Contains(Convert.ToChar(0x00)))
                throw new ArgumentException("Input string contains the 0x00 character.");

            byte[] aWrite = Encoding.UTF8.GetBytes(AString);
            AByteStream.WriteBytes(aWrite);
            AByteStream.WriteByte(0x00);
        }
    }
}