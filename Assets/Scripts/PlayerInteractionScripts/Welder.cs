using UnityEngine;

[RequireComponent(typeof(SelectionHandler))]
public class Welder : MonoBehaviour
{
    [Header("Weld Settings")]

    private WeldingService weldingService;
    private SelectionHandler selectionHandler;

    private void Start()
    {
        selectionHandler = GetComponent<SelectionHandler>();
        weldingService = new WeldingService();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryToggleWeld();
        }
    }

    private void TryToggleWeld()
    {
        GameObject selected = selectionHandler.CurrentSelection;
        if (selected == null) return;

        if (WeldingUtils.IsWelded(selected))
        {
            weldingService.Unweld(selected);
        }
        else
        {
            selectionHandler.ClearSelection();
            StartCoroutine(weldingService.Weld(selected));
        }
    }

}
