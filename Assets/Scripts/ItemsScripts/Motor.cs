using UnityEngine;

public class Motor : MonoBehaviour, IWeldable
{
    public void OnWeld(Welder welder)
    {
        print("motor welded!");
    }

    public void OnUnweld(Welder welder)
    {
        print("motor unwelded!");
    }
}
