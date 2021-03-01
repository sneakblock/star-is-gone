using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    //private Animator anim;
    public string sceneName;
    // Start is called before the first frame update
    void Start()
    {
        //anim = GameObject.FindWithTag("transitioncanvas").GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
