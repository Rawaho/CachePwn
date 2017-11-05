using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace CachePwn
{
    public class Chunk
    {
        // server hard coded round 2 decryption keys
        public Dictionary<uint, (uint Key1, uint Key2)> Round2DecryptionKeys
            = new Dictionary<uint, (uint Key1, uint Key2)>
        {
            { 1u,  (0xE8B00434, 0x82092270) },
            { 2u,  (0x5D97BAEC, 0x41675123) },
            { 3u,  (0x7DC126EB, 0x5F41B9AD) },
            { 4u,  (0x5F41B9AD, 0x7DC126EB) },
            { 5u,  (0x887AEF9C, 0xA92EC9AC) },
            { 6u,  (0xCD57FD07, 0x697A2224) },
            { 7u,  (0x591C34E9, 0x2250B020) },
            { 8u,  (0xE80D81CA, 0x8ECA9786) },
            { 9u,  (0xD8FD6B02, 0xA0427974) },
            { 10u, (0x5F1FA913, 0xE345C74C) }  // server references this chunk but doesn't exist in cache.bin
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            public uint Index { get; }
            public uint Id { get; }
            public uint Key1 { get; }
            public uint Key2 { get; }
            public uint Key3 { get; }
            public byte HasData { get; }

            public Header(BinaryReader reader)
            {
                Index   = reader.ReadUInt32();
                Id      = reader.ReadUInt32();
                Key1    = reader.ReadUInt32();
                Key2    = reader.ReadUInt32();
                Key3    = reader.ReadUInt32();
                HasData = reader.ReadByte();
            }

            public Header(ChunkKeyJson.ChunkKey chunkKey)
            {
                Index   = chunkKey.Id;
                Id      = chunkKey.Id;
                Key1    = chunkKey.Key1;
                Key2    = chunkKey.Key2;
                Key3    = chunkKey.Key3;
                HasData = 1;
            }
        }

        public Header ChunkHeader { get; }
        public byte[] Payload { get; private set; }

        public Chunk(BinaryReader reader)
        {
            ChunkHeader = new Header(reader);
            Console.WriteLine($"New Chunk - Id:{ChunkHeader.Id}");

            if (ChunkHeader.HasData != 1)
                return;

            int compressedLength = reader.ReadInt32();
            Payload = reader.ReadBytes(compressedLength);

            // process chunk...
            uint decompressedPayloadSize = reader.ReadUInt32();
            Decompress(decompressedPayloadSize);

            Round1(true);
            Round2(true);

            decompressedPayloadSize = BitConverter.ToUInt32(Payload, 0);
            Payload = Payload.Skip(sizeof(int)).ToArray();
            Decompress(decompressedPayloadSize);

            Console.WriteLine($"Saving chunk as {ChunkHeader.Id:X4}.raw...");
            File.WriteAllBytes($"{ChunkHeader.Id:X4}.raw", Payload);
        }

        public Chunk(ChunkKeyJson.ChunkKey chunkKey, byte[] data)
        {
            ChunkHeader = new Header(chunkKey);
            Console.WriteLine($"New Chunk - Id:{ChunkHeader.Id}");

            Payload = data;

            uint decompressedLength = (uint)Payload.Length;
            Compress();

            Payload = BitConverter.GetBytes(decompressedLength)
                .Concat(Payload)
                .ToArray();

            Round1(false);
            Round2(false);

            decompressedLength = (uint)Payload.Length;
            Compress();

            Payload = ChunkHeader.Serialise()
                .Concat(BitConverter.GetBytes(Payload.Length))
                .Concat(Payload)
                .Concat(BitConverter.GetBytes(decompressedLength))
                .ToArray();
        }

        /// <summary>
        /// Encrypt or decrypt payload with first round keys in chunk header.
        /// </summary>
        private void Round1(bool decrypt)
        {
            Console.WriteLine($"{(decrypt ? "Decryption" : "Encryption")} Round 1 - Id:{ChunkHeader.Id}, Key:0x{ChunkHeader.Key1:X4},0x{ChunkHeader.Key2:X4},0x{ChunkHeader.Key3:X4}");

            uint v7 = ChunkHeader.Key1;
            for (int i = 0; i < Payload.Length >> 2; i++)
            {
                v7 = ChunkHeader.Key3 + (ChunkHeader.Key2 ^ v7);

                uint value = BitConverter.ToUInt32(Payload, i * sizeof(int));

                if (decrypt)
                    value += v7 * (uint)(i + 1);
                else
                    value -= v7 * (uint)(i + 1);

                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, Payload, i * sizeof(int), sizeof(int));
            }
        }

        /// <summary>
        /// Encrypt or decrypt payload with second round keys hard coded in server.
        /// </summary>
        private void Round2(bool decrypt)
        {
            (uint Key1, uint Key2) round2DecryptionKeys = Round2DecryptionKeys[ChunkHeader.Id];
            uint key1 = round2DecryptionKeys.Key1;
            uint key2 = round2DecryptionKeys.Key2;

            Console.WriteLine($"{(decrypt ? "Decryption" : "Encryption")} Round 2 - Id:{ChunkHeader.Id}, Key:0x{key1:X4},0x{key2:X4}");

            for (int i = 0; i < Payload.Length >> 2; i++)
            {
                uint v13 = key1 * (uint)(i + 1);
                key1 = key1 ^ key2;

                uint value = BitConverter.ToUInt32(Payload, i * sizeof(int));

                if (decrypt)
                    value += v13;
                else
                    value -= v13;

                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, Payload, i * sizeof(int), sizeof(int));
            }
        }

        private void Decompress(uint decompressedPayloadSize)
        {
            Console.WriteLine("Decompressing payload...");

            byte[] decompressedPayload = ZlibProvider.Decompress(Payload);
            if (decompressedPayloadSize != decompressedPayload.Length)
                throw new InvalidDataException("Decompressed payload size doesn't match payload!");

            Payload = decompressedPayload;
        }

        private void Compress()
        {
            Console.WriteLine("Compressing payload...");
            Payload = ZlibProvider.Compress(Payload);
        }
    }
}
