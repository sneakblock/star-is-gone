using System.Collections;
using System.Collections.Generic;
using EmeraldAI;
using UnityEngine;

public class SpeedModifier : MonoBehaviour
{

    public EmeraldAISystem sys;

    void Start()
    {
        sys.WalkSpeed *= 3;
        sys.RunSpeed *= 3;
        //sys.WalkBackwardsSpeed *= 3;
    }

    
}
