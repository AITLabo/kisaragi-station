using UnityEngine;
using TMPro;

// きさらぎ駅ループ – 違和感ヒント表示UI
// GameManager.OnProgressAdvanced を購読し、現在の調査目標を画面左下に表示する。
public class AnomalyHintUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI progressText;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnProgressAdvanced += OnHintUpdated;
            // 初期表示
            OnHintUpdated(GameManager.Instance.GetCurrentHint(), GameManager.Instance.currentProgress);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnProgressAdvanced -= OnHintUpdated;
    }

    private void OnHintUpdated(string hint, int progress)
    {
        if (hintText != null)
            hintText.text = hint;

        if (progressText != null)
        {
            int total = GameManager.Instance?.correctSequence?.Length ?? 10;
            progressText.text = $"{progress} / {total}";
        }
    }
}
