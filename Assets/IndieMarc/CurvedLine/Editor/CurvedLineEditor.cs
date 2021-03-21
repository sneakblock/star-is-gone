using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace IndieMarc.CurvedLine
{

    [CustomEditor(typeof(CurvedLine2D)), CanEditMultipleObjects]
    public class CurvedLineEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            CurvedLine2D myScript = target as CurvedLine2D;
            
            DrawDefaultInspector();
            if (GUILayout.Button("Refresh Now"))
            {
                myScript.RefreshAll();
            }

            EditorGUILayout.Space();
        }

    }

}
