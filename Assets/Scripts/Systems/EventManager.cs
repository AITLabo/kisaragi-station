using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// きさらぎ駅 – 違和感イベントをデータ駆動で制御（ScriptableObject で演出追加可能）
public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    [Header("Data (推奨)")]
    [SerializeField] private EventData[] events;

    [Header("Legacy – Level 別（events 未設定時用）")]
    [SerializeField] private MonoBehaviour[] level1Behaviours;
    [SerializeField] private MonoBehaviour[] level2Behaviours;
    [SerializeField] private MonoBehaviour[] level3Behaviours;
    [SerializeField] private MonoBehaviour[] level4Behaviours;

    private const int MaxEventsPerLoop = 3;
    private readonly HashSet<string> _triggeredEvents = new HashSet<string>();
    private int _eventsFiredThisLoop;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>イベント発火を試行（GazeTrigger 等から呼ぶ）</summary>
    public void TryTriggerEvent(string eventID)
    {
        if (events == null) return;

        var data = System.Array.Find(events, e => e != null && e.eventID == eventID);
        if (data == null) return;
        if (_triggeredEvents.Contains(eventID)) return;

        int loop = GameManager.Instance != null ? GameManager.Instance.CurrentLoop : 1;
        if (loop < data.minLoop) return;
        if (data.maxLoop > 0 && loop > data.maxLoop) return;
        if (_eventsFiredThisLoop >= MaxEventsPerLoop) return;

        ExecuteEvent(data);
        if (data.disableAfterTrigger)
            _triggeredEvents.Add(eventID);
        _eventsFiredThisLoop++;
    }

    private void ExecuteEvent(EventData data)
    {
        if (data.targetObject != null)
            data.targetObject.SetActive(true);
        if (data.sound != null)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = data.sound;
            src.spatialBlend = 0f;
            src.Play();
            Destroy(src, data.sound.length + 0.1f);
        }
    }

    public void UpdateEvents(int loop)
    {
        _eventsFiredThisLoop = 0;
        if (level1Behaviours != null || level2Behaviours != null || level3Behaviours != null || level4Behaviours != null)
        {
            switch (loop)
            {
                case 1: TryActivate("L1_ClockDrift", level1Behaviours); TryActivate("L1_SignFont", level1Behaviours); TryActivate("L1_LightFlicker", level1Behaviours); break;
                case 2: TryActivate("L2_FootstepDelay", level2Behaviours); TryActivate("L2_PlatformLonger", level2Behaviours); TryActivate("L2_ShadowFigure", level2Behaviours); break;
                case 3: TryActivate("L3_DummyAppear", level3Behaviours); TryActivate("L3_ClockReverse", level3Behaviours); TryActivate("L3_OutOfRange", level3Behaviours); break;
                default: TryActivate("L4_BehindSound", level4Behaviours); TryActivate("L4_PlayerNameSign", level4Behaviours); TryActivate("L4_FogHeavy", level4Behaviours); break;
            }
        }
    }

    private void TryActivate(string eventId, MonoBehaviour[] behaviours)
    {
        if (_triggeredEvents.Contains(eventId) || _eventsFiredThisLoop >= MaxEventsPerLoop || behaviours == null) return;
        foreach (var b in behaviours)
        {
            if (b != null && b is ILoopEvent e)
            {
                e.OnLoopEvent(eventId);
                _triggeredEvents.Add(eventId);
                _eventsFiredThisLoop++;
                break;
            }
        }
    }

    public List<string> GetTriggeredEvents() => _triggeredEvents.ToList();
    public void SetTriggeredEvents(List<string> list)
    {
        _triggeredEvents.Clear();
        if (list != null) foreach (var id in list) _triggeredEvents.Add(id);
    }
    public void ResetFiredEvents() => _triggeredEvents.Clear();
}

public interface ILoopEvent
{
    void OnLoopEvent(string eventId);
}
