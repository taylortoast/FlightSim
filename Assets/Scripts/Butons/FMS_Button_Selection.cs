using TMPro;
using UnityEngine;

public class FMS_Button_Selection : MonoBehaviour
{
    [Header("Target TMP (Scratchpad_Input)")]
    [SerializeField] private TMP_Text scratchpadText;

    [Header("Rules")]
    [SerializeField] private int maxChars = 24;
    [SerializeField] private bool forceUppercase = true;

    public void OnKey(string key)
    {
        if (!scratchpadText || string.IsNullOrEmpty(key)) return;

        string cur = scratchpadText.text ?? "";
        if (cur.Length >= maxChars) return;

        string add = forceUppercase ? key.ToUpperInvariant() : key;
        scratchpadText.text = cur + add;
    }
    public void OnDegree() => OnKey("\u00B0"); // °

    public void OnPlusMinus()
    {
        if (!scratchpadText) return;

        string cur = scratchpadText.text ?? "";
        if (cur.Length == 0) { OnKey("+"); return; }

        char last = cur[cur.Length - 1];
        if (last == '+') scratchpadText.text = cur.Substring(0, cur.Length - 1) + "-";
        else if (last == '-') scratchpadText.text = cur.Substring(0, cur.Length - 1) + "+";
        else OnKey("+");
    }


}
