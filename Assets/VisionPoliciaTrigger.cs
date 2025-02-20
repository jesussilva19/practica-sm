using UnityEngine;

public class VisionPoliciaTrigger : MonoBehaviour
{
    public Ladron scriptLadron; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == scriptLadron.transform)
        {
            Debug.Log("�Ladr�n detectado! Deteniendo al ladr�n.");
            scriptLadron.enabled = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == scriptLadron.transform)
        {
            Debug.Log("Ladr�n fuera de rango. Puede moverse de nuevo.");
            scriptLadron.enabled = true; 
        }
    }
}
