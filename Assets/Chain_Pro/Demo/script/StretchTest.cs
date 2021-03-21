using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StretchTest : MonoBehaviour
{

    [SerializeField] Transform Joint_A;
    [SerializeField] Transform Joint_B;
    [SerializeField] float range = 0.5f;
    [SerializeField] float offset = 0f;
    [SerializeField] float speed;
    Transform pointA;
    Transform pointB;


    void Start()
    {
        pointA = transform.GetChild(0);
        pointB = transform.GetChild(1);
    }

    void Update()
    {
        pointA.localPosition = new Vector3(0f, 0f, offset/2f + range * Mathf.Sin(Time.time * speed));
        pointB.localPosition = new Vector3(0f, 0f, -offset/2f + range * Mathf.Sin(Time.time * speed + Mathf.PI));

        Joint_A.position = pointA.position;
        Joint_B.position = pointB.position;
    }
}
