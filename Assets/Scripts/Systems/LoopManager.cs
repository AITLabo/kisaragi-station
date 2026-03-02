using UnityEngine;

// きさらぎ駅 – ループごとの演出強度を適用（LoopDatabase 優先・データ駆動）
public class LoopManager : MonoBehaviour
{
    public static LoopManager Instance { get; private set; }

    [Header("Data (推奨)")]
    [Tooltip("設定時は ApplyLoopSettings で LoopData を適用")]
    [SerializeField] private LoopDatabase loopDatabase;

    [Header("Optional References")]
    [SerializeField] private EnvironmentManager environmentManager;
    [SerializeField] private EventManager eventManager;
    [Tooltip("Station 用。設定時は LoopVariantData で床・駅名・Fog・Watcher・点滅を適用")]
    [SerializeField] private VariantController variantController;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>ループ番号に応じた演出を一括適用（コードを増やさず演出を増やせる）</summary>
    public void ApplyLoopSettings(int loopIndex)
    {
        if (loopDatabase != null)
        {
            var data = loopDatabase.GetLoop(loopIndex);
            if (data != null)
            {
                environmentManager?.Apply(data);
                variantController?.ApplyVariant(loopIndex - 1);
                Debug.Log($"[LoopManager] ループ {loopIndex} を LoopData で適用しました");
            }
        }
        else
        {
            environmentManager?.UpdateEnvironment(loopIndex);
            variantController?.ApplyVariant(loopIndex - 1);
        }

        eventManager?.UpdateEvents(loopIndex);
    }
}
