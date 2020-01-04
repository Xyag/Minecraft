using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System;

public class World
{
    const float EXPLOSION_PARTICLES_SCALE = 0.125f;

    public ChunkBuffer unloadChunkBuffer = new ChunkBuffer(1000);
    public Dictionary<Vector3Int, Chunk> loadedChunks = new Dictionary<Vector3Int, Chunk>();
    public Dictionary<EntityType, Pool<GameObject>> entityTypes = new Dictionary<EntityType, Pool<GameObject>>();
    public List<Entity> loadedEntities = new List<Entity>();
    public GameObject explosionParticles
    {
        set
        {
            explosionParticlesPool = Pool<GameObject>.createEntityPool(value, this);
        }
    }


    private System.Diagnostics.Stopwatch unloadStopwatch = new System.Diagnostics.Stopwatch();
    private Pool<GameObject> explosionParticlesPool;

    public GameObject spawnEntity(EntityType type, Vector3 position)
    {
        var go = entityTypes[type].get();
        go.transform.position = position;
        Entity e = go.GetComponent<Entity>();
        e.initialize(this);
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
                    {
                        remeshQueue.Add(getChunk(chunkPos + new Vector3Int(x, y, z)));
                    }
                }
            }
        }
        Task t = MeshGenerator.remeshAll(remeshQueue, this, remeshQueue.Count);
        foreach (var en in loadedEntities)
        {
            if (en != null && Vector3.SqrMagnitude(en.transform.position - (Vector3)origin) < explosionStrength * explosionStrength)
            {
                en.velocity += (en.transform.position - (Vector3)origin).normalized * explosionStrength;
            }
        }
        var explo = explosionParticlesPool.get();
        explo.transform.localScale = explosionStrength * EXPLOSION_PARTICLES_SCALE * Vector3.one;
        explo.transform.position = origin;
        await t;
    }
    public void unloadFromQueue(long maxTimeMS, int minUnloads)
    {
        lock (unloadChunkBuffer)
        {
            unloadStopwatch.Restart();
            int chunksRemaining = unloadChunkBuffer.Count();
            int unloads = 0;
            while (chunksRemaining > 0 && (unloads < minUnloads || unloadStopwatch.ElapsedMilliseconds < maxTimeMS))
            {
                Chunk data = unloadChunkBuffer.Dequeue();
                unloadChunk(data);
                chunksRemaining--;
                unloads++;
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
            {
                chunk.gameObject.SetActive(false);
                chunk.gameObject.GetComponent<MeshFilter>().sharedMesh.Clear();
            }
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
            chunk = new Chunk(null, coords);
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
            if (chunk.blocks == null)
                chunk.blocks = new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];
            chunk.blocks[blockCoords.x, blockCoords.y, blockCoords.z].type = block;
        }
        else if (forceLoadChunk)
        {
            chunk = new Chunk(new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE], chunkCoords);
            loadChunk(chunk);
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
            if (chunk.blocks == null)
                chunk.blocks = new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];
            chunk.blocks[blockCoords.x, blockCoords.y, blockCoords.z].type = block;
        }
        else
        {
            chunk = new Chunk(new Block[Chunk.CHUNK_SIZE,Chunk.CHUNK_SIZE,Chunk.CHUNK_SIZE], chunkCoords);
            loadChunk(chunk);
            chunk.blocks[blockCoords.x,blockCoords.y,blockCoords.z].type = block;
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
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
            if (chunk.blocks == null)
                return Block.blockTypes[(int)BlockType.empty];
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
            if (chunk == null)
                return Block.blockTypes[(int)BlockType.chunk_border];
            if (chunk.blocks == null)
                return Block.blockTypes[(int)BlockType.empty];
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
    public BlockHit raycast(Vector3 origin, Vector3 direction, float distance)
    {
        direction = direction.normalized;
        if (direction.x == 0)
        {
            direction.x = 0.0000001f;
        }
        if (direction.y == 0)
        {
            direction.y = 0.0000001f;
        }
        if (direction.z == 0)
        {
            direction.z = 0.0000001f;
        }
        
        float distanceRemaining = distance;
        Vector3 currEnd = origin;


        while (distanceRemaining >= 0)
        {
            Vector3Int hitCoords = new Vector3Int(Mathf.RoundToInt(currEnd.x), Mathf.RoundToInt(currEnd.y), Mathf.RoundToInt(currEnd.z));
            BlockData hitBlock = getBlock(hitCoords);
            if (hitBlock.fullCollision)
            {
                return new BlockHit(hitBlock, hitCoords);
            }

            Vector3 dests = new Vector3(Mathf.Floor(currEnd.x + 1), Mathf.Floor(currEnd.y + 1), Mathf.Floor(currEnd.z + 1));
            if (direction.x < 0)
            {
                dests.x = Mathf.Ceil(currEnd.x - 1);
            }
            if (direction.y < 0)
            {
                dests.y = Mathf.Ceil(currEnd.y - 1);
            }
            if (direction.z < 0)
            {
                dests.z = Mathf.Ceil(currEnd.z - 1);
            }
            Vector3 delta = currEnd - dests;
            if (distanceRemaining*distanceRemaining < delta.sqrMagnitude)
            {
                return new BlockHit(null, Vector3Int.zero, false);
            }
            Vector3 ratios = new Vector3(delta.x/direction.x,delta.y/direction.y,delta.z/direction.z);
            if (ratios.x <= ratios.y && ratios.x <= ratios.z)
            {
                float r = delta.x / direction.x;
                Vector3 travel = -new Vector3(delta.x, r*direction.y,r*direction.z);
                currEnd.x += travel.x;
                currEnd.y += travel.y;
                currEnd.z += travel.z;
                distanceRemaining -= travel.magnitude;
                Debug.Log("direction: " + direction + ", " + travel.normalized.ToString());
            }
            else if (ratios.y <= ratios.z)
            {
                //y is best
                float r = delta.y / direction.y;
                Vector3 travel = -new Vector3(r*direction.x, delta.y, r * direction.z);
                currEnd.x += travel.x;
                currEnd.y += travel.y;
                currEnd.z += travel.z;
                distanceRemaining -= travel.magnitude;
                Debug.Log("direction: " + direction + ", " + travel.normalized.ToString());
            }
            else
            {
                //z is best
                float r = delta.z / direction.z;
                Vector3 travel = -new Vector3(r * direction.x, r * direction.y, delta.z);
                currEnd.x += travel.x;
                currEnd.y += travel.y;
                currEnd.z += travel.z;
                distanceRemaining -= travel.magnitude;
                Debug.Log("direction: " + direction + ", " + travel.normalized.ToString());
            }
        }
        return new BlockHit(null, Vector3Int.zero, false);
    }
}