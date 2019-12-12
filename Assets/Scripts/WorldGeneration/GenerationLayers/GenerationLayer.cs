using System.Collections.Generic;

public interface IGenerationLayer
{
    bool isSingleThreaded();
    Chunk generateChunk(Chunk workingOn, World world);
}