using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Block
{
    public static BlockData[] blockTypes = new BlockData[]
    {
        new BlockData { type = BlockType.chunk_border, opaque = true, fullCollision = false },
        new BlockData { type = BlockType.empty, opaque = false, fullCollision = false },
        new BlockData { type = BlockType.stone, opaque = true, fullCollision = true, texture = new BlockTexture(0) },
        new BlockData { type = BlockType.dirt, opaque = true, fullCollision = true, texture = new BlockTexture(1) },
        new BlockData { type = BlockType.grass, opaque = true, fullCollision = true, texture = new BlockTexture(2) },
        new BlockData { type = BlockType.log, opaque = true, fullCollision = true, texture = new BlockTexture(3,4,3,3,4,3)},
        new BlockData { type = BlockType.iron_ore, opaque = true, fullCollision = true, texture = new BlockTexture(5)},
        new BlockData { type = BlockType.leaves, opaque = true, fullCollision = true, texture = new BlockTexture(6)},
        new BlockData { type = BlockType.sand, opaque = true, fullCollision = true, texture = new BlockTexture(7)},
        new TNTBlockData { type = BlockType.tnt, opaque = true, fullCollision = true, texture = new BlockTexture(8),interactable = true, explosionStrength = 8, fuseLength = 2},
    };

    public const int TEXTURE_SIZE = 4;
    public BlockType type;
    public Block(BlockType type)
    {
        this.type = type;
    }
}

public enum BlockType
{
    chunk_border,
    empty,
    stone,
    dirt,
    grass,
    log,
    iron_ore,
    leaves,
    sand,
    tnt
}