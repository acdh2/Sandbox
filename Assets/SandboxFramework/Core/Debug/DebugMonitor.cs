using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

[DisallowMultipleComponent]
public class DebugMonitor : MonoBehaviour
{
    [Header("Debug Window Settings")]
    public bool enableOnStart = true;
    [Range(0.1f, 1f)] public float widthPercent = 0.4f;
    [Range(0.1f, 1f)] public float heightPercent = 0.3f;
    public Anchor anchorPosition = Anchor.BottomLeft;

    [Header("Log Settings")]
    public bool showTimestamp = true;

    [Header("State Machines to Watch")]
    public StateMachine stateMachine1;
    public StateMachine stateMachine2;
    public StateMachine stateMachine3;

    private TextMeshProUGUI outputText;
    private RectTransform panel;
    private readonly Queue<string> logLines = new Queue<string>();

    public enum Anchor { TopLeft, TopRight, BottomLeft, BottomRight }

    void Start()
    {
        if (enableOnStart)
        {
            Initialize();
        }
    }

    void Initialize()
    {
        CreateUI();
        LogInfo("Press SHIFT+C to clear the console.");        
    }

    void Update()
    {
        if (outputText == null) return;

        if (Keyboard.current.cKey.wasPressedThisFrame && Keyboard.current.leftShiftKey.isPressed)
        {
            Clear();
        }

        string stateInfo = "";
        if (stateMachine1 != null) stateInfo += $"[1] <b>{stateMachine1.name}</b>: {stateMachine1.CurrentState}\n";
        if (stateMachine2 != null) stateInfo += $"[2] <b>{stateMachine2.name}</b>: {stateMachine2.CurrentState}\n";
        if (stateMachine3 != null) stateInfo += $"[3] <b>{stateMachine3.name}</b>: {stateMachine3.CurrentState}\n";

        while (logLines.Count < maxLines)
        {
            logLines.Enqueue("");
        }
        outputText.text = string.Join("\n", logLines) + $"\n<size=80%><i>States</i>\n{stateInfo}</size>";
    }

    public void Log(string message) => AddLine(Wrap("INFO", message, "white"));
    public void Log(float number) => Log(number.ToString("F2"));
    public void Log(int number) => Log(number.ToString());
    public void Log(bool value) => Log(value.ToString());
    public void LogInfo(string message) => AddLine(Wrap("INFO", message, "white"));
    public void LogWarning(string message) => AddLine(Wrap("WARN", message, "yellow"));
    public void LogError(string message) => AddLine(Wrap("ERROR", message, "red"));
    public void Clear() => logLines.Clear();

    private int maxLines = 0;

    private void AddLine(string line)
    {
        if (outputText == null) Initialize();
        if (logLines.Count >= maxLines)
            logLines.Dequeue();
        logLines.Enqueue(line);
    }

    private string Wrap(string tag, string message, string color)
    {
        string time = showTimestamp ? $"[{Time.time:0.00}] " : "";
        return $"<color={color}>{time}[{tag}] {message}</color>";
    }

    private void CreateUI()
    {
        // Ensure Canvas
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("DebugCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Panel
        GameObject panelGO = new GameObject("DebugPanel", typeof(Image));
        panel = panelGO.GetComponent<RectTransform>();
        panel.SetParent(canvas.transform, false);
        panel.anchorMin = GetAnchorMin();
        panel.anchorMax = GetAnchorMax();
        panel.pivot = panel.anchorMin;
        panel.sizeDelta = new Vector2(Screen.width * widthPercent, Screen.height * heightPercent);
        panel.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        // Text
        GameObject textGO = new GameObject("DebugText", typeof(TextMeshProUGUI));
        outputText = textGO.GetComponent<TextMeshProUGUI>();
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textGO.transform.SetParent(panel, false);
        textRT.anchorMin = new Vector2(0, 0);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.offsetMin = new Vector2(10, 10);
        textRT.offsetMax = new Vector2(-10, -10);
        outputText.textWrappingMode = TextWrappingModes.Normal;
        outputText.richText = true;
        outputText.fontSize = 12;
        outputText.text = "Debug Output...";

        // Clear Button
        // GameObject buttonGO = new GameObject("ClearButton", typeof(Button), typeof(Image));
        // buttonGO.transform.SetParent(panel, false);
        // RectTransform btnRT = buttonGO.GetComponent<RectTransform>();
        // btnRT.anchorMin = new Vector2(1, 1);
        // btnRT.anchorMax = new Vector2(1, 1);
        // btnRT.pivot = new Vector2(1, 1);
        // btnRT.anchoredPosition = new Vector2(-10, -10);
        // btnRT.sizeDelta = new Vector2(60, 25);
        // buttonGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.2f);

        // TextMeshProUGUI btnText = new GameObject("ButtonText", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        // btnText.text = "Clear";
        // btnText.alignment = TextAlignmentOptions.Center;
        // btnText.fontSize = 14;
        // btnText.color = Color.black;
        // RectTransform txtRT = btnText.GetComponent<RectTransform>();
        // btnText.transform.SetParent(buttonGO.transform, false);
        // txtRT.anchorMin = Vector2.zero;
        // txtRT.anchorMax = Vector2.one;
        // txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;

        // Button btn = buttonGO.GetComponent<Button>();
        // btn.onClick.AddListener(Clear);

        panel.GetComponent<Image>().raycastTarget = false;
        outputText.raycastTarget = false;        

        float lineHeight = outputText.fontSize * 1.2f;
        float availableHeight = panel.sizeDelta.y - 0f;
        maxLines = Mathf.FloorToInt(availableHeight / lineHeight) - 2;
    }

    private Vector2 GetAnchorMin()
    {
        return anchorPosition switch
        {
            Anchor.TopLeft => new Vector2(0, 1),
            Anchor.TopRight => new Vector2(1, 1),
            Anchor.BottomRight => new Vector2(1, 0),
            _ => new Vector2(0, 0),
        };
    }

    private Vector2 GetAnchorMax() => GetAnchorMin();
}
