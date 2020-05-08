using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PenishPicker : IGenerationLayer
{
    int sqrPlanetRadius = 64;
    int ballSpacing = 10;
    int shaftSqrRadius = 16;
    public bool isSingleThreaded() { return false; }
    public Chunk generateChunk(Chunk chunk, World world)
    {
        if (chunk.blocks == null)
        {
            chunk.blocks = new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];
        }
        for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                {
                    if (((chunk.worldCoords + new Vector3Int(x + ballSpacing,y,z)).sqrMagnitude < sqrPlanetRadius) || ((chunk.worldCoords + new Vector3Int(x - ballSpacing, y, z)).sqrMagnitude < sqrPlanetRadius))
                    {
                        chunk.blocks[x, y, z] = new Block(BlockType.leaves);
                    }
                    else if (y % 16 == 0 && (new Vector3Int(chunk.worldCoords.x, 0, chunk.worldCoords.z) + new Vector3Int(x, 0, z)).sqrMagnitude < shaftSqrRadius*2)
                    {
                        chunk.blocks[x, y, z] = new Block(BlockType.tnt);
                    }
                    else if (chunk.worldCoords.y >= 0 && (new Vector3Int(chunk.worldCoords.x,0,chunk.worldCoords.z) + new Vector3Int(x,0,z)).sqrMagnitude < shaftSqrRadius)
                    {
                        chunk.blocks[x, y, z] = new Block(BlockType.log);
                    }
                    else
                    {
                        chunk.blocks[x, y, z] = new Block(BlockType.empty);
                    }
                }
            }
        }
        return chunk;
    }
}
