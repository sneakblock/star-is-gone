using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class ChainGenerator : MonoBehaviour
{
    [Space(10)]
    [SerializeField] [Range(3, 100)] int count = 15;
    [SerializeField] [Range(0f, 0.05f)] float offset = 0.035f;
    [Space(10)]
    [SerializeField] bool rotateMidChains = true;
    [SerializeField] bool offsetAffectJoint = true;
    [SerializeField] bool startIsKinematic = true;
    [SerializeField] bool endIsKinematic = false;
    [Space(10)]
    [SerializeField] GameObject[] prefabs;

    // Chain List
    [SerializeField] [HideInInspector] List<GameObject> chains;

    // Previous States
    [SerializeField] [HideInInspector] bool prevBool_rotation = false;
    [SerializeField] [HideInInspector] bool prevBool_offset = false;
    [SerializeField] [HideInInspector] bool prevBool_kinematicStart = true;
    [SerializeField] [HideInInspector] bool prevBool_kinematicEnd = true;
    [SerializeField] [HideInInspector] GameObject prevPrefab_1 = null;
    [SerializeField] [HideInInspector] GameObject prevPrefab_2 = null;
    [SerializeField] [HideInInspector] int prevPrefabCount = 0;



    void Update()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            return;

        if (UnityEditor.Selection.activeGameObject != this.gameObject)
            return;

        if (prefabs == null)
            return;

        if (prefabs.Length == 0 ||
           (prefabs.Length == 1 && prefabs[0] == null) ||
           (prefabs.Length == 2 && (prefabs[0] == null || prefabs[1] == null)))
            return;

        //- Remove Null Refs
        chains.RemoveAll(item => item == null);

        //- Add / Remove
        int delta = count - chains.Count;
        if (delta != 0)
        {
            if (delta > 0) // add chain
            {
                for (int i = 0; i < delta; i++)
                {
                    int index = 0;
                    if (prefabs.Length == 2) index = chains.Count % 2;

                    GameObject chain = CreateChain(prefabs[index]);
                    chains.Add(chain);

                    // kinematic end
                    if (endIsKinematic && chains.Count > 1)
                        chains[chains.Count - 2].GetComponent<Rigidbody>().isKinematic = false;

                    UpdateJoint(chain, chains.IndexOf(chain));
                }
            }
            else if (delta < 0) // remove Chain
            {
                for (int i = chains.Count - 1; i >= chains.Count + delta; i--)
                    DestroyImmediate(chains[i]);
                chains.RemoveRange(chains.Count + delta, -delta);
            }
        }

        // Reset Chains
        UpdateBoolChanges(delta != 0);

        //- Offset
        for (int i = 0; i < chains.Count; i++)
            if (chains[i] != null)
            {
                chains[i].transform.localPosition = new Vector3(0f, -offset, 0f) * i;
                if (i > 0 && offsetAffectJoint)
                {
                    Joint joint = chains[i].GetComponent<Joint>();
                    joint.anchor = new Vector3(offset / 2f, 0f, 0f);
                    if (!joint.autoConfigureConnectedAnchor)
                        joint.connectedAnchor = new Vector3(-offset / 2f, 0f, 0f);
                }
            }

        //- Angles
        if(rotateMidChains)
            UpdateAngles();

        //- Names
        UpdateNames();

        UpdatePrefabChange();
#endif

    }


    void UpdatePrefabChange()
    {
        if (prefabs != null && prefabs.Length > 2)
        {
            Debug.Log("Prefabs max lenght is 2");
            prefabs = new GameObject[2];
            return;
        }

        if (prefabs != null && prefabs.Length > 0)
        {
            if (prefabs[0] != prevPrefab_1)
            {
                Debug.Log("Prefab [1] changed");
                prevPrefab_1 = prefabs[0];
                DeleteAllJoints();
                Update();
            }
            if (prefabs.Length == 2 && prefabs[1] != prevPrefab_2)
            {
                Debug.Log("Prefab [2] changed");
                prevPrefab_2 = prefabs[1];
                DeleteAllJoints();
                Update();
            }
            if (prefabs.Length != prevPrefabCount)
            {
                Debug.Log("Prefabs lenght changed");
                prevPrefabCount = prefabs.Length;
                DeleteAllJoints();
                Update();
            }
        }
    }
    void UpdateBoolChanges(bool countChanged)
    {
        if (rotateMidChains != prevBool_rotation)
        {
            prevBool_rotation = rotateMidChains;
            DeleteAllJoints();
            Update();
        }
        if (offsetAffectJoint != prevBool_offset)
        {
            prevBool_offset = offsetAffectJoint;
            DeleteAllJoints();
            Update();
        }
        if (startIsKinematic != prevBool_kinematicStart || countChanged)
        {
            prevBool_kinematicStart = startIsKinematic;
            chains[0].GetComponent<Rigidbody>().isKinematic = startIsKinematic;
        }
        if (endIsKinematic != prevBool_kinematicEnd || countChanged)
        {
            prevBool_kinematicEnd = endIsKinematic;
            chains[chains.Count - 1].GetComponent<Rigidbody>().isKinematic = endIsKinematic;
        }
    }
    void DeleteAllJoints()
    {
        for (int i = 0; i < chains.Count; i++)
            DestroyImmediate(chains[i]);
    }
    void UpdateAngles()
    {
        for (int i = 0; i < chains.Count; i++)
        {
            if (i % 2 == 0)
                chains[i].transform.localEulerAngles = new Vector3(0f, 0f, 90f);
            else
                chains[i].transform.localEulerAngles = new Vector3(0f, 90f, 90f);
        }
    }
    void UpdateJoint(GameObject go, int index)
    {
        Rigidbody rBody = go.GetComponent<Rigidbody>();
        rBody.isKinematic = false;

        Joint joint = go.GetComponent<Joint>();

        if (index == 0)
            DestroyImmediate(chains[0].GetComponent<Joint>());
            //joint.autoConfigureConnectedAnchor = true;
        else
        if (index > 0)
        {
            joint.connectedBody = chains[chains.Count - 2].GetComponent<Rigidbody>();
            if (offsetAffectJoint)
            {
                joint.anchor = new Vector3(offset / 2f, 0f, 0f);
                if (!joint.autoConfigureConnectedAnchor)
                    joint.connectedAnchor = new Vector3(-offset / 2f, 0f, 0f);
            }
        }
    }
    void UpdateNames()
    {
        for (int i = 0; i < chains.Count; i++)
        {
            if (i == 0)
                chains[i].name = "Start";
            else
                chains[i].name =  "Chain (" + i.ToString() + ")";
        }

        if (endIsKinematic)
            chains[chains.Count - 1].name = "End";
    }
    GameObject CreateChain(GameObject prefab)
    {
        GameObject go = Instantiate(prefab);
        go.transform.parent = transform;
        go.transform.localPosition -= new Vector3(0f, offset, 0f) * chains.Count;
        return go;
    }
}
