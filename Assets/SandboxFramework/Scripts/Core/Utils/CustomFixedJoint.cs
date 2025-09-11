using System.Collections.Generic;
using UnityEngine;

public class CustomFixedJoint : MonoBehaviour
{
    [Tooltip("Het Transform-component van het object waaraan dit object vastzit.")]
    public Transform targetTransform; 

    // De initiële relatieve positie en rotatie t.o.v. het targetTransform
    private Vector3 initialLocalPosition; 
    private Quaternion initialLocalRotation;

    private FixedJoint fixedJoint;

    public WeldType weldType = WeldType.Undefined;

    void Start()
    {
        if (targetTransform == null)
        {
            Debug.LogError("CustomFixedJoint: Target Transform is niet toegewezen op " + gameObject.name + "! Schakel het script uit.");
            enabled = false; // Schakel het script uit als er geen target is
            return;
        }

        initialLocalPosition = transform.InverseTransformPoint(targetTransform.position);
        initialLocalRotation = Quaternion.Inverse(transform.rotation) * targetTransform.rotation;

        if (weldType != WeldType.HierarchyBased)
        {
            Rigidbody targetRigidbody = targetTransform.gameObject.GetComponent<Rigidbody>();
            if (targetRigidbody)
            {
                fixedJoint = gameObject.AddComponent<FixedJoint>();
                fixedJoint.connectedBody = targetRigidbody;
                weldType = WeldType.PhysicsBased;
            }
            else
            {
                weldType = WeldType.HierarchyBased;
            }
        }
    }

    void OnDestroy()
    {
        if (fixedJoint != null)
        {
            Destroy(fixedJoint);
        }        
    }

    Transform transformToUpdate = null;

    // Deze methode zorgt ervoor dat dit object het targetTransform volgt.
    // Roep deze methode aan in LateUpdate of vanuit je sleepsysteem.
    public static void UpdateJoint(Transform t)
    {
        Stack<Transform> stack = new();
        stack.Push(t);

        HashSet<Transform> visited = new();

        while (stack.Count > 0)
        {
            Transform current = stack.Pop();

            if (visited.Contains(current)) continue;
            visited.Add(current);

            foreach (CustomFixedJoint cj in current.GetComponents<CustomFixedJoint>())
            {
                if (!visited.Contains(cj.targetTransform))
                {
                    cj.UpdateSingleTransform();
                    stack.Push(cj.targetTransform);
                }
            }
        }

        Physics.SyncTransforms();
    }


    public void UpdateSingleTransform()
    {
        if (targetTransform == null) return;

        // Bereken de gewenste wereldpositie en rotatie voor dit object,
        // gebaseerd op de huidige positie/rotatie van het targetTransform.
        Vector3 desiredPosition = transform.TransformPoint(initialLocalPosition);
        Quaternion desiredRotation = transform.rotation * initialLocalRotation;

        // Pas de positie en rotatie direct toe.
        targetTransform.position = desiredPosition;
        targetTransform.rotation = desiredRotation;

        // --- Optioneel: Voor Rigidbodies ---
        // Als dit object een Rigidbody heeft en je wilt dat de physics-engine hiervan op de hoogte is,
        // stel dan de Rigidbody.position/rotation in en roep Physics.SyncTransforms() aan.
        // Zorg ervoor dat Rigidbody.isKinematic op TRUE staat als je de transform direct instelt.
        Rigidbody rb = targetTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = desiredPosition;
            rb.rotation = desiredRotation;
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            //Physics.SyncTransforms(); // Essentieel om de physics-engine bij te werken
                //rb.WakeUp(); // Zorg ervoor dat het object actief blijft voor physics-interacties
            }
    }

    // Het is vaak het beste om deze update in LateUpdate() te doen.
    // Dit zorgt ervoor dat alle andere bewegingen (input, animaties, etc.) van het targetTransform
    // al zijn verwerkt voordat dit object volgt.
    // void LateUpdate()
    // {
    //     UpdateJoint();
    // }
}

// using System.Collections.Generic;
// using UnityEngine;

// /// <summary>
// /// A custom joint component that mimics a FixedJoint, connecting a GameObject
// /// to a target Transform. It can operate based on either a physics-driven
// /// FixedJoint or a direct hierarchy-based transform update.
// /// </summary>
// public class CustomFixedJoint : MonoBehaviour
// {
//     // The Transform component of the object this object is attached to.
//     [Tooltip("The Transform component of the object this object is attached to.")]
//     public Transform targetTransform;

//     // The initial relative position and rotation of this object to the targetTransform.
//     private Vector3 initialLocalPosition;
//     private Quaternion initialLocalRotation;

//     private FixedJoint fixedJoint;

//     public WeldType weldType = WeldType.Undefined;
    
//     // An enum to define the different types of joint behavior.
//     public enum WeldType
//     {
//         Undefined,
//         HierarchyBased,
//         PhysicsBased
//     }

//     /// <summary>
//     /// Initializes the joint. It checks for the target and determines
//     /// whether to use a physics-based FixedJoint or a hierarchy-based approach.
//     /// </summary>
//     void Start()
//     {
//         if (targetTransform == null)
//         {
//             Debug.LogError("CustomFixedJoint: Target Transform is not assigned on " + gameObject.name + "! Disabling the script.");
//             enabled = false;
//             return;
//         }

//         // Calculate the initial offset
//         initialLocalPosition = transform.InverseTransformPoint(targetTransform.position);
//         initialLocalRotation = Quaternion.Inverse(transform.rotation) * targetTransform.rotation;

//         if (weldType != WeldType.HierarchyBased)
//         {
//             Rigidbody targetRigidbody = targetTransform.gameObject.GetComponent<Rigidbody>();
//             if (targetRigidbody)
//             {
//                 fixedJoint = gameObject.AddComponent<FixedJoint>();
//                 fixedJoint.connectedBody = targetRigidbody;
//                 weldType = WeldType.PhysicsBased;
//             }
//             else
//             {
//                 weldType = WeldType.HierarchyBased;
//             }
//         }
//     }

//     /// <summary>
//     /// Cleans up the dynamically created FixedJoint when the component is destroyed.
//     /// </summary>
//     void OnDestroy()
//     {
//         if (fixedJoint != null)
//         {
//             Destroy(fixedJoint);
//         }
//     }

//     /// <summary>
//     /// Traverses a hierarchy of CustomFixedJoints and updates their transforms.
//     /// This method should be called once per frame from a central location
//     /// (e.g., LateUpdate) to ensure all joints are updated correctly.
//     /// It uses a breadth-first traversal to ensure a stable update order.
//     /// </summary>
//     /// <param name="startTransform">The starting Transform of the traversal.</param>
//     public static void UpdateJoints(Transform startTransform)
//     {
//         Stack<Transform> stack = new Stack<Transform>();
//         stack.Push(startTransform);

//         HashSet<Transform> visited = new HashSet<Transform>();

//         while (stack.Count > 0)
//         {
//             Transform current = stack.Pop();

//             if (visited.Contains(current)) continue;
//             visited.Add(current);

//             foreach (CustomFixedJoint cj in current.GetComponents<CustomFixedJoint>())
//             {
//                 if (!visited.Contains(cj.targetTransform))
//                 {
//                     cj.UpdateSingleTransform();
//                     stack.Push(cj.targetTransform);
//                 }
//             }
//         }

//         Physics.SyncTransforms();
//     }

//     /// <summary>
//     /// Updates the position and rotation of the targetTransform based on this
//     /// object's transform. This method is called by UpdateJoints().
//     /// </summary>
//     public void UpdateSingleTransform()
//     {
//         if (targetTransform == null || weldType != WeldType.HierarchyBased)
//         {
//             return;
//         }

//         // Calculate the desired world position and rotation for the target object
//         // based on the current position/rotation of this object.
//         Vector3 desiredPosition = transform.TransformPoint(initialLocalPosition);
//         Quaternion desiredRotation = transform.rotation * initialLocalRotation;

//         // Apply the position and rotation directly.
//         targetTransform.position = desiredPosition;
//         targetTransform.rotation = desiredRotation;
//     }

//     /// <summary>
//     /// It's often best to perform this update in LateUpdate().
//     /// This ensures that all other movements (input, animations, etc.) of the
//     /// target Transform have already been processed before this object follows.
//     /// The commented out code provides an example of how to use UpdateJoints().
//     /// </summary>
//     void LateUpdate()
//     {
//         // Example usage:
//         // UpdateJoints(transform);
//     }
// }