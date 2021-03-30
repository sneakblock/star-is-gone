using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class JointBreaker : MonoBehaviour
{

    [SerializeField] LayerMask layerMaskChain;

    RaycastHit rHit;
    Ray ray;
    Joint joint;
    MeshRenderer mRendChain;


    void Update()
    {
        Vector3 screenPos = Input.mousePosition;
        ray = Camera.main.ScreenPointToRay(screenPos);


        // Select
        if (Physics.SphereCast(ray, 0.015f, out rHit, 10f, layerMaskChain))
        {
            joint = rHit.collider.GetComponent<Joint>();
            if (joint == null)
                return;

            MeshRenderer mRend = joint.GetComponent<MeshRenderer>();
            if (mRend != mRendChain)
            {
                if (mRendChain != null)
                    mRendChain.material.SetColor("_EmissionColor", Color.black);
                mRendChain = mRend;
                mRend.material.EnableKeyword("_EMISSION");
                mRendChain.material.SetColor("_EmissionColor", Color.yellow);
            }
        }
        else
        {
            if(mRendChain != null)
                mRendChain.material.SetColor("_EmissionColor", Color.black);
        }


        // Break
        if (Input.GetMouseButtonDown(0))
        {
            if (joint == null)
                return;

            joint.breakForce = 0f;
            joint.GetComponent<Rigidbody>().WakeUp();
        }
    }
}
