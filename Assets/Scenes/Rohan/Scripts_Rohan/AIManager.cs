using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIManager : MonoBehaviour {
    Animator animator1;
    Animator animator2;
    NavMeshAgent agent;
    NavMeshAgent agent1;
    NavMeshAgent agent2;

    public bool changesForm;
    public List<GameObject> waypoints;
    public GameObject player;
    public GameObject form1;
    public GameObject form2;

    public int health = 100;
    public float detectionRange = 10f;
    public int waypointRandomness = 1;
    public float baseSpeed = 1f;
    public float fov = 160f;
    float speed;
    Vector3 lastPositionPlayerSeen;
    int currWaypoint = 0;
    int lookingAround = 0;
    Quaternion fromRotation;

    // state parameters
    bool moving = false;
    bool dead = false;
    bool playerInView = false;
    bool playerNear = false;
    bool playerSeen = false;
    float timeSincePlayerInView = 0f;
    float timeSinceLastAttack = 0f;
    bool playerTakenDamageYet = false;


    // Start is called before the first frame update
    void Start() {
        animator1 = form1.GetComponent<Animator>();
        if (changesForm)
        {
            animator2 = form2.GetComponent<Animator>();
        }
        agent = GetComponent<NavMeshAgent>();
        agent1 = form1.GetComponent<NavMeshAgent>();
        if (changesForm)
        {
            agent2 = gameObject.transform.Find("feral_form").GetComponent<NavMeshAgent>();
        }
        speed = baseSpeed;
    }

    // Update is called once per frame
    void Update() {
        var stateInfo = animator1.GetCurrentAnimatorStateInfo(0);

        animator1.SetBool("PlayerInView", playerInView);
        animator1.SetBool("PlayerNear", playerNear);
        animator1.SetInteger("TimeSincePlayerInView", (int) timeSincePlayerInView);
        animator1.SetBool("Moving", moving);
        animator1.SetBool("Dead", dead);
        
        if (changesForm)
        {
            animator2.SetBool("PlayerInView", playerInView);
            animator2.SetBool("PlayerNear", playerNear);
            animator2.SetInteger("TimeSincePlayerInView", (int) timeSincePlayerInView);
            animator2.SetBool("Moving", moving);
            animator2.SetBool("Dead", dead);
        }

        // manage activity based off parameters
        if (playerInView) {
            lastPositionPlayerSeen = player.transform.position;
        }
        if (health <= 0) {
            dead = true;
        }
        if (!stateInfo.IsName("Searching")) {
            lookingAround = 0;
        }

        // manage activity while in certain states
        if (stateInfo.IsName("Idle")) {
            if (waypoints != null && waypoints.Count > 0) {
                moving = true; // basically never be idle; this can be changed if desired
            }
        } else if (stateInfo.IsName("Wandering")) {
            timeSincePlayerInView = 0f;
            if (moving) {
                if (Vector3.Distance(gameObject.transform.position, waypoints[currWaypoint].transform.position) < 2f) {
                    currWaypoint += Random.Range(1, 1 + waypointRandomness);
                    if (currWaypoint >= waypoints.Count) {
                        currWaypoint = 0;
                    }
                } 
                if (Vector3.Distance(gameObject.transform.position, waypoints[currWaypoint].transform.position) >= 2f) {
                    MoveTowardPoint(waypoints[currWaypoint].transform.position);
                }
            }
        } else if (stateInfo.IsName("Searching")) {
            if (Vector3.Distance(gameObject.transform.position, lastPositionPlayerSeen) < 0.1f) {
                fromRotation = transform.rotation;
                LookAround();
            } else {
                MoveTowardPoint(lastPositionPlayerSeen);
            }
        } else if (stateInfo.IsName("PursuingPlayer")) {
            MoveTowardPoint(lastPositionPlayerSeen);
            float dist = Vector3.Distance(gameObject.transform.position, player.transform.position);
            if ((dist < 1.5f || (form2.GetComponentInChildren<Renderer>().enabled && dist < 5f)) && timeSinceLastAttack > 3f) {
                int rand = Random.Range(0, 2);
                if (rand == 0) {
                    animator1.SetTrigger("BasicAttack");
                    if (changesForm)
                    {
                        animator2.SetTrigger("BasicAttack");
                    }
                } else if (rand == 1) {
                    animator1.SetTrigger("SpecialAttack");
                    if (changesForm)
                    {
                        animator2.SetTrigger("SpecialAttack");
                    }
                }
                playerTakenDamageYet = false;
                timeSinceLastAttack = 0f;
            } else {
                timeSinceLastAttack += Time.deltaTime;
            }
        } else if (stateInfo.IsName("BasicAttack")) {
            if (checkPlayerTouching() && !playerTakenDamageYet) {
                Debug.Log("Dealt damage!");
                playerTakenDamageYet = true;
                // deal damage
            }

        } else if (stateInfo.IsName("SpecialAttack")) {
            if (checkPlayerTouching() && !playerTakenDamageYet) {
                Debug.Log("Dealt damage!");
                playerTakenDamageYet = true;
                // deal extra damage
            }

        } else if (stateInfo.IsName("TakingHit")) {

        } else if (stateInfo.IsName("Death")) {

        }

        UpdatePlayerInView();
        UpdatePlayerNear();
        UpdateTimeSincePlayerInView();

        if (changesForm)
        {
            updateForm();
        }
    }

    void UpdatePlayerInView () {
        bool inView = false;
        Vector3 dirToPlayer = player.transform.position - transform.position;
        float angleToPlayer = Vector3.Angle(new Vector3(dirToPlayer.x, 0, dirToPlayer.z), new Vector3(transform.forward.x, 0, transform.forward.z));
            
        if (angleToPlayer > 360 - (fov / 2) || angleToPlayer < (fov / 2)) { // player is in front of enemy
            RaycastHit hit;
            // Debug.DrawRay (transform.position, dirToPlayer, Color.red, 0f, true);
            if(Physics.Raycast(transform.position, dirToPlayer, out hit, 100f)) {
                if(hit.collider.gameObject == player || hit.collider.gameObject.transform.IsChildOf(player.transform)) { // line of sight is not blocked
                    inView = true;
                }
            }
        }
        inView = inView || checkPlayerTouching();
        if (inView) {
            playerSeen = true;
            timeSincePlayerInView = 0f;
        }
        playerInView = inView;
    }

    void UpdatePlayerNear() {
        playerNear = Vector3.Distance(gameObject.transform.position, player.transform.position) <= detectionRange;
    }

    bool checkPlayerTouching() {
        return Vector3.Distance(gameObject.transform.position, player.transform.position) <= 2f;
    }

    void UpdateTimeSincePlayerInView() {
        if (playerSeen && !playerInView) {
            timeSincePlayerInView += Time.deltaTime;
            if (timeSincePlayerInView >= 10f) {
                playerSeen = false;
            }
        } 
    }

    void MoveTowardPoint(Vector3 target) {
        agent.destination = target; 
        agent1.destination = target;
        if (changesForm)
        {
            agent2.destination = target; 
        }
        agent.speed = speed;
        agent1.speed = speed;
        if (changesForm)
        {
            agent2.speed = speed; 
        }
    }

    void LookAround() {
        if (lookingAround == 0) {
            Quaternion toRotate = transform.rotation * Quaternion.Euler(0, 90f * Time.deltaTime, 0);
            transform.rotation = Quaternion.Lerp(fromRotation, toRotate, Time.time * 1);

            if (Quaternion.Angle(transform.rotation, toRotate) < 0.5) {
                lookingAround = 1;
            }
        } else if (lookingAround == 1) {
            Quaternion toRotate = transform.rotation * Quaternion.Euler(0, -180f * Time.deltaTime, 0);
            transform.rotation = Quaternion.Lerp(fromRotation * Quaternion.Euler(0, 90f * Time.deltaTime, 0), toRotate, Time.time * 1);

            if (Quaternion.Angle(transform.rotation, toRotate) < 0.5) {
                lookingAround = 2;
            }
        } else if (lookingAround == 2) {
            Quaternion toRotate = transform.rotation * Quaternion.Euler(0, 90f * Time.deltaTime, 0);
            transform.rotation = Quaternion.Lerp(fromRotation * Quaternion.Euler(0, -90f * Time.deltaTime, 0), toRotate, Time.time * 1);

            if (Quaternion.Angle(transform.rotation, toRotate) < 0.5) {
                lookingAround = 0;
            }
        }
    }

    public void TakeHit(int damage) {
        // subtract health, set taking hit trigger
        health -= damage;
        animator1.SetTrigger("TakeHit");
        animator2.SetTrigger("TakeHit");
    }

    public void updateForm() {
        if (form1.GetComponentInChildren<Renderer>().enabled) {
            speed = baseSpeed;
        } else if (form2.GetComponentInChildren<Renderer>().enabled) {
            speed = baseSpeed * 3f;
        }
        
    }
}
