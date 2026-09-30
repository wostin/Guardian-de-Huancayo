using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Guardián de Huancayo - Administrador del juego (ODS 11).
/// Estados: Menú -> Jugando -> Ganado/Perdido. Con niveles por zonas reales de
/// Huancayo, vidas, tiempo, contaminación y SEGREGACIÓN de residuos según el
/// código de colores de la NTP 900.058-2019.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public enum Estado { Menu, Jugando, Ganado, Perdido }

    [Header("Nivel base")]
    public int basuraTotal = 12;
    public int capacidadCarga = 6;
    public float tiempoLimiteBase = 180f;

    [Header("Contaminación (0-100)")]
    [Range(0f, 100f)] public float contaminacionInicial = 5f;
    public float subeContaminacionBase = 0.30f;
    public float maxContaminacion = 100f;

    [Header("Vida")]
    public int maxVidas = 3;

    [Header("Zonas")]
    public int numZonas = 3;

    // Estado en tiempo real
    public Estado estado { get; private set; } = Estado.Menu;
    public int nivel { get; private set; } = 1;
    public int vidas { get; private set; }
    public int recicladas { get; private set; }
    public int puntaje { get; private set; }

    /// <summary>
    /// Segregaciones correctas seguidas. Es lo que vuelve el juego un juego y no
    /// un tramite: acertar cinco veces al hilo vale mas que acertar cinco veces
    /// sueltas, y un solo error tira la racha al suelo. Da algo que cuidar
    /// mientras se camina, y ademas premia lo que el curso quiere ensenar, que
    /// es segregar BIEN, no solo recoger.
    /// </summary>
    public int racha { get; private set; }

    /// <summary>Frase que explica POR QUE ese residuo va donde va. Aparece unos
    /// segundos despues de un error, debajo del aviso.</summary>
    public string leccion { get; private set; } = "";
    private float leccionHasta;
    public bool HayLeccion => !string.IsNullOrEmpty(leccion) && Time.time < leccionHasta;
    /// <summary>La racha mas larga de la partida, para el resumen final.</summary>
    public int mejorRacha { get; private set; }
    /// <summary>Segundo en que se sumo el ultimo acierto (para animar el HUD).</summary>
    public float ultimoAcierto { get; private set; }
    public float contaminacion { get; private set; }
    public float tiempoRestante { get; private set; }
    public int basuraEnSuelo { get; private set; }
    public bool perdioPorTiempo { get; private set; }
    public bool pausado { get; private set; }
    public int Record => PlayerPrefs.GetInt("guardian_record", 0);
    public int MejorTasa => PlayerPrefs.GetInt("guardian_tasa", 0);
    public int NivelMaximo => PlayerPrefs.GetInt("guardian_nivel", 1);

    // --- Segregación ---
    private readonly List<TipoResiduo> carga = new List<TipoResiduo>();
    public int cargaActual => carga.Count;
    public IList<TipoResiduo> Carga => carga;
    public int aciertos { get; private set; }
    public int errores { get; private set; }

    /// <summary>Cuántos residuos de cada tipo se segregaron bien en este nivel.</summary>
    private readonly int[] porTipo = new int[Residuo.TIPOS];
    public int AciertosDe(TipoResiduo t) { return porTipo[Mathf.Clamp((int)t, 0, porTipo.Length - 1)]; }

    /// <summary>
    /// Modo recorrido: se puede caminar por toda la ciudad sin cronómetro, sin
    /// contaminación y sin perder vidas, con las tres zonas visibles a la vez.
    /// Es el modo para presentar el trabajo: se puede mostrar cada cosa con calma.
    /// </summary>
    public bool modoRecorrido { get; private set; }

    /// <summary>
    /// La zona ya está limpia y falta el último paso: llevar la jornada al
    /// PUNTO DE ACOPIO. Mientras dura, el cronómetro y la contaminación se
    /// congelan; si el jugador no lo encuentra, igual cierra solo.
    /// </summary>
    public bool esperandoAcopio { get; private set; }
    private float limiteAcopio;

    /// <summary>Nivel sin un solo error de contenedor.</summary>
    public bool Perfecta { get { return errores == 0 && aciertos > 0; } }

    /// <summary>Ruta del CSV con el historial de partidas (evidencia para el informe).</summary>
    public string ultimoRegistro { get; private set; } = "";

    /// <summary>Porcentaje de residuos que fueron al contenedor correcto a la primera.</summary>
    public int TasaSegregacion
    {
        get
        {
            int total = aciertos + errores;
            return total <= 0 ? 100 : Mathf.RoundToInt(100f * aciertos / total);
        }
    }

    public int CuantosLlevo(TipoResiduo t)
    {
        int n = 0;
        for (int i = 0; i < carga.Count; i++) if (carga[i] == t) n++;
        return n;
    }

    // Aviso corto en pantalla (enseña el color correcto al equivocarse).
    /// <summary>Pista fija del tutorial (solo en el nivel 1). "" = sin pista.</summary>
    public string pista { get; private set; } = "";
    private int pasoTutorial;
    private float tutoHasta;

    /// <summary>¿Acaba de limpiar las tres zonas de la ciudad?</summary>
    public bool CompletoLaCiudad
    {
        get { return estado == Estado.Ganado && nivel >= Mathf.Max(1, numZonas); }
    }

    public string aviso { get; private set; } = "";
    public Color avisoColor { get; private set; } = Color.white;
    private float avisoHasta;
    public bool HayAviso => !string.IsNullOrEmpty(aviso) && Time.unscaledTime < avisoHasta;

    private void Avisar(string texto, Color c, float seg = 2.6f)
    {
        aviso = texto; avisoColor = c; avisoHasta = Time.unscaledTime + seg;
    }

    public int zonaActual { get; private set; }
    private readonly string[] nombresZona = {
        "Centro · Plaza Constitución",
        "Mercado Mayorista · El Tambo",
        "Ribera del río Shullcas"
    };
    public string ZonaNombre => nombresZona[Mathf.Clamp(zonaActual, 0, nombresZona.Length - 1)];

    /// <summary>Nombre de la zona que le toca a un nivel dado (para anunciar la siguiente).</summary>
    public string NombreDeNivel(int n)
    {
        int z = (Mathf.Max(1, n) - 1) % Mathf.Max(1, numZonas);
        return nombresZona[Mathf.Clamp(z, 0, nombresZona.Length - 1)];
    }

    // Zona peatonal = sin carros (nivel de calentamiento). Con tráfico = hay carros.
    public bool ZonaPeatonal { get; private set; }

    // Dificultad escalada por nivel
    /// <summary>
    /// Cada zona se juega distinto, no solo "mas rapido".
    ///
    /// Antes las tres eran la misma partida con los carros mas veloces. Ahora el
    /// Centro es peatonal y da aire para aprender; el Mercado aprieta el reloj y
    /// se ensucia rapido, que es lo que pasa en el Mayorista a media manana; y la
    /// Ribera da mas tiempo, pero la basura se le escapa rio abajo.
    /// </summary>
    private static readonly float[] FACTOR_TIEMPO = { 1.00f, 0.78f, 1.12f };
    private static readonly float[] FACTOR_SUCIO  = { 0.80f, 1.45f, 1.10f };

    public float TiempoLimite
    {
        get
        {
            float f = FACTOR_TIEMPO[Mathf.Clamp(zonaActual, 0, FACTOR_TIEMPO.Length - 1)];
            return Mathf.Max(80f, (tiempoLimiteBase - (nivel - 1) * 15f) * f);
        }
    }

    public float SubeContaminacion
    {
        get
        {
            float f = FACTOR_SUCIO[Mathf.Clamp(zonaActual, 0, FACTOR_SUCIO.Length - 1)];
            return (subeContaminacionBase + (nivel - 1) * 0.15f) * f;
        }
    }

    private readonly List<TrashItem> todas = new List<TrashItem>();
    public event Action OnCambio;

    // --- Basura que los vecinos botan durante la partida ---
    [Header("Vecinos que ensucian")]
    public int maxExtrasPorNivel = 4;          // tope para que el nivel siga siendo ganable
    private int extras;
    public int Extras => extras;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        contaminacion = contaminacionInicial;
        tiempoRestante = TiempoLimite;
        vidas = maxVidas;
        estado = Estado.Menu;
    }

    public void RegistrarBasura(TrashItem t)
    {
        if (t != null && !todas.Contains(t)) todas.Add(t);
    }

    private void ResetNivel()
    {
        // Cada nivel usa una zona distinta de la ciudad.
        zonaActual = (nivel - 1) % Mathf.Max(1, numZonas);

        // ¿La zona actual tiene carros? Si no, es una zona peatonal (segura).
        ZonaPeatonal = !HayCarrosEnZona(zonaActual);

        int total = 0;
        for (int i = 0; i < todas.Count; i++)
        {
            if (todas[i] == null) continue;
            bool activa = !todas[i].deReserva
                       && (modoRecorrido || todas[i].zona == zonaActual);
            todas[i].gameObject.SetActive(activa);
            if (activa) total++;
        }
        if (total > 0) basuraTotal = total;

        // Solo se ven los contenedores de la zona en la que se juega.
        RecycleBin[] botes = FindObjectsByType<RecycleBin>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < botes.Length; i++)
            if (botes[i] != null)
                botes[i].gameObject.SetActive(modoRecorrido || botes[i].zona == zonaActual);

        carga.Clear();
        esperandoAcopio = false;
        for (int i = 0; i < porTipo.Length; i++) porTipo[i] = 0;
        alarmaDada = false;
        pasoTutorial = 0;
        pista = "";
        extras = 0;
        recicladas = 0;
        aciertos = 0;
        errores = 0;
        aviso = "";
        leccion = "";
        leccionHasta = 0f;
        contaminacion = contaminacionInicial;
        tiempoRestante = TiempoLimite;
        vidas = maxVidas;
        perdioPorTiempo = false;
        pausado = false;
        Time.timeScale = 1f;

        ReposicionarJugador(CentroZona(zonaActual));

        // Presentación de la zona: cada nivel enseña algo del problema real.
        Avisar(IntroZona(zonaActual), new Color(0.65f, 0.90f, 1f), 6.5f);
    }

    // Cada zona anuncia COMO se juega, no solo donde esta. Antes las tres decian
    // lo mismo con otras palabras y el jugador no sabia que cambiaba.
    private static readonly string[] INTRO_ZONA = {
        "Plaza Constitución · zona peatonal, sin carros: acá se aprende a segregar",
        "Mercado mayorista · MENOS TIEMPO y se ensucia rápido · cuidado con los camiones",
        "Ribera del Shullcas · MÉTETE AL AGUA: la basura baja con la corriente y hay que sacarla",
    };

    private string IntroZona(int z)
    {
        return INTRO_ZONA[Mathf.Clamp(z, 0, INTRO_ZONA.Length - 1)];
    }

    private bool HayCarrosEnZona(int z)
    {
        Contaminante[] carros = FindObjectsOfType<Contaminante>();
        for (int i = 0; i < carros.Length; i++)
            if (carros[i] != null && carros[i].zona == z) return true;
        return false;
    }

    private Vector3 CentroZona(int z)
    {
        Vector3 sum = Vector3.zero; int c = 0;
        for (int i = 0; i < todas.Count; i++)
            if (todas[i] != null && todas[i].zona == z) { sum += todas[i].transform.position; c++; }
        return c > 0 ? sum / c : Vector3.zero;
    }

    private void ReposicionarJugador(Vector3 destino)
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;

        // La herramienta del editor deja un punto "Inicio_z{n}" sobre la VEREDA.
        // Si existe, se usa ese: así el Guardián nunca aparece en medio de la pista.
        GameObject inicio = GameObject.Find("Inicio_z" + zonaActual);
        Vector3 pos;
        if (inicio != null) pos = inicio.transform.position;
        else if (destino != Vector3.zero) pos = new Vector3(destino.x, 1f, destino.z - 6f);
        else return;

        CharacterController cc = p.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        p.transform.position = new Vector3(pos.x, Mathf.Max(1f, pos.y), pos.z);
        if (inicio != null) p.transform.rotation = inicio.transform.rotation;
        if (cc != null) cc.enabled = true;
    }

    /// <summary>Vuelve a la pantalla de inicio (después de limpiar toda la ciudad).</summary>
    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        pausado = false;
        estado = Estado.Menu;
        modoRecorrido = false;
        pista = "";
        aviso = "";
        OnCambio?.Invoke();
    }

    public void TogglePausa()
    {
        if (estado != Estado.Jugando && !pausado) return;
        pausado = !pausado;
        Time.timeScale = pausado ? 0f : 1f;
        OnCambio?.Invoke();
    }

    public void Empezar()      { modoRecorrido = false; nivel = 1; puntaje = 0; racha = 0; mejorRacha = 0; ResetNivel(); estado = Estado.Jugando; OnCambio?.Invoke(); }

    /// <summary>Modo recorrido: pasear por la ciudad sin presión, para presentar el trabajo.</summary>
    public void EmpezarRecorrido()
    {
        modoRecorrido = true;
        nivel = 1;
        puntaje = 0;
        ResetNivel();
        estado = Estado.Jugando;
        Avisar("MODO RECORRIDO · sin tiempo ni contaminación · pulsa C para ensuciar la ciudad",
               new Color(0.62f, 0.90f, 1f), 8f);
        OnCambio?.Invoke();
    }

    /// <summary>
    /// Solo en modo recorrido: sube o baja la contaminación de golpe para mostrar
    /// en vivo cómo cambia el valle, el cielo y el río. Es la demo del mensaje.
    /// </summary>
    public void DemoContaminacion()
    {
        if (!modoRecorrido || estado != Estado.Jugando) return;
        bool sucio = contaminacion > maxContaminacion * 0.5f;
        contaminacion = sucio ? contaminacionInicial : maxContaminacion * 0.92f;
        Avisar(sucio ? "Ciudad limpia: mira cómo se aclara el valle y baja la basura del río"
                     : "Ciudad contaminada: mira la neblina, el sol y lo que baja por el Shullcas",
               sucio ? new Color(0.5f, 1f, 0.7f) : new Color(1f, 0.7f, 0.35f), 5f);
        OnCambio?.Invoke();
    }

    /// <summary>Entrar directo a una zona (selector del menú, útil para mostrar el juego).</summary>
    public void EmpezarEn(int n)
    {
        modoRecorrido = false;
        nivel = Mathf.Clamp(n, 1, Mathf.Max(1, numZonas));
        puntaje = 0; ResetNivel(); estado = Estado.Jugando; OnCambio?.Invoke();
    }

    public void SiguienteNivel(){ modoRecorrido = false; nivel++;     ResetNivel(); estado = Estado.Jugando; OnCambio?.Invoke(); }
    public void ReiniciarNivel(){ modoRecorrido = false; puntaje = 0; racha = 0; ResetNivel(); estado = Estado.Jugando; OnCambio?.Invoke(); }

    void Update()
    {
        if (estado != Estado.Jugando) return;

        // Fase final: la zona está limpia y falta entregar en el punto de acopio.
        if (esperandoAcopio)
        {
            if (Time.time > limiteAcopio) { Terminar(true, false); return; }
            OnCambio?.Invoke();
            return;
        }

        if (!modoRecorrido)
        {
            tiempoRestante -= Time.deltaTime;
            if (tiempoRestante <= 0f) { tiempoRestante = 0f; Terminar(false, true); return; }
        }

        ActualizarTutorial();

        basuraEnSuelo = ContarActivas();
        if (!modoRecorrido && basuraEnSuelo > 0)
        {
            contaminacion = Mathf.Min(maxContaminacion, contaminacion + SubeContaminacion * Time.deltaTime);

            // Alarma una sola vez por nivel al pasar el 80 %: todavía hay tiempo de reaccionar.
            if (!alarmaDada && contaminacion >= maxContaminacion * 0.8f)
            {
                alarmaDada = true;
                Avisar("¡La contaminación pasó el 80 %! Recoge lo que queda en la calle",
                       new Color(1f, 0.42f, 0.34f), 4.5f);
            }

            if (contaminacion >= maxContaminacion) { Terminar(false, false); return; }
        }
        OnCambio?.Invoke();
    }

    /// <summary>
    /// Tutorial de tres pasos, solo en el primer nivel: recoger, leer el color y
    /// entregar bien. No bloquea nada, solo acompaña las primeras acciones para
    /// que alguien que agarra el juego por primera vez entienda la mecánica.
    /// </summary>
    private void ActualizarTutorial()
    {
        if (nivel != 1 || modoRecorrido) { pista = ""; return; }

        switch (pasoTutorial)
        {
            case 0:
                pista = "1 · Camina con W A S D y pasa por encima de un residuo para recogerlo";
                if (cargaActual > 0) pasoTutorial = 1;
                break;

            case 1:
                pista = "2 · Mira el color en tu mochila y busca ESE contenedor en el mapa de la derecha";
                if (aciertos > 0) { pasoTutorial = 2; tutoHasta = Time.unscaledTime + 7f; }
                break;

            case 2:
                pista = "¡Eso es segregar en la fuente! Limpia la zona antes de que suba la contaminación";
                if (Time.unscaledTime > tutoHasta) pasoTutorial = 3;
                break;

            default:
                pista = "";
                break;
        }
    }

    int ContarActivas()
    {
        int n = 0;
        for (int i = 0; i < todas.Count; i++)
            if (todas[i] != null && todas[i].gameObject.activeSelf) n++;
        return n;
    }

    /// <summary>
    /// Un vecino bota un residuo a la calle. Se activa uno de los residuos de
    /// reserva en ese punto y SUBE la meta del nivel: hay que recogerlo también.
    /// Devuelve false si ya se llegó al tope de basura extra del nivel.
    /// </summary>
    public bool TirarBasura(TrashItem t, Vector3 donde)
    {
        if (estado != Estado.Jugando) return false;
        if (t == null || t.gameObject.activeSelf) return false;
        if (extras >= maxExtrasPorNivel) return false;

        t.zona = zonaActual;
        t.transform.position = donde;
        t.transform.rotation = UnityEngine.Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        t.gameObject.SetActive(true);

        extras++;
        basuraTotal++;
        Avisar("Un vecino botó " + Residuo.Nombre(t.tipo).ToLower() + " a la vereda · recógelo",
               new Color(1f, 0.70f, 0.32f), 3f);
        OnCambio?.Invoke();
        return true;
    }

    /// <summary>El Guardián levanta un residuo y se lo lleva en la mochila.</summary>
    public bool RecogerBasura(TipoResiduo t)
    {
        if (estado != Estado.Jugando) return false;
        if (carga.Count >= capacidadCarga)
        {
            Avisar("Mochila llena · lleva los residuos a su contenedor",
                   new Color(1f, 0.72f, 0.3f));
            return false;
        }
        carga.Add(t);
        Avisar(Residuo.Nombre(t) + " → contenedor " + Residuo.ColorNTP(t), Residuo.Tinte(t), 2.2f);
        OnCambio?.Invoke();
        return true;
    }

    /// <summary>
    /// Deposita en el contenedor SOLO los residuos del tipo que acepta.
    /// Devuelve cuántos entraron.
    /// </summary>
    public int Depositar(TipoResiduo acepta) { return Depositar(acepta, true); }

    public int Depositar(TipoResiduo acepta, bool contarError)
    {
        if (estado != Estado.Jugando) return 0;

        int n = 0;
        for (int i = carga.Count - 1; i >= 0; i--)
            if (carga[i] == acepta) { carga.RemoveAt(i); n++; }

        if (n > 0)
        {
            recicladas += n;
            aciertos += n;
            porTipo[Mathf.Clamp((int)acepta, 0, porTipo.Length - 1)] += n;

            // Racha: cada acierto seguido vale mas. El tope de x5 esta puesto a
            // proposito, para que premie la constancia sin que una sola entrega
            // enorme decida la partida.
            racha += n;
            if (racha > mejorRacha) mejorRacha = racha;
            ultimoAcierto = Time.time;
            int multi = Mathf.Clamp(1 + (racha - 1) / 3, 1, 5);

            int ganado = n * 15 * multi;
            puntaje += ganado;
            contaminacion = Mathf.Max(0f, contaminacion - n * 7f);

            string extra = (multi > 1) ? "   RACHA x" + multi : "";
            Avisar("✔ " + n + " de " + Residuo.Nombre(acepta) + " bien segregado   +" + ganado + extra,
                   new Color(0.4f, 1f, 0.6f));

            // El tono sube con la racha: se oye que vas bien sin mirar el HUD.
            float tono = Mathf.Min(1.0f + (racha - 1) * 0.06f, 1.55f);
            GuardianAudio.EnPantalla(GuardianAudio.Acierto, 0.60f, tono, tono + 0.06f);
            if (multi >= 3 && n > 0)
                GuardianAudio.EnPantalla(GuardianAudio.Bonus, 0.45f, 0.98f, 1.06f);
        }
        else if (!contarError)
        {
            // Solo estaba parado al lado: ni aviso ni penalización.
        }
        else if (carga.Count > 0)
        {
            errores++;
            if (racha >= 3)
                Avisar("Se cort\u00f3 la racha de " + racha, new Color(1f, 0.72f, 0.4f), 2f);
            racha = 0;
            GuardianAudio.EnPantalla(GuardianAudio.Error, 0.55f, 0.95f, 1.05f);
            Avisar("✖ Contenedor " + Residuo.ColorNTP(acepta) + " = " + Residuo.Nombre(acepta)
                   + " (" + Residuo.Ejemplos(acepta) + ")", new Color(1f, 0.5f, 0.45f), 3f);

            // El aviso dice cual era el contenedor; la LECCION dice por que
            // importa. Decir solo el color ensena a memorizar colores, y eso es
            // la mitad de lo que pide el ODS 11.
            leccion = Residuo.PorQue(acepta);
            leccionHasta = Time.time + 7.5f;
        }
        else
        {
            Avisar("Contenedor " + Residuo.ColorNTP(acepta) + " · " + Residuo.Nombre(acepta),
                   Residuo.Tinte(acepta), 1.8f);
        }

        OnCambio?.Invoke();
        if (recicladas >= basuraTotal) ZonaLimpia();
        return n;
    }

    // --- Compatibilidad con scripts antiguos (antes no había tipos de residuo) ---
    public bool RecogerBasura() { return RecogerBasura(TipoResiduo.Plastico); }

    public void Depositar()
    {
        if (estado != Estado.Jugando || carga.Count == 0) return;
        for (int i = 0; i < Residuo.TIPOS; i++)
        {
            TipoResiduo t = Residuo.Desde(i);
            if (CuantosLlevo(t) > 0) Depositar(t);
        }
    }

    /// <summary>
    /// Rango del Guardian al terminar. Pesa la SEGREGACION, no el puntaje bruto:
    /// se puede recoger mucho y separar mal, y eso es justo lo que el juego no
    /// debe premiar.
    /// </summary>
    public string Rango()
    {
        int tasa = TasaSegregacion;
        if (tasa >= 95 && errores == 0 && mejorRacha >= 8) return "GUARDIÁN DE ORO DEL SHULLCAS";
        if (tasa >= 85) return "GUARDIÁN DE PLATA";
        if (tasa >= 70) return "GUARDIÁN DE BRONCE";
        if (tasa >= 50) return "PROMOTOR AMBIENTAL";
        return "APRENDIZ DE GUARDIÁN";
    }

    public Color ColorRango()
    {
        int tasa = TasaSegregacion;
        if (tasa >= 95 && errores == 0 && mejorRacha >= 8) return new Color(1f, 0.84f, 0.30f);
        if (tasa >= 85) return new Color(0.86f, 0.90f, 0.96f);
        if (tasa >= 70) return new Color(0.88f, 0.62f, 0.36f);
        if (tasa >= 50) return new Color(0.60f, 0.90f, 0.70f);
        return new Color(0.75f, 0.78f, 0.82f);
    }

    /// <summary>
    /// El vecino recoge lo que acaba de botar porque el Guardian lo alcanzo.
    ///
    /// Vale mas que recogerlo tu: evitar que ensucien es mejor que limpiar
    /// despues, y el juego deberia decirlo con los puntos y no solo con un texto.
    /// </summary>
    public void VecinoRecogeLoSuyo(TrashItem t)
    {
        if (estado != Estado.Jugando || t == null) return;

        t.gameObject.SetActive(false);
        if (t.deReserva && extras > 0)
        {
            extras--;
            basuraTotal = Mathf.Max(recicladas, basuraTotal - 1);
        }

        puntaje += 25;
        contaminacion = Mathf.Max(0f, contaminacion - 6f);
        GuardianAudio.EnPantalla(GuardianAudio.Bonus, 0.55f, 1.02f, 1.10f);
        Avisar("\ud83d\ude4f El vecino recogi\u00f3 lo que bot\u00f3   +25 \u00b7 evitar que ensucien vale m\u00e1s que limpiar",
               new Color(0.5f, 1f, 0.72f), 3.4f);
        OnCambio?.Invoke();
        if (recicladas >= basuraTotal) ZonaLimpia();
    }

    /// <summary>
    /// La limpieza pública municipal (el policía aliado) levanta un residuo que
    /// un vecino acababa de botar a la vereda.
    ///
    /// No cuenta como reciclado del jugador: baja la meta del nivel al valor que
    /// tenía antes de que lo botaran. Así el marcador "recicladas / total" sigue
    /// midiendo SOLO lo que el Guardián segregó con sus manos, que es lo que el
    /// informe y el CSV tienen que poder demostrar. El barrido municipal ayuda a
    /// que la calle no se vuelva imposible, pero no segrega: por eso da apenas
    /// dos puntos y baja poquísimo la contaminación.
    /// </summary>
    public void LimpiezaMunicipal(TrashItem t)
    {
        if (estado != Estado.Jugando) return;

        if (t != null && t.deReserva && extras > 0)
        {
            extras--;
            basuraTotal = Mathf.Max(recicladas, basuraTotal - 1);
        }

        puntaje += 2;
        contaminacion = Mathf.Max(0f, contaminacion - 2f);
        Avisar("🧹 Limpieza municipal recogió lo que botaron · limpiar no reemplaza segregar",
               new Color(0.55f, 0.78f, 1f), 3f);
        OnCambio?.Invoke();
        if (recicladas >= basuraTotal) ZonaLimpia();
    }

    /// <summary>Compatibilidad: versión antigua sin residuo.</summary>
    public void RecicladoPorAliado() { LimpiezaMunicipal(null); }

    /// <summary>
    /// Un carro tuvo que frenar por el Guardián. No le quita vida —el golpe ya
    /// lo hace— pero sí le cuesta puntos y le recuerda dónde se cruza. Seguridad
    /// vial: el juego avisa ANTES del accidente, no después.
    /// </summary>
    public void CasiTeAtropellan()
    {
        if (estado != Estado.Jugando || modoRecorrido) return;
        if (Time.time < proximoSusto) return;
        proximoSusto = Time.time + 6f;

        puntaje = Mathf.Max(0, puntaje - 3);
        Avisar("¡Cuidado! Estás en la pista · cruza por el crucero peatonal",
               new Color(1f, 0.72f, 0.30f), 3f);
        OnCambio?.Invoke();
    }

    private float proximoSusto;
    private bool alarmaDada;

    /// <summary>Aviso disparado desde otro script (camión municipal, etc.).</summary>
    public void AvisoPublico(string texto, Color c, float seg)
    {
        if (estado != Estado.Jugando) return;
        Avisar(texto, c, seg);
        OnCambio?.Invoke();
    }

    /// <summary>
    /// Ya no queda basura en la zona. En vez de terminar de golpe, el nivel pide
    /// el último paso real del ciclo: llevar lo recogido al punto de acopio.
    /// </summary>
    private void ZonaLimpia()
    {
        if (modoRecorrido || esperandoAcopio || estado != Estado.Jugando) return;

        esperandoAcopio = true;
        limiteAcopio = Time.time + 80f;      // red de seguridad: cierra solo
        Avisar("¡Zona limpia! Ahora lleva la jornada al PUNTO DE ACOPIO (míralo en el mapa)",
               new Color(0.55f, 1f, 0.78f), 7f);
        OnCambio?.Invoke();
    }

    /// <summary>El Guardián llegó al punto de acopio y cierra la jornada.</summary>
    public void CerrarJornada()
    {
        if (!esperandoAcopio || estado != Estado.Jugando) return;
        puntaje += 50;
        GuardianAudio.EnPantalla(GuardianAudio.Acopio, 0.75f);
        Avisar("Entregado en el punto de acopio · +50", new Color(0.5f, 1f, 0.72f), 3f);
        Terminar(true, false);
    }

    /// <summary>
    /// El Guardián está parado en la pista con el semáforo en verde para los
    /// autos. No le quita vida —para eso está el golpe— pero sí puntos, y le
    /// recuerda la regla. Cruzar con el peatonal en verde no penaliza nada.
    /// </summary>
    public void CruceIndebido()
    {
        if (estado != Estado.Jugando || modoRecorrido) return;

        puntaje = Mathf.Max(0, puntaje - 5);
        Avisar("Estás cruzando con el semáforo en VERDE para los autos · espera tu luz",
               new Color(1f, 0.55f, 0.30f), 3.5f);
        OnCambio?.Invoke();
    }

    // ------------------------------------------------------------ API para ENEMIGOS

    /// <summary>
    /// La Rata Basurera le arranca UN residuo de la mochila y lo tira en 'donde'.
    /// Se reactiva el mismo objeto que se había recogido, así las cuentas del
    /// nivel (basuraTotal / recicladas) no se rompen: hay que volver a recogerlo.
    /// </summary>
    public bool RobarResiduo(Vector3 donde, out TipoResiduo robado)
    {
        robado = TipoResiduo.Plastico;
        if (estado != Estado.Jugando || carga.Count == 0) return false;

        for (int k = carga.Count - 1; k >= 0; k--)
        {
            TipoResiduo t = carga[k];
            for (int i = 0; i < todas.Count; i++)
            {
                TrashItem ti = todas[i];
                if (ti == null || ti.gameObject.activeSelf || ti.deReserva) continue;
                if (ti.tipo != t || (!modoRecorrido && ti.zona != zonaActual)) continue;

                carga.RemoveAt(k);
                ti.transform.position = donde;
                ti.gameObject.SetActive(true);
                robado = t;
                racha = 0;
                Avisar("¡Una rata te robó " + Residuo.Nombre(t).ToLower() + "! Recógelo otra vez",
                       new Color(1f, 0.6f, 0.35f), 3f);
                OnCambio?.Invoke();
                return true;
            }
        }
        return false;
    }

    /// <summary>Suma (o resta) puntos con un aviso: enemigos derrotados, jefe, etc.</summary>
    public void SumarPuntos(int pts, string texto, Color c)
    {
        if (estado != Estado.Jugando) return;
        puntaje = Mathf.Max(0, puntaje + pts);
        if (!string.IsNullOrEmpty(texto)) Avisar(texto, c, 3f);
        OnCambio?.Invoke();
    }

    /// <summary>Sube o baja la contaminación desde un enemigo (humo tóxico, jefe…).</summary>
    public void SumarContaminacion(float cantidad)
    {
        if (estado != Estado.Jugando || modoRecorrido) return;
        contaminacion = Mathf.Clamp(contaminacion + cantidad, 0f, maxContaminacion);
        if (contaminacion >= maxContaminacion) Terminar(false, false);
    }

    public void PerderVida()
    {
        if (estado != Estado.Jugando || modoRecorrido) return;
        vidas--;
        GuardianAudio.EnPantalla(GuardianAudio.Vida, 0.75f, 0.90f, 1.05f);
        contaminacion = Mathf.Min(maxContaminacion, contaminacion + 8f);
        Avisar("¡Cuidado con el tráfico!", new Color(1f, 0.5f, 0.45f), 2f);
        OnCambio?.Invoke();
        if (vidas <= 0) Terminar(false, false);
    }

    void Terminar(bool gano, bool porTiempo)
    {
        Time.timeScale = 1f;
        pausado = false;
        perdioPorTiempo = porTiempo;
        esperandoAcopio = false;
        estado = gano ? Estado.Ganado : Estado.Perdido;
        if (gano)
        {
            puntaje += Mathf.RoundToInt(tiempoRestante) + TasaSegregacion;
            if (Perfecta) puntaje += 50;          // ni un solo error de contenedor
        }

        GuardianAudio.EnPantalla(gano ? GuardianAudio.Ganaste : GuardianAudio.Perdiste, 0.75f);
        GuardianEventos.AvisarFin(gano);
        if (gano && Perfecta) GuardianAudio.EnPantalla(GuardianAudio.Bonus, 0.55f);
        if (puntaje > PlayerPrefs.GetInt("guardian_record", 0))
            PlayerPrefs.SetInt("guardian_record", puntaje);

        if (gano)
        {
            // Se guarda la MEJOR tasa de segregación y hasta qué nivel llegó:
            // son los dos indicadores que sirven para el informe del curso.
            if (TasaSegregacion > PlayerPrefs.GetInt("guardian_tasa", 0))
                PlayerPrefs.SetInt("guardian_tasa", TasaSegregacion);
            if (nivel > PlayerPrefs.GetInt("guardian_nivel", 1))
                PlayerPrefs.SetInt("guardian_nivel", nivel);
            PlayerPrefs.Save();
        }

        GuardarRegistro(gano);
        OnCambio?.Invoke();
    }

    /// <summary>
    /// Guarda una línea por partida en un CSV. Sirve como evidencia de las
    /// pruebas para el informe del curso: fecha, zona, puntaje y tasa de
    /// segregación de cada intento.
    /// </summary>
    private void GuardarRegistro(bool gano)
    {
        try
        {
            string ruta = System.IO.Path.Combine(Application.persistentDataPath,
                                                 "guardian_resultados.csv");
            bool nuevo = !System.IO.File.Exists(ruta);

            using (System.IO.StreamWriter w = new System.IO.StreamWriter(ruta, true))
            {
                if (nuevo)
                    w.WriteLine("fecha;nivel;zona;resultado;puntaje;recicladas;meta;aciertos;errores;tasa");

                w.WriteLine(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ";" +
                            nivel + ";" + ZonaNombre + ";" + (gano ? "ganado" : "perdido") + ";" +
                            puntaje + ";" + recicladas + ";" + basuraTotal + ";" +
                            aciertos + ";" + errores + ";" + TasaSegregacion + "%");
            }
            ultimoRegistro = ruta;
        }
        catch { ultimoRegistro = ""; }
    }
}
