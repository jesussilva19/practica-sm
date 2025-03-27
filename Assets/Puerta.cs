using UnityEngine;

public class PuertaSimple : MonoBehaviour
{
    public float velocidad = 90f;
    private bool abrir = false;
    private Quaternion rotInicial;
    private Quaternion rotAbierta;

    void Start()
    {
        rotInicial = transform.rotation;
        rotAbierta = Quaternion.Euler(transform.eulerAngles + new Vector3(0, 90, 0));
    }

    void Update()
    {
        Quaternion rotObjetivo = abrir ? rotAbierta : rotInicial;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotObjetivo, velocidad * Time.deltaTime);

        if (abrir)
            Debug.Log(" Abriendo puerta...");
    }

    private void OnTriggerEnter(Collider other)
    {
        string nombreRaiz = other.transform.root.name;

        if (nombreRaiz == "Ladron" || nombreRaiz.StartsWith("Policia"))
        {
            abrir = true;
            Debug.Log("✅ Puerta detectó a: " + nombreRaiz);
        }
    }


    private void OnTriggerExit(Collider other)
    {
        string nombre = other.gameObject.name;

        if (nombre == "Ladron" || nombre.StartsWith("Policia"))
        {
            abrir = false;
            Debug.Log("Puerta dejó de detectar: " + nombre);
        }
    }

}

