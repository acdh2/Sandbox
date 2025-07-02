//EDITED

using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public enum DragState
{
    Idle,
    Dragging
}

/// <summary>
/// Allows dragging and placing of a selected object on a grid using mouse input.
/// Requires a SelectionHandler component to function.
/// </summary>
[RequireComponent(typeof(SelectionHandler))]
public class DragHandler : MonoBehaviour
{
    [System.Serializable]
    public struct RotationAxisFlags
    {
        public bool X;
        public bool Y;
        public bool Z;
    }

    [Header("Grid Settings")]
    public Vector3 gridSize = Vector3.one;
    public Vector3 gridCenter = Vector3.zero;

    [Header("Placement Constraints")]
    public float minY = 0f;

    //[Header("Rotation Settings")]
    //public bool enableRotation = true;
    //public Vector3 rotationSnapDegrees = Vector3.zero;

    [Header("Rotation Settings")]
    public RotationAxisFlags allowRotation = new RotationAxisFlags { X = true, Y = true, Z = true };
    public Vector3 rotationSnapDegrees = Vector3.zero;

    [Header("Movement Settings")]
    public bool smoothMovement = false;
    public float moveResponsiveness = 100f;

    private Camera cam;
    private Transform selectedTransform;
    private Vector3 localOffset;
    private Quaternion rotationOffset;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private SelectionHandler selectionHandler;
    //private 
    //  originalRigidbody;
    //private bool originalKinematicState;

    private DragState currentState = DragState.Idle;
    public DragState CurrentState => currentState;

    void Start()
    {
        cam = Camera.main;
        selectionHandler = GetComponent<SelectionHandler>();
    }

    void Update()
    {
        HandleInput();
        HandleRotation();
    }

    // private void ApplyRotation(float x, float y, float z)
    // {
    //     if (selectedTransform != null)
    //     {
    //         Vector3 rotation = selectedTransform.rotation.eulerAngles;
    //         rotation += new Vector3(x, y, z);
    //         selectedTransform.rotation = Quaternion.Euler(GetSnappedRotation(rotation));
    //     }
    // }

    private void RotateSelected(float x, float y, float z)
    {
        GameObject selectedObject = selectionHandler.CurrentSelection;
        if (selectedObject == null) return;

        Transform selectedTransform = selectedObject.transform.root;
        if (selectedTransform == null) return;

        Vector3 pivot = selectedObject.transform.position;
        Quaternion deltaRotation = Quaternion.Euler(x, y, z);

        Vector3 direction = selectedTransform.position - pivot;
        Vector3 rotatedDirection = deltaRotation * direction;
        selectedTransform.position = pivot + rotatedDirection;

        selectedTransform.rotation = deltaRotation * selectedTransform.rotation;

        InitializeSelection(selectedObject);
    }

    private void RotateSelectedTowardCamera()
    {
        GameObject selectedObject = selectionHandler.CurrentSelection;
        if (selectedObject == null) return;

        Transform selectedTransform = selectedObject.transform.root;
        if (selectedTransform == null) return;

        Vector3 toCamera = Camera.main.transform.position - selectedTransform.position;
        toCamera.y = 0; // Alleen horizontaal kijken
        toCamera.Normalize();

        float dotForward = Vector3.Dot(toCamera, Vector3.forward);
        float dotRight = Vector3.Dot(toCamera, Vector3.right);

        // Bepaal richting met hoogste absolute waarde
        Vector3 euler = Vector3.zero;

        if (Mathf.Abs(dotForward) > Mathf.Abs(dotRight))
        {
            // Meer naar voren/achter
            euler = (dotForward > 0) ? new Vector3(90, 0, 0) : new Vector3(-90, 0, 0);
        }
        else
        {
            // Meer naar rechts/links
            euler = (dotRight < 0) ? new Vector3(0, 0, 90) : new Vector3(0, 0, -90);
        }

        // Roteer met jouw bestaande functie
        RotateSelected(euler.x, euler.y, euler.z);
    }

    private void HandleRotation()
    {
        if (InputSystem.GetButtonDown(InputButton.Rotate1))
        {
            RotateSelected(0, 90, 0);
        }
        if (InputSystem.GetButtonDown(InputButton.Rotate2))
        {
            RotateSelectedTowardCamera();
        }
    }

    /// <summary>
    /// Handles mouse input depending on the current drag state.
    /// </summary>
    private void HandleInput()
    {
        switch (currentState)
        {
            case DragState.Idle:
                if (InputSystem.GetPointerDown())
                    TryStartDragging();
                break;

            case DragState.Dragging:
                if (selectedTransform == null || InputSystem.GetPointerUp())
                    StopDragging();
                else if (InputSystem.GetPointerHeld())
                {
                    UpdateTargetTransform();
                    ApplyTransformToSelection();
                }
                break;
        }
    }

    private void InitializeSelection(GameObject selectedObject)
    {
        selectedTransform = selectedObject.transform.root;
        localOffset = cam.transform.InverseTransformPoint(selectedTransform.position);

        if (allowRotation.X || allowRotation.Y || allowRotation.Z)
            rotationOffset = Quaternion.Inverse(cam.transform.rotation) * selectedTransform.rotation;
    }

    /// <summary>
    /// Attempts to start dragging the currently selected object.
    /// </summary>
    private void TryStartDragging()
    {
        GameObject selectedObject = selectionHandler.CurrentSelection;
        if (selectedObject == null) return;

        var selectable = selectedObject.GetComponent<Selectable>();
        if (selectable == null || !selectable.IsDraggable) return;
        selectionHandler.LockSelection();

        InitializeSelection(selectedObject);
        OnGrabEvent(selectedObject);

        currentState = DragState.Dragging;
    }

    /// <summary>
    /// Invokes the grab event on the object if it has a IGrabbable component.
    /// </summary>
    /// <param name="targetObject">The object being grabbed.</param>
    private void OnGrabEvent(GameObject targetObject)
    {
        targetObject.GetComponent<IGrabbable>()?.OnGrab();
    }

    /// <summary>
    /// Stops dragging and restores the Rigidbody state.
    /// </summary>
    private void StopDragging()
    {
        if (selectedTransform != null)
                OnReleaseEvent(selectedTransform.gameObject);

        //RestoreRigidbodyIfPresent();
        selectedTransform = null;
        selectionHandler.UnlockSelection();
        currentState = DragState.Idle;
    }

    /// <summary>
    /// Invokes the release event on the object if it has a IGrabbable.
    /// </summary>
    /// <param name="targetObject">The object being released.</param>
    void OnReleaseEvent(GameObject targetObject)
    {
        targetObject.GetComponent<IGrabbable>()?.OnRelease();
    }

    private void ApplyPlacementConstraints()
    {
        if (selectedTransform != null)
        {
            // Haal alle colliders op van het object en zijn kinderen
            Collider[] colliders = selectedTransform.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                // Combineer alle collider-bounds tot één bounding box in wereldruimte
                Renderer[] renderers = selectedTransform.GetComponentsInChildren<Renderer>();
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
                // Bepaal hoe ver de onderkant van het object onder minY zou zakken
                float offsetY = minY - combinedBounds.min.y;

                if (offsetY > 0f)
                {
                    Vector3 newPosition = selectedTransform.position + new Vector3(0f, offsetY, 0f);
                    selectedTransform.position = newPosition;
                    targetPosition = newPosition;

                    // <<< FIX: update localOffset zodat volgende frame niet weer te laag zit
                    //localOffset = localOffset + cam.transform.InverseTransformVector(Vector3.up * offsetY);
                }
            }
        }
    }

    /// <summary>
    /// Updates the target position and rotation for the dragged object.
    /// </summary>
    private void UpdateTargetTransform()
    {
        if (selectedTransform == null) return;
        Vector3 worldTargetPosition = cam.transform.TransformPoint(localOffset);
        targetPosition = SnapToGrid(worldTargetPosition);

        if (allowRotation.X || allowRotation.Y || allowRotation.Z)
            targetRotation = GetDragRotation();
        else
            targetRotation = Quaternion.Euler(GetSnappedRotation(selectedTransform.rotation.eulerAngles));
    }

    /// <summary>
    /// Moves the selected object to the target position and rotation.
    /// </summary>
    private void ApplyTransformToSelection()
    {
        if (selectedTransform == null) return;

        if (smoothMovement)
        {
            selectedTransform.position = Vector3.Lerp(
                selectedTransform.position,
                targetPosition,
                moveResponsiveness * Time.deltaTime
            );

            Quaternion interpolated = Quaternion.Slerp(
                selectedTransform.rotation,
                targetRotation,
                moveResponsiveness * Time.deltaTime
            );

            if (IsNormalized(interpolated))
                selectedTransform.rotation = interpolated;
        }
        else
        {
            selectedTransform.position = targetPosition;
            if (IsNormalized(targetRotation))
                selectedTransform.rotation = targetRotation;
        }

        ApplyPlacementConstraints();
    }

    private Quaternion GetDragRotation()
    {
        Quaternion rawRotation = cam.transform.rotation * rotationOffset;
        Vector3 cameraEuler = rawRotation.eulerAngles;
        Vector3 currentEuler = selectedTransform.rotation.eulerAngles;

        Vector3 resultEuler = currentEuler;

        if (allowRotation.X)
            resultEuler.x = cameraEuler.x;
        if (allowRotation.Y)
            resultEuler.y = cameraEuler.y;
        if (allowRotation.Z)
            resultEuler.z = cameraEuler.z;

        return Quaternion.Euler(GetSnappedRotation(resultEuler));
    }

    Vector3 GetSnappedRotation(Vector3 rotation) {
        // Apply snapping if enabled
        if (rotationSnapDegrees.x > 0f)
            rotation.x = Mathf.Round(rotation.x / rotationSnapDegrees.x) * rotationSnapDegrees.x;
        if (rotationSnapDegrees.y > 0f)
            rotation.y = Mathf.Round(rotation.y / rotationSnapDegrees.y) * rotationSnapDegrees.y;
        if (rotationSnapDegrees.z > 0f)
            rotation.z = Mathf.Round(rotation.z / rotationSnapDegrees.z) * rotationSnapDegrees.z;

        return rotation;
    }


    /// <summary>
    /// Snaps the rotation to specified angular increments.
    /// </summary>
    // private Quaternion GetSnappedRotation()
    // {
    //     Quaternion rawRotation = cam.transform.rotation * rotationOffset;
    //     Vector3 euler = rawRotation.eulerAngles;

    //     if (rotationSnapDegrees.x > 0f) euler.x = Mathf.Round(euler.x / rotationSnapDegrees.x) * rotationSnapDegrees.x;
    //     if (rotationSnapDegrees.y > 0f) euler.y = Mathf.Round(euler.y / rotationSnapDegrees.y) * rotationSnapDegrees.y;
    //     if (rotationSnapDegrees.z > 0f) euler.z = Mathf.Round(euler.z / rotationSnapDegrees.z) * rotationSnapDegrees.z;

    //     return Quaternion.Euler(euler);
    // }

    // /// <summary>
    // /// Disables the Rigidbody on the object if it exists, and stores its original state.
    // /// </summary>
    // private void DisableRigidbodyIfPresent(GameObject obj)
    // {
    //     Rigidbody rb = obj.GetComponent<Rigidbody>();
    //     if (rb != null)
    //     {
    //         originalRigidbody = rb;
    //         originalKinematicState = rb.isKinematic;
    //         rb.isKinematic = true;
    //     }
    // }

    // /// <summary>
    // /// Restores the original Rigidbody state if one was modified.
    // /// </summary>
    // private void RestoreRigidbodyIfPresent()
    // {
    //     if (originalRigidbody != null)
    //     {
    //         originalRigidbody.isKinematic = originalKinematicState;
    //         originalRigidbody = null;
    //     }
    // }

    /// <summary>
    /// Snaps a position to the configured grid size and center.
    /// </summary>
    private Vector3 SnapToGrid(Vector3 position)
    {
        Vector3 localPos = position - gridCenter;

        if (gridSize.x > 0f) localPos.x = Mathf.Round(localPos.x / gridSize.x) * gridSize.x;
        if (gridSize.y > 0f) localPos.y = Mathf.Round(localPos.y / gridSize.y) * gridSize.y;
        if (gridSize.z > 0f) localPos.z = Mathf.Round(localPos.z / gridSize.z) * gridSize.z;

        return localPos + gridCenter;
    }

    /// <summary>
    /// Returns true if the given quaternion is approximately normalized.
    /// </summary>
    private bool IsNormalized(Quaternion q, float tolerance = 0.0001f)
    {
        float magnitude = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        return Mathf.Abs(1f - magnitude) < tolerance;
    }
}
