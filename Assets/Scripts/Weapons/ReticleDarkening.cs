using UnityEngine;

public class ReticleDarkening : MonoBehaviour
{
    public Material darkeningMaterial;  // Referenca na materijal s prilagođenim shaderom
    public RectTransform reticle;  // RectTransform ciljnika

    void Update()
    {
        // Položaj ciljnika na ekranu
        Vector2 reticlePos = reticle.position;
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);

        // Normaliziraj položaj ciljnika
        Vector2 normalizedPos = new Vector2(reticlePos.x / Screen.width, reticlePos.y / Screen.height);

        // Ažuriraj središte kruga shadera kako bi odgovaralo položaju ciljnika
        darkeningMaterial.SetVector("_CircleCenter", new Vector4(normalizedPos.x, normalizedPos.y, 0.0f, 0.0f));
    }
}
