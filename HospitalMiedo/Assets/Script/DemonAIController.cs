using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DemonAIController : MonoBehaviour
{
    public enum State { Idle, Patrol, Teleporting, Chasing, KillSequence, Disabled }

    [Header("Components")]
    public NavMeshAgent agent;
    public Transform playerCamera; // assign VR Camera or player's head transform
    public GameObject playerRigToDisable; // objeto con controles AutoHand que se desactivará
    public LayerMask obstacleMask; // para raycast (ej: Default)
    public LayerMask playerLayer; // layer del jugador

    [Header("Detection")]
    public float detectionRadius = 8f;
    public float fieldOfView = 60f; // grados (half-angle)
    public float timeToStartHunt = 0.5f; // reacción antes de cambiar a chase

    [Header("Chase / Kill")]
    public float stayNearForKillSeconds = 10f; // si el jugador permanece cerca ese tiempo -> kill
    public float killProximity = 2.2f; // distancia considerada "en su area"
    public GameObject cameraScreamerPrefab; // prefab con anim stab y audio; se instanciará como hijo de playerCamera
    public float gameOverDelay = 5f; // tiempo en estado gameover antes de llamar GameOverHandler

    [Header("Teleport")]
    public float teleportCheckInterval = 6f;
    public float teleportRange = 12f;
    public bool useTeleportPoints = false;
    public Transform[] teleportPoints; // opcional: puntos predefinidos en NavMesh

    [Header("Misc")]
    public float chaseStopDistance = 1.0f;
    public float teleportSampleRadius = 2f; // para SamplePosition
    public float timeBetweenPlayerVisibleChecks = 0.2f;

    // internal
    State currentState = State.Idle;
    float playerNearTimer = 0f;
    Transform playerHead;
    Coroutine teleportCoroutine;
    Coroutine chaseCoroutine;
    bool isPlayerVisible = false;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (playerCamera == null)
        {
            Camera cam = Camera.main;
            if (cam != null) playerCamera = cam.transform;
        }
        playerHead = playerCamera;
        currentState = State.Idle;
        if (teleportCoroutine == null) teleportCoroutine = StartCoroutine(TeleportLoop());
        StartCoroutine(PlayerVisibilityLoop());
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                if (isPlayerVisible) EnterChase();
                break;
            case State.Chasing:
                if (!isPlayerVisible)
                {
                    // Lost sight -> fallback to Idle (or Patrol)
                    currentState = State.Idle;
                    agent.ResetPath();
                }
                else
                {
                    agent.stoppingDistance = chaseStopDistance;
                    agent.SetDestination(playerHead.position);
                    HandlePlayerProximity();
                }
                break;
            case State.KillSequence:
            case State.Disabled:
                // nothing
                break;
        }
    }

    IEnumerator PlayerVisibilityLoop()
    {
        while (true)
        {
            CheckPlayerVisibility();
            yield return new WaitForSeconds(timeBetweenPlayerVisibleChecks);
        }
    }

    void CheckPlayerVisibility()
    {
        if (playerHead == null) { isPlayerVisible = false; return; }

        Vector3 dir = (playerHead.position - transform.position);
        float dist = dir.magnitude;

        if (dist <= detectionRadius)
        {
            float angle = Vector3.Angle(transform.forward, dir.normalized);
            if (angle <= fieldOfView)
            {
                // raycast to check obstacles
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up * 0.6f, dir.normalized, out hit, detectionRadius, ~0))
                {
                    // prefer hit.collider attached to playerRig or a tag
                    bool hitPlayer = ((1 << hit.collider.gameObject.layer) & playerLayer) != 0
                                     || hit.collider.transform.IsChildOf(playerHead);
                    if (hitPlayer)
                    {
                        isPlayerVisible = true;
                        return;
                    }
                }
            }
        }

        isPlayerVisible = false;
    }

    void EnterChase()
    {
        if (currentState == State.KillSequence || currentState == State.Disabled) return;
        currentState = State.Chasing;
        if (chaseCoroutine != null) StopCoroutine(chaseCoroutine);
        chaseCoroutine = StartCoroutine(ChaseBehavior());
    }

    IEnumerator ChaseBehavior()
    {
        playerNearTimer = 0f;
        while (currentState == State.Chasing)
        {
            if (playerHead == null) yield break;
            agent.SetDestination(playerHead.position);
            // HandleKillTimer is polled in Update for precise timing.
            yield return null;
        }
    }

    void HandlePlayerProximity()
    {
        float d = Vector3.Distance(transform.position, playerHead.position);
        if (d <= killProximity)
        {
            playerNearTimer += Time.deltaTime;
            if (playerNearTimer >= stayNearForKillSeconds)
            {
                StartCoroutine(ExecuteKillSequence());
            }
        }
        else
        {
            playerNearTimer = 0f;
        }
    }

    IEnumerator ExecuteKillSequence()
    {
        if (currentState == State.KillSequence) yield break;
        currentState = State.KillSequence;

        // 1) deactivate the enemy prefab (but wait a frame to ensure agent stops)
        agent.isStopped = true;
        yield return null;

        gameObject.SetActive(false); // deactivate this prefab

        // 2) spawn screamer attached to player's camera
        if (cameraScreamerPrefab != null && playerCamera != null)
        {
            GameObject inst = Instantiate(cameraScreamerPrefab, playerCamera);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;

            // if the screamer prefab has a ScreamerCameraController, it will handle audio/anim
        }

        // 3) disable player controls
        if (playerRigToDisable != null)
            playerRigToDisable.SetActive(false);
        else
        {
            // try to find common AutoHand scripts and disable them (best-effort)
            var autoHand = playerCamera.GetComponentInParent<MonoBehaviour>();
            if (autoHand != null)
                autoHand.enabled = false;
        }

        // 4) wait gameOverDelay then notify GameOverHandler
        yield return new WaitForSeconds(gameOverDelay);

        //GameOverHandler.Instance?.OnPlayerDeath(); // safe call if exists

        // remain in disabled state (game over handles rest)
        currentState = State.Disabled;
    }

    IEnumerator TeleportLoop()
    {
        // If you want to teleport while idle/patrolling
        while (true)
        {
            yield return new WaitForSeconds(teleportCheckInterval);

            if (currentState == State.Chasing || currentState == State.KillSequence || currentState == State.Disabled)
                continue;

            // Only teleport if player is NOT visible
            if (isPlayerVisible) continue;

            Vector3 target = Vector3.zero;
            bool found = false;
            if (useTeleportPoints && teleportPoints != null && teleportPoints.Length > 0)
            {
                Transform t = teleportPoints[Random.Range(0, teleportPoints.Length)];
                target = t.position;
                // sample navmesh near that point:
                NavMeshHit hit;
                if (NavMesh.SamplePosition(target, out hit, teleportSampleRadius, NavMesh.AllAreas))
                {
                    target = hit.position;
                    found = true;
                }
            }
            else
            {
                // random sample around current position
                for (int i = 0; i < 10; i++)
                {
                    Vector3 rnd = transform.position + Random.insideUnitSphere * teleportRange;
                    rnd.y = transform.position.y + 2f; // sample with some height
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(rnd, out hit, teleportSampleRadius, NavMesh.AllAreas))
                    {
                        target = hit.position;
                        found = true;
                        break;
                    }
                }
            }

            if (found)
            {
                // optional: small teleport VFX here
                agent.Warp(target); // warp instantly to target
            }
        }
    }

    // Optional: public function to force disable (e.g., when player leaves area or level resets)
    public void ForceDisable()
    {
        StopAllCoroutines();
        agent.isStopped = true;
        gameObject.SetActive(false);
        currentState = State.Disabled;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, killProximity);
        // FOV lines
        Vector3 left = Quaternion.Euler(0, -fieldOfView, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, fieldOfView, 0) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, left * detectionRadius);
        Gizmos.DrawRay(transform.position, right * detectionRadius);
    }

}
