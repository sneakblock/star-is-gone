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
    public GameObject camera;

    public int health = 100;
    //public float bigSize = 2;
    public float growthDuration = 3f;
    int growing = 0; // 0 = nothing, 1 = shrink, 2 = grow
    float growthTimer = 0f;
    public float baseDetectionRange = 10f;
    float detectionRange;
    public float baseTouchingRange = 1.5f;
    float touchingRange;
    public int waypointRandomness = 1;
    public float timeIdleAtWaypoint = 3f;
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
    public float timeToStun = 2f;
    float timePlayerStunning = 0f;
    bool inStunCone = false;
    float timeAtCurrWaypoint = 0f;
    bool interacting = false;

    // state parameters
    bool moving = false;
    bool dead = false;
    bool playerInView = false;
    bool playerNear = false;
    bool playerSeen = false;
    float timeSincePlayerInView = 0f;
    float timeSinceLastAttack = 0f;
    bool playerTakenDamageYet = false;
    bool stunned = false;


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
        touchingRange = baseTouchingRange;
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
        if (!stateInfo.IsName("Stunned")) {
            stunned = false;
        }

        if (Vector3.Distance(gameObject.transform.position, waypoints[currWaypoint].transform.position) < 2f) {
            timeAtCurrWaypoint += Time.deltaTime;
            if (timeAtCurrWaypoint >= timeIdleAtWaypoint) {
                moving = true;
            } else {
                moving = false;
            }
        } else {
            timeAtCurrWaypoint = 0f;
        }

        // manage activity while in certain states
        if (stateInfo.IsName("Idle")) {
            // do nothing
            agent.ResetPath(); 
            // look at player
            if (interacting) {
                Vector3 targetDirection = player.transform.position - transform.position;
                Vector3 newDirection = Vector3.RotateTowards(transform.forward, targetDirection, speed * Time.deltaTime, 0.0f);
                transform.rotation = Quaternion.LookRotation(newDirection);
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
                gameObject.GetComponent<EffectsManager>().SetDamageOffset(0.8f);
            }

        } else if (stateInfo.IsName("SpecialAttack")) {
            if (checkPlayerTouching() && !playerTakenDamageYet) {
                Debug.Log("Dealt damage!");
                playerTakenDamageYet = true;
                player.transform.parent.gameObject.GetComponent<HealthSystem>().TakeDamage((int) (damage + 10f));
                gameObject.GetComponent<EffectsManager>().SetDamageOffset(1f);
            }

        } else if (stateInfo.IsName("TakingHit")) {

        } else if (stateInfo.IsName("Death")) {

        } else if (stateInfo.IsName("Stunned")) {

        }

        UpdatePlayerSneaking();
        UpdatePlayerInView();
        UpdatePlayerNear();
        UpdateTimeSincePlayerInView();
        //UpdateSize();

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
        bool unblocked = false;
        Vector3 dirToPlayer = player.transform.position - transform.position;
        float angleToPlayer = Vector3.Angle(new Vector3(dirToPlayer.x, 0, dirToPlayer.z), new Vector3(transform.forward.x, 0, transform.forward.z));
            
        RaycastHit hit;
        // Debug.DrawRay(transform.position, dirToPlayer, Color.red, 0f, true);
        if(Physics.Raycast(transform.position, dirToPlayer, out hit, detectionRange * 10f)) {
            if(hit.collider.gameObject == player || hit.collider.gameObject.transform.IsChildOf(player.transform)) { // line of sight is not blocked
                if (angleToPlayer > 360 - (fov / 2) || angleToPlayer < (fov / 2)) { // player is in front of enemy
                    inView = true;
                }
                unblocked = true;
            }
        }
        inView = inView || checkPlayerTouching();
        if (inView) {
            playerSeen = true;
            timeSincePlayerInView = 0f;
        }
        playerInView = inView;

        StunManager manager = player.transform.parent.gameObject.GetComponent<StunManager>();
        Vector3 camAngleFromPlayer = new Vector3(camera.transform.forward.x, 0, camera.transform.forward.z);
        float angleToCam = Vector3.Angle(new Vector3(-1 * dirToPlayer.x, 0, -1 * dirToPlayer.z), camAngleFromPlayer);

        Debug.DrawRay(player.transform.position, new Vector3(-1 * dirToPlayer.x, 0, -1 * dirToPlayer.z), Color.red, 0f, true);
        Debug.DrawRay(player.transform.position, 100 * camAngleFromPlayer, Color.red, 0f, true);
        Debug.DrawRay(player.transform.position, manager.stunConeLength * (Quaternion.Euler(0, -1 * manager.stunConeAngle / 2, 0) * camAngleFromPlayer), Color.green, 0f, true);
        Debug.DrawRay(player.transform.position, manager.stunConeLength * (Quaternion.Euler(0, manager.stunConeAngle / 2, 0) * camAngleFromPlayer), Color.blue, 0f, true);
        
        if (manager.GetStunning() && unblocked 
        && (angleToCam <= manager.stunConeAngle / 2 || angleToCam >= 360 - (manager.stunConeAngle / 2)) 
        && Vector3.Distance(transform.position, player.transform.position) <= manager.stunConeLength) {
            inStunCone = true;
            StayInStunCone();
        } else {
            if (!inStunCone) {
                ExitStunCone();
            }
            inStunCone = false;
        }
        bool inStunConeAngle = angleToCam <= manager.stunConeAngle / 2 || angleToCam >= 360 - (manager.stunConeAngle / 2);
        Debug.Log("in stun cone angle: " + inStunConeAngle);
    }

    void UpdatePlayerNear() {
        playerNear = Vector3.Distance(gameObject.transform.position, player.transform.position) <= detectionRange;
    }

    bool checkPlayerTouching()
    {
        Debug.Log((int)(Vector3.Distance(gameObject.transform.position, player.transform.position)));
        return Vector3.Distance(gameObject.transform.position, player.transform.position) <= touchingRange;
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
        NavMesh.SamplePosition(randomDirection, out hit, baseDetectionRange * 5, 1);
        Vector3 finalPosition = hit.position;
        lookAroundPoint = finalPosition;
    }

    /*
    void UpdateSize() {
        float newScale;
        if (growing == 2) {
            newScale = Mathf.Lerp(1, bigSize, growthTimer / growthDuration);
            growthTimer += Time.deltaTime;
            transform.localScale = new Vector3(newScale, newScale, newScale);
            if (System.Math.Abs(bigSize - newScale) < 0.1) {
                transform.localScale = new Vector3(bigSize, bigSize, bigSize);
                growing = 0;
                growthTimer = 0f;
            }
        } else if (growing == 1) {
            newScale = Mathf.Lerp(bigSize, 1, growthTimer / growthDuration);
            growthTimer += Time.deltaTime;
            transform.localScale = new Vector3(newScale, newScale, newScale);
            if (System.Math.Abs(1 - newScale) < 0.1) {
                transform.localScale = new Vector3(1f, 1f, 1f);
                growing = 0;
                growthTimer = 0f;
            }
        }
    }


    public void ToggleSize() {
        if (growing == 1) {
            growing = 2;
        } else if (growing == 2) {
            growing = 1;
        } else if (growing == 0) {
            if (transform.localScale.x > 1) {
                growing = 1;
            } else {
                growing = 2;
            }
        }
    }
    */

    public void TakeHit(int damage) {
        // subtract health, set taking hit trigger
        health -= damage;
        animator1.SetTrigger("TakeHit");
        if (changesForm) {
            animator2.SetTrigger("TakeHit");
        }
    }

    public void UpdateForm() {
        if (checkPlayerTouching() && isEnemy || stunned) {
            speed = 0f;
        } else if (timePlayerStunning > 0f) {
            speed = 0.5f;
        } else  {
            if (form1.GetComponentInChildren<Renderer>().enabled) {
                speed = baseSpeed;
                damage = baseDamage;
                touchingRange = baseTouchingRange;
            } else if (form2.GetComponentInChildren<Renderer>().enabled) {
                speed = baseSpeed * 3f;
                damage = baseDamage * 2f;
                touchingRange = baseTouchingRange * 2f;
            }
        }
        
    }

    public void Interact(bool start) {
        moving = !start;
        interacting = start;
    }

    void Stun() {
        Debug.Log("Stun complete!");
        if (isEnemy) {
            stunned = true;
            animator1.SetTrigger("Stun");
            if (changesForm) {
                animator2.SetTrigger("Stun");
            }
        }
    }

    void StayInStunCone() {
        Debug.Log("Stun charging for..." + (int) timePlayerStunning);
        timePlayerStunning += Time.deltaTime;
        if (timePlayerStunning >= timeToStun) {
            Stun();
            timePlayerStunning = 0f;
            player.GetComponent<StunManager>().SetIsAttacking(false);
        }
    }

    void ExitStunCone() {
        timePlayerStunning = 0f;
    }
}
