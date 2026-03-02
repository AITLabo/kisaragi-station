using UnityEngine;

// きさらぎ駅 – 電車を「異常存在」に昇格（P.T. 参考）。Loop4: 逆方向・無音・赤ヘッドライト
[RequireComponent(typeof(TrainPass))]
public class AbnormalTrain : MonoBehaviour
{
    [Header("Loop4 異常")]
    [Tooltip("ON で走行音を消す")]
    public bool silent;
    [Tooltip("赤ヘッドライトにする")]
    public Light headLight;
    [Tooltip("ON で進行方向を逆にする（Z +80 → -80）")]
    public bool reverseDirection;

    private AudioSource _audio;
    private TrainPass _trainPass;

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        _trainPass = GetComponent<TrainPass>();
    }

    private void Start()
    {
        if (silent && _audio != null)
            _audio.volume = 0f;

        if (headLight != null)
            headLight.color = Color.red;
    }

    /// <summary>異常モードを適用（ループに応じて呼び出す）</summary>
    public void SetAbnormal(bool silentMode, bool redLight, bool reverse)
    {
        silent = silentMode;
        reverseDirection = reverse;
        if (_audio != null) _audio.volume = silentMode ? 0f : 1f;
        if (headLight != null) headLight.color = redLight ? Color.red : Color.white;
        if (_trainPass != null) _trainPass.ReverseDirection(reverse);
    }

    /// <summary>逆方向出現用。Z = +80 から -80 へ</summary>
    public void SpawnAndPassReverse(Vector3 position)
    {
        if (_trainPass == null) return;
        _trainPass.SpawnAndPassReverse(position);
    }
}
