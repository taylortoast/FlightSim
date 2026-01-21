using UnityEngine;
using TMPro;

public class FMSToggle : MonoBehaviour
{
    [SerializeField] private Animator fmsAnimator;
    [SerializeField] private TMP_Text buttonLabel;

    private static readonly int IsOpen = Animator.StringToHash("IsOpen");

    void Awake()
    {
        SyncLabel();
    }

    public void ToggleFMS()
    {
        if (!fmsAnimator) return;

        bool open = fmsAnimator.GetBool(IsOpen);
        fmsAnimator.SetBool(IsOpen, !open);

        SyncLabel();
    }

    private void SyncLabel()
    {
        if (!buttonLabel || !fmsAnimator) return;

        bool open = fmsAnimator.GetBool(IsOpen);
        buttonLabel.text = open ? "Close FMS" : "Open FMS";
    }
}
