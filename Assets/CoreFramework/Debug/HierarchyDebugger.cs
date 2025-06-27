// #if UNITY_EDITOR
// using UnityEngine;
// using UnityEditor;

// [ExecuteAlways]
// public class HierarchyDebugger : MonoBehaviour
// {
//     private void OnDrawGizmos()
//     {
//         DrawDepthLabels(transform, 0);
//     }

//     private void DrawDepthLabels(Transform current, int depth)
//     {
//         // Tekst tekenen boven het object
//         Vector3 labelPosition = current.position + Vector3.up * 0.5f;
//         GUIStyle style = new GUIStyle();
//         style.normal.textColor = Color.yellow;
//         Handles.Label(labelPosition, $"Level {depth}", style);

//         // Herhaal voor alle children
//         foreach (Transform child in current)
//         {
//             DrawDepthLabels(child, depth + 1);
//         }
//     }
// }
// #endif
