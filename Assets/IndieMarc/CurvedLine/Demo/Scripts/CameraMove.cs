using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IndieMarc.CurvedLine
{
    public class CameraMove : MonoBehaviour
    {
        public float move_speed = 10f;

        void LateUpdate()
        {
            Vector3 move = Vector3.zero;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                move += Vector3.left;

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                move += Vector3.right;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                move += Vector3.forward;

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                move += Vector3.back;


            if (Input.GetKey(KeyCode.E))
                move += Vector3.up;

            if (Input.GetKey(KeyCode.Q))
                move += Vector3.down;

            move = move.normalized * Mathf.Min(move.magnitude, 1f);
            transform.position += move * move_speed * Time.deltaTime;
        }
    }

}