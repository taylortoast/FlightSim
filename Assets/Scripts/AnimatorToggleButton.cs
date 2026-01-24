using UnityEngine;
using TMPro;

public class AnimatorToggleButton : MonoBehaviour
{
    [Header("Animator Setup")]
    [SerializeField] private Animator animator;
    [SerializeField] private string boolParameter = "isOpen";

    [Header("UI Setup")]
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField] private string openLabel = "Close";
    [SerializeField] private string closedLabel = "Open";

    private int boolHash;

    void Awake()
    {
        boolHash = Animator.StringToHash(boolParameter);
        SyncLabel();
    }

    public void Toggle()
    {
        if (!animator) return;

        bool current = animator.GetBool(boolHash);
        animator.SetBool(boolHash, !current);

        SyncLabel();
    }

    private void SyncLabel()
    {
        if (!buttonLabel || !animator) return;

        bool open = animator.GetBool(boolHash);
        buttonLabel.text = open ? openLabel : closedLabel;
    }
}
