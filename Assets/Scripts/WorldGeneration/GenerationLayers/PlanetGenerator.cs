using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PlanetGenerator : IGenerationLayer
{
    int sqrPlanetRadius = 16384;
    Vector3Int planetCenter;
    public bool isSingleThreaded() { return false; }
    public Chunk generateChunk(Chunk chunk, World world)
    {
        if ((chunk.worldCoords-planetCenter).sqrMagnitude > sqrPlanetRadius+2*Chunk.CHUNK_SIZE)
        {
            return chunk;
        }
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
                    if (new Vector3Int(chunk.worldCoords.x + x - planetCenter.x, chunk.worldCoords.y + y - planetCenter.y, chunk.worldCoords.z + z - planetCenter.z).sqrMagnitude <= sqrPlanetRadius)
                        chunk.blocks[x, y, z] = new Block(BlockType.stone);
                    else
                        chunk.blocks[x, y, z] = new Block(BlockType.empty);
                }
            }
        }
        return chunk;
    }
}
