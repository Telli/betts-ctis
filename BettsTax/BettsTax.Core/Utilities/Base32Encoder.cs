using System;
using System.Text;

namespace BettsTax.Core.Utilities
{
    public static class Base32Encoder
    {
        private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        private const int InByteSize = 8;
        private const int OutByteSize = 5;

        public static string ToBase32String(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            var result = new StringBuilder((data.Length + 7) * InByteSize / OutByteSize);

            int buffer = 0;
            int bufferLength = 0;

            foreach (byte b in data)
            {
                buffer = (buffer << InByteSize) | b;
                bufferLength += InByteSize;

                while (bufferLength >= OutByteSize)
                {
                    bufferLength -= OutByteSize;
                    result.Append(Base32Chars[(buffer >> bufferLength) & 0x1F]);
                }
            }

            if (bufferLength > 0)
            {
                buffer <<= (OutByteSize - bufferLength);
                result.Append(Base32Chars[buffer & 0x1F]);
            }

            return result.ToString();
        }

        public static byte[] FromBase32String(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
                return Array.Empty<byte>();

            encoded = encoded.TrimEnd('=').ToUpper();
            
            var bytes = new byte[encoded.Length * OutByteSize / InByteSize];
            int buffer = 0;
            int bufferLength = 0;
            int byteIndex = 0;

            foreach (char c in encoded)
            {
                int value = Base32Chars.IndexOf(c);
                if (value < 0)
                    throw new ArgumentException($"Invalid character '{c}' in Base32 string.");

                buffer = (buffer << OutByteSize) | value;
                bufferLength += OutByteSize;

                if (bufferLength >= InByteSize)
                {
                    bufferLength -= InByteSize;
                    bytes[byteIndex++] = (byte)(buffer >> bufferLength);
                    buffer &= (1 << bufferLength) - 1;
                }
            }

            if (byteIndex != bytes.Length)
            {
                Array.Resize(ref bytes, byteIndex);
            }

            return bytes;
        }
    }
}