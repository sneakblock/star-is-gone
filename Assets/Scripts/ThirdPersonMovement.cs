
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    public Animator anim;
    public CharacterController myCC;
    public Transform cam;

    public float speed = 6f;

    public float turnSmoothTime = 0.1f;

    private float turnSmoothVelocity;

    public Vector3 fallVector;

    public AudioSource footstepSource;

    void Start()
    {
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        anim.SetBool("isMoving", false);
        footstepSource.mute = true;

        fallVector = Vector3.zero;
        
        if (myCC.isGrounded == false)
        {
            fallVector += Physics.gravity;
            myCC.Move(fallVector * Time.deltaTime);
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        
        
        if (direction.magnitude >= 0.1f)
        {

            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            if (moveDir != Vector3.zero)
            {
                anim.SetBool("isMoving", true);
                footstepSource.mute = false;
            }
            myCC.Move(moveDir.normalized * speed * Time.deltaTime);
                
        }
    }
}
