using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShaderLerp : MonoBehaviour {

	float handler = 0f;
	public bool grow = false;
	public bool dissolve = false;

	
	public float secondDuration = 3f;
	
	// Update is called once per frame
	void Update () 
	{
		if (grow)
		{	
			float fragmentation = 1	 / secondDuration;
			handler += fragmentation * Time.deltaTime;
			
			if (handler > 1)
				grow = false;
		}

		if (dissolve)
		{
			float fragmentation = -1 / secondDuration;
			handler += fragmentation * Time.deltaTime;
			
			if (handler < 0)
				dissolve = false;
		}

        gameObject.GetComponent<SkinnedMeshRenderer>().material.SetFloat("_DissolveAmount", handler);
		// Shader.SetGlobalFloat ("_Dissolve", handler);
	}

	public void Grow ()
	{
		handler = 1;
		dissolve = true;
		grow = false;
	}

	public void Dissolve ()
	{
		handler = 0;
		dissolve = false;
		grow = true;
	}
}
