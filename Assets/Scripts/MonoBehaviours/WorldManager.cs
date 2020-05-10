using UnityEngine;
using System.Threading.Tasks;
using System.Collections;
using System.Diagnostics;
using Mirror;

public class WorldManager : NetworkBehaviour
{
    public static WorldManager singleton;
    public GameObject EmptyChunkPrefab;
    public World world;
    public GameObject ExplosionParticles;
    public AssignPlayer AssignPlayer;
    public GameObject[] spawnableEntities;
    public int MinChunkLoadsPerFrame = 5;
    public int MinChunkUnloadsPerFrame = 5;

    public Inventory cursorInventory;

    private long targetFrameTimeMS;
    private Stopwatch frameTimer;
    private CommandExecutor playerCommandExecutor;

    public void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else
        {
            UnityEngine.Debug.LogError("WorldManager already exists in this session!");
        }
        ((CustomNetworkManager)NetworkManager.singleton).worldManager = this;
        Application.targetFrameRate = 60;

        targetFrameTimeMS = (long)(1000.0f/(float)Application.targetFrameRate);
        frameTimer = new Stopwatch();

        MeshGenerator.chunkPool = Pool<GameObject>.createGameObjectPool(EmptyChunkPrefab,3000); //just picking 3000 cause that's probably more chunks than we need
        world = new World(Application.persistentDataPath + "/" + SceneData.targetWorld + "/", SceneData.targetWorld, ExplosionParticles, NetworkServer.active);

        if (NetworkServer.active)
        {
            WorldLoader wl = GetComponent<WorldLoader>();
            wl.world = world;
            UnityEngine.Debug.Log("assigned world");
            registerServerHandlers();
        }
        if (NetworkClient.active)
        {
            registerClientHandlers();
        }
        
        foreach (var go in spawnableEntities)
        {
            var entity = go.GetComponent<Entity>();
            world.entityTypes.Add(entity.type, Pool<GameObject>.createEntityPool(go, world));
            NetworkManager.singleton.spawnPrefabs.Add(go);
        }
        
        cursorInventory.items = new Item[1];
    }
    public void clientInitialize(Entity playerEntity)
    {
        WorldLoader wl = GetComponent<WorldLoader>();
        wl.world = world;
        wl.player = playerEntity;
        playerCommandExecutor = new CommandExecutor(playerEntity, world);
        AssignPlayer.Assign(playerEntity);
        playerEntity.initialize(world);
        world.loadedEntities.Add(playerEntity);
        UnityEngine.Debug.Log("client initialized");
    }
    //called on server from customnetworkmanager
    public void onPlayerConnect(Entity playerEntity, NetworkConnection conn)
    {
        world.players.Add(conn, playerEntity);
        playerEntity.initialize(world);
        world.loadedEntities.Add(playerEntity);
    }
    //called on the server when someone disconnects.
    public void onPlayerDisconnect(NetworkConnection conn)
    {
        world.players.Remove(conn);
        world.loadedEntities.Remove(conn.identity.gameObject.GetComponent<Entity>());
    }
    private void registerClientHandlers()
    {
        UnityEngine.Debug.Log("registering client hanlders");
        NetworkClient.RegisterHandler<ChunkMessage>(OnChunkRecieved);
        NetworkClient.RegisterHandler<LocalPlayerJoinMessage>(OnPlayerAssigned);
    }
    public void OnChunkRecieved(ChunkMessage message)
    {
        if (message.chunk != null)
        {
            MeshGenerator.generateAndQueue(world, world.recieveChunk(message));
        }
    }
    public void OnPlayerAssigned(LocalPlayerJoinMessage player)
    {
        UnityEngine.Debug.Log("player recieved");
        Entity playerEntity = player.gameObject.GetComponent<Entity>();
        clientInitialize(playerEntity);
    }
    private void registerServerHandlers()
    {
        UnityEngine.Debug.Log("registering server handlers");
        NetworkServer.RegisterHandler<RequestChunkMessage>(OnChunkRequested);
        NetworkServer.RegisterHandler<SetBlockMessage>(OnSetBlock);
    }
    public void OnChunkRequested(RequestChunkMessage message)
    {
        UnityEngine.Debug.Log("sending chunk: " + message.position);
        message.client.connectionToClient.Send(new ChunkMessage { chunk = world.getChunk(message.position) });
        UnityEngine.Debug.Log("chunk sent: " + message.position);
    }
    public void OnSetBlock(SetBlockMessage message)
    {
        UnityEngine.Debug.Log("setting block");
        world.setBlock(message.position, message.type, true, false);
        NetworkServer.SendToAll(new ChunkMessage { chunk = world.getChunk(world.WorldToChunkCoords(message.position)) });
    }
    public bool runGameCommand(string command)
    {
        return playerCommandExecutor.runGameCommand(command);
    }
    public void Update()
    {
        frameTimer.Restart();
    }
    public void LateUpdate()
    {
        //we divide the remaining frame time between spawning and unloading
        MeshGenerator.emptyFrameBuffer(world);
        long currTime = frameTimer.ElapsedMilliseconds;
        MeshGenerator.spawnFromQueue((targetFrameTimeMS - currTime) / 3, MinChunkLoadsPerFrame);
        currTime = frameTimer.ElapsedMilliseconds;
        world.unloadFromQueue((targetFrameTimeMS - currTime) / 3, MinChunkUnloadsPerFrame);
        currTime = frameTimer.ElapsedMilliseconds;
        if (currTime > targetFrameTimeMS)
        {
            UnityEngine.Debug.Log((currTime - targetFrameTimeMS) + " ms over time");
        }
        
    }
    [Client]
    public void SendRequestChunk(Vector3Int chunkCoords)
    {
        PlayerManager.playerIdentity.connectionToServer.Send(new RequestChunkMessage { position = chunkCoords, client = PlayerManager.playerIdentity });
    }
    [Client]
    public void SendRequestSetBlock(NetworkIdentity client, Vector3Int block, BlockType to)
    {
        PlayerManager.playerIdentity.connectionToServer.Send(new SetBlockMessage { client = client, position = block, type = to });
    }
}