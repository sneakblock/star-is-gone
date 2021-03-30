using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IndieMarc.CurvedLine
{
    public enum ChainType
    {
        Align=0,
        Alternate=1,
    }

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class CurvedChain3D : MonoBehaviour
    {

        [Header("Path")]
        public Transform[] paths;
        
        [Header("Curve")]
        [Range(0, 5)]
        public int precision = 4;
        [Range(0.01f, 0.49f)]
        public float shape = 0.25f;
        public bool skip_extremities = false;
        public bool debug = true;

        [Header("Repeated Mesh")]
        public MeshFilter model_mesh;
        public Material material;
        public Vector3 model_offset = Vector3.zero;
        public Vector3 model_rotation = Vector3.zero;
        public Vector3 model_scale = Vector3.one;

        [Header("Chain")]
        public ChainType chain_type;
        public AxisType axis_ref = AxisType.AxisY;
        public float spacing = 1f;
        public int max_meshes = 0; //0 means infinite
        public bool debug_chain = false;

        [Header("Refresh & Optim")]
        [Tooltip("Turn this off you have performance issues")]
        public bool auto_refresh_editor = true;
        public bool auto_refresh_playmode = true;
        public float refresh_rate = 0.02f;
        public bool frustum_cull_playmode = true;
        public float frustum_cull_radius = 1f;

        private MeshRenderer render;
        private MeshFilter mesh;
        private float refresh_timer = 0f;

        private List<Vector3> oripaths = new List<Vector3>();
        private List<Vector3> subpaths = new List<Vector3>();
        
        private Vector3[] mesh_vertices;
        private Vector3[] mesh_normals;
        private Vector2[] mesh_uvs;
        private int[] mesh_triangles;

        private Vector3[] vertices = new Vector3[0];
        private int[] triangles = new int[0];
        private Vector3[] normals = new Vector3[0];
        private Vector2[] uv = new Vector2[0];

        void Awake()
        {
            RefreshAll();
        }
        
        private void CalculateCurvedPath()
        {
            oripaths.Clear();
            subpaths.Clear();

            if (paths == null || paths.Length < 2)
                return;

            //Add normal path
            foreach (Transform p in paths)
            {
                if(p != null)
                    oripaths.Add(p.position);
            }
            foreach (Transform p in paths)
            {
                if (p != null)
                    subpaths.Add(p.position);
            }
            
            //Subdivide the path
            if (paths.Length >= 2 && precision > 0)
                CalculateCurvedPathDiv(1);

            if (skip_extremities && paths.Length >= 4)
            {
                int max = Mathf.RoundToInt(Mathf.Pow(2, precision));
                for(int i=0; i< max; i++)
                    subpaths.RemoveAt(subpaths.Count - 1);
                for (int i = 0; i < max; i++)
                    subpaths.RemoveAt(0);
            }

            //Debug.Log("Calculated line " + gameObject.name + " "  + subpaths.Count);
        }

        private void CalculateCurvedPathDiv(int div)
        {
            List<Vector3> prev = new List<Vector3>(subpaths);
            subpaths.Clear();
            subpaths.Add(prev[0]);

            for (int i = 0; i < prev.Count - 1; i++)
            {
                Vector3 cur = prev[i];
                Vector3 next = prev[i + 1];
                Vector3 dir = next - cur;
                Vector3 newp1 = cur + dir * shape;
                Vector3 newp2 = cur + dir * (1f- shape);
                subpaths.Add(newp1);
                subpaths.Add(newp2);
            }

            subpaths.Add(prev[prev.Count - 1]);

            if (div < precision)
                CalculateCurvedPathDiv(div + 1);
        }

        //Creates the 3D mesh following the path
        private void BuildMesh()
        {
            mesh.sharedMesh.Clear();

            if (subpaths.Count < 2 || model_mesh == null)
                return;

            //Calculate total distance
            float total_dist = 0f;
            Vector3 prev = subpaths[0];
            for (int i = 1; i < subpaths.Count; i++)
            {
                Vector3 pos1 = subpaths[i];
                total_dist += (pos1 - prev).magnitude;
                prev = pos1;
            }

            Vector3 refdir = Vector3.up;
            if (axis_ref == AxisType.AxisX)
                refdir = Vector3.right;
            if (axis_ref == AxisType.AxisZ)
                refdir = Vector3.forward;

            //Store some values before the loop for faster process
            int max_meshes_count = max_meshes > 0 ? max_meshes : int.MaxValue;
            float average_scale = (transform.lossyScale.x + transform.lossyScale.y + transform.lossyScale.z) / 3f;

            int nb_chains = Mathf.CeilToInt(total_dist / (spacing * average_scale));
            nb_chains = Mathf.Min(nb_chains, max_meshes_count);
            int nb_mesh_vertices = mesh_vertices.Length;
            int nb_mesh_uvs = mesh_uvs.Length;
            int nb_mesh_triangles = mesh_triangles.Length;

            //Resize array to new mesh size
            int varray_length = nb_chains * nb_mesh_vertices;
            int tarray_length = nb_chains * nb_mesh_triangles;
            if (varray_length != vertices.Length)
                vertices = new Vector3[varray_length];
            if (tarray_length != triangles.Length)
                triangles = new int[tarray_length];
            if (varray_length != normals.Length)
                normals = new Vector3[varray_length];
            if (varray_length != uv.Length)
                uv = new Vector2[varray_length]; //Use vertices length in case no UV
            
            float dist_traveled = 0f;
            int chain_index = 0;
            int total_index = 0;
            float dist_traveled_last = 0f;
            Vector3 last_pos = subpaths[0];
            Vector3 last_ipos = subpaths[0];

            //Draw the main line
            for (int i = 0; i < subpaths.Count; i++)
            {
                Vector3 pos1 = subpaths[i];
                Vector3 pos2;
                Vector3 dir;
                if (i < subpaths.Count - 1)
                {
                    pos2 = subpaths[i + 1];
                    dir = pos2 - pos1;
                }
                else
                {
                    pos2 = subpaths[i - 1];
                    dir = pos1 - pos2;
                }

                if (debug_chain)
                    Debug.DrawRay(pos1, refdir * 0.6f, Color.red);

                
                float chain_space = spacing * average_scale * chain_index;
                
                if (dist_traveled >= chain_space && chain_index < nb_chains)
                {
                    float dist_added = dist_traveled - dist_traveled_last;
                    float dist_supposed = chain_space - dist_traveled_last;
                    float interp_val = dist_added > 0f ? Mathf.Clamp01(dist_supposed / dist_added) : 0f;
                    Vector3 ipos = last_pos * (1f - interp_val) + pos1 * interp_val;

                    if (debug_chain)
                        Debug.DrawRay(ipos, refdir * 0.5f, Color.green);
                    
                    //Set triangles
                    for (int t = 0; t < nb_mesh_triangles; t++)
                    {
                        int tri = mesh_triangles[t];
                        triangles[nb_mesh_triangles * chain_index + t] = total_index + tri;
                    }

                    Vector2 default_uv = new Vector2(dist_traveled / total_dist, 0f);
                    Vector3 pos_local = transform.InverseTransformPoint(ipos);
                    Vector3 dir_local = transform.InverseTransformDirection(dir.normalized);

                    Quaternion alternate_rot = Quaternion.identity;
                    if (chain_type == ChainType.Alternate && chain_index % 2 == 1)
                        alternate_rot = Quaternion.AngleAxis(90f, dir_local);
                    Quaternion mesh_rot = Quaternion.Euler(model_rotation.x, model_rotation.y, model_rotation.z);
                    Quaternion faceat_rot = Quaternion.LookRotation(dir_local, refdir);

                    //Set vertices, normals, uvs
                    Vector3 vertex; Vector3 normal;Vector3 vuv;
                    for (int v = 0; v < nb_mesh_vertices; v++)
                    {
                        //Translate/Rotate/Scale custom, then Rotate torward line and alternate rot
                        vertex = alternate_rot * faceat_rot * mesh_rot * Vector3.Scale(mesh_vertices[v] + model_offset, model_scale);
                        vertex += pos_local; //Move to position on the line

                        normal = mesh_normals[v];
                        vuv = (v < nb_mesh_uvs) ? mesh_uvs[v] : default_uv;

                        vertices[total_index] = vertex;
                        normals[total_index] = normal;
                        uv[total_index] = vuv;
                        total_index++;
                    }

                    chain_index++;
                    last_ipos = ipos;
                }

                dist_traveled_last = dist_traveled;
                last_pos = pos1;
                dist_traveled += dir.magnitude;
            }
            
            //Add vertices to mesh
            mesh.sharedMesh.vertices = (Vector3[]) vertices.Clone();
            mesh.sharedMesh.triangles = (int[])triangles.Clone();
            mesh.sharedMesh.normals = (Vector3[])normals.Clone();
            mesh.sharedMesh.uv = (Vector2[])uv.Clone();

            mesh.sharedMesh.RecalculateBounds();

        }
        
        void Update()
        {
            //Auto refresh
            if (IsAutoRefresh() && IsInFrustum() && paths != null && paths.Length >= 2)
            {
                refresh_timer += Time.deltaTime;
                if (refresh_timer > refresh_rate)
                {
                    refresh_timer = 0f;
                    Refresh();
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            //Draw debug lines
            if (debug && paths != null && paths.Length >= 2)
            {
                Vector3 prev = oripaths[0];
                for (int i = 1; i < oripaths.Count; i++)
                {
                    Debug.DrawLine(prev, oripaths[i], Color.blue);
                    prev = oripaths[i];
                }
                Vector3 prev2 = subpaths[0];
                for (int i = 1; i < subpaths.Count; i++)
                {
                    Debug.DrawLine(prev2, subpaths[i], Color.white);
                    prev2 = subpaths[i];
                }
            }
        }

        public void RefreshAll()
        {
            RefreshRenderer(); //Refresh renderer and mesh
            Refresh(); //Refresh the line
        }

        //Refresh the whole thing
        public void RefreshRenderer()
        {
            render = GetComponent<MeshRenderer>();
            mesh = GetComponent<MeshFilter>();
            
            if (model_mesh != null)
            {
                //Debug.Log("Model Mesh " + model_mesh.name + " (" + model_mesh.sharedMesh.triangles.Length + ")");
                mesh_vertices = (Vector3[]) model_mesh.sharedMesh.vertices.Clone();
                mesh_normals = (Vector3[]) model_mesh.sharedMesh.normals.Clone();
                mesh_uvs = (Vector2[]) model_mesh.sharedMesh.uv.Clone();
                mesh_triangles = (int[]) model_mesh.sharedMesh.triangles.Clone();
            }
            
            if (mesh.sharedMesh == null)
            {
                mesh.sharedMesh = new Mesh();
                mesh.sharedMesh.name = "Line Mesh";
            }

            if (material != null)
            {
                if (render.sharedMaterial != material)
                    render.sharedMaterial = material;
                
            }
        }

        //Refresh just the path (faster)
        public void Refresh()
        {
            CalculateCurvedPath();
            BuildMesh();
        }
        
        public void NewMesh()
        {
            mesh.sharedMesh = new Mesh();
            mesh.sharedMesh.name = "Line Mesh";
            RefreshAll();
        }

        //Called when value changed
        void OnValidate()
        {
            if (IsAutoRefresh())
                RefreshAll();
        }

        private bool IsInFrustum()
        {
            if (!Application.isPlaying)
                return true; //Editor mode, no culling

            if (!frustum_cull_playmode || Camera.main == null)
                return true; //Frustum test disabled

            bool in_frustum = false;
            foreach (Vector3 p in oripaths)
            {
                Vector3 view_point = Camera.main.WorldToViewportPoint(p);
                Vector3 view_point_dir = Camera.main.WorldToViewportPoint(p + Vector3.up * frustum_cull_radius);
                float dist = (view_point - view_point_dir).magnitude;
                if (view_point.x > -dist && view_point.y > -dist && view_point.x < (1f + dist) && view_point.y < (1f + dist) && view_point.z > -dist)
                    in_frustum = true;
            }
            return in_frustum;
        }

        public bool IsAutoRefresh()
        {
            if (!Application.isPlaying)
                return auto_refresh_editor;
            else
                return auto_refresh_playmode;
        }

        public List<Vector3> GetOriginalPath()
        {
            return oripaths;
        }

        public List<Vector3> GetCurvedPath()
        {
            return subpaths;
        }
    }

}