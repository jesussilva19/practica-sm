using UnityEngine;
using System.Collections;


public class DetectorPresencia : MonoBehaviour
{
    public PuertaCorredera puerta;
    public string[] etiquetasDetectar = { "Player" };


    // Se activa cuando un objeto con la etiqueta especificada entra en el trigger
    private void OnTriggerEnter(Collider other)
    {

        foreach (string etiqueta in etiquetasDetectar)
        {
            if (other.CompareTag(etiqueta))
            {
                if (puerta != null)
                {
                    puerta.PersonaDetectada(true);
                }
                break;
            }
        }
    }

    // Se activa cuando un objeto con la etiqueta especificada sale del trigger
    private void OnTriggerExit(Collider other)
    {
        foreach (string etiqueta in etiquetasDetectar)
        {
            if (other.CompareTag(etiqueta))
            {
                if (puerta != null)
                {
                    puerta.PersonaDetectada(false);
                }
                break;
            }
        }
    }

}

