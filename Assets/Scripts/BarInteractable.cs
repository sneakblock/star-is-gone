using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class BarInteractable : AbstractInteractable
{
    public override void TriggerBehavior(GameObject obj)
    {
        obj.GetComponent<BarEvent>().StartEvent();
    }
}
