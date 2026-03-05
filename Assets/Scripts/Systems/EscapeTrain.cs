using UnityEngine;
using System.Collections;

// きさらぎ駅ループ – 脱出電車
// 10個の異変を全解決すると南端(spawnZ)から出現してホームに停車。
// ドア開放後、乗車ゾーンにプレイヤーが入ると EscapeScene へ遷移。
// Rigidbody(isKinematic) が必須（子の BoardingZone トリガーを受け取るため）
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class EscapeTrain : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("出現Z座標（北端・+Z側から来る）")]
    public float spawnZ   = 80f;
    [Tooltip("停車Z座標")]
    public float stopZ    = -10f;
    [Tooltip("最高接近速度 m/s")]
    public float maxSpeed = 25f;

    [Header("References")]
    [Tooltip("スライドドアオブジェクト（停車後に上方へ開く）")]
    public GameObject doorObject;
    [Tooltip("乗車判定トリガーコライダー（停車後に有効化）")]
    public Collider   boardingZone;
    [Tooltip("線路側バリア（到着時に無効化しプレイヤーが近づけるようにする）")]
    public Collider   platformBarrier;

    [Header("Debug")]
    [Tooltip("0より大きい値を設定するとPlay開始からその秒数後に強制到着（テスト用）。0=無効")]
    public float testArrivalDelay = 10f;
    [Tooltip("true: プレイ開始時に Z=spawnZ へ移動して非表示（本番）\nfalse: ホームに表示したまま（位置確認用）")]
    public bool startHidden = false;

    private AudioSource _audio;
    private bool _triggered;
    private Renderer[] _renderers;

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;
        // 有効状態のうちにキャッシュ（無効化後は GetComponentsInChildren で取得できない）
        _renderers = GetComponentsInChildren<Renderer>(true);
        Debug.Log($"[EscapeTrain] Awake: レンダラー {_renderers.Length} 個キャッシュ済み");
    }

    private void Start()
    {
        if (boardingZone != null) boardingZone.enabled = false;

        // doorObject 未設定時は子から名前で自動検索
        if (doorObject == null)
            doorObject = transform.Find("SlideDoor")?.gameObject;
        Debug.Log($"[EscapeTrain] doorObject = {(doorObject != null ? doorObject.name : "null (ドアアニメなし)")}");

        // SetActive(false) はコルーチン不可になるため使わない。
        // 代わりに Renderer を無効化して見た目だけ隠す。
        if (startHidden || testArrivalDelay > 0f)
        {
            var p = transform.position;
            p.z = spawnZ;
            transform.position = p;
            SetVisible(false);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnProgressAdvanced += OnProgress;

        if (testArrivalDelay > 0f)
            StartCoroutine(TestArrivalTimer());
    }

    private void SetVisible(bool visible)
    {
        foreach (var r in _renderers) if (r != null) r.enabled = visible;
    }

    private IEnumerator TestArrivalTimer()
    {
        yield return new WaitForSeconds(testArrivalDelay);
        if (!_triggered)
        {
            _triggered = true;
            StartCoroutine(ArrivalSequence());
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnProgressAdvanced -= OnProgress;
    }

    private void OnProgress(string _, int progress)
    {
        if (_triggered) return;
        if (GameManager.Instance == null) return;
        if (progress < GameManager.Instance.correctSequence.Length) return;

        _triggered = true;
        StartCoroutine(ArrivalSequence());
    }

    // ── 到着シーケンス ──────────────────────────────────────
    private IEnumerator ArrivalSequence()
    {
        // 南端に配置して出現
        var p = transform.position;
        p.z = spawnZ;
        transform.position = p;
        SetVisible(true);

        if (_audio != null) { _audio.loop = true; _audio.Play(); }

        Debug.Log("[EscapeTrain] 到着シーケンス開始");

        // 接近 → 減速 → 停車（Z=spawnZ から Z=stopZ へ -Z 方向に進む）
        while (transform.position.z > stopZ)
        {
            float dist  = transform.position.z - stopZ;
            float speed = Mathf.Lerp(1.5f, maxSpeed, Mathf.Clamp01(dist / 25f));
            transform.Translate(-Vector3.forward * speed * Time.deltaTime, Space.World);
            yield return null;
        }

        // ぴったり停車
        p = transform.position;
        p.z = stopZ;
        transform.position = p;
        if (_audio != null) _audio.Stop();
        Debug.Log($"[EscapeTrain] 停車完了 worldPos={transform.position} レンダラー数={_renderers.Length}");

        // 線路側バリア解除（プレイヤーがホーム端に近づけるよう）
        if (platformBarrier != null) platformBarrier.enabled = false;

        yield return new WaitForSeconds(1.0f);

        // ドア開放
        if (doorObject != null)
            yield return StartCoroutine(OpenDoor());

        yield return new WaitForSeconds(0.5f);

        // 乗車ゾーン有効化
        if (boardingZone != null) boardingZone.enabled = true;
        Debug.Log("[EscapeTrain] ドア開放完了。乗車可能です。");
    }

    private IEnumerator OpenDoor()
    {
        float elapsed = 0f;
        const float dur = 1.5f;
        Vector3 start = doorObject.transform.localPosition;
        Vector3 end   = start + Vector3.up * 2.1f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            doorObject.transform.localPosition = Vector3.Lerp(start, end, elapsed / dur);
            yield return null;
        }
        doorObject.transform.localPosition = end;
    }

    // Rigidbody(isKinematic) 経由で子 BoardingZone のトリガーを受け取る
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (boardingZone != null && !boardingZone.enabled) return;

        Debug.Log("[EscapeTrain] 乗車完了。EscapeScene へ遷移します。");
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.LoadEscapeScene();
        else
            Debug.LogWarning("[EscapeTrain] SceneFlowManager が見つかりません");
    }

    // ── デバッグ用：Editorから手動トリガー ──────────────────
    [ContextMenu("Debug: Trigger Arrival")]
    private void DebugTriggerArrival()
    {
        if (!Application.isPlaying) return;
        _triggered = true;
        StartCoroutine(ArrivalSequence());
    }
}
