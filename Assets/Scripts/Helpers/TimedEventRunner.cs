using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TimedEventRunner : MonoBehaviour
{
    struct TimeFrame
    {
        public float start;
        public float end;

        public TimeFrame(float start, float end)
        {
            if (end > start)
            {
                this.start = start;
                this.end = end;
            }
            else
            {
                this.start = end;
                this.end = start;
            }
        }

        public void AddTime(float time)
        {
            if (time < start) start = time;
            if (time > end) end = time;
        }

        public bool Contains(float t)
        {
            return t >= start && t <= end;
        }
    }

    public bool autoPlay = true;

    [Serializable]
    public class TimedEvent
    {
        public float delay;
        public UnityEvent unityEvent;
    }

    public enum PlaybackMode
    {
        Once,
        Loop,
        PingPong
    }

    [SerializeField] public List<TimedEvent> events = new List<TimedEvent>();
    [SerializeField] public PlaybackMode mode = PlaybackMode.Once;
    [SerializeField] public float playbackSpeed = 1.0f;

    private bool isPlaying = false;
    private float currentTime = 0f;
    private float previousTime = 0f;
    private float duration = 0f;

    private List<(float time, UnityEvent unityEvent)> processedEvents = new();

    void OnValidate()
    {
        ProcessEvents();
    }

    void Awake()
    {
        ProcessEvents();
    }

    void Start()
    {
        if (autoPlay)
        {
            Play();
        }
    }

    void Update()
    {
        if (!isPlaying || processedEvents.Count == 0) return;

        HandlePlayback();
    }

    void HandlePlayback()
    {
        previousTime = currentTime;
        currentTime += Time.deltaTime * playbackSpeed;
        TimeFrame timeFrameCovered = new TimeFrame(previousTime, currentTime);

        bool firedInsideSwitch = false;

        switch (mode)
        {
            case PlaybackMode.Once:
                if ((playbackSpeed >= 0 && currentTime > duration) || (playbackSpeed < 0 && currentTime < 0))
                {
                    // Clamp currentTime naar boundary
                    currentTime = Mathf.Clamp(currentTime, 0f, duration);
                    // TimeFrame uitbreiden tot einde
                    timeFrameCovered = new TimeFrame(timeFrameCovered.start, currentTime);
                    FireEventsInWindow(timeFrameCovered);
                    Stop();
                    firedInsideSwitch = true;
                    return; // voorkom dubbele fire buiten switch
                }
                break;

            case PlaybackMode.Loop:
                if (playbackSpeed >= 0 && currentTime > duration)
                {
                    float overshoot = currentTime - duration;

                    // Eerst events van previousTime tot einde (duration)
                    var firstFrame = new TimeFrame(timeFrameCovered.start, duration);
                    FireEventsInWindow(firstFrame);

                    // spring terug naar begin + overshoot
                    currentTime = overshoot;

                    // Dan events van 0 tot overshoot
                    var secondFrame = new TimeFrame(0f, currentTime);
                    FireEventsInWindow(secondFrame);

                    timeFrameCovered = new TimeFrame(0f, currentTime);
                    firedInsideSwitch = true;
                }
                else if (playbackSpeed < 0 && currentTime < 0)
                {
                    float overshoot = -currentTime;

                    // Eerst events van previousTime tot 0
                    var firstFrame = new TimeFrame(timeFrameCovered.start, 0f);
                    FireEventsInWindow(firstFrame);

                    // spring naar einde - overshoot
                    currentTime = duration - overshoot;

                    // Dan events van currentTime tot einde
                    var secondFrame = new TimeFrame(currentTime, duration);
                    FireEventsInWindow(secondFrame);

                    timeFrameCovered = new TimeFrame(currentTime, duration);
                    firedInsideSwitch = true;
                }
                break;

            case PlaybackMode.PingPong:
                if (currentTime > duration)
                {
                    float overshoot = currentTime - duration;
                    currentTime = duration - overshoot; // reflecteer terug
                    playbackSpeed *= -1;

                    // Fire events van previousTime tot einde
                    timeFrameCovered = new TimeFrame(timeFrameCovered.start, duration);
                    FireEventsInWindow(timeFrameCovered);

                    // Fire events van einde terug naar currentTime
                    timeFrameCovered = new TimeFrame(duration, currentTime);
                    FireEventsInWindow(timeFrameCovered);

                    firedInsideSwitch = true;
                }
                else if (currentTime < 0)
                {
                    float overshoot = -currentTime;
                    currentTime = overshoot; // reflecteer terug
                    playbackSpeed *= -1;

                    // Fire events van previousTime tot 0
                    timeFrameCovered = new TimeFrame(timeFrameCovered.start, 0f);
                    FireEventsInWindow(timeFrameCovered);

                    // Fire events van 0 tot currentTime
                    timeFrameCovered = new TimeFrame(0f, currentTime);
                    FireEventsInWindow(timeFrameCovered);

                    firedInsideSwitch = true;
                }
                break;
        }

        if (!firedInsideSwitch)
        {
            FireEventsInWindow(timeFrameCovered);
        }
    }

    void FireEventsInWindow(TimeFrame time)
    {
        foreach (var (eventTime, unityEvent) in processedEvents)
        {
            if (time.Contains(eventTime))
                unityEvent?.Invoke();
        }
    }

    public void JumpTo(float time)
    {
        currentTime = Mathf.Clamp(time, 0f, duration);
        previousTime = currentTime;
        FireEventsInWindow(new TimeFrame(currentTime, currentTime)); // Alleen huidige tijd
    }

    public void Play() => isPlaying = true;
    public void Pause() => isPlaying = false;

    public void Stop()
    {
        isPlaying = false;
        JumpTo(0f);
    }

    private void ProcessEvents()
    {
        processedEvents.Clear();
        duration = 0f; // reset duration!
        foreach (var e in events)
        {
            if (e != null && e.unityEvent != null)
                processedEvents.Add((e.delay, e.unityEvent));
            if (e.delay > duration)
                duration = e.delay;
        }
        processedEvents.Sort((a, b) => a.time.CompareTo(b.time));
    }
}
