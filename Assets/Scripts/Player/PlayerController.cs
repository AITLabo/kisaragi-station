using UnityEngine;
using System.Collections;

// きさらぎ駅ループ – 一人称プレイヤー移動・視点制御（ホラー仕様：遅め・足音遅延）
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement (ホラー: 遅め)")]
    [SerializeField] private float moveSpeed   = 2f;
    [SerializeField] private float gravity     = -9.81f;

    [Header("Look (急に振り向けない)")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 70f;
    [SerializeField] private float maxLookAngle     = 80f;
    [SerializeField] private float lookSmoothing    = 10f;  // 補間係数（大きいほど即応・小さいほど滑らか）

    [Header("Footstep")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private float footstepInterval = 0.55f;
    [Tooltip("ループ3から0.2秒。LoopData.footstepDelay で上書き可")]
    [SerializeField] private float footstepDelay = 0f;

    private CharacterController characterController;
    private float   verticalVelocity  = 0f;
    private float   xRotation         = 0f;   // 現在の補間済み X 角度
    private float   xRotationTarget   = 0f;   // 入力ターゲット X 角度
    private float   yRotation         = 0f;   // 累積 Y 角度（ターゲット）
    private float   yRotationSmoothed = 0f;   // 補間済み Y 角度
    private float   footstepTimer     = 0f;
    private bool    isMoving          = false;
    private bool    _footstepScheduled;

    [HideInInspector] public float footstepDelayMultiplier = 1.0f;

    public void SetFootstepDelay(float delay) { footstepDelay = Mathf.Max(0f, delay); }
    public bool IsMoving => isMoving;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Inspector 未設定の場合は子オブジェクトの Camera を自動取得
        if (cameraTransform == null)
        {
            Camera childCam = GetComponentInChildren<Camera>();
            if (childCam != null)
                cameraTransform = childCam.transform;
            else
                Debug.LogError("[PlayerController] cameraTransform が未設定です（子に Camera も見つかりません）");
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        yRotation         = transform.eulerAngles.y;
        yRotationSmoothed = yRotation;
    }

    private void Update()
    {
        HandleLook();
        HandleMove();
        HandleFootstep();
    }

    // ──────────────────────────────────────
    // 視点制御
    // ──────────────────────────────────────
    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // ターゲット角度を更新
        xRotationTarget -= mouseY;
        xRotationTarget  = Mathf.Clamp(xRotationTarget, -maxLookAngle, maxLookAngle);
        yRotation       += mouseX;

        // Lerp でスムーズに追従
        float t          = lookSmoothing * Time.deltaTime;
        xRotation        = Mathf.Lerp(xRotation,        xRotationTarget, t);
        yRotationSmoothed = Mathf.Lerp(yRotationSmoothed, yRotation,      t);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.rotation = Quaternion.Euler(0f, yRotationSmoothed, 0f);
    }

    // ──────────────────────────────────────
    // 移動
    // ──────────────────────────────────────
    private void HandleMove()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = transform.right * h + transform.forward * v;
        isMoving = move.magnitude > 0.1f;

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        characterController.Move(move * moveSpeed * Time.deltaTime);
    }

    // ──────────────────────────────────────
    // 足音（遅延再生＝ループ3から0.2秒ずらす）
    // ──────────────────────────────────────
    private void HandleFootstep()
    {
        if (!isMoving || footstepSource == null) return;

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f && !_footstepScheduled)
        {
            if (footstepDelay > 0f)
            {
                _footstepScheduled = true;
                StartCoroutine(DelayedFootstep());
            }
            else
                footstepSource.Play();
            footstepTimer = footstepInterval * footstepDelayMultiplier;
        }
    }

    private IEnumerator DelayedFootstep()
    {
        yield return new WaitForSeconds(footstepDelay);
        if (footstepSource != null) footstepSource.Play();
        _footstepScheduled = false;
    }

    // ──────────────────────────────────────
    // カーソルロック解除（ESCでメニュー表示時）
    // ──────────────────────────────────────
    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
}
