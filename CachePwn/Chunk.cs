using System;
using System.Collections.Generic;
using System.IO;

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

        public struct Header
        {
            public uint Id { get; }
            public uint Id2 { get; } // ??
            public uint Key1 { get; }
            public uint Key2 { get; }
            public uint Key3 { get; }
            public bool HasData { get; }

            public Header(BinaryReader reader)
            {
                Id      = reader.ReadUInt32();
                Id2     = reader.ReadUInt32();
                Key1    = reader.ReadUInt32();
                Key2    = reader.ReadUInt32();
                Key3    = reader.ReadUInt32();
                HasData = reader.ReadByte() == 1;
            }
        }

        private readonly Header chunkHeader;
        private byte[] payload;

        public Chunk(BinaryReader reader)
        {
            chunkHeader = new Header(reader);
            Console.WriteLine($"New Chunk - Id:{chunkHeader.Id}");

            if (chunkHeader.HasData)
            {
                int compressedLength = reader.ReadInt32();
                payload = reader.ReadBytes(compressedLength);
            }

            // process chunk...
            uint decompressedPayloadSize = reader.ReadUInt32();
            DeflateRound1(decompressedPayloadSize);
            DecryptRound1();
            DecryptRound2();
            DeflateRound2();

            Console.WriteLine($"Saving chunk as {chunkHeader.Id:X4}.raw...");
            File.WriteAllBytes($"{chunkHeader.Id:X4}.raw", payload);
        }

        private int Resize()
        {
            int old  = payload.Length;
            int size = (int)(Math.Ceiling((double)payload.Length / sizeof(int)) * sizeof(int));
            Array.Resize(ref payload, size);

            for (int i = old; i < payload.Length; i++)
                payload[i] = 0xAB;

            return old;
        }

        /// <summary>
        /// Decrypt payload with first round keys in chunk header.
        /// </summary>
        private void DecryptRound1()
        {
            int old = Resize();
            Console.WriteLine($"Decryption Round 1 - Id:{chunkHeader.Id}, Key:0x{chunkHeader.Key1:X4},0x{chunkHeader.Key2:X4},0x{chunkHeader.Key3:X4}");

            uint v7 = chunkHeader.Key1;
            for (int i = 0; i < payload.Length; i += sizeof(int))
            {
                v7 = chunkHeader.Key3 + (chunkHeader.Key2 ^ v7);

                uint value = BitConverter.ToUInt32(payload, i);
                value += v7 * (uint)(i / sizeof(int) + 1);

                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, payload, i, sizeof(int));
            }

            Array.Resize(ref payload, old);
        }

        /// <summary>
        /// Decrypt payload with second round keys hard coded in server.
        /// </summary>
        private void DecryptRound2()
        {
            int old = Resize();

            (uint Key1, uint Key2) round2DecryptionKeys = Round2DecryptionKeys[chunkHeader.Id];
            uint key1 = round2DecryptionKeys.Key1;
            uint key2 = round2DecryptionKeys.Key2;

            Console.WriteLine($"Decryption Round 2 - Id:{chunkHeader.Id}, Key:0x{key1:X4},0x{key2:X4}");

            for (int i = 0; i < payload.Length; i += sizeof(int))
            {
                uint v13 = key1 * (uint)(i / sizeof(int) + 1);
                key1 = key1 ^ key2;

                uint value = BitConverter.ToUInt32(payload, i);
                value += v13;

                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, payload, i, sizeof(int));
            }

            Array.Resize(ref payload, old);
        }

        /// <summary>
        /// Deflate payload before the two rounds of encryption
        /// </summary>
        private void DeflateRound1(uint decompressedPayloadSize)
        {
            Console.WriteLine("Deflate Round 1...");

            ZlibProvider.Deflate(payload, out byte[] decompressedPayload);

            // chunk 9 doesn't match, probably a fuckup somewhere
            if (decompressedPayloadSize != decompressedPayload.Length && chunkHeader.Id != 9)
                throw new InvalidDataException("Decompressed payload size doesn't match payload!");

            payload = decompressedPayload;
        }

        /// <summary>
        /// Deflate payload after the two rounds of encryption.
        /// </summary>
        private void DeflateRound2()
        {
            Console.WriteLine("Deflate Round 2...");

            uint decompressedPayloadSize = BitConverter.ToUInt32(payload, 0);

            // skip size
            byte[] moobar = new byte[payload.Length - sizeof(int)];
            Buffer.BlockCopy(payload, sizeof(int), moobar, 0, payload.Length - sizeof(int));

            ZlibProvider.Deflate(moobar, out byte[] decompressedPayload);
            if (decompressedPayloadSize != decompressedPayload.Length)
                throw new InvalidDataException("Decompressed payload size doesn't match payload!");

            payload = decompressedPayload;
        }
    }
}
