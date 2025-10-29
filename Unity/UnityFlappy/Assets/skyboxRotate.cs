using UnityEngine;

public class RotateSkybox : MonoBehaviour
{
    public float rotationSpeed = 1f; // en degr�s par seconde

    void Update()
    {
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * rotationSpeed);
    }
}

