#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace GrassFlow {
    public class GrassFlowMapEditor : EditorWindow {
        GrassFlowRenderer grassFlow;

        bool generateNoisyParameters = true;

        public enum MapType { GrassColor, GrassParameters, GrassType }
        MapType mapType;

        Texture2D resultTex;

        bool saveComplete = false;
        Texture2D prevTex;

        //map values
        int mapWidth = 512;
        int mapHeight = 512;

        float noiseScale1 = 10;
        float noiseScale2 = 50;
        float noiseScale3 = 8;

        float normalization1 = 0.85f;
        float normalization2 = 0.6f;
        float normalization3 = 0.8f;

        float heightMult = 0.4f;


        private void OnEnable() {
            SetStyles();
        }



        public static void Open(GrassFlowRenderer grassController, MapType mapType) {
            GrassFlowMapEditor mapCreator = EditorWindow.GetWindow<GrassFlowMapEditor>();
            mapCreator.grassFlow = grassController;
            mapCreator.mapType = mapType;

            mapCreator.resultTex = new Texture2D(512, 512, TextureFormat.ARGB32, false, true);
            mapCreator.name = mapType.ToString();

            switch(mapType) {
                case MapType.GrassColor:
                    mapCreator.minSize = new Vector2(512, 465);
                    mapCreator.prevTex = grassController.colorMap;
                    grassController.colorMap = mapCreator.resultTex;
                    break;

                case MapType.GrassParameters:
                    mapCreator.minSize = new Vector2(512, 525);
                    mapCreator.prevTex = grassController.paramMap;
                    grassController.paramMap = mapCreator.resultTex;
                    break;

                case MapType.GrassType:
                    mapCreator.generateNoisyParameters = false;
                    mapCreator.minSize = new Vector2(512, 465);
                    mapCreator.prevTex = grassController.typeMap;
                    mapCreator.normalization1 = 0;
                    grassController.typeMap = mapCreator.resultTex;
                    break;
            }

            mapCreator.UpdateTex();

            mapCreator.titleContent = new GUIContent("GrassFlow Map Creator");
            mapCreator.ShowPopup();
        }

        private void OnGUI() {
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Create " + mapType.ToString() + " Texture", bold);

            EditorGUI.BeginChangeCheck();


            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Width");
            mapWidth = EditorGUILayout.DelayedIntField("", mapWidth, GUILayout.MinWidth(0));
            GUILayout.Space(5);
            GUILayout.Label("Height");
            mapHeight = EditorGUILayout.DelayedIntField("", mapHeight, GUILayout.MinWidth(0));
            EditorGUILayout.EndHorizontal();


            DrawNoiseVarianceGUI();


            GUILayout.Space(10);
            GUILayout.Label("Make sure your map resolution is big enough for your terrain!\n" +
                "Too small of a detail map will mean you won't be able to paint fine details.\n\n" +
                "The preview is visible live on the grass.",
                new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(0, 0, 8, 8) });
            GUILayout.Space(10);

            if(GUILayout.Button("Create")) {
                string fileName = EditorUtility.SaveFilePanelInProject("Choose Save Location", mapType.ToString(), "png", "");
                if(string.IsNullOrEmpty(fileName)) return;

                UpdateTex();
                SaveTex(fileName);

                switch(mapType) {
                    case MapType.GrassColor: grassFlow.colorMap = resultTex; break;
                    case MapType.GrassParameters: grassFlow.paramMap = resultTex; break;
                    case MapType.GrassType: grassFlow.typeMap = resultTex; break;
                }

                grassFlow.RevertDetailMaps();

                saveComplete = true;

                Close();
            }
        }

        void DrawNoiseVarianceGUI() {

            if (mapType == MapType.GrassParameters) {
                heightMult = EditorGUILayout.DelayedFloatField(new GUIContent("Height Multiplier",
                    "Controls the base for the height channel on parameter texture. This is useful for lowering base " +
                    "height so that there is room to increase grass height by painting."),
                    heightMult);
            }

            generateNoisyParameters = EditorGUILayout.ToggleLeft("Automatic Noisy Variance", generateNoisyParameters);
            if(EditorGUI.EndChangeCheck()) {
                UpdateTex();
            }            

            if (generateNoisyParameters) {
                switch(mapType) {
                    case MapType.GrassColor:
                    case MapType.GrassType:
                        DrawColorMapGUI(); break;

                    case MapType.GrassParameters: DrawParamMapGUI(); break;
                }

                GUILayout.Space(10);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUILayout.Box(new GUIContent(), GUILayout.Width(256), GUILayout.Height(256));
                EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetLastRect(), resultTex);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }


        void SaveTex(string filePath) {
            //even though the file path given isnt a full path, apparently unity knows to save it relative to the project folder
            File.WriteAllBytes(filePath, resultTex.EncodeToPNG());
            AssetDatabase.Refresh();

            TextureImporter tI = AssetImporter.GetAtPath(filePath) as TextureImporter;
            tI.sRGBTexture = false;
            tI.mipmapEnabled = false;
            tI.isReadable = true;

            if(mapType == MapType.GrassType) {
                tI.filterMode = FilterMode.Point;
                tI.textureCompression = TextureImporterCompression.Uncompressed;

                //for some reason, only 2018.1 has support for R8 textures and/or changing the single channel texture
                //also im just gonna rant about how for some reason you cant change the channel from the texture importer
                //you gotta go through the whole TextureImporterSettings thing even though it has mostly the same settings
#if UNITY_2018_1_OR_NEWER
                tI.textureType = TextureImporterType.SingleChannel;
                TextureImporterSettings tis = new TextureImporterSettings();
                tI.ReadTextureSettings(tis);
                tis.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
                tI.SetTextureSettings(tis);
#endif
            }

            EditorUtility.SetDirty(tI);
            tI.SaveAndReimport();
            resultTex = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
        }

        void DrawColorMapGUI() {
            EditorGUI.BeginChangeCheck();

            DrawNoiseParams("Color Noise: ", ref noiseScale1, ref normalization1);

            if(EditorGUI.EndChangeCheck()) {
                UpdateTex();
            }
        }

        void DrawParamMapGUI() {
            EditorGUI.BeginChangeCheck();

            DrawNoiseParams("Density Noise: ", ref noiseScale1, ref normalization1);
            DrawNoiseParams("Height Noise:  ", ref noiseScale2, ref normalization2);
            DrawNoiseParams("Wind Noise:    ", ref noiseScale3, ref normalization3);

            if(EditorGUI.EndChangeCheck()) {
                UpdateTex();
            }
        }

        void DrawNoiseParams(string label, ref float noiseScale, ref float noiseNormalization) {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, bold, GUILayout.Width(120));
            GUILayout.Label("Scale");
            noiseScale = EditorGUILayout.DelayedFloatField("", noiseScale, GUILayout.MinWidth(0));
            GUILayout.Label("Range");
            noiseNormalization = EditorGUILayout.DelayedFloatField("", noiseNormalization, GUILayout.MinWidth(0));

            EditorGUILayout.EndHorizontal();
        }

        void UpdateTex() {

            switch(mapType) {
                case MapType.GrassColor:
                    TextureCreator.CreateColorMap(resultTex, mapWidth, mapHeight, noiseScale1, normalization1,
                        generateNoisyParameters, Color.white);
                    break;

                case MapType.GrassType:
                    TextureCreator.CreateColorMap(resultTex, mapWidth, mapHeight, noiseScale1, normalization1,
                        generateNoisyParameters, generateNoisyParameters ? Color.white : Color.black);
                    break;

                case MapType.GrassParameters:
                    if (generateNoisyParameters) {
                        TextureCreator.CreateParamMap(resultTex, mapWidth, mapHeight, heightMult,
                    noiseScale1, noiseScale2, noiseScale3,
                    normalization1, normalization2, normalization3);
                    } else {
                        TextureCreator.CreateParamMap(resultTex, mapWidth, mapHeight,
                            heightMult, 0, 0, 0, 1, 1, 1);
                    }
                    
                    break;
            }

            grassFlow.RevertDetailMaps();
        }


        GUIStyle bold = new GUIStyle();
        GUIStyle center = new GUIStyle();
        GUIStyle label = new GUIStyle();

        void SetStyles() {
            bold.fontStyle = FontStyle.Bold;
            bold.fontSize = 12;
            bold.margin = new RectOffset(5, 5, 0, 0);
            bold.padding = new RectOffset(0, 0, 2, 2);

            center.alignment = TextAnchor.LowerCenter;
            center.fontStyle = FontStyle.Bold;
            center.fontSize = 12;

            label.padding = new RectOffset(4, 0, 0, 0);
            label.alignment = TextAnchor.UpperLeft;
            label.fontSize = 12;
        }


        //called when the window is closed
        private void OnDestroy() {
            if(!saveComplete) {
                switch(mapType) {
                    case MapType.GrassColor: grassFlow.colorMap = prevTex; break;
                    case MapType.GrassParameters: grassFlow.paramMap = prevTex; break;
                    case MapType.GrassType: grassFlow.typeMap = prevTex; break;
                }

                grassFlow.RevertDetailMaps();

                grassFlow.UpdateShaders();

                DestroyImmediate(resultTex);
            }
        }


    }
}

#endif