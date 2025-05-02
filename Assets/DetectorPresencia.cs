using UnityEngine;
using System.Collections;


public class DetectorPresencia : MonoBehaviour
{
    public PuertaCorredera puerta;
    public string[] etiquetasDetectar = { "Player" };

    private void Start()
    {
        // Verificar configuración
        if (puerta == null)
        {
            //Debug.LogError("¡Debe asignarse una puerta al detector!");
            // Intentar encontrar una puerta cercana
            puerta = FindObjectOfType<PuertaCorredera>();
        }

        // Verificar que el collider sea trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            //Debug.LogError("¡El detector debe tener un collider!");
            col = gameObject.AddComponent<BoxCollider>();
        }

        if (!col.isTrigger)
        {
            //Debug.LogWarning("El collider del detector debe estar marcado como trigger");
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Objeto detectado: " + other.gameObject.name + " con tag: " + other.tag);

        foreach (string etiqueta in etiquetasDetectar)
        {
            if (other.CompareTag(etiqueta))
            {
                if (puerta != null)
                {
                    puerta.PersonaDetectada(true);
                    //Debug.Log("Persona detectada - ENTRANDO");
                }
                break;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        foreach (string etiqueta in etiquetasDetectar)
        {
            if (other.CompareTag(etiqueta))
            {
                if (puerta != null)
                {
                    puerta.PersonaDetectada(false);
                    //Debug.Log("Persona detectada - SALIENDO");
                }
                break;
            }
        }
    }

}

