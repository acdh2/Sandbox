using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Motor : MonoBehaviour, IWeldable
{

    private WeldingService weldingService;

    public Transform connector;
    public float radius = 0.01f;

    void Start()
    {
        weldingService = new WeldingService();
    }

    private GameObject GetConnectedObject()
    {
        if (connector != null)
        {
            Collider[] hits = Physics.OverlapSphere(connector.transform.position, radius);

            foreach (Collider col in hits)
            {
                return col.gameObject;
            }
        }

        return null;
    }

    private Rigidbody EnsureRigidbody(GameObject obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (!rb)
        {
            rb = obj.AddComponent<Rigidbody>();
        }
        return rb;
    }

    public void OnWeld()
    {
        print("motor welded!");

        GameObject target = GetConnectedObject(); // object dat de as raakt
        if (target == null)
        {
            Debug.LogWarning("Geen overlappend object gevonden voor motor-as.");
            return;
        }

        // Start coroutine die alles regelt
        StartCoroutine(HandleMotorWeld(target));
    }

    private IEnumerator HandleMotorWeld(GameObject target)
    {
        // 1. Weld het overlappende object
        GameObject newRoot = null;
        yield return StartCoroutine(weldingService.Weld(target, result =>
        {
            newRoot = result;
        }));

        if (newRoot == null)
        {
            Debug.LogError("Weld faalde of gaf geen root terug.");
            yield break;
        }

        // 2. Zoek huidige root van motorgroep na weld
        GameObject motorRoot = WeldingUtils.GetWeldableRoot(this.gameObject);
        if (motorRoot == null)
        {
            Debug.LogError("Kan root van motor niet vinden.");
            yield break;
        }

        // 3. Zorg dat beide roots Rigidbody hebben
        Rigidbody motorRb = EnsureRigidbody(motorRoot);
        Rigidbody targetRb = EnsureRigidbody(newRoot);

        // 4. Maak Hingejoint op motorRoot
        HingeJoint joint = motorRoot.AddComponent<HingeJoint>();
        joint.connectedBody = targetRb;
        joint.axis = Vector3.up; // (0,1,0)
        joint.anchor = new Vector3(0, 0.5f, 0);
        joint.useMotor = true;

        JointMotor motor = new JointMotor
        {
            force = 1000f,
            targetVelocity = 100f,
            freeSpin = false
        };
        joint.motor = motor;

        Debug.Log("HingeJoint toegevoegd tussen motorgroep en doelobject.");
    }

    public void OnUnweld()
    {
        print("motor unwelded!");

        // Zoek root van dit object
        GameObject motorRoot = WeldingUtils.GetWeldableRoot(this.gameObject);
        if (motorRoot == null)
        {
            Debug.LogWarning("OnUnweld: Kan root van motor niet vinden.");
            return;
        }

        // Verwijder HingeJoint van de root, als die er is
        HingeJoint joint = motorRoot.GetComponent<HingeJoint>();
        if (joint != null)
        {
            Destroy(joint);
            Debug.Log("HingeJoint verwijderd van motorgroep.");
        }
    }

}
