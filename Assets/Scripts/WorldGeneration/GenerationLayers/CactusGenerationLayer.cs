using System.Collections.Generic;
using UnityEngine;

public class CactusGenerationLayer : IGenerationLayer
{
    public int maxHeight = 13;
    public int minHeight = 7;
    public int armOffset = 4;
    public float cactusDensity = 4;
    public float cactusDensityConstant = 0;
    private FastNoise noise = new FastNoise();
    public bool isSingleThreaded()
    {
        return true;
    }
    public Chunk generateChunk(Chunk chunk, World world)
    {
        if (chunk == null || chunk.blocks == null)
            return chunk;
        if (chunk.worldCoords.y > -33)
        {
            int cacCount = (int)(cactusDensityConstant + cactusDensity * noise.GetSimplex(chunk.chunkCoords.x, chunk.chunkCoords.y, chunk.chunkCoords.z));
            for (int c = 0; c < cacCount; c++)
            {
                int start;
                bool set = false;
                Vector2Int cactusCoords = new Vector2Int((int)(Chunk.CHUNK_SIZE*(0.5f+0.5f*noise.GetSimplex(chunk.worldCoords.x,chunk.worldCoords.y,chunk.worldCoords.z+c))), (int)(Chunk.CHUNK_SIZE * (0.5f+0.5f*noise.GetSimplex(chunk.worldCoords.x+c+123, chunk.worldCoords.y, chunk.worldCoords.z))));
                Debug.Log(cactusCoords);
                for (start = Chunk.CHUNK_SIZE - 1; start > 0; start--)
                {
                    if (chunk.blocks[cactusCoords.x, start, cactusCoords.y].type != BlockType.empty && chunk.blocks[cactusCoords.x, start, cactusCoords.y].type != BlockType.chunk_border)
                    {
                        set = true;
                        break;
                    }
                }
                if (set)
                {
                    Vector3Int startWorldCoords = chunk.worldCoords + new Vector3Int(cactusCoords.x, start + 1, cactusCoords.y);
                    int height = (int)(Mathf.Lerp(minHeight, maxHeight, 0.5f + 0.5f * noise.GetSimplex(startWorldCoords.x, startWorldCoords.y)));
                    for (int i = startWorldCoords.y; i < startWorldCoords.y + height; i++)
                    {
                        world.setBlock(new Vector3Int(startWorldCoords.x, i, startWorldCoords.z), BlockType.cactus);
                        if (startWorldCoords.y + height - i == armOffset)
                        {
                            Debug.Log("arms");
                            bool zArms = 0.5f + 0.5f * noise.GetSimplex(startWorldCoords.x, 1, startWorldCoords.y) > 0.5f; //50% chance for arms to be orientated on x or z axis
                            if (zArms)
                            {
                                world.setBlock(new Vector3Int(startWorldCoords.x, i, startWorldCoords.z + 1), BlockType.cactus);
                                world.setBlock(new Vector3Int(startWorldCoords.x, i, startWorldCoords.z + 2), BlockType.cactus);
                                world.setBlock(new Vector3Int(startWorldCoords.x, i + 1, startWorldCoords.z + 2), BlockType.cactus);
                                world.setBlock(new Vector3Int(startWorldCoords.x, i, startWorldCoords.z - 1), BlockType.cactus);
                                world.setBlock(new Vector3Int(startWorldCoords.x, i, startWorldCoords.z - 2), BlockType.cactus);
                                world.setBlock(new Vector3Int(startWorldCoords.x, i + 1, startWorldCoords.z - 2), BlockType.cactus);
                            }
                            else
                            {
                                world.setBlock(new Vector3Int(startWorldCoords.x + 1, i, startWorldCoords.z), BlockType.cactus);
                                world.setBlock(new Vector3Int(startWorldCoords.x + 2, i, startWorldCoords.z), BlockType.cactus);
                                world.setBlock(new Vector3Int(startWorldCoords.x + 2, i + 1, startWorldCoords.z), BlockType.cactus);
                                world.setBlock(new Vector3Int(startWorldCoords.x - 1, i, startWorldCoords.z), BlockType.cactus);
                                world.setBlock(new Vector3Int(startWorldCoords.x - 2, i, startWorldCoords.z), BlockType.cactus);
                                world.setBlock(new Vector3Int(startWorldCoords.x - 2, i + 1, startWorldCoords.z), BlockType.cactus);
                            }
                        }
                    }
                }
            }
        }

        return chunk;
    }
}