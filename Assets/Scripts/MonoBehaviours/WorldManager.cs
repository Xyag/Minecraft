using UnityEngine;
using System.Threading.Tasks;
using System.Collections;
using System.Diagnostics;

public class WorldManager : MonoBehaviour
{
    public GameObject EmptyChunkPrefab;
    public PhysicMaterial ChunkPhysicMaterial;
    public World world;
    public GameObject ExplosionParticles;
    public GameObject Player;
    public GameObject[] spawnableEntities;
    public int MinChunkLoadsPerFrame = 5;
    public int MinChunkUnloadsPerFrame = 5;

    public Inventory cursorInventory;

    private long targetFrameTimeMS;
    private Stopwatch frameTimer;

    public void Awake()
    {
        Application.targetFrameRate = 60;

        targetFrameTimeMS = (long)(1000.0f/(float)Application.targetFrameRate);
        frameTimer = new Stopwatch();

        MeshGenerator.chunkPool = Pool<GameObject>.createGameObjectPool(EmptyChunkPrefab,3000); //just picking 3000 cause that's probably more chunks than we need
        MeshGenerator.chunkPhysMaterial = ChunkPhysicMaterial;

        world = new World(Application.persistentDataPath + "/" + SceneData.targetWorld + "/", ExplosionParticles);
        var playerEntity = Player.GetComponent<Entity>();
        playerEntity.initialize(world);
        world.loadedEntities.Add(playerEntity);

        foreach (var go in spawnableEntities)
        {
            var entity = go.GetComponent<Entity>();
            world.entityTypes.Add(entity.type, Pool<GameObject>.createEntityPool(go, world));
        }

        WorldLoader wl = GetComponent<WorldLoader>();
        if (wl)
        {
            wl.world = world;
            wl.player = playerEntity;
        }

        cursorInventory.items = new Item[1];

        
    }

    public void Update()
    {
        frameTimer.Restart();
    }
    public void LateUpdate()
    {
        //we divide the remaining frame time between spawning and unloading
        long currTime = frameTimer.ElapsedMilliseconds;
        MeshGenerator.spawnFromQueue((targetFrameTimeMS - currTime) / 3, MinChunkLoadsPerFrame);
        currTime = frameTimer.ElapsedMilliseconds;
        world.unloadFromQueue((targetFrameTimeMS - currTime) / 2, MinChunkUnloadsPerFrame);
        currTime = frameTimer.ElapsedMilliseconds;
        if (currTime > targetFrameTimeMS)
        {
            UnityEngine.Debug.Log((currTime - targetFrameTimeMS) + " ms over time");
        }
        
    }
}