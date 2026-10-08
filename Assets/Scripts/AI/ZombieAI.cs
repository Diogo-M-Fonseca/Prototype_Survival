using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Zombie que só anda em corredores (área de NavMesh "Corridor").
/// Passeia pelo corredor; se vir o jogador corre atrás dele; se o jogador sair do corredor
/// (ou o zombie o perder de vista durante algum tempo) volta a passear.
/// As paredes são respeitadas pelo NavMesh, por isso o zombie nunca bate nelas nem as atravessa.
/// Controla o Animator através dos parâmetros "Speed" (Float) e "Attack" (Trigger).
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
    [Tooltip("Distância a que o zombie pára junto ao jogador.")]
    [SerializeField, Min(0.1f)] private float _stopDistance = 1.2f;

    [Header("Som")]
    [Tooltip("AudioSource da voz do zombie. Se vazio, é criado um automaticamente.")]
    [SerializeField] private AudioSource _voice;

    [Tooltip("Sons tocados ao acaso, um de cada vez, enquanto passeia.")]
    [SerializeField] private AudioClip[] _casualClips;

    [Tooltip("Som em loop enquanto persegue o jogador.")]
    [SerializeField] private AudioClip _chaseClip;

    [SerializeField, Range(0f, 1f)] private float _voiceVolume = 1f;

    [Tooltip("Distância a partir da qual o som deixa de se ouvir.")]
    [SerializeField, Min(1f)] private float _voiceMaxDistance = 25f;

    [Tooltip("Pausa mínima e máxima (s) entre sons de passeio.")]
    [SerializeField, Min(0f)] private float _casualGapMin = 3f;
    [SerializeField, Min(0f)] private float _casualGapMax = 8f;

    [Header("Ataque")]
    [Tooltip("Margem extra à distância de paragem dentro da qual o zombie ataca.")]
    [SerializeField, Min(0f)] private float _attackRangeMargin = 0.3f;

    [Tooltip("Segundos entre ataques.")]
    [SerializeField, Min(0.1f)] private float _attackCooldown = 1.5f;

    [Tooltip("Dano causado ao jogador em cada golpe.")]
    [SerializeField, Min(0)] private int _attackDamage = 10;

    [Tooltip("Segundos entre o início do ataque e o momento em que o golpe acerta (ajusta ao clip de ataque).")]
    [SerializeField, Min(0f)] private float _attackHitDelay = 0.4f;

    [Tooltip("Velocidade com que o zombie se vira para o jogador ao atacar.")]
    [SerializeField, Min(0f)] private float _attackTurnSpeed = 10f;

    [Header("Animação")]
    [Tooltip("Animator do zombie. Se vazio, procura nos filhos.")]
    [SerializeField] private Animator _animator;

    [SerializeField, Min(0f)] private float _speedDampTime = 0.1f;

    [Header("Depuração")]
    [Tooltip("Com o zombie selecionado na Scene View (em Play) mostra estado, velocidades e distância.")]
    [SerializeField] private bool _showDebug = true;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private NavMeshAgent _agent;
    private CharacterController _targetController;
    private NavMeshPath _path;
    private int _areaMask;

    private State _state = State.Wander;
    private Vector3 _lastKnownPos;
    private float _timeSinceSeen;
    private float _waitTimer;
    private float _repathTimer;
    private float _nextAttackTime;
    private float _pendingHitTime = -1f;
    private PlayerStats _targetStats;
    private Vector3 _lastVisualPos;
    private float _visualSpeed;
    private bool _dbgSees;
    private bool _dbgInCorridor;
    private bool _waiting;
    private float _nextCasualTime;
    private int _lastCasual = -1;

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
        SetupVoice();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        int area = NavMesh.GetAreaFromName(_corridorArea);
        if (area < 0)
        {
            Debug.LogError($"ZombieAI: a área de NavMesh '{_corridorArea}' não existe.", this);
            enabled = false;
            return;
        }

        _areaMask = 1 << area;
        _agent.areaMask = _areaMask; // o zombie só consegue andar nesta área
        SanitizeMovement();
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
        _targetStats = _target.GetComponentInParent<PlayerStats>();
        if (_targetStats == null) _targetStats = FindFirstObjectByType<PlayerStats>();
        if (_targetStats == null) Debug.LogWarning("ZombieAI: não foi encontrado o PlayerStats; o zombie não causa dano.", this);

        // Se o zombie nasceu fora do corredor, encosta-o ao corredor mais próximo
        if (!_agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, _areaMask))
            _agent.Warp(hit.position);

        StartWander();
    }

    private void Update()
    {
        UpdateAnimator();
        UpdateVoice();
        UpdateAttackHit();
        TrackVisualSpeed();

        if (!_agent.isOnNavMesh) return;

        // O jogador só conta se os seus pés estiverem no corredor
        bool inCorridor = TryGetPlayerCorridorPos(out Vector3 playerNavPos);
        bool sees = inCorridor && CanSeePlayer();
        _dbgInCorridor = inCorridor;

        if (_state == State.Wander) UpdateWander(sees, playerNavPos);
        else UpdateChase(inCorridor, sees, playerNavPos);
    }

    // ---------- Animação ----------

    private void UpdateAnimator()
    {
        if (_animator == null) return;

        // Velocidade real do agent (0 parado, ~walk a passear, ~run a perseguir)
        _animator.SetFloat(SpeedHash, _agent.velocity.magnitude, _speedDampTime, Time.deltaTime);
    }

    // ---------- Som ----------

    private void SetupVoice()
    {
        if (_voice == null) _voice = GetComponent<AudioSource>();
        if (_voice == null) _voice = gameObject.AddComponent<AudioSource>();

        _voice.playOnAwake = false;
        _voice.spatialBlend = 1f; // som 3D: ouve-se de onde o zombie está
        _voice.rolloffMode = AudioRolloffMode.Linear;
        _voice.minDistance = 2f;
        _voice.maxDistance = _voiceMaxDistance;
        _voice.volume = _voiceVolume;
    }

    private void OnStartChaseVoice()
    {
        if (_voice == null || _chaseClip == null) return;

        _voice.Stop();
        _voice.clip = _chaseClip;
        _voice.loop = true;
        _voice.Play();
    }

    private void OnStartWanderVoice()
    {
        if (_voice == null) return;

        _voice.Stop();
        _voice.loop = false;
        _nextCasualTime = Time.time + Random.Range(_casualGapMin, _casualGapMax);
    }

    private void UpdateVoice()
    {
        if (_voice == null || _state != State.Wander) return;
        if (_casualClips == null || _casualClips.Length == 0) return;
        if (_voice.isPlaying || Time.time < _nextCasualTime) return;

        // Escolhe um som ao acaso, evitando repetir o último
        int index = Random.Range(0, _casualClips.Length);
        if (_casualClips.Length > 1 && index == _lastCasual) index = (index + 1) % _casualClips.Length;
        _lastCasual = index;

        AudioClip clip = _casualClips[index];
        if (clip == null) return;

        _voice.clip = clip;
        _voice.loop = false;
        _voice.Play();
        _nextCasualTime = Time.time + clip.length + Random.Range(_casualGapMin, _casualGapMax);
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
        _agent.isStopped = false;
        _agent.autoBraking = true;
        OnStartWanderVoice();
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

        // Perto do jogador o zombie sente-o mesmo fora do campo de visão
        // (senão não se virava para ele e acabava por desistir ao chegar)
        float flat = FlatDistanceToTarget();
        float attackRange = _stopDistance + _attackRangeMargin;
        bool near = flat <= attackRange;
        bool aware = sees || near;
        _dbgSees = aware;

        if (aware)
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

        // Pára quando está ao alcance e só volta a andar quando o jogador se afasta um pouco
        // (a histerese evita arrancar e travar de cada vez que o jogador mexe)
        if (near) _agent.isStopped = true;
        else if (flat > attackRange + 0.3f) _agent.isStopped = false;

        TryAttack(aware);

        // Só pede um caminho novo se o jogador se moveu (verifica a cada 0.2 s)
        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f)
        {
            _repathTimer = 0.2f;
            if (!_agent.isStopped && (!_agent.hasPath || (_agent.destination - _lastKnownPos).sqrMagnitude > 0.09f))
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
        _agent.isStopped = false;
        _agent.autoBraking = false; // trava-se à mão em UpdateChase, sem abrandar a cada passo
        OnStartChaseVoice();
        _waiting = false;
        _timeSinceSeen = 0f;
        _lastKnownPos = playerNavPos;
        _repathTimer = 0f;
    }

    // ---------- Ataque ----------

    private void TryAttack(bool sees)
    {
        if (!sees) return;

        float dist = FlatDistanceToTarget();
        if (dist > _stopDistance + _attackRangeMargin) return;

        // Vira-se para o jogador (o agent pára mas não garante que fica virado para ele)
        Vector3 flat = _target.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(flat),
                _attackTurnSpeed * Time.deltaTime);
        }

        if (Time.time < _nextAttackTime) return;

        _nextAttackTime = Time.time + _attackCooldown;
        if (_animator != null) _animator.SetTrigger(AttackHash);

        _pendingHitTime = Time.time + _attackHitDelay; // o dano é aplicado em UpdateAttackHit
    }

    /// <summary>
    /// Aplica o dano quando chega o momento do golpe, se o jogador ainda estiver ao alcance.
    /// </summary>
    private void UpdateAttackHit()
    {
        if (_pendingHitTime < 0f || Time.time < _pendingHitTime) return;
        _pendingHitTime = -1f;

        if (_targetStats == null || _attackDamage <= 0) return;

        // O golpe só acerta se o jogador não se afastou entretanto
        float dist = FlatDistanceToTarget();
        if (dist > _stopDistance + _attackRangeMargin + 0.5f) return;

        _targetStats.TakeDamage(_attackDamage);
    }

    /// <summary>
    /// Distância horizontal ao jogador. Ignora a altura, porque a raiz do jogador costuma estar
    /// ao nível do centro/cabeça e não dos pés, o que fazia a distância 3D ficar sempre acima do alcance.
    /// </summary>
    private float FlatDistanceToTarget()
    {
        Vector3 d = _target.position - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    // ---------- Movimento ----------

    /// <summary>
    /// Garante que só o NavMeshAgent move o zombie: root motion e física a mexer ao mesmo tempo
    /// duplicam a velocidade e fazem o zombie travar.
    /// </summary>
    private void SanitizeMovement()
    {
        foreach (Animator a in GetComponentsInChildren<Animator>(true))
        {
            if (!a.applyRootMotion) continue;
            a.applyRootMotion = false;
            Debug.LogWarning($"ZombieAI: desliguei o Apply Root Motion do Animator '{a.name}' (duplicava o movimento do NavMeshAgent).", this);
        }

        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>(true))
        {
            if (rb.isKinematic) continue;
            rb.isKinematic = true;
            Debug.LogWarning($"ZombieAI: pus o Rigidbody '{rb.name}' como Kinematic (a física lutava com o NavMeshAgent).", this);
        }
    }

    /// <summary>
    /// Velocidade horizontal a que o modelo se move de facto (para comparar com a do agent).
    /// </summary>
    private void TrackVisualSpeed()
    {
        Transform visual = _animator != null ? _animator.transform : transform;
        Vector3 pos = visual.position;

        Vector3 delta = pos - _lastVisualPos;
        delta.y = 0f;
        if (Time.deltaTime > 0f) _visualSpeed = delta.magnitude / Time.deltaTime;

        _lastVisualPos = pos;
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

        // Alcance do ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _stopDistance + _attackRangeMargin);

#if UNITY_EDITOR
        if (_showDebug && Application.isPlaying && _agent != null && _target != null)
        {
            string info =
                $"{_state}   consciente: {_dbgSees}   no corredor: {_dbgInCorridor}\n" +
                $"agent speed {_agent.speed:0.0} | agent vel {_agent.velocity.magnitude:0.0} | modelo {_visualSpeed:0.0}\n" +
                $"distância {FlatDistanceToTarget():0.00} (alcance {_stopDistance + _attackRangeMargin:0.00})";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.4f, info);
        }
#endif
    }
}