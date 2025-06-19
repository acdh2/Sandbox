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

    public void OnWeld()
    {
        print("motor welded!");
        GameObject target = GetConnectedObject();
        if (target)
        {
            StartCoroutine(weldingService.Weld(target));
        }
    }

    public void OnUnweld()
    {
        print("motor unwelded!");
        GameObject target = GetConnectedObject();
        if (target)
        {
            weldingService.Unweld(target);
        }
    }
}
