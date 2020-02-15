using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Block
{
    public static BlockData[] blockTypes = new BlockData[]
    {
        new BlockData { type = BlockType.chunk_border, opaque = false, fullCollision = false, raycastable = false },
        new BlockData { type = BlockType.empty, opaque = false, fullCollision = false, raycastable = false },
        new BlockData { type = BlockType.stone, opaque = true, fullCollision = true, texture = new BlockTexture(0) },
        new BlockData { type = BlockType.dirt, opaque = true, fullCollision = true, texture = new BlockTexture(1) },
        new BlockData { type = BlockType.grass, opaque = true, fullCollision = true, texture = new BlockTexture(2,3,2,2,1,2) },
        new BlockData { type = BlockType.log, opaque = true, fullCollision = true, texture = new BlockTexture(4,5,4,4,5,4)},
        new BlockData { type = BlockType.iron_ore, opaque = true, fullCollision = true, texture = new BlockTexture(6)},
        new BlockData { type = BlockType.leaves, opaque = true, fullCollision = true, texture = new BlockTexture(7)},
        new BlockData { type = BlockType.sand, opaque = true, fullCollision = false, texture = new BlockTexture(8)},
        new TNTBlockData { type = BlockType.tnt, opaque = true, fullCollision = true, texture = new BlockTexture(9),interactable = true, explosionStrength = 16, fuseLength = 2},
        new BlockData { type = BlockType.snow, opaque = true, fullCollision = true, texture = new BlockTexture(10)},
    };

    public const int TEXTURE_SIZE = 4;
    public BlockType type;
    public Block(BlockType type)
    {
        this.type = type;
    }
}

public enum BlockType : short
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
    tnt,
    snow
}