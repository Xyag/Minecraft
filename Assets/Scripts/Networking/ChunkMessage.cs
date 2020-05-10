using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mirror;

public class ChunkMessage : IMessageBase
{
    public Chunk chunk;
    public void Deserialize(NetworkReader reader)
    {
        chunk = ChunkSerializer.readChunk(reader);
    }
    public void Serialize(NetworkWriter writer)
    {
        ChunkSerializer.writeChunkToNetwork(chunk, writer);
    }
}
