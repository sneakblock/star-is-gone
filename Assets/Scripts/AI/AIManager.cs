using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIManager : MonoBehaviour {
    Animator animator1;
    Animator animator2;
    NavMeshAgent agent;

    public bool changesForm;
    public bool isEnemy;
    public bool canBeLost;
    public List<GameObject> waypoints;
    public GameObject player;
    public GameObject form1;
    public GameObject form2;

    public int health = 100;
    public float baseDetectionRange = 10f;
    float detectionRange;
    public int waypointRandomness = 1;
    public float baseSpeed = 1f;
    float speed;
    public float baseFov = 160f;
    float fov;
    public float baseDamage = 50f;
    float damage;
    Vector3 lastPositionPlayerSeen;
    Vector3 lookAroundPoint;
    int currWaypoint = 0;
    Quaternion fromRotation;
    bool playerSneaking = false;

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

        animator1.SetBool("IsEnemy", isEnemy);
        animator1.SetBool("CanBeLost", canBeLost);
        if (changesForm) {
            animator2.SetBool("IsEnemy", isEnemy);
            animator2.SetBool("CanBeLost", canBeLost);
        }

        speed = baseSpeed;
        detectionRange = baseDetectionRange;
        fov = baseFov;
        damage = baseDamage;
        moving = true;
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
            lookAroundPoint = Vector3.zero;
        }

        // manage activity while in certain states
        if (stateInfo.IsName("Idle")) {
            // do nothing
            agent.ResetPath(); 
            // look at player
            Vector3 targetDirection = player.transform.position - transform.position;
            Vector3 newDirection = Vector3.RotateTowards(transform.forward, targetDirection, speed * Time.deltaTime, 0.0f);
            transform.rotation = Quaternion.LookRotation(newDirection);
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
            if (canBeLost) {
                if (Vector3.Distance(gameObject.transform.position, lastPositionPlayerSeen) < 2f) {
                    if (lookAroundPoint == Vector3.zero || Vector3.Distance(gameObject.transform.position, lookAroundPoint) < 2f) {
                        LookAround();
                    } else {
                        MoveTowardPoint(lookAroundPoint);
                    }
                } else {
                    MoveTowardPoint(lastPositionPlayerSeen);
                }
            } else {
                MoveTowardPoint(player.transform.position);
            }
        } else if (stateInfo.IsName("PursuingPlayer")) {
            MoveTowardPoint(lastPositionPlayerSeen);
            float dist = Vector3.Distance(gameObject.transform.position, player.transform.position);
            if ((dist < 1.5f || (form2.GetComponentInChildren<Renderer>().enabled && dist < 5f)) && timeSinceLastAttack > 0.5f) {
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
                player.transform.parent.gameObject.GetComponent<HealthSystem>().TakeDamage((int) (damage));
            }

        } else if (stateInfo.IsName("SpecialAttack")) {
            if (checkPlayerTouching() && !playerTakenDamageYet) {
                Debug.Log("Dealt damage!");
                playerTakenDamageYet = true;
                player.transform.parent.gameObject.GetComponent<HealthSystem>().TakeDamage((int) (damage + 10f));
            }

        } else if (stateInfo.IsName("TakingHit")) {

        } else if (stateInfo.IsName("Death")) {

        }

        UpdatePlayerSneaking();
        UpdatePlayerInView();
        UpdatePlayerNear();
        UpdateTimeSincePlayerInView();

        if (changesForm)
        {
            UpdateForm();
        }
    }

    void UpdatePlayerSneaking () {
        playerSneaking = player.transform.parent.gameObject.GetComponent<NewPlayerMovement>().GetSneaking();
        if (playerSneaking) {
            detectionRange = baseDetectionRange / 3f;
            fov = baseFov / 3f;
        } else {
            detectionRange = baseDetectionRange;
            fov = baseFov;
        }
    }

    void UpdatePlayerInView () {
        bool inView = false;
        Vector3 dirToPlayer = player.transform.position - transform.position;
        float angleToPlayer = Vector3.Angle(new Vector3(dirToPlayer.x, 0, dirToPlayer.z), new Vector3(transform.forward.x, 0, transform.forward.z));
            
        if (angleToPlayer > 360 - (fov / 2) || angleToPlayer < (fov / 2)) { // player is in front of enemy
            RaycastHit hit;
            // Debug.DrawRay (transform.position, dirToPlayer, Color.red, 0f, true);
            if(Physics.Raycast(transform.position, dirToPlayer, out hit, detectionRange * 10f)) {
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
        return Vector3.Distance(gameObject.transform.position, player.transform.position) <= 1.5f;
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
        agent.speed = speed;
        form1.transform.position = gameObject.transform.position;
        if (changesForm)
        {
            form2.transform.position = gameObject.transform.position;
        }
    }

    void LookAround() {
        Vector3 randomDirection = Random.insideUnitSphere * baseDetectionRange;
        randomDirection += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, baseDetectionRange, 1);
        Vector3 finalPosition = hit.position;
        lookAroundPoint = finalPosition;
    }

    public void TakeHit(int damage) {
        // subtract health, set taking hit trigger
        health -= damage;
        animator1.SetTrigger("TakeHit");
        animator2.SetTrigger("TakeHit");
    }

    public void UpdateForm() {
        if (checkPlayerTouching()) {
            speed = 0f;
        } else {
            if (form1.GetComponentInChildren<Renderer>().enabled) {
                speed = baseSpeed;
                damage = baseDamage;
            } else if (form2.GetComponentInChildren<Renderer>().enabled) {
                speed = baseSpeed * 3f;
                damage = baseDamage * 2f;
            }
        }
        
    }

    public void Interact(bool start) {
        moving = !start;
    }
}
