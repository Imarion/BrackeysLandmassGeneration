﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public enum NormalizeMode { Local, Global };

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCentre) {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(settings.seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        float maxPossibleHeight = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + settings.offset.x + sampleCentre.x;
            float offsetY = prng.Next(-100000, 100000) - settings.offset.y - sampleCentre.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= settings.persistance;
        }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        // used to zoom in in the cente of the map instead of the top right corner
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1f;
                frequency = 1f;
                float noiseHeight = 0f;

                for (int i = 0; i < settings.octaves; i++)
                {
                    float samplex = (x - halfWidth + octaveOffsets[i].x) / settings.scale * frequency;
                    float sampley = (y - halfHeight + octaveOffsets[i].y) / settings.scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(samplex, sampley) * 2 - 1; // range of Perlinoise [0 1] -> *2-1 gives range [-1 +1]
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= settings.persistance;
                    frequency *= settings.lacunarity;                    
                }

                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }
                if (noiseHeight < minLocalNoiseHeight) {
                    minLocalNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;

                if (settings.normalizeMode == NormalizeMode.Global) { 
                    float normalizeHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f);
                    noiseMap[x, y] = Mathf.Clamp(normalizeHeight, 0, int.MaxValue);
                }
            }
        }

        if (settings.normalizeMode == NormalizeMode.Local) { 
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    // Normalize noiseMap
                     noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]); // InverseLerp returns value between 0 and 1
                }
            }
        }

        return noiseMap;
    }    
}

[System.Serializable]
public class NoiseSettings {
    public Noise.NormalizeMode normalizeMode;

    public float scale = 50f;

    public int octaves = 6;
    [Range(0, 1)]
    public float persistance = .6f;
    public float lacunarity = 2f;

    public int seed;
    public Vector2 offset;

    public void ValidateValues() {
        scale = Mathf.Max(scale, 0.01f);
        octaves = Mathf.Max(octaves, 1);
        lacunarity = Mathf.Max(lacunarity, 1);
        persistance = Mathf.Clamp01(persistance);
    }
}