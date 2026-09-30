using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Guardián de Huancayo - Controlador del jugador (3D, Low Poly, URP).
/// Movimiento con CharacterController relativo a la cámara, giro suave, salto,
/// carrera con Shift y pasos con sonido generado por código. La animación de
/// caminar/idle se reproduce por nombre de clip, así funciona con el Animator
/// que traiga el personaje del pack.
/// Solo se puede mover cuando el juego está en estado "Jugando".
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    public float multiplicadorCorrer = 1.55f;
    public float velocidadGiro = 720f;

    [Header("Física")]
    public float gravedad = -20f;
    public float fuerzaSalto = 6f;

    [Header("Pasos")]
    public float pasoCada = 0.44f;          // segundos entre pasos caminando
    public float volumenPasos = 0.22f;

    /// <summary>
    /// Sonidos de paso del proyecto. La herramienta del editor los engancha sola
    /// desde Assets/Sounds; si no hay ninguno, el juego sigue usando el paso
    /// sintetizado por codigo, asi nunca se queda mudo.
    /// </summary>
    public AudioClip[] clipsPaso;
    public AudioClip clipSalto;
    public float volumenSalto = 0.5f;

    /// <summary>
    /// Pisadas POR SUPERFICIE (pack The Sound Guild). Antes sonaba siempre el
    /// mismo par de pasos, cayera el Guardian en la vereda, en el pasto o en la
    /// orilla del Shullcas, y eso aplanaba todo el recorrido: el oido no
    /// distinguia cuando salias de la pista. Ahora un rayo hacia abajo mira que
    /// pisas y elige el grupo que corresponde. Si falta un grupo se cae al
    /// generico, asi que el juego nunca se queda mudo.
    /// </summary>
    [Header("Pisadas por superficie")]
    public AudioClip[] pasosVereda;     // zapato sobre cemento y asfalto
    public AudioClip[] pasosPasto;      // parques, riberas, cerros
    public AudioClip[] pasosArena;      // orilla del rio
    public AudioClip[] pasosMadera;     // puentes y tarimas del mercado
    public AudioClip[] pasosMetal;      // rejillas, tapas, estribos
    public AudioClip[] clipsCaida;      // aterrizaje despues de un salto

    /// <summary>Que se piso en el ultimo chequeo (0 vereda, 1 pasto, 2 arena, 3 madera, 4 metal).</summary>
    private int superficie;
    private float proximaSuperficie;
    private bool enElAireAntes;

    private CharacterController cc;

    /// <summary>Multiplicador externo de velocidad (el Humo Tóxico lo baja). Vuelve solo a 1.</summary>
    [System.NonSerialized] public float multVelocidad = 1f;
    public bool EnElAire => cc != null && !cc.isGrounded;
    private Vector3 velocidadVertical;
    private Transform cam;

    private Animator anim;
    private string estadoCaminar;
    private string estadoIdle;
    private bool caminando;

    private AudioSource pasos;
    private float relojPaso;

    // Retroceso al ser golpeado (se suma al movimiento y se apaga solo).
    private Vector3 empuje;
    // Acción única del Animator (recoger, golpe, celebrar) y luego vuelve a caminar/idle.
    private float accionHasta;
    private bool refrescarAnim;

    /// <summary>Empuja al Guardián (golpe de un carro): retrocede unos metros.</summary>
    public void Empujar(Vector3 dir, float fuerza)
    {
        dir.y = 0f;
        empuje = dir.normalized * fuerza;
    }

    /// <summary>
    /// Reproduce UNA VEZ el primer clip del Animator cuyo nombre contenga alguna
    /// de las claves (p. ej. "pick", "cheer"). Si el pack no trae ese clip, no
    /// hace nada y el juego sigue con caminar/idle.
    /// </summary>
    public bool Accion(float maxDur, params string[] claves)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return false;
        foreach (AnimationClip cl in anim.runtimeAnimatorController.animationClips)
        {
            if (cl == null) continue;
            string n = cl.name.ToLower();
            foreach (string k in claves)
                if (n.Contains(k))
                {
                    anim.CrossFadeInFixedTime(cl.name, 0.12f);
                    accionHasta = Time.time + Mathf.Min(cl.length, maxDur);
                    refrescarAnim = true;
                    return true;
                }
        }
        return false;
    }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (Camera.main != null) cam = Camera.main.transform;

        anim = GetComponentInChildren<Animator>();
        BuscarClips();
    }

    void Start()
    {
        pasos = gameObject.AddComponent<AudioSource>();
        pasos.playOnAwake = false;
        pasos.loop = false;
        pasos.spatialBlend = 0f;            // los pasos del jugador se oyen parejo
        pasos.volume = volumenPasos;
        pasos.priority = 190;

        AtravesarALaGente();
    }

    /// <summary>
    /// Deja que el Guardián pase POR ENTRE la gente en lugar de chocar con ella.
    ///
    /// Un CharacterController se sale solo de cualquier collider que lo esté
    /// tocando, así que bastaba con que un peatón se le parara al lado para que
    /// el juego fuera empujando al Guardián de a poquitos: en una prueba de un
    /// minuto, parado sin tocar ninguna tecla, terminó seis metros más allá, y
    /// en una zona con tráfico eso lo habría metido a la pista. Como los
    /// peatones son decorado —lo que sí es sólido son los edificios, los carros
    /// y los contenedores— lo correcto es que no se empujen entre ellos.
    /// Se hace por par de colliders, no por capas, para no tocar la
    /// configuración de física del proyecto.
    /// </summary>
    private void AtravesarALaGente()
    {
        if (cc == null) return;

        var gente = new System.Collections.Generic.List<GameObject>();
        foreach (Peaton p in Object.FindObjectsByType<Peaton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (p != null) gente.Add(p.gameObject);
        foreach (Aliado a in Object.FindObjectsByType<Aliado>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (a != null) gente.Add(a.gameObject);

        for (int i = 0; i < gente.Count; i++)
        {
            if (gente[i] == null) continue;
            foreach (Collider c in gente[i].GetComponentsInChildren<Collider>(true))
            {
                if (c == null || c.isTrigger) continue;
                Physics.IgnoreCollision(cc, c, true);
            }
        }
    }

    private void BuscarClips()
    {
        if (anim == null || anim.runtimeAnimatorController == null) return;
        foreach (AnimationClip cl in anim.runtimeAnimatorController.animationClips)
        {
            string n = cl.name.ToLower();
            if (estadoCaminar == null && (n.Contains("walk") || n.Contains("jog") || n.Contains("run")))
                estadoCaminar = cl.name;
            if (estadoIdle == null && n.Contains("idle"))
                estadoIdle = cl.name;
        }
        if (estadoIdle != null) anim.CrossFadeInFixedTime(estadoIdle, 0.1f);
    }

    void Update()
    {
        bool puedeMover = GameManager.Instance == null
            || GameManager.Instance.estado == GameManager.Estado.Jugando;

        Vector2 input = puedeMover ? LeerInput() : Vector2.zero;
        Vector3 dir = new Vector3(input.x, 0f, input.y);

        if (cam != null && dir.sqrMagnitude > 0.01f)
        {
            Vector3 fwd = cam.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = cam.right; right.y = 0f; right.Normalize();
            dir = fwd * input.y + right * input.x;
        }

        bool moviendo = dir.sqrMagnitude > 0.01f;
        bool corriendo = moviendo && puedeMover && CorrerPresionado();

        if (GuardianCameraHUD.PrimeraPersona && cam != null)
        {
            // En primera persona el cuerpo mira hacia donde mira la cámara.
            Vector3 f = cam.forward; f.y = 0f;
            if (f.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(f);
        }
        else if (moviendo)
        {
            Quaternion objetivo = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, objetivo, velocidadGiro * Time.deltaTime);
        }

        float vel = velocidad * (corriendo ? multiplicadorCorrer : 1f) * multVelocidad;
        Vector3 mov = dir.normalized * vel;

        if (cc.isGrounded)
        {
            velocidadVertical.y = -1f;
            if (puedeMover && SaltoPresionado())
            {
                velocidadVertical.y = fuerzaSalto;
                Accion(0.7f, "jump");
                if (clipSalto != null && pasos != null)
                {
                    pasos.pitch = Random.Range(0.94f, 1.08f);
                    pasos.PlayOneShot(clipSalto, volumenSalto);
                }
            }
        }
        else
        {
            velocidadVertical.y += gravedad * Time.deltaTime;
        }

        empuje = Vector3.MoveTowards(empuje, Vector3.zero, 30f * Time.deltaTime);
        multVelocidad = Mathf.MoveTowards(multVelocidad, 1f, 0.8f * Time.deltaTime);
        cc.Move((mov + velocidadVertical + empuje) * Time.deltaTime);
        if (pasos != null) pasos.volume = volumenPasos * GuardianMezcla.Ganancia(GuardianMezcla.Bus.Efectos);

        Animar(moviendo, corriendo);
        MirarSuperficie();
        Aterrizaje();
        Pasos(moviendo && cc.isGrounded, corriendo);
    }

    private void Animar(bool moviendo, bool corriendo)
    {
        if (anim == null) return;
        if (Time.time < accionHasta) { anim.speed = 1f; return; }
        if (refrescarAnim) { refrescarAnim = false; caminando = !moviendo; }
        anim.speed = corriendo ? 1.45f : 1f;

        if (moviendo && !caminando)
        {
            caminando = true;
            if (estadoCaminar != null) anim.CrossFadeInFixedTime(estadoCaminar, 0.15f);
        }
        else if (!moviendo && caminando)
        {
            caminando = false;
            if (estadoIdle != null) anim.CrossFadeInFixedTime(estadoIdle, 0.2f);
        }
    }

    /// <summary>Pasos sobre la vereda: da peso al personaje sin usar audio de terceros.</summary>
    private void Pasos(bool andando, bool corriendo)
    {
        if (pasos == null) return;

        if (!andando) { relojPaso = 0.12f; return; }

        relojPaso -= Time.deltaTime * (corriendo ? 1.5f : 1f);
        if (relojPaso > 0f) return;

        relojPaso = pasoCada;
        pasos.pitch = Random.Range(0.90f, 1.12f);

        AudioClip c = UnoDe(GrupoDeLaSuperficie());
        if (c == null && clipsPaso != null && clipsPaso.Length > 0)
            c = clipsPaso[Random.Range(0, clipsPaso.Length)];
        if (c == null) c = ClipPaso();

        // El pasto y la arena suenan mas apagados que el cemento.
        float cuerpoVol = (superficie == 1) ? 0.78f : (superficie == 2 ? 0.70f : 1f);
        pasos.PlayOneShot(c, (corriendo ? 1f : 0.8f) * cuerpoVol);
    }

    private AudioClip UnoDe(AudioClip[] g)
    {
        if (g == null || g.Length == 0) return null;
        AudioClip c = g[Random.Range(0, g.Length)];
        return c;
    }

    private AudioClip[] GrupoDeLaSuperficie()
    {
        switch (superficie)
        {
            case 1:  return (pasosPasto  != null && pasosPasto.Length  > 0) ? pasosPasto  : pasosVereda;
            case 2:  return (pasosArena  != null && pasosArena.Length  > 0) ? pasosArena  : pasosPasto;
            case 3:  return (pasosMadera != null && pasosMadera.Length > 0) ? pasosMadera : pasosVereda;
            case 4:  return (pasosMetal  != null && pasosMetal.Length  > 0) ? pasosMetal  : pasosVereda;
            default: return pasosVereda;
        }
    }

    /// <summary>
    /// Mira que hay bajo los pies. Se consulta tres veces por segundo y no en
    /// cada cuadro: un rayo por cuadro por un dato que casi nunca cambia es
    /// gasto puro, y entre dos pasos no da tiempo de cambiar de piso.
    /// </summary>
    private void MirarSuperficie()
    {
        if (Time.time < proximaSuperficie) return;
        proximaSuperficie = Time.time + 0.3f;

        RaycastHit h;
        if (!Physics.Raycast(transform.position + Vector3.up * 0.6f, Vector3.down, out h,
                             2.4f, ~0, QueryTriggerInteraction.Ignore))
        { superficie = 0; return; }

        string n = h.collider != null ? h.collider.name : "";
        if (h.collider != null && h.collider.transform.parent != null)
            n += " " + h.collider.transform.parent.name;
        n = n.ToLowerInvariant();

        if (n.Contains("arena") || n.Contains("orilla") || n.Contains("sand")
            || n.Contains("playa") || n.Contains("cauce"))                      superficie = 2;
        else if (n.Contains("puente") || n.Contains("madera") || n.Contains("wood")
            || n.Contains("bridge") || n.Contains("tarima") || n.Contains("deck")) superficie = 3;
        else if (n.Contains("rejilla") || n.Contains("metal") || n.Contains("tapa")
            || n.Contains("alcantarilla"))                                      superficie = 4;
        else if (n.Contains("pasto") || n.Contains("grass") || n.Contains("cesped")
            || n.Contains("ribera") || n.Contains("cerro") || n.Contains("suelovalle")
            || n.Contains("parque") || n.Contains("jardin") || n.Contains("vegetacion")
            || n.Contains("nature") || n.Contains("green"))                     superficie = 1;
        else                                                                    superficie = 0;
    }

    /// <summary>Golpe seco al caer de un salto: le da peso al personaje.</summary>
    private void Aterrizaje()
    {
        bool aire = (cc != null) && !cc.isGrounded;
        if (enElAireAntes && !aire && pasos != null)
        {
            AudioClip c = UnoDe(clipsCaida);
            if (c == null) c = UnoDe(GrupoDeLaSuperficie());
            if (c != null)
            {
                pasos.pitch = Random.Range(0.86f, 0.98f);
                pasos.PlayOneShot(c, 0.85f);
            }
        }
        enElAireAntes = aire;
    }

    private static AudioClip _paso;

    private static AudioClip ClipPaso()
    {
        if (_paso != null) return _paso;

        int sr = 44100;
        int len = Mathf.RoundToInt(sr * 0.11f);
        float[] d = new float[len];

        float grave = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / sr;
            float blanco = Random.value * 2f - 1f;

            grave = grave * 0.72f + blanco * 0.28f;                 // ruido filtrado = suela
            float golpe = Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Exp(-t * 55f);

            d[i] = (grave * 0.55f + golpe * 0.45f) * Mathf.Exp(-t * 28f);
        }

        _paso = AudioClip.Create("paso", len, 1, sr, false);
        _paso.SetData(d, 0);
        return _paso;
    }

    private Vector2 LeerInput()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 v = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
        }
        var gp = Gamepad.current;
        if (gp != null) v += gp.leftStick.ReadValue();
        v += GuardianMovil.Mover;                              // joystick táctil (celular)
        return Vector2.ClampMagnitude(v, 1f);
#else
        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
#endif
    }

    private bool SaltoPresionado()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        return (kb != null && kb.spaceKey.wasPressedThisFrame)
            || (gp != null && gp.buttonSouth.wasPressedThisFrame)
            || GuardianMovil.Salto;
#else
        return Input.GetButtonDown("Jump");
#endif
    }

    private bool CorrerPresionado()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        return (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed))
            || (gp != null && gp.leftStickButton.isPressed)
            || GuardianMovil.Correr;
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }
}
