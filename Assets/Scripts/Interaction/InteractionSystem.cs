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
    [SerializeField] private UnityEngine.UI.Image reticleImage; // 画面中央の照準

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
        // 画面中央から Raycast
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // interactableLayer が 0（未設定）の場合は全レイヤーを対象にするフォールバック
        // ※ Edit > Project Settings > Tags and Layers で "Interactable" を追加すると正常動作
        LayerMask effectiveMask = (interactableLayer.value == 0) ? ~0 : interactableLayer;

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, effectiveMask))
        {
            IInteractable found = hit.collider.GetComponent<IInteractable>();
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
        if (reticleImage == null) return;
        reticleImage.color = highlight
            ? new Color(1f, 0.8f, 0.2f, 0.9f)  // 黄色：対象あり
            : new Color(1f, 1f, 1f, 0.5f);       // 白：通常
    }
}
