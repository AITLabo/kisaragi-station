using UnityEngine;
using System.Collections;

// きさらぎ駅 – 蛍光灯ランダム点滅。ループ3以降で発生率増加（スクショ映え・不穏さ）
[RequireComponent(typeof(Light))]
public class FlickerLight : MonoBehaviour
{
    public Light target;
    [Tooltip("点滅間隔の最小秒数")]
    public float minInterval = 3f;
    [Tooltip("点滅間隔の最大秒数")]
    public float maxInterval = 8f;
    [Tooltip("初回点滅までの遅延秒数")]
    public float firstDelay = 5f;
    [Tooltip("オフ時間の最小〜最大秒数")]
    public Vector2 offDuration = new Vector2(0.05f, 0.2f);

    [Header("ループ3以降")]
    [Tooltip("ループ3以降で間隔を短くする倍率（1=変化なし）")]
    [Range(0.2f, 1f)] public float intervalMultiplierFromLoop3 = 0.6f;

    private void Start()
    {
        if (target == null) target = GetComponent<Light>();
        if (target == null) return;
        float interval = Random.Range(minInterval, maxInterval);
        if (GameManager.Instance != null && GameManager.Instance.CurrentLoop >= 3)
            interval *= intervalMultiplierFromLoop3;
        InvokeRepeating(nameof(Flicker), firstDelay, interval);
    }

    private void Flicker()
    {
        StartCoroutine(FlickRoutine());
    }

    private IEnumerator FlickRoutine()
    {
        if (target == null) yield break;
        target.enabled = false;
        yield return new WaitForSeconds(Random.Range(offDuration.x, offDuration.y));
        target.enabled = true;
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Flicker));
    }
}
