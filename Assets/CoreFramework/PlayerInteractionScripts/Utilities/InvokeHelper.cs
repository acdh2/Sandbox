using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Utility class to delay execution of actions.
/// Use: InvokeHelper.InvokeAfter(seconds, () => { /* your code */ });
/// </summary>
public class InvokeHelper : MonoBehaviour
{
    private static InvokeHelper _instance;

    private static InvokeHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("InvokeHelper");
                _instance = go.AddComponent<InvokeHelper>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    /// <summary>
    /// Invoke an action after a delay.
    /// </summary>
    /// <param name="delay">Time in seconds to wait.</param>
    /// <param name="action">Action to invoke.</param>
    public static void InvokeAfter(float delay, Action action)
    {
        Instance.StartCoroutine(Instance.DelayedInvoke(delay, action));
    }

    private IEnumerator DelayedInvoke(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
}
