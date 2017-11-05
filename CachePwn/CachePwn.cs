using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace CachePwn
{
    internal static class CachePwn
    {
        private static byte[] payload;
        private static readonly List<Chunk> chunks = new List<Chunk>();

        private static void Main(string[] args)
        {
            Console.Title = "CachePwn - PhatAC Cache Pwnage Tool";
            if (args.Length != 2)
                throw new ArgumentException($"Invalid argument count, expected 2 got {args.Length}!");

            if (!uint.TryParse(args[0], out uint mode))
                throw new ArgumentException();

            var sw = new Stopwatch();
            sw.Start();

            switch (mode)
            {
                case 1:
                    UnPack(args[1]);
                    break;
                case 2:
                    Pack(args[1]);
                    break;
                default:
                    throw new NotImplementedException();
            }

            Console.WriteLine($"Finished processing in {sw.ElapsedMilliseconds}ms!");
            Console.ReadLine();
        }

        /// <summary>
        /// Unpack and decrypt the contents of cache.bin.
        /// </summary>
        private static void UnPack(string cachePath)
        {
            Console.WriteLine("Unpacking...");

            if (!File.Exists(cachePath))
                throw new FileNotFoundException($"{cachePath} is an invalid cache.bin file!");

            payload = File.ReadAllBytes(cachePath);
            Decrypt();

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

            Console.WriteLine("Saving chunk keys as keys.json...");
            File.WriteAllText("keys.json", ChunkKeyJson.Save(chunks));
        }

        /// <summary>
        /// Pack and encrypt the contents of modified chunk files into a new cache.bin.
        /// </summary>
        private static void Pack(string assetPath)
        {
            Console.WriteLine("Packing...");

            if (!Directory.Exists(assetPath))
                throw new DirectoryNotFoundException($"{assetPath} is an invalid directory!");

            string keysPath = Path.Combine(assetPath, "keys.json");
            if (!File.Exists(keysPath))
                throw new FileNotFoundException($"{assetPath} doesn't contain the keys.json file!");

            ChunkKeyJson chunkKeys = JsonConvert.DeserializeObject<ChunkKeyJson>(File.ReadAllText(keysPath));

            string[] chunkFilePaths = Directory.GetFiles(assetPath, "*.raw");
            if (chunkFilePaths.Length < 9)
                throw new InvalidDataException($"Invalid amount of chunks files, expected 9 got {chunkFilePaths.Length}!");

            foreach (string chunkPath in chunkFilePaths)
            {
                uint id = uint.Parse(Path.GetFileNameWithoutExtension(chunkPath));
                ChunkKeyJson.ChunkKey chunkKey = chunkKeys.Chunks.SingleOrDefault(c => c.Id == id);

                chunks.Add(new Chunk(chunkKey, File.ReadAllBytes(chunkPath)));
            }

            payload = BitConverter.GetBytes(1u)
                .Concat(BitConverter.GetBytes(chunkFilePaths.Length))
                .ToArray();

            chunks.ForEach(c => payload = payload.Concat(c.Payload).ToArray());

            Decrypt();


            File.WriteAllBytes(Path.Combine(assetPath, "cache_test.bin"), payload);
        }

        private static void Decrypt()
        {
            uint key = (uint)(0x8001 * payload.Length);
            for (int i = 0; i < payload.Length >> 2; i++)
            {
                uint value = BitConverter.ToUInt32(payload, i * sizeof(int));
                value ^= key;
                key = (uint)(payload.Length + 8 * key);

                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, payload, i * sizeof(int), sizeof(int));
            }
        }
    }
}
