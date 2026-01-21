using UnityEngine;

public class FMSBoot : MonoBehaviour
{
    [SerializeField] Animator anim;
    static readonly int IsOpen = Animator.StringToHash("IsOpen");

    void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        anim.enabled = true;
        anim.SetBool(IsOpen, false);
        anim.Play("Close_FMS", 0, 1f); // force closed pose immediately
        anim.Update(0f);
    }
}
