using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using Mirror;
public static class ChunkSerializer
{
    private const int CHUNK_MAGIC_NUMBER = 556677123;
    public static string savePath;
    //stores info like the last played date in a file called "worldInfo.dat"
    //1. last played date: long in utc
    //2. world name: string
    public static void updateWorldInfo(WorldInfo info)
    {
        using (BinaryWriter bw = new BinaryWriter(File.Create(savePath + "worldInfo.dat")))
        {
            bw.Write(info.lastPlayed.ToFileTimeUtc());
            bw.Write(info.fileName);
        }
    }
    //saves a chunk to a file in savePath named <chunkCoords>.chunk
    //first there is the magic number, then the chunk coords xyz order.
    //compresses it by writing the block type, then how many blocks of that type there are in a row. xyz order
    //if the chunk has a null block array, it writes 'n' and returns
    //if it has a block array, it writes 'g' before continuing
    public static void writeChunkToFile(Chunk c)
    {
        if (c == null)
            return;
        using (BinaryWriter writer = new BinaryWriter(File.Create(savePath + c.chunkCoords + ".chunk")))
        {
            writer.Write(CHUNK_MAGIC_NUMBER);
            writer.Write(c.chunkCoords.x);
            writer.Write(c.chunkCoords.y);
            writer.Write(c.chunkCoords.z);
            if (c.blocks == null)
            {
                writer.Write('n');
                return;
            }
            writer.Write('g');
            
            BlockType currType = c.blocks[0, 0, 0].type;
            int number = 0; //first iteration will bring this to 1
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        BlockType blockType = c.blocks[x, y, z].type;
                        if (blockType == currType)
                        {
                            number++;
                        }
                        else
                        {
                            writer.Write((int)currType);
                            writer.Write(number);
                            currType = blockType;
                            number = 1;
                        }
                    }
                }
            }
            writer.Write((int)currType);
            writer.Write(number);
        }
    }
    public static void writeChunkToNetwork(Chunk c, NetworkWriter writer)
    {
        if (c == null)
        {
            throw new Exception("null chunk writing to network");
        }

        writer.WriteInt32(CHUNK_MAGIC_NUMBER);
        writer.WriteInt32(c.chunkCoords.x);
        writer.WriteInt32(c.chunkCoords.y);
        writer.WriteInt32(c.chunkCoords.z);
        if (c.blocks == null)
        {
            writer.WriteChar('n');
            return;
        }
        writer.WriteChar('g');

        BlockType currType = c.blocks[0, 0, 0].type;
        int number = 0; //first iteration will bring this to 1
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    BlockType blockType = c.blocks[x, y, z].type;
                    if (blockType == currType)
                    {
                        number++;
                    }
                    else
                    {
                        writer.WriteInt32((int)currType);
                        writer.WriteInt32(number);
                        currType = blockType;
                        number = 1;
                    }
                }
            }
        }
        writer.WriteInt32((int)currType);
        writer.WriteInt32(number);
    }
    //opens the file <coords>.chunk and reads the chunk
    //if the chunk doesn't exist, returns null
    public static Chunk readChunk(Vector3Int coords)
    {
        FileStream fs = null;
        try
        {
            fs = File.OpenRead(savePath + coords + ".chunk");
        }
        catch
        {
            fs?.Dispose();
            return null;
        }
        using (BinaryReader reader = new BinaryReader(fs))
        {
            int magicNum = reader.ReadInt32();
            if (magicNum != CHUNK_MAGIC_NUMBER)
            {
                Debug.LogError("INVALID CHUNK MAGIC NUMBER: " + magicNum + " AT: " + coords);
                return new Chunk(null, coords);
            }
            //chunk coords xyz. don't really care about these since we requested a specific coordinate to begin with
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            if (reader.ReadChar() != 'g') //saved chunk had a null block array
            {
                return new Chunk(null, coords);
            }
            Chunk chunk = new Chunk(new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE], coords);
            BlockType type = (BlockType)reader.ReadInt32();
            int count = reader.ReadInt32();
            int currCount = count;
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        if (currCount == 0)
                        {
                            type = (BlockType)reader.ReadInt32();
                            count = reader.ReadInt32();
                            currCount = count;
                        }
                        currCount--;
                        chunk.blocks[x, y, z] = new Block(type);
                    }
                }
            }
            fs.Dispose();
            return chunk;
        }
    }
    //reads the contents of the NetworkReader into a Chunk object and returns that.
    public static Chunk readChunk(NetworkReader reader)
    {
        int magicNum = reader.ReadInt32();
        if (magicNum != CHUNK_MAGIC_NUMBER)
        {
            Debug.LogError("INVALID CHUNK MAGIC NUMBER: " + magicNum);
            return new Chunk(null, Vector3Int.zero);
        }
        int chunkX = reader.ReadInt32();
        int chunkY = reader.ReadInt32();
        int chunkZ = reader.ReadInt32();
        Vector3Int coords = new Vector3Int(chunkX, chunkY, chunkZ);
        if (reader.ReadChar() != 'g') //saved chunk had a null block array
        {
            return new Chunk(null, coords);
        }
        Chunk chunk = new Chunk(new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE], coords);
        BlockType type = (BlockType)reader.ReadInt32();
        int count = reader.ReadInt32();
        int currCount = count;
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    if (currCount == 0)
                    {
                        type = (BlockType)reader.ReadInt32();
                        count = reader.ReadInt32();
                        currCount = count;
                    }
                    currCount--;
                    chunk.blocks[x, y, z] = new Block(type);
                }
            }
        }
        return chunk;
    }
}
