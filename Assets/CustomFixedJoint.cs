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
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            //Physics.SyncTransforms(); // Essentieel om de physics-engine bij te werken
            //rb.WakeUp(); // Zorg ervoor dat het object actief blijft voor physics-interacties
        }
    }

    // Het is vaak het beste om deze update in LateUpdate() te doen.
    // Dit zorgt ervoor dat alle andere bewegingen (input, animaties, etc.) van het targetTransform
    // al zijn verwerkt voordat dit object volgt.
    void LateUpdate()
    {
        //UpdateJoint();
    }
}