using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class FMS_CLR_DEL_Button : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Target TMP (Scratchpad_Input)")]
    [SerializeField] private TMP_Text scratchpadText;

    [Header("CLR / DEL Timing")]
    [SerializeField] private float holdTimeSeconds = 0.6f;

    private bool isPressing;
    private bool delFired;
    private Coroutine holdRoutine;

    // ---------- Pointer ----------
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressing = true;
        delFired = false;
        holdRoutine = StartCoroutine(HoldToDelete());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        CleanupHold();

        if (!delFired)
            BackspaceOne(); // CLR on tap
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CleanupHold();
    }

    // ---------- Hold Logic ----------
    private IEnumerator HoldToDelete()
    {
        yield return new WaitForSecondsRealtime(holdTimeSeconds);

        if (!isPressing) yield break;

        DeleteAll();   // DEL fires immediately on hold
        delFired = true;
    }

    private void CleanupHold()
    {
        isPressing = false;

        if (holdRoutine != null)
        {
            StopCoroutine(holdRoutine);
            holdRoutine = null;
        }
    }

    // ---------- Actions ----------
    private void BackspaceOne()
    {
        if (!scratchpadText) return;

        string cur = scratchpadText.text ?? "";
        if (cur.Length == 0) return;

        scratchpadText.text = cur.Substring(0, cur.Length - 1);
    }

    private void DeleteAll()
    {
        if (!scratchpadText) return;
        scratchpadText.text = "";
    }
}
