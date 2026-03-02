using UnityEngine;

// きさらぎ駅ループ – インタラクション可能オブジェクトの実装サンプル
// このスクリプトを各「調べるオブジェクト」に付けてActionIDを設定する
// 例：改札 → ActionID = "gate"、時計 → ActionID = "clock"
public class CorrectObject : MonoBehaviour, IInteractable
{
    [Header("Sequence Settings")]
    [Tooltip("このオブジェクトのシーケンスID。GameManagerのcorrectSequenceと一致させること")]
    [SerializeField] private string actionID;

    [Header("Visual Feedback")]
    [SerializeField] private bool destroyAfterInteract = false;
    [SerializeField] private GameObject highlightEffect;

    public string ActionID => actionID;

    public void OnInteract()
    {
        if (highlightEffect != null) highlightEffect.SetActive(false);

        // train_door の場合はきさらぎ駅へ遷移
        if (actionID == "train_door")
        {
            var tc = Object.FindObjectOfType<TrainController>();
            if (tc != null)
                tc.OnBoardTrain();
            else if (SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.LoadKisaragiScene();
            return;
        }

        if (destroyAfterInteract)
            Destroy(gameObject, 0.1f);
    }

    private void OnValidate()
    {
        // Play中のみ警告（Editorスクリプトの AddComponent 直後は actionID 未設定のため除外）
        if (string.IsNullOrEmpty(actionID) && gameObject.scene.isLoaded && Application.isPlaying)
            Debug.LogWarning($"[CorrectObject] {gameObject.name} の ActionID が未設定です");
    }
}
