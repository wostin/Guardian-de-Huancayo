using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guardián de Huancayo - MISIONES DEL NIVEL.
///
/// Cada vez que empieza una zona aparecen DOS misiones cortas, con ícono, debajo
/// del panel del HUD. Son metas opcionales que dan puntos extra y cambian de
/// zona en zona, para que cada partida tenga algo distinto que buscar:
///
///  · "Segrega 3 de VIDRIO"      (siempre: el color que más hay en la zona)
///  · "Racha de 4"               (segregar bien varias veces seguidas)
///  · "Conversa con un vecino"   (siempre en la Plaza: enseña a usar los diálogos)
///  · "3 residuos en 25 s"       (recoger rápido)
///  · "Pedalea en bicicleta"     (transporte sostenible, meta 11.2 del ODS 11)
///  · "Limpia sin golpes"        (solo en zonas con tráfico: cruza por el crucero)
///
/// Se completan solas mientras se juega; al cumplirse suman puntos con el
/// mismo aviso del juego (GameManager.SumarPuntos). No cambian la lógica de la
/// partida: si se borra este archivo, el juego sigue igual.
/// </summary>
public class GuardianMisiones : MonoBehaviour
{
    public static GuardianMisiones Instancia { get; private set; }

    private enum Tipo { SegregarColor, Racha, Conversar, Rapido, Bici, SinGolpes }

    private class Mision
    {
        public Tipo tipo;
        public TipoResiduo color;
        public int meta = 1;
        public int avance;
        public int premio;
        public bool hecha, fallida;
        public float hechaEn;
    }

    private readonly List<Mision> misiones = new List<Mision>();
    private readonly List<float> recogidas = new List<float>();
    private bool estabaJugando;
    private float tiempoAntes;
    private int zonaAntes = -1;
    private bool hablandoAntes;
    private float inicioNivel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (Instancia != null) return;
        if (GameObject.FindGameObjectWithTag("Player") == null) return;
        new GameObject("GuardianMisiones").AddComponent<GuardianMisiones>();
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
    }

    void OnDestroy() { if (Instancia == this) Instancia = null; }

    void OnEnable()
    {
        GuardianEventos.Recogio += AlRecoger;
        GuardianEventos.Deposito += AlDepositar;
        GuardianEventos.Golpe += AlGolpe;
    }

    void OnDisable()
    {
        GuardianEventos.Recogio -= AlRecoger;
        GuardianEventos.Deposito -= AlDepositar;
        GuardianEventos.Golpe -= AlGolpe;
    }

    // ------------------------------------------------------------ elegir misiones

    private void Preparar(GameManager gm)
    {
        misiones.Clear();
        recogidas.Clear();
        inicioNivel = Time.unscaledTime;
        if (gm.modoRecorrido) return;

        // 1) Segregar el color que más abunda en la zona (así siempre se puede cumplir).
        int[] cuenta = new int[Residuo.TIPOS];
        foreach (TrashItem t in FindObjectsByType<TrashItem>(FindObjectsSortMode.None))
            if (t != null && t.gameObject.activeInHierarchy && !t.deReserva) cuenta[Mathf.Clamp((int)t.tipo, 0, Residuo.TIPOS - 1)]++;
        int mejor = 0;
        for (int i = 1; i < Residuo.TIPOS; i++) if (cuenta[i] > cuenta[mejor]) mejor = i;
        if (cuenta[mejor] >= 2)
            misiones.Add(new Mision { tipo = Tipo.SegregarColor, color = Residuo.Desde(mejor), meta = Mathf.Min(3, cuenta[mejor]), premio = 40 });

        // 2) Una segunda misión según la zona.
        List<Tipo> opciones = new List<Tipo> { Tipo.Racha, Tipo.Rapido };
        if (!gm.ZonaPeatonal) opciones.Add(Tipo.SinGolpes);
        if (FindObjectsByType<Bicicleta>(FindObjectsSortMode.None).Length > 0) opciones.Add(Tipo.Bici);
        opciones.Add(Tipo.Conversar);

        Tipo segunda = gm.zonaActual == 0 ? Tipo.Conversar : opciones[Random.Range(0, opciones.Count)];
        Mision m = new Mision { tipo = segunda };
        switch (segunda)
        {
            case Tipo.Racha:     m.meta = 4; m.premio = 40; break;
            case Tipo.Rapido:    m.meta = 3; m.premio = 35; break;
            case Tipo.SinGolpes: m.meta = 1; m.premio = 50; break;
            case Tipo.Bici:      m.meta = 1; m.premio = 25; break;
            default:             m.meta = 1; m.premio = 25; break;
        }
        misiones.Add(m);
    }

    // ------------------------------------------------------------ avance

    private void AlRecoger(Vector3 p, TipoResiduo t)
    {
        float ahora = Time.time;
        recogidas.Add(ahora);
        recogidas.RemoveAll(x => ahora - x > 25f);
        foreach (Mision m in misiones)
            if (m.tipo == Tipo.Rapido) Avanzar(m, Mathf.Max(m.avance, recogidas.Count));
    }

    private void AlDepositar(Vector3 p, TipoResiduo t, int n, int pts)
    {
        GameManager gm = GameManager.Instance;
        foreach (Mision m in misiones)
        {
            if (m.tipo == Tipo.SegregarColor && t == m.color) Avanzar(m, m.avance + Mathf.Max(1, n));
            if (m.tipo == Tipo.Racha && gm != null) Avanzar(m, Mathf.Max(m.avance, gm.racha));
        }
    }

    private void AlGolpe(Vector3 p)
    {
        foreach (Mision m in misiones)
            if (m.tipo == Tipo.SinGolpes && !m.hecha) m.fallida = true;
    }

    private void Avanzar(Mision m, int valor)
    {
        if (m.hecha || m.fallida) return;
        m.avance = Mathf.Min(valor, m.meta);
        if (m.avance < m.meta) return;

        m.hecha = true;
        m.hechaEn = Time.unscaledTime;
        GameManager gm = GameManager.Instance;
        if (gm != null)
            gm.SumarPuntos(m.premio, "✔ Misión cumplida: " + Texto(m) + "   +" + m.premio, new Color(1f, 0.86f, 0.40f));
        GuardianAudio.EnPantalla(GuardianAudio.Bonus, 0.5f, 1.08f, 1.14f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        bool jugando = gm.estado == GameManager.Estado.Jugando;
        // Nivel nuevo: se entra a jugar, cambia la zona, o el reloj vuelve a llenarse (reinicio desde la pausa).
        bool reinicio = jugando && (!estabaJugando || gm.zonaActual != zonaAntes || gm.tiempoRestante > tiempoAntes + 1f);
        estabaJugando = jugando;
        tiempoAntes = gm.tiempoRestante;
        zonaAntes = gm.zonaActual;
        if (reinicio) Preparar(gm);
        if (!jugando) return;

        bool hablando = GuardianConversacion.Hablando;
        foreach (Mision m in misiones)
        {
            // La presentación de la zona (que se abre sola al empezar) no cuenta:
            // la misión pide ir a hablar con alguien por iniciativa propia.
            if (m.tipo == Tipo.Conversar && hablando && !hablandoAntes && Time.unscaledTime - inicioNivel > 3f) Avanzar(m, 1);
            if (m.tipo == Tipo.Bici && Bicicleta.EnUso != null) Avanzar(m, 1);
            if (m.tipo == Tipo.SinGolpes && gm.esperandoAcopio) Avanzar(m, 1);
        }
        hablandoAntes = hablando;
    }

    // ------------------------------------------------------------ dibujo

    private static string Texto(Mision m)
    {
        switch (m.tipo)
        {
            case Tipo.SegregarColor: return "Segrega " + m.meta + " de " + Residuo.Nombre(m.color);
            case Tipo.Racha:         return "Racha de " + m.meta + " aciertos";
            case Tipo.Conversar:     return "Conversa con un vecino (E)";
            case Tipo.Rapido:        return m.meta + " residuos en 25 s";
            case Tipo.Bici:          return "Pedalea en bicicleta (E)";
            default:                 return "Limpia la zona sin golpes";
        }
    }

    private static Texture Icono(Mision m)
    {
        switch (m.tipo)
        {
            case Tipo.SegregarColor: return GuardianIconos.Contenedor(m.color);
            case Tipo.Racha:         return GuardianLogros.Medalla(GuardianLogros.RACHA_5, true);
            case Tipo.Conversar:     return GuardianLogros.Medalla(GuardianLogros.CHARLA, true);
            case Tipo.Rapido:        return GuardianLogros.Medalla(GuardianLogros.IMPARABLE, true);
            case Tipo.Bici:          return GuardianLogros.Medalla(GuardianLogros.BICI, true);
            default:                 return GuardianLogros.Medalla(GuardianLogros.PEATON, true);
        }
    }

    void OnGUI()
    {
        GUI.depth = -20;
        GameManager gm = GameManager.Instance;
        if (gm == null || misiones.Count == 0) return;
        if (gm.estado != GameManager.Estado.Jugando || gm.pausado || GuardianConversacion.Hablando) return;
        if (GuardianUIExtra.HayModal) return;
        GuardianUIExtra.Preparar();

        // Debajo del panel del HUD (que ocupa hasta ~y 244 cuando hay racha).
        float x = 14f, y = 252f, w = 320f, fila = 38f;
        float alto = 26f + misiones.Count * fila;
        GuardianUIExtra.Caja(new Rect(x, y, w, alto), new Color(0.05f, 0.07f, 0.10f, 0.72f), 8);
        GUI.Label(new Rect(x + 12f, y + 4f, w - 24f, 18f), "MISIONES DE LA ZONA",
                  GuardianUIExtra.Txt(12, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleLeft));

        for (int i = 0; i < misiones.Count; i++)
        {
            Mision m = misiones[i];
            float fy = y + 24f + i * fila;

            // Destello dorado durante un segundo al cumplirla.
            float brillo = m.hecha ? Mathf.Clamp01(1f - (Time.unscaledTime - m.hechaEn)) : 0f;
            if (brillo > 0f) GuardianUIExtra.Caja(new Rect(x + 4f, fy, w - 8f, fila - 4f), new Color(1f, 0.85f, 0.35f, 0.45f * brillo), 6);

            Color antes = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, m.fallida ? 0.35f : 1f);
            GUI.DrawTexture(new Rect(x + 10f, fy + 2f, 30f, 30f), Icono(m), ScaleMode.ScaleToFit);
            GUI.color = antes;

            Color c = m.hecha ? new Color(0.55f, 1f, 0.70f) : m.fallida ? new Color(0.70f, 0.55f, 0.55f) : Color.white;
            GUI.Label(new Rect(x + 48f, fy, w - 110f, fila - 4f), Texto(m),
                      GuardianUIExtra.Txt(13, FontStyle.Bold, c, TextAnchor.MiddleLeft));

            Rect estado = new Rect(x + w - 58f, fy + 4f, 48f, fila - 12f);
            if (m.hecha) GUI.DrawTexture(new Rect(estado.x + 14f, estado.y, estado.height, estado.height), GuardianIconos.Check());
            else if (m.fallida) GUI.DrawTexture(new Rect(estado.x + 14f, estado.y, estado.height, estado.height), GuardianIconos.Equis());
            else if (m.meta > 1)
                GUI.Label(estado, m.avance + "/" + m.meta, GuardianUIExtra.Txt(14, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleRight));
            else
                GUI.Label(estado, "+" + m.premio, GuardianUIExtra.Txt(13, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleRight));
        }
    }
}
