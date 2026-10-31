using UnityEngine;

/// <summary>
/// Guardián de Huancayo - utilidades de interfaz para los módulos nuevos
/// (Insignias, Reto NTP, Opciones y Misiones).
///
/// Usa la MISMA escala que el HUD (GuardianCameraHUD): la interfaz se diseñó
/// para ~800 px de alto y se agranda con la resolución, así que todo lo nuevo
/// queda alineado con lo que ya existía, en Full HD, 4K o una laptop.
///
/// También lleva la cuenta de qué ventana está abierta (solo una a la vez) y
/// de los botones extra del menú principal, que se apilan abajo a la izquierda.
/// </summary>
public static class GuardianUIExtra
{
    // ------------------------------------------------------------ escala

    public static float Escala { get; private set; } = 1f;
    public static float SW => Screen.width / Escala;
    public static float SH => Screen.height / Escala;

    /// <summary>Calcula la escala (misma fórmula del HUD) y la aplica a GUI.matrix.</summary>
    public static void Preparar()
    {
        Escala = Mathf.Clamp(Mathf.Min(Screen.height / 800f, Screen.width / 1360f), 0.7f, 2.4f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Escala, Escala, 1f));
    }

    /// <summary>Punto del mundo → coordenadas de la GUI escalada. false si queda detrás de la cámara.</summary>
    public static bool APantalla(Camera cam, Vector3 mundo, out Vector2 p)
    {
        p = Vector2.zero;
        if (cam == null) return false;
        Vector3 s = cam.WorldToScreenPoint(mundo);
        if (s.z <= 0.05f) return false;
        p = new Vector2(s.x / Escala, (Screen.height - s.y) / Escala);
        return p.x > -60f && p.x < SW + 60f && p.y > -60f && p.y < SH + 60f;
    }

    // ------------------------------------------------------------ ventana abierta (una a la vez)

    /// <summary>Nombre de la ventana nueva que está abierta ("" = ninguna).</summary>
    public static string Modal { get; private set; } = "";
    public static bool HayModal => !string.IsNullOrEmpty(Modal);

    public static bool Abrir(string quien)
    {
        if (HayModal && Modal != quien) return false;
        Modal = quien;
        return true;
    }

    public static void Cerrar(string quien) { if (Modal == quien) Modal = ""; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar() { Modal = ""; }

    // ------------------------------------------------------------ estilos

    public static GUIStyle Txt(int tam, FontStyle f, Color c, TextAnchor a, bool envolver = false)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label);
        s.fontSize = Mathf.Max(8, tam);
        s.fontStyle = f;
        s.alignment = a;
        s.wordWrap = envolver;
        s.richText = false;
        s.normal.textColor = c;
        return s;
    }

    /// <summary>Panel de esquinas redondeadas (9-slice: las esquinas no se deforman al estirar).</summary>
    public static void Caja(Rect r, Color c, int radio = 10)
    {
        if (Event.current.type != EventType.Repaint) return;
        GUIStyle s = new GUIStyle();
        s.normal.background = GuardianIconos.Panel(c, radio);
        s.border = new RectOffset(radio + 2, radio + 2, radio + 2, radio + 2);
        s.Draw(r, false, false, false, false);
    }

    /// <summary>
    /// Botón propio: panel redondeado que se aclara al pasar el mouse, con
    /// ícono opcional a la izquierda. Devuelve true al hacer clic (o tocar).
    /// </summary>
    public static bool Boton(Rect r, string texto, Texture icono = null, bool resaltado = false,
                             int tamLetra = 15, Color? acento = null)
    {
        Color ac = acento ?? new Color(0.30f, 0.85f, 0.55f);
        bool encima = r.Contains(Event.current.mousePosition);
        Color fondo = resaltado ? Color.Lerp(new Color(0.16f, 0.22f, 0.30f), ac, 0.45f)
                    : encima ? Color.Lerp(new Color(0.16f, 0.22f, 0.30f), ac, 0.28f)
                             : new Color(0.12f, 0.16f, 0.22f, 0.96f);
        Caja(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f),
             new Color(ac.r, ac.g, ac.b, encima || resaltado ? 0.95f : 0.45f), 10);
        Caja(r, fondo, 9);

        float x = r.x + 12f;
        if (icono != null)
        {
            float lado = Mathf.Min(r.height - 8f, 44f);
            GUI.DrawTexture(new Rect(x, r.y + (r.height - lado) / 2f, lado, lado), icono, ScaleMode.ScaleToFit);
            x += lado + 8f;
        }
        TextAnchor ancla = icono != null ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
        Rect rt = icono != null ? new Rect(x, r.y, r.xMax - x - 8f, r.height) : r;
        GUI.Label(rt, texto, Txt(tamLetra, FontStyle.Bold, Color.white, ancla));

        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    /// <summary>Interruptor ON/OFF dibujado (como un Toggle de la clase, pero grande y legible).</summary>
    public static bool Interruptor(Rect r, bool valor)
    {
        Color on = new Color(0.30f, 0.85f, 0.55f), off = new Color(0.30f, 0.33f, 0.40f);
        Caja(r, valor ? on : off, Mathf.RoundToInt(r.height / 2f) - 1);
        float lado = r.height - 6f;
        float x = valor ? r.xMax - lado - 3f : r.x + 3f;
        GUI.DrawTexture(new Rect(x, r.y + 3f, lado, lado), GuardianIconos.Punto());
        GUI.Label(new Rect(valor ? r.x + 8f : r.x + lado + 6f, r.y, r.width - lado - 14f, r.height),
                  valor ? "ON" : "OFF",
                  Txt(12, FontStyle.Bold, valor ? new Color(0.05f, 0.20f, 0.10f) : new Color(0.85f, 0.88f, 0.92f),
                      valor ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight));
        if (GUI.Button(r, GUIContent.none, GUIStyle.none)) return !valor;
        return valor;
    }

    /// <summary>Oscurece toda la pantalla (fondo de las ventanas).</summary>
    public static void Velo(float alfa)
    {
        Color antes = GUI.color;
        GUI.color = new Color(0.02f, 0.03f, 0.05f, alfa);
        GUI.DrawTexture(new Rect(0, 0, SW, SH), Texture2D.whiteTexture);
        GUI.color = antes;
    }

    /// <summary>Se come los clics que nadie usó, para que no lleguen al menú de abajo.</summary>
    public static void Tapar()
    {
        GUI.Button(new Rect(0, 0, SW, SH), GUIContent.none, GUIStyle.none);
    }

    /// <summary>Tarjeta centrada de las ventanas nuevas (con franja roja y blanca, como el menú).</summary>
    public static Rect Tarjeta(float w, float h, Color franja)
    {
        Rect r = new Rect(SW / 2f - w / 2f, SH / 2f - h / 2f, w, h);
        Caja(new Rect(r.x - 3f, r.y - 3f, r.width + 6f, r.height + 6f), new Color(0f, 0f, 0f, 0.55f), 14);
        Caja(r, new Color(0.08f, 0.11f, 0.15f, 0.98f), 12);
        Color antes = GUI.color;
        GUI.color = franja;
        GUI.DrawTexture(new Rect(r.x + 10f, r.y, r.width - 20f, 6f), Texture2D.whiteTexture);
        GUI.color = antes;
        return r;
    }

    // ------------------------------------------------------------ botones extra del menú principal

    /// <summary>
    /// Ranura de botón abajo a la izquierda del menú principal (0 = la de más
    /// abajo). Así cada módulo pone su botón sin pisar al otro ni a la tarjeta
    /// central del menú.
    /// </summary>
    public static Rect RanuraMenu(int i)
    {
        float w = 262f, h = 46f;
        return new Rect(18f, SH - 64f - i * (h + 10f), w, h);
    }

    /// <summary>¿Se puede dibujar algo nuevo encima del menú principal?</summary>
    public static bool EnMenuPrincipal()
    {
        GameManager gm = GameManager.Instance;
        return gm != null && gm.estado == GameManager.Estado.Menu && !gm.pausado;
    }

    // ------------------------------------------------------------ teclado (vale con el Input System y con el antiguo)

    /// <summary>Tecla pulsada en este evento de GUI (IMGUI recibe el teclado con cualquier sistema de entrada).</summary>
    public static bool Tecla(KeyCode k)
    {
        Event e = Event.current;
        return e != null && e.type == EventType.KeyDown && e.keyCode == k;
    }
}
