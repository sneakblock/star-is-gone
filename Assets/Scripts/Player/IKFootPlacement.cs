using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKFootPlacement : MonoBehaviour
{
    public Animator anim;

    public LayerMask layerMask;

    [Range (0, 1f)]
    public float distanceToGround;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnAnimatorIK(int layerIndex)
    {
        
        anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
        anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);
        anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
        anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);
        
        //left foot
        RaycastHit hit;
        Ray ray = new Ray(anim.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up, Vector3.down);
        //May need to add a layermask here if the character controller is behaving like a capusle collider.
        if (Physics.Raycast(ray, out hit, distanceToGround + 1f, layerMask))
        {
            //Must be replaced with a layer functionality later on.
            if (hit.transform.tag == "Walkable")
            {
                Vector3 footPosition = hit.point;
                footPosition.y += distanceToGround;
                anim.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
                anim.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation);
            }
        }
        
        //right foot
        ray = new Ray(anim.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up, Vector3.down);
        //May need to add a layermask here if the character controller is behaving like a capusle collider.
        if (Physics.Raycast(ray, out hit, distanceToGround + 1f, layerMask))
        {
            //Must be replaced with a layer functionality later on.
            if (hit.transform.tag == "Walkable")
            {
                Vector3 footPosition = hit.point;
                footPosition.y += distanceToGround;
                anim.SetIKPosition(AvatarIKGoal.RightFoot, footPosition);
                anim.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation);
            }
        }

    }
}
