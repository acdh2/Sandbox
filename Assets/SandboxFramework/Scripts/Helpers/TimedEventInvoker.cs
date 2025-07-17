using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TimedEventInvoker : MonoBehaviour
{
    /// <summary>
    /// Represents a time interval with start and end boundaries.
    /// Automatically sorts start and end so start <= end.
    /// </summary>
    struct TimeFrame
    {
        public float start;
        public float end;

        public TimeFrame(float a, float b)
        {
            // Ensure start <= end
            if (b > a)
            {
                start = a;
                end = b;
            }
            else
            {
                start = b;
                end = a;
            }
        }

        /// <summary>
        /// Checks if a given time 't' is inside the time frame (inclusive).
        /// </summary>
        public bool Contains(float t)
        {
            return t >= start && t <= end;
        }
    }

    [Serializable]
    public class TimedEvent
    {
        public float delay;       // Time in seconds when event fires
        public UnityEvent unityEvent;  // The event to invoke
    }

    public enum PlaybackMode
    {
        Once,       // Play from start to end once
        Loop,       // Loop from end back to start or vice versa
        PingPong    // Play forward and backward repeatedly
    }

    [SerializeField] private List<TimedEvent> events = new List<TimedEvent>();
    [SerializeField] private PlaybackMode mode = PlaybackMode.Once;
    [SerializeField] private float playbackSpeed = 1.0f;
    [SerializeField] private bool autoPlay = true;

    private bool isPlaying = false;
    private float currentTime = 0f;
    private float previousTime = 0f;
    private float duration = 0f;

    // Cached, sorted list of event times and their UnityEvents
    private List<(float time, UnityEvent unityEvent)> processedEvents = new();

    private void OnValidate()
    {
        // Recalculate processed events and duration when values are changed in inspector
        ProcessEvents();
    }

    private void Awake()
    {
        ProcessEvents();
    }

    private void Start()
    {
        if (autoPlay)
            Play();
    }

    private void Update()
    {
        if (!isPlaying || processedEvents.Count == 0)
            return;

        HandlePlayback();
    }

    /// <summary>
    /// Core playback handler advancing time, handling playback modes, and firing events accordingly.
    /// </summary>
    private void HandlePlayback()
    {
        if (!enabled) return;

        previousTime = currentTime;
        currentTime += Time.deltaTime * playbackSpeed;
        TimeFrame timeWindow = new TimeFrame(previousTime, currentTime);
        bool handledInSwitch = false;

        switch (mode)
        {
            case PlaybackMode.Once:
                // Clamp time and stop at boundaries when playback reaches the start or end
                if ((playbackSpeed >= 0 && currentTime > duration) || (playbackSpeed < 0 && currentTime < 0))
                {
                    currentTime = Mathf.Clamp(currentTime, 0f, duration);
                    timeWindow = new TimeFrame(timeWindow.start, currentTime);
                    FireEventsInWindow(timeWindow);
                    Stop();
                    handledInSwitch = true;
                }
                break;

            case PlaybackMode.Loop:
                if (playbackSpeed >= 0 && currentTime > duration)
                {
                    float overshoot = currentTime - duration;
                    // Fire events from previousTime to end
                    FireEventsInWindow(new TimeFrame(timeWindow.start, duration));
                    // Loop back to beginning plus overshoot
                    currentTime = overshoot;
                    // Fire events from start to overshoot
                    FireEventsInWindow(new TimeFrame(0f, currentTime));
                    handledInSwitch = true;
                }
                else if (playbackSpeed < 0 && currentTime < 0)
                {
                    float overshoot = -currentTime;
                    // Fire events from previousTime to start
                    FireEventsInWindow(new TimeFrame(timeWindow.start, 0f));
                    // Loop back to end minus overshoot
                    currentTime = duration - overshoot;
                    // Fire events from currentTime to end
                    FireEventsInWindow(new TimeFrame(currentTime, duration));
                    handledInSwitch = true;
                }
                break;

            case PlaybackMode.PingPong:
                if (currentTime > duration)
                {
                    float overshoot = currentTime - duration;
                    currentTime = duration - overshoot; // Reflect backward
                    playbackSpeed *= -1; // Reverse direction
                    // Fire events forward to end
                    FireEventsInWindow(new TimeFrame(timeWindow.start, duration));
                    // Fire events backward from end to reflected time
                    FireEventsInWindow(new TimeFrame(duration, currentTime));
                    handledInSwitch = true;
                }
                else if (currentTime < 0)
                {
                    float overshoot = -currentTime;
                    currentTime = overshoot; // Reflect forward
                    playbackSpeed *= -1; // Reverse direction
                    // Fire events backward to start
                    FireEventsInWindow(new TimeFrame(timeWindow.start, 0f));
                    // Fire events forward from start to reflected time
                    FireEventsInWindow(new TimeFrame(0f, currentTime));
                    handledInSwitch = true;
                }
                break;
        }

        // If no special case handled, just fire events in the current frame window
        if (!handledInSwitch)
        {
            FireEventsInWindow(timeWindow);
        }
    }

    /// <summary>
    /// Invokes all events whose times fall within the given time frame.
    /// </summary>
    private void FireEventsInWindow(TimeFrame time)
    {
        if (!enabled) return;
        
        foreach (var (eventTime, unityEvent) in processedEvents)
        {
            if (time.Contains(eventTime))
            {
                unityEvent?.Invoke();
            }
        }
    }

    /// <summary>
    /// Immediately jumps playback to a specific time and fires events scheduled exactly at that time.
    /// </summary>
    /// <param name="time">Time in seconds to jump to</param>
    public void JumpTo(float time)
    {
        currentTime = Mathf.Clamp(time, 0f, duration);
        previousTime = currentTime;
        // Fire events exactly at currentTime only
        FireEventsInWindow(new TimeFrame(currentTime, currentTime));
    }

    public void Play() => isPlaying = true;
    public void Pause() => isPlaying = false;

    /// <summary>
    /// Stops playback and resets time to zero.
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        JumpTo(0f);
    }

    /// <summary>
    /// Processes the list of events by caching them with their times and determining total duration.
    /// </summary>
    private void ProcessEvents()
    {
        processedEvents.Clear();
        duration = 0f;

        foreach (var e in events)
        {
            if (e == null || e.unityEvent == null)
                continue;

            processedEvents.Add((e.delay, e.unityEvent));

            if (e.delay > duration)
                duration = e.delay;
        }

        // Sort events by scheduled time ascending
        processedEvents.Sort((a, b) => a.time.CompareTo(b.time));
    }
}
