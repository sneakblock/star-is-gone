using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GrassFlow {
    public class TextureCreator : MonoBehaviour {

        public static void CreateColorMap(Texture2D inTex, int width, int height, float noiseScale,
            float normalization, bool useNoise, Color color) {
            if (inTex.width != width || inTex.height != height) {
                inTex.Resize(width, height);
            }

            Color[] pixels = new Color[width * height];
            float normMult = 1f - normalization;

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    float u = x / (float)width;
                    float v = y / (float)height;

                    float pNoise = 1;
                    if (useNoise) {
                        pNoise = Mathf.PerlinNoise(u * noiseScale, v * noiseScale) * normMult + normalization;
                    }

                    pixels[width * y + x] = color * pNoise;
                }
            }

            inTex.SetPixels(pixels);
            inTex.Apply();
        }

        public static void CreateParamMap(Texture2D inTex, int width = 128, int height = 128, float heightMult = 0.1f,
            float noiseScaleDensity = 10f, float noiseScaleHeight = 50f, float noiseScaleWind = 8f,
            float normalizationDensity = 0.85f, float normalizationHeight = 0.5f, float normalizationWind = 0.8f) {
            if (inTex.width != width || inTex.height != height) {
                inTex.Resize(width, height);
            }

            Color[] pixels = new Color[width * height];
            float normMultD = 1f - normalizationDensity;
            float normMultH = 1f - normalizationHeight;
            float normMultW = 1f - normalizationWind;

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    float u = x / (float)width;
                    float v = y / (float)height;

                    float pNoiseD = Mathf.PerlinNoise(u * noiseScaleDensity + 10f, v * noiseScaleDensity + 10f) * normMultD + normalizationDensity;
                    float pNoiseH = Mathf.PerlinNoise(u * noiseScaleHeight + 20f, v * noiseScaleHeight + 20f) * normMultH + normalizationHeight;
                    float pnoiseW = Mathf.PerlinNoise(u * noiseScaleWind + 30f, v * noiseScaleHeight + 30f) * normMultW + normalizationWind;

                    pixels[width * y + x] = new Vector4(pNoiseD, pNoiseH * heightMult, 1f, pnoiseW);
                }
            }

            inTex.SetPixels(pixels);
            inTex.Apply();
        }

        public static RenderTexture GetTerrainHeightMap(Terrain terrainObj, ComputeShader heightShader, int heightKernel, bool highQuality) {
            TerrainData terrain = terrainObj.terrainData;

#if UNITY_2018_3_OR_NEWER
            //2018.3 and onward exposes the heightmap rendertexture directly so we can just grab it and its really cool B)
            return terrain.heightmapTexture;
#else
            int w = terrain.heightmapWidth - 1;
            int h = terrain.heightmapHeight - 1;

            float[,] heightData = terrain.GetHeights(0, 0, w, h);


            RenderTextureFormat rtFormat = highQuality ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.ARGBHalf;
            RenderTexture terrainHeightmap = new RenderTexture(w, h, 0, rtFormat, RenderTextureReadWrite.Linear) {
                enableRandomWrite = true, wrapMode = TextureWrapMode.Clamp
            };
            terrainHeightmap.Create();

            ComputeBuffer heightBuffer = new ComputeBuffer(heightData.Length, 4);
            heightBuffer.SetData(heightData);

            heightShader.SetInt("resolution", w);
            heightShader.SetBuffer(heightKernel, "inHeights", heightBuffer);
            heightShader.SetTexture(heightKernel, "HeightResult", terrainHeightmap);
            heightShader.Dispatch(heightKernel, Mathf.CeilToInt(w / 8f), Mathf.CeilToInt(h / 8f), 1);

            heightBuffer.Release();

            return terrainHeightmap;
#endif
        }

        public static Texture GetTerrainNormalMap(Terrain terrainObj, ComputeShader normalShader, RenderTexture heightmap, int normalKernel) {

            TerrainData terrain = terrainObj.terrainData;
            int w = terrain.heightmapResolution - 1;
            int h = terrain.heightmapResolution - 1;

            RenderTextureFormat rtFormat = RenderTextureFormat.ARGBHalf;
            RenderTexture terrainNormalMap = new RenderTexture(w, h, 0, rtFormat, RenderTextureReadWrite.Linear) {
                enableRandomWrite = true, wrapMode = TextureWrapMode.Clamp
            };
            terrainNormalMap.Create();

            normalShader.SetInt("resolution", w);
            //normalShader.SetBuffer(normalKernel, "inHeights", heightBuffer);
            normalShader.SetTexture(normalKernel, "HeightMapInput", heightmap);
            normalShader.SetTexture(normalKernel, "NormalResult", terrainNormalMap);
            normalShader.Dispatch(normalKernel, Mathf.CeilToInt(w / 8f), Mathf.CeilToInt(h / 8f), 1);

            //if(compress) {

            //    RenderTexture prevRT = RenderTexture.active;
            //    RenderTexture.active = terrainNormalMap;

            //    Texture2D compressedNormap = new Texture2D(terrainNormalMap.width, terrainNormalMap.height, TextureFormat.RGBAHalf, false, false);
            //    compressedNormap.ReadPixels(new Rect(0, 0, terrainNormalMap.width, terrainNormalMap.height), 0, 0, false);
            //    compressedNormap.Compress(true);

            //    compressedNormap.Apply();

            //    print(compressedNormap.format);
            //    print(UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(compressedNormap) * 0.00000095367431640625f);

            //    RenderTexture.active = prevRT;
            //    terrainNormalMap.Release();

            //    return compressedNormap;

            //}

            return terrainNormalMap;
        }

        public static Color[] GetTerrainHeightMapData(Terrain terrainObj) {
            TerrainData terrain = terrainObj.terrainData;

            int w = terrain.heightmapResolution;
            int h = terrain.heightmapResolution;
            float[,] heightData = terrain.GetHeights(0, 0, w, h);

            Color[] rawClrs = new Color[w * h];
            int index = 0;
            for (int x = 0; x < w; x++) {
                for (int y = 0; y < h; y++) {
                    rawClrs[index++] = new Color(heightData[x, y], 0, 0);
                }
            }

            return rawClrs;
        }

        public static Texture2D GetTerrainHeightMap(Terrain terrainObj) {
            TerrainData terrain = terrainObj.terrainData;

            int w = terrain.heightmapResolution;
            int h = terrain.heightmapResolution;


            Color[] rawClrs = GetTerrainHeightMapData(terrainObj);


            Texture2D heightMap = new Texture2D(w, h, TextureFormat.RFloat, false, true) {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };

            heightMap.SetPixels(rawClrs);

            heightMap.Apply();

            return heightMap;
        }



    } //class
} //namespace