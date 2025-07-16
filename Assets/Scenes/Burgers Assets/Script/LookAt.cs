using UnityEngine;

public class LookAt : MonoBehaviour
{
    public Transform Target;

    [SerializeField]
    public Transform target
    {
        get { return Target; }
        set { Target = value; }
    }

    void Update()
    {
        if (Target == null) return;

        Vector3 direction = Target.position - transform.position;
        direction.y = 0; // Negeer Y voor alleen horizontale rotatie

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            Vector3 euler = targetRotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0, euler.y, 0);
        }
    }
}
