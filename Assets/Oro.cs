using UnityEngine;

public class Oro : MonoBehaviour
{
    public Transform ladron; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == ladron)
        {
            Debug.Log("¡El ladrón ha robado el objeto!");
            transform.SetParent(ladron); // Asigna el oro como hijo del ladrón
            transform.localPosition = Vector3.zero; // Opcional: Ajusta la posición relativa del oro
        }
    }
}
