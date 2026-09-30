using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Bicicleta que el Guardián puede usar.
///
/// Por qué está en un juego sobre el ODS 11: la meta 11.2 habla de transporte
/// sostenible, y la bicicleta es el ejemplo más directo que hay. Acá además
/// cumple una función de juego concreta: las zonas son grandes y llevar los
/// residuos hasta el contenedor de su color obliga a caminar harto, así que la
/// bici acorta esos viajes. No es un adorno: es la herramienta de movilidad.
///
/// Se maneja sola con el mismo PlayerController —solo le sube la velocidad y le
/// deja la bici debajo— para no tener dos sistemas de movimiento distintos que
/// después se contradigan.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Bicicleta : MonoBehaviour
{
    [Header("Qué tanto acelera")]
    public float multiplicadorVelocidad = 1.85f;
    public float multiplicadorCorrer = 1.25f;    // pedalear fuerte con Shift

    [Header("Dónde se sienta el Guardián")]
    public Vector3 desplazamiento = new Vector3(0f, -0.92f, 0.10f);

    [Header("Sonido")]
    public AudioClip timbre;
    public float volumenTimbre = 0.55f;

    /// <summary>Distancia a la que se puede subir.</summary>
    public float radioUso = 2.6f;

    /// <summary>La bici que está usando el Guardián ahora mismo (si hay alguna).</summary>
    public static Bicicleta EnUso { get; private set; }

    private Transform jugador;
    private PlayerController pc;
    private Transform padreOriginal;
    private Vector3 posOriginal;
    private Quaternion rotOriginal;

    private float velNormal, correrNormal;
    private bool montada;
    private float proximoCambio;

    void Awake()
    {
        padreOriginal = transform.parent;
        posOriginal = transform.position;
        rotOriginal = transform.rotation;

        // El collider es solo para poder acercarse, no para chocar contra ella.
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) { jugador = p.transform; pc = p.GetComponent<PlayerController>(); }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || jugador == null || pc == null) return;

        // Al salir de la partida, el Guardián se baja: si no, la bici se quedaría
        // pegada a él en el menú y reaparecería montada en el siguiente nivel.
        if (gm.estado != GameManager.Estado.Jugando)
        {
            if (montada) Bajar();
            return;
        }

        if (montada)
        {
            SeguirAlJugador();
            if (TeclaUso()) Bajar();
            else if (TeclaTimbre()) Sonar();
            return;
        }

        if (EnUso != null) return;                       // ya está montado en otra
        if (!TeclaUso()) return;

        Vector3 d = jugador.position - transform.position; d.y = 0f;
        if (d.sqrMagnitude > radioUso * radioUso) return;

        Montar();
    }

    private void Montar()
    {
        if (montada || EnUso != null) return;

        montada = true;
        EnUso = this;

        velNormal = pc.velocidad;
        correrNormal = pc.multiplicadorCorrer;
        pc.velocidad = velNormal * multiplicadorVelocidad;
        pc.multiplicadorCorrer = correrNormal * multiplicadorCorrer;

        transform.SetParent(jugador, true);
        SeguirAlJugador();

        GuardianAudio.EnPunto(GuardianAudio.SubirBici, transform.position, 0.6f);
        Avisar("🚲 En bicicleta · E para bajar · T para el timbre");
    }

    private void Bajar()
    {
        montada = false;
        if (EnUso == this) EnUso = null;

        if (pc != null)
        {
            pc.velocidad = velNormal;
            pc.multiplicadorCorrer = correrNormal;
        }

        // Se queda estacionada al lado del Guardián, mirando hacia donde iba.
        transform.SetParent(padreOriginal, true);
        if (jugador != null)
        {
            Vector3 lado = Vector3.Cross(Vector3.up, jugador.forward);
            Vector3 p = jugador.position + lado * 0.9f;
            transform.position = new Vector3(p.x, posOriginal.y, p.z);
            transform.rotation = Quaternion.LookRotation(jugador.forward);
        }
        else
        {
            transform.position = posOriginal;
            transform.rotation = rotOriginal;
        }

        GuardianAudio.EnPunto(GuardianAudio.SubirBici, transform.position, 0.5f);
        Avisar("Bicicleta estacionada");
    }

    private void SeguirAlJugador()
    {
        transform.localPosition = desplazamiento;
        transform.localRotation = Quaternion.identity;
    }

    private void Sonar()
    {
        if (timbre != null)
            GuardianAudio.EnPunto(timbre, transform.position, volumenTimbre);
        else
            GuardianAudio.EnPunto(GuardianAudio.Timbre, transform.position, volumenTimbre);
    }

    private void Avisar(string texto)
    {
        GameManager gm = GameManager.Instance;
        if (gm != null) gm.AvisoPublico(texto, new Color(0.62f, 0.92f, 1f), 3f);
    }

    // --- Teclas (funciona con el sistema de entrada viejo y con el nuevo) ---

    private bool TeclaUso()
    {
        if (Time.time < proximoCambio) return false;
#if ENABLE_INPUT_SYSTEM
        bool p = UnityEngine.InputSystem.Keyboard.current != null
              && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame;
#else
        bool p = Input.GetKeyDown(KeyCode.E);
#endif
        if (p) proximoCambio = Time.time + 0.4f;    // evita subir y bajar en el mismo frame
        return p;
    }

    private bool TeclaTimbre()
    {
#if ENABLE_INPUT_SYSTEM
        return UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.T);
#endif
    }

    /// <summary>Aviso en pantalla cuando el Guardián está cerca y todavía no se sube.</summary>
    void OnTriggerStay(Collider other)
    {
        if (montada || EnUso != null) return;
        if (!other.CompareTag("Player")) return;
        if (Time.time < proximoCambio) return;

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.estado != GameManager.Estado.Jugando) return;
        if (gm.HayAviso) return;                    // no pisar otros avisos del juego

        gm.AvisoPublico("Pulsa E para subir a la bicicleta",
                        new Color(0.62f, 0.92f, 1f), 1.2f);
    }
}
