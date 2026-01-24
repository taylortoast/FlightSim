using UnityEngine;

public class TileContent : MonoBehaviour
{
    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SetTexture(Texture2D tex)
    {
        meshRenderer.material.mainTexture = tex;
    }
}
