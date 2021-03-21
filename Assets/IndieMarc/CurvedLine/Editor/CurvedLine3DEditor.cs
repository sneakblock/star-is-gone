using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace IndieMarc.CurvedLine
{

    [CustomEditor(typeof(CurvedLine3D)), CanEditMultipleObjects]
    public class CurvedLine3DEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            CurvedLine3D myScript = target as CurvedLine3D;
            
            DrawDefaultInspector();

            if (GUILayout.Button("Refresh Now"))
            {
                myScript.RefreshAll();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUIStyle style = new GUIStyle();
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField("Create Unique Mesh", style);
            EditorGUILayout.LabelField("Click here to make this mesh unique\nThis is useful after dupplicating an object\notherwise the 2 objects will be linked\nto the same mesh.", GUILayout.Height(60));
            //GUI.Label(new Rect(10, 10, 100, 100), "This is the first line.\nThis is the second line.");

            if (GUILayout.Button("New Mesh Instance"))
            {
                myScript.NewMesh();
            }

            EditorGUILayout.Space();
        }

    }

}
