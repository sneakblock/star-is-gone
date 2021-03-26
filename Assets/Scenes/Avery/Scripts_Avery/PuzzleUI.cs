using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleUI : MonoBehaviour
{
    public Button button1;
    public Button button2;
    public Button button3;
    public Button button4;
    public ArrayList solutions;
    public ArrayList input;
    public Canvas canvas;
    public bool hasBeenTriggered = false;
    public int counter = 0;
    bool matchingArrays;
    public GameObject enemy;
    public GameObject exitArtifact;
    public GameObject behaviorToBeTriggered;

    // Start is called before the first frame update
    void Start()
    {
        canvas = GetComponentInChildren<Canvas>();
        canvas.enabled = false;
        button1 = GameObject.FindGameObjectWithTag("button1").GetComponent<Button>();
        button2 = GameObject.FindGameObjectWithTag("button2").GetComponent<Button>();
        button3 = GameObject.FindGameObjectWithTag("button3").GetComponent<Button>();
        button4 = GameObject.FindGameObjectWithTag("button4").GetComponent<Button>();
        hasBeenTriggered = false;
        input = new ArrayList();
        solutions = new ArrayList() { 1, 4, 2, 3 };
        enemy = GameObject.FindGameObjectWithTag("AI");
        enemy.SetActive(true);
        exitArtifact = GameObject.FindGameObjectWithTag("ExitArtifact");
        exitArtifact.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (counter == 4)
        {
            Debug.Log(input[0] + " " + solutions[0]);
            matchingArrays = input[0].Equals(solutions[0]);
            Debug.Log(matchingArrays);
            Debug.Log(input[1] + " " + solutions[1]);
            matchingArrays = matchingArrays && input[1].Equals(solutions[1]);
            Debug.Log(matchingArrays);
            Debug.Log(input[2] + " " + solutions[2]);
            matchingArrays = matchingArrays && input[2].Equals(solutions[2]);
            Debug.Log(matchingArrays);
            Debug.Log(input[3] + " " + solutions[3]);
            matchingArrays = matchingArrays && input[3].Equals(solutions[3]);
            Debug.Log(matchingArrays);
            if (matchingArrays)
            {
                hasBeenTriggered = true;
                Debug.Log("Correct Answer!");
                canvas.enabled = false;
                
            }
            else
            {
                hasBeenTriggered = false;
                Debug.Log("Incorrect Combination!");
                canvas.enabled = false;
            }
            input = new ArrayList();
            counter = 0;
            canvas.enabled = false;
        }
        if (hasBeenTriggered == true)
        {
            enemy.SetActive(false);
            //Instantiate(exitArtifact);
            exitArtifact.SetActive(true);
        }
    }

    // put order of buttons pressed, see if they match 
    public void ShowUI()
    {
        canvas.enabled = true;
        Cursor.visible = true;
    }
    public void HideUI()
    {
        canvas.enabled = false;
        Cursor.visible = false;
    }

    public void IncrementCounter()
    {
        Debug.Log($"Input: {input[counter-1]}");
        Debug.Log($"Solutions: {solutions[counter-1]}");
    }

    public void OnClickButton1()
    {
        counter++;
        input.Add(1);
        Debug.Log($"Button 1 Pressed.  Counter: {counter}");
    }

    public void OnClickButton2()
    {
        counter++;
        input.Add(2);
        Debug.Log($"Button 2 Pressed.  Counter: {counter}");
    }

    public void OnClickButton3()
    {
        counter++;
        input.Add(3);
        Debug.Log($"Button 3 Pressed.  Counter: {counter}");
    }

    public void OnClickButton4()
    {
        counter++;
        input.Add(4);
        Debug.Log($"Button 4 Pressed.  Counter: {counter}");
    }
}
