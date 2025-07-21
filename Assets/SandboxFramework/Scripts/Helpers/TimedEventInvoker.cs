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
            // Ensure that start is always less or equal than end
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
        /// Checks if a given time 't' lies within this time frame (inclusive).
        /// </summary>
        public bool Contains(float t)
        {
            return t >= start && t <= end;
        }
    }

    [Serializable]
    public class TimedEvent
    {
        public float delay;           // Time in seconds when this event should fire
        public UnityEvent unityEvent; // The UnityEvent to invoke at the specified delay
    }

    public enum PlaybackMode
    {
        Once,       // Play events once from start to end
        Loop,       // Loop playback continuously from end back to start (or reverse)
        PingPong    // Play forward then backward repeatedly (bouncing)
    }

    [SerializeField] private List<TimedEvent> events = new List<TimedEvent>();  // List of timed events
    [SerializeField] private PlaybackMode mode = PlaybackMode.Once;             // Playback mode
    [SerializeField] private float playbackSpeed = 1.0f;                        // Playback speed multiplier
    [SerializeField] private bool autoPlay = true;                             // Should playback start automatically

    private bool isPlaying = false;     // Is playback currently active
    private float currentTime = 0f;     // Current playback time
    private float previousTime = 0f;    // Playback time at previous frame
    private float duration = 0f;        // Total duration of all events (max delay)

    // Cached and sorted list of event times with their corresponding UnityEvents
    private List<(float time, UnityEvent unityEvent)> processedEvents = new();

    private void OnValidate()
    {
        // When values change in inspector, update the processed events and duration
        ProcessEvents();
    }

    private void Awake()
    {
        // Initialize event processing before use
        ProcessEvents();
    }

    private void Start()
    {
        // Start playback automatically if enabled
        if (autoPlay)
            Play();
    }

    private void Update()
    {
        // Skip update if not playing or no events to process
        if (!isPlaying || processedEvents.Count == 0)
            return;

        HandlePlayback();
    }

    /// <summary>
    /// Core playback loop: advance time, handle playback modes, and trigger events accordingly.
    /// </summary>
    private void HandlePlayback()
    {
        if (!enabled) return;

        previousTime = currentTime;
        currentTime += Time.deltaTime * playbackSpeed;  // Advance time with speed factor
        TimeFrame timeWindow = new TimeFrame(previousTime, currentTime);
        bool handledInSwitch = false;

        switch (mode)
        {
            case PlaybackMode.Once:
                // If playback exceeds bounds, clamp time, fire events in remaining window and stop
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
                    // Fire events from previous time to end
                    FireEventsInWindow(new TimeFrame(timeWindow.start, duration));
                    // Loop back to start plus overshoot
                    currentTime = overshoot;
                    // Fire events from start to overshoot
                    FireEventsInWindow(new TimeFrame(0f, currentTime));
                    handledInSwitch = true;
                }
                else if (playbackSpeed < 0 && currentTime < 0)
                {
                    float overshoot = -currentTime;
                    // Fire events from previous time down to start
                    FireEventsInWindow(new TimeFrame(timeWindow.start, 0f));
                    // Loop back to end minus overshoot
                    currentTime = duration - overshoot;
                    // Fire events from current time up to end
                    FireEventsInWindow(new TimeFrame(currentTime, duration));
                    handledInSwitch = true;
                }
                break;

            case PlaybackMode.PingPong:
                if (currentTime > duration)
                {
                    float overshoot = currentTime - duration;
                    currentTime = duration - overshoot; // Reflect backwards inside range
                    playbackSpeed *= -1;                 // Reverse playback direction
                    // Fire forward events to end
                    FireEventsInWindow(new TimeFrame(timeWindow.start, duration));
                    // Fire backward events from end to reflected time
                    FireEventsInWindow(new TimeFrame(duration, currentTime));
                    handledInSwitch = true;
                }
                else if (currentTime < 0)
                {
                    float overshoot = -currentTime;
                    currentTime = overshoot;  // Reflect forwards inside range
                    playbackSpeed *= -1;      // Reverse playback direction
                    // Fire backward events to start
                    FireEventsInWindow(new TimeFrame(timeWindow.start, 0f));
                    // Fire forward events from start to reflected time
                    FireEventsInWindow(new TimeFrame(0f, currentTime));
                    handledInSwitch = true;
                }
                break;
        }

        // If no special handling in switch, fire events in the current frame's time window
        if (!handledInSwitch)
        {
            FireEventsInWindow(timeWindow);
        }
    }

    /// <summary>
    /// Invokes all events whose scheduled times fall within the given time frame.
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
    /// Jumps playback to a specific time and immediately fires events scheduled exactly at that time.
    /// </summary>
    /// <param name="time">Time in seconds to jump to (clamped to valid range)</param>
    public void JumpTo(float time)
    {
        currentTime = Mathf.Clamp(time, 0f, duration);
        previousTime = currentTime;
        // Fire events scheduled exactly at currentTime
        FireEventsInWindow(new TimeFrame(currentTime, currentTime));
    }

    public void Play() => isPlaying = true;
    public void Pause() => isPlaying = false;

    /// <summary>
    /// Stops playback and resets current time to zero.
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        JumpTo(0f);
    }

    /// <summary>
    /// Processes and caches all events, sorting them by delay time and determining total playback duration.
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

        // Sort events by their scheduled time ascending
        processedEvents.Sort((a, b) => a.time.CompareTo(b.time));
    }
}
