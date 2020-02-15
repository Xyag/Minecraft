using UnityEngine;

public class BlockData
{
    public BlockType type;
    public bool opaque = true;
    public bool fullCollision = true;
    public bool raycastable = true;
    public bool interactable = false;
    public BlockTexture texture;

    public virtual void interact(Vector3Int worldPos, Vector3Int chunkPos, Chunk chunk, World world)
    {
    }
}