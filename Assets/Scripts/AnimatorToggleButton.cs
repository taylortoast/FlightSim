using UnityEngine;
using TMPro;
using System.Collections;

public class AnimatorToggleButton : MonoBehaviour
{
    [Header("Animator Setup")]
    [SerializeField] private Animator animator;
    [SerializeField] private string boolParameter = "isOpen";

    [Header("UI Setup")]
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField] private string openLabel = "Close";
    [SerializeField] private string closedLabel = "Open";

    [Header("Behavior")]
    [SerializeField] private bool allowCloseOnSecondClick = true;




    private int boolHash;
    private bool isBusy;

    void Awake()
    {
        boolHash = Animator.StringToHash(boolParameter);
        SyncLabel();
    }

    public void Toggle()
    {
        if (!animator || isBusy) return;

        bool isOpen = animator.GetBool(boolHash);

        if (isOpen)
        {
            if (allowCloseOnSecondClick) Close();
        }
        else
        {
            UiSlideGroup.RequestOpen(this);
            Open();
        }
    }

    public void Open()
    {
        if (!animator) return;
        isBusy = true;
        animator.SetBool(boolHash, true);
        SyncLabel();
    }

    public void Close()
    {
        if (!animator) return;
        isBusy = true;
        animator.SetBool(boolHash, false);
        UiSlideGroup.NotifyClosed(this);
        SyncLabel();
    }

    // Called by UiSlideGroup when another panel opens
    public void ForceClose()
    {
        if (!animator) return;
        animator.SetBool(boolHash, false);
        UiSlideGroup.NotifyClosed(this);
        SyncLabel();
    }





    private void SyncLabel()
    {
        if (!buttonLabel || !animator) return;
        buttonLabel.text = animator.GetBool(boolHash) ? openLabel : closedLabel;
    }

    public void OnSlideAnimationComplete()
    {
        isBusy = false;
    }

}
