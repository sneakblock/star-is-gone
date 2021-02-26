using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InstanceExit : MonoBehaviour
{

    Collider collider;
    PuzzleUI puzzleUI;
    // Start is called before the first frame update
    void Start()
    {
        puzzleUI = GameObject.FindGameObjectWithTag("puzzleui").GetComponent<PuzzleUI>();
        collider = GameObject.FindGameObjectWithTag("collidable").GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnCollisionEnter(Collider collider)
    {
        if (puzzleUI.hasBeenTriggered && collider.gameObject.tag == "Player")
        {
            Debug.Log("Collision Detected!");
            SceneManager.LoadScene("Overworld_Avery");
        }
    }
}
