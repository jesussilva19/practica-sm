using UnityEngine;
public class PuertaSimple : MonoBehaviour
{
    public float velocidad = 90f;
    private bool abrir = false;
    private bool estadoAnterior = false; // Para detectar cambios de estado
    private Quaternion rotInicial;
    private Quaternion rotAbierta;

    void Start()
    {
        rotInicial = transform.rotation;
        rotAbierta = Quaternion.Euler(transform.eulerAngles + new Vector3(0, 90, 0));
    }

    void Update()
    {
        ActualizarPuerta();
    }

    private void ActualizarPuerta()
    {
        // Verificar si el estado ha cambiado
        if (abrir != estadoAnterior)
        {
            if (abrir)
            {
                Debug.Log("Abriendo puerta...");
            }
            else
            {
                Debug.Log("Cerrando puerta...");
            }
            estadoAnterior = abrir; // Actualizar el estado anterior
        }

        // Actualizar la rotación
        Quaternion rotObjetivo = abrir ? rotAbierta : rotInicial;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotObjetivo, velocidad * Time.deltaTime);
    }

    public void CambiarEstadoPuerta(bool estaAbierta)
    {
        abrir = estaAbierta;
    }

    private void OnTriggerEnter(Collider other)
    {
        string nombreRaiz = other.transform.root.name;
        if (nombreRaiz == "Ladron" || nombreRaiz.StartsWith("Policia"))
        {
            CambiarEstadoPuerta(true);
            Debug.Log("Puerta detectó a: " + nombreRaiz);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        string nombreRaiz = other.transform.root.name;
        if (nombreRaiz == "Ladron" || nombreRaiz.StartsWith("Policia"))
        {
            CambiarEstadoPuerta(false);
            Debug.Log("Puerta dejó de detectar: " + nombreRaiz);
        }
    }
}