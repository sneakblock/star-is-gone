using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomPos : MonoBehaviour
{
    [SerializeField] Rigidbody targetBody;
    [SerializeField] float radius;
    [SerializeField] float speed;
    Vector3 initPos;


    void Start()
    {
        initPos = transform.position;
    }

    void FixedUpdate()
    {
        // Move Kinematic Rigidbody
        targetBody.MovePosition(targetBody.position + (transform.position - targetBody.position) * speed);

        // Next Random Point
        if (Vector3.Distance(targetBody.position, transform.position) <= 0.1f)
            transform.position = initPos + Random.insideUnitSphere * radius;
    }
}
