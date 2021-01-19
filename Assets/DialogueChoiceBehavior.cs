using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DialogueChoiceBehavior : MonoBehaviour
{
    private GameObject firstSelected;
    // Start is called before the first frame update
    void Start()
    {
        firstSelected = GameObject.Find("Option 1");
    }

    void SetSelected()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
