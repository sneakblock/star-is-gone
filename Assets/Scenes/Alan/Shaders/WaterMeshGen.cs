using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class WaterMeshGen : MonoBehaviour
{
    [Range(1, 100)]
    public int resolution;

    Vector3[] vertices;
    int[] triangles;
    int xSize, ySize;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void UpdateMesh()
    {
        vertices = new Vector3[(xSize + 1) * (ySize + 1)];
        triangles = new int[xSize * ySize * 6];

        Mesh m = new Mesh();
        m.name = "grid";

        for (int index = 0, j = 0; j <= ySize; j++)
        {
            for (float i = 0; i <= xSize; i++, index++)
            {
                vertices[index] = new Vector3(i / (float)xSize, 0, j / (float)ySize);
            }
        }

        for (int j = 0, t = 0, v = 0; j < ySize; j++, v++)
        {
            for (int i = 0; i < xSize; i++, t += 6, v++)
            {
                triangles[t + 0] = v + 0;
                triangles[t + 1] = v + xSize + 1;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1;
                triangles[t + 4] = v + xSize + 1;
                triangles[t + 5] = v + xSize + 2;
            }
        }

        m.vertices = vertices;
        m.triangles = triangles;

        m.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = m;
    }

    /*
    private void OnDrawGizmos()
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 temp = new Vector3(
                vertices[i].x,
                vertices[i].y,
                vertices[i].z
                );
            temp.Scale(transform.localScale);
            Gizmos.DrawSphere(temp + transform.position, 0.03f);
        }
    }*/

    // Update is called once per frame
    void Update()
    {
        xSize = resolution;
        ySize = resolution;

        if (vertices.Length != (xSize + 1) * (ySize + 1))
        {
            UpdateMesh();
        }
    }
}
