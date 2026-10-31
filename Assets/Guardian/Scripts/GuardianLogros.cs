using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guardián de Huancayo - INSIGNIAS (logros).
///
/// 16 medallas que premian justo lo que el juego quiere enseñar: segregar bien
/// (rachas, cero errores, los cinco colores), cuidar la ciudad (alcanzar al
/// vecino que bota basura, cruzar sin que te atropellen, usar la bicicleta),
/// conversar con la gente y terminar las tres zonas.
///
/// - Al ganar una aparece una tarjeta con la MEDALLA (imagen, no un texto largo).
/// - En el menú principal, el botón "INSIGNIAS" abre el álbum con todas: las que
///   faltan salen en gris con la pista de cómo conseguirlas.
/// - Se guardan entre partidas (PlayerPrefs).
///
/// Escucha los eventos del juego (GuardianEventos, patrón Observer) y no toca
/// la lógica de puntaje: si se borra este archivo el juego sigue igual.
/// </summary>
public class GuardianLogros : MonoBehaviour
{
    public static GuardianLogros Instancia { get; private set; }

    private const string MODAL = "insignias";

    // ------------------------------------------------------------ catálogo
    // El índice de cada insignia es también el índice de su dibujo en GuardianIconos.

    public const int PRIMER_RESIDUO = 0, BUEN_OJO = 1, RACHA_5 = 2, IMPARABLE = 3, PERFECTA = 4,
                     PLAZA = 5, MERCADO = 6, SHULLCAS = 7, CINCO_COLORES = 8, VECINO = 9,
                     CHARLA = 10, BICI = 11, PEATON = 12, REY = 13, QUIZ = 14, CIEN = 15;

    private static readonly string[] TITULO =
    {
        "PRIMER RESIDUO", "BUEN OJO", "RACHA x5", "IMPARABLE",
        "SEGREGACIÓN PERFECTA", "PLAZA LIMPIA", "MERCADO LIMPIO", "SHULLCAS VIVO",
        "LOS 5 COLORES", "VECINO CONSCIENTE", "BUENA CHARLA", "MOVILIDAD SOSTENIBLE",
        "PEATÓN SEGURO", "REY DESTRONADO", "MAESTRO DE LA NTP", "100 RESIDUOS",
    };

    private static readonly string[] COMO =
    {
        "Recoge tu primer residuo",
        "10 residuos bien segregados",
        "5 aciertos seguidos",
        "12 aciertos seguidos",
        "Gana una zona sin errores",
        "Limpia la Plaza Constitución",
        "Limpia el Mercado Mayorista",
        "Limpia la ribera del Shullcas",
        "Segrega los 5 colores de la NTP",
        "Alcanza a un vecino que botó basura",
        "Conversa con 5 personas",
        "Recorre la ciudad en bicicleta",
        "Gana una zona con tráfico sin golpes",
        "Derrota al Rey Basurón",
        "10 de 10 en el Reto NTP",
        "100 residuos bien segregados en total",
    };

    private static readonly Color[] COLOR =
    {
        new Color(0.25f, 0.60f, 0.90f), new Color(0.20f, 0.65f, 0.45f), new Color(0.95f, 0.45f, 0.20f), new Color(0.55f, 0.35f, 0.85f),
        new Color(0.85f, 0.62f, 0.12f), new Color(0.75f, 0.25f, 0.25f), new Color(0.90f, 0.50f, 0.15f), new Color(0.15f, 0.55f, 0.75f),
        new Color(0.30f, 0.40f, 0.55f), new Color(0.80f, 0.30f, 0.45f), new Color(0.25f, 0.55f, 0.85f), new Color(0.20f, 0.62f, 0.35f),
        new Color(0.35f, 0.38f, 0.45f), new Color(0.60f, 0.25f, 0.70f), new Color(0.95f, 0.60f, 0.10f), new Color(0.80f, 0.55f, 0.10f),
    };

    public static int Total => TITULO.Length;
    public static string Titulo(int id) { return TITULO[Mathf.Clamp(id, 0, TITULO.Length - 1)]; }
    public static string Como(int id) { return COMO[Mathf.Clamp(id, 0, COMO.Length - 1)]; }

    public static Texture2D Medalla(int id, bool ganada)
    {
        id = Mathf.Clamp(id, 0, Total - 1);
        return GuardianIconos.Insignia(id, COLOR[id], ganada);
    }

    // ------------------------------------------------------------ guardado

    private const string K_LOGRO = "guardian_ins_";
    private const string K_TOTAL = "guardian_ins_total";
    private const string K_CHARLAS = "guardian_ins_charlas";
    private const string K_TIPOS = "guardian_ins_tipos";

    public static bool Tiene(int id) { return PlayerPrefs.GetInt(K_LOGRO + id, 0) == 1; }

    public static int Cantidad
    {
        get { int n = 0; for (int i = 0; i < Total; i++) if (Tiene(i)) n++; return n; }
    }

    /// <summary>Gana la insignia (si no la tenía) y la anuncia con su medalla.</summary>
    public static void Desbloquear(int id)
    {
        if (id < 0 || id >= Total || Tiene(id)) return;
        PlayerPrefs.SetInt(K_LOGRO + id, 1);
        PlayerPrefs.Save();
        if (Instancia != null) Instancia.cola.Add(id);
    }

    // ------------------------------------------------------------ arranque

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (Instancia != null) return;
        if (GameObject.FindGameObjectWithTag("Player") == null) return;
        new GameObject("GuardianLogros").AddComponent<GuardianLogros>();
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
    }

    void OnEnable()
    {
        GuardianEventos.Recogio += AlRecoger;
        GuardianEventos.Deposito += AlDepositar;
        GuardianEventos.Golpe += AlGolpe;
        GuardianEventos.FinNivel += AlTerminar;
        GuardianEventos.VecinoAlcanzado += AlAlcanzarVecino;
    }

    void OnDisable()
    {
        GuardianEventos.Recogio -= AlRecoger;
        GuardianEventos.Deposito -= AlDepositar;
        GuardianEventos.Golpe -= AlGolpe;
        GuardianEventos.FinNivel -= AlTerminar;
        GuardianEventos.VecinoAlcanzado -= AlAlcanzarVecino;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
        GuardianUIExtra.Cerrar(MODAL);
    }

    // ------------------------------------------------------------ reglas

    private int golpesNivel;
    private bool estabaJugando;
    private bool hablandoAntes;

    private void AlRecoger(Vector3 p, TipoResiduo t) { Desbloquear(PRIMER_RESIDUO); }

    private void AlDepositar(Vector3 p, TipoResiduo t, int n, int pts)
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.modoRecorrido) return;      // en el recorrido no se compite

        int total = PlayerPrefs.GetInt(K_TOTAL, 0) + Mathf.Max(1, n);
        PlayerPrefs.SetInt(K_TOTAL, total);
        int tipos = PlayerPrefs.GetInt(K_TIPOS, 0) | (1 << (int)t);
        PlayerPrefs.SetInt(K_TIPOS, tipos);

        if (total >= 10) Desbloquear(BUEN_OJO);
        if (total >= 100) Desbloquear(CIEN);
        if (tipos == (1 << Residuo.TIPOS) - 1) Desbloquear(CINCO_COLORES);

        if (gm != null)
        {
            if (gm.racha >= 5) Desbloquear(RACHA_5);
            if (gm.racha >= 12) Desbloquear(IMPARABLE);
        }
    }

    private void AlGolpe(Vector3 p) { golpesNivel++; }

    private void AlAlcanzarVecino(Vector3 p) { Desbloquear(VECINO); }

    private void AlTerminar(bool gano)
    {
        GameManager gm = GameManager.Instance;
        if (!gano || gm == null || gm.modoRecorrido) return;

        if (gm.zonaActual == 0) Desbloquear(PLAZA);
        else if (gm.zonaActual == 1) Desbloquear(MERCADO);
        else if (gm.zonaActual == 2) Desbloquear(SHULLCAS);

        if (gm.Perfecta) Desbloquear(PERFECTA);
        if (!gm.ZonaPeatonal && golpesNivel == 0 && gm.vidas >= gm.maxVidas) Desbloquear(PEATON);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        bool jugando = gm.estado == GameManager.Estado.Jugando;
        if (jugando && !estabaJugando) golpesNivel = 0;       // empezó (o se reinició) un nivel
        estabaJugando = jugando;
        if (!jugando || gm.modoRecorrido) { hablandoAntes = GuardianConversacion.Hablando; return; }

        // Conversaciones: cada vez que se abre un diálogo nuevo cuenta una.
        bool hablando = GuardianConversacion.Hablando;
        if (hablando && !hablandoAntes)
        {
            int charlas = PlayerPrefs.GetInt(K_CHARLAS, 0) + 1;
            PlayerPrefs.SetInt(K_CHARLAS, charlas);
            if (charlas >= 5) Desbloquear(CHARLA);
        }
        hablandoAntes = hablando;

        if (Bicicleta.EnUso != null) Desbloquear(BICI);

        GuardianEnemigos en = GuardianEnemigos.Instancia;
        if (en != null && en.Jefe != null && en.Jefe.Muerto) Desbloquear(REY);
    }

    // ------------------------------------------------------------ tarjeta de "insignia ganada"

    private readonly List<int> cola = new List<int>();
    private int mostrando = -1;
    private float desde;
    private const float DURACION = 4.6f;

    private void ActualizarTarjeta()
    {
        GameManager gm = GameManager.Instance;
        bool puede = gm != null && !gm.pausado && !GuardianConversacion.Hablando;

        if (mostrando >= 0 && Time.unscaledTime - desde > DURACION) mostrando = -1;
        if (mostrando < 0 && cola.Count > 0 && puede)
        {
            mostrando = cola[0];
            cola.RemoveAt(0);
            desde = Time.unscaledTime;
            GuardianAudio.EnPantalla(GuardianAudio.Bonus, 0.55f, 1.05f, 1.12f);
        }
    }

    private void DibujarTarjeta()
    {
        if (mostrando < 0) return;
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.pausado || GuardianConversacion.Hablando) return;

        float t = Time.unscaledTime - desde;
        float entra = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.35f));
        float sale = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((DURACION - t) / 0.35f));
        float k = Mathf.Min(entra, sale);

        float w = 400f, h = 86f;
        float x = GuardianUIExtra.SW / 2f - w / 2f;
        float y = Mathf.Lerp(-h - 10f, 70f, k);                 // baja desde arriba, debajo de la barra del jefe
        Rect r = new Rect(x, y, w, h);

        Color oro = new Color(1f, 0.82f, 0.32f);
        GuardianUIExtra.Caja(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), new Color(oro.r, oro.g, oro.b, 0.85f), 14);
        GuardianUIExtra.Caja(r, new Color(0.06f, 0.08f, 0.12f, 0.96f), 12);

        float latido = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f);
        float lado = 74f * latido;
        GUI.DrawTexture(new Rect(r.x + 8f + (74f - lado) / 2f, r.y + 6f + (74f - lado) / 2f, lado, lado), Medalla(mostrando, true));

        GUI.Label(new Rect(r.x + 92f, r.y + 8f, w - 100f, 20f), "¡INSIGNIA DESBLOQUEADA!",
                  GuardianUIExtra.Txt(12, FontStyle.Bold, oro, TextAnchor.MiddleLeft));
        GUI.Label(new Rect(r.x + 92f, r.y + 28f, w - 100f, 28f), Titulo(mostrando),
                  GuardianUIExtra.Txt(20, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft));
        GUI.Label(new Rect(r.x + 92f, r.y + 56f, w - 100f, 20f), Como(mostrando),
                  GuardianUIExtra.Txt(12, FontStyle.Normal, new Color(0.78f, 0.84f, 0.90f), TextAnchor.MiddleLeft));
    }

    // ------------------------------------------------------------ álbum

    private bool albumAbierto;
    private int seleccion = -1;

    public static bool AlbumAbierto => Instancia != null && Instancia.albumAbierto;

    public static void AbrirAlbum()
    {
        if (Instancia == null || !GuardianUIExtra.Abrir(MODAL)) return;
        Instancia.albumAbierto = true;
        Instancia.seleccion = -1;
    }

    private void CerrarAlbum()
    {
        albumAbierto = false;
        GuardianUIExtra.Cerrar(MODAL);
    }

    void OnGUI()
    {
        GUI.depth = -30;          // encima del HUD, debajo de las conversaciones
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        GuardianUIExtra.Preparar();

        if (Event.current.type == EventType.Layout) ActualizarTarjeta();

        if (albumAbierto)
        {
            if (!GuardianUIExtra.EnMenuPrincipal()) { CerrarAlbum(); return; }
            DibujarAlbum();
            return;
        }

        // Botón del álbum en el menú principal.
        if (GuardianUIExtra.EnMenuPrincipal() && !GuardianUIExtra.HayModal)
        {
            int n = Cantidad;
            if (GuardianUIExtra.Boton(GuardianUIExtra.RanuraMenu(1), "INSIGNIAS  " + n + " / " + Total,
                                      Medalla(n > 0 ? UltimaGanada() : 4, n > 0)))
                AbrirAlbum();
        }

        DibujarTarjeta();
    }

    private int UltimaGanada()
    {
        int u = 0;
        for (int i = 0; i < Total; i++) if (Tiene(i)) u = i;
        return u;
    }

    private void DibujarAlbum()
    {
        if (GuardianUIExtra.Tecla(KeyCode.Escape)) { CerrarAlbum(); Event.current.Use(); return; }

        GuardianUIExtra.Velo(0.80f);
        Rect card = GuardianUIExtra.Tarjeta(960f, 640f, new Color(1f, 0.82f, 0.32f));

        int ganadas = Cantidad;
        GUI.Label(new Rect(card.x, card.y + 14f, card.width, 40f), "ÁLBUM DE INSIGNIAS DEL GUARDIÁN",
                  GuardianUIExtra.Txt(28, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));

        // Barra de progreso
        Rect barra = new Rect(card.x + 260f, card.y + 60f, card.width - 520f, 14f);
        GuardianUIExtra.Caja(barra, new Color(0.22f, 0.26f, 0.32f), 6);
        if (ganadas > 0)
            GuardianUIExtra.Caja(new Rect(barra.x, barra.y, Mathf.Max(14f, barra.width * ganadas / Total), barra.height),
                                 new Color(1f, 0.80f, 0.30f), 6);
        GUI.Label(new Rect(card.x, barra.yMax + 2f, card.width, 22f), ganadas + " de " + Total + " insignias",
                  GuardianUIExtra.Txt(14, FontStyle.Bold, new Color(1f, 0.85f, 0.45f), TextAnchor.MiddleCenter));

        // Rejilla 4 x 4: la medalla es lo que manda; el texto es corto.
        const int columnas = 4;
        float cw = 222f, ch = 116f;
        float x0 = card.x + (card.width - columnas * cw) / 2f;
        float y0 = card.y + 108f;
        for (int i = 0; i < Total; i++)
        {
            int col = i % columnas, fila = i / columnas;
            Rect celda = new Rect(x0 + col * cw + 4f, y0 + fila * ch + 4f, cw - 8f, ch - 8f);
            bool ganada = Tiene(i);
            bool encima = celda.Contains(Event.current.mousePosition);
            GuardianUIExtra.Caja(celda, ganada ? (encima ? new Color(0.20f, 0.26f, 0.34f) : new Color(0.14f, 0.18f, 0.24f))
                                               : new Color(0.10f, 0.12f, 0.16f), 10);

            float lado = 82f + (encima ? 6f : 0f);
            Rect rm = new Rect(celda.x + 6f + (82f - lado) / 2f, celda.y + 8f + (82f - lado) / 2f, lado, lado);
            GUI.DrawTexture(rm, Medalla(i, ganada));
            if (!ganada) GUI.DrawTexture(new Rect(rm.xMax - 30f, rm.yMax - 30f, 28f, 28f), GuardianIconos.Candado());

            GUI.Label(new Rect(celda.x + 94f, celda.y + 10f, celda.width - 98f, 44f), Titulo(i),
                      GuardianUIExtra.Txt(14, FontStyle.Bold, ganada ? Color.white : new Color(0.62f, 0.66f, 0.72f), TextAnchor.UpperLeft, true));
            GUI.Label(new Rect(celda.x + 94f, celda.y + 54f, celda.width - 98f, 44f), Como(i),
                      GuardianUIExtra.Txt(12, FontStyle.Normal, ganada ? new Color(0.70f, 0.90f, 0.75f) : new Color(0.55f, 0.60f, 0.66f), TextAnchor.UpperLeft, true));
            if (ganada)
                GUI.DrawTexture(new Rect(celda.xMax - 24f, celda.y + 6f, 18f, 18f), GuardianIconos.Check());
        }

        if (GuardianUIExtra.Boton(new Rect(card.x + card.width / 2f - 110f, card.yMax - 62f, 220f, 46f), "◀  VOLVER"))
            CerrarAlbum();

        GuardianUIExtra.Tapar();
    }
}
