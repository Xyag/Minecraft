using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;

public class ChunkSerializer
{
    public string savePath;
    public ChunkSerializer(string savePath)
    {
        this.savePath = savePath;
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
    }
    //saves a chunk to a file in savePath named <chunkCoords>.chunk
    //compresses it by writing the block type, then how many blocks of that type there are in a row. xyz order
    //if the chunk has a null block array, it writes 'n' and returns
    //if it has a block array, it writes 'g' before continuing
    public void writeChunk(Chunk c)
    {
        if (c == null)
            return;
        using (BinaryWriter bw = new BinaryWriter(File.Create(savePath + c.chunkCoords + ".chunk")))
        {
            if (c.blocks == null)
            {
                bw.Write('n');
                return;
            }
            bw.Write('g');
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
                            bw.Write((int)currType);
                            bw.Write(number);
                            currType = blockType;
                            number = 1;
                        }
                    }
                }
            }
            bw.Write((int)currType);
            bw.Write(number);
        }
    }
    //opens the file <coords>.chunk and reads the chunk
    //if the chunk doesn't exist, returns null
    public Chunk readChunk(Vector3Int coords)
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
        using (BinaryReader br = new BinaryReader(fs))
        {
            if (br.ReadChar() != 'g') //saved chunk had a null block array
            {
                return new Chunk(null, coords);
            }
            Chunk chunk = new Chunk(new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE], coords);
            BlockType type = (BlockType)br.ReadInt32();
            int count = br.ReadInt32();
            int currCount = count;
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        if (currCount == 0)
                        {
                            type = (BlockType)br.ReadInt32();
                            count = br.ReadInt32();
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
}
