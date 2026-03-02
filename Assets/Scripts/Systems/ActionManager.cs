using UnityEngine;

// きさらぎ駅 – 10個の正解行動の順番判定
// 「順番が命」：間違えたらループ++・プレイヤーを戻す・環境変化
// オブジェクト側は OnInteract で ActionManager.Instance.RegisterAction(actionID) を呼ぶ
public class ActionManager : MonoBehaviour
{
    public static ActionManager Instance { get; private set; }

    [Header("正解順（design_spec / design_complete 準拠）")]
    [Tooltip("この順番で触らないとループ")]
    [SerializeField] private string[] correctOrder = new string[]
    {
        "station_sign",
        "clock",
        "timetable",
        "announcement",
        "bench",
        "gate",
        "poster",
        "phone",
        "track",
        "light"
    };

    [Header("References")]
    [SerializeField] private LoopController loopController;

    private int _currentStep;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (loopController == null)
            loopController = FindObjectOfType<LoopController>();
    }

    /// <summary>プレイヤーがオブジェクトに触れたときに呼ぶ。正解順なら進行、違えばループ。</summary>
    public void RegisterAction(string actionID)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameCleared)
        {
            Debug.Log("[ActionManager] すでにクリア済み");
            return;
        }

        if (_currentStep < correctOrder.Length && correctOrder[_currentStep] == actionID)
        {
            _currentStep++;
            Debug.Log($"[ActionManager] 正解: {actionID} | 進行: {_currentStep}/{correctOrder.Length}");

            if (_currentStep >= correctOrder.Length)
                ClearGame();
        }
        else
        {
            FailSequence(actionID);
        }
    }

    private void FailSequence(string wrongActionID)
    {
        Debug.Log($"[ActionManager] 不正解: {wrongActionID} → ループへ");
        _currentStep = 0;

        if (GameManager.Instance != null)
            GameManager.Instance.NextLoop();

        if (loopController != null)
            loopController.TriggerSoftLoop();
        else
            Debug.LogWarning("[ActionManager] LoopController が未設定です。プレイヤーリセットは行われません。");
    }

    private void ClearGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ClearGame();
        Debug.Log("[ActionManager] ★ 10正解完了 → エンディングへ ★");
    }

    /// <summary>現在の正解ステップ数（デバッグ用）</summary>
    public int CurrentStep => _currentStep;

    /// <summary>正解順の長さ</summary>
    public int TotalSteps => correctOrder?.Length ?? 0;

    /// <summary>現在の正解ステップ（セーブ用）</summary>
    public int GetCurrentStep() => _currentStep;

    /// <summary>セーブロード用：ステップを復元</summary>
    public void SetStep(int step)
    {
        _currentStep = Mathf.Clamp(step, 0, TotalSteps);
    }
}
