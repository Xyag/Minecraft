using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Mirror;
using System;

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
    public float RequestedChunksClearInterval = 0.5f;
    private float requestedChunksTimer;

    public Inventory cursorInventory;

    private long targetFrameTimeMS;
    private Stopwatch frameTimer;
    private CommandExecutor playerCommandExecutor;
    private WorldLoader wl;
    private bool clientInit = false;
    private List<Vector3Int> requestedChunks = new List<Vector3Int>();

    private void Awake()
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
        
        foreach (var go in spawnableEntities)
        {
            var entity = go.GetComponent<Entity>();
            world.entityTypes.Add(entity.type, Pool<GameObject>.createEntityPool(go, world));
            NetworkManager.singleton.spawnPrefabs.Add(go);
        }
        clientInit = false;
        if (NetworkClient.active)
        {
            //if hosting, this isn't true yet, so we check again in start
            //however it needs to be in Awake() for normal clients to register the handlers in time.
            registerClientHandlers();
            clientInit = true;
        }
        if (NetworkServer.active)
        {
            wl = GetComponent<WorldLoader>();
            wl.world = world;
            registerServerHandlers();
        }
        
        cursorInventory.items = new Item[1];
    }
    private void Start()
    {
        if (!clientInit && NetworkClient.active)
        {
            //only runs if the player is hosting.
            registerClientHandlers();
            clientInit = true;
        }
    }
    public void clientInitialize(Entity playerEntity)
    {
        wl = GetComponent<WorldLoader>();
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
    [Client]
    private void registerClientHandlers()
    {
        UnityEngine.Debug.Log("registering client hanlders");
        requestedChunks = new List<Vector3Int>();
        NetworkClient.RegisterHandler<ChunkMessage>(OnChunkRecieved);
        NetworkClient.RegisterHandler<LocalPlayerJoinMessage>(OnPlayerAssigned);
    }
    [Client]
    public void OnChunkRecieved(ChunkMessage message)
    {
        if (message.chunk != null)
        {
            MeshGenerator.generateAndQueue(world, world.recieveChunk(message));
        }
        else if (message.willFulfill && !requestedChunks.Contains(message.chunkPos))
        {
            requestedChunks.Add(message.chunkPos);
        }
    }
    public void OnPlayerAssigned(LocalPlayerJoinMessage player)
    {
        UnityEngine.Debug.Log("player recieved");
        Entity playerEntity = player.gameObject.GetComponent<Entity>();
        clientInitialize(playerEntity);
    }
    [Server]
    private void registerServerHandlers()
    {
        UnityEngine.Debug.Log("registering server handlers");
        NetworkServer.RegisterHandler<RequestChunkMessage>(OnChunkRequested);
        NetworkServer.RegisterHandler<SetBlockMessage>(OnSetBlock);
    }
    [Server]
    public void OnChunkRequested(RequestChunkMessage message)
    {
        message.client.connectionToClient.Send(new ChunkMessage(world.getChunk(message.position), message.position, world.validChunkRequest(message.client.transform.position, message.position, wl )));
    }
    [Server]
    public void OnSetBlock(SetBlockMessage message)
    {
        if (world.validChunkRequest(message.client.transform.position, message.position, wl))
        {
            world.setBlock(message.position, message.type, true, false);
            NetworkServer.SendToAll(new ChunkMessage(world.getChunk(world.WorldToChunkCoords(message.position)), message.position, true));
        }
    }
    public bool runGameCommand(string command)
    {
        return playerCommandExecutor.runGameCommand(command);
    }
    public void Update()
    {
        frameTimer.Restart();
        if (NetworkClient.active)
        {
            requestedChunksTimer += Time.deltaTime;
            if (requestedChunksTimer >= RequestedChunksClearInterval)
            {
                if (NetworkServer.active)
                {
                    //host will already have the chunks loaded
                    foreach (var chunkPos in requestedChunks)
                    {
                            SendRequestChunk(chunkPos);
                    }
                }
                else
                {
                    foreach (var chunkPos in requestedChunks)
                    {
                        if (!world.loadedChunks.ContainsKey(chunkPos) && wl.chunkNearPlayer(world.WorldToChunkCoords(PlayerManager.singleton.Player.transform.position), chunkPos))
                        {
                            SendRequestChunk(chunkPos);
                        }
                    }
                }
                if (requestedChunks.Count > 0)
                    UnityEngine.Debug.Log("cleared request queue (" + requestedChunks.Count + ")");
                requestedChunks.Clear();
            }
        }
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