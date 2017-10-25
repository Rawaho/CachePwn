using System;
using System.Collections.Generic;
using System.IO;

namespace CachePwn
{
    internal static class CachePwn
    {
        private static byte[] payload;
        private static readonly List<Chunk> chunks = new List<Chunk>();

        private static void Main(string[] args)
        {
            Console.Title = "CachePwn - PhatAC Cache Pwnage Tool";
            if (args.Length != 1)
                throw new ArgumentException($"Invalid argument count, expected 1 got {args.Length}!");

            if (!File.Exists(args[0]))
                throw new FileNotFoundException($"{args[0]} is an invalid cache.bin file!");

            payload = File.ReadAllBytes(args[0]);
            Decrypt();
            Read();

            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        private static void Decrypt()
        {
            int old  = payload.Length;
            int size = (int)(Math.Ceiling((double)payload.Length / sizeof(int)) * sizeof(int));
            Array.Resize(ref payload, size);

            uint key = (uint)(0x8001 * old);
            for (int i = 0; i < payload.Length; i += sizeof(int))
            {
                uint value = BitConverter.ToUInt32(payload, i);
                value ^= key;
                key = (uint)(old + 8 * key);

                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, payload, i, sizeof(int));
            }
        }

        private static void Read()
        {
            using (MemoryStream stream = new MemoryStream(payload))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    reader.ReadUInt32(); // version?

                    uint chunkCount = reader.ReadUInt32();
                    for (uint i = 0u; i < chunkCount; i++)
                        chunks.Add(new Chunk(reader));
                }
            }
        }
    }
}
