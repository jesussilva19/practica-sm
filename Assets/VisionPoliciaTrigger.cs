using UnityEngine;

public class VisionPoliciaTrigger : MonoBehaviour
{
    public Transform ladron;

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == ladron)
        {
            Debug.Log("ladron detectado");
            
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == ladron)
        {
            Debug.Log("fuera de rango");
        }
    }
}
