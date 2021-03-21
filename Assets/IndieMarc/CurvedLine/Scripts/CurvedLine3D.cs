using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IndieMarc.CurvedLine
{
    public enum AxisType
    {
        AxisX,
        AxisY,
        AxisZ,
        Auto
    }

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class CurvedLine3D : MonoBehaviour
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

        [Header("Mesh")]
        public AxisType axis_ref = AxisType.Auto;
        public Material material;
        [Range(3, 100)]
        public int mesh_precision = 10;
        public float radius = 1f;
        public float max_length = 0f; //0 means infinite
        public bool debug_mesh = false;

        [Header("Refresh & Optim")]
        [Tooltip("Turn this off you have performance issues")]
        public bool auto_refresh_editor = true;
        public bool auto_refresh_playmode = true;
        public float refresh_rate = 0.01f;
        public bool frustum_cull_playmode = true;
        public float frustum_cull_radius = 1f;

        private MeshRenderer render;
        private MeshFilter mesh;
        private float refresh_timer = 0f;

        private List<Vector3> oripaths = new List<Vector3>();
        private List<Vector3> subpaths = new List<Vector3>();

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

            if (subpaths.Count < 2)
                return;
            
            //Calculate total distance
            float total_dist = 0f;
            float max_distance = max_length > 0.01f ? max_length : float.MaxValue;
            Vector3 prev = subpaths[0];
            int nb_points_max = 0;
            for (int i = 1; i < subpaths.Count; i++)
            {
                Vector3 pos1 = subpaths[i];
                total_dist += (pos1 - prev).magnitude;
                prev = pos1;
                if (total_dist <= max_distance)
                    nb_points_max = i;
            }

            //Resize array to new mesh size
            int mesh_dots = mesh_precision + 1; //+1 because first is at same position than last
            int varray_length = mesh_dots * nb_points_max + 2;
            int tarray_length = mesh_dots * nb_points_max * 6 + mesh_precision * 6;
            if(varray_length != vertices.Length)
                vertices = new Vector3[varray_length];
            if (tarray_length != triangles.Length)
                triangles = new int[tarray_length];
            if (varray_length != normals.Length)
                normals = new Vector3[varray_length];
            if (varray_length != uv.Length)
                uv = new Vector2[varray_length];

            Vector3 refdir = transform.up;
            if (axis_ref == AxisType.AxisX)
                refdir = transform.right;
            if (axis_ref == AxisType.AxisZ)
                refdir = transform.forward;
            if (axis_ref == AxisType.Auto) {
                Vector3 dir1 = (subpaths[1] - subpaths[0]).normalized;
                refdir = Vector3.Cross(dir1, Vector3.one - dir1).normalized;
            }

            Vector3 prev_refdir = refdir;
            
            //Draw the main line
            float dist_traveled = 0f;
            total_dist = Mathf.Min(total_dist, max_distance);
            int last_index = 0;
            for (int i = 0; i < nb_points_max; i++)
            {
                Vector3 pos1 = subpaths[i];
                Vector3 dir = Vector3.up;
                if (i < subpaths.Count - 1)
                {
                    Vector3 pos2 = subpaths[i + 1];
                    dir = pos2 - pos1;
                }
                else
                {
                    Vector3 pos2 = subpaths[i - 1];
                    dir = pos1 - pos2;
                }
                
                Vector3 dir_norm = dir.normalized;
                Vector3 dref = axis_ref == AxisType.Auto ? Vector3.Cross(prev_refdir, dir_norm) : refdir;
                Vector3 perpend = Vector3.Cross(dir_norm, dref).normalized;
                
                if (Vector3.Dot(perpend, prev_refdir) < 0f)
                    perpend = -perpend;

                prev_refdir = perpend; //New ref for next segment

                float percent_traveled = dist_traveled / total_dist;
                Vector3 lossy_scale = transform.lossyScale;
                Transform obj_trans = transform;

                float angle1; Vector3 perp1;
                Vector3 pos_local; Vector3 dir_local; Vector2 uvvect;
                int vindex; int vnext;

                for (int a = 0; a < mesh_dots; a++)
                {
                    angle1 = a * 360f / (float)(mesh_precision);
                    perp1 = Quaternion.AngleAxis(angle1, dir_norm) * perpend;

                    pos_local = obj_trans.InverseTransformPoint(pos1 + perp1 * radius);
                    pos_local = Vector3.Scale(pos_local, lossy_scale);
                    dir_local = obj_trans.InverseTransformDirection(perp1);
                    uvvect.x = percent_traveled;
                    uvvect.y = angle1 / 360f;

                    vindex = i * mesh_dots + a;
                    vnext = i * mesh_dots + a + 1;

                    vertices[vindex] = pos_local;
                    normals[vindex] = dir_local;
                    uv[vindex] = uvvect;
                    
                    //Will there be a next row of vertices?
                    if (i < nb_points_max - 1  && a < mesh_dots - 1)
                    {
                        triangles[vindex * 6 + 0] = vindex;
                        triangles[vindex * 6 + 1] = vnext;
                        triangles[vindex * 6 + 2] = vindex + mesh_dots;
                        triangles[vindex * 6 + 3] = vnext;
                        triangles[vindex * 6 + 4] = vnext + mesh_dots;
                        triangles[vindex * 6 + 5] = vindex + mesh_dots;
                    }
                }

                dist_traveled += dir.magnitude;
                last_index = i;
            }

            //Draw the caps
            Vector3 pos_local1 = transform.InverseTransformPoint(subpaths[0]);
            Vector3 pos_local2 = transform.InverseTransformPoint(subpaths[last_index]);
            pos_local1 = new Vector3(pos_local1.x * transform.lossyScale.x, pos_local1.y * transform.lossyScale.y, pos_local1.z * transform.lossyScale.z);
            pos_local2 = new Vector3(pos_local2.x * transform.lossyScale.x, pos_local2.y * transform.lossyScale.y, pos_local2.z * transform.lossyScale.z);
            Vector3 norm1 = subpaths[0] - subpaths[1];
            Vector3 norm2 = subpaths[last_index] - subpaths[last_index - 1];
            int index_start = nb_points_max * mesh_dots;
            vertices[index_start + 0] = pos_local1;
            vertices[index_start + 1] = pos_local2;
            normals[index_start + 0] = transform.InverseTransformDirection(norm1);
            normals[index_start + 1] = transform.InverseTransformDirection(norm2);
            uv[index_start + 0] = new Vector2(0f, 0.5f);
            uv[index_start + 1] = new Vector2(1f, 0.5f);

            int indice_first = vertices.Length - 2;
            int indice_last = vertices.Length - 1;
            int index_last_row = vertices.Length - 3;

            int tri_index_first = index_start * 6;
            for (int a = 0; a < mesh_precision; a++)
            {
                triangles[tri_index_first + a * 6 + 0] = indice_first;
                triangles[tri_index_first + a * 6 + 1] = a + 1;
                triangles[tri_index_first + a * 6 + 2] = a;

                triangles[tri_index_first + a * 6 + 3] = indice_last;
                triangles[tri_index_first + a * 6 + 4] = index_last_row - a - 1;
                triangles[tri_index_first + a * 6 + 5] = index_last_row - a;
            }

            //Add vertices to mesh
            mesh.sharedMesh.vertices = vertices;
            mesh.sharedMesh.triangles = triangles;
            mesh.sharedMesh.normals = normals;
            mesh.sharedMesh.uv = uv;

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

            if (debug_mesh && paths != null && paths.Length >= 2)
            {
                for (int i = 0; i < mesh.sharedMesh.vertexCount; i++)
                {
                    Vector3 pos = transform.TransformPoint(mesh.sharedMesh.vertices[i]);
                    Vector3 dir = transform.TransformDirection(mesh.sharedMesh.normals[i]);
                    int loop_index = i % (mesh_precision + 1);
                    Debug.DrawRay(pos, dir * radius, loop_index == 0 || loop_index == mesh_precision ? Color.red : Color.green);
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

            if (mesh.sharedMesh == null)
            {
                mesh.sharedMesh = new Mesh();
                mesh.sharedMesh.name = "Line Mesh";
            }

            if (material != null)
            {
                if (render.sharedMaterial != material)
                    render.sharedMaterial = material;

                /*string texNameDefault = "_MainTex";
                if (render.sharedMaterial.GetTexture(texNameDefault))
                {
                    render.sharedMaterial.SetTextureScale(texNameDefault, new Vector2(material_stretch, render.sharedMaterial.GetTextureScale(texNameDefault).y));
                    render.sharedMaterial.SetTextureOffset(texNameDefault, new Vector2(material_offset, render.sharedMaterial.GetTextureOffset(texNameDefault).y));
                }*/
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