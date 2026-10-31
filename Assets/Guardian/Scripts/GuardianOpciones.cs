using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Guardián de Huancayo - OPCIONES (ajustes y accesibilidad).
///
/// Ventana con los controles típicos de un menú de configuración —lo mismo que
/// se practicó en la guía de UI del curso: interruptores (Toggle) y deslizadores
/// (Slider) conectados a lo que controlan—, aplicados al juego:
///
///  · Música ON/OFF y volumen de la música  → bus "Música" del mezclador
///  · Efectos de sonido                     → bus "Efectos 3D"
///  · Brillo                                → exposición del post-proceso (URP)
///  · Sensibilidad del mouse e invertir eje → cámara del Guardián
///  · MODO DALTÓNICO: el juego entero se basa en distinguir cinco colores, y
///    alrededor del 8 % de los varones confunde el verde con el marrón o el
///    rojo. Con este modo, cada contenedor y cada residuo cercano muestran su
///    PICTOGRAMA y el nombre del material: ya no hace falta ver el color.
///
/// Se abre desde el menú principal (botón OPCIONES) y desde la pausa.
/// Todo se guarda entre partidas.
/// </summary>
public class GuardianOpciones : MonoBehaviour
{
    public static GuardianOpciones Instancia { get; private set; }
    private const string MODAL = "opciones";

    private const string K_BRILLO = "guardian_op_brillo";
    private const string K_SENS = "guardian_op_sens";
    private const string K_INV = "guardian_op_invertir";
    private const string K_DALT = "guardian_op_daltonico";
    private const string K_MUS_ON = "guardian_op_musica_on";
    private const string K_MUS_PREV = "guardian_op_musica_prev";

    // Exposición que ya usa GuardianIluminacion: el brillo se suma sobre ella.
    private const float EXPOSICION_BASE = 0.25f;

    public static bool Daltonico => PlayerPrefs.GetInt(K_DALT, 0) == 1;

    private bool abierta;
    private float brillo = 1f, sens = 1f;
    private bool invertir, daltonico, musicaOn = true;

    private GuardianCameraHUD hud;
    private float sensBase = -1f, sensBaseV = -1f;
    private Volume volBrillo;
    private ColorAdjustments ajuste;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (Instancia != null) return;
        if (GameObject.FindGameObjectWithTag("Player") == null) return;
        new GameObject("GuardianOpciones").AddComponent<GuardianOpciones>();
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
        brillo = Mathf.Clamp(PlayerPrefs.GetFloat(K_BRILLO, 1f), 0.6f, 1.5f);
        sens = Mathf.Clamp(PlayerPrefs.GetFloat(K_SENS, 1f), 0.4f, 2.2f);
        invertir = PlayerPrefs.GetInt(K_INV, 0) == 1;
        daltonico = PlayerPrefs.GetInt(K_DALT, 0) == 1;
        musicaOn = PlayerPrefs.GetInt(K_MUS_ON, 1) == 1;
    }

    void Start()
    {
        AplicarBrillo();
        if (!musicaOn) GuardianMezcla.FijarVolumen(GuardianMezcla.Bus.Musica, 0f);
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
        GuardianUIExtra.Cerrar(MODAL);
    }

    void Update()
    {
        // La cámara se crea sola al dar Play; se busca hasta encontrarla.
        if (hud == null)
        {
            hud = FindAnyObjectByType<GuardianCameraHUD>();
            if (hud != null && sensBase < 0f) { sensBase = hud.sensibilidadMouse; sensBaseV = hud.sensibilidadVertical; }
            if (hud != null) AplicarCamara();
        }

        // Si la pausa se cerró (Esc / P) mientras la ventana estaba abierta, se cierra también.
        if (abierta)
        {
            GameManager gm = GameManager.Instance;
            bool valido = gm != null && (gm.pausado || gm.estado == GameManager.Estado.Menu);
            if (!valido) CerrarVentana();
        }
    }

    // ------------------------------------------------------------ aplicar

    private void AplicarBrillo()
    {
        // Brillo 1 = como se diseñó: no hace falta crear nada.
        if (volBrillo == null && Mathf.Abs(brillo - 1f) < 0.01f) return;
        if (volBrillo == null)
        {
            GameObject go = new GameObject("Opciones_Brillo");
            go.transform.SetParent(transform, false);
            volBrillo = go.AddComponent<Volume>();
            volBrillo.isGlobal = true;
            volBrillo.priority = 50f;                         // por encima del post-proceso base (10)
            VolumeProfile perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            volBrillo.profile = perfil;
            // false: SOLO se sobrescribe la exposición. Si se sobrescribiera todo,
            // se perdería la saturación que baja con la contaminación.
            ajuste = perfil.Add<ColorAdjustments>(false);
        }
        if (ajuste != null) ajuste.postExposure.Override(EXPOSICION_BASE + Mathf.Log(brillo, 2f));
    }

    private void AplicarCamara()
    {
        if (hud == null || sensBase < 0f) return;
        hud.sensibilidadMouse = sensBase * sens;
        hud.sensibilidadVertical = sensBaseV * sens;
        hud.invertirVertical = invertir;
    }

    private void FijarMusica(bool on)
    {
        if (on == musicaOn) return;
        musicaOn = on;
        PlayerPrefs.SetInt(K_MUS_ON, on ? 1 : 0);
        if (!on)
        {
            float v = GuardianMezcla.Volumen(GuardianMezcla.Bus.Musica);
            if (v > 0.02f) PlayerPrefs.SetFloat(K_MUS_PREV, v);
            GuardianMezcla.FijarVolumen(GuardianMezcla.Bus.Musica, 0f);
        }
        else
        {
            GuardianMezcla.FijarVolumen(GuardianMezcla.Bus.Musica, Mathf.Max(0.1f, PlayerPrefs.GetFloat(K_MUS_PREV, 0.8f)));
        }
    }

    private void Restablecer()
    {
        brillo = 1f; sens = 1f; invertir = false; daltonico = false;
        PlayerPrefs.SetFloat(K_BRILLO, brillo);
        PlayerPrefs.SetFloat(K_SENS, sens);
        PlayerPrefs.SetInt(K_INV, 0);
        PlayerPrefs.SetInt(K_DALT, 0);
        FijarMusica(true);
        GuardianMezcla.FijarVolumen(GuardianMezcla.Bus.Musica, 0.8f);
        GuardianMezcla.FijarVolumen(GuardianMezcla.Bus.Efectos, 1f);
        AplicarBrillo();
        AplicarCamara();
    }

    // ------------------------------------------------------------ ventana

    public static void Abrir()
    {
        if (Instancia == null || !GuardianUIExtra.Abrir(MODAL)) return;
        Instancia.abierta = true;
    }

    private void CerrarVentana()
    {
        abierta = false;
        PlayerPrefs.Save();
        GuardianUIExtra.Cerrar(MODAL);
    }

    void OnGUI()
    {
        GUI.depth = -31;
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        GuardianUIExtra.Preparar();

        if (abierta) { Ventana(); return; }

        if (!GuardianUIExtra.HayModal)
        {
            if (GuardianUIExtra.EnMenuPrincipal())
            {
                if (GuardianUIExtra.Boton(GuardianUIExtra.RanuraMenu(2), "OPCIONES  ·  sonido, brillo, accesibilidad",
                                          GuardianIconos.Pictograma(TipoResiduo.Papel), false, 13, new Color(0.45f, 0.70f, 1f)))
                    Abrir();
            }
            else if (gm.pausado)
            {
                if (GuardianUIExtra.Boton(new Rect(18f, 18f, 230f, 46f), "OPCIONES", GuardianIconos.Pictograma(TipoResiduo.Papel),
                                          false, 15, new Color(0.45f, 0.70f, 1f)))
                    Abrir();
            }
        }

        if (daltonico) DibujarDaltonico(gm);
    }

    private void Ventana()
    {
        if (GuardianUIExtra.Tecla(KeyCode.Escape)) { CerrarVentana(); Event.current.Use(); return; }

        GuardianUIExtra.Velo(0.80f);
        Rect card = GuardianUIExtra.Tarjeta(780f, 620f, new Color(0.45f, 0.70f, 1f));
        GUI.Label(new Rect(card.x, card.y + 14f, card.width, 42f), "OPCIONES",
                  GuardianUIExtra.Txt(30, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));

        float y = card.y + 70f;
        const float alto = 58f;

        // --- Sonido ---
        Seccion(card, ref y, "SONIDO");
        bool nm = Fila(card, ref y, alto, "Música", "Encender o apagar la música del juego", musicaOn, true, out bool cambioMusica);
        if (cambioMusica) FijarMusica(nm);

        float vm = GuardianMezcla.Volumen(GuardianMezcla.Bus.Musica);
        float nvm = Deslizador(card, ref y, alto, "Volumen de la música", vm, 0f, 1f, Mathf.RoundToInt(vm * 100f) + " %");
        if (Mathf.Abs(nvm - vm) > 0.001f)
        {
            GuardianMezcla.FijarVolumen(GuardianMezcla.Bus.Musica, nvm);
            if (nvm > 0.01f && !musicaOn) { musicaOn = true; PlayerPrefs.SetInt(K_MUS_ON, 1); }
        }

        float ve = GuardianMezcla.Volumen(GuardianMezcla.Bus.Efectos);
        float nve = Deslizador(card, ref y, alto, "Efectos de sonido", ve, 0f, 1f, Mathf.RoundToInt(ve * 100f) + " %");
        if (Mathf.Abs(nve - ve) > 0.001f) GuardianMezcla.FijarVolumen(GuardianMezcla.Bus.Efectos, nve);

        // --- Imagen y control ---
        Seccion(card, ref y, "IMAGEN Y CONTROL");
        float nb = Deslizador(card, ref y, alto, "Brillo", brillo, 0.6f, 1.5f, Mathf.RoundToInt(brillo * 100f) + " %");
        if (Mathf.Abs(nb - brillo) > 0.001f) { brillo = nb; PlayerPrefs.SetFloat(K_BRILLO, brillo); AplicarBrillo(); }

        float ns = Deslizador(card, ref y, alto, "Sensibilidad del mouse", sens, 0.4f, 2.2f, "x" + sens.ToString("0.0"));
        if (Mathf.Abs(ns - sens) > 0.001f) { sens = ns; PlayerPrefs.SetFloat(K_SENS, sens); AplicarCamara(); }

        bool ninv = Fila(card, ref y, alto, "Invertir mirada vertical", "Subir el mouse baja la vista", invertir, false, out bool cambioInv);
        if (cambioInv) { invertir = ninv; PlayerPrefs.SetInt(K_INV, invertir ? 1 : 0); AplicarCamara(); }

        // --- Accesibilidad ---
        Seccion(card, ref y, "ACCESIBILIDAD");
        bool nd = Fila(card, ref y, alto, "Modo daltónico", "Pictograma y nombre sobre cada contenedor y residuo", daltonico, false, out bool cambioDal);
        if (cambioDal) { daltonico = nd; PlayerPrefs.SetInt(K_DALT, daltonico ? 1 : 0); }
        // Vista previa: así se ven los contenedores con el modo activo.
        if (daltonico)
        {
            float px = card.x + 40f;
            for (int i = 0; i < Residuo.TIPOS; i++)
            {
                Rect r = new Rect(px + i * 140f, y - 4f, 132f, 34f);
                Etiqueta(r, Residuo.Desde(i), 1f);
            }
        }

        if (GuardianUIExtra.Boton(new Rect(card.x + card.width / 2f - 250f, card.yMax - 70f, 240f, 50f), "↺  RESTABLECER", null, false, 16))
            Restablecer();
        if (GuardianUIExtra.Boton(new Rect(card.x + card.width / 2f + 10f, card.yMax - 70f, 240f, 50f), "◀  LISTO", null, true, 16,
                                  new Color(0.45f, 0.70f, 1f)))
            CerrarVentana();

        GuardianUIExtra.Tapar();
    }

    private void Seccion(Rect card, ref float y, string titulo)
    {
        GUI.Label(new Rect(card.x + 40f, y, card.width - 80f, 22f), titulo,
                  GuardianUIExtra.Txt(13, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleLeft));
        Color antes = GUI.color; GUI.color = new Color(1f, 0.85f, 0.4f, 0.35f);
        GUI.DrawTexture(new Rect(card.x + 40f, y + 22f, card.width - 80f, 1f), Texture2D.whiteTexture);
        GUI.color = antes;
        y += 28f;
    }

    /// <summary>Fila con un interruptor ON/OFF. Devuelve el valor nuevo y si cambió.</summary>
    private bool Fila(Rect card, ref float y, float alto, string titulo, string ayuda, bool valor, bool grande, out bool cambio)
    {
        float h = alto - 12f;
        GUI.Label(new Rect(card.x + 40f, y, 380f, 24f), titulo,
                  GuardianUIExtra.Txt(17, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft));
        GUI.Label(new Rect(card.x + 40f, y + 22f, 400f, 20f), ayuda,
                  GuardianUIExtra.Txt(12, FontStyle.Normal, new Color(0.68f, 0.74f, 0.80f), TextAnchor.MiddleLeft));
        bool nuevo = GuardianUIExtra.Interruptor(new Rect(card.xMax - 40f - 96f, y + 4f, 96f, h - 6f), valor);
        cambio = nuevo != valor;
        y += h;
        return nuevo;
    }

    private float Deslizador(Rect card, ref float y, float alto, string titulo, float valor, float min, float max, string texto)
    {
        float h = alto - 18f;
        GUI.Label(new Rect(card.x + 40f, y, 300f, h), titulo,
                  GuardianUIExtra.Txt(17, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft));
        float nuevo = GUI.HorizontalSlider(new Rect(card.x + 360f, y + h / 2f - 6f, card.width - 360f - 140f, 16f), valor, min, max);
        GUI.Label(new Rect(card.xMax - 130f, y, 90f, h), texto,
                  GuardianUIExtra.Txt(16, FontStyle.Bold, new Color(0.80f, 0.90f, 1f), TextAnchor.MiddleRight));
        y += h;
        return nuevo;
    }

    // ------------------------------------------------------------ modo daltónico

    private RecycleBin[] botes;
    private TrashItem[] residuos;
    private float proximoRefresco;

    private void DibujarDaltonico(GameManager gm)
    {
        if (gm.estado != GameManager.Estado.Jugando || gm.pausado || GuardianConversacion.Hablando) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        if (Time.unscaledTime >= proximoRefresco)
        {
            proximoRefresco = Time.unscaledTime + 1.5f;
            botes = FindObjectsByType<RecycleBin>(FindObjectsSortMode.None);
            residuos = FindObjectsByType<TrashItem>(FindObjectsSortMode.None);
        }

        Vector3 ojo = cam.transform.position;
        Vector2 p;

        if (residuos != null)
        {
            for (int i = 0; i < residuos.Length; i++)
            {
                TrashItem t = residuos[i];
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                float d = Vector3.Distance(ojo, t.transform.position);
                if (d > 32f) continue;
                if (!GuardianUIExtra.APantalla(cam, t.transform.position + Vector3.up * 0.9f, out p)) continue;
                float lado = Mathf.Lerp(34f, 20f, d / 32f);
                Color antes = GUI.color;
                GUI.color = new Color(0.05f, 0.07f, 0.10f, 0.80f);
                GUI.DrawTexture(new Rect(p.x - lado / 2f - 3f, p.y - lado / 2f - 3f, lado + 6f, lado + 6f), GuardianIconos.Punto());
                GUI.color = antes;
                GUI.DrawTexture(new Rect(p.x - lado / 2f, p.y - lado / 2f, lado, lado), GuardianIconos.Pictograma(t.tipo));
            }
        }

        if (botes != null)
        {
            for (int i = 0; i < botes.Length; i++)
            {
                RecycleBin b = botes[i];
                if (b == null || !b.gameObject.activeInHierarchy) continue;
                float d = Vector3.Distance(ojo, b.transform.position);
                if (d > 95f) continue;
                if (!GuardianUIExtra.APantalla(cam, b.transform.position + Vector3.up * 3.0f, out p)) continue;
                float k = Mathf.Lerp(1f, 0.62f, d / 95f);
                Etiqueta(new Rect(p.x - 70f * k, p.y - 40f * k, 140f * k, 40f * k), b.acepta, k);
            }
        }
    }

    /// <summary>Cartel del modo daltónico: pictograma + color + material.</summary>
    private static void Etiqueta(Rect r, TipoResiduo t, float k)
    {
        GuardianUIExtra.Caja(r, new Color(0.05f, 0.07f, 0.10f, 0.86f), 8);
        Color antes = GUI.color;
        GUI.color = Residuo.Tinte(t);
        GUI.DrawTexture(new Rect(r.x, r.y + 3f, 5f * k, r.height - 6f), Texture2D.whiteTexture);
        GUI.color = antes;
        float lado = r.height - 6f;
        GUI.DrawTexture(new Rect(r.x + 8f * k, r.y + 3f, lado, lado), GuardianIconos.Pictograma(t));
        float tx = r.x + 10f * k + lado;
        GUI.Label(new Rect(tx, r.y + 1f, r.xMax - tx - 4f, r.height * 0.5f), Residuo.ColorNTP(t),
                  GuardianUIExtra.Txt(Mathf.RoundToInt(12f * k), FontStyle.Bold, Color.white, TextAnchor.LowerLeft));
        GUI.Label(new Rect(tx, r.y + r.height * 0.5f - 1f, r.xMax - tx - 4f, r.height * 0.5f), Residuo.Nombre(t),
                  GuardianUIExtra.Txt(Mathf.RoundToInt(10f * k), FontStyle.Normal, new Color(0.80f, 0.85f, 0.92f), TextAnchor.UpperLeft));
    }
}
