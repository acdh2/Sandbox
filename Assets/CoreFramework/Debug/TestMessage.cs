using UnityEngine;

public class TestMessage : MonoBehaviour
{
    public void PrintMessage(string message)
    {
        Debug.Log(message);
    }

    public void PrintFloat(float value)
    {
        Debug.Log(value);
    }

    public void PrintObject(object value)
    {
        Debug.Log(value.ToString());
    }

}
