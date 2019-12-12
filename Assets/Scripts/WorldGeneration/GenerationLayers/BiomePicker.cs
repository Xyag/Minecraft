using System.Collections.Generic;

public class BiomePicker
{

    private NoiseGroup biomeSelectionNoise;
    private List<float> selectionCount;
    private List<Biome> biomes;
    private float totalRarity;

    public BiomePicker(NoiseGroup biomeSelectionNoise, params Biome[] biomes)
    {
        this.biomes = new List<Biome>(biomes);
        this.biomeSelectionNoise = biomeSelectionNoise;
        selectionCount = new List<float>(biomes.Length);
        float cumCount = 0;
        for (int i = 0; i < biomes.Length; i++)
        {
            cumCount += biomes[i].rarity;
            selectionCount[i] = cumCount;
        }
        totalRarity = cumCount;
    }

    public Biome selectBiome(float x, float y)
    {
        float luckyNumber = biomeSelectionNoise.sample(x, y, 0);
        for (int i = 0; i < selectionCount.Count; i++)
        {
            if (selectionCount[i] >= luckyNumber)
            {
                return biomes[i];
            }
        }
        return biomes[biomes.Count - 1];
    }
}

public class Biome
{
    public List<IGenerationLayer> layers;
    public float rarity;
    public Biome(List<IGenerationLayer> layers, float rarity = 1)
    {
        this.layers = layers;
        this.rarity = rarity;
    }
}