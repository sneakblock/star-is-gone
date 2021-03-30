using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IndieMarc.CurvedLine
{

    [ExecuteInEditMode]
    [RequireComponent(typeof(LineRenderer))]
    public class CurvedLine2D : MonoBehaviour
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

        [Header("Renderer")]
        public Material material;
        public float width = 1f;
        public bool auto_scale_texture = false;

        [Header("Refresh & Optim")]
        [Tooltip("Turn this off you have performance issues")]
        public bool auto_refresh_editor = true;
        public bool auto_refresh_playmode = true;
        public float refresh_rate = 0.01f;
        public bool frustum_cull_playmode = true;
        public float frustum_cull_radius = 1f;

        private LineRenderer render;
        private Material mat;
        private float refresh_timer = 0f;

        private List<Vector3> oripaths = new List<Vector3>();
        private List<Vector3> subpaths = new List<Vector3>();

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

        //Called when value changed
        void OnValidate()
        {
            if(IsAutoRefresh())
                RefreshAll();
        }

        public void RefreshAll()
        {
            RefreshRenderer(); //Refresh renderer
            Refresh(); //Refresh the line
        }

        //Refresh the whole thing
        public void RefreshRenderer()
        {
            render = GetComponent<LineRenderer>();

            if (material != null)
            {
                mat = new Material(material);
                render.sharedMaterial = mat;
            }

            render.widthMultiplier = width;
            render.useWorldSpace = true;
        }

        //Refresh just the path (faster)
        public void Refresh()
        {
            CalculateCurvedPath();
            float length = GetLength();

            if (render.sharedMaterial && auto_scale_texture)
            {
                Vector2 mat_scale = material.mainTextureScale; //Original mat
                render.sharedMaterial.mainTextureScale = new Vector2(mat_scale.x * length, mat_scale.y);
            }

            render.positionCount = subpaths.Count;
            for (int i = 0; i < subpaths.Count; i++)
            {
                render.SetPosition(i, subpaths[i]);
            }
        }

        private float GetLength() {
            float length = 0f;
            for (int i = 1; i < subpaths.Count; i++)
            {
                length += (subpaths[i] - subpaths[i-1]).magnitude;
            }
            return length;
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