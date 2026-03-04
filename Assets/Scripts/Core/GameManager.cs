using UnityEngine;
using System.Collections.Generic;

// きさらぎ駅ループ – 進行・歪みの中枢管理
// Singleton。シーン内に1つだけ配置する。
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ──────────────────────────────────────
    // 正解シーケンス（ゲーム開始時にランダムシャッフル）
    // ──────────────────────────────────────
    [Header("Sequence Settings")]
    [Tooltip("違和感のアクションID一覧。ゲーム開始時にシャッフルされる")]
    public string[] correctSequence = new string[]
    {
        "station_sign", "clock", "timetable", "announcement", "bench",
        "gate", "poster", "phone", "track", "light"
    };

    [Tooltip("trueでゲーム開始時にシーケンスをランダムシャッフルする")]
    [SerializeField] private bool randomizeSequence = true;

    // ヒント辞書（actionID → ホラー演出向けの曖昧なガイダンス）
    private static readonly Dictionary<string, string> s_hintMap = new Dictionary<string, string>
    {
        { "station_sign",  "駅名を確かめろ"           },
        { "clock",         "時を刻む音に耳を澄ませ"   },
        { "timetable",     "時刻表を調べろ"            },
        { "announcement",  "声が聞こえる方へ向かえ"   },
        { "bench",         "誰かが残した物がある"       },
        { "gate",          "改札を確認しろ"            },
        { "poster",        "壁のポスターを見ろ"        },
        { "phone",         "鳴り続ける電話がある"      },
        { "track",         "線路を見渡せ"              },
        { "light",         "暗い場所に手がかりがある"  },
    };

    /// <summary>正解 or 進行時に発火。引数: (次のヒント文字列, 現在の進行数)</summary>
    public event System.Action<string, int> OnProgressAdvanced;

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

    private void Start()
    {
        if (randomizeSequence)
            ShuffleSequence();

        // 最初のヒントをUIに通知
        NotifyHint();
    }

    // Fisher-Yates シャッフル
    private void ShuffleSequence()
    {
        for (int i = correctSequence.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string tmp = correctSequence[i];
            correctSequence[i] = correctSequence[j];
            correctSequence[j] = tmp;
        }
        Debug.Log("[GameManager] シーケンスをランダム化しました: " + string.Join("→", correctSequence));
    }

    public string GetCurrentHint()
    {
        if (currentProgress >= correctSequence.Length) return "全ての異変を確認した";
        string id = correctSequence[currentProgress];
        return s_hintMap.TryGetValue(id, out string hint) ? hint : id;
    }

    private void NotifyHint()
    {
        OnProgressAdvanced?.Invoke(GetCurrentHint(), currentProgress);
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
            NotifyHint();
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
