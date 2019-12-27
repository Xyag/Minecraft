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
        Cursor.lockState = CursorLockMode.Locked;

        targetFrameTimeMS = (long)(1000.0f/(float)Application.targetFrameRate);
        frameTimer = new Stopwatch();

        textureLoader = GetComponent<TextureLoader>();

        MeshGenerator.emptyChunk = EmptyChunkPrefab;
        MeshGenerator.chunkPhysMaterial = ChunkPhysicMaterial;

        world = new World
        {
            explosionParticles = ExplosionParticles
        };

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
    public void Update()
    {
        frameTimer.Restart();
    }
    public void LateUpdate()
    {
        //we divide the remaining frame time between spawning and unloading
        long currTime = frameTimer.ElapsedMilliseconds;
        MeshGenerator.spawnFromQueue((targetFrameTimeMS-currTime)/3,MinChunkLoadsPerFrame);
        currTime = frameTimer.ElapsedMilliseconds;
        world.unloadFromQueue((targetFrameTimeMS-currTime)/2,MinChunkUnloadsPerFrame);
    }
}