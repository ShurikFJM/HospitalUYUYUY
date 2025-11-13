using UnityEngine;
using UnityEngine.AI;

public class TeleportingHunter : MonoBehaviour
{
    [Header("Configuraciones de Teletransporte")]
    public float teleportDistance = 10f;          // Distancia base para el teletransporte cerca del jugador
    public float teleportCooldown = 5f;           // Tiempo entre teletransportes
    [Range(0f, 1f)] public float chaseProbability = 0.65f; // Probabilidad de teletransportarse mientras caza

    [Header("Efecto de Estática")]
    public GameObject staticObject;               // Objeto que representa la estática
    public float staticActivationRange = 5f;      // Rango en el que se activa la estática

    [Header("Audio")]
    public AudioClip teleportSound;               // Sonido de teletransporte
    private AudioSource audioSource;              // Fuente de audio del NPC

    [Header("Detección y Movimiento")]
    [SerializeField] private Transform player;    // Asignar manualmente en el Inspector
    public float detectionRange = 15f;            // Rango para detectar al jugador
    public float chaseSpeed = 6f;                 // Velocidad cuando persigue al jugador
    public float patrolSpeed = 3f;                // Velocidad cuando patrulla
    public Vector2 patrolArea = new Vector2(250f, 250f); // Tamaño del terreno (x, z)

    private NavMeshAgent agent;
    private float teleportTimer;
    private bool isChasing = false;               // Controla si el NPC está persiguiendo al jugador
    private Vector3 randomPatrolTarget;

    void Start()
    {
        teleportTimer = teleportCooldown;

        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Estática
        if (staticObject != null)
            staticObject.SetActive(false);

        // NavMesh
        agent = GetComponent<NavMeshAgent>();

        // Validación del jugador
        if (player == null)
            Debug.LogWarning("⚠️ No se asignó el Player en el Inspector para " + gameObject.name);

        // Primer destino de patrulla
        randomPatrolTarget = GetRandomPatrolPoint();
        agent.SetDestination(randomPatrolTarget);
    }

    void Update()
    {
        teleportTimer -= Time.deltaTime;

        if (player == null)
        {
            PatrolRandomly();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 🔹 Cambiar entre modo patrulla y caza
        if (!isChasing && distanceToPlayer <= detectionRange)
        {
            StartChasing();
        }
        else if (isChasing && distanceToPlayer > detectionRange * 1.2f) // un pequeño margen
        {
            StopChasing();
        }

        // 🔹 Comportamiento según el modo actual
        if (isChasing)
        {
            ChasePlayer(distanceToPlayer);
        }
        else
        {
            PatrolRandomly();

            // Teletransporte ocasional mientras patrulla
            if (teleportTimer <= 0f)
            {
                RandomTeleportInMap();
                teleportTimer = teleportCooldown;
            }
        }

        // 🔹 Efecto de estática
        HandleStaticEffect(distanceToPlayer);
    }

    // ================================
    // ---   MODOS DE COMPORTAMIENTO ---
    // ================================

    void StartChasing()
    {
        isChasing = true;
        agent.speed = chaseSpeed;
        Debug.Log(gameObject.name + " ha detectado al jugador. ¡Entrando en modo caza!");
    }

    void StopChasing()
    {
        isChasing = false;
        agent.speed = patrolSpeed;
        randomPatrolTarget = GetRandomPatrolPoint();
        agent.SetDestination(randomPatrolTarget);
        Debug.Log(gameObject.name + " ha perdido al jugador. Volviendo a patrullar.");
    }

    void ChasePlayer(float distanceToPlayer)
    {
        if (player == null) return;

        agent.SetDestination(player.position);

        // Teletransportarse cerca del jugador de forma aleatoria
        if (teleportTimer <= 0f)
        {
            DecideTeleportAction();
            teleportTimer = teleportCooldown;
        }
    }

    // ================================
    // ---         PATRULLA           ---
    // ================================

    void PatrolRandomly()
    {
        agent.speed = patrolSpeed;

        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            randomPatrolTarget = GetRandomPatrolPoint();
            agent.SetDestination(randomPatrolTarget);
        }
    }

    Vector3 GetRandomPatrolPoint()
    {
        Vector3 randomPoint = new Vector3(
            Random.Range(-patrolArea.x / 2f, patrolArea.x / 2f),
            0,
            Random.Range(-patrolArea.y / 2f, patrolArea.y / 2f)
        );

        randomPoint += new Vector3(transform.position.x, 0, transform.position.z);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 5f, NavMesh.AllAreas))
            return hit.position;
        else
            return transform.position;
    }

    // ================================
    // ---       TELETRANSPORTE       ---
    // ================================

    void DecideTeleportAction()
    {
        float randomValue = Random.value;
        if (randomValue <= chaseProbability)
            TeleportNearPlayer();
    }

    void TeleportNearPlayer()
    {
        if (player == null) return;

        Vector3 randomDirection = Random.insideUnitSphere * teleportDistance;
        randomDirection.y = 0f;
        Vector3 teleportPosition = player.position + randomDirection;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(teleportPosition, out hit, 5f, NavMesh.AllAreas))
        {
            transform.position = hit.position;

            if (teleportSound != null && audioSource != null)
                audioSource.PlayOneShot(teleportSound);
        }
    }

    void RandomTeleportInMap()
    {
        Vector3 randomPosition = new Vector3(
            Random.Range(-patrolArea.x / 2f, patrolArea.x / 2f),
            0,
            Random.Range(-patrolArea.y / 2f, patrolArea.y / 2f)
        );

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPosition, out hit, 5f, NavMesh.AllAreas))
        {
            transform.position = hit.position;

            if (teleportSound != null && audioSource != null)
                audioSource.PlayOneShot(teleportSound);
        }
    }

    // ================================
    // ---         ESTÁTICA           ---
    // ================================

    void HandleStaticEffect(float distanceToPlayer)
    {
        if (staticObject == null) return;

        bool shouldActivate = distanceToPlayer <= staticActivationRange;
        if (staticObject.activeSelf != shouldActivate)
            staticObject.SetActive(shouldActivate);
    }

    // ================================
    // ---   GIZMOS VISUALES (debug) ---
    // ================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, staticActivationRange);
    }
}