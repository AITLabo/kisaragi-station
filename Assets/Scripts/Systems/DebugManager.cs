using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

// きさらぎ駅 – ループ高速テスト・現在状態表示・イベント強制発火（QA 短縮）
public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance { get; private set; }

    [Header("Fast Loop Test")]
    [Tooltip("ON 時: L キーで NextLoop、TimeScale=3")]
    [SerializeField] private bool fastLoopTest;

    [Header("Debug UI (Optional)")]
    [SerializeField] private bool showDebugUI = true;
    [SerializeField] private TMP_Text loopText;
    [SerializeField] private TMP_Text eventsText;
    [SerializeField] private TMP_InputField eventIdInput;
    [SerializeField] private Button fireEventButton;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        if (fastLoopTest)
        {
            Time.timeScale = 3f;
            if (Input.GetKeyDown(KeyCode.L) && GameManager.Instance != null)
                GameManager.Instance.NextLoop();
        }
        else
            Time.timeScale = 1f;

        if (showDebugUI)
            RefreshDebugUI();
    }

    private void RefreshDebugUI()
    {
        if (loopText != null && GameManager.Instance != null)
            loopText.text = $"Loop: {GameManager.Instance.CurrentLoop}";

        if (eventsText != null && EventManager.Instance != null)
        {
            var list = EventManager.Instance.GetTriggeredEvents();
            eventsText.text = list != null ? string.Join(", ", list) : "-";
        }
    }

    private void Start()
    {
        if (fireEventButton != null && eventIdInput != null)
            fireEventButton.onClick.AddListener(OnFireEventClick);
    }

    private void OnFireEventClick()
    {
        if (EventManager.Instance == null || string.IsNullOrEmpty(eventIdInput.text)) return;
        EventManager.Instance.TryTriggerEvent(eventIdInput.text.Trim());
    }
}
