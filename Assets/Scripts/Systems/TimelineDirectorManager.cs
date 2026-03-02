using UnityEngine;
using UnityEngine.Playables;

// きさらぎ駅 – ループ転換・最終到着・Dummy出現などを Timeline で制御（コードではなくクリップで演出）
public class TimelineDirectorManager : MonoBehaviour
{
    public static TimelineDirectorManager Instance { get; private set; }

    [Header("Timelines")]
    [Tooltip("ループ失敗時のフェード＋環境変化")]
    public PlayableDirector loopTransition;
    [Tooltip("最終電車到着演出")]
    public PlayableDirector finalArrival;
    [Tooltip("Dummy 出現（Active＋光 flicker＋カメラ微振動→Destroy）")]
    public PlayableDirector dummyAppear;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void PlayLoopTransition()
    {
        if (loopTransition != null) loopTransition.Play();
    }

    public void PlayFinalArrival()
    {
        if (finalArrival != null) finalArrival.Play();
    }

    public void PlayDummyAppear()
    {
        if (dummyAppear != null) dummyAppear.Play();
    }
}
