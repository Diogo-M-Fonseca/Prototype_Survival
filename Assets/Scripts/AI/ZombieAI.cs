using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Zombie que só anda em corredores (área de NavMesh "Corridor").
/// Passeia pelo corredor; se vir o jogador corre atrás dele; se o jogador sair do corredor
/// (ou o zombie o perder de vista durante algum tempo) volta a passear.
/// As paredes são respeitadas pelo NavMesh, por isso o zombie nunca bate nelas nem as atravessa.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieAI : MonoBehaviour
{
    private enum State { Wander, Chase }

    [Header("Referências")]
    [Tooltip("Jogador a perseguir. Se vazio, procura o PlayerMove na cena.")]
    [SerializeField] private Transform _target;

    [Tooltip("Ponto de onde o zombie vê (cabeça). Se vazio, usa o próprio transform.")]
    [SerializeField] private Transform _eyes;

    [Header("Corredor")]
    [Tooltip("Nome da área de NavMesh que representa os corredores (Window > AI > Navigation > Areas).")]
    [SerializeField] private string _corridorArea = "Corridor";

    [Tooltip("Distância máxima entre os pés do jogador e o NavMesh do corredor para contar como 'dentro do corredor'.")]
    [SerializeField, Min(0.05f)] private float _corridorTolerance = 0.5f;

    [Header("Visão")]
    [SerializeField, Min(1f)] private float _viewDistance = 15f;
    [SerializeField, Range(10f, 360f)] private float _viewAngle = 110f;

    [Tooltip("Layers que bloqueiam a visão (paredes, portas, o próprio jogador...).")]
    [SerializeField] private LayerMask _sightMask = ~0;

    [Tooltip("Segundos a seguir a última posição conhecida depois de perder o jogador de vista (dentro do corredor).")]
    [SerializeField, Min(0f)] private float _loseSightTime = 2f;

    [Header("Movimento")]
    [SerializeField, Min(0.1f)] private float _walkSpeed = 1.2f;
    [SerializeField, Min(0.1f)] private float _runSpeed = 4f;

    [Header("Passeio")]
    [SerializeField, Min(1f)] private float _minWanderDistance = 4f;
    [SerializeField, Min(2f)] private float _maxWanderDistance = 12f;
    [SerializeField, Min(0f)] private float _waitMin = 1f;
    [SerializeField, Min(0f)] private float _waitMax = 4f;

    [Header("Perseguição")]
    [Tooltip("Distância a que o zombie pára junto ao jogador (ligar o ataque aqui).")]
    [SerializeField, Min(0.1f)] private float _stopDistance = 1.2f;

    private NavMeshAgent _agent;
    private CharacterController _targetController;
    private NavMeshPath _path;
    private int _areaMask;

    private State _state = State.Wander;
    private Vector3 _lastKnownPos;
    private float _timeSinceSeen;
    private float _waitTimer;
    private float _repathTimer;
    private bool _waiting;

    private readonly RaycastHit[] _hits = new RaycastHit[16];

    /// <summary>
    /// True enquanto o zombie persegue o jogador (útil para animações/sons).
    /// </summary>
    public bool IsChasing => _state == State.Chase;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _path = new NavMeshPath();
        if (_eyes == null) _eyes = transform;

        int area = NavMesh.GetAreaFromName(_corridorArea);
        if (area < 0)
        {
            Debug.LogError($"ZombieAI: a área de NavMesh '{_corridorArea}' não existe.", this);
            enabled = false;
            return;
        }

        _areaMask = 1 << area;
        _agent.areaMask = _areaMask; // o zombie só consegue andar nesta área
    }

    private void Start()
    {
        if (_target == null)
        {
            PlayerMove player = FindFirstObjectByType<PlayerMove>();
            if (player != null) _target = player.transform;
        }

        if (_target == null)
        {
            Debug.LogError("ZombieAI: não foi encontrado o jogador.", this);
            enabled = false;
            return;
        }

        _targetController = _target.GetComponentInChildren<CharacterController>();

        // Se o zombie nasceu fora do corredor, encosta-o ao corredor mais próximo
        if (!_agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, _areaMask))
            _agent.Warp(hit.position);

        StartWander();
    }

    private void Update()
    {
        if (!_agent.isOnNavMesh) return;

        // O jogador só conta se os seus pés estiverem no corredor
        bool inCorridor = TryGetPlayerCorridorPos(out Vector3 playerNavPos);
        bool sees = inCorridor && CanSeePlayer();

        if (_state == State.Wander) UpdateWander(sees, playerNavPos);
        else UpdateChase(inCorridor, sees, playerNavPos);
    }

    // ---------- Passeio ----------

    private void UpdateWander(bool sees, Vector3 playerNavPos)
    {
        if (sees)
        {
            StartChase(playerNavPos);
            return;
        }

        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f) PickNewWanderDestination();
            return;
        }

        // Chegou ao destino (ou o caminho falhou): espera um pouco e escolhe outro ponto
        bool arrived = !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.2f;
        bool failed = !_agent.pathPending && _agent.pathStatus == NavMeshPathStatus.PathInvalid;
        if (arrived || failed)
        {
            _waiting = true;
            _waitTimer = Random.Range(_waitMin, _waitMax);
            _agent.ResetPath();
        }
    }

    private void StartWander()
    {
        _state = State.Wander;
        _agent.speed = _walkSpeed;
        _agent.stoppingDistance = 0.3f;
        _waiting = false;
        PickNewWanderDestination();
    }

    private void PickNewWanderDestination()
    {
        _waiting = false;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            float dist = Random.Range(_minWanderDistance, _maxWanderDistance);
            Vector3 candidate = transform.position + new Vector3(dir.x, 0f, dir.y) * dist;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, _areaMask)) continue;
            if (!NavMesh.CalculatePath(transform.position, hit.position, _areaMask, _path)) continue;
            if (_path.status != NavMeshPathStatus.PathComplete) continue;

            // Evita pontos que só se alcançam com uma volta enorme (ex.: corredor do outro lado de uma parede)
            if (PathLength(_path) > _maxWanderDistance * 2f) continue;

            _agent.SetPath(_path);
            return;
        }

        // Nenhum ponto válido: espera e tenta outra vez
        _waiting = true;
        _waitTimer = 1f;
    }

    // ---------- Perseguição ----------

    private void UpdateChase(bool inCorridor, bool sees, Vector3 playerNavPos)
    {
        // Jogador saiu do corredor: perde-o logo
        if (!inCorridor)
        {
            StartWander();
            return;
        }

        if (sees)
        {
            _timeSinceSeen = 0f;
            _lastKnownPos = playerNavPos;
        }
        else
        {
            _timeSinceSeen += Time.deltaTime;
            bool reachedLast = !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.2f;
            if (_timeSinceSeen >= _loseSightTime && reachedLast)
            {
                StartWander();
                return;
            }
            if (_timeSinceSeen >= _loseSightTime * 3f) // segurança
            {
                StartWander();
                return;
            }
        }

        // Atualiza o destino a cada 0.1 s em vez de todos os frames
        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f)
        {
            _repathTimer = 0.1f;
            _agent.SetDestination(_lastKnownPos);
        }

        // Caminho impossível (ex.: jogador num corredor desligado): desiste
        if (!_agent.pathPending && _agent.pathStatus == NavMeshPathStatus.PathInvalid)
            StartWander();
    }

    private void StartChase(Vector3 playerNavPos)
    {
        _state = State.Chase;
        _agent.speed = _runSpeed;
        _agent.stoppingDistance = _stopDistance;
        _waiting = false;
        _timeSinceSeen = 0f;
        _lastKnownPos = playerNavPos;
        _repathTimer = 0f;
    }

    // ---------- Sensores ----------

    /// <summary>
    /// Posição do jogador projetada no corredor; false se os pés dele não estiverem num corredor.
    /// </summary>
    private bool TryGetPlayerCorridorPos(out Vector3 navPos)
    {
        Vector3 feet = _target.position;
        if (_targetController != null)
        {
            Bounds b = _targetController.bounds;
            feet = new Vector3(b.center.x, b.min.y, b.center.z);
        }

        if (NavMesh.SamplePosition(feet, out NavMeshHit hit, _corridorTolerance, _areaMask))
        {
            navPos = hit.position;
            return true;
        }

        navPos = default;
        return false;
    }

    /// <summary>
    /// Distância + ângulo + linha de visão livre (sem paredes pelo meio).
    /// </summary>
    private bool CanSeePlayer()
    {
        Vector3 origin = _eyes.position;
        Vector3 targetPoint = _targetController != null
            ? _targetController.bounds.center
            : _target.position + Vector3.up * 1.4f;

        Vector3 toTarget = targetPoint - origin;
        float distance = toTarget.magnitude;
        if (distance > _viewDistance) return false;

        if (Vector3.Angle(_eyes.forward, toTarget) > _viewAngle * 0.5f) return false;

        // Procura o que está mais perto no caminho, ignorando o próprio zombie
        int count = Physics.RaycastNonAlloc(origin, toTarget / distance, _hits, distance + 0.1f,
            _sightMask, QueryTriggerInteraction.Ignore);

        float closest = float.MaxValue;
        Transform closestT = null;
        for (int i = 0; i < count; i++)
        {
            Transform t = _hits[i].transform;
            if (t.IsChildOf(transform)) continue;
            if (_hits[i].distance < closest)
            {
                closest = _hits[i].distance;
                closestT = t;
            }
        }

        return closestT != null && (closestT == _target || closestT.IsChildOf(_target));
    }

    private static float PathLength(NavMeshPath path)
    {
        Vector3[] c = path.corners;
        float len = 0f;
        for (int i = 1; i < c.Length; i++) len += Vector3.Distance(c[i - 1], c[i]);
        return len;
    }

    private void OnDrawGizmosSelected()
    {
        Transform e = _eyes != null ? _eyes : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(e.position, _viewDistance);

        Vector3 left = Quaternion.Euler(0f, -_viewAngle * 0.5f, 0f) * e.forward;
        Vector3 right = Quaternion.Euler(0f, _viewAngle * 0.5f, 0f) * e.forward;
        Gizmos.DrawRay(e.position, left * _viewDistance);
        Gizmos.DrawRay(e.position, right * _viewDistance);
    }
}
