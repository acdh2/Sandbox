using UnityEngine;

public interface IWeldable
{
    void OnWeld(Welder welder);
    void OnUnweld(Welder welder);
}
