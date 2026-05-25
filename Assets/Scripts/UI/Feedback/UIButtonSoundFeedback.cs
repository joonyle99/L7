using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSoundFeedback : UIButtonFeedback
{
    public override void OnHoverEnter(PointerEventData eventData)
    {
        // SoundManager.Instance.PlaySfx(SfxType.UI_HOVER);
    }

    public override void OnHoverExit(PointerEventData eventData)
    {
        
    }


    public override void OnPress(PointerEventData eventData)
    {

    }

    public override void OnRelease(PointerEventData eventData)
    {
        
    }
}
