using UnityEngine;
using TMPro;

// きさらぎ駅ループ – インタラクション処理
// 画面中央にレティクルを常時表示し、Raycast対象にカーソルが当たったら
// 左クリック または Enter キーで interact する
public class InteractionSystem : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float interactDistance = 8f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private TextMeshProUGUI interactPromptText;

    [Header("Reticle")]
    [SerializeField] private ReticleHUD reticleHUD; // 画面中央の「+」（OnGUI, Canvas不要）

    private IInteractable currentTarget;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                Debug.LogError("[InteractionSystem] playerCamera が見つかりません");
        }

        // シリアライズ参照がなければ自動取得・追加（フォールバック）
        if (reticleHUD == null)
        {
            reticleHUD = Object.FindObjectOfType<ReticleHUD>();
        }
        if (reticleHUD == null)
        {
            // どこにも無ければ自分自身に追加（確実に動く）
            reticleHUD = gameObject.AddComponent<ReticleHUD>();
            Debug.Log("[InteractionSystem] ReticleHUD を自動追加しました");
        }
        if (interactPromptText == null)
        {
            var pGO = GameObject.Find("InteractPrompt");
            if (pGO != null) interactPromptText = pGO.GetComponent<TMPro.TextMeshProUGUI>();
        }

        if (interactPromptText != null)
            interactPromptText.gameObject.SetActive(false);

        // 起動時に参照状態をすべてログ出力
        Debug.Log($"[InteractionSystem] Awake:" +
                  $"\n  playerCamera    = {(playerCamera    != null ? playerCamera.name    : "NULL !!!")}" +
                  $"\n  reticleHUD      = {(reticleHUD      != null ? reticleHUD.name      : "NULL !!!")}" +
                  $"\n  interactPrompt  = {(interactPromptText != null ? interactPromptText.name : "NULL !!!")}" +
                  $"\n  interactDist    = {interactDistance}" +
                  $"\n  interactLayer   = {interactableLayer.value}");

        if (reticleHUD == null)
            Debug.LogError("[InteractionSystem] reticleHUD が NULL！再ビルドしてください。");
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

    private void DetectTarget()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = (interactableLayer.value == 0)
            ? Physics.RaycastAll(ray, interactDistance)
            : Physics.RaycastAll(ray, interactDistance, interactableLayer);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            IInteractable found = hit.collider.GetComponentInParent<IInteractable>();
            if (found != null)
            {
                if (found != currentTarget)
                    Debug.Log($"[InteractionSystem] Target: {found.ActionID} ({hit.collider.gameObject.name} dist={hit.distance:F1}m)");
                currentTarget = found;
                ShowPrompt(true, hit.collider.gameObject.name);
                SetReticleHighlight(true);
                return;
            }
        }

        if (currentTarget != null)
            Debug.Log("[InteractionSystem] Target lost");
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
        if (reticleHUD == null) return;
        reticleHUD.color = highlight
            ? new Color(1f, 0.85f, 0.1f, 1f)   // 黄色
            : new Color(0.2f, 1f,   0.5f, 1f);  // 緑
    }
}
