using UnityEngine;
using System.Collections;

// きさらぎ駅ループ – 廊下ループ処理
// 通路端に配置したInvisible TriggerがプレイヤーをresetPointへ戻す
// ソフトループ：即テレポートではなくフェードアウト→戻す演出
public class LoopController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform     resetPoint;
    [SerializeField] private Transform     playerTransform;
    [SerializeField] private CanvasGroup   fadeCanvasGroup; // 暗転用UI

    [Header("Loop Settings")]
    [SerializeField] private float fadeDuration  = 0.6f;
    [SerializeField] private float holdDuration  = 0.3f;

    private bool isLooping = false;

    private void Awake()
    {
        if (resetPoint == null)
            Debug.LogError("[LoopController] resetPoint が未設定です");
        if (playerTransform == null)
            Debug.LogError("[LoopController] playerTransform が未設定です");
    }

    // ──────────────────────────────────────
    // Trigger通過による自動ループ
    // ──────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isLooping)
        {
            StartCoroutine(SoftLoopRoutine());
        }
    }

    // ──────────────────────────────────────
    // 不正解時にGameManagerから呼ばれる
    // ──────────────────────────────────────
    public void TriggerSoftLoop()
    {
        if (!isLooping)
            StartCoroutine(SoftLoopRoutine());
    }

    // ──────────────────────────────────────
    // ループ演出コルーチン
    // ──────────────────────────────────────
    private IEnumerator SoftLoopRoutine()
    {
        isLooping = true;

        // フェードアウト
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
        yield return new WaitForSeconds(holdDuration);

        // 位置リセット
        if (playerTransform != null && resetPoint != null)
        {
            playerTransform.position = resetPoint.position;
            playerTransform.rotation = resetPoint.rotation;
        }

        GameManager.Instance?.ResetLoop();

        // フェードイン
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        isLooping = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        float elapsed = 0f;
        fadeCanvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
    }
}
