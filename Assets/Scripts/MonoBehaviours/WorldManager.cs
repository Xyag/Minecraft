using UnityEngine;
using System.Threading.Tasks;
using System.Collections;
using System.Diagnostics;

[RequireComponent(typeof(TextureLoader))]
public class WorldManager : MonoBehaviour
{
    public GameObject EmptyChunkPrefab;
    public PhysicMaterial ChunkPhysicMaterial;
    public World world;
    public GameObject ExplosionParticles;
    public Entity Player;
    public GameObject[] spawnableEntities;
    public int MinChunkLoadsPerFrame = 5;
    public int MinChunkUnloadsPerFrame = 5;

    private TextureLoader textureLoader;
    private long targetFrameTimeMS;
    private Stopwatch frameTimer;

    public void Awake()
    {
        Application.targetFrameRate = 60;
        targetFrameTimeMS = (long)(1000.0f/(float)Application.targetFrameRate);
        frameTimer = new Stopwatch();

        textureLoader = GetComponent<TextureLoader>();

        MeshGenerator.emptyChunk = EmptyChunkPrefab;
        MeshGenerator.chunkPhysMaterial = ChunkPhysicMaterial;

        world = new World();
        world.explosionParticles = ExplosionParticles;

        Player.world = world;
        world.loadedEntities.Add(Player);

        foreach (var go in spawnableEntities)
        {
            var entity = go.GetComponent<Entity>();
            world.entityTypes.Add(entity.type, go);
        }

        WorldLoader wl = GetComponent<WorldLoader>();
        if (wl)
        {
            wl.world = world;
            wl.player = Player;
        }
    }
    public async void Start()
    {
        float currTime = Time.realtimeSinceStartup;
        int worldHeight = 5 * 16 / Chunk.CHUNK_SIZE;
        int worldWidth = 5 * 16 / Chunk.CHUNK_SIZE;
        Cursor.lockState = CursorLockMode.Locked;

        await WorldGenerator.generateRegion(world, new Vector3Int(-worldWidth / 2, -worldHeight / 2, -worldWidth / 2), worldWidth, worldHeight, worldWidth);
        float newTime = Time.realtimeSinceStartup;
        UnityEngine.Debug.Log("generated " + (1000 * (newTime - currTime)));
        currTime = Time.realtimeSinceStartup;

        MeshGenerator.spawnAll(world.loadedChunks.Values, world);
        newTime = Time.realtimeSinceStartup;
    }
    public void Update()
    {
        frameTimer.Restart();
    }
    public void LateUpdate()
    {
        //we do about half the remaining frame time on loads and half on unloads
        long currTime = frameTimer.ElapsedMilliseconds;
        MeshGenerator.spawnFromQueue((targetFrameTimeMS-currTime)/2,MinChunkLoadsPerFrame);
        currTime = frameTimer.ElapsedMilliseconds;
        world.unloadFromQueue(targetFrameTimeMS-currTime,MinChunkUnloadsPerFrame);
    }
    async Task slowGen(World world, int startZ)
    {
        System.Threading.Thread.Sleep(2);
        startZ++;
        await WorldGenerator.generateRegion(world, new Vector3Int(0, 0, startZ), 10, 10, 1);
    }
}