using UnityEngine;

// きさらぎ駅 – Watcher（Steam仕様）: 停止時・画面端15%・個体差シード・見た瞬間フレーム落ち
public class WatcherAI : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject dummyPrefab;
    [Tooltip("ループ2未満では出現しない。立ち止まったときのチェック間隔用")]
    [SerializeField] private float spawnCheckInterval = 0.5f;
    [Tooltip("起動時に一度だけ決定。配信者ごとに違う Watcher にする")]
    [SerializeField] private bool usePerLaunchSeed = true;

    [Header("画面端（Steam: 15%で「端にだけ」）")]
    [Tooltip("Viewport 端とみなす幅。0.15 = 左右上下 15%")]
    [Range(0.1f, 0.4f)] [SerializeField] private float edgeViewportMargin = 0.15f;

    [Header("直視検知（角度 15°）")]
    [SerializeField] private float lookAngleDegrees = 15f;

    [Header("接近（見てない時のみ。ループ別にこれ以上近づかない）")]
    [SerializeField] private float approachDistanceLoop2 = 20f;
    [SerializeField] private float approachDistanceLoop3 = 10f;
    [SerializeField] private float approachDistanceLoop4 = 5f;
    [SerializeField] private float approachMoveSpeed = 2f;

    [Header("見た瞬間フレーム落ち（Steam仕様・無意識の違和感）")]
    [SerializeField] private bool enableFrameDropOnLook = true;
    [SerializeField] private int frameDropTarget = 50;
    [SerializeField] private float frameDropDuration = 0.2f;

    [Header("Optional（心理学設計では音なし）")]
    [SerializeField] private AudioSource behindFootstepSource;
    [SerializeField] private AudioClip behindFootstepClip;
    [Tooltip("3回直視で消滅＋ループ進行する旧仕様を使うか")]
    [SerializeField] private bool useThreeLookDisappear;
    [SerializeField] private float destroyLookDuration = 0.5f;
    [SerializeField] private int lookCountToDisappear = 3;

    private float _approachTimer;
    private float _spawnCheckTimer;
    private GameObject _currentDummy;
    private Transform _playerTransform;
    private PlayerController _playerController;
    private static int _watcherSeed = -1;
    private int _savedTargetFrameRate = 60;
    private bool _didFrameDropThisSpawn;

    private void Awake()
    {
        if (usePerLaunchSeed && _watcherSeed < 0)
            _watcherSeed = Random.Range(0, 100000);
    }

    private void Start()
    {
        var cam = Camera.main;
        _playerTransform = cam != null ? cam.transform : null;
        if (_playerTransform != null && _playerTransform.parent != null)
            _playerTransform = _playerTransform.root;
        _playerController = _playerTransform != null ? _playerTransform.GetComponent<PlayerController>() : null;
        _savedTargetFrameRate = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60;
    }

    private void RestoreFPS()
    {
        Application.targetFrameRate = _savedTargetFrameRate;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentLoop < 2) return;
        if (spawnPoints == null || spawnPoints.Length == 0 || dummyPrefab == null) return;

        // 立ち止まったときのみ・画面端にのみ出現
        _spawnCheckTimer += Time.deltaTime;
        if (_spawnCheckTimer >= spawnCheckInterval)
        {
            _spawnCheckTimer = 0f;
            if (_currentDummy == null && IsPlayerStopped())
            {
                float chance = 0.15f * (GameManager.Instance.CurrentLoop - 1);
                if (Random.value < chance)
                    TrySpawnAtScreenEdge();
            }
        }

        if (_currentDummy == null) return;

        bool looking = IsPlayerLookingAtDummy();

        if (looking)
        {
            _approachTimer = 0f;
            if (enableFrameDropOnLook && !_didFrameDropThisSpawn)
            {
                _didFrameDropThisSpawn = true;
                Application.targetFrameRate = frameDropTarget;
                Invoke(nameof(RestoreFPS), frameDropDuration);
            }
            if (useThreeLookDisappear)
                UpdateThreeLookDisappear();
        }
        else
        {
            _approachTimer += Time.deltaTime;
            if (_approachTimer > 0.5f)
            {
                _approachTimer = 0f;
                MoveCloser();
            }
        }
    }

    private float GetApproachDistance()
    {
        if (GameManager.Instance == null) return approachDistanceLoop2;
        return GameManager.Instance.CurrentLoop switch
        {
            2 => approachDistanceLoop2,
            3 => approachDistanceLoop3,
            _ => approachDistanceLoop4
        };
    }

    private bool IsPlayerStopped()
    {
        return _playerController != null && !_playerController.IsMoving;
    }

    private bool IsPlayerLookingAtDummy()
    {
        if (_currentDummy == null || Camera.main == null) return false;
        Vector3 dir = (_currentDummy.transform.position - Camera.main.transform.position).normalized;
        float angle = Vector3.Angle(Camera.main.transform.forward, dir);
        return angle < lookAngleDegrees;
    }

    private void MoveCloser()
    {
        if (_currentDummy == null || _playerTransform == null) return;
        var d = _currentDummy.transform;
        Vector3 toPlayer = _playerTransform.position - d.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;
        float minDist = GetApproachDistance();
        if (dist <= minDist) return;
        Vector3 dir = toPlayer.normalized;
        float step = approachMoveSpeed * Time.deltaTime;
        if (dist - step < minDist) step = dist - minDist;
        d.position += dir * step;
    }

    private int _lookCount;
    private float _lookTimer;

    private void UpdateThreeLookDisappear()
    {
        if (!useThreeLookDisappear) return;
        _lookTimer += Time.deltaTime;
        if (_lookTimer >= destroyLookDuration)
        {
            _lookTimer = 0f;
            _lookCount++;
            if (_lookCount >= lookCountToDisappear)
            {
                OnDummyDisappearAfterThreeLooks();
            }
        }
    }

    private void OnDummyDisappearAfterThreeLooks()
    {
        if (_currentDummy != null)
        {
            Destroy(_currentDummy);
            _currentDummy = null;
        }
        _lookCount = 0;
        _lookTimer = 0f;
        _approachTimer = 0f;
        if (behindFootstepSource != null && behindFootstepClip != null)
            behindFootstepSource.PlayOneShot(behindFootstepClip);
        if (GameManager.Instance != null)
            GameManager.Instance.NextLoop();
    }

    private void TrySpawnAtScreenEdge()
    {
        if (Camera.main == null) return;
        Camera cam = Camera.main;
        float margin = edgeViewportMargin;
        // 画面端 = viewport (0,0)-(margin,1), (1-margin,0)-(1,1), (0,0)-(1,margin), (0,1-margin)-(1,1)
        Transform best = null;
        float bestScore = -1f;
        foreach (var p in spawnPoints)
        {
            Vector3 vp = cam.WorldToViewportPoint(p.position);
            if (vp.z <= 0) continue;
            bool atEdge = vp.x <= margin || vp.x >= 1f - margin || vp.y <= margin || vp.y >= 1f - margin;
            if (!atEdge) continue;
            if (IsSpawnPointVisibleCenter(vp)) continue;
            float edgeScore = Mathf.Max(vp.x, 1f - vp.x, vp.y, 1f - vp.y);
            if (edgeScore > bestScore) { bestScore = edgeScore; best = p; }
        }
        if (best == null) return;
        if (IsSpawnPointVisible(best)) return;

        _currentDummy = Instantiate(dummyPrefab, best.position, best.rotation);
        _lookCount = 0;
        _lookTimer = 0f;
        _approachTimer = 0f;
        _didFrameDropThisSpawn = false;

        int seed = usePerLaunchSeed ? _watcherSeed : Random.Range(0, 100000);
        var variation = _currentDummy.GetComponent<WatcherVariation>();
        if (variation == null) variation = _currentDummy.GetComponentInChildren<WatcherVariation>();
        if (variation != null) variation.ApplySeed(seed);

        TimelineDirectorManager.Instance?.PlayDummyAppear();
    }

    private bool IsSpawnPointVisibleCenter(Vector3 viewportPoint)
    {
        float center = 0.5f;
        float margin = 0.35f;
        return Mathf.Abs(viewportPoint.x - center) < margin && Mathf.Abs(viewportPoint.y - center) < margin;
    }

    private bool IsSpawnPointVisible(Transform target)
    {
        if (Camera.main == null) return false;
        Vector3 vp = Camera.main.WorldToViewportPoint(target.position);
        return vp.x >= 0 && vp.x <= 1 && vp.y >= 0 && vp.y <= 1 && vp.z > 0;
    }
}
