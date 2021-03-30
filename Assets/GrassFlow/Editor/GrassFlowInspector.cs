#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.IO;
using GrassFlow;

[CustomEditor(typeof(GrassFlowRenderer))]
public class GrassFlowInspector : Editor {

    GUIStyle bold = new GUIStyle();
    GUIStyle center = new GUIStyle();
    GUIStyle label = new GUIStyle();

    void SetStyles() {
        bold.fontStyle = FontStyle.Bold;
        bold.fontSize = 12;

        center.alignment = TextAnchor.LowerCenter;
        center.fontStyle = FontStyle.Bold;
        center.fontSize = 12;

        label.padding = new RectOffset(4, 0, 0, 0);
        label.alignment = TextAnchor.UpperLeft;
        label.fontSize = 12;
    }

    MaterialEditor matEditor;

    [SerializeField] static int mainTabIndex = 0;
    [SerializeField] static int selectedPaintToolIndex = 0;
    [SerializeField] static int selectedBrushIndex = 0;
    [SerializeField] static bool continuousPaint = false;
    [SerializeField] static bool useDeltaTimePaint = true;
    [SerializeField] static LayerMask paintRaycastMask = new LayerMask() { value = -1 };
    [SerializeField] static Vector2 clampRange = new Vector2(0, 1);
    [SerializeField] static bool saveBeforeStroke = false;
    [SerializeField] static bool shouldPaint = false;
    [SerializeField] static float paintBrushSize = 0.5f;
    [SerializeField] static float paintBrushStrength = 0.1f;
    [SerializeField] static Color paintBrushColor = new Color(1, 1, 1, 0);
    [SerializeField] static int grassTypeAtlasIdx = 1;
    [SerializeField] static bool useBrushOpacity = true;
    [SerializeField] static PaintToolType paintToolType = PaintToolType.Color;
    [SerializeField] static int splatMapLayerIdx = 0;
    [SerializeField] static float splatMapTolerance = 0;

    [SerializeField] static PaintHistory currentPaintHistory;
    [System.Serializable]
    class PaintUndoRedoController : ScriptableObject {
        public GrassFlowRenderer grass;
        public List<PaintHistory> paintHistory = new List<PaintHistory>();
    }
    [SerializeField] static PaintUndoRedoController paintUndoRedoController;
    static void PaintUndoRedoCallback() {
        HandlePaintUndoRedo();
    }


    const int _MaxTexAtlasSize = 16;
    const float _TexAtlasOff = 1f / 256f;

    static BrushList _BrushList;
    static BrushList brushList {
        get {
            if(_BrushList == null) { _BrushList = new BrushList(); }
            return _BrushList;
        }
    }

    static readonly HashSet<GrassFlowMapEditor.MapType> dirtyTypes = new HashSet<GrassFlowMapEditor.MapType>();


    enum PaintToolType {
        Color = 0,
        Density = 1,
        Height = 2,
        Flat = 3,
        Wind = 4,
        Type = 5,
    }


    GrassFlowRenderer _grassFlow;
    GrassFlowRenderer grassFlow {
        get {
            if(!_grassFlow) {
                _grassFlow = (GrassFlowRenderer)target;
            }
            return _grassFlow;
        }
    }

    private void OnEnable() {
        SetStyles();
        LoadInspectorSettings();

        UpdateMaterialEditorKeyword();

        if(!paintUndoRedoController || paintUndoRedoController.grass != grassFlow) {
            paintUndoRedoController = CreateInstance<PaintUndoRedoController>();
            paintUndoRedoController.grass = grassFlow;
        }

        Undo.undoRedoPerformed += UndoRedoCallback;

        Undo.undoRedoPerformed -= PaintUndoRedoCallback;
        Undo.undoRedoPerformed += PaintUndoRedoCallback;

#if UNITY_2019_1_OR_NEWER
        SceneView.duringSceneGui += SceneGUICallback;
#else
        SceneView.onSceneGUIDelegate += SceneGUICallback;
#endif
    }



    private void OnDisable() {
        SaveInspectorSettings();
        DisablePaintHighlight();

        Undo.undoRedoPerformed -= UndoRedoCallback;

#if UNITY_2019_1_OR_NEWER
        SceneView.duringSceneGui -= SceneGUICallback;
#else
        SceneView.onSceneGUIDelegate -= SceneGUICallback;
#endif

        if(grassFlow.drawMat) {
            grassFlow.drawMat.DisableKeyword("GRASS_EDITOR");
        }

        SaveData(grassFlow, prompt: true);
    }

    void UpdateMaterialEditorKeyword() {
        if(grassFlow.drawMat) {
            switch(mainTabIndex) {
                case 0:
                    grassFlow.drawMat.DisableKeyword("GRASS_EDITOR");
                    break;

                case 1:
                    grassFlow.drawMat.EnableKeyword("GRASS_EDITOR");
                    break;
            }
        }
    }

    public override void OnInspectorGUI() {
        Event e = Event.current;
        HandleHotkeys(e);

        serializedObject.Update();

#if GRASSFLOW_BURST
        EditorGUILayout.LabelField("GrassFlow is currently in Burst mode.", EditorStyles.helpBox);
#endif

#if GRASSFLOW_URP
        EditorGUILayout.LabelField("GrassFlow is in URP mode.", EditorStyles.helpBox);
#endif

        if(GUILayout.Button(new GUIContent("Refresh", "Releases/destroys all current data and resets everything. Use to reset grass after changing certain things."))) {
            ClearPaintUndoHistory();
            grassFlow.Refresh();
            return;
        }
        EditorGUILayout.Space();


        EditorGUI.BeginChangeCheck();
        int tabIndex = GUILayout.Toolbar(mainTabIndex, new string[] { "Settings", "Paint Mode" });
        if(EditorGUI.EndChangeCheck()) {
            SaveData(grassFlow, prompt: true);
            mainTabIndex = tabIndex;
            UpdateMaterialEditorKeyword();
        }

        switch(mainTabIndex) {
            case 0:
                DrawSettingsGUI();
                break;

            case 1:
                DrawPaintGUI();
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }


    public void UndoRedoCallback() {
        grassFlow.castShadows = grassFlow.castShadows;
        grassFlow.instanceCount = grassFlow.instanceCount;
        grassFlow.enableMapPainting = grassFlow.enableMapPainting;

        brushList.UpdateSelection(selectedBrushIndex);

        Repaint();
    }

    static void HandlePaintUndoRedo() {
        if(!paintUndoRedoController.grass) return;
        paintUndoRedoController.grass.RevertDetailMaps();

        int storeBrushIdx = brushList.selectedIndex;

        foreach(PaintHistory history in paintUndoRedoController.paintHistory) {
            if(!history.grass) continue;
            RenderTexture paintMap = GetPaintMap(history.paintType, history.grass);
            if(!paintMap) continue;

            brushList.UpdateSelection(history.brushIdx);
            GrassFlowRenderer.SetPaintBrushTexture(GetActiveBrushTexture());

            foreach(PaintHistory.PaintAction action in history.paintActions) {
                action.Dispatch(history, paintMap);
            }
        }

        brushList.UpdateSelection(storeBrushIdx);
        GrassFlowRenderer.SetPaintBrushTexture(GetActiveBrushTexture());
    }

    static void ClearPaintUndoHistory() {
        Undo.RecordObject(paintUndoRedoController, "GrassFlow Revert Maps");
        paintUndoRedoController.paintHistory.Clear();
    }


    void DrawSettingsGUI() {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Blade Params", bold);
        int instanceCount = EditorGUILayout.DelayedIntField(GetContent(() => grassFlow.instanceCount), grassFlow.instanceCount);
        bool updateBuffers = EditorGUILayout.ToggleLeft(GetContent(() => grassFlow.updateBuffers), grassFlow.updateBuffers);


        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grass Render Properties", bold);
        int renderLayer = EditorGUILayout.LayerField(GetContent(() => grassFlow.renderLayer), grassFlow.renderLayer);
        GrassFlowRenderer.GrassRenderType renderType = (GrassFlowRenderer.GrassRenderType)EditorGUILayout.EnumPopup(GetContent(() => grassFlow.renderType), grassFlow.renderType);
        Terrain terrainObject = grassFlow.terrainObject;
        Mesh terrainMesh = grassFlow.grassMesh;
        int meshSubdivCount = grassFlow.grassPerTri;
        GUIContent subDivContent;
        float terrainExpansion = grassFlow.terrainExpansion;
        switch(renderType) {
            case GrassFlowRenderer.GrassRenderType.Mesh:
                subDivContent = GetContent(() => grassFlow.grassPerTri);
                meshSubdivCount = EditorGUILayout.IntField(subDivContent, grassFlow.grassPerTri);
                terrainMesh = EditorGUILayout.ObjectField(GetContent(() => grassFlow.grassMesh), grassFlow.grassMesh, typeof(Mesh), true) as Mesh;
                break;

            case GrassFlowRenderer.GrassRenderType.Terrain:
                subDivContent = new GUIContent("Grass Per Chunk",
                    "Amount of grass to render per chunk in terrain mode. Technically controls the amount of grass per instance, per chunk, meaning maximum total grass per chunk = " +
                    "GrassPerChunk * InstanceCount.");
                meshSubdivCount = EditorGUILayout.IntField(subDivContent, grassFlow.grassPerTri);
                terrainObject = EditorGUILayout.ObjectField(GetContent(() => grassFlow.terrainObject), grassFlow.terrainObject, typeof(Terrain), true) as Terrain;
                terrainExpansion = EditorGUILayout.FloatField(GetContent(() => grassFlow.terrainExpansion), grassFlow.terrainExpansion);
                break;
        }
        Transform terrainTransform = EditorGUILayout.ObjectField(GetContent(() => grassFlow.terrainTransform), grassFlow.terrainTransform, typeof(Transform), true) as Transform;

        bool highQualityHeightmap = grassFlow.useTerrainNormalMap;
        //bool compressNormalMap = grassFlow.compressTerrainNormalMap;
        if(renderType == GrassFlowRenderer.GrassRenderType.Terrain) {
            highQualityHeightmap = EditorGUILayout.ToggleLeft(GetContent(() => grassFlow.useTerrainNormalMap), grassFlow.useTerrainNormalMap);

            //if(highQualityHeightmap) {
            //    compressNormalMap = EditorGUILayout.ToggleLeft(GetContent(() => grassFlow.compressTerrainNormalMap), grassFlow.compressTerrainNormalMap);
            //}
        }

        bool useIndirectInstancing = EditorGUILayout.ToggleLeft(GetContent(() => grassFlow.useIndirectInstancing), grassFlow.useIndirectInstancing);
        bool useMaterialInstance = EditorGUILayout.ToggleLeft(GetContent(() => grassFlow.useMaterialInstance), grassFlow.useMaterialInstance);
        Material grassMaterial = EditorGUILayout.ObjectField(GetContent(() => grassFlow.grassMaterial), grassFlow.grassMaterial, typeof(Material), true) as Material;

        bool receiveShadows = EditorGUILayout.ToggleLeft(GetContent(() => grassFlow.receiveShadows), grassFlow.receiveShadows);
        bool castShadows = EditorGUILayout.ToggleLeft(GetContent(() => grassFlow.castShadows), grassFlow.castShadows);



        EditorGUILayout.Space();
        EditorGUILayout.LabelField("LOD", bold);
        float maxRenderDist = EditorGUILayout.FloatField(GetContent(() => grassFlow.maxRenderDist), grassFlow.maxRenderDist);
        Vector3 lodParams = EditorGUILayout.Vector3Field(GetContent(() => grassFlow.lodParams), grassFlow.lodParams);
        Vector3 lodChunks;
        bool discardEmptyChunks = grassFlow.discardEmptyChunks;
        const string meshChunkTooltip = "Number of chunks to use for LOD culling. Distance to each chunk controls amount of grass that will be rendered there. " +
            "In MESH mode, generally you won't need more than one chunk in the Y direction but if you have incredibly vertical terrain it might be useful. Too many chunks is bad for performance, " +
            "but not enough chunks will look bad and blocky when culling grass, so set this to have as few chunks as you can while also not looking bad. (Tip: you don't need as many as you think you do.)";
        const string terrainChunkTooltip = "Number of chunks to use for LOD culling. Distance to each chunk controls amount of grass that will be rendered there. " +
            "Too many chunks is bad for performance, " +
            "but not enough chunks will look bad and blocky when culling grass, so set this to have as few chunks as you can while also not looking bad. (Tip: you don't need as many as you think you do.)";
        if(renderType == GrassFlowRenderer.GrassRenderType.Mesh) {
            lodChunks = EditorGUILayout.Vector3Field(new GUIContent("Mesh Lod Chunks", meshChunkTooltip), new Vector3(grassFlow.chunksX, grassFlow.chunksY, grassFlow.chunksZ));
        } else {
            lodChunks = EditorGUILayout.Vector2Field(new GUIContent("Terrain Lod Chunks", terrainChunkTooltip), new Vector2(grassFlow.chunksX, grassFlow.chunksZ));
            discardEmptyChunks = EditorGUILayout.ToggleLeft(GetContent(() => grassFlow.discardEmptyChunks), grassFlow.discardEmptyChunks);

        }
        bool visualizeChunkBounds = EditorGUILayout.ToggleLeft(GetContent(() => grassFlow.visualizeChunkBounds), grassFlow.visualizeChunkBounds);
        bool useManualCulling = EditorGUILayout.ToggleLeft(GetContent(() => grassFlow.useManualCulling), grassFlow.useManualCulling);


        if(EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(grassFlow, "GrassFlow Change Variable");

            grassFlow.instanceCount = instanceCount;
            grassFlow.updateBuffers = updateBuffers;

            if(grassFlow.grassMaterial != grassMaterial) {
                grassFlow.grassMaterial = grassMaterial;
                matEditor = (MaterialEditor)CreateEditor(grassFlow.grassMaterial);
                grassFlow.Refresh();
            }

            grassFlow.terrainExpansion = terrainExpansion;

            if(grassFlow.useIndirectInstancing != useIndirectInstancing) {
                grassFlow.useIndirectInstancing = useIndirectInstancing;
                grassFlow.OnEnable();
            }

            grassFlow.useMaterialInstance = useMaterialInstance;
            grassFlow.renderLayer = renderLayer;
            grassFlow.renderType = renderType;
            grassFlow.terrainTransform = terrainTransform;
            grassFlow.terrainObject = terrainObject;
            grassFlow.useTerrainNormalMap = highQualityHeightmap;
            //grassFlow.compressTerrainNormalMap = compressNormalMap;
            grassFlow.grassMesh = terrainMesh;
            grassFlow.grassPerTri = meshSubdivCount;
            grassFlow.receiveShadows = receiveShadows;
            grassFlow.castShadows = castShadows;

#if GRASSFLOW_SRP
            //lil bit gross doing this here but eh
            if(grassFlow.drawMat) {
                if(receiveShadows) {
                    grassFlow.drawMat.DisableKeyword("_RECEIVE_SHADOWS_OFF");
                } else {
                    grassFlow.drawMat.EnableKeyword("_RECEIVE_SHADOWS_OFF");
                }
                grassFlow.drawMat.SetFloat("_ReceiveShadows", receiveShadows ? 1 : 0);
            }
#endif

            grassFlow.lodParams = lodParams;
            grassFlow.maxRenderDist = maxRenderDist;
            grassFlow.visualizeChunkBounds = visualizeChunkBounds;
            grassFlow.useManualCulling = useManualCulling;
            grassFlow.discardEmptyChunks = discardEmptyChunks;

            if(renderType == GrassFlowRenderer.GrassRenderType.Mesh) {
                grassFlow.chunksX = Mathf.RoundToInt(lodChunks.x);
                grassFlow.chunksY = Mathf.RoundToInt(lodChunks.y);
                grassFlow.chunksZ = Mathf.RoundToInt(lodChunks.z);
            } else {
                grassFlow.chunksX = Mathf.RoundToInt(lodChunks.x);
                grassFlow.chunksZ = Mathf.RoundToInt(lodChunks.y);
            }

            Undo.FlushUndoRecordObjects();
        }

        DrawMapsInspector();

        EditorGUILayout.Space();

        if(grassFlow.grassMaterial) {
            if(matEditor == null)
                matEditor = (MaterialEditor)CreateEditor(grassFlow.grassMaterial);

            matEditor.DrawHeader();
            matEditor.OnInspectorGUI();
        }
    }


    void DrawMapsInspector() {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Detail Maps", bold);

        if(addMapIcon == null) {
            _toolIcons = GetToolIcons();
        }

        EditorGUI.BeginChangeCheck();

        bool createButtonPushed = false;

        Texture2D colorMap = DrawMapField(grassFlow.colorMap, GetContent(() => grassFlow.colorMap), GrassFlowMapEditor.MapType.GrassColor, ref createButtonPushed);
        Texture2D paramMap = DrawMapField(grassFlow.paramMap, GetContent(() => grassFlow.paramMap), GrassFlowMapEditor.MapType.GrassParameters, ref createButtonPushed);
        Texture2D typeMap = DrawMapField(grassFlow.typeMap, GetContent(() => grassFlow.typeMap), GrassFlowMapEditor.MapType.GrassType, ref createButtonPushed);

        if(EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(grassFlow, "GrassFlow Set Detail Map");

            if (!createButtonPushed) {
                grassFlow.colorMap = colorMap;
                grassFlow.paramMap = paramMap;
                grassFlow.typeMap = typeMap;
            }

            if(grassFlow.drawMat) {
                if(!colorMap) {
                    grassFlow.drawMat.SetTexture("colorMap", null);
                }
                if(!paramMap) {
                    grassFlow.drawMat.SetTexture("dhfParamMap", null);
                }
                if(!typeMap) {
                    grassFlow.drawMat.SetTexture("typeMap", null);
                }
            }

            grassFlow.RevertDetailMaps();
            grassFlow.UpdateShaders();

            Undo.FlushUndoRecordObjects();
        }
    }

    Texture2D DrawMapField(Texture2D srcMap, GUIContent content, GrassFlowMapEditor.MapType mapType, ref bool creatBtn) {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(content, GUILayout.Width(100));
        if(GUILayout.Button(addMapIcon, new GUIStyle(EditorStyles.miniButton) { fontSize = 13, padding = new RectOffset() }, GUILayout.Width(16), GUILayout.Height(16))) {
            creatBtn = true;
            SaveData(grassFlow, prompt: true);
            GrassFlowMapEditor.Open(grassFlow, mapType);
        }
        Texture2D map = EditorGUILayout.ObjectField(srcMap, typeof(Texture2D), true) as Texture2D;
        EditorGUILayout.EndHorizontal();

        return map;
    }

    void DrawPaintGUI() {
        DrawMapsInspector();

        EditorGUI.BeginChangeCheck();

        bool tMapPaintingEnabled = EditorGUILayout.ToggleLeft(GetContent(() => grassFlow.enableMapPainting), grassFlow.enableMapPainting);

        if(EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(grassFlow, "GrassFlow Change Variable");

            grassFlow.enableMapPainting = tMapPaintingEnabled;

            Undo.FlushUndoRecordObjects();
        }

        if(!grassFlow.enableMapPainting) return;

        EditorGUI.BeginChangeCheck();


        bool _saveBeforeStroke = EditorGUILayout.ToggleLeft(new GUIContent("Save On Stroke", "Saves the maps before each stroke so that the stroke can be reverted to the save before the stroke."), saveBeforeStroke);

        if(EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(this, "GrassFlow Change Variable");

            saveBeforeStroke = _saveBeforeStroke;

            Undo.FlushUndoRecordObjects();
        }


        if(GUILayout.Button(new GUIContent("Revert Changes", "Discards changes to detail maps since they were last saved. " +
            "The maps are saved whenever the project assets are saved e.g. on Ctrl+S. Revert hotkey: Shift-R. " +
            "This action \"should\" have undo/redo support, it probably works."))) {
            RevertDetailMaps(grassFlow);
        }

        if(grassFlow.renderType == GrassFlowRenderer.GrassRenderType.Mesh) {
            GUILayout.Space(12);

            if(GUILayout.Button(new GUIContent("Bake Density to Mesh", "Creates a new mesh based on the density information in the parameter map. " +
                "You can use this mesh to more efficiently only render grass on certain parts of your mesh. Does NOT automatically apply the resulting mesh."))) {

                string fileName = EditorUtility.SaveFilePanelInProject("Choose Save Location", "GrassflowDensityMesh", "asset", "");
                if(string.IsNullOrEmpty(fileName)) return;

                SaveData(grassFlow, prompt: true);
                Mesh bakedMesh = MeshChunker.BakeDensityToMesh(grassFlow.grassMesh, grassFlow.paramMap);

                AssetDatabase.CreateAsset(bakedMesh, fileName);
                AssetDatabase.SaveAssets();
            }
        }


        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", new GUIStyle(GUI.skin.horizontalScrollbarThumb) { fixedHeight = 2 }, GUILayout.Height(3));
        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUI.BeginChangeCheck();
        int newToolIndex = GUILayout.Toolbar(selectedPaintToolIndex, toolIcons, GUILayout.Height(iconSize - 15), GUILayout.Width(iconSize * toolIcons.Length));
        if(EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(this, "GrassFlow Change Paint Tool");
            selectedPaintToolIndex = newToolIndex;
            paintToolType = (PaintToolType)selectedPaintToolIndex;
            Undo.FlushUndoRecordObjects();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label(toolInfos[selectedPaintToolIndex].text);
        GUILayout.Label(toolInfos[selectedPaintToolIndex].tooltip, EditorStyles.wordWrappedMiniLabel);
        GUILayout.EndVertical();

        if(brushList.ShowGUI()) {
            Undo.RecordObject(this, "GrassFlow Change Brush");
            selectedBrushIndex = brushList.selectedIndex;
            GrassFlowRenderer.SetPaintBrushTexture(GetActiveBrushTexture());
            Undo.FlushUndoRecordObjects();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Settings", bold);

        Undo.RecordObject(this, "GrassFlow Change Variable");

        if(paintToolType == PaintToolType.Color) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Brush Color", GUILayout.Width(100));
            paintBrushColor = EditorGUILayout.ColorField(new GUIContent(), paintBrushColor, true, true, true, new ColorPickerHDRConfig(0, 10, 0, 10));
            EditorGUILayout.EndHorizontal();
        } else if(paintToolType == PaintToolType.Type) {

            useBrushOpacity = EditorGUILayout.ToggleLeft(new GUIContent("Use Brush Opacity",
                "Whether or not to use the brush opacity when painting. When painting grass type at full strength, " +
                "turning this off can be ideal to avoid artifacts where brush opacity affects density undesirably."), useBrushOpacity, GUILayout.Width(125));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Grass Type Index",
                "Index into the grass texture atlas. For selecting which texture to paint."), GUILayout.Width(125));
            grassTypeAtlasIdx = EditorGUILayout.IntSlider(grassTypeAtlasIdx, 1, _MaxTexAtlasSize);
            EditorGUILayout.EndHorizontal();

        } else {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Clamp Range",
                "Min and max range for parameters while painting. This can be used to essentially paint a set value instead of being additive or subtractive."),
                GUILayout.Width(100));
            clampRange = EditorGUILayout.Vector2Field("", clampRange, GUILayout.Width(100));
            GUILayout.Space(5);
            EditorGUILayout.MinMaxSlider("", ref clampRange.x, ref clampRange.y, 0, 1, GUILayout.MinWidth(20));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Brush Size", GUILayout.Width(100));
        paintBrushSize = GUILayout.HorizontalSlider(paintBrushSize, 0f, 1f);
        GUILayout.Space(5);
        paintBrushSize = EditorGUILayout.FloatField("", paintBrushSize, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Brush Strength", GUILayout.Width(100));
        paintBrushStrength = GUILayout.HorizontalSlider(paintBrushStrength, 0f, 1f);
        GUILayout.Space(5);
        paintBrushStrength = EditorGUILayout.FloatField("", paintBrushStrength, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        LayerMask rayCastMask = EditorGUILayout.MaskField(new GUIContent("Raycast Layer Mask", "This mask is used when raycasting the terrain/mesh for painting. " +
            "You can use this to only paint on the layer your terrain is on and paint through blocking objects, or vice versa."),
            InternalEditorUtility.LayerMaskToConcatenatedLayersMask(paintRaycastMask), InternalEditorUtility.layers);
        paintRaycastMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(rayCastMask);

        continuousPaint = EditorGUILayout.ToggleLeft(new GUIContent("Paint Continuously",
            "If off the mouse needs to be moved to paint, otherwise it will paint continuously while the mouse is down."), continuousPaint);

        useDeltaTimePaint = EditorGUILayout.ToggleLeft(new GUIContent("Use Delta Time Paint",
            "If on the brush strength is multiplied by delta time to make painting strength framerate independent. " +
            "It's useful to turn this off if you want to use brushes more like stamps and use strength of 1 and apply the full brush to the grass with a single click."), useDeltaTimePaint);


        if(grassFlow.renderType == GrassFlowRenderer.GrassRenderType.Terrain) {
            DrawSplatMapGUI();
        }
    }

    void DrawSplatMapGUI() {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", new GUIStyle(GUI.skin.horizontalScrollbarThumb) { fixedHeight = 2 }, GUILayout.Height(3));
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Splat Maps", bold);

        if(grassFlow.terrainObject) {
            int numSplatLayers = grassFlow.terrainObject.terrainData.alphamapLayers;

            if(numSplatLayers > 0) {
                EditorGUI.BeginChangeCheck();

                int _splatMapLayerIdx = Mathf.Clamp(splatMapLayerIdx, 0, numSplatLayers);
                float _splatMapTolerance = splatMapTolerance;

                int[] splatInts = new int[numSplatLayers];
                GUIContent[] splatStrs = new GUIContent[numSplatLayers];

                for(int i = 0; i < splatInts.Length; i++) {
                    splatInts[i] = i;
                    var splat = grassFlow.terrainObject.terrainData.splatPrototypes[i];
                    string splatName = splat.texture ? splat.texture.name : "Null";
                    splatStrs[i] = new GUIContent((i + 1).ToString() + " : " + splatName);
                }

                _splatMapLayerIdx = EditorGUILayout.IntPopup(new GUIContent("Splat Layer",
                    "The index of the splat texture layer you want to use to mask where grass appears."),
                    _splatMapLayerIdx, splatStrs, splatInts);

                _splatMapTolerance = EditorGUILayout.Slider(new GUIContent("Tolerance",
                    "Controls opacity tolerance when applying splat map layers."), splatMapTolerance, 0f, 1f);

                if(EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(this, "GrassFlow Change Paint Tool");

                    splatMapLayerIdx = _splatMapLayerIdx;
                    splatMapTolerance = _splatMapTolerance;

                    Undo.FlushUndoRecordObjects();
                }


                EditorGUILayout.BeginHorizontal();
                if(GUILayout.Button(new GUIContent("Apply Additive", "Adds grass based on the selected layer, but does not remove any existing grass."))) {
                    grassFlow.ApplySplatTex(grassFlow.terrainObject, splatMapLayerIdx, 0, splatMapTolerance);
                    SetParametersDirty();
                }

                if(GUILayout.Button(new GUIContent("Apply Subtractive", "Removes grass based on the selected layer, but does not affect grass outside of the splat map."))) {
                    grassFlow.ApplySplatTex(grassFlow.terrainObject, splatMapLayerIdx, 1, 1f - splatMapTolerance);
                    SetParametersDirty();
                }

                if(GUILayout.Button(new GUIContent("Apply Replace", "Adds grass based on the selected layer, removing and overwriting existing grass."))) {
                    grassFlow.ApplySplatTex(grassFlow.terrainObject, splatMapLayerIdx, 2, splatMapTolerance);
                    SetParametersDirty();
                }
                EditorGUILayout.EndHorizontal();

            } else {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("No splat layers on the terrain.");
                GUILayout.EndVertical();
            }

            //grassFlow.terrainObject.terrainData.alphamapTextures[]
        } else {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Please assign terrain object in settings.");
            GUILayout.EndVertical();
        }
    }


    const int iconSize = 40;
    GUIContent addMapIcon;
    GUIContent[] _toolIcons;
    GUIContent[] toolIcons { get { return _toolIcons == null ? (_toolIcons = GetToolIcons()) : _toolIcons; } }
    GUIContent[] GetToolIcons() {
        List<Texture2D> iconTextures = AssetDatabase.FindAssets("t:Texture", new string[] { "Assets/GrassFlow/Editor/InspectorIcons" })
                    .Select(p => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(p), typeof(Texture2D)) as Texture2D).Where(b => b != null).ToList();

        addMapIcon = new GUIContent(iconTextures.Find(x => x.name == "mapAdd"));

        return new GUIContent[] {
            new GUIContent(iconTextures.Find(x => x.name == "paintClr"), "Color"),
            new GUIContent(iconTextures.Find(x => x.name == "paintDensity"), "Density"),
            new GUIContent(iconTextures.Find(x => x.name == "paintHeight"), "Height"),
            new GUIContent(iconTextures.Find(x => x.name == "paintFlat"), "Flatness"),
            new GUIContent(iconTextures.Find(x => x.name == "paintWind"), "Wind Strength"),
            new GUIContent(iconTextures.Find(x => x.name == "paintType"), "Grass Type"),
        };
    }

    public readonly GUIContent[] toolInfos = new GUIContent[] {
                new GUIContent("Paint Grass Color", "Click to paint color. Simple."),
                new GUIContent("Paint Grass Density", "Click to fill in grass. Shift+Click to erase grass."),
                new GUIContent("Paint Grass Height", "Click to raise grass. Shift+Click to lower grass."),
                new GUIContent("Paint Grass Flatness", "Click to flatten grass. Shift+Click to unflatten grass."),
                new GUIContent("Paint Grass Wind Strength", "Click to increase wind strength. Shift+Click to decrease."),
                new GUIContent("Paint Grass Type", "Click to paint which texture from the grass texture atlas (if using one) is shown. " +
                    "Shift+Click to paint first texture. Brush strength controls density of selected type."),
    };


    static object[] paramsArr = new object[1];
    static System.Action paintAction;
    static MethodInfo _SetGrassPaintAction_Method;
    static MethodInfo SetGrassPaintAction_Method {
        get {
            if(_SetGrassPaintAction_Method == null) {
                _SetGrassPaintAction_Method = typeof(GrassFlowRenderer).GetMethod("InspectorSetPaintAction", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            return _SetGrassPaintAction_Method;
        }
    }

    void SceneGUICallback(SceneView sceneView) {
        Event e = Event.current;

        HandleHotkeys(e);

        if(grassFlow && grassFlow.enableMapPainting && mainTabIndex == 1) {

            RaycastHit hit;
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            Physics.Raycast(ray, out hit, float.PositiveInfinity, paintRaycastMask);

            bool hitTerrain = hit.transform == grassFlow.terrainTransform;


            if(e.type == EventType.MouseDown || e.type == EventType.MouseUp) {
                if(e.button == 0 && !e.alt) {
                    Selection.activeObject = grassFlow;
                }
            }

            if(!hitTerrain) {
                DisablePaintHighlight();
                if(!shouldPaint) return;
            }

            if(grassFlow.drawMat) {
                grassFlow.drawMat.SetTexture("paintHighlightBrushTex", GetActiveBrushTexture());
                grassFlow.drawMat.SetColor("paintHightlightColor", paintBrushColor);

                if(shouldPaint) {
                    if(paintToolType == PaintToolType.Color) {
                        grassFlow.drawMat.SetVector("paintHighlightBrushParams", Vector4.zero);
                    } else {
                        grassFlow.drawMat.SetVector("paintHighlightBrushParams", new Vector4(hit.textureCoord.x, hit.textureCoord.y, paintBrushSize * 0.05f, 0.5f));
                    }
                } else {
                    grassFlow.drawMat.SetVector("paintHighlightBrushParams", new Vector4(hit.textureCoord.x, hit.textureCoord.y, paintBrushSize * 0.05f, 1f));
                }
            }

            int id = GUIUtility.GetControlID(grassEditorHash, FocusType.Passive);
            float brushDir = e.shift ? -1f : 1f;


            switch(e.GetTypeForControl(id)) {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(id);

                    if(continuousPaint && shouldPaint) {
                        //this is really silly but i guess theres a bug with getting a temporary rendertexture during scene gui
                        //not doing this causes the scene gui to do weird things when the painting function asks for a temp RT
                        //weird things like: rendering the scene gui onto the grass O_O
                        //or rendering the gizmo where the shading mode selector is
                        //or blacking out the the entire scene view border frame
                        //this is only needed for repaint/layout events, thus needed for continous paint mode
                        //it doesnt matter on mouse drag events since that isnt during gui drawing stuff
                        paramsArr[0] = paintAction = new System.Action(() => {
                            PaintSwitch(hit.textureCoord, brushDir);
                        });
                        SetGrassPaintAction_Method.Invoke(grassFlow, paramsArr);
                        EditorUtility.SetDirty(grassFlow);
                    }
                    break;

                case EventType.MouseMove:
                    HandleUtility.Repaint();
                    break;

                case EventType.MouseDown:
                case EventType.MouseDrag: {
                        // Don't do anything at all if someone else owns the hotControl. Fixes case 677541.
                        if(EditorGUIUtility.hotControl != 0 && EditorGUIUtility.hotControl != id)
                            return;

                        // Don't do anything on MouseDrag if we don't own the hotControl.
                        if(e.GetTypeForControl(id) == EventType.MouseDrag && EditorGUIUtility.hotControl != id)
                            return;

                        // If user is ALT-dragging, we want to return to main routine
                        if(Event.current.alt)
                            return;

                        // Allow painting with LMB only
                        if(e.button != 0)
                            return;

                        if(HandleUtility.nearestControl != id)
                            return;

                        if(e.type == EventType.MouseDown) {
                            EditorGUIUtility.hotControl = id;
                            shouldPaint = true;
                            lastPaintTime = Time.realtimeSinceStartup - 0.016f;
                            if(saveBeforeStroke) {
                                SaveData(grassFlow, false);
                            }
                            GrassFlowRenderer.SetPaintBrushTexture(GetActiveBrushTexture());
                            CheckPaintTextureExists(GetPaintMap(paintToolType, grassFlow));
                        }


                        if(!continuousPaint) {
                            PaintSwitch(hit.textureCoord, brushDir);
                        }

                        e.Use();
                    }
                    break;

                case EventType.MouseUp: {

                        if(GUIUtility.hotControl != id) {
                            return;
                        }

                        shouldPaint = false;
                        MarkDirtyMaps();
                        if(saveBeforeStroke) {
                            AssetDatabase.Refresh();
                        }

                        if(currentPaintHistory) {
                            Undo.RecordObject(paintUndoRedoController, "GrassFlow Paint");
                            paintUndoRedoController.paintHistory.Add(currentPaintHistory);
                            currentPaintHistory = null;

                            paramsArr[0] = paintAction = null;
                            SetGrassPaintAction_Method.Invoke(grassFlow, paramsArr);
                        }

                        // Release hot control
                        GUIUtility.hotControl = 0;
                    }
                    break;
            }
        } else {
            DisablePaintHighlight();
        }
    }

    void MarkDirtyMaps() {
        switch(paintToolType) {
            case PaintToolType.Color:
                SetColorMapDirty();
                break;

            case PaintToolType.Density:
            case PaintToolType.Height:
            case PaintToolType.Flat:
            case PaintToolType.Wind:
                SetParametersDirty();
                break;

            case PaintToolType.Type:
                SetTypeMapDirty();
                break;
        }
    }

    static RenderTexture GetPaintMap(PaintToolType type, GrassFlowRenderer grass) {
        switch(type) {
            case PaintToolType.Color: return grass.colorMapRT;

            case PaintToolType.Density:
            case PaintToolType.Height:
            case PaintToolType.Flat:
            case PaintToolType.Wind:
                return grass.paramMapRT;

            case PaintToolType.Type: return grass.typeMapRT;
        }

        return null;
    }

    void SetColorMapDirty() {
        dirtyTypes.Add(GrassFlowMapEditor.MapType.GrassColor);
    }

    void SetParametersDirty() {
        dirtyTypes.Add(GrassFlowMapEditor.MapType.GrassParameters);
    }

    void SetTypeMapDirty() {
        dirtyTypes.Add(GrassFlowMapEditor.MapType.GrassType);
    }

    void DisablePaintHighlight() {
        if(grassFlow.drawMat) grassFlow.drawMat.SetVector("paintHighlightBrushParams", Vector4.zero);
    }

    static int grassEditorHash = "GrassFlowEditor".GetHashCode();

    void PaintSwitch(Vector2 textureCoord, float brushDir) {
        switch(paintToolType) {
            case PaintToolType.Color: //paint color
                PaintTerrain(textureCoord, paintBrushColor, grassFlow.colorMapRT, new Vector2(-999f, 999f), paintBrushStrength, 0f);
                break;

            case PaintToolType.Density: //paint density
                PaintTerrain(textureCoord, new Vector4(brushDir, 0, 0), grassFlow.paramMapRT, clampRange, paintBrushStrength, 1f);
                break;

            case PaintToolType.Height: //paint height
                PaintTerrain(textureCoord, new Vector4(0, brushDir, 0), grassFlow.paramMapRT, clampRange, paintBrushStrength, 1f);
                break;

            case PaintToolType.Flat: //paint flatness
                PaintTerrain(textureCoord, new Vector4(0, 0, -brushDir), grassFlow.paramMapRT, clampRange, paintBrushStrength, 1f);
                break;

            case PaintToolType.Wind: //paint wind affectedness
                PaintTerrain(textureCoord, new Vector4(0, 0, 0, brushDir), grassFlow.paramMapRT, clampRange, paintBrushStrength, 1f);
                break;

            case PaintToolType.Type:
                int atlasIdx = grassTypeAtlasIdx;
                if(brushDir == -1) {
                    atlasIdx = 1;
                }
                float paintIdx = (atlasIdx - 1) / (float)_MaxTexAtlasSize;
                float paintPct = paintIdx + (1f / _MaxTexAtlasSize - _TexAtlasOff) * paintBrushStrength;
                float lowerBound = useBrushOpacity ? paintIdx : paintPct;
                //Debug.Log(paintIdx + " : " + (paintPct));
                //PaintTerrain(textureCoord, new Vector4(paintIdx, 0, 0), grassFlow.typeMapRT, new Vector2(paintIdx, paintIdx), 1f, 0f);
                PaintTerrain(textureCoord, new Vector4(paintPct, 0, 0), grassFlow.typeMapRT, new Vector2(lowerBound, paintPct), 1f, 0f);
                break;
        }
    }

    float lastPaintTime = 0;
    void PaintTerrain(Vector2 texCoord, Vector4 blendParams, RenderTexture mapRT, Vector2 _clampRange, float strength, float blendMode) {
        if(paintToolType != PaintToolType.Type) {
            strength = useDeltaTimePaint ? strength * (Time.realtimeSinceStartup - lastPaintTime) : strength;
        }

        if(!currentPaintHistory) {

            currentPaintHistory = new PaintHistory() {
                grass = grassFlow,
                brushSize = paintBrushSize,
                blendParams = blendParams,
                paintType = paintToolType,
                _clampRange = _clampRange,
                blendMode = blendMode,
                brushIdx = selectedBrushIndex
            };
        }

        PaintHistory.PaintAction paintAct = new PaintHistory.PaintAction() {
            texCoord = texCoord,
            strength = strength
        };
        paintAct.Dispatch(currentPaintHistory, mapRT);
        currentPaintHistory.paintActions.Add(paintAct);

        lastPaintTime = Time.realtimeSinceStartup;
    }

    void CheckPaintTextureExists(RenderTexture tex) {
        if(!tex) {
            Debug.LogError("GrassFlow: Texture for selected paint mode not set.");
        }
    }

    [System.Serializable]
    class PaintHistory {

        public List<PaintAction> paintActions = new List<PaintAction>();

        public GrassFlowRenderer grass;

        public Vector4 blendParams;
        public Vector2 _clampRange;
        public PaintToolType paintType;
        public float blendMode;
        public float brushSize;
        public int brushIdx;

        [System.Serializable]
        public class PaintAction {
            public Vector2 texCoord;
            public float strength;

            public void Dispatch(PaintHistory history, RenderTexture mapRT) {
                GrassFlowRenderer.PaintDispatch(texCoord, history.brushSize, strength, history.blendParams,
                    mapRT, history._clampRange, history.blendMode);
            }
        }

        public static implicit operator bool(PaintHistory h) { return h != null; }
    }

    //-------------------------------------------------------------
    //------------------utilityyy stuff------------------------
    //-------------------------------------------------------------

    void LoadInspectorSettings() {
        paintBrushSize = EditorPrefs.GetFloat("GrassFlowBrushSize", paintBrushSize);
        paintBrushStrength = EditorPrefs.GetFloat("GrassFlowBrushStrength", paintBrushStrength);

        paintBrushColor.a = EditorPrefs.GetFloat("GrassFlowBrushColorA", paintBrushColor.a);
        paintBrushColor.r = EditorPrefs.GetFloat("GrassFlowBrushColorR", paintBrushColor.r);
        paintBrushColor.g = EditorPrefs.GetFloat("GrassFlowBrushColorG", paintBrushColor.g);
        paintBrushColor.b = EditorPrefs.GetFloat("GrassFlowBrushColorB", paintBrushColor.b);

        useBrushOpacity = EditorPrefs.GetBool("GrassFlowUseBrushOpacity", useBrushOpacity);
        grassTypeAtlasIdx = EditorPrefs.GetInt("GrassFlowGrassTypeAtlasIdx", grassTypeAtlasIdx);

        mainTabIndex = EditorPrefs.GetInt("GrassFlowMainTab", mainTabIndex);
        selectedPaintToolIndex = EditorPrefs.GetInt("GrassFlowPaintToolIndex", selectedPaintToolIndex);
        brushList.UpdateSelection(EditorPrefs.GetInt("GrassFlowSelectedBrush", 0));
        paintToolType = (PaintToolType)selectedPaintToolIndex;

        paintRaycastMask = EditorPrefs.GetInt("GrassFlowPaintRaycastMask", paintRaycastMask);

        continuousPaint = EditorPrefs.GetBool("GrassFlowContinuousPaint", continuousPaint);
        useDeltaTimePaint = EditorPrefs.GetBool("GrassFlowDeltaPaint", useDeltaTimePaint);
        saveBeforeStroke = EditorPrefs.GetBool("GrassFlowSaveBeforStroke", saveBeforeStroke);

        clampRange.x = EditorPrefs.GetFloat("GrassFlowClampMin", clampRange.x);
        clampRange.y = EditorPrefs.GetFloat("GrassFlowClampMax", clampRange.y);

        selectedBrushIndex = brushList.selectedIndex;

        splatMapLayerIdx = EditorPrefs.GetInt("GrassFlowSplatMapLayerIdx", splatMapLayerIdx);
        splatMapTolerance = EditorPrefs.GetFloat("GrassFlowSplatMapTolerance", splatMapTolerance);
    }

    void SaveInspectorSettings() {
        EditorPrefs.SetFloat("GrassFlowBrushSize", paintBrushSize);
        EditorPrefs.SetFloat("GrassFlowBrushStrength", paintBrushStrength);

        EditorPrefs.SetFloat("GrassFlowBrushColorA", paintBrushColor.a);
        EditorPrefs.SetFloat("GrassFlowBrushColorR", paintBrushColor.r);
        EditorPrefs.SetFloat("GrassFlowBrushColorG", paintBrushColor.g);
        EditorPrefs.SetFloat("GrassFlowBrushColorB", paintBrushColor.b);

        EditorPrefs.SetBool("GrassFlowUseBrushOpacity", useBrushOpacity);
        EditorPrefs.SetInt("GrassFlowGrassTypeAtlasIdx", grassTypeAtlasIdx);

        EditorPrefs.SetInt("GrassFlowMainTab", mainTabIndex);
        EditorPrefs.SetInt("GrassFlowPaintToolIndex", selectedPaintToolIndex);
        EditorPrefs.SetInt("GrassFlowSelectedBrush", brushList.selectedIndex);

        EditorPrefs.SetInt("GrassFlowPaintRaycastMask", paintRaycastMask);

        EditorPrefs.SetBool("GrassFlowContinuousPaint", continuousPaint);
        EditorPrefs.SetBool("GrassFlowDeltaPaint", useDeltaTimePaint);
        EditorPrefs.SetBool("GrassFlowSaveBeforStroke", saveBeforeStroke);

        EditorPrefs.SetFloat("GrassFlowClampMin", clampRange.x);
        EditorPrefs.SetFloat("GrassFlowClampMax", clampRange.y);

        EditorPrefs.SetInt("GrassFlowSplatMapLayerIdx", splatMapLayerIdx);
        EditorPrefs.SetFloat("GrassFlowSplatMapTolerance", splatMapTolerance);
    }


    static bool SavePaintTexture(GrassFlowMapEditor.MapType mapType, Texture2D original, RenderTexture newTex) {
        if(!original || !newTex) return false;

        string savePath = AssetDatabase.GetAssetPath(original);

        if(string.IsNullOrEmpty(savePath)) {
            Debug.LogError("Cant save texture map! Probably because it has no file.");
            return false;
        }

        if(Path.GetExtension(savePath).ToLower() != ".png") {
            Debug.LogError("Detail maps need to be .png format!");
            return false;
        }

        savePath = Path.GetFullPath(Application.dataPath + "/../" + savePath);

        RenderTexture oldRT = RenderTexture.active;
        Texture2D saveTex = new Texture2D(newTex.width, newTex.height, TextureFormat.ARGB32, false, true);

        if(mapType == GrassFlowMapEditor.MapType.GrassType) {
            //this is required for older versions of unity that for one reason or another do not properly
            //read pixels from an R8 rendertexture
            RenderTexture tmp = RenderTexture.GetTemporary(newTex.width, newTex.height, 0);
            if(!tmp.IsCreated()) tmp.Create();
            bool srgb = GL.sRGBWrite;
            GL.sRGBWrite = false;
            Graphics.Blit(newTex, tmp);
            GL.sRGBWrite = srgb;
            newTex = tmp;
        }

        RenderTexture.active = newTex;
        saveTex.ReadPixels(new Rect(0, 0, saveTex.width, saveTex.height), 0, 0, false);
        saveTex.Apply();

        if(mapType == GrassFlowMapEditor.MapType.GrassType) {
            RenderTexture.ReleaseTemporary(newTex);
        }

        RenderTexture.active = oldRT;

        File.WriteAllBytes(savePath, saveTex.EncodeToPNG());
        DestroyImmediate(saveTex);

        return true;
    }


    static void SaveData(GrassFlowRenderer grass, bool refresh = true, bool prompt = false) {
        if(grass.enableMapPainting && mainTabIndex == 1) {
            bool shouldRefresh = false;

            if(prompt && dirtyTypes.Count > 0) {
                if(!EditorUtility.DisplayDialog("GrassFlow", "GrassFlow detail map(s) have been modified.\nSave changes?", "Yes", "No")) {
                    dirtyTypes.Clear();
                    grass.RevertDetailMaps();
                }
            }

            foreach(GrassFlowMapEditor.MapType mapType in dirtyTypes) {
                shouldRefresh |= SaveMapSwitch(grass, mapType);
            }

            if(refresh && shouldRefresh) AssetDatabase.Refresh();

            if(paintUndoRedoController) {
                Undo.ClearUndo(paintUndoRedoController);
            }
            paintUndoRedoController = CreateInstance<PaintUndoRedoController>();
            paintUndoRedoController.grass = grass;


            dirtyTypes.Clear();
        }

        grass.UpdateShaders();
    }

    static bool SaveMapSwitch(GrassFlowRenderer grass, GrassFlowMapEditor.MapType mapType) {
        switch(mapType) {
            case GrassFlowMapEditor.MapType.GrassColor: return SavePaintTexture(mapType, grass.colorMap, grass.colorMapRT);
            case GrassFlowMapEditor.MapType.GrassParameters: return SavePaintTexture(mapType, grass.paramMap, grass.paramMapRT);
            case GrassFlowMapEditor.MapType.GrassType: return SavePaintTexture(mapType, grass.typeMap, grass.typeMapRT);

            default: return false;
        }
    }

    void HandleHotkeys(Event e) {
        if(e.type != EventType.KeyDown) {
            return;
        }


        switch(e.keyCode) {
            case KeyCode.R:
                if(!e.control && !e.alt && e.shift) {
                    RevertDetailMaps(grassFlow);
                }
                break;
        }
    }

    static void RevertDetailMaps(GrassFlowRenderer grass) {
        if(!grass) return;
        ClearPaintUndoHistory();
        grass.RevertDetailMaps();
    }

    private class SaveProcessor : UnityEditor.AssetModificationProcessor {
        static string[] OnWillSaveAssets(string[] paths) {
            GrassFlowRenderer[] grasses = FindObjectsOfType<GrassFlowRenderer>();
            foreach(GrassFlowRenderer grass in grasses) {
                SaveData(grass);
            }

            return paths;
        }
    }

    GUIContent GetContent<T>(Expression<System.Func<T>> memberExpression) {
        MemberExpression expressionBody = (MemberExpression)memberExpression.Body;
        string fieldName = expressionBody.Member.Name;

        char[] label = fieldName.Replace('_', '\0').ToCharArray();
        string labelStr = label[0].ToString().ToUpper();
        for(int i = 1; i < fieldName.Length; i++) {
            if(char.IsUpper(label[i])) {
                labelStr += " ";
            }
            labelStr += label[i];
        }
        return new GUIContent(labelStr, GetTooltip(fieldName));
    }

    string GetTooltip(string fieldName) {

        FieldInfo tip = GetTooltipAttribute(fieldName);
        if(tip == null) tip = GetTooltipAttribute("_" + fieldName);
        if(tip == null) return "";

        return ((TooltipAttribute)tip.GetCustomAttributes(typeof(TooltipAttribute), false)[0]).tooltip;
    }

    FieldInfo GetTooltipAttribute(string fieldName) {
        return typeof(GrassFlowRenderer).GetField(fieldName,
            BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public
            );
    }


    static Texture2D GetActiveBrushTexture() {
        Brush aBrush = brushList.GetActiveBrush();
        if(!aBrush.m_Mask) {
            _BrushList = new BrushList();
        }

        brushList.UpdateSelection(selectedBrushIndex);
        return brushList.GetActiveBrush().texture;
    }

    class Brush {

        public Texture2D m_Mask;

        Texture2D m_Texture = null;
        Texture2D m_Thumbnail = null;

        bool m_UpdateTexture = true;
        bool m_UpdateThumbnail = true;

        internal static Brush CreateInstance(Texture2D t) {
            var b = new Brush {
                m_Mask = t
            };
            return b;
        }

        void UpdateTexture() {
            if(m_UpdateTexture || m_Texture == null) {
                m_Texture = GenerateBrushTexture(m_Mask, m_Mask.width, m_Mask.height);
                m_UpdateTexture = false;
            }
        }

        void UpdateThumbnail() {
            if(m_UpdateThumbnail || m_Thumbnail == null) {
                m_Thumbnail = GenerateBrushTexture(m_Mask, 64, 64, true);
                m_UpdateThumbnail = false;
            }
        }

        public Texture2D texture { get { UpdateTexture(); return m_Texture; } }
        public Texture2D thumbnail { get { UpdateThumbnail(); return m_Thumbnail; } }

        public void SetDirty(bool isDirty) {
            m_UpdateTexture |= isDirty;
            m_UpdateThumbnail |= isDirty;
        }

        static Texture2D GenerateBrushTexture(Texture2D mask, int width, int height, bool isThumbnail = false) {
            RenderTexture oldRT = RenderTexture.active;
            RenderTextureFormat outputRenderFormat = RenderTextureFormat.ARGB32;
            TextureFormat outputTexFormat = TextureFormat.ARGB32;

            // build brush texture
            RenderTexture tempRT = RenderTexture.GetTemporary(width, height, 0, outputRenderFormat, RenderTextureReadWrite.Linear);
            Graphics.Blit(mask, tempRT);

            Texture2D previewTexture = new Texture2D(width, height, outputTexFormat, false, true);

            RenderTexture.active = tempRT;
            previewTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            previewTexture.Apply();

            RenderTexture.ReleaseTemporary(tempRT);
            tempRT = null;

            RenderTexture.active = oldRT;
            return previewTexture;
        }
    }



    class BrushList {
        [SerializeField] int m_SelectedBrush = 0;
        Brush[] m_BrushList = null;
        GUIContent[] m_Thumnails;

        // UI
        Vector2 m_ScrollPos;

        public int selectedIndex { get { return m_SelectedBrush; } }
        static class Styles {
            public static GUIStyle gridList = "GridList";
            public static GUIContent brushes = new GUIContent("Brushes");
        }

        public BrushList() {
            if(m_BrushList == null) {
                LoadBrushes();
                UpdateSelection(0);
            }
        }

        public void LoadBrushes() {
            // Load the textures;
            var arr = new List<Brush>();
            int idx = 1;
            Texture2D t = null;

            // Load brushes from editor resources
            do {
                Object tBrush = null;

#if UNITY_2019_1_OR_NEWER
                tBrush = EditorGUIUtility.Load(UnityEditor.Experimental.EditorResources.brushesPath + "builtin_brush_" + idx + ".brush");
#endif

                if(tBrush) {
                    //this is so freakin stupid but i have to do this now for Unity 2019+ because unity changed
                    //the way built-in brushes are stored and also removed the old brushes and compeletely broke compatibility and its a really bogus situation
                    //so now i have to use reflection methods to worm into the 2019 brush class and call methods and get the texture out of it
                    System.Type tType = tBrush.GetType();
                    var texField = tType.GetField("m_Texture", BindingFlags.Instance | BindingFlags.NonPublic);
                    var updateField = tType.GetField("m_UpdateTexture", BindingFlags.Instance | BindingFlags.NonPublic);
                    var updateMethod = tType.GetMethod("UpdateTexture", BindingFlags.Instance | BindingFlags.NonPublic);
                    updateField.SetValue(tBrush, true);
                    updateMethod.Invoke(tBrush, null);

                    t = (Texture2D)texField.GetValue(tBrush);
                    if(t) {
                        Color32[] pixels = t.GetPixels32();
                        for(int i = 0; i < pixels.Length; i++) {
                            pixels[i].a = pixels[i].r;
                        }
                        t = new Texture2D(t.width, t.height, TextureFormat.Alpha8, false);
                        t.SetPixels32(0, 0, t.width, t.height, pixels, 0);
                        t.Apply();

                        arr.Add(Brush.CreateInstance(t));
                    }
                } else {
                    t = (Texture2D)EditorGUIUtility.Load("builtin_brush_" + idx + ".png");
                    if(t) {
                        arr.Add(Brush.CreateInstance(t));
                    }
                }


                idx++;
            }
            while(t);

            // Load user created brushes from the Assets/Gizmos folder
            idx = 0;
            do {
                t = EditorGUIUtility.FindTexture("brush_" + idx + ".png");
                if(t)
                    arr.Add(Brush.CreateInstance(t));
                idx++;
            }
            while(t);

            m_BrushList = arr.ToArray();
        }

        public void SelectPrevBrush() {
            if(--m_SelectedBrush < 0)
                m_SelectedBrush = m_BrushList.Length - 1;
            UpdateSelection(m_SelectedBrush);
        }

        public void SelectNextBrush() {
            if(++m_SelectedBrush >= m_BrushList.Length)
                m_SelectedBrush = 0;
            UpdateSelection(m_SelectedBrush);
        }

        public void UpdateSelection(int newSelectedBrush) {
            m_SelectedBrush = newSelectedBrush;
        }

        public Brush GetCircleBrush() {
            return m_BrushList[0];
        }

        public Brush GetActiveBrush() {
            if(m_SelectedBrush >= m_BrushList.Length)
                m_SelectedBrush = 0;

            return m_BrushList[m_SelectedBrush];
        }

        public bool ShowGUI() {
            bool repaint = false;

            GUILayout.Label(Styles.brushes, EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            {
                Rect brushPreviewRect = EditorGUILayout.GetControlRect(true, GUILayout.Width(128), GUILayout.Height(128));
                if(m_BrushList != null) {
                    EditorGUI.DrawTextureAlpha(brushPreviewRect, GetActiveBrush().thumbnail);

                    bool dummy;
                    m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, GUILayout.Height(128));
                    var missingBrush = new GUIContent("No brushes defined.");
                    int newBrush = BrushSelectionGrid(m_SelectedBrush, m_BrushList, 32, Styles.gridList, missingBrush, out dummy);
                    if(newBrush != m_SelectedBrush) {
                        UpdateSelection(newBrush);
                        repaint = true;
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
            EditorGUILayout.EndHorizontal();

            return repaint;
        }

        int BrushSelectionGrid(int selected, Brush[] brushes, int approxSize, GUIStyle style, GUIContent emptyString, out bool doubleClick) {
            GUILayout.BeginVertical("box", GUILayout.MinHeight(approxSize));
            int retval = 0;

            doubleClick = false;

            if(brushes.Length != 0) {
                int columns = (int)(EditorGUIUtility.currentViewWidth - 150) / approxSize;
                int rows = (int)Mathf.Ceil((brushes.Length + columns - 1) / columns);
                Rect r = GUILayoutUtility.GetAspectRect((float)columns / (float)rows);
                Event evt = Event.current;
                if(evt.type == EventType.MouseDown && evt.clickCount == 2 && r.Contains(evt.mousePosition)) {
                    doubleClick = true;
                    evt.Use();
                }

                if(m_Thumnails == null || m_Thumnails.Length != brushes.Length) {
                    m_Thumnails = GUIContentFromBrush(brushes);
                }
                retval = GUI.SelectionGrid(r, System.Math.Min(selected, brushes.Length - 1), m_Thumnails, (int)columns, style);
            } else
                GUILayout.Label(emptyString);

            GUILayout.EndVertical();
            return retval;
        }

    }

    static GUIContent[] GUIContentFromBrush(Brush[] brushes) {
        GUIContent[] retval = new GUIContent[brushes.Length];

        for(int i = 0; i < brushes.Length; i++)
            retval[i] = new GUIContent(brushes[i].thumbnail);

        return retval;
    }

}



#endif