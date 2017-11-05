using System.IO;
using Ionic.Zlib;

namespace CachePwn
{
    public static class ZlibProvider
    {
        public static byte[] Decompress(byte[] data)
        {
            using (MemoryStream compressedStream = new MemoryStream(data))
            {
                using (var zlibStream = new ZlibStream(compressedStream, CompressionMode.Decompress))
                {
                    using (MemoryStream decompressedStream = new MemoryStream())
                    {
                        zlibStream.CopyTo(decompressedStream);
                        return decompressedStream.ToArray();
                    }
                }
            }
        }

        public static byte[] Compress(byte[] data)
        {
            using (MemoryStream decompressedStream = new MemoryStream(data))
            {
                using (var zlibStream = new ZlibStream(decompressedStream, CompressionMode.Compress, CompressionLevel.BestCompression))
                {
                    using (MemoryStream compressedStream = new MemoryStream())
                    {
                        zlibStream.CopyTo(compressedStream);
                        return compressedStream.ToArray();
                    }
                }
            }
        }
    }
}
