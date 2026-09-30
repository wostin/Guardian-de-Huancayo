using UnityEngine;

/// <summary>
/// Guardián de Huancayo - PROTOTIPO AUTOMÁTICO (demo jugable).
/// Al presionar PLAY arma solo un mini-nivel: piso, el Guardián (capsula verde),
/// basura recolectable, la cámara que lo sigue y un HUD con el puntaje.
/// NO necesitas arrastrar nada: solo presiona Play y muévete con WASD.
///
/// Se ejecuta automáticamente gracias a [RuntimeInitializeOnLoadMethod].
/// PARA DESACTIVARLO: pon ACTIVO = false (abajo) o borra este archivo cuando
/// ya tengas armada tu escena real.
/// </summary>
public class GuardianAutoSetup : MonoBehaviour
{
    // ---- Configuración rápida ----
    private const bool ACTIVO = false;  // pon false para apagar el prototipo
    private const int CANTIDAD_BASURA = 12;
    private const float RADIO_MAPA = 20f;

    private Transform jugador;
    private Transform camara;
    private readonly Vector3 offsetCam = new Vector3(0f, 10f, -10f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Arrancar()
    {
        if (!ACTIVO) return;

        // Evita duplicar si ya existe un jugador armado en la escena.
        if (GameObject.FindGameObjectWithTag("Player") != null) return;

        GameObject go = new GameObject("GuardianAuto");
        go.AddComponent<GuardianAutoSetup>();
    }

    void Start()
    {
        ConstruirPiso();
        ConstruirJugador();
        ConstruirGameManager();
        ConstruirBasura();
        PrepararCamara();
        AsegurarLuz();
    }

    // ---------- Construcción ----------

    private Material NuevoMaterial(Color c)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        return m;
    }

    private void ConstruirPiso()
    {
        GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Plane);
        piso.name = "Piso_Guardian";
        piso.transform.localScale = new Vector3(RADIO_MAPA / 2.5f, 1f, RADIO_MAPA / 2.5f);
        piso.transform.position = Vector3.zero;
        piso.GetComponent<Renderer>().material = NuevoMaterial(new Color(0.30f, 0.55f, 0.30f));
    }

    private void ConstruirJugador()
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        p.name = "Guardian";
        // Quitar la capsula collider por defecto: usaremos CharacterController.
        Destroy(p.GetComponent<CapsuleCollider>());
        p.transform.position = new Vector3(0f, 1.1f, 0f);
        p.GetComponent<Renderer>().material = NuevoMaterial(new Color(0.10f, 0.75f, 0.35f));

        CharacterController cc = p.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0f, 0f);
        cc.height = 2f;
        cc.radius = 0.5f;

        p.AddComponent<PlayerController>();
        p.tag = "Player";

        jugador = p.transform;
    }

    private void ConstruirGameManager()
    {
        GameObject gm = new GameObject("GameManager");
        GameManager comp = gm.AddComponent<GameManager>();
        comp.basuraTotal = CANTIDAD_BASURA;
    }

    private void ConstruirBasura()
    {
        Material matBasura = NuevoMaterial(new Color(0.85f, 0.35f, 0.10f));

        for (int i = 0; i < CANTIDAD_BASURA; i++)
        {
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = "Basura_" + i;
            b.transform.localScale = Vector3.one * 0.7f;

            Vector2 r = Random.insideUnitCircle * RADIO_MAPA;
            b.transform.position = new Vector3(r.x, 0.5f, r.y);
            b.GetComponent<Renderer>().material = matBasura;

            // Collider como trigger + Rigidbody kinematico => detecta al jugador seguro.
            BoxCollider col = b.GetComponent<BoxCollider>();
            col.isTrigger = true;
            Rigidbody rb = b.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            TrashItem t = b.AddComponent<TrashItem>();
            t.valor = 10;
        }
    }

    private void PrepararCamara()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject cGo = new GameObject("Main Camera");
            cGo.tag = "MainCamera";
            cam = cGo.AddComponent<Camera>();
            cGo.AddComponent<AudioListener>();
        }
        camara = cam.transform;
    }

    private void AsegurarLuz()
    {
        if (FindObjectOfType<Light>() == null)
        {
            GameObject l = new GameObject("Sol");
            Light luz = l.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;
            l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }

    // ---------- Cámara que sigue ----------

    void LateUpdate()
    {
        if (jugador == null || camara == null) return;
        Vector3 objetivo = jugador.position + offsetCam;
        camara.position = Vector3.Lerp(camara.position, objetivo, 8f * Time.deltaTime);
        camara.LookAt(jugador.position + Vector3.up * 0.5f);
    }

    // ---------- HUD sin dependencias (siempre visible) ----------

    void OnGUI()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.fontSize = 22;
        estilo.fontStyle = FontStyle.Bold;
        estilo.normal.textColor = Color.white;

        GUI.Label(new Rect(20, 15, 400, 30), "Puntaje: " + gm.puntaje, estilo);
        GUI.Label(new Rect(20, 45, 400, 30),
            "Recicladas: " + gm.recicladas + " / " + gm.basuraTotal, estilo);

        if (gm.recicladas >= gm.basuraTotal)
        {
            GUIStyle victoria = new GUIStyle(estilo);
            victoria.fontSize = 40;
            victoria.alignment = TextAnchor.MiddleCenter;
            victoria.normal.textColor = new Color(0.3f, 1f, 0.4f);
            GUI.Label(new Rect(0, Screen.height / 2f - 40, Screen.width, 80),
                "¡ZONA LIMPIA! Huancayo te lo agradece", victoria);
        }
    }
}
