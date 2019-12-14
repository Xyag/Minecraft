using UnityEngine;
using System.Threading.Tasks;
using System.Collections;

[RequireComponent(typeof(TextureLoader))]
public class WorldManager : MonoBehaviour
{
    public GameObject EmptyChunkPrefab;
    public PhysicMaterial ChunkPhysicMaterial;
    public World world;
    public GameObject ExplosionParticles;
    public Entity Player;
    public GameObject[] spawnableEntities;

    private TextureLoader textureLoader;

    public void Awake()
    {
        Application.targetFrameRate = 60;
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
        Debug.Log("generated " + (1000 * (newTime - currTime)));
        currTime = Time.realtimeSinceStartup;
        //
        await MeshGenerator.spawnAll(world.loadedChunks.Values, world, world.loadedChunks.Count);
        newTime = Time.realtimeSinceStartup;
        Debug.Log("spawned " + (1000 * (newTime - currTime)));
    }

    async Task slowGen(World world, int startZ)
    {
        System.Threading.Thread.Sleep(2);
        startZ++;
        await WorldGenerator.generateRegion(world, new Vector3Int(0, 0, startZ), 10, 10, 1);
    }
}