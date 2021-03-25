using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using GrassFlow;

#if GRASSFLOW_BURST
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using static Unity.Mathematics.math;
#endif

[ExecuteInEditMode]
[AddComponentMenu("Rendering/GrassFlow")]
public class GrassFlowRenderer : MonoBehaviour {

    [Tooltip("Maximum number of instances to render. This number gets used with the LOD system to decrease number of instances in the distance.")]
    [SerializeField] private int _instanceCount = 50;
    public int instanceCount {
        get { return _instanceCount; }
        set {
            _instanceCount = value;
            UpdateTransform();
        }
    }

    [Tooltip("Receive shadows on the grass. Can be expensive, especially with cascaded shadows on. (Requires the grass shader with depth pass to render properly)")]
    public bool receiveShadows = true;

    [Tooltip("Grass casts shadows. Fairly expensive option. (Also requires the grass shader with depth pass to render at all)")]
    [SerializeField] private bool _castShadows = false;
    [SerializeField] private ShadowCastingMode shadowMode;
    public bool castShadows {
        get { return _castShadows; }
        set {
            _castShadows = value;
            shadowMode = value ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }
    }

    [Tooltip("This setting only effects the editor. Most of the time you're going to want this on as it prevents visual popping as scripts are recompiled and such. " +
        "You can turn it off to get a more accurate view of game performance, though really it hardly makes any difference.")]
    public bool updateBuffers = true;

    [Tooltip("When on, this will manually frustum cull LOD chunks. This can help prevent Unity from having to cull the many instances that might be rendered per chunk. " +
        "This may be faster in some situations but it doesn't take shadows into account and can make shadows pop out when chunks go offscreen. " +
        "Turning this off, Unity will cull the instances rendered in the chunks.")]
    public bool useManualCulling = false;

    [Tooltip("Enables the ability to paint grass color and parameters dynamically in both the editor and in game. If true it creates Rendertextures from supplied textures " +
        "that can be painted and saved.")]
    [SerializeField] private bool _enableMapPainting = false;
    public bool enableMapPainting {
        get { return _enableMapPainting; }
        set {
            _enableMapPainting = value;

            if(value) {
                MapSetup();
            } else {
                ReleaseDetailMapRTs();
            }

            UpdateShaders();
        }
    }


    [Tooltip("Texture that controls grass color. The alpha channel of this texture is used to control how the color gets applied. " +
        "If alpha is 1, the color is also multiplied by material color, if 0, material color is ignored. Inbetween values work too.")]
    public Texture2D colorMap;

    [Tooltip("Texture that controls various parameters of the grass. Red channel = density. Green channel = height, Blue channel = flattenedness. Alpha channel = wind strength.")]
    public Texture2D paramMap;

    [Tooltip("Texture that controls which texture to use from the atlas in the grass texture atlas (if using one). " +
        "NOTE: Read the documentation for information about how this texture works.")]
    public Texture2D typeMap;

    [Tooltip("If true, an instance of the material will be created to render with. Important if you want to use the same material for multiple grasses but want them to have different textures etc.")]
    public bool useMaterialInstance = false;

    [Tooltip("Material to use to render the grass. The material should use one of the grassflow shaders.")]
    public Material grassMaterial;

    [Tooltip("Layer to render the grass on.")]
    public int renderLayer;

    [Tooltip("Mode this grass is for. Mesh will attach grass to the triangles of a mesh, terrain will attach grass to surface of a unity terrain object.")]
    public GrassRenderType renderType;

    [Tooltip("Amount to expand grass chunks on terrain, helps avoid artifacts on edges of chunks. Preferably set this as low as you can without it looking bad.")]
    public float terrainExpansion = 0.35f;


    [Tooltip("Enables the ability for grass to orient itself to the slope of the terrain and shade itself better. You can disable this to save on memory and a slight load time boost, " +
        "but it really isn't recommended to do so unless you really need to.")]
    public bool useTerrainNormalMap = true;

    //[Tooltip("Compress the terrain normal map to save on memory at the expense of a small increase in initial loading time.")]
    //public bool compressTerrainNormalMap = true;

    [Tooltip("Transform that the grass belongs to.")]
    public Transform terrainTransform;

    [Tooltip("Terrain object to attach grass to in terrain mode.")]
    public Terrain terrainObject;

    [Tooltip("Mesh to attach grass to in mesh mode.")]
    public Mesh grassMesh;

    [Tooltip("Amount of grass to render per mesh triangle in mesh mode. Technically controls the amount of grass per instance, per tri, meaning maximum total grass per tri = " +
                    "GrassPerTri * InstanceCount.")]
    public int grassPerTri = 4;

    [Tooltip("In my testing this made performance worse. Even though it feels like it should be faster with how indirect instancing works. Which is frustrating because ykno " +
        "I made the feature and then it's just worse somehow but well, here we are. You can try it out anyway and see if it's better for your situation. " +
        "IMPORTANT: You'll need to enable a shader keyword at the top of GrassFlow/Shaders/GrassStructsVars.cginc by uncommenting it for this to work properly.")]
    public bool useIndirectInstancing = false;

    [Tooltip("Does this really need a tooltip? Uhh, well chunk bounds are expanded automatically by blade height to avoid grass popping out when the bounds are culled at strange angles.")]
    [HideInInspector] public bool visualizeChunkBounds = false;

    [Tooltip("Dicards chunks that don't have ANY grass in them based on the parameter map density channel, " +
        "this will be significantly more performant if your terrain has large areas without grass." +
        "WARNING: enabling this removes the chunks completely, meaning that grass could not be dynamically added back in those chunks during runtime. " +
        "Recommended you leave this off while styling the grass or you might remove chunks and then if you try to paint density back into those areas it wont show up until you refresh.")]
    public bool discardEmptyChunks = false;

    RenderTexture terrainHeightmap;
    Texture terrainNormalMap;
    float terrainMapOffset = 0.0005f;

    [Tooltip("Controls the LOD parameter of the grass. X = render distance. Y = density falloff sharpness (how quickly the amount of grass is reduced to zero). " +
        "Z = offset, basically a positive number prevents blades from popping out within this distance.")]
    [SerializeField] private Vector3 _lodParams = new Vector3(15, 1.1f, 0);
    public Vector3 lodParams {
        get { return _lodParams; }
        set {
            _lodParams = value;
            if(drawMat) drawMat.SetVector("_LOD", value);
        }
    }

    [SerializeField] private float maxRenderDistSqr = 150f * 150f;

    [Tooltip("Controls max render dist of the grass chunks. This value is mostly just used to quickly reject far away chunks for rendering.")]
    [SerializeField] private float _maxRenderDist = 150f;
    public float maxRenderDist {
        get { return _maxRenderDist; }
        set {
            _maxRenderDist = value;
            maxRenderDistSqr = value * value;
        }
    }

    public int chunksX = 5;
    public int chunksY = 1;
    public int chunksZ = 5;

    public float numGrassAtlasTexes = 1;

    bool hasRequiredAssets {
        get {
            bool sharedAssets = grassMaterial && terrainTransform;
            if(renderType == GrassRenderType.Mesh) {
                return sharedAssets && grassMesh;
            } else {
                return sharedAssets && terrainObject;
            }
        }
    }

    [SerializeField] [HideInInspector] public Material drawMat;

    [System.NonSerialized] public RenderTexture colorMapRT;
    [System.NonSerialized] public RenderTexture paramMapRT;
    [System.NonSerialized] public RenderTexture typeMapRT;

    MeshChunker.MeshChunk[] terrainChunks;

    //BURST Vars
#if GRASSFLOW_BURST
    NativeArray<GrassBurstChunk> burstChunks;
    NativeQueue<GrassBurstChunk> culledBurstChunks;
#endif


    public enum GrassRenderType { Terrain, Mesh }


    //Static Vars
    static ComputeShader gfComputeShader;
    static ComputeBuffer rippleBuffer;
    static ComputeBuffer counterBuffer;
    static ComputeBuffer forcesBuffer;
    static RippleData[] forcesArray;
    static GrassForce[] forceClassArray;
    static int forcesCount;
    static bool forcesDirty;
    static int updateRippleKernel;
    static int addRippleKernel;
    static int noiseKernel;
    static int normalKernel;
    static int heightKernel;
    static int emptyChunkKernel;
    static int ripDeltaTimeHash = Shader.PropertyToID("ripDeltaTime");

    static RenderTexture noise3DTexture;

    //static Shader paintShader;
    //static Material paintMat;
    //const int paintPass = 0;
    //const int splatPass = 1;
    static ComputeShader paintShader;
    static int paintKernel;
    static int splatKernel;

    public static HashSet<GrassFlowRenderer> instances = new HashSet<GrassFlowRenderer>();
    static bool runRipple = true;

    /// <summary>
    /// This is set to true as soon as a ripple is added and stays true unless manually set to false.
    /// When true it signals the ripple update shaders to run, it doesn't take long to run them and theres no easy generic way to know when all ripples are depleted without asking the gpu for the memory which would be slow.
    /// But you can manually set this if you know your ripples only last a certain amount of time or something.
    /// Realistically its not that important though.
    /// </summary>
    public static bool updateRipples = false;


    //
    //-------------Actual code-------------
    //

    void Awake() {
        instances.Add(this);

        if(hasRequiredAssets)
            Init();
    }

    private void Start() {

    }

    void UnHookRender() {
#if !GRASSFLOW_SRP
        Camera.onPreCull -= Render;
#else
        RenderPipelineManager.beginCameraRendering -= Render;
#endif
    }

    void HookRender() {
#if !GRASSFLOW_SRP
        Camera.onPreCull += Render;
#else
        RenderPipelineManager.beginCameraRendering += Render;
#endif
    }

#if UNITY_2019_2_OR_NEWER
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#else
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
    static void StaticDomain() {
        runRipple = true;
        updateRipples = false;
        instances = new HashSet<GrassFlowRenderer>();
        forcesCount = 0;

        if (noise3DTexture) {
            noise3DTexture.Release();
            noise3DTexture = null;
        }

        ReleaseBuffers();
    }

    public void OnEnable() {
        UpdateTransform();
        UnHookRender();

        if(hasRequiredAssets) {
            if(!initialized) {
                Init();
            } else {
                HookRender();
            }
        }

        //have to reset these on enable due to reasons related to what is described in OnDisable
        CheckIndirectInstancingArgs();
    }



    private void OnDisable() {
        UnHookRender();

        ReleaseBuffers();


        //unity throws a buncha warnings about the indirect args buffer being unallocated and disposed by the garbage collector when scripts are rebuilt if we dont do this
        //becuse of how unity's weird system of serialization works it just automatically unallocates the buffer on reload so we have to catch it here and do it manually
        //because for whatever reason youre not supposed to let garbage collection dispose of them automatically or itll complain
        ReleaseIndirectArgsBuffers();

        BurstDisposal();
    }


#if UNITY_EDITOR

    private void Reset() {
        terrainTransform = transform;
        terrainObject = GetComponent<Terrain>();

        MeshFilter meshF;
        if(meshF = GetComponent<MeshFilter>()) {
            grassMesh = meshF.sharedMesh;
            renderType = GrassRenderType.Mesh;
        }

    }


    //the validation function is mainly to regenerate certain things that are lost upon unity recompiling scripts
    //but also in some other situations like saving the scene
    private void OnValidate() {
        if(!isActiveAndEnabled || !hasRequiredAssets || StackTraceUtility.ExtractStackTrace().Contains("Inspector"))
            return;

        if(terrainChunks == null) {
            Refresh();
        } else {
            GetResources();
            UpdateShaders();
            MapSetup();
            UpdateBurstData();
        }


        //on script reload, property blocks are lost for some reason so re add them so they dont break
        foreach(MeshChunker.MeshChunk chunk in terrainChunks) {
            if(chunk.propertyBlock == null) {
                chunk.propertyBlock = new MaterialPropertyBlock();
            }
            if(renderType == GrassRenderType.Terrain) {
                chunk.propertyBlock.SetVector(_chunkPosID, chunk.chunkPos);
            }
        }


        UnHookRender();
        HookRender();
    }

    private void OnDrawGizmos() {
        if(!visualizeChunkBounds) return;

        Gizmos.color = Color.green;
        foreach(MeshChunker.MeshChunk chunk in terrainChunks) {
            Gizmos.DrawWireCube(chunk.worldBounds.center, chunk.worldBounds.size);
        }
    }

    //stuff for toggling the preprocessor definition for ability to use burst
    public const string grassBurstDefine = "GRASSFLOW_BURST";
    public const string grassSRPDefine = "GRASSFLOW_SRP";

    static readonly UnityEditor.BuildTargetGroup[] burstPlatforms = new UnityEditor.BuildTargetGroup[] {
        UnityEditor.BuildTargetGroup.Standalone,
        UnityEditor.BuildTargetGroup.XboxOne,
        UnityEditor.BuildTargetGroup.PS4,
        UnityEditor.BuildTargetGroup.Android,
        UnityEditor.BuildTargetGroup.iOS,
        UnityEditor.BuildTargetGroup.WebGL,
    };

#if GRASSFLOW_BURST
    [UnityEditor.MenuItem("CONTEXT/GrassFlowRenderer/Disable Burst Capability (READ DOC)")]
#else
    [UnityEditor.MenuItem("CONTEXT/GrassFlowRenderer/Enable Burst Capability (READ DOC)")]
#endif
    static void ToggleBurst() {

        if (!CheckForDefineSymbol(grassBurstDefine)) {
            ImportBurstPackages();
        } else {
            ToggleDefineSymbol(grassBurstDefine, burstPlatforms);
        }
    }

#if GRASSFLOW_SRP
    [UnityEditor.MenuItem("CONTEXT/GrassFlowRenderer/Disable URP Support (READ DOC)")]   
#else
    [UnityEditor.MenuItem("CONTEXT/GrassFlowRenderer/Enable URP Support (READ DOC)")]
#endif
    static void ToggleSRP() {
        ToggleDefineSymbol(grassSRPDefine, burstPlatforms);
    }

    static bool CheckForDefineSymbol(string symbolName) {
        return UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(burstPlatforms[0]).Contains(symbolName);
    }

    static bool ToggleDefineSymbol(string symbolName, UnityEditor.BuildTargetGroup[] platforms) {
        bool enable = !CheckForDefineSymbol(symbolName);

        foreach (UnityEditor.BuildTargetGroup buildTarget in platforms) {
            string defines = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);

            if (!defines.Contains(symbolName) && enable) {
                UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, defines + ";" + symbolName);

            } else if (defines.Contains(symbolName) && !enable) {
                UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, defines.Replace(symbolName, ""));
            }
        }

        return enable;
    }


#if UNITY_2019_1_OR_NEWER
    async static void ImportBurstPackages() {
        if (UnityEditor.EditorUtility.DisplayDialog("GrassFlow", "Try to automatically import required packages for GrassFlow BURST support?", "Yes", "No")) {
            
            var req = UnityEditor.PackageManager.Client.Add("com.unity.burst");
            while (!req.IsCompleted) await System.Threading.Tasks.Task.Delay(100);
            print("Imported com.unity.burst");

            req = UnityEditor.PackageManager.Client.Add("com.unity.mathematics");
            while (!req.IsCompleted) await System.Threading.Tasks.Task.Delay(100);
            print("Imported com.unity.mathematics");

            req = UnityEditor.PackageManager.Client.Add("com.unity.collections");
            while (!req.IsCompleted) await System.Threading.Tasks.Task.Delay(100);
            print("Imported com.unity.collections");
        }
#else
    static void ImportBurstPackages() {
#endif
        ToggleDefineSymbol(grassBurstDefine, burstPlatforms);
    }


#endif

    /// <summary>
    /// Releases current assets and reinitializes the grass.
    /// Warning: Will reset current map paint status. (If that is the intended effect, use RevertDetailMaps() instead)
    /// </summary>
    public void Refresh() {
        if(isActiveAndEnabled) {
            ReleaseAssets();

            Init();
        }
    }

    bool initialized = false;
    void Init() {


        if(!hasRequiredAssets) {
            Debug.LogError("GrassFlow: Not all required assets assigned in the inspector!");
            return;
        }

        if(!isActiveAndEnabled) return;

        GetResources();

        CheckRippleBuffers();

        MapSetup();

        HandleLodChunks();

        UpdateShaders();

        UpdateTransform();

        UpdateBurstData();


        UnHookRender();
        HookRender();

        //print("init");
        initialized = true;

    }


    void CheckIndirectInstancingArgs() {
        if(useIndirectInstancing) {
            if(terrainChunks != null) {
                foreach(MeshChunker.MeshChunk chunk in terrainChunks) {
                    if(chunk.indirectArgs == null) {
                        chunk.SetIndirectArgs();
                    }
                }
            }
        } else {
            ReleaseIndirectArgsBuffers();
        }
    }

    void ReleaseIndirectArgsBuffers() {
        if(terrainChunks != null) {
            foreach(MeshChunker.MeshChunk chunk in terrainChunks) {
                if(chunk.indirectArgs != null) {
                    chunk.indirectArgs.Release();
                    chunk.indirectArgs = null;
                }
            }
        }
    }

    void HandleLodChunks() {
        if(terrainChunks != null) {
            ReleaseIndirectArgsBuffers();
        }

        if(renderType == GrassRenderType.Mesh) {
            terrainChunks = MeshChunker.Chunk(grassMesh, chunksX, chunksY, chunksZ, grassPerTri, 1);
        } else {

            terrainChunks = MeshChunker.ChunkTerrain(terrainObject, chunksX, chunksZ, grassPerTri, terrainExpansion, drawMat.GetFloat("bladeHeight"));

            if(!terrainHeightmap) {
                terrainHeightmap = TextureCreator.GetTerrainHeightMap(terrainObject, gfComputeShader, heightKernel, true);
            }

            //offset by half a pixel so it aligns properly
            terrainMapOffset = 1f / terrainHeightmap.width * 0.5f;


            if(useTerrainNormalMap && !terrainNormalMap) {
                terrainNormalMap = TextureCreator.GetTerrainNormalMap(terrainObject, gfComputeShader, terrainHeightmap, normalKernel);
            }

            if(discardEmptyChunks) DiscardUnusedChunks();
        }


        CheckIndirectInstancingArgs();
    }

    void DiscardUnusedChunks() {
        Texture paramTex;
        if(!(paramTex = paramMapRT)) paramTex = paramMap;

        if(terrainChunks == null || !hasRequiredAssets || !paramTex
            || renderType != GrassRenderType.Terrain) return;

        gfComputeShader.SetVector("chunkDims", new Vector4(chunksX, chunksZ));
        gfComputeShader.SetTexture(emptyChunkKernel, "paramMap", paramTex);
        ComputeBuffer chunkResultsBuffer = new ComputeBuffer(terrainChunks.Length, sizeof(int));
        int[] chunkResults = new int[terrainChunks.Length];
        chunkResultsBuffer.SetData(chunkResults);
        gfComputeShader.SetBuffer(emptyChunkKernel, "chunkResults", chunkResultsBuffer);

        gfComputeShader.Dispatch(emptyChunkKernel, Mathf.CeilToInt(paramMap.width / paintThreads), Mathf.CeilToInt(paramMap.height / paintThreads), 1);

        chunkResultsBuffer.GetData(chunkResults);
        chunkResultsBuffer.Release();

        List<MeshChunker.MeshChunk> resultChunks = new List<MeshChunker.MeshChunk>();
        for(int i = 0; i < terrainChunks.Length; i++) {
            if(chunkResults[i] > 0) resultChunks.Add(terrainChunks[i]);
        }

        terrainChunks = resultChunks.ToArray();
    }

    new void Destroy(Object obj) {
        if(Application.isPlaying) {
            Object.Destroy(obj);
        } else {
            DestroyImmediate(obj);
        }
    }


    void ReleaseAssets() {
        UnHookRender();

        ReleaseDetailMapRTs();

        drawMat = null;

        if(terrainHeightmap) terrainHeightmap.Release();
        terrainHeightmap = null;

        if(terrainNormalMap && terrainNormalMap.GetType() == typeof(RenderTexture)) {
            (terrainNormalMap as RenderTexture).Release();
        }
        terrainNormalMap = null;

        if(terrainChunks != null) {
            foreach(MeshChunker.MeshChunk chunk in terrainChunks) {
                if(chunk.indirectArgs != null) {
                    chunk.indirectArgs.Release();
                    chunk.indirectArgs = null;
                }

                Destroy(chunk.mesh);
            }

            terrainChunks = null;
        }
    }

    void ReleaseDetailMapRTs() {
        if(colorMapRT) colorMapRT.Release(); colorMapRT = null;
        if(paramMapRT) paramMapRT.Release(); paramMapRT = null;
        if(typeMapRT) typeMapRT.Release(); typeMapRT = null;
    }

    /// <summary>
    /// Reverts unsaved paints to grass color and paramter maps.
    /// </summary>
    public void RevertDetailMaps() {
        ReleaseDetailMapRTs();
        MapSetup();
    }

    Matrix4x4[] matrices;


    void MakeMatrices(Matrix4x4 tMatrix) {

        if(matrices == null || matrices.Length != instanceCount) {
            matrices = new Matrix4x4[instanceCount];
        }

        for(int i = 0; i < matrices.Length; i++) {
            matrices[i] = tMatrix;
        }
    }

    /// <summary>
    /// Updates the transformation matrices used to render grass.
    /// You should call this if the object the grass is attached to moves.
    /// </summary>
    public void UpdateTransform() {
        if(!terrainTransform) return;

        if(useIndirectInstancing) {
            SetDrawmatObjMatrices();
        }

        FillTransformData(terrainTransform.localToWorldMatrix, terrainTransform.position);
    }

    void FillTransformData(Matrix4x4 tMatrix, Vector3 terrainPos) {
        if(!useIndirectInstancing) {
            MakeMatrices(tMatrix);
        }

        if(terrainChunks == null) return;
        if(renderType == GrassRenderType.Mesh) {
            foreach(MeshChunker.MeshChunk chunk in terrainChunks) {
                //need to transform the chunk bounds to match the new matrix
                chunk.worldBounds.center = tMatrix.MultiplyPoint3x4(chunk.meshBounds.center);

                //kinda dumb and inefficient but its the easiest way to make sure
                //the bounds encapsulate the mesh if its been rotated
                Vector3 ext = tMatrix.MultiplyVector(chunk.meshBounds.extents);
                float maxExt = Mathf.Max(
                    Mathf.Abs(ext.x),
                    Mathf.Abs(ext.y),
                    Mathf.Abs(ext.z)
                );
                chunk.worldBounds.extents = new Vector3(maxExt, maxExt, maxExt);

                //if(useManualCulling) {
                //Vector3 ext = chunk.worldBounds.extents;
                //    ext.x = Mathf.Abs(ext.x);
                //    ext.y = Mathf.Abs(ext.y);
                //    ext.z = Mathf.Abs(ext.z);
                //    chunk.worldBounds.extents = ext;
                //}
            }
        } else {
            foreach(MeshChunker.MeshChunk chunk in terrainChunks) {
                chunk.worldBounds.center = chunk.meshBounds.center + terrainPos;
            }
        }
    }

    /// <summary>
    /// Asynchronously updates the transformation matrices used to render grass.
    /// <para>You should call this if the object the grass is attached to moves.</para>
    /// </summary>
#if CSHARP_7_3_OR_NEWER
    public async System.Threading.Tasks.Task
#else
    public void
#endif
    UpdateTransformAsync() {
#if CSHARP_7_3_OR_NEWER
        Matrix4x4 tMatrix = terrainTransform.localToWorldMatrix;
        Vector3 pos = terrainTransform.position;

        await System.Threading.Tasks.Task.Run(() => {
            FillTransformData(tMatrix, pos);
        });
#else
        Debug.LogError("GrassFlow.UpdateTransformAsync: Project C# version does not support async methods.");
#endif
    }


    void UpdateBurstData() {
#if GRASSFLOW_BURST

        if(terrainChunks == null) return;

        if(burstChunks.IsCreated) {
            burstChunks.Dispose();
        }


        burstChunks = new NativeArray<GrassBurstChunk>(terrainChunks.Length, Allocator.Persistent);

        for(int i = 0; i < terrainChunks.Length; i++) {
            burstChunks[i] = new GrassBurstChunk() {
                idx = i,
                worldBounds = terrainChunks[i].worldBounds
            };
        }

        if(!culledBurstChunks.IsCreated) {
            culledBurstChunks = new NativeQueue<GrassBurstChunk>(Allocator.Persistent);
        }

        culledBurstChunks.Clear();

#endif
    }

    void SetDrawmatObjMatrices() {
        if(drawMat) {
            drawMat.SetMatrix("objToWorldMatrix", terrainTransform.localToWorldMatrix);
            drawMat.SetMatrix("worldToObjMatrix", terrainTransform.worldToLocalMatrix);
        }
    }

    const int maxRipples = 128;
    const int maxForces = 64;

    void CheckRippleBuffers() {
        if(rippleBuffer == null) {
            rippleBuffer = new ComputeBuffer(maxRipples, Marshal.SizeOf(typeof(RippleData)));
        }
        if(forcesBuffer == null) {
            forcesBuffer = new ComputeBuffer(maxForces, Marshal.SizeOf(typeof(RippleData)));
        }
        if(forcesArray == null) {
            forcesArray = new RippleData[maxForces];
        }
        if(forceClassArray == null) {
            forceClassArray = new GrassForce[maxForces];
        }
        if(counterBuffer == null) {
            counterBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(Vector4)));
            counterBuffer.SetData(new Vector4[] { Vector4.zero });
        }
    }

    void GetResources() {
        if(!drawMat) {
            drawMat = useMaterialInstance ? Instantiate(grassMaterial) : grassMaterial;
        }


        if(renderType == GrassRenderType.Mesh) {
            drawMat.EnableKeyword("RENDERMODE_MESH");
        } else {
            drawMat.DisableKeyword("RENDERMODE_MESH");
        }

        //#if UNITY_EDITOR
        //        drawMat.EnableKeyword("GRASS_EDITOR");
        //#else
        //        drawMat.DisableKeyword("GRASS_EDITOR");
        //#endif

#if UNITY_2018_3_OR_NEWER
        drawMat.DisableKeyword("BAKED_HEIGHTMAP");
#else
        drawMat.EnableKeyword("BAKED_HEIGHTMAP");
#endif


        if(!gfComputeShader) gfComputeShader = Resources.Load<ComputeShader>("GrassFlow/GrassFlowCompute");
        addRippleKernel = gfComputeShader.FindKernel("AddRipple");
        updateRippleKernel = gfComputeShader.FindKernel("UpdateRipples");
        noiseKernel = gfComputeShader.FindKernel("NoiseMain");
        normalKernel = gfComputeShader.FindKernel("NormalsMain");
        heightKernel = gfComputeShader.FindKernel("HeightmapMain");
        emptyChunkKernel = gfComputeShader.FindKernel("EmptyChunkDetect");

        if(!paintShader) paintShader = Resources.Load<ComputeShader>("GrassFlow/GrassFlowPainter");
        //if(!paintMat) paintMat = new Material(paintShader);
        paintKernel = paintShader.FindKernel("PaintKernel");
        splatKernel = paintShader.FindKernel("ApplySplatTex");

        if(!noise3DTexture) {
            noise3DTexture = Resources.Load<RenderTexture>("GrassFlow/GF3DNoise");
            noise3DTexture.Release();
            noise3DTexture.enableRandomWrite = true;
            noise3DTexture.Create();

            //compute 3d noise
            gfComputeShader.SetTexture(noiseKernel, "NoiseResult", noise3DTexture);
            gfComputeShader.Dispatch(noiseKernel, noise3DTexture.width / 8, noise3DTexture.height / 8, noise3DTexture.volumeDepth / 8);
        }
    }

    void MapSetup() {
        if(enableMapPainting) {
            CheckMap(colorMap, ref colorMapRT, RenderTextureFormat.ARGB32);
            CheckMap(paramMap, ref paramMapRT, RenderTextureFormat.ARGB32);
            CheckMap(typeMap, ref typeMapRT, RenderTextureFormat.R8);
        }
    }

    void CheckMap(Texture2D srcMap, ref RenderTexture outRT, RenderTextureFormat format) {
        if(srcMap && !outRT) {
            RenderTexture oldRT = RenderTexture.active;
            outRT = new RenderTexture(srcMap.width, srcMap.height, 0, format, RenderTextureReadWrite.Linear) {
                enableRandomWrite = true, filterMode = srcMap.filterMode, wrapMode = srcMap.wrapMode, name = srcMap.name + "RT"
            };
            outRT.Create();
            Graphics.Blit(srcMap, outRT);
            RenderTexture.active = oldRT;
        }
    }

    struct RippleData {
        internal Vector4 pos; // w = strength
        internal Vector4 drssParams;//xyzw = decay, radius, sharpness, speed 
    }


    private void Update() {
        UpdateRipples();

#if UNITY_EDITOR
        if(updateBuffers && hasRequiredAssets)
            UpdateShaders();

        CheckInspectorPaint();
#endif
    }


#if UNITY_EDITOR
    bool shouldPaint;
    System.Action paintAction;

    void CheckInspectorPaint() {
        if(shouldPaint && paintAction != null) {
            paintAction.Invoke();
            shouldPaint = false;
        }
    }

    //its really stupid that this has to exist but it do be that way
    //its explained why in GrassFlowInspector
    //used only for painting during scene gui
    void InspectorSetPaintAction(System.Action action) {
        paintAction = action;
        shouldPaint = true;
    }
#endif


    /// <summary>
    /// This basically sets all required variables and textures to the various shaders to make them run.
    /// You might need to call this after changing certain variables/textures to make them take effect.
    /// </summary>
    public void UpdateShaders() {
        if(!drawMat) return;

        if(rippleBuffer != null && counterBuffer != null) {
            drawMat.SetBuffer(rippleBufferID, rippleBuffer);
            drawMat.SetBuffer(rippleCountID, counterBuffer);

            try {
                gfComputeShader.SetBuffer(addRippleKernel, rippleBufferID, rippleBuffer);
                gfComputeShader.SetBuffer(updateRippleKernel, rippleBufferID, rippleBuffer);
                gfComputeShader.SetBuffer(addRippleKernel, rippleCountID, counterBuffer);
                gfComputeShader.SetBuffer(updateRippleKernel, rippleCountID, counterBuffer);
            } catch { }
        }

        if(forcesBuffer != null) {
            drawMat.SetBuffer(forcesBufferID, forcesBuffer);
        }

        if(noise3DTexture) drawMat.SetTexture(_NoiseTexID, noise3DTexture);

        if(enableMapPainting) {
            if(colorMapRT) drawMat.SetTexture(colorMapID, colorMapRT);
            if(paramMapRT) drawMat.SetTexture(dhfParamMapID, paramMapRT);
            if(typeMapRT) drawMat.SetTexture(typeMapID, typeMapRT);
        } else {
            if(colorMap) drawMat.SetTexture(colorMapID, colorMap);
            if(paramMap) drawMat.SetTexture(dhfParamMapID, paramMap);
            if(typeMap) drawMat.SetTexture(typeMapID, typeMap);
        }

        if(terrainObject) {
            if(terrainHeightmap) drawMat.SetTexture(terrainHeightMapID, terrainHeightmap);
            if(useTerrainNormalMap && terrainNormalMap) drawMat.SetTexture(terrainNormalMapID, terrainNormalMap);
            else drawMat.SetTexture(terrainNormalMapID, null);

            Vector3 terrainScale = terrainObject.terrainData.size;
            drawMat.SetVector(terrainSizeID, terrainObject.terrainData.size);
            drawMat.SetVector(terrainChunkSizeID, new Vector4(terrainScale.x / chunksX, terrainScale.z / chunksZ));
            drawMat.SetFloat(terrainExpansionID, terrainExpansion);
            drawMat.SetFloat(terrainMapOffsetID, terrainMapOffset);
        }

        //a bit weird but saves having to do an extra division in the shader ¯\_(ツ)_/¯
        numGrassAtlasTexes = drawMat.GetFloat(numTexturesID);
        drawMat.SetFloat(numTexturesPctUVID, 1.0f / numGrassAtlasTexes);

        if(useIndirectInstancing && terrainTransform) {
            SetDrawmatObjMatrices();
        }
    }


    //----------------------------------
    //MAIN RENDER FUNCTION--------------
    //----------------------------------
    Plane[] frustumPlanes;
#if !GRASSFLOW_SRP
    void Render(Camera cam) {
#else
    void Render(ScriptableRenderContext context, Camera cam) {
#endif

#if UNITY_EDITOR
        //these arent really as much of an issue in a built game
#if UNITY_2018_3_OR_NEWER
        //make sure not to render grass in prefab stage unless its part of the prefab
        if(UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null
            && UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) == null) return;
#endif
        if(cam.name == "Preview Scene Camera") return;
        if(!hasRequiredAssets) OnDisable();
#endif

#if !GRASSFLOW_BURST

        if(useManualCulling)
            frustumPlanes = GeometryUtility.CalculateFrustumPlanes(cam);

        float instanceMult = instanceCount;

        foreach(MeshChunker.MeshChunk chunk in terrainChunks) {

            if(useManualCulling && !GeometryUtility.TestPlanesAABB(frustumPlanes, chunk.worldBounds)) {
                continue;
            }

            float camDist = chunk.worldBounds.SqrDistance(cam.transform.position);
            if(camDist > maxRenderDistSqr) {
                continue;
            }

            camDist = Mathf.Sqrt(camDist) - lodParams.z;
            if(camDist <= 0f) camDist = 0.0001f;
            camDist = 1.0f / camDist;

            float bladePct = Mathf.Pow(camDist * lodParams.x, lodParams.y);
            float bladeCnt = bladePct * instanceMult;
            //if(bladeCnt > 1f) bladeCnt = 1f;


            if(bladeCnt > instanceMult) bladeCnt = instanceMult;
            int instancesToRender = Mathf.CeilToInt(bladeCnt);

            chunk.propertyBlock.SetFloat(_instancePctID, bladePct);
            chunk.propertyBlock.SetFloat(_instanceLodID, bladeCnt);

            if(useIndirectInstancing) {
                chunk.instanceCount = (uint)instancesToRender;
                Graphics.DrawMeshInstancedIndirect(chunk.mesh, 0, drawMat, chunk.worldBounds, chunk.indirectArgs, 0, chunk.propertyBlock, shadowMode, receiveShadows, renderLayer, cam);
            } else {

                if(renderType == GrassRenderType.Terrain) {
                    chunk.mesh.bounds = chunk.meshBounds;
                }

                //Graphics.DrawMesh(chunk.mesh, matrices[0], drawMat, renderLayer, cam, 0, chunk.propertyBlock, shadowMode, receiveShadows);
                Graphics.DrawMeshInstanced(chunk.mesh, 0, drawMat, matrices, instancesToRender, chunk.propertyBlock, shadowMode, receiveShadows, renderLayer, cam);
            }

        }

#else
        if(!culledBurstChunks.IsCreated) return;

        RunBurstCulling(cam.transform.position);

        for(int i = culledBurstChunks.Count - 1; i >= 0; i--) {
            var burstChunk = culledBurstChunks.Dequeue();
            var chunk = terrainChunks[burstChunk.idx];

            chunk.propertyBlock.SetFloat(_instancePctID, burstChunk.bladePct);
            chunk.propertyBlock.SetFloat(_instanceLodID, burstChunk.bladeCnt);

            if(useIndirectInstancing) {
                chunk.instanceCount = (uint)burstChunk.instancesToRender;
                Graphics.DrawMeshInstancedIndirect(chunk.mesh, 0, drawMat, chunk.worldBounds, chunk.indirectArgs, 0, chunk.propertyBlock, shadowMode, receiveShadows, renderLayer, cam);
            } else {

                if(renderType == GrassRenderType.Terrain) {
                    chunk.mesh.bounds = chunk.meshBounds;
                }

                Graphics.DrawMeshInstanced(chunk.mesh, 0, drawMat, matrices, burstChunk.instancesToRender,
                    chunk.propertyBlock, shadowMode, receiveShadows, renderLayer, cam);
            }
        }


#endif
    }

    CommandBuffer tBuffer;

    //--------------------------------    
    //BURST STUFF---------------------
    //--------------------------------
#if GRASSFLOW_BURST
    void RunBurstCulling(Vector3 cameraPos) {

        culledBurstChunks.Clear();

        GrassCullJob cullJob = new GrassCullJob() {
            camPos = cameraPos,
            culledResults = culledBurstChunks.AsParallelWriter(),
            inputChunks = burstChunks,
            maxRenderDistSqr = maxRenderDistSqr,
            instanceMult = instanceCount,
            lodParams = lodParams
        };

        cullJob.Schedule(burstChunks.Length, 25).Complete();
    }

    //struct for use in burst culling
    struct GrassBurstChunk {
        public int idx;
        public Bounds worldBounds;
        public int instancesToRender;
        public float bladePct;
        public float bladeCnt;
    }

    [BurstCompile]
    struct GrassCullJob : IJobParallelFor {

        [ReadOnly] public NativeArray<GrassBurstChunk> inputChunks;
        public NativeQueue<GrassBurstChunk>.ParallelWriter culledResults;

        [ReadOnly] public Vector3 camPos;
        [ReadOnly] public float maxRenderDistSqr;
        [ReadOnly] public float instanceMult;
        [ReadOnly] public float3 lodParams;

        public void Execute(int idx) {
            GrassBurstChunk chunk = inputChunks[idx];

            float camDist = chunk.worldBounds.SqrDistance(camPos);
            if(camDist > maxRenderDistSqr) {
                return;
            }

            camDist = sqrt(camDist) - lodParams.z;
            if(camDist <= 0f) camDist = 0.0001f;
            camDist = 1.0f / camDist;

            float bladePct = pow(camDist * lodParams.x, lodParams.y);

            chunk.bladePct = saturate(bladePct);
            chunk.bladeCnt = chunk.bladePct * instanceMult;
            chunk.instancesToRender = (int)ceil(chunk.bladeCnt);
            culledResults.Enqueue(chunk);
        }
    }
#endif

    void BurstDisposal() {
#if GRASSFLOW_BURST
        if(burstChunks.IsCreated) burstChunks.Dispose();
        if(culledBurstChunks.IsCreated) culledBurstChunks.Dispose();
#endif
    }


    //--------------------------------    
    //RIPPLES-------------------------
    //--------------------------------
    private void LateUpdate() {
        runRipple = true;
    }

    void UpdateRipples() {
        if(runRipple && updateRipples) {
            runRipple = false;
            gfComputeShader.SetFloat(ripDeltaTimeHash, Time.deltaTime);
            gfComputeShader.Dispatch(updateRippleKernel, 1, 1, 1);
        }

        UpdateForces();
    }


    /// <summary>
    /// Adds a ripple into the ripple buffer that affects all grasses.
    /// Ripples are just that, ripples that animate accross the grass, a simple visual effect.
    /// </summary>
    /// <param name="pos">World position the ripple is placed at.</param>
    /// <param name="strength">How forceful the ripple is.</param>
    /// <param name="decayRate">How quickly the ripple dissipates.</param>
    /// <param name="speed">How fast the ripple moves across the grass.</param>
    /// <param name="startRadius">Start size of the ripple.</param>
    /// <param name="sharpness">How much this ripple appears like a ring rather than a circle.</param>
    public static void AddRipple(Vector3 pos, float strength = 1f, float decayRate = 2.5f, float speed = 25f, float startRadius = 0f, float sharpness = 0f) {
        if(!gfComputeShader) return;

        gfComputeShader.SetVector("pos", new Vector4(pos.x, pos.y, pos.z, strength));
        gfComputeShader.SetVector("drssParams", new Vector4(decayRate, startRadius, sharpness, speed));
        gfComputeShader.Dispatch(addRippleKernel, 1, 1, 1);
        updateRipples = true;
    }

    /// <summary>
    /// Adds a ripple into the ripple buffer that affects all grasses.
    /// Ripples are just that, ripples that animate accross the grass, a simple visual effect.
    /// </summary>
    /// <param name="pos">World position the ripple is placed at.</param>
    /// <param name="strength">How forceful the ripple is.</param>
    /// <param name="decayRate">How quickly the ripple dissipates.</param>
    /// <param name="speed">How fast the ripple moves across the grass.</param>
    /// <param name="startRadius">Start size of the ripple.</param>
    /// <param name="sharpness">How much this ripple appears like a ring rather than a circle.</param>
    public void AddARipple(Vector3 pos, float strength = 1f, float decayRate = 2.5f, float speed = 25f, float startRadius = 0f, float sharpness = 0f) {
        AddRipple(pos, strength, decayRate, speed, startRadius, sharpness);
    }




    //--------------------------------------------------------------------------------
    //------------------------FORCES---------------------------------------
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Intermediary class to handle point source grass forces.
    /// <para>Do not manually create instances of this class. Instead, use GrassFlowRenderer.AddGrassForce</para>
    /// </summary>
    public class GrassForce {

        public int index = -1;

        public bool added { get; private set; }

        public void Add() {

            if(forcesCount >= maxForces) {
                return;
            }

            if(added) {
                return;
            }

            index = forcesCount;
            forceClassArray[forcesCount] = this;
            forcesCount++;
            added = true;
            forcesDirty = true;
        }

        public void Remove() {

            if(!added) {
                return;
            }

            if(forcesArray == null) {
                forcesCount = 0;
                return;
            }

            forcesCount--;
            forcesArray[index] = forcesArray[forcesCount];
            GrassForce swapForce = forceClassArray[forcesCount];
            swapForce.index = index;
            forceClassArray[index] = swapForce;


            index = -1;
            added = false;

            forcesDirty = true;
        }

        public Vector3 position {
            get {
                return forcesArray[index].pos;
            }
            set {
                forcesArray[index].pos = value;
                forcesDirty = true;
            }
        }

        public float radius {
            get {
                return forcesArray[index].drssParams.y;
            }
            set {
                forcesArray[index].drssParams.y = value;
                forcesDirty = true;
            }
        }

        public float strength {
            get {
                return forcesArray[index].drssParams.w;
            }
            set {
                forcesArray[index].drssParams.w = value;
                forcesDirty = true;
            }
        }
    }

    /// <summary>
    /// Adds a point-source constant force that pushes all grasses.
    /// <para>Store the returned force and change its values to update it.</para>
    /// </summary>
    public GrassForce AddForce(Vector3 pos, float radius, float strength) {
        return AddGrassForce(pos, radius, strength);
    }

    /// <summary>
    /// Removes the given GrassForce.
    /// </summary>
    public void RemoveForce(GrassForce force) {
        RemoveGrassForce(force);
    }
    /// <summary>
    /// Removes the given GrassForce.
    /// </summary>
    public static void RemoveGrassForce(GrassForce force) {
        force.Remove();
    }

    /// <summary>
    /// Adds a point-source constant force that pushes all grasses.
    /// <para>Store the returned force and change its values to update it.</para>
    /// </summary>
    public static GrassForce AddGrassForce(Vector3 pos, float radius, float strength) {
        if(forcesArray == null) {
            return null;
        }

        if(forcesCount >= maxForces) {
            return null;
        }

        GrassForce force = new GrassForce() {
            index = forcesCount,
            position = pos,
            radius = radius,
            strength = strength,
        };

        force.Add();

        return force;
    }


    void UpdateForces() {
        if(forcesDirty) {
            //print("update forces: " + forcesCount);
            forcesBuffer.SetData(forcesArray, 0, 0, forcesCount);
            drawMat.SetInt(forcesCountID, forcesCount);
            forcesDirty = false;
        }
    }



    //--------------------------------    
    //PAINTING------------------------
    //--------------------------------    
    static int mapToPaintID = Shader.PropertyToID("mapToPaint");
    static int brushTextureID = Shader.PropertyToID("brushTexture");
    const float paintThreads = 8f;

    /// <summary>
    /// Sets the texture to be used when calling paint functions.
    /// </summary>
    public static void SetPaintBrushTexture(Texture2D brushTex) {
        if(paintShader) paintShader.SetTexture(paintKernel, brushTextureID, brushTex);
    }

    /// <summary>
    /// Paints color onto the colormap.
    /// enableMapPainting needs to be turned on for this to work.
    /// Uses a global texture as the brush texture, should be set via SetPaintBrushTexture(Texture2D brushTex).
    /// </summary>
    /// <param name="texCoord">texCoord to paint at, usually obtained by a raycast.</param>
    /// <param name="clampRange">Clamp the painted values between this range. Not really used for colors but exists just in case.
    /// Should be set to 0 to 1 or greater than 1 for HDR colors.</param>
    public void PaintColor(Vector2 texCoord, float brushSize, float brushStrength, Color colorToPaint, Vector2 clampRange) {
        PaintDispatch(texCoord, brushSize, brushStrength, colorToPaint, colorMapRT, clampRange, 0f);
    }

    /// <summary>
    /// Paints parameters onto the paramMap.
    /// enableMapPainting needs to be turned on for this to work.
    /// Uses a global texture as the brush texture, should be set via SetPaintBrushTexture(Texture2D brushTex).
    /// </summary>
    /// <param name="texCoord">texCoord to paint at, usually obtained by a raycast.</param>
    /// <param name="densityAmnt">Amount density to paint.</param>
    /// <param name="heightAmnt">Amount height to paint.</param>
    /// <param name="flattenAmnt">Amount flatten to paint.</param>
    /// <param name="windAmnt">Amount wind to paint.</param>
    /// <param name="clampRange">Clamp the painted values between this range. Valid range for parameters is 0 to 1.</param>
    public void PaintParameters(Vector2 texCoord, float brushSize, float brushStrength, float densityAmnt, float heightAmnt, float flattenAmnt, float windAmnt, Vector2 clampRange) {
        PaintDispatch(texCoord, brushSize, brushStrength, new Vector4(densityAmnt, heightAmnt, flattenAmnt, windAmnt), paramMapRT, clampRange, 1f);
    }


    /// <summary>
    /// A more manual paint function that you most likely don't want to use.
    /// It's mostly only exposed so that the GrassFlowInspector can use it. But maybe you want to too, I'm not the boss of you.
    /// You could use this to paint onto your own RenderTextures.
    /// </summary>
    /// <param name="blendMode">Controls blend type: 0 for lerp towards, 1 for additive</param>
    public static void PaintDispatch(Vector2 texCoord, float brushSize, float brushStrength, Vector4 blendParams, RenderTexture mapRT, Vector2 clampRange, float blendMode) {
        if(!paintShader || !mapRT) return;

        //print(brushSize + " : "+ brushStrength + " : " + texCoord + " : " + blendParams + " : " +clampRange + " : " + blendMode);
        //srsBrushParams = (strength, radius, unused, alpha controls type/ 0 for lerp towards, 1 for additive)
        paintShader.SetVector(srsBrushParamsID, new Vector4(brushStrength, brushSize * 0.05f, 0, blendMode));
        paintShader.SetVector(clampRangeID, clampRange);

        paintShader.SetVector(brushPosID, texCoord);
        paintShader.SetVector(blendParamsID, blendParams);

        PaintShaderExecute(mapRT, paintKernel);
        //paintShader.Dispatch(paintKernel, Mathf.CeilToInt(mapRT.width / paintThreads), Mathf.CeilToInt(mapRT.height / paintThreads), 1);
    }

    static void PaintShaderExecute(RenderTexture mapRT, int pass) {
        //paintMat.SetTexture(mapToPaintID, mapRT);
        paintShader.SetTexture(pass, mapToPaintID, mapRT);

        RenderTexture tmpRT = RenderTexture.GetTemporary(mapRT.width, mapRT.height, 0, mapRT.format);
        if(!tmpRT.IsCreated()) {
            //I think theres some kind of bug on older versions of unity where sometimes,
            //at least in certain situations, RenderTexture.GetTemporary() returns you
            //a texture that hasn't actually been created. Go figure.
            //It'll still work fine with Graphics.Blit, but it won't work with Graphics.CopyTexture()
            //unless we make sure its created first like this
            //this will only happen once usually, as internally unity will reuse this texture next time we ask for it.
            //but will be discarded after a few frames of un-use
            tmpRT.Create();
        }
        //Graphics.CopyTexture(mapRT, tmpRT); //copytexture for some reason didnt work on URP last time i checked
        Graphics.Blit(mapRT, tmpRT);
        paintShader.SetTexture(pass, tmpMapRTID, tmpRT);

        paintShader.Dispatch(pass, Mathf.CeilToInt(mapRT.width / paintThreads), Mathf.CeilToInt(mapRT.height / paintThreads), 1);
        //Graphics.Blit(tmpRT, mapRT, paintMat, pass);
        RenderTexture.ReleaseTemporary(tmpRT);
    }

    /// <summary>
    /// Automatically controls grass density based on a splat layer from terrain data.
    /// </summary>
    /// <param name="terrain">Terrain to get splat data from</param>
    /// <param name="splatLayer">Zero based index of the splat layer from the terrain to use.</param>
    /// <param name="mode">Controls how the tex is applied. 0 = additive, 1 = subtractive, 2 = replace.</param>
    /// <param name="splatTolerance">Controls opacity tolerance.</param>
    public void ApplySplatTex(Terrain terrain, int splatLayer, int mode, float splatTolerance) {
        int channel = splatLayer % 4;
        int texIdx = splatLayer / 4;

        ApplySplatTex(terrain.terrainData.alphamapTextures[texIdx], channel, mode, splatTolerance);
    }

    /// <summary>
    /// Automatically controls grass density based on a splat tex.
    /// </summary>
    /// <param name="splatAlphaMap">The particular splat alpha map texture that has the desired splat layer on it.</param>
    /// <param name="channel">Zero based index of the channel of the texture that represents the splat layer.</param>
    /// <param name="mode">Controls how the tex is applied. 0 = additive, 1 = subtractive, 2 = replace.</param>
    /// <param name="splatTolerance">Controls opacity tolerance.</param>
    public void ApplySplatTex(Texture2D splatAlphaMap, int channel, int mode, float splatTolerance) {
        if(!enableMapPainting || !paramMapRT) {
            Debug.LogError("Couldn't apply splat tex, map painting not enabled!");
            return;
        }

        paintShader.SetTexture(splatKernel, "splatTex", splatAlphaMap);
        paintShader.SetTexture(splatKernel, "mapToPaint", paramMapRT);

        paintShader.SetInt("splatMode", mode);
        paintShader.SetInt("splatChannel", channel);

        paintShader.SetFloat("splatTolerance", splatTolerance);

        PaintShaderExecute(paramMapRT, splatKernel);
        //paintShader.Dispatch(splatKernel, Mathf.CeilToInt(paramMapRT.width / paintThreads), Mathf.CeilToInt(paramMapRT.width / paintThreads), 1);
    }


    //
    //Shader Property IDs
    //
    //base shader
    static int rippleBufferID = Shader.PropertyToID("rippleBuffer");
    static int forcesBufferID = Shader.PropertyToID("forcesBuffer");
    static int rippleCountID = Shader.PropertyToID("rippleCount");
    static int forcesCountID = Shader.PropertyToID("forcesCount");
    static int _NoiseTexID = Shader.PropertyToID("_NoiseTex");
    static int colorMapID = Shader.PropertyToID("colorMap");
    static int dhfParamMapID = Shader.PropertyToID("dhfParamMap");
    static int typeMapID = Shader.PropertyToID("typeMap");
    static int terrainHeightMapID = Shader.PropertyToID("terrainHeightMap");
    static int terrainNormalMapID = Shader.PropertyToID("terrainNormalMap");
    static int terrainSizeID = Shader.PropertyToID("terrainSize");
    static int terrainChunkSizeID = Shader.PropertyToID("terrainChunkSize");
    static int terrainExpansionID = Shader.PropertyToID("terrainExpansion");
    static int terrainMapOffsetID = Shader.PropertyToID("terrainMapOffset");
    static int numTexturesID = Shader.PropertyToID("numTextures");
    static int numTexturesPctUVID = Shader.PropertyToID("numTexturesPctUV");
    //
    //instance props
    static int _chunkPosID = Shader.PropertyToID("_chunkPos");
    static int _instancePctID = Shader.PropertyToID("_instancePct");
    static int _instanceLodID = Shader.PropertyToID("_instanceLod");
    //
    //painting
    static int srsBrushParamsID = Shader.PropertyToID("srsBrushParams");
    static int clampRangeID = Shader.PropertyToID("clampRange");
    static int brushPosID = Shader.PropertyToID("brushPos");
    static int blendParamsID = Shader.PropertyToID("blendParams");
    static int tmpMapRTID = Shader.PropertyToID("tmpMapRT");


    static void ReleaseBuffers() {
        if(rippleBuffer != null) rippleBuffer.Release();
        if(forcesBuffer != null) forcesBuffer.Release();
        if(counterBuffer != null) counterBuffer.Release();
        rippleBuffer = null;
        counterBuffer = null;
        forcesBuffer = null;
        forcesArray = null;
        forceClassArray = null;
    }

    private void OnDestroy() {
        ReleaseAssets();

        ReleaseBuffers();

        BurstDisposal();
    }
}
