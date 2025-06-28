using UnityEngine;
using TMPro;

public class DisplayItemInfo : MonoBehaviour
{
    public SelectionHandler selectionHandler;
    public DragHandler dragHandler;

    public TextMeshProUGUI itemName;
    public TextMeshProUGUI itemDescription;

    void Update()
    {
        GameObject selected = null;
        if (dragHandler)
        {
            if (dragHandler.CurrentState == DragState.Idle)
            {
                if (selectionHandler)
                {
                    selected = selectionHandler.CurrentSelection;
                }
            }
        }
        UpdateItemInfo(selected);
    }

    void UpdateItemInfo(GameObject item)
    {
        if (itemName && itemDescription)
        {
            if (item != null)
            {
                string name = item.name;
                string description = item.GetComponent<Selectable>()?.ObjectDescription ?? "";

                itemName.enabled = true;
                itemDescription.enabled = true;
                itemName.text = name;
                itemDescription.text = description;
            }
            else
            {
                itemName.enabled = false;
                itemDescription.enabled = false;
            }
        }
    }
}
