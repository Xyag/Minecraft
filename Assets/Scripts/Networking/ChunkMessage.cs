using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;
public class ChunkMessage : IMessageBase
{
    public Chunk chunk;
    public Vector3Int chunkPos;
    public bool willFulfill;

    public ChunkMessage() { }
    public ChunkMessage(Chunk chunk, Vector3Int coords, bool willFulfill)
    {
        this.chunk = chunk;
        chunkPos = coords;
        this.willFulfill = willFulfill;
    }
    public void Deserialize(NetworkReader reader)
    {
        willFulfill = reader.ReadBoolean();
        chunkPos.x = reader.ReadInt32();
        chunkPos.y = reader.ReadInt32();
        chunkPos.z = reader.ReadInt32();
        if (reader.ReadBoolean())
        {
            chunk = ChunkSerializer.readChunk(reader);
        }
        else
        {
            chunk = null;
        }
    }
    public void Serialize(NetworkWriter writer)
    {
        writer.WriteBoolean(willFulfill);
        writer.WriteInt32(chunkPos.x);
        writer.WriteInt32(chunkPos.y);
        writer.WriteInt32(chunkPos.z);
        writer.WriteBoolean(chunk != null);
        if (chunk != null)
        {
            ChunkSerializer.writeChunkToNetwork(chunk, writer);
        }
    }
}
