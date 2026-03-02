using UnityEngine;
using System.Collections;
using TMPro;

// きさらぎ駅ループ – 心理演出統合コントローラ
// GameManager の DistortionLevel に応じて自動的に演出を起動する
public class PsychologicalEffects : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform      playerTransform;
    [SerializeField] private Transform      playerShadow;       // 影オブジェクト（Blobシャドウ等）
    [SerializeField] private TextMeshPro    stationNameText3D;  // 3D駅名看板テキスト
    [SerializeField] private TextMeshPro    clockText3D;        // 3D時計テキスト
    [SerializeField] private AudioSource    footstepSource;
    [SerializeField] private AudioSource    whisperSource;      // ささやき声

    [Header("Station Name Settings")]
    [SerializeField] private string normalName = "き さ ら ぎ 駅";
    [SerializeField] private string[] corruptedNames = {
        "き さ ぎ ら 駅",
        "き ◻ ◻ ぎ 駅",
        "─ ─ ─ ─ ─",
        "? ? ? ? ?",
        "あ な た の 駅"
    };

    [Header("Clock Settings")]
    [SerializeField] private string normalTime = "23:58";

    [Header("Footstep Delay")]
    [SerializeField] private float normalFootstepInterval  = 1.0f;
    [SerializeField] private float delayedFootstepInterval = 1.8f;

    [Header("Shadow Delay")]
    [SerializeField] private float shadowDelaySeconds = 0.5f;

    private DistortionLevel lastLevel = DistortionLevel.None;
    private Vector3[]       positionBuffer;
    private int             bufferIndex   = 0;
    private int             bufferSize    = 30; // 0.5秒分（60fps想定）
    private bool            shadowDelayOn = false;
    private Coroutine       clockCoroutine;
    private Coroutine       nameCoroutine;

    private void Start()
    {
        positionBuffer = new Vector3[bufferSize];
        for (int i = 0; i < bufferSize; i++)
            positionBuffer[i] = playerTransform != null ? playerTransform.position : Vector3.zero;

        if (stationNameText3D != null) stationNameText3D.text = normalName;
        if (clockText3D != null)       clockText3D.text       = normalTime;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        DistortionLevel current = GameManager.Instance.currentDistortion;

        // レベルが変化したら演出を切り替える
        if (current != lastLevel)
        {
            OnDistortionLevelChanged(current);
            lastLevel = current;
        }

        // 影遅延処理（毎フレーム）
        if (shadowDelayOn && playerShadow != null && playerTransform != null)
        {
            positionBuffer[bufferIndex] = playerTransform.position;
            bufferIndex = (bufferIndex + 1) % bufferSize;
            int delayedIndex = (bufferIndex + 1) % bufferSize;
            Vector3 delayed = positionBuffer[delayedIndex];
            playerShadow.position = new Vector3(delayed.x, playerShadow.position.y, delayed.z);
        }
    }

    // ──────────────────────────────────────
    // レベル変化時の処理
    // ──────────────────────────────────────
    private void OnDistortionLevelChanged(DistortionLevel level)
    {
        StopAllCoroutines();

        switch (level)
        {
            case DistortionLevel.None:
                RestoreAll();
                break;

            case DistortionLevel.Low:
                // 足音が微妙に遅くなる
                SetFootstepDelay(false);
                StartCoroutine(OccasionalNameFlicker());
                break;

            case DistortionLevel.Medium:
                // 時計が逆行・駅名崩壊が頻発
                SetFootstepDelay(false);
                clockCoroutine = StartCoroutine(ClockRewindLoop());
                nameCoroutine  = StartCoroutine(FrequentNameCorrupt());
                break;

            case DistortionLevel.High:
                // 足音遅延・影遅延・ささやき声
                SetFootstepDelay(true);
                shadowDelayOn = true;
                if (whisperSource != null) whisperSource.Play();
                clockCoroutine = StartCoroutine(ClockRewindLoop());
                nameCoroutine  = StartCoroutine(FrequentNameCorrupt());
                break;

            case DistortionLevel.Collapse:
                // 駅名が自分への呼びかけに変わる・時計停止
                SetFootstepDelay(true);
                shadowDelayOn = true;
                if (stationNameText3D != null) stationNameText3D.text = "あ な た は こ こ に い る";
                if (clockText3D != null)       clockText3D.text       = "──:──";
                if (whisperSource != null && !whisperSource.isPlaying) whisperSource.Play();
                break;
        }
    }

    // ──────────────────────────────────────
    // 足音遅延
    // ──────────────────────────────────────
    private void SetFootstepDelay(bool delayed)
    {
        PlayerController pc = playerTransform?.GetComponent<PlayerController>();
        if (pc == null) return;
        pc.footstepDelayMultiplier = delayed ? delayedFootstepInterval / normalFootstepInterval : 1.0f;
    }

    // ──────────────────────────────────────
    // 時計逆行コルーチン
    // ──────────────────────────────────────
    private IEnumerator ClockRewindLoop()
    {
        int minutes = 58;
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(4f, 8f));
            minutes = Mathf.Max(0, minutes - 1);
            if (clockText3D != null)
                clockText3D.text = $"23:{minutes:D2}";
            // 1秒後に戻す（ちらつき演出）
            yield return new WaitForSeconds(1f);
            if (clockText3D != null)
                clockText3D.text = normalTime;
        }
    }

    // ──────────────────────────────────────
    // 駅名崩壊（たまに）
    // ──────────────────────────────────────
    private IEnumerator OccasionalNameFlicker()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(8f, 15f));
            if (stationNameText3D != null)
            {
                stationNameText3D.text = corruptedNames[Random.Range(0, corruptedNames.Length)];
                yield return new WaitForSeconds(0.2f);
                stationNameText3D.text = normalName;
            }
        }
    }

    // ──────────────────────────────────────
    // 駅名崩壊（頻繁に）
    // ──────────────────────────────────────
    private IEnumerator FrequentNameCorrupt()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(3f, 6f));
            if (stationNameText3D != null)
            {
                string corrupt = corruptedNames[Random.Range(0, corruptedNames.Length)];
                stationNameText3D.text = corrupt;
                yield return new WaitForSeconds(Random.Range(0.3f, 0.8f));
                stationNameText3D.text = normalName;
            }
        }
    }

    // ──────────────────────────────────────
    // 全演出リセット
    // ──────────────────────────────────────
    private void RestoreAll()
    {
        shadowDelayOn = false;
        SetFootstepDelay(false);
        if (stationNameText3D != null) stationNameText3D.text = normalName;
        if (clockText3D != null)       clockText3D.text       = normalTime;
        if (whisperSource != null)     whisperSource.Stop();
    }
}
