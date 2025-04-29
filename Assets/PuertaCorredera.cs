// PuertaCorredera.cs
using UnityEngine;

public class PuertaCorredera : MonoBehaviour
{
    [Header("Configuración")]
    public Transform posicionCerrada;
    public Transform posicionAbierta;
    public float velocidad = 5.0f;
    public float tiempoEspera = 2.0f;

    [Header("Sonidos")]
    public AudioClip sonidoAbrir;
    public AudioClip sonidoCerrar;

    private AudioSource audioSource;
    private Vector3 posicionObjetivo;
    private bool alguienDetectado = false;
    private float tiempoContador = 0f;

    void Start()
    {
        // Verificar y crear posiciones si es necesario
        if (posicionCerrada == null || posicionAbierta == null)
        {
            Debug.LogError("¡Debes asignar posiciones de apertura y cierre en el Inspector!");

            // Crear posiciones por defecto para evitar errores
            if (posicionCerrada == null)
            {
                GameObject posCerrada = new GameObject("PosicionCerrada");
                posCerrada.transform.position = transform.position;
                posicionCerrada = posCerrada.transform;
            }

            if (posicionAbierta == null)
            {
                GameObject posAbierta = new GameObject("PosicionAbierta");
                posAbierta.transform.position = transform.position + new Vector3(3.0f, 0, 0);
                posicionAbierta = posAbierta.transform;
            }
        }

        posicionObjetivo = posicionCerrada.position;

        // Configurar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    void Update()
    {

        // Movimiento suave hacia la posición objetivo
        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, velocidad * Time.deltaTime);

        // Lógica de tiempo para cierre automático
        if (alguienDetectado)
        {
            tiempoContador = 0f;
        }
        else if (Vector3.Distance(transform.position, posicionAbierta.position) < 0.1f)
        {
            tiempoContador += Time.deltaTime;

            if (tiempoContador >= tiempoEspera)
            {
                CerrarPuerta();
            }
        }
    }

    public void PersonaDetectada(bool detectado)
    {
        alguienDetectado = detectado;

        if (detectado)
        {
            AbrirPuerta();
        }
    }

    void AbrirPuerta()
    {
        posicionObjetivo = posicionAbierta.position;
        if (sonidoAbrir != null)
        {
            audioSource.clip = sonidoAbrir;
            audioSource.Play();
        }
    }

    void CerrarPuerta()
    {
        posicionObjetivo = posicionCerrada.position;
        if (sonidoCerrar != null)
        {
            audioSource.clip = sonidoCerrar;
            audioSource.Play();
        }
    }
}
