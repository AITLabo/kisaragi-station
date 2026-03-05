using UnityEngine;
using TMPro;

// きさらぎ駅ループ – インタラクション処理
// 画面中央にレティクルを常時表示し、Raycast対象にカーソルが当たったら
// 左クリック または Enter キーで interact する
public class InteractionSystem : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private TextMeshProUGUI interactPromptText;

    [Header("Reticle")]
    [SerializeField] private UnityEngine.UI.Image reticleImage; // 画面中央の照準（透明Image）
    [SerializeField] private TMPro.TextMeshProUGUI reticleText;  // 実際に描画するTMP「＋」

    private IInteractable currentTarget;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                Debug.LogError("[InteractionSystem] playerCamera が見つかりません");
        }

        if (interactPromptText != null)
            interactPromptText.gameObject.SetActive(false);

        // 起動時に参照状態をすべてログ出力
        Debug.Log($"[InteractionSystem] Awake:" +
                  $"\n  playerCamera    = {(playerCamera    != null ? playerCamera.name    : "NULL !!!")}" +
                  $"\n  reticleText     = {(reticleText     != null ? reticleText.name     : "NULL !!!")}" +
                  $"\n  reticleImage    = {(reticleImage    != null ? reticleImage.name    : "NULL")}" +
                  $"\n  interactPrompt  = {(interactPromptText != null ? interactPromptText.name : "NULL !!!")}" +
                  $"\n  interactDist    = {interactDistance}" +
                  $"\n  interactLayer   = {interactableLayer.value}");
    }

    private void Update()
    {
        DetectTarget();

        // 左クリック または Enter で interact
        bool triggered = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E);

        if (triggered && currentTarget != null)
        {
            GameManager.Instance?.ValidateAction(currentTarget.ActionID);
            currentTarget.OnInteract();
        }
    }

    private float _logTimer;

    private void DetectTarget()
    {
        if (playerCamera == null) return;

        // 画面中央から Raycast
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        LayerMask effectiveMask = (interactableLayer.value == 0) ? ~0 : interactableLayer;

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, effectiveMask))
        {
            // GetComponentInParent で FBX 子コライダーも含めて検索
            IInteractable found = hit.collider.GetComponentInParent<IInteractable>();

            // 1秒おきにレイの当たり先をログ
            _logTimer -= Time.deltaTime;
            if (_logTimer <= 0f)
            {
                _logTimer = 1f;
                Debug.Log($"[InteractionSystem] Ray hit: {hit.collider.gameObject.name}" +
                          $" dist={hit.distance:F1}m" +
                          $" IInteractable={(found != null ? found.ActionID : "なし")}");
            }

            if (found != null)
            {
                currentTarget = found;
                ShowPrompt(true, hit.collider.gameObject.name);
                SetReticleHighlight(true);
                return;
            }
        }

        currentTarget = null;
        ShowPrompt(false, "");
        SetReticleHighlight(false);
    }

    private void ShowPrompt(bool visible, string targetName)
    {
        if (interactPromptText == null) return;
        interactPromptText.gameObject.SetActive(visible);
        if (visible)
            interactPromptText.text = $"[ E ]  {targetName}を調べる";
    }

    // レティクルの色をハイライト切り替え
    private void SetReticleHighlight(bool highlight)
    {
        var yellow = new Color(1f, 0.8f, 0.2f, 0.95f);
        var white  = new Color(1f, 1f,   1f,   0.70f);
        if (reticleText  != null) reticleText.color  = highlight ? yellow : white;
        if (reticleImage != null) reticleImage.color = new Color(0f, 0f, 0f, 0f); // 常に透明
    }
}
