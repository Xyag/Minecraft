using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Block
{
    public static BlockData[] blockTypes = new BlockData[]
    {
        new BlockData { type = BlockType.empty, opaque = false, fullCollision = false, raycastable = false },
        new BlockData { type = BlockType.unloadedChunk, opaque = false, fullCollision = false, raycastable = false }, //unloaded chunk should never be an actual block in a chunk. it's what the world returns if you try to get a block from an unloaded chunk.
        new BlockData { type = BlockType.stone, opaque = true, fullCollision = true, texture = new BlockTexture(0) },
        new BlockData { type = BlockType.dirt, opaque = true, fullCollision = true, texture = new BlockTexture(1) },
        new BlockData { type = BlockType.grass, opaque = true, fullCollision = true, texture = new BlockTexture(2,3,2,2,1,2) },
        new BlockData { type = BlockType.log, opaque = true, fullCollision = true, texture = new BlockTexture(4,5,4,4,5,4)},
        new BlockData { type = BlockType.ironOre, opaque = true, fullCollision = true, texture = new BlockTexture(6)},
        new BlockData { type = BlockType.leaves, opaque = true, fullCollision = true, texture = new BlockTexture(7)},
        new FallingBlockData { type = BlockType.sand, opaque = true, fullCollision = true, texture = new BlockTexture(8)},
        new TNTBlockData { type = BlockType.tnt, opaque = true, fullCollision = true, texture = new BlockTexture(9),interactable = true, explosionStrength = 8, fuseLength = 2},
        new BlockData { type = BlockType.snow, opaque = true, fullCollision = true, texture = new BlockTexture(10)},
        new BlockData { type = BlockType.cactus, opaque = true, fullCollision = true, texture = new BlockTexture(11,12,11,11,12,11)},
        new BlockData { type = BlockType.glass, opaque = false, fullCollision = true, texture = new BlockTexture(13)},
        new BlockData { type = BlockType.water, opaque = false, fullCollision = false, raycastable = false, texture = new BlockTexture(14)},
    };

    public BlockType type;
    public Block(BlockType type)
    {
        this.type = type;
    }
}

public enum BlockType : short
{
    unloadedChunk,//unloaded chunk should never be an actual block in a chunk. it's what the world returns if you try to get a block from an unloaded chunk.
    empty,
    stone,
    dirt,
    grass,
    log,
    ironOre,
    leaves,
    sand,
    tnt,
    snow,
    cactus,
    glass,
    water
}