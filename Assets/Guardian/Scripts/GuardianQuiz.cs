using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guardián de Huancayo - RETO NTP: "¿A qué contenedor va?"
///
/// Minijuego de 10 preguntas que se abre desde el menú principal. Es casi todo
/// IMAGEN: aparece el dibujo de un residuo de la vida diaria (botella, periódico,
/// lata, cáscara de plátano…) y abajo los cinco contenedores de la NTP
/// 900.058-2019. Se responde con un clic (o tocando, o con las teclas 1 a 5)
/// antes de que se vacíe la barra de tiempo.
///
/// Al responder, el contenedor correcto se ilumina y una frase corta dice POR
/// QUÉ va ahí (Residuo.Clave). Así el reto refuerza lo mismo que el juego, pero
/// en un minuto y sin caminar por la ciudad: sirve para practicar antes de
/// jugar y para mostrar el tema en clase.
///
/// 10 de 10 da la insignia "MAESTRO DE LA NTP". El mejor puntaje se guarda.
/// </summary>
public class GuardianQuiz : MonoBehaviour
{
    public static GuardianQuiz Instancia { get; private set; }

    private const string MODAL = "reto";
    private const int PREGUNTAS = 10;
    private const float TIEMPO = 9f;            // segundos por pregunta
    private const float PAUSA_RESPUESTA = 2.6f;
    private const string K_RECORD = "guardian_reto_record";
    private const string K_MEJOR = "guardian_reto_mejor";      // mejores aciertos (de 10)

    private enum Fase { Cerrado, Portada, Pregunta, Respuesta, Final }
    private Fase fase = Fase.Cerrado;

    private readonly List<int> orden = new List<int>();
    private readonly List<bool> historial = new List<bool>();
    private int idx, aciertos, puntos, elegido = -1;
    private float tInicio, tRespuesta, restante;
    private bool nuevoRecord;

    public static bool Abierto => Instancia != null && Instancia.fase != Fase.Cerrado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (Instancia != null) return;
        if (GameObject.FindGameObjectWithTag("Player") == null) return;
        new GameObject("GuardianQuiz").AddComponent<GuardianQuiz>();
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
        GuardianUIExtra.Cerrar(MODAL);
    }

    // ------------------------------------------------------------ flujo

    public static void Abrir()
    {
        if (Instancia == null || !GuardianUIExtra.Abrir(MODAL)) return;
        Instancia.fase = Fase.Portada;
    }

    private void Cerrar()
    {
        fase = Fase.Cerrado;
        GuardianUIExtra.Cerrar(MODAL);
    }

    private void Empezar()
    {
        orden.Clear();
        historial.Clear();

        // Dos residuos de cada color: el reto siempre recorre los cinco contenedores.
        for (int t = 0; t < Residuo.TIPOS; t++)
        {
            List<int> deEste = new List<int>();
            for (int i = 0; i < GuardianIconos.OBJETOS.Length; i++)
                if ((int)GuardianIconos.OBJETOS[i].tipo == t) deEste.Add(i);
            Barajar(deEste);
            for (int k = 0; k < Mathf.Min(2, deEste.Count); k++) orden.Add(deEste[k]);
        }
        Barajar(orden);
        while (orden.Count > PREGUNTAS) orden.RemoveAt(orden.Count - 1);

        idx = 0; aciertos = 0; puntos = 0; nuevoRecord = false;
        SiguientePregunta(true);
    }

    private static void Barajar(List<int> l)
    {
        for (int i = l.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = l[i]; l[i] = l[j]; l[j] = tmp;
        }
    }

    private void SiguientePregunta(bool primera)
    {
        if (!primera) idx++;
        if (idx >= orden.Count) { Terminar(); return; }
        elegido = -1;
        tInicio = Time.unscaledTime;
        fase = Fase.Pregunta;
    }

    private TipoResiduo Correcto => GuardianIconos.OBJETOS[orden[idx]].tipo;

    private void Responder(int tipo)
    {
        if (fase != Fase.Pregunta) return;
        elegido = tipo;
        restante = Mathf.Max(0f, TIEMPO - (Time.unscaledTime - tInicio));
        bool bien = tipo == (int)Correcto;
        historial.Add(bien);
        if (bien)
        {
            aciertos++;
            int ganado = 100 + Mathf.RoundToInt(restante * 10f);
            puntos += ganado;
            GuardianAudio.EnPantalla(GuardianAudio.Acierto, 0.6f, 1f + aciertos * 0.03f, 1.05f + aciertos * 0.03f);
        }
        else
        {
            GuardianAudio.EnPantalla(GuardianAudio.Error, 0.55f, 0.95f, 1.0f);
        }
        tRespuesta = Time.unscaledTime;
        fase = Fase.Respuesta;
    }

    private void Terminar()
    {
        fase = Fase.Final;
        if (puntos > PlayerPrefs.GetInt(K_RECORD, 0)) { PlayerPrefs.SetInt(K_RECORD, puntos); nuevoRecord = true; }
        if (aciertos > PlayerPrefs.GetInt(K_MEJOR, 0)) PlayerPrefs.SetInt(K_MEJOR, aciertos);
        PlayerPrefs.Save();
        if (aciertos >= PREGUNTAS) GuardianLogros.Desbloquear(GuardianLogros.QUIZ);
        GuardianAudio.EnPantalla(aciertos >= 7 ? GuardianAudio.Ganaste : GuardianAudio.Bonus, 0.65f);
    }

    void Update()
    {
        if (fase == Fase.Pregunta && Time.unscaledTime - tInicio >= TIEMPO) Responder(-1);   // se acabó el tiempo
        if (fase == Fase.Respuesta && Time.unscaledTime - tRespuesta >= PAUSA_RESPUESTA) SiguientePregunta(false);
        if (fase != Fase.Cerrado && !GuardianUIExtra.EnMenuPrincipal()) Cerrar();
    }

    // ------------------------------------------------------------ dibujo

    void OnGUI()
    {
        GUI.depth = -32;
        if (GameManager.Instance == null) return;
        GuardianUIExtra.Preparar();

        if (fase == Fase.Cerrado)
        {
            if (GuardianUIExtra.EnMenuPrincipal() && !GuardianUIExtra.HayModal)
            {
                int mejor = PlayerPrefs.GetInt(K_MEJOR, 0);
                string sub = "¿A qué tacho va?" + (mejor > 0 ? "   ·   mejor " + mejor + "/10" : "");
                if (GuardianUIExtra.Boton(GuardianUIExtra.RanuraMenu(0), "RETO NTP", GuardianIconos.Objeto(9), false, 16,
                                          new Color(0.98f, 0.72f, 0.20f), sub))
                    Abrir();
            }
            return;
        }

        if (GuardianUIExtra.Tecla(KeyCode.Escape)) { Cerrar(); Event.current.Use(); return; }

        GuardianUIExtra.Velo(0.85f);
        Rect card = GuardianUIExtra.Tarjeta(980f, 650f, new Color(0.98f, 0.72f, 0.20f));

        switch (fase)
        {
            case Fase.Portada: Portada(card); break;
            case Fase.Pregunta:
            case Fase.Respuesta: Pregunta(card); break;
            case Fase.Final: Final(card); break;
        }

        GuardianUIExtra.Tapar();
    }

    private void Titulo(Rect card, string texto)
    {
        GUI.Label(new Rect(card.x, card.y + 14f, card.width, 42f), texto,
                  GuardianUIExtra.Txt(28, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));
    }

    private void Portada(Rect card)
    {
        Titulo(card, "RETO NTP · ¿A QUÉ CONTENEDOR VA?");
        GUI.Label(new Rect(card.x, card.y + 58f, card.width, 24f),
                  "Mira el residuo  →  toca su contenedor  ·  10 preguntas · " + (int)TIEMPO + " segundos cada una",
                  GuardianUIExtra.Txt(16, FontStyle.Normal, new Color(0.80f, 0.86f, 0.92f), TextAnchor.MiddleCenter));

        // Ejemplo visual: tres residuos con una flecha a su contenedor.
        int[] ejemplo = { 4, 9, 11 };
        float ancho = 270f, x0 = card.x + (card.width - ancho * 3f) / 2f, y = card.y + 110f;
        for (int k = 0; k < ejemplo.Length; k++)
        {
            GuardianIconos.ObjetoReto o = GuardianIconos.OBJETOS[ejemplo[k]];
            Rect celda = new Rect(x0 + k * ancho + 8f, y, ancho - 16f, 200f);
            GuardianUIExtra.Caja(celda, new Color(0.13f, 0.17f, 0.23f), 12);
            GUI.DrawTexture(new Rect(celda.x + 12f, celda.y + 30f, 104f, 104f), GuardianIconos.Objeto(ejemplo[k]));
            GUI.Label(new Rect(celda.x + 112f, celda.y + 60f, 30f, 40f), "→",
                      GuardianUIExtra.Txt(30, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter));
            GUI.DrawTexture(new Rect(celda.x + 140f, celda.y + 26f, 104f, 104f), GuardianIconos.Contenedor(o.tipo));
            GUI.Label(new Rect(celda.x, celda.y + 146f, celda.width, 22f), o.nombre,
                      GuardianUIExtra.Txt(15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(celda.x, celda.y + 168f, celda.width, 20f), "→ " + Residuo.ColorNTP(o.tipo),
                      GuardianUIExtra.Txt(13, FontStyle.Bold, ColorTexto(o.tipo), TextAnchor.MiddleCenter));
        }

        // Los cinco contenedores, en fila, tal como se verán en el reto.
        float bw = 120f, bx = card.x + (card.width - bw * 5f) / 2f;
        for (int i = 0; i < Residuo.TIPOS; i++)
        {
            TipoResiduo t = Residuo.Desde(i);
            GUI.DrawTexture(new Rect(bx + i * bw + 14f, card.y + 340f, 92f, 92f), GuardianIconos.Contenedor(t));
            GUI.Label(new Rect(bx + i * bw, card.y + 434f, bw, 18f), Residuo.ColorNTP(t),
                      GuardianUIExtra.Txt(12, FontStyle.Bold, ColorTexto(t), TextAnchor.MiddleCenter));
            GUI.Label(new Rect(bx + i * bw, card.y + 450f, bw, 18f), Residuo.Nombre(t),
                      GuardianUIExtra.Txt(11, FontStyle.Normal, new Color(0.75f, 0.80f, 0.86f), TextAnchor.MiddleCenter));
        }

        int record = PlayerPrefs.GetInt(K_RECORD, 0);
        if (record > 0)
            GUI.Label(new Rect(card.x, card.y + 480f, card.width, 22f), "Récord: " + record + " puntos",
                      GuardianUIExtra.Txt(15, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter));

        if (GuardianUIExtra.Boton(new Rect(card.x + card.width / 2f - 250f, card.yMax - 120f, 240f, 54f), "▶  EMPEZAR", null, true, 18,
                                  new Color(0.98f, 0.72f, 0.20f))
            || GuardianUIExtra.Tecla(KeyCode.Return) || GuardianUIExtra.Tecla(KeyCode.Space))
        { Empezar(); return; }
        if (GuardianUIExtra.Boton(new Rect(card.x + card.width / 2f + 10f, card.yMax - 120f, 240f, 54f), "◀  VOLVER", null, false, 18))
            Cerrar();
    }

    private void Pregunta(Rect card)
    {
        GuardianIconos.ObjetoReto o = GuardianIconos.OBJETOS[orden[idx]];
        bool respondiendo = fase == Fase.Respuesta;

        Titulo(card, "¿A QUÉ CONTENEDOR VA?");

        // Progreso: un punto por pregunta (verde = bien, rojo = mal).
        float px0 = card.x + card.width / 2f - PREGUNTAS * 13f;
        for (int i = 0; i < PREGUNTAS; i++)
        {
            Color c = i < historial.Count ? (historial[i] ? new Color(0.35f, 0.90f, 0.55f) : new Color(0.95f, 0.40f, 0.35f))
                    : i == idx ? Color.white : new Color(1f, 1f, 1f, 0.22f);
            Color antes = GUI.color; GUI.color = c;
            GUI.DrawTexture(new Rect(px0 + i * 26f, card.y + 62f, 16f, 16f), GuardianIconos.Punto());
            GUI.color = antes;
        }
        GUI.Label(new Rect(card.x + 24f, card.y + 56f, 220f, 28f), "Pregunta " + (idx + 1) + " / " + PREGUNTAS,
                  GuardianUIExtra.Txt(15, FontStyle.Bold, new Color(0.80f, 0.86f, 0.92f), TextAnchor.MiddleLeft));
        GUI.Label(new Rect(card.xMax - 244f, card.y + 56f, 220f, 28f), puntos + " pts",
                  GuardianUIExtra.Txt(18, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleRight));

        // Barra de tiempo
        float quedan = respondiendo ? restante : Mathf.Max(0f, TIEMPO - (Time.unscaledTime - tInicio));
        float f = quedan / TIEMPO;
        Rect barra = new Rect(card.x + 140f, card.y + 92f, card.width - 280f, 12f);
        GuardianUIExtra.Caja(barra, new Color(0.20f, 0.24f, 0.30f), 5);
        if (f > 0.01f)
            GuardianUIExtra.Caja(new Rect(barra.x, barra.y, Mathf.Max(10f, barra.width * f), barra.height),
                                 Color.Lerp(new Color(0.95f, 0.35f, 0.30f), new Color(0.35f, 0.90f, 0.55f), f), 5);

        // El residuo, grande y con un rebote al aparecer.
        float t = Time.unscaledTime - tInicio;
        float rebote = respondiendo ? 1f : 1f + 0.12f * Mathf.Exp(-t * 7f) * Mathf.Cos(t * 18f);
        float lado = 210f * rebote;
        Rect rObj = new Rect(card.x + card.width / 2f - lado / 2f, card.y + 120f + (210f - lado) / 2f, lado, lado);
        GuardianUIExtra.Caja(new Rect(card.x + card.width / 2f - 125f, card.y + 114f, 250f, 222f), new Color(0.13f, 0.17f, 0.23f), 16);
        GUI.DrawTexture(rObj, GuardianIconos.Objeto(orden[idx]));
        GUI.Label(new Rect(card.x, card.y + 340f, card.width, 34f), o.nombre,
                  GuardianUIExtra.Txt(26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));

        bool bien = respondiendo && elegido == (int)o.tipo;
        if (respondiendo)
        {
            float k = Mathf.Clamp01((Time.unscaledTime - tRespuesta) / 0.2f);
            float s = 120f * Mathf.Lerp(1.6f, 1f, k);
            Color antes = GUI.color;
            GUI.color = bien ? new Color(0.35f, 0.95f, 0.55f, k) : new Color(0.98f, 0.38f, 0.32f, k);
            GUI.DrawTexture(new Rect(rObj.center.x + 70f - s / 2f, rObj.y + 10f, s, s), bien ? GuardianIconos.Check() : GuardianIconos.Equis());
            GUI.color = antes;

            string msg = bien ? "¡Bien!  " + Mayuscula(Residuo.Clave(o.tipo))
                       : (elegido < 0 ? "¡Se acabó el tiempo!  " : "")
                         + "Va al " + Residuo.ColorNTP(o.tipo) + " (" + Residuo.Nombre(o.tipo).ToLower() + "): " + Residuo.Clave(o.tipo);
            GUI.Label(new Rect(card.x + 30f, card.y + 374f, card.width - 60f, 28f), msg,
                      GuardianUIExtra.Txt(17, FontStyle.Bold, bien ? new Color(0.45f, 1f, 0.65f) : new Color(1f, 0.62f, 0.52f), TextAnchor.MiddleCenter));
        }

        // Los cinco contenedores (botones grandes con imagen).
        float bw = 172f, bh = 196f, gap = 12f;
        float x0 = card.x + (card.width - (bw * 5f + gap * 4f)) / 2f;
        float y0 = card.y + 412f;
        for (int i = 0; i < Residuo.TIPOS; i++)
        {
            TipoResiduo tt = Residuo.Desde(i);
            Rect r = new Rect(x0 + i * (bw + gap), y0, bw, bh);
            bool encima = !respondiendo && r.Contains(Event.current.mousePosition);
            if (encima) r.y -= 5f;

            Color marco = new Color(0.30f, 0.36f, 0.44f);
            if (respondiendo && i == (int)o.tipo) marco = new Color(0.35f, 0.95f, 0.55f);
            else if (respondiendo && i == elegido) marco = new Color(0.98f, 0.38f, 0.32f);
            else if (encima) marco = Residuo.Tinte(tt);
            float grosor = (respondiendo && (i == (int)o.tipo || i == elegido)) ? 5f : 3f;
            GuardianUIExtra.Caja(new Rect(r.x - grosor, r.y - grosor, r.width + grosor * 2f, r.height + grosor * 2f), marco, 14);
            GuardianUIExtra.Caja(r, new Color(0.11f, 0.14f, 0.19f), 12);

            float apagado = (respondiendo && i != (int)o.tipo && i != elegido) ? 0.35f : 1f;
            Color antes = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, apagado);
            GUI.DrawTexture(new Rect(r.x + 16f, r.y + 10f, bw - 32f, bw - 32f), GuardianIconos.Contenedor(tt));
            GUI.color = antes;
            GUI.Label(new Rect(r.x, r.y + bw - 20f, bw, 22f), Residuo.ColorNTP(tt),
                      GuardianUIExtra.Txt(15, FontStyle.Bold, ColorTexto(tt), TextAnchor.MiddleCenter));
            GUI.Label(new Rect(r.x, r.y + bw, bw, 20f), Residuo.Nombre(tt),
                      GuardianUIExtra.Txt(11, FontStyle.Normal, new Color(0.75f, 0.80f, 0.86f), TextAnchor.MiddleCenter));

            // Tecla de atajo
            GuardianUIExtra.Caja(new Rect(r.x + 6f, r.y + 6f, 24f, 24f), new Color(0f, 0f, 0f, 0.55f), 8);
            GUI.Label(new Rect(r.x + 6f, r.y + 6f, 24f, 24f), (i + 1).ToString(),
                      GuardianUIExtra.Txt(13, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));

            if (!respondiendo && GUI.Button(r, GUIContent.none, GUIStyle.none)) Responder(i);
            if (!respondiendo && (GuardianUIExtra.Tecla(KeyCode.Alpha1 + i) || GuardianUIExtra.Tecla(KeyCode.Keypad1 + i)))
            { Responder(i); Event.current.Use(); }
        }

        if (respondiendo)
        {
            GUI.Label(new Rect(card.x, card.yMax - 30f, card.width, 22f), "Enter o clic para seguir",
                      GuardianUIExtra.Txt(12, FontStyle.Normal, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleCenter));
            bool seguir = GuardianUIExtra.Tecla(KeyCode.Return) || GuardianUIExtra.Tecla(KeyCode.Space)
                       || (Event.current.type == EventType.MouseDown && Time.unscaledTime - tRespuesta > 0.4f);
            if (seguir) { Event.current.Use(); SiguientePregunta(false); }
        }
    }

    private void Final(Rect card)
    {
        bool perfecto = aciertos >= PREGUNTAS;
        Titulo(card, perfecto ? "¡MAESTRO DE LA NTP!" : aciertos >= 7 ? "¡MUY BIEN, GUARDIÁN!" : "SIGUE PRACTICANDO");

        // Estrellas (0 a 3)
        int estrellas = aciertos >= 10 ? 3 : aciertos >= 8 ? 2 : aciertos >= 5 ? 1 : 0;
        for (int i = 0; i < 3; i++)
        {
            float s = (i == 1 ? 96f : 76f);
            float x = card.x + card.width / 2f - 150f + i * 110f + (96f - s) / 2f;
            float y = card.y + 70f + (i == 1 ? 0f : 16f);
            Color antes = GUI.color;
            GUI.color = i < estrellas ? Color.white : new Color(1f, 1f, 1f, 0.6f);
            GUI.DrawTexture(new Rect(x, y, s, s), GuardianIconos.Insignia(GuardianLogros.PERFECTA, new Color(0.85f, 0.62f, 0.12f), i < estrellas));
            GUI.color = antes;
        }

        GUI.Label(new Rect(card.x, card.y + 184f, card.width, 56f), aciertos + " / " + PREGUNTAS,
                  GuardianUIExtra.Txt(46, FontStyle.Bold, perfecto ? new Color(1f, 0.85f, 0.35f) : Color.white, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(card.x, card.y + 240f, card.width, 28f), puntos + " puntos" + (nuevoRecord ? "   ·   ¡NUEVO RÉCORD!" : ""),
                  GuardianUIExtra.Txt(20, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter));

        // Repaso visual: cada residuo de la ronda con un visto o una equis.
        float lado = 64f, gap = 10f;
        float x0 = card.x + (card.width - (lado * orden.Count + gap * (orden.Count - 1))) / 2f;
        for (int i = 0; i < orden.Count && i < historial.Count; i++)
        {
            Rect r = new Rect(x0 + i * (lado + gap), card.y + 290f, lado, lado);
            GuardianUIExtra.Caja(r, historial[i] ? new Color(0.14f, 0.30f, 0.20f) : new Color(0.32f, 0.14f, 0.14f), 10);
            GUI.DrawTexture(new Rect(r.x + 4f, r.y + 4f, lado - 8f, lado - 8f), GuardianIconos.Objeto(orden[i]));
            GUI.DrawTexture(new Rect(r.xMax - 22f, r.yMax - 22f, 20f, 20f), historial[i] ? GuardianIconos.Check() : GuardianIconos.Equis());
            GUI.DrawTexture(new Rect(r.x + 2f, r.y + 2f, 20f, 20f), GuardianIconos.Contenedor(GuardianIconos.OBJETOS[orden[i]].tipo));
        }

        string leccion = perfecto ? "Segregar en la fuente es el primer paso para que el Shullcas vuelva a ser cristalino."
                       : aciertos >= 7 ? "Casi perfecto: repasa los que fallaste (arriba a la izquierda de cada uno está su tacho)."
                       : "Tip: en el juego, la tabla de colores de la NTP está siempre arriba a la derecha.";
        GUI.Label(new Rect(card.x + 40f, card.y + 372f, card.width - 80f, 46f), leccion,
                  GuardianUIExtra.Txt(15, FontStyle.Normal, new Color(0.72f, 0.86f, 0.96f), TextAnchor.MiddleCenter, true));

        if (perfecto)
        {
            GUI.DrawTexture(new Rect(card.x + card.width / 2f - 44f, card.y + 424f, 88f, 88f), GuardianLogros.Medalla(GuardianLogros.QUIZ, true));
        }
        else
        {
            GUI.Label(new Rect(card.x, card.y + 450f, card.width, 24f),
                      "Récord: " + PlayerPrefs.GetInt(K_RECORD, 0) + " puntos   ·   Mejor resultado: " + PlayerPrefs.GetInt(K_MEJOR, 0) + " / 10",
                      GuardianUIExtra.Txt(15, FontStyle.Bold, new Color(0.82f, 0.86f, 0.92f), TextAnchor.MiddleCenter));
        }

        if (GuardianUIExtra.Boton(new Rect(card.x + card.width / 2f - 250f, card.yMax - 82f, 240f, 54f), "↻  OTRA VEZ", null, true, 18,
                                  new Color(0.98f, 0.72f, 0.20f))
            || GuardianUIExtra.Tecla(KeyCode.Return))
        { Empezar(); return; }
        if (GuardianUIExtra.Boton(new Rect(card.x + card.width / 2f + 10f, card.yMax - 82f, 240f, 54f), "◀  VOLVER", null, false, 18))
            Cerrar();
    }

    // ------------------------------------------------------------ utilidades

    /// <summary>Color legible del nombre del contenedor sobre el fondo oscuro (el blanco y el marrón se aclaran).</summary>
    private static Color ColorTexto(TipoResiduo t)
    {
        Color c = Residuo.Tinte(t);
        if (t == TipoResiduo.Organico) return new Color(0.86f, 0.64f, 0.42f);
        if (t == TipoResiduo.Papel) return new Color(0.45f, 0.70f, 1f);
        return c;
    }

    private static string Mayuscula(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s.Substring(1);
    }
}
