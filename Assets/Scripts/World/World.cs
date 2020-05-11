using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using Mirror;

public class World
{
    const float EXPLOSION_PARTICLES_SCALE = 0.125f;
    public static readonly int ConcurrencyLevel = System.Environment.ProcessorCount * 2;

    public readonly ConcurrentDictionary<Vector3Int, Chunk> unloadChunkBuffer = new ConcurrentDictionary<Vector3Int, Chunk>(ConcurrencyLevel, 1000);//1000 is arbitrary, doesn't really matter.

    public readonly ConcurrentDictionary<Vector3Int, Chunk> loadedChunks = new ConcurrentDictionary<Vector3Int, Chunk>(ConcurrencyLevel, 1000); //1000 is arbitrary, doesn't really matter.
    public readonly Dictionary<EntityType, Pool<GameObject>> entityTypes = new Dictionary<EntityType, Pool<GameObject>>();
    public readonly Dictionary<NetworkConnection, Entity> players;
    public readonly List<Entity> loadedEntities = new List<Entity>();
    public readonly string savePath;
    public readonly string name;
    public readonly Vector3Int worldRadius;
    public readonly bool infinite;
    private readonly System.Diagnostics.Stopwatch unloadStopwatch = new System.Diagnostics.Stopwatch();
    private readonly Pool<GameObject> explosionParticlesPool;

    public readonly bool isServer;
    public readonly NetworkConnection connection;

    public World(string savePath, string name, GameObject explosionParticles, bool isServer)
    {
        if (isServer)
        {
            this.savePath = savePath;
            ChunkSerializer.updateWorldInfo(new WorldInfo { fileName = name, lastPlayed = System.DateTime.Now });
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            ChunkSerializer.savePath = savePath;
            players = new Dictionary<NetworkConnection, Entity>();
        }
        
        explosionParticlesPool = Pool<GameObject>.createEntityPool(explosionParticles, this);
        infinite = true;
        this.isServer = isServer;
    }
    public void saveAll()
    {
        if (!isServer)
        {
            Debug.LogError("trying to save the world but i'm not the server!");
            return;
        }
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        List<Chunk> toSave = new List<Chunk>(loadedChunks.Values.Count);
        foreach (var chunk in loadedChunks.Values)
        {
            toSave.Add(chunk);
        }
        foreach (var chunk in toSave)
        {
            ChunkSerializer.writeChunkToFile(chunk);
        }
    }

    public bool chunkInBounds(Vector3Int coords)
    {
        if (infinite) return true;
        return (-worldRadius.x < coords.x && coords.x < worldRadius.x) && (-worldRadius.y < coords.y && coords.y < worldRadius.y) && (-worldRadius.z < coords.z && coords.z < worldRadius.z);
    }
    public GameObject spawnEntity(EntityType type, Vector3 position, Vector3 velocity)
    {
        if (!isServer)
        {
            Debug.LogError("trying to spawn entity but i'm not the server!");
            return null;
        }
        var go = entityTypes[type].get();
        go.transform.position = position;
        Entity e = go.GetComponent<Entity>();
        e.initialize(this);
        e.velocity = velocity;
        NetworkServer.Spawn(go);
        return go;
    }
    public void createExplosion(float explosionStrength, Vector3Int origin)
    {
        if (!isServer)
        {
            Debug.LogError("trying to create explosion but i'm not the server");
            return;
        }
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
                        setBlock(blockPos + origin, BlockType.empty);
                        currInterval++;
                    }
                }
            }
        }
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
        NetworkServer.Spawn(explo);
    }
    public void unloadFromQueue(long maxTimeMS, int minUnloads)
    {
        lock (unloadChunkBuffer)
        {
            unloadStopwatch.Restart();
            int chunksRemaining = unloadChunkBuffer.Count;
            int unloads = 0;
            var enumerator = unloadChunkBuffer.Keys.GetEnumerator();
            while (chunksRemaining > 0 && (unloads < minUnloads || unloadStopwatch.ElapsedMilliseconds < maxTimeMS))
            {
                if (unloadChunkBuffer.TryRemove(enumerator.Current, out Chunk data))
                {
                    unloadChunk(data);
                    chunksRemaining--;
                    unloads++;
                }
                enumerator.MoveNext();
            }
        }
    }
    //returns true if the chunk successfully loaded (so it needs to be meshed)
    public bool loadChunkFromFile(Vector3Int coords)
    {
        if (!isServer)
        {
            Debug.LogError("loading chunk from file but i'm not the server!");
            return false;
        }
        if (!chunkInBounds(coords))
            return false;
        if (loadedChunks.ContainsKey(coords))
        {
            Debug.Log("loading already loaded chunk");
            return false;
        }
        else
        {
            Chunk read = ChunkSerializer.readChunk(coords);
            if (read != null)
            {
                loadedChunks.TryAdd(coords, read);
            }
        }

        return false;
    }
    //called on the client when a chunk is recieved from the server
    public Chunk recieveChunk(ChunkMessage message)
    {
        return loadedChunks.AddOrUpdate(message.chunk.chunkCoords, message.chunk, (key, old) => {
            message.chunk.gameObject = old.gameObject;
            message.chunk.changed = true;
            return message.chunk;
        });
    }
    public void createChunk(Chunk c)
    {
        if (!chunkInBounds(c.chunkCoords))
            return;
        loadedChunks.TryAdd(c.chunkCoords, c);
    }
    public void unloadChunk(Chunk chunk)
    {
        if (loadedChunks.TryRemove(chunk.chunkCoords, out Chunk c))
        {
            c.gameObject?.SetActive(false);
        }
        if (isServer)
        {
            ChunkSerializer.writeChunkToFile(chunk);
        }
    }
    public void unloadChunk(Vector3Int coords)
    {
        if (loadedChunks.TryGetValue(coords, out Chunk chunk))
        {
            unloadChunk(chunk);
        }
    }
    public async Task generateChunks(List<Vector3Int> coords)
    {
        if (!isServer)
        {
            Debug.LogError("tried to generate chunks but i'm not the server!");
            return;
        }
        List<Vector3Int> toGenerate = new List<Vector3Int>();
        foreach (var pos in coords)
        {
            Chunk temp = getChunk(pos);
            if (temp == null)
            {
                toGenerate.Add(pos);
            }
        }
       List<Chunk> chunks = await WorldGenerator.generateList(this, toGenerate);
       foreach (Chunk c in chunks)
        {
            loadedChunks.AddOrUpdate(c.chunkCoords, c, (key, oldval) => c);
        }
    }
    //if called from a client and the client has to request the chunk, return null
    public Chunk getChunk(Vector3Int chunkCoords)
    {
        if (!chunkInBounds(chunkCoords))
            return null;
        if (loadedChunks.TryGetValue(chunkCoords, out Chunk chunk))
        {
            return chunk;
        }
        if (isServer)
        {
            if ((chunk = ChunkSerializer.readChunk(chunkCoords)) != null)
            {
                loadedChunks.TryAdd(chunkCoords, chunk);
                return chunk;
            }
            else
            {
                return null;
            }
        }
        else
        {
            WorldManager.singleton.SendRequestChunk(chunkCoords);
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
    public Chunk setBlock(Vector3Int worldCoords, BlockType block, bool forceLoadChunk = false, bool updateNeighbors = false)
    {
        if (!isServer)
        {
            Debug.LogError("i'm trying to setBlock but i'm not the server!");
            return null;
        }
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
        if (!isServer)
        {
            Debug.LogError("i'm trying to setBlock but i'm not the server!");
            return null;
        }
        Chunk chunk = getChunk(chunkCoords);
        if (chunk != null)
        {
            if (chunk.blocks == null)
                chunk.blocks = new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];
            BlockType oldType = chunk.blocks[blockCoords.x, blockCoords.y, blockCoords.z].type;
            Block.blockTypes[(int)oldType].onDestroy(chunkCoords * Chunk.CHUNK_SIZE + blockCoords, this);
            chunk.blocks[blockCoords.x, blockCoords.y, blockCoords.z].type = block;
            Block.blockTypes[(int)block].onPlace(chunkCoords * Chunk.CHUNK_SIZE + blockCoords, this);
            if (updateNeighbors)
            {
                getBlock(chunkCoords, blockCoords).onBlockUpdate(Chunk.CHUNK_SIZE * chunkCoords + blockCoords, this);
                updateNeighborBlocks(chunkCoords*Chunk.CHUNK_SIZE + blockCoords);
            }
            chunk.changed = true;
            return chunk;
        }
        else if (forceLoadChunk)
        {
            chunk = new Chunk(new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE], chunkCoords);
            createChunk(chunk);
            chunk.blocks[blockCoords.x, blockCoords.y, blockCoords.z].type = block;
            loadedChunks.TryAdd(chunk.chunkCoords, chunk);
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
    
    //returns chunk_border if the chunk doesn't exist
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
    public bool validChunkRequest(Vector3 requesterPosition, Vector3Int requestedChunk, WorldLoader loader)
    {
        return loader.chunkNearPlayer(WorldToChunkCoords(requesterPosition), requestedChunk);
    }
}