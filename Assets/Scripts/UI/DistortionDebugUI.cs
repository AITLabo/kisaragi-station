using UnityEngine;
using TMPro;

// 開発用：歪みレベルをリアルタイム表示（リリース時はオフにする）
public class DistortionDebugUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private bool showInBuild = false;

    private void Update()
    {
#if !UNITY_EDITOR
        if (!showInBuild) { gameObject.SetActive(false); return; }
#endif
        if (GameManager.Instance == null || debugText == null) return;

        debugText.text =
            $"進行: {GameManager.Instance.currentProgress} / {GameManager.Instance.correctSequence.Length}\n" +
            $"歪み値: {GameManager.Instance.distortionValue}\n" +
            $"レベル: {GameManager.Instance.currentDistortion}";
    }
}
