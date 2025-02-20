using UnityEngine;

public class VisionPoliciaTrigger : MonoBehaviour
{
    public Ladron scriptLadron; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == scriptLadron.transform)
        {
            Debug.Log("¡Ladrón detectado! Deteniendo al ladrón.");
            scriptLadron.enabled = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == scriptLadron.transform)
        {
            Debug.Log("Ladrón fuera de rango. Puede moverse de nuevo.");
            scriptLadron.enabled = true; 
        }
    }
}
