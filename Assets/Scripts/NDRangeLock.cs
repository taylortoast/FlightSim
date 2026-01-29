using UnityEngine;

[ExecuteAlways]
public class NDRangeLock : MonoBehaviour
{
    public float ndWidthNm = 20f; // full width across the ND view
    public Camera ndCam;

    void OnEnable() { if (!ndCam) ndCam = GetComponent<Camera>(); Apply(); }
    void OnValidate() => Apply();

    void Apply()
    {
        if (!ndCam) return;
        ndCam.orthographic = true;

        float widthM = ndWidthNm * 1852f;
        float aspect = 1f; // your RT is 785x785
        ndCam.orthographicSize = (widthM / aspect) * 0.5f; // half-height in meters
    }
}
