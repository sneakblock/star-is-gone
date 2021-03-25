using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TVManager : MonoBehaviour
{
    [SerializeField] TVInteractable tv1;
    [SerializeField] TVInteractable tv2;
    [SerializeField] TVInteractable tv3;
    [SerializeField] TVInteractable tv4;
    [SerializeField] TVInteractable tv5;
    private ArrayList solutions;
    private ArrayList inputs;
    bool correctSolution;
    public GameObject exitArtifact;
    GameObject enemy;

    // Start is called before the first frame update
    void Start()
    {
        solutions = new ArrayList() { 6, 4, 5, 10, 2 };
        inputs = new ArrayList() { 0, 0, 0, 0, 0 };
        exitArtifact = GameObject.FindGameObjectWithTag("ExitArtifact");
        exitArtifact.SetActive(false);
        enemy = GameObject.FindGameObjectWithTag("AI");
        enemy.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        inputs[0] = tv1.dialsUI.vid.currChannel;
        inputs[1] = tv2.dialsUI.vid.currChannel;
        inputs[2] = tv3.dialsUI.vid.currChannel;
        inputs[3] = tv4.dialsUI.vid.currChannel;
        inputs[4] = tv5.dialsUI.vid.currChannel;
        if (!inputs.Contains(0))
        {
            correctSolution = inputs[0].Equals(solutions[0]);
            Debug.Log(inputs[0]);
            correctSolution = correctSolution && inputs[1].Equals(solutions[1]);
            Debug.Log(inputs[1]);
            correctSolution = correctSolution && inputs[2].Equals(solutions[2]);
            Debug.Log(inputs[2]);
            correctSolution = correctSolution && inputs[3].Equals(solutions[3]);
            Debug.Log(inputs[3]);
            correctSolution = correctSolution && inputs[4].Equals(solutions[4]);
            Debug.Log(inputs[4]);
            if (correctSolution)
            {
                Debug.Log("Correct Answer!");
                exitArtifact.SetActive(true);
                enemy.SetActive(false);
            }
            else
            {
                Debug.Log("Incorrect");
            }
        }
    }


}
