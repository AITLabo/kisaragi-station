using UnityEngine;

// きさらぎ駅 – Watcher 個体差（Steam仕様）。起動時シードで高さ・首の角度・目の位置を変え、配信者ごとに違う見た目に
public class WatcherVariation : MonoBehaviour
{
    [Header("シード（WatcherAI から注入）")]
    [SerializeField] private int seed;

    [Header("変形対象（空ならこのオブジェクトの Transform のみ）")]
    [SerializeField] private Transform neckBone;
    [SerializeField] private Transform eyeLeft;
    [SerializeField] private Transform eyeRight;

    [Header("変動範囲")]
    [SerializeField] private Vector2 heightScaleRange = new Vector2(0.92f, 1.08f);
    [SerializeField] private float neckPitchRange = 8f;
    [SerializeField] private float neckYawRange = 12f;
    [SerializeField] private Vector2 eyeOffsetRange = new Vector2(0.02f, 0.08f);

    public void ApplySeed(int variationSeed)
    {
        seed = variationSeed;
        Random.InitState(seed);

        Vector3 scale = transform.localScale;
        scale.y *= Mathf.Lerp(heightScaleRange.x, heightScaleRange.y, (float)Random.Range(0, 10000) / 10000f);
        transform.localScale = scale;

        if (neckBone != null)
        {
            float pitch = Random.Range(-neckPitchRange, neckPitchRange);
            float yaw = Random.Range(-neckYawRange, neckYawRange);
            neckBone.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        float eyeOff = Random.Range(eyeOffsetRange.x, eyeOffsetRange.y);
        if (eyeLeft != null) eyeLeft.localPosition += Random.insideUnitSphere * eyeOff;
        if (eyeRight != null) eyeRight.localPosition += Random.insideUnitSphere * eyeOff;
    }
}
