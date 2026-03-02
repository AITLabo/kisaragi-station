using UnityEngine;

// きさらぎ駅ループ – 進行・歪みの中枢管理
// Singleton。シーン内に1つだけ配置する。
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ──────────────────────────────────────
    // 正解シーケンス（Inspectorで並び替え可能）
    // ──────────────────────────────────────
    [Header("Sequence Settings")]
    [Tooltip("正解順に並べたアクションID一覧（きさらぎ駅の違和感）")]
    public string[] correctSequence = new string[]
    {
        "station_sign",  // 1. 駅名が「きさらぎ」ではない
        "clock",         // 2. 時計が逆回り
        "timetable",     // 3. 時刻表が白紙
        "announcement",  // 4. 放送が日本語ではない
        "bench",         // 5. ベンチに荷物だけある
        "gate",          // 6. 改札が無人で開いている
        "poster",        // 7. ポスターの人物が消えている
        "phone",         // 8. 公衆電話が鳴っている
        "track",         // 9. 線路が片側だけない
        "light"          // 10. 蛍光灯が1本だけ消えている
    };

    [Header("Progress")]
    [ReadOnly] public int currentProgress = 0;

    [Header("Loop (設計: ループ回数・クリア状態)")]
    [ReadOnly] public int CurrentLoop = 1;
    [ReadOnly] public bool IsGameCleared = false;

    // ──────────────────────────────────────
    // 歪み管理
    // ──────────────────────────────────────
    [Header("Distortion Thresholds")]
    public int lowThreshold      = 2;
    public int mediumThreshold   = 5;
    public int highThreshold     = 8;
    public int collapseThreshold = 12;

    [ReadOnly] public int distortionValue = 0;
    [ReadOnly] public DistortionLevel currentDistortion = DistortionLevel.None;

    // ──────────────────────────────────────
    // 依存コンポーネント参照
    // ──────────────────────────────────────
    [Header("References")]
    [SerializeField] private AnomalyController anomalyController;
    [SerializeField] private AudioManager      audioManager;
    [SerializeField] private LogManager        logManager;
    [SerializeField] private LoopController    loopController;

    // ──────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ValidateReferences();
    }

    private void ValidateReferences()
    {
        if (anomalyController == null) Debug.LogError("[GameManager] AnomalyController が未設定です");
        if (audioManager      == null) Debug.LogError("[GameManager] AudioManager が未設定です");
        if (logManager        == null) Debug.LogError("[GameManager] LogManager が未設定です");
        if (loopController    == null) Debug.LogError("[GameManager] LoopController が未設定です");
    }

    // ──────────────────────────────────────
    // 外部から呼ぶ主要API
    // ──────────────────────────────────────

    /// <summary>プレイヤーが何かに触れた時に呼ぶ。正否を自動判定。</summary>
    public void ValidateAction(string actionID)
    {
        if (currentProgress >= correctSequence.Length)
        {
            Debug.Log("[GameManager] すでにクリア済み");
            return;
        }

        if (actionID == correctSequence[currentProgress])
        {
            AdvanceProgress(actionID);
        }
        else
        {
            TriggerWrongAction(actionID);
        }
    }

    private void AdvanceProgress(string actionID)
    {
        currentProgress++;
        IncreaseDistortionSoft();
        logManager?.SaveLog(actionID, "correct");

        if (currentProgress >= correctSequence.Length)
        {
            TriggerEnding();
        }
        else
        {
            Debug.Log($"[GameManager] 正解: {actionID} | 進行: {currentProgress}/{correctSequence.Length}");
        }
    }

    private void TriggerWrongAction(string actionID)
    {
        IncreaseDistortionHard();
        logManager?.SaveLog(actionID, "wrong");
        NextLoop();
        loopController?.TriggerSoftLoop();
        Debug.Log($"[GameManager] 不正解: {actionID} | 歪み値: {distortionValue} | ループ: {CurrentLoop}");
    }

    /// <summary>ループを1進め、LoopManager に演出適用を依頼（不正解時 or ActionManager.FailSequence から呼ばれる）</summary>
    public void NextLoop()
    {
        CurrentLoop++;
        if (LoopManager.Instance != null)
            LoopManager.Instance.ApplyLoopSettings(CurrentLoop);
    }

    /// <summary>10正解完了時に ActionManager から呼ばれる</summary>
    public void ClearGame()
    {
        IsGameCleared = true;
        TriggerEnding();
    }

    /// <summary>セーブロード用：ループ状態を復元し演出を再適用</summary>
    public void LoadState(int loop)
    {
        CurrentLoop = Mathf.Max(1, loop);
        if (LoopManager.Instance != null)
            LoopManager.Instance.ApplyLoopSettings(CurrentLoop);
    }

    // ──────────────────────────────────────
    // 歪み操作
    // ──────────────────────────────────────

    public void IncreaseDistortionSoft()
    {
        distortionValue += 1;
        RefreshDistortionLevel();
    }

    public void IncreaseDistortionHard()
    {
        distortionValue += 2;
        RefreshDistortionLevel();
    }

    private void RefreshDistortionLevel()
    {
        DistortionLevel newLevel;

        if      (distortionValue >= collapseThreshold) newLevel = DistortionLevel.Collapse;
        else if (distortionValue >= highThreshold)     newLevel = DistortionLevel.High;
        else if (distortionValue >= mediumThreshold)   newLevel = DistortionLevel.Medium;
        else if (distortionValue >= lowThreshold)      newLevel = DistortionLevel.Low;
        else                                           newLevel = DistortionLevel.None;

        if (newLevel != currentDistortion)
        {
            currentDistortion = newLevel;
            anomalyController?.ApplyDistortion(currentDistortion);
            audioManager?.ApplyDistortion(currentDistortion);
        }
    }

    // ──────────────────────────────────────
    // ループリセット（LoopControllerから呼ばれる）
    // ──────────────────────────────────────
    public void ResetLoop()
    {
        Debug.Log("[GameManager] ソフトループリセット");
    }

    // ──────────────────────────────────────
    // クリアエンディング
    // ──────────────────────────────────────
    private void TriggerEnding()
    {
        Debug.Log("[GameManager] ★ エンディング: 現実へ帰還 ★");
        var sceneFlow = FindObjectOfType<SceneFlowManager>();
        if (sceneFlow != null)
            sceneFlow.LoadEscapeScene();
        else
            Debug.LogWarning("[GameManager] SceneFlowManager が見つかりません。シーンに SceneFlowManager オブジェクトを配置してください。");
    }
}

// ──────────────────────────────────────
// Inspector上でreadonly表示するカスタム属性
// ──────────────────────────────────────
#if UNITY_EDITOR
public class ReadOnlyAttribute : UnityEngine.PropertyAttribute { }
#else
public class ReadOnlyAttribute : System.Attribute { }
#endif
