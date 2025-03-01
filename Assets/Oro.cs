using UnityEngine;

public class Oro : MonoBehaviour
{
    public Transform ladron; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == ladron)
        {
            Debug.Log("¡El ladrón ha robado el objeto!");
            Destroy(gameObject);
        }
    }
}
