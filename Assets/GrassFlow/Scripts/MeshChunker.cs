using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GrassFlow {

    public class MeshChunker {


        class MeshChunkData {
            public List<int> tris = new List<int>();
            public Vector3[] verts;
            public Vector3[] normals;
            public Vector2[] uvs;
        }


        [System.Serializable]
        public class MeshChunk {
            public Mesh mesh;
            public Bounds worldBounds;
            public Bounds meshBounds;
            public Vector4 chunkPos;
            public MaterialPropertyBlock propertyBlock;

            public uint instanceCount {
                get { return indirectArgsArr[1]; }
                set {
                    if(indirectArgsArr[1] != value) {
                        indirectArgsArr[1] = value;
                        indirectArgs.SetData(indirectArgsArr);
                    }
                }
            }
            public uint[] indirectArgsArr;
            [System.NonSerialized] public ComputeBuffer indirectArgs;

            public void SetIndirectArgs() {
                //documentation for indirect args is seriously lacking so a lot of this doesnt make a ton of sense
                //pretty sure most of these indexes are not important considering i know for a fact the meshes in question dont have submeshes
                //but may as well set it up properly for future reference
                indirectArgsArr = new uint[] {
                mesh.GetIndexCount(0), //index count per instance
                0, //instance count, placeholder for now
                mesh.GetIndexStart(0), //start index location

#if UNITY_2018_1_OR_NEWER
                mesh.GetBaseVertex(0), //base vertex location
#else
                0, //base vertex location
#endif

                0 //start instance location
            };

                //not at all sure why count is applied to the size stride rather than, you know, the count slot. but whatever.
                indirectArgs = new ComputeBuffer(1, indirectArgsArr.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                //dont bother setting data here because it will be needed to be set later when rendering anyway
            }
        }

        static float map(float value, float from1, float to1, float from2, float to2) {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }


        public static MeshChunk[] Chunk(Mesh meshToChunk, int xChunks, int yChunks, int zChunks, int subdiv, float bladeHeight) {
            Bounds meshBounds = meshToChunk.bounds;
            if(subdiv < 0) subdiv = 0;
            subdiv += 1;

            int[] tris = meshToChunk.triangles;
            Vector3[] verts = meshToChunk.vertices;
            Vector3[] norms = meshToChunk.normals;
            Vector2[] uvs = meshToChunk.uv;

            MeshChunkData[,,] meshChunks = new MeshChunkData[xChunks, yChunks, zChunks];
            List<MeshChunk> resultChunks = new List<MeshChunk>();

            for(int cx = 0; cx < xChunks; cx++) {
                for(int cy = 0; cy < yChunks; cy++) {
                    for(int cz = 0; cz < zChunks; cz++) {
                        meshChunks[cx, cy, cz] = new MeshChunkData();
                    }
                }
            }

            int[] thisTris = new int[(subdiv + 1) * 3];
            for(int i = 0; i < tris.Length; i += 3) {

                int t1 = tris[i]; int t2 = tris[i + 1]; int t3 = tris[i + 2];
                Vector3 checkVert = verts[t3];

                int xIdx = (int)(map(checkVert.x, meshBounds.min.x, meshBounds.max.x, 0f, 0.99999f) * xChunks);
                int yIdx = (int)(map(checkVert.y, meshBounds.min.y, meshBounds.max.y, 0f, 0.99999f) * yChunks);
                int zIdx = (int)(map(checkVert.z, meshBounds.min.z, meshBounds.max.z, 0f, 0.99999f) * zChunks);

                MeshChunkData chunk = meshChunks[xIdx, yIdx, zIdx];


                for(int sub = 0; sub < thisTris.Length; sub += 3) {
                    thisTris[sub] = t1;
                    thisTris[sub + 1] = t2;
                    thisTris[sub + 2] = t3;
                }

                chunk.tris.AddRange(thisTris);
            }

            int meshIdx = 0;
            Dictionary<int, int> triMap = new Dictionary<int, int>();
            for(int cx = 0; cx < xChunks; cx++) {
                for(int cy = 0; cy < yChunks; cy++) {
                    for(int cz = 0; cz < zChunks; cz++) {
                        triMap.Clear();
                        MeshChunkData chunk = meshChunks[cx, cy, cz];

                        var distinctTriIndexes = chunk.tris.Distinct().ToArray();

                        chunk.verts = new Vector3[distinctTriIndexes.Length];
                        chunk.normals = new Vector3[distinctTriIndexes.Length];
                        chunk.uvs = uvs.Length > 0 ? new Vector2[distinctTriIndexes.Length] : new Vector2[0];

                        for(int i = 0; i < distinctTriIndexes.Length; i++) {
                            int distinctTriIdx = distinctTriIndexes[i];
                            triMap.Add(distinctTriIdx, i);
                            chunk.verts[i] = verts[distinctTriIdx];
                            chunk.normals[i] = norms[distinctTriIdx];

                            if(uvs.Length > 0) {
                                chunk.uvs[i] = uvs[distinctTriIdx];
                            }
                        }
                        int[] remappedTris = new int[chunk.tris.Count];
                        for(int i = 0; i < remappedTris.Length; i++) {
                            remappedTris[i] = triMap[chunk.tris[i]];
                        }

                        if(chunk.verts.Length > 0) {

                            Mesh cMesh = new Mesh();
                            cMesh.vertices = chunk.verts;
                            cMesh.normals = chunk.normals;
                            cMesh.uv = chunk.uvs;
                            cMesh.SetTriangles(remappedTris, 0, true);
                            cMesh.RecalculateTangents();

                            resultChunks.Add(new MeshChunk() {
                                mesh = cMesh,
                                meshBounds = cMesh.bounds,
                                propertyBlock = new MaterialPropertyBlock()
                            });
                        }
                    }
                }
            }

            MeshChunk[] finalChunks = resultChunks.ToArray();

            ExpandChunks(finalChunks, bladeHeight);

            return finalChunks;
        }


        public static MeshChunk[] ChunkTerrain(Terrain terrainObj, int xChunks, int zChunks, int bladesPerChunk, float expandAmnt, float bladeHeight) {
            TerrainData terrain = terrainObj.terrainData;
            Vector3 terrainScale = terrain.size;

            Vector3 chunkSize = new Vector3(terrainScale.x / xChunks, terrainScale.y * 0.5f, terrainScale.z / zChunks);
            Vector3 halfChunkSize = chunkSize * 0.5f;

            MeshChunk[] chunks = new MeshChunk[xChunks * zChunks];

            int w = terrain.heightmapResolution - 1;
            int h = terrain.heightmapResolution - 1;
            float cWf = w / (float)xChunks;
            float cHf = h / (float)zChunks;
            int cW = (int)cWf;
            int cH = (int)cHf;

            Mesh cMesh = new Mesh();
            cMesh.vertices = new Vector3[] { Vector3.zero };
            cMesh.SetTriangles(new int[bladesPerChunk * 3], 0, false);

            int index = 0;
            for(int z = 0; z < zChunks; z++) {
                for(int x = 0; x < xChunks; x++) {
                    float[,] tHeights = terrain.GetHeights((int)(cWf * x), (int)(cHf * z), cW, cH);
                    float maxHeight = 0;
                    float minHeight = 1;
                    foreach(float tH in tHeights) {
                        if(tH > maxHeight)
                            maxHeight = tH;
                        if(tH < minHeight)
                            minHeight = tH;
                    }

                    MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
                    Vector3 chunkPos = Vector3.Scale(chunkSize, new Vector3(x, 0, z));
                    Vector3 mapChunkPos = new Vector4(chunkPos.x, chunkPos.z);
                    propBlock.SetVector("_chunkPos", mapChunkPos);

                    chunkPos += halfChunkSize;
                    chunkPos.y = chunkSize.y * (maxHeight + minHeight);

                    halfChunkSize.y = chunkSize.y * (maxHeight - minHeight);

                    chunks[index++] = new MeshChunk() {
                        meshBounds = new Bounds() {
                            center = chunkPos,
                            extents = halfChunkSize
                        },
                        worldBounds = new Bounds() {
                            extents = halfChunkSize
                        },
                        chunkPos = mapChunkPos,
                        propertyBlock = propBlock,
                        mesh = cMesh
                    };
                }
            }

            ExpandChunks(chunks, 1f + expandAmnt, bladeHeight);

            return chunks;
        }

        public static void ExpandChunks(MeshChunk[] chunks, float expandAmount, float bladeHeight) {
            Vector3 bladeBoundsExpand = new Vector3(bladeHeight, bladeHeight, bladeHeight);

            foreach(MeshChunk chunk in chunks) {
                Vector3 extents = chunk.meshBounds.extents;
                extents.x *= expandAmount;
                extents.z *= expandAmount;
                extents.y = chunk.meshBounds.extents.y;

                chunk.meshBounds.extents = extents + bladeBoundsExpand;
                chunk.worldBounds.extents = chunk.meshBounds.extents;
                //chunk.mesh.bounds = chunk.meshBounds;
            }

        }

        public static void ExpandChunks(MeshChunk[] chunks, float bladeHeight) {
            Vector3 bladeBoundsExpand = new Vector3(bladeHeight, bladeHeight, bladeHeight);

            foreach(MeshChunk chunk in chunks) {
                chunk.meshBounds.extents += bladeBoundsExpand;
            }
        }

        static int UVtoIdx(int w, int h, Vector2 uv) {
            return Mathf.Clamp(Mathf.RoundToInt(uv.x * w) + Mathf.RoundToInt(uv.y * h) * w, 0, w * h - 1);
        }

        const float densityThresh = 0.05f;
        const float byte255to01 = 0.0039215686274509803921568627451f;

        public static Mesh BakeDensityToMesh(Mesh baseMesh, Texture2D detailMap) {
            Vector2[] baseUvs = baseMesh.uv;

            if(baseUvs.Length == 0) {
                Debug.LogError("GrassFlow:BakeDensityToMesh: Base mesh does not have uvs!");
            }

            Color32[] pixels = detailMap.GetPixels32();

            int width = detailMap.width;
            int height = detailMap.height;

            List<int> filledTris = new List<int>();

            int[] baseTris = baseMesh.triangles;
            Vector3[] baseNorms = baseMesh.normals;
            Vector3[] baseVerts = baseMesh.vertices;


            for(int i = 0; i < baseTris.Length; i += 3) {
                int[] thisTri = new int[] { baseTris[i], baseTris[i + 1], baseTris[i + 2] };
                Vector2 uv1 = baseUvs[thisTri[0]]; Vector2 uv2 = baseUvs[thisTri[1]]; Vector2 uv3 = baseUvs[thisTri[2]];

                float densityAcc = 0f;

                densityAcc += pixels[UVtoIdx(width, height, uv1)].r;
                densityAcc += pixels[UVtoIdx(width, height, uv2)].r;
                densityAcc += pixels[UVtoIdx(width, height, uv3)].r;

                uv1 = Vector2.LerpUnclamped(uv1, uv2, 0.5f);
                uv2 = Vector2.LerpUnclamped(uv2, uv3, 0.5f);
                uv3 = Vector2.LerpUnclamped(uv1, uv3, 0.5f);

                densityAcc += pixels[UVtoIdx(width, height, uv1)].r;
                densityAcc += pixels[UVtoIdx(width, height, uv2)].r;
                densityAcc += pixels[UVtoIdx(width, height, uv3)].r;

                if(densityAcc * byte255to01 > densityThresh) {
                    filledTris.AddRange(thisTri);
                }
            }

            var distinctTriIndexes = filledTris.Distinct().ToArray();

            Vector3[] verts = new Vector3[distinctTriIndexes.Length];
            Vector3[] norms = new Vector3[distinctTriIndexes.Length];
            Vector2[] uvs = new Vector2[distinctTriIndexes.Length];

            Dictionary<int, int> triMap = new Dictionary<int, int>();
            for(int i = 0; i < distinctTriIndexes.Length; i++) {
                int distinctTriIdx = distinctTriIndexes[i];
                triMap.Add(distinctTriIdx, i);

                verts[i] = baseVerts[distinctTriIdx];
                norms[i] = baseNorms[distinctTriIdx];
                uvs[i] = baseUvs[distinctTriIdx];
            }

            int[] remappedTris = new int[filledTris.Count];
            for(int i = 0; i < remappedTris.Length; i++) {
                remappedTris[i] = triMap[filledTris[i]];
            }


            Mesh cMesh = new Mesh();
            cMesh.vertices = verts;
            cMesh.normals = norms;
            cMesh.uv = uvs;
            cMesh.triangles = remappedTris;
            cMesh.RecalculateBounds();
            cMesh.RecalculateTangents();

            return cMesh;
        }


    }//class
}//namespace