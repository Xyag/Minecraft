using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;

public class World
{
    const float EXPLOSION_PARTICLES_SCALE = 0.125f;

    public ChunkBuffer unloadChunkBuffer = new ChunkBuffer(1000);

    public Dictionary<Vector3Int, Chunk> loadedChunks = new Dictionary<Vector3Int, Chunk>();
    public Dictionary<EntityType, Pool<GameObject>> entityTypes = new Dictionary<EntityType, Pool<GameObject>>();
    public List<Entity> loadedEntities = new List<Entity>();
    public readonly string savePath;
    public readonly string name;
    public readonly Vector3Int worldRadius;
    public readonly bool infinite;
    private readonly ChunkSerializer chunkSerializer;
    public GameObject explosionParticles
    {
        set
        {
            explosionParticlesPool = Pool<GameObject>.createEntityPool(value, this);
        }
    }

    public World(string savePath, string name, GameObject explosionParticles)
    {
        this.savePath = savePath;
        chunkSerializer = new ChunkSerializer(savePath);
        chunkSerializer.updateWorldInfo(new WorldInfo { fileName = name, lastPlayed = System.DateTime.Now });
        this.explosionParticles = explosionParticles;
        worldRadius = new Vector3Int(3, 3, 3);
        infinite = true;
    }
    public void saveAll()
    {
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        List<Chunk> toSave = new List<Chunk>(loadedChunks.Values.Count);
        lock (loadedChunks)
        {
            foreach(var chunk in loadedChunks.Values)
            {
                toSave.Add(chunk);
            }
        }
        foreach(var chunk in toSave)
        {
            chunkSerializer.writeChunk(chunk);
        }
    }



    private System.Diagnostics.Stopwatch unloadStopwatch = new System.Diagnostics.Stopwatch();
    private Pool<GameObject> explosionParticlesPool;
    public bool chunkInBounds(Vector3Int coords)
    {
        if (infinite) return true;
        return (-worldRadius.x < coords.x && coords.x < worldRadius.x) && (-worldRadius.y < coords.y && coords.y < worldRadius.y) && (-worldRadius.z < coords.z && coords.z < worldRadius.z);
    }
    public GameObject spawnEntity(EntityType type, Vector3 position, Vector3 velocity)
    {
        var go = entityTypes[type].get();
        go.transform.position = position;
        Entity e = go.GetComponent<Entity>();
        e.initialize(this);
        e.velocity = velocity;
        return go;
    }
    public void createExplosion(float explosionStrength, Vector3Int origin)
    {
        int currInterval = 0;
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
                            currBlock.interact(blockPos + origin, this);
                        }
                        setBlockAndMesh(blockPos + origin, BlockType.empty, updateNeighbors: true);
                        currInterval++;
                    }
                }
            }
        }
        /*Vector3Int chunkPos = WorldToChunkCoords(origin);
        List<Chunk> remeshQueue = new List<Chunk>(csize * csize * csize * 5);
        for (int x = -csize; x <= csize; x++)
        {
            for (int y = -csize; y <= csize; y++)
            {
                for (int z = -csize; z <= csize; z++)
                {
                    if (x * x + y * y + z * z <= (csize + 1) * (csize + 1))
                    {
                        var chunk = getChunk(chunkPos + new Vector3Int(x, y, z));
                        if (chunk != null)
                            remeshQueue.Add(chunk);
                    }
                }
            }
        }
        foreach (var chunk in remeshQueue)
        {
            MeshGenerator.addToFrameBuffer(chunk);
        }*/
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
                Chunk data = unloadChunkBuffer.Pop();
                unloadChunk(data);
                chunksRemaining--;
                unloads++;
            }
        }
    }
    //returns true if the chunk successfully loaded (so it needs to be meshed)
    public bool loadChunkFromFile(Vector3Int coords)
    {
        if (!chunkInBounds(coords))
            return false;
        lock (loadedChunks)
        {
            if (loadedChunks.ContainsKey(coords))
            {
                Debug.Log("loading already loaded chunk");
                return false;
            }
            else
            {
                Chunk read = chunkSerializer.readChunk(coords);
                if (read != null)
                {
                    loadedChunks.Add(coords, read);
                }
            }
        }
        return false;
    }
    public void createChunk(Chunk c)
    {
        if (!chunkInBounds(c.chunkCoords))
            return;
        lock (loadedChunks)
        {
            if (!loadedChunks.TryGetValue(c.chunkCoords, out Chunk temp))
                loadedChunks.Add(c.chunkCoords, c);
            else
                Debug.Log("creating already loaded chunk");
        }
    }
    public void unloadChunk(Chunk chunk)
    {
        lock (loadedChunks)
        {
            if (chunk.gameObject != null)
            {
                chunk.gameObject.SetActive(false);
            }
            loadedChunks.Remove(chunk.chunkCoords);
        }
        lock (chunkSerializer)
        {
            chunkSerializer.writeChunk(chunk);
        }
    }
    public void unloadChunk(Vector3Int coords)
    {
        Chunk chunk = null;
        lock (loadedChunks)
        {
            chunk = loadedChunks[coords];
        }
        unloadChunk(chunk);
    }
    public async Task getChunks(List<Vector3Int> coords, List<Chunk> output)
    {
        List<Vector3Int> toGenerate = new List<Vector3Int>();
        foreach (var pos in coords)
        {
            Chunk temp = getChunk(pos);
            if (temp == null)
                toGenerate.Add(pos);
            else
                output.Add(temp);
        }
        List<Chunk> generated = await WorldGenerator.generateList(this, toGenerate);
        foreach (Chunk c in generated)
        {
            output.Add(c);
        }
    }
    public Chunk getChunk(Vector3Int chunkCoords)
    {
        if (!chunkInBounds(chunkCoords))
            return null;
        Chunk chunk;
        lock (loadedChunks)
        {
            if (loadedChunks.TryGetValue(chunkCoords, out chunk))
            {
                return chunk;
            }
        }
        if ((chunk = chunkSerializer.readChunk(chunkCoords)) != null)
        {
            lock (loadedChunks)
            {
                loadedChunks.Add(chunkCoords, chunk);
            }
            return chunk;
        }
        else
        {
            return null;
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
        lock (loadedChunks)
        {
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
        }
        return neighbors;
    }
    public Chunk setBlock(Vector3Int worldCoords, BlockType block, bool forceLoadChunk = false, bool updateNeighbors = false)
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
        return setBlock(chunkCoords, blockCoords, block, forceLoadChunk, updateNeighbors);
    }
    public Chunk setBlock(Vector3Int chunkCoords, Vector3Int blockCoords, BlockType block, bool forceLoadChunk = false, bool updateNeighbors = false)
    {
        Chunk chunk = getChunk(chunkCoords);
        if (chunk != null)
        {
            if (chunk.blocks == null)
                chunk.blocks = new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];
            chunk.blocks[blockCoords.x, blockCoords.y, blockCoords.z].type = block;
            if (updateNeighbors)
            {
                getBlock(chunkCoords, blockCoords).onBlockUpdate(Chunk.CHUNK_SIZE * chunkCoords + blockCoords, this);
                updateNeighborBlocks(chunkCoords*Chunk.CHUNK_SIZE + blockCoords);
            }
            return chunk;
        }
        else if (forceLoadChunk)
        {
            chunk = new Chunk(new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE], chunkCoords);
            createChunk(chunk);
            chunk.blocks[blockCoords.x, blockCoords.y, blockCoords.z].type = block;
            return chunk;
        }
        return null;
    }
    public void updateNeighborBlocks(Vector3Int worldPos)
    {
        getBlock(new Vector3Int(worldPos.x + 1, worldPos.y, worldPos.z)).onBlockUpdate(new Vector3Int(worldPos.x + 1, worldPos.y, worldPos.z), this);
        getBlock(new Vector3Int(worldPos.x - 1, worldPos.y, worldPos.z)).onBlockUpdate(new Vector3Int(worldPos.x - 1, worldPos.y, worldPos.z), this);
        getBlock(new Vector3Int(worldPos.x, worldPos.y + 1, worldPos.z)).onBlockUpdate(new Vector3Int(worldPos.x, worldPos.y + 1, worldPos.z), this);
        getBlock(new Vector3Int(worldPos.x, worldPos.y - 1, worldPos.z)).onBlockUpdate(new Vector3Int(worldPos.x, worldPos.y - 1, worldPos.z), this);
        getBlock(new Vector3Int(worldPos.x, worldPos.y, worldPos.z + 1)).onBlockUpdate(new Vector3Int(worldPos.x, worldPos.y, worldPos.z + 1), this);
        getBlock(new Vector3Int(worldPos.x, worldPos.y, worldPos.z - 1)).onBlockUpdate(new Vector3Int(worldPos.x, worldPos.y, worldPos.z - 1), this);
    }
    //force loads
    public void setBlockAndMesh(Vector3Int worldCoords, BlockType block, bool updateNeighbors = true)
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
        Chunk chunk = setBlock(chunkCoords, blockCoords, block, true, updateNeighbors);
        MeshGenerator.meshChunkBlockChanged(chunk, blockCoords, this);
    }
    
    //returns chunk_border if the chunk doesn't exist
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
        return getBlock(chunkCoords, blockCoords);
    }
    //returns chunk_border if the chunk doesn't exist
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public BlockData getBlock(Vector3Int chunkCoords, Vector3Int blockCoords)
    {
        if (loadedChunks.TryGetValue(chunkCoords, out Chunk chunk))
        {
            if (chunk == null)
                return Block.blockTypes[(int)BlockType.chunk_border];
            if (chunk.blocks == null)
                return Block.blockTypes[(int)BlockType.empty];
            int blockType = (int)chunk.blocks[blockCoords.x, blockCoords.y, blockCoords.z].type;
            return Block.blockTypes[blockType];
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
    public BlockHit raycast(Vector3 origin, Vector3 direction, float distance)
    {
        direction = direction.normalized;
        float distTraveled = 0;
        const float step = 0.1f;
        while (distTraveled <= distance)
        {
            Vector3 testPoint = distTraveled * direction + origin;
            distTraveled += step;
            Vector3Int testWorldCoord = new Vector3Int(Mathf.RoundToInt(testPoint.x), Mathf.RoundToInt(testPoint.y), Mathf.RoundToInt(testPoint.z));
            BlockData currBlock = getBlock(testWorldCoord);
            if (currBlock.raycastable)
            {
                return new BlockHit(currBlock, testWorldCoord, new Vector3(testPoint.x-testWorldCoord.x, testPoint.y - testWorldCoord.y, testPoint.z - testWorldCoord.z));
            }
        }
        return new BlockHit(null, Vector3Int.zero, Vector3.zero, false);
    }
    public BlockHit raycastToEmpty(Vector3 origin, Vector3 direction, float distance)
    {
        direction = direction.normalized;
        float distTraveled = 0;
        const float step = 0.1f;
        while (distTraveled <= distance)
        {
            Vector3 testPoint = distTraveled * direction + origin;
            distTraveled += step;
            Vector3Int testWorldCoord = new Vector3Int(Mathf.RoundToInt(testPoint.x), Mathf.RoundToInt(testPoint.y), Mathf.RoundToInt(testPoint.z));
            BlockData currBlock = getBlock(testWorldCoord);
            if (currBlock.type == BlockType.empty)
            {
                return new BlockHit(currBlock, testWorldCoord, new Vector3(testPoint.x - testWorldCoord.x, testPoint.y - testWorldCoord.y, testPoint.z - testWorldCoord.z));
            }
        }
        return new BlockHit(null, Vector3Int.zero, Vector3.zero, false);
    }
}