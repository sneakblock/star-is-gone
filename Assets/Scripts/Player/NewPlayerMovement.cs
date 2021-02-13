
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewPlayerMovement : MonoBehaviour
{
    private Vector2 leftStickMove;
    private Vector2 rightStickMove;
    private PlayerControls controls;
    public Animator anim;
    public CharacterController myCC;
    public Transform cam;

    private float speed = 3f;

    public float turnSmoothTime = 0.1f;

    private float turnSmoothVelocity;

    private bool isSneaking = false;

    public Vector3 fallVector;

    public AudioSource footstepSource;

    private Vector3 worldDirection;

    void Awake()
    {
        Cursor.visible = false;
        controls = new PlayerControls();
        controls.Standard.Move.performed += ctx => leftStickMove = ctx.ReadValue<Vector2>();
        controls.Standard.Move.canceled += ctx => leftStickMove = Vector2.zero;
        controls.Standard.RotateCamera.performed += ctx => rightStickMove = ctx.ReadValue<Vector2>();
        controls.Standard.RotateCamera.canceled += ctx => rightStickMove = Vector2.zero;
        controls.Standard.Sneak.performed += ctx => isSneaking = !isSneaking;
        controls.Standard.Sprint.performed += ctx => leftStickMove *= 3;
        controls.Standard.Sprint.canceled += ctx => leftStickMove /= 3;
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    // Update is called once per frame
    void Update()
    {

        anim.SetBool("isMoving", false);

        if (isSneaking)
        {
            anim.SetBool("isSneaking", true);
        } else if (!isSneaking)
        {
            anim.SetBool("isSneaking", false);
        }
        
        //bool isRunning = (Math.Abs(leftStickMove.x) > Math.Abs(.75) || Math.Abs(leftStickMove.y) > Math.Abs(.75));
        bool isRunning = (new Vector2(leftStickMove.x, leftStickMove.y).magnitude > .75f);
        
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

        //float horizontal = Input.GetAxisRaw("Horizontal");
        //float vertical = Input.GetAxisRaw("Vertical");
        

        if (isRunning)
        {
            anim.SetBool("isRunning", true);
            anim.SetBool("isSneaking", false);
            footstepSource.pitch = 2.5f;
            isSneaking = false;
            speed = 5.5f;
        }
        else if (!isSneaking)
        {
            anim.SetBool("isRunning", false);
            anim.SetBool("isSneaking", false);
            footstepSource.pitch = 1.3f;
            speed = 2.5f;
        } else if (isSneaking)
        {
            anim.SetBool("isSneaking", true);
            footstepSource.pitch = 1.2f;
            speed = 1.3f;
        }

        //Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        
        worldDirection = new Vector3(leftStickMove.x, 0, leftStickMove.y);
        
        if (worldDirection.magnitude >= 0.1f)
        {
            //Debug.Log("Detecting stick input of " + leftStickMove);
            //Debug.Log("Attempting to move CC along vector" + worldDirection);

            float targetAngle = Mathf.Atan2(worldDirection.x, worldDirection.z) * 
                Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, 
                targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            if (moveDir != Vector3.zero)
            {
                anim.SetBool("isMoving", true);
                footstepSource.mute = false;
            }
            myCC.Move(moveDir.normalized * (speed * Time.deltaTime));
                
        }
    }
}
