using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System;

public class World
{
    public ChunkBuffer unloadChunkBuffer = new ChunkBuffer(1000);
    public Dictionary<Vector3Int, Chunk> loadedChunks = new Dictionary<Vector3Int, Chunk>();
    public Dictionary<EntityType, GameObject> entityTypes = new Dictionary<EntityType, GameObject>();
    public List<Entity> loadedEntities = new List<Entity>();
    public GameObject explosionParticles;

    public GameObject spawnEntity(EntityType type, Vector3 position)
    {
        var go = MonoBehaviour.Instantiate(entityTypes[type]);
        go.transform.position = position;
        Entity e = go.GetComponent<Entity>();
        e.world = this;
        return go;
    }
    public async void createExplosion(float explosionStrength, Vector3Int origin)
    {
        int size = Mathf.CeilToInt(explosionStrength);
        int csize = Mathf.CeilToInt((float)size / (float)Chunk.CHUNK_SIZE);
        for (int x = -size; x <= size; x++)
        {
            for (int y = -size; y <= size; y++)
            {
                for (int z = -size; z <= size; z++)
                {
                    Vector3Int blockPos = new Vector3Int(x, y, z);
                    if (blockPos.sqrMagnitude < explosionStrength * explosionStrength)
                    {
                        BlockData currBlock = getBlock(blockPos + origin);
                        if (currBlock != null && currBlock.type == BlockType.tnt)
                        {
                            currBlock.interact(blockPos + origin, WorldToChunkCoords(blockPos + origin), loadedChunks[WorldToChunkCoords(blockPos + origin)], this);
                        }
                        setBlock(blockPos + origin, BlockType.empty);
                    }
                }
            }
        }
        Vector3Int chunkPos = WorldToChunkCoords(origin);
        List<Chunk> remeshQueue = new List<Chunk>(csize * csize * csize * 5);
        for (int x = -csize; x <= csize; x++)
        {
            for (int y = -csize; y <= csize; y++)
            {
                for (int z = -csize; z <= csize; z++)
                {
                    if (x * x + y * y + z * z <= (csize + 1) * (csize + 1))
                        remeshQueue.Add(getChunk(chunkPos + new Vector3Int(x, y, z)));
                }
            }
        }
        Task t = MeshGenerator.remeshAll(remeshQueue, this, remeshQueue.Count);
        foreach (var en in loadedEntities)
        {
            if (Vector3.SqrMagnitude(en.transform.position - (Vector3)origin) < explosionStrength * explosionStrength)
            {
                en.rigidbody.AddForce((en.transform.position - (Vector3)origin).normalized * explosionStrength, ForceMode.VelocityChange);
            }
        }
        var explo = MonoBehaviour.Instantiate(explosionParticles);
        explo.transform.position = origin;
        MonoBehaviour.Destroy(explo, 5);
        await t;
    }
    public void unloadFromQueue(int max)
    {
        lock (unloadChunkBuffer)
        {
            int iterations = System.Math.Min(max, unloadChunkBuffer.Count());
            for (int i = 0; i < iterations; i++)
            {
                Chunk data = unloadChunkBuffer.Dequeue();
                unloadChunk(data);
            }
        }
    }
    public void loadChunk(Chunk c)
    {
        lock (loadedChunks)
        {
            Chunk temp;
            if (!loadedChunks.TryGetValue(c.chunkCoords, out temp))
                loadedChunks.Add(c.chunkCoords, c);
            else
                for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                {
                    for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                    {
                        for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                        {
                            if (c.blocks[x, y, z].type != BlockType.empty)
                            {
                                temp.blocks[x, y, z] = c.blocks[x, y, z];
                            }
                        }
                    }
                }
        }
    }
    public void unloadChunk(Chunk chunk)
    {
        lock (loadedChunks)
        {
            if (chunk.gameObject != null)
                MonoBehaviour.Destroy(chunk.gameObject);
            loadedChunks.Remove(chunk.chunkCoords);
        }
    }
    public void unloadChunk(Vector3Int coords)
    {
        lock (loadedChunks)
        {
            Chunk chunk = loadedChunks[coords];
            if (chunk.gameObject != null)
                MonoBehaviour.Destroy(chunk.gameObject);
            loadedChunks.Remove(coords);
        }
    }
    public Chunk getChunk(Vector3Int coords)
    {
        Chunk chunk;
        if (loadedChunks.TryGetValue(coords, out chunk))
        {
            return chunk;
        }
        else
        {
            chunk = new Chunk(new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE], coords);
            return chunk;
        }
    }
    public Vector3Int WorldToChunkCoords(Vector3 worldCoords)
    {
        return WorldToChunkCoords(new Vector3Int((int)worldCoords.x, (int)worldCoords.y, (int)worldCoords.z));
    }
    public Vector3Int WorldToChunkCoords(Vector3Int worldCoords)
    {
        Vector3Int chunkCoords = new Vector3Int(worldCoords.x / Chunk.CHUNK_SIZE, worldCoords.y / Chunk.CHUNK_SIZE, worldCoords.z / Chunk.CHUNK_SIZE);
        Vector3Int blockCoords = new Vector3Int(worldCoords.x % Chunk.CHUNK_SIZE, worldCoords.y % Chunk.CHUNK_SIZE, worldCoords.z % Chunk.CHUNK_SIZE);
        if (blockCoords.x < 0)
        {
            chunkCoords.x -= 1;
        }
        if (blockCoords.y < 0)
        {
            chunkCoords.y -= 1;
        }
        if (blockCoords.z < 0)
        {
            chunkCoords.z -= 1;
        }
        return chunkCoords;
    }
    public List<Chunk> getNeighboringChunks(Vector3Int coords)
    {
        List<Chunk> neighbors = new List<Chunk>(6);
        Chunk temp;
        if (loadedChunks.TryGetValue(coords + new Vector3Int(1, 0, 0), out temp))
        {
            neighbors.Add(temp);
        }
        if (loadedChunks.TryGetValue(coords + new Vector3Int(-1, 0, 0), out temp))
        {
            neighbors.Add(temp);
        }
        if (loadedChunks.TryGetValue(coords + new Vector3Int(0, 1, 0), out temp))
        {
            neighbors.Add(temp);
        }
        if (loadedChunks.TryGetValue(coords + new Vector3Int(0, -1, 0), out temp))
        {
            neighbors.Add(temp);
        }
        if (loadedChunks.TryGetValue(coords + new Vector3Int(0, 0, 1), out temp))
        {
            neighbors.Add(temp);
        }
        if (loadedChunks.TryGetValue(coords + new Vector3Int(0, 0, -1), out temp))
        {
            neighbors.Add(temp);
        }
        return neighbors;
    }
    //not thread safe: loads the chunk if it doesn't exist.
    public void setBlock(Vector3Int worldCoords, BlockType block, bool forceLoadChunk = false)
    {
        Vector3Int chunkCoords = new Vector3Int(worldCoords.x / Chunk.CHUNK_SIZE, worldCoords.y / Chunk.CHUNK_SIZE, worldCoords.z / Chunk.CHUNK_SIZE);
        Vector3Int blockCoords = new Vector3Int(worldCoords.x % Chunk.CHUNK_SIZE, worldCoords.y % Chunk.CHUNK_SIZE, worldCoords.z % Chunk.CHUNK_SIZE);
        if (blockCoords.x < 0)
        {
            chunkCoords.x -= 1;
            blockCoords.x += Chunk.CHUNK_SIZE;
        }
        if (blockCoords.y < 0)
        {
            chunkCoords.y -= 1;
            blockCoords.y += Chunk.CHUNK_SIZE;
        }
        if (blockCoords.z < 0)
        {
            chunkCoords.z -= 1;
            blockCoords.z += Chunk.CHUNK_SIZE;
        }
        Chunk chunk;
        if (loadedChunks.TryGetValue(chunkCoords, out chunk))
        {
            //Debug.Log("world coords: " + worldCoords + ", chunk: " + chunkCoords + ", block: " + blockCoords);
            chunk.blocks[blockCoords.x, blockCoords.y, blockCoords.z].type = block;
        }
        else if (forceLoadChunk)
        {
            chunk = new Chunk(new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE], chunkCoords);
            loadChunk(chunk);
            //Debug.Log("FL world coords: " + worldCoords + ", chunk: " + chunkCoords + ", block: " + blockCoords);
            chunk.blocks[blockCoords.x, blockCoords.y, blockCoords.z].type = block;
        }
    }
    public void setBlockAndMesh(Vector3Int worldCoords, BlockType block)
    {
        Vector3Int chunkCoords = new Vector3Int(worldCoords.x / Chunk.CHUNK_SIZE, worldCoords.y / Chunk.CHUNK_SIZE, worldCoords.z / Chunk.CHUNK_SIZE);
        Vector3Int blockCoords = new Vector3Int(worldCoords.x % Chunk.CHUNK_SIZE, worldCoords.y % Chunk.CHUNK_SIZE, worldCoords.z % Chunk.CHUNK_SIZE);
        if (blockCoords.x < 0)
        {
            chunkCoords.x -= 1;
            blockCoords.x += Chunk.CHUNK_SIZE;
        }
        if (blockCoords.y < 0)
        {
            chunkCoords.y -= 1;
            blockCoords.y += Chunk.CHUNK_SIZE;
        }
        if (blockCoords.z < 0)
        {
            chunkCoords.z -= 1;
            blockCoords.z += Chunk.CHUNK_SIZE;
        }
        Chunk chunk;
        if (loadedChunks.TryGetValue(chunkCoords, out chunk))
        {
            //Debug.Log("world coords: " + worldCoords + ", chunk: " + chunkCoords + ", block: " + blockCoords);
            chunk.blocks[blockCoords.x, blockCoords.y, blockCoords.z].type = block;
        }
        MeshGenerator.remeshChunk(this, chunk, false);

        //check surrounding chunks
        if (blockCoords.x == Chunk.CHUNK_SIZE - 1 && loadedChunks.TryGetValue(chunkCoords + new Vector3Int(1, 0, 0), out chunk))
        {
            MeshGenerator.remeshChunk(this, chunk);
        }
        if (blockCoords.y == Chunk.CHUNK_SIZE - 1 && loadedChunks.TryGetValue(chunkCoords + new Vector3Int(0, 1, 0), out chunk))
        {
            MeshGenerator.remeshChunk(this, chunk);
        }
        if (blockCoords.z == Chunk.CHUNK_SIZE - 1 && loadedChunks.TryGetValue(chunkCoords + new Vector3Int(0, 0, 1), out chunk))
        {
            MeshGenerator.remeshChunk(this, chunk);
        }

        if (blockCoords.x == 0 && loadedChunks.TryGetValue(chunkCoords + new Vector3Int(-1, 0, 0), out chunk))
        {
            MeshGenerator.remeshChunk(this, chunk);
        }
        if (blockCoords.y == 0 && loadedChunks.TryGetValue(chunkCoords + new Vector3Int(0, -1, 0), out chunk))
        {
            MeshGenerator.remeshChunk(this, chunk);
        }
        if (blockCoords.z == 0 && loadedChunks.TryGetValue(chunkCoords + new Vector3Int(0, 0, -1), out chunk))
        {
            MeshGenerator.remeshChunk(this, chunk);
        }


    }
    //returns empty if the chunk doesn't exist
    public BlockData getBlock(Vector3Int worldCoords)
    {
        Vector3Int chunkCoords = new Vector3Int(worldCoords.x / Chunk.CHUNK_SIZE, worldCoords.y / Chunk.CHUNK_SIZE, worldCoords.z / Chunk.CHUNK_SIZE);
        Vector3Int blockCoords = new Vector3Int(worldCoords.x % Chunk.CHUNK_SIZE, worldCoords.y % Chunk.CHUNK_SIZE, worldCoords.z % Chunk.CHUNK_SIZE);
        if (blockCoords.x < 0)
        {
            chunkCoords.x -= 1;
            blockCoords.x += Chunk.CHUNK_SIZE;
        }
        if (blockCoords.y < 0)
        {
            chunkCoords.y -= 1;
            blockCoords.y += Chunk.CHUNK_SIZE;
        }
        if (blockCoords.z < 0)
        {
            chunkCoords.z -= 1;
            blockCoords.z += Chunk.CHUNK_SIZE;
        }
        Chunk chunk;
        if (loadedChunks.TryGetValue(chunkCoords, out chunk))
        {
            return Block.blockTypes[(int)chunk.blocks[blockCoords.x, blockCoords.y, blockCoords.z].type];
        }
        else
        {
            return Block.blockTypes[(int)BlockType.chunk_border];
        }
    }
    //returns empty if the chunk doesn't exist
    public BlockData getBlock(Vector3Int chunkCoords, Vector3Int blockCoords)
    {
        Chunk chunk;
        if (loadedChunks.TryGetValue(chunkCoords, out chunk))
        {
            if (chunk == null || chunk.blocks == null)
                return Block.blockTypes[(int)BlockType.chunk_border];
            return Block.blockTypes[(int)chunk.blocks[blockCoords.x, blockCoords.y, blockCoords.z].type];
        }
        else
        {
            return Block.blockTypes[(int)BlockType.chunk_border];
        }
    }
    //stores blockData using getBlock() in indicies 0-5 in toStore.
    public void getSurroundingBlocks(Vector3Int position, BlockData[] toStore)
    {
        toStore[(int)Direction.PosX] = getBlock(new Vector3Int(position.x + 1, position.y, position.z));
        toStore[(int)Direction.PosY] = getBlock(new Vector3Int(position.x, position.y + 1, position.z));
        toStore[(int)Direction.PosZ] = getBlock(new Vector3Int(position.x, position.y, position.z + 1));
        toStore[(int)Direction.NegX] = getBlock(new Vector3Int(position.x - 1, position.y, position.z));
        toStore[(int)Direction.NegY] = getBlock(new Vector3Int(position.x, position.y - 1, position.z));
        toStore[(int)Direction.NegZ] = getBlock(new Vector3Int(position.x, position.y, position.z - 1));
    }

    //stores blockData using getBlock() in indicies 0-5 in toStore. blockCoords should be [0,CHUNK_SIZE-1] on all axis, but we don't check this in interest of speed.
    public void getSurroundingBlocks(Vector3Int chunkCoords, Vector3Int blockCoords, BlockData[] toStore)
    {
        //we have to test for chunk borders
        if (blockCoords.x == Chunk.CHUNK_SIZE - 1)
        {
            toStore[(int)Direction.PosX] = getBlock(new Vector3Int(chunkCoords.x + 1, chunkCoords.y, chunkCoords.z), new Vector3Int(0, blockCoords.y, blockCoords.z));
            toStore[(int)Direction.NegX] = getBlock(chunkCoords, new Vector3Int(blockCoords.x - 1, blockCoords.y, blockCoords.z));
        }
        else
        {
            toStore[(int)Direction.PosX] = getBlock(chunkCoords, new Vector3Int(blockCoords.x + 1, blockCoords.y, blockCoords.z));
            if (blockCoords.x == 0)
            {
                toStore[(int)Direction.NegX] = getBlock(new Vector3Int(chunkCoords.x - 1, chunkCoords.y, chunkCoords.z), new Vector3Int(Chunk.CHUNK_SIZE - 1, blockCoords.y, blockCoords.z));
            }
            else
            {
                toStore[(int)Direction.NegX] = getBlock(chunkCoords, new Vector3Int(blockCoords.x - 1, blockCoords.y, blockCoords.z));
            }
        }

        if (blockCoords.y == Chunk.CHUNK_SIZE - 1)
        {
            toStore[(int)Direction.PosY] = getBlock(new Vector3Int(chunkCoords.x, chunkCoords.y + 1, chunkCoords.z), new Vector3Int(blockCoords.x, 0, blockCoords.z));
            toStore[(int)Direction.NegY] = getBlock(chunkCoords, new Vector3Int(blockCoords.x, blockCoords.y - 1, blockCoords.z));
        }
        else
        {
            toStore[(int)Direction.PosY] = getBlock(chunkCoords, new Vector3Int(blockCoords.x, blockCoords.y + 1, blockCoords.z));
            if (blockCoords.y == 0)
            {
                toStore[(int)Direction.NegY] = getBlock(new Vector3Int(chunkCoords.x, chunkCoords.y - 1, chunkCoords.z), new Vector3Int(blockCoords.x, Chunk.CHUNK_SIZE - 1, blockCoords.z));
            }
            else
            {
                toStore[(int)Direction.NegY] = getBlock(chunkCoords, new Vector3Int(blockCoords.x, blockCoords.y - 1, blockCoords.z));
            }
        }

        if (blockCoords.z == Chunk.CHUNK_SIZE - 1)
        {
            toStore[(int)Direction.PosZ] = getBlock(new Vector3Int(chunkCoords.x, chunkCoords.y, chunkCoords.z + 1), new Vector3Int(blockCoords.x, blockCoords.y, 0));
            toStore[(int)Direction.NegZ] = getBlock(chunkCoords, new Vector3Int(blockCoords.x, blockCoords.y, blockCoords.z - 1));
        }
        else
        {
            toStore[(int)Direction.PosZ] = getBlock(chunkCoords, new Vector3Int(blockCoords.x, blockCoords.y, blockCoords.z + 1));
            if (blockCoords.z == 0)
            {
                toStore[(int)Direction.NegZ] = getBlock(new Vector3Int(chunkCoords.x, chunkCoords.y, chunkCoords.z - 1), new Vector3Int(blockCoords.x, blockCoords.y, Chunk.CHUNK_SIZE - 1));
            }
            else
            {
                toStore[(int)Direction.NegZ] = getBlock(chunkCoords, new Vector3Int(blockCoords.x, blockCoords.y, blockCoords.z - 1));
            }
        }
    }
}