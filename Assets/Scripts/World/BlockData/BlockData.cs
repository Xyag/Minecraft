using UnityEngine;

public class BlockData
{
    public BlockType type;
    public bool opaque;
    public bool fullCollision;
    public bool interactable;
    public BlockTexture texture;

    public virtual void interact(Vector3Int worldPos, Vector3Int chunkPos, Chunk chunk, World world)
    {
    }
}