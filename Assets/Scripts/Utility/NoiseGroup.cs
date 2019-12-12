using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class NoiseGroup
{
    private List<NoiseLayer> noises;
    public NoiseGroup(int layerCount, float baseFreq, float freqMult, float baseAmp, float ampMult, int seed = 42000)
    {
        noises = new List<NoiseLayer>(layerCount);
        for (int i = 0; i < layerCount; i++)
        {
            NoiseLayer noise = new NoiseLayer(baseFreq * (float)System.Math.Pow(freqMult, i), baseAmp * (float)System.Math.Pow(ampMult, i), seed);
            noises.Add(noise);
        }
    }

    public float sample(float x, float y, float z)
    {
        float start = 0;
        foreach (var item in noises)
        {
            start += item.get(x, y, z);
        }
        return start;
    }


    private struct NoiseLayer
    {
        private float amp;
        private FastNoise myNoise;

        public NoiseLayer(float freq, float amp, int seed)
        {
            this.amp = amp;
            myNoise = new FastNoise(seed);
            myNoise.SetFrequency(freq);
        }

        [MethodImplAttribute(256)] //aggressive inline
        public float get(float x, float y, float z)
        {
            return amp * myNoise.GetSimplex(x, y, z);
        }

    }
}