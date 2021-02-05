
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
//using UnityEngine.InputSystem;

public class ThirdPersonMovement : MonoBehaviour
{
    //private PlayerControls controls;
    public Animator anim;
    public CharacterController myCC;
    public Transform cam;

    private float speed = 3f;

    public float turnSmoothTime = 0.1f;

    private float turnSmoothVelocity;

    private bool isSneaking = false;

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
        if (Input.GetKeyDown(KeyCode.LeftControl) && !isSneaking)
        {
            isSneaking = true;
            anim.SetBool("isSneaking", true);
            //Debug.Log("Character is now sneaking.");
        } else if (Input.GetKeyDown(KeyCode.LeftControl) && isSneaking)
        {
            isSneaking = false;
            anim.SetBool("isSneaking", false);
            //Debug.Log("Character is no longer sneaking.");
        }
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        
        footstepSource.mute = true;

        fallVector = Vector3.zero;
        
        if (myCC.isGrounded == false)
        {
            fallVector += Physics.gravity;
            myCC.Move(fallVector * Time.deltaTime);
        }

        if (myCC.velocity.y < -9.8f)
        {
            anim.SetBool("isFalling", true);
        }
        else
        {
            anim.SetBool("isFalling", false);
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        

        if (isRunning)
        {
            anim.SetBool("isRunning", true);
            anim.SetBool("isSneaking", false);
            //Debug.Log("Character is no longer sneaking.");
            footstepSource.pitch = 2.5f;
            isSneaking = false;
            speed = 5.5f;
        }
        else if (!isRunning && !isSneaking)
        {
            anim.SetBool("isRunning", false);
            anim.SetBool("isSneaking", false);
            //Debug.Log("Character is no longer sneaking.");
            footstepSource.pitch = 1.3f;
            speed = 2.5f;
        } else if (!isRunning && isSneaking)
        {
            anim.SetBool("isSneaking", true);
            //Debug.Log("Character is now sneaking.");
            footstepSource.pitch = 1.2f;
            speed = 1.3f;
        }

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
