using UnityEngine;

// 電車の揺れ（カメラに適用）
// 横揺れ・縦揺れ・微細振動を組み合わせてリアルな走行感を演出
public class TrainSway : MonoBehaviour
{
    [Header("横揺れ（カーブ）")]
    [SerializeField] private float swayAmplitude  = 0.018f;
    [SerializeField] private float swayFrequency  = 0.55f;

    [Header("縦揺れ（線路の継ぎ目）")]
    [SerializeField] private float bumpAmplitude  = 0.010f;
    [SerializeField] private float bumpFrequency  = 2.80f;

    [Header("微細振動（エンジン）")]
    [SerializeField] private float vibrateAmount  = 0.003f;
    [SerializeField] private float vibrateSpeed   = 12f;

    [Header("傾き（カーブ）")]
    [SerializeField] private float tiltAmount     = 0.6f;   // 度
    [SerializeField] private float tiltFrequency  = 0.28f;

    private Vector3    originPos;
    private Quaternion originRot;
    private float      time;

    private void Start()
    {
        originPos = transform.localPosition;
        originRot = transform.localRotation;
        // 少し位相をずらす（乗客ごとに異なるタイミングに見せるため）
        time = Random.Range(0f, 100f);
    }

    private void Update()
    {
        time += Time.deltaTime;

        // ── 位置の揺れ ──
        float px = Mathf.Sin(time * swayFrequency)    * swayAmplitude
                 + Mathf.Sin(time * vibrateSpeed * 1.3f) * vibrateAmount;
        float py = Mathf.Sin(time * bumpFrequency)    * bumpAmplitude
                 + Mathf.Sin(time * bumpFrequency * 2.1f) * bumpAmplitude * 0.3f;

        transform.localPosition = originPos + new Vector3(px, py, 0f);

        // ── 傾き（Z軸回転：カーブの体感） ──
        float tilt = Mathf.Sin(time * tiltFrequency) * tiltAmount;
        transform.localRotation = originRot * Quaternion.Euler(0f, 0f, tilt);
    }

    // 歪みレベルに応じて揺れを強くする（AnomalyControllerから呼べる）
    public void SetDistortionMultiplier(float multiplier)
    {
        swayAmplitude  = 0.018f * multiplier;
        bumpAmplitude  = 0.010f * multiplier;
        vibrateAmount  = 0.003f * multiplier;
        tiltAmount     = 0.6f   * multiplier;
    }
}
