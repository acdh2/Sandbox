#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// HierarchyDebugger draws a label above each GameObject in the hierarchy,
/// showing its depth level relative to the root. Useful for debugging nested object structures in the scene.
/// Only runs in the Editor due to [ExecuteAlways] and UNITY_EDITOR directive.
/// </summary>
[ExecuteAlways]
public class HierarchyDebugger : MonoBehaviour
{
    // Called automatically by Unity's editor to draw gizmos in the Scene view
    private void OnDrawGizmos()
    {
        // Start drawing labels from the root of this hierarchy
        DrawDepthLabels(transform.root, 0);
    }

    // Recursively draw labels above each GameObject indicating its depth
    private void DrawDepthLabels(Transform current, int depth)
    {
        // Calculate the position just above the object to place the label
        Vector3 labelPosition = current.position + Vector3.up * 0.5f;

        // Define the style for the label text (yellow color)
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.yellow;

        // Draw the label with the current depth level
        Handles.Label(labelPosition, $"Level {depth}", style);

        // Recursively draw labels for all children, increasing depth
        foreach (Transform child in current)
        {
            DrawDepthLabels(child, depth + 1);
        }
    }
}
#endif
