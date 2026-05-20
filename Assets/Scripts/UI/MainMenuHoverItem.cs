using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to each menu Button in the Main Menu scene.
/// Assign the shared controller and this button's index so hover
/// events show/hide the matching arrow Images.
/// </summary>
public class MainMenuHoverItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private MainMenuController controller;
    [SerializeField] private int itemIndex;

    public void OnPointerEnter(PointerEventData eventData) => controller.OnItemPointerEnter(itemIndex);
    public void OnPointerExit(PointerEventData eventData)  => controller.OnItemPointerExit(itemIndex);
}
