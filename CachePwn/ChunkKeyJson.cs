using System.Collections.Generic;
using Newtonsoft.Json;

namespace CachePwn
{
    public class ChunkKeyJson
    {
        public struct ChunkKey
        {
            public uint Id { get; set; }
            public uint Key1 { get; set; }
            public uint Key2 { get; set; }
            public uint Key3 { get; set; }
        }

        public List<ChunkKey> Chunks = new List<ChunkKey>();

        public static string Save(IEnumerable<Chunk> fileChunks)
        {
            var keys = new ChunkKeyJson();
            foreach (Chunk chunk in fileChunks)
            {
                keys.Chunks.Add(new ChunkKey
                {
                    Id   = chunk.ChunkHeader.Id,
                    Key1 = chunk.ChunkHeader.Key1,
                    Key2 = chunk.ChunkHeader.Key2,
                    Key3 = chunk.ChunkHeader.Key3
                });
            }

            return JsonConvert.SerializeObject(keys);
        }
    }
}
