using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Regla de cruce peatonal.
/// Si el Guardián se mete a la pista de un cruce mientras el semáforo está en
/// VERDE PARA LOS AUTOS, el juego se lo advierte y le cuesta puntos; si cruza
/// con el semáforo en rojo para los autos (verde peatonal), no pasa nada.
/// Es la otra mitad del ODS 11 que se trabaja acá: una ciudad sostenible también
/// es una ciudad en la que se puede caminar sin que te atropellen.
/// Da 2 segundos de gracia antes de penalizar: entrar y salir no cuenta.
/// </summary>
public class SeguridadVial : MonoBehaviour
{
    public float radioSemaforo = 17f;
    public float graciaSegundos = 2f;
    public float esperaEntreAvisos = 9f;

    private Transform jugador;
    private Semaforo[] semaforos;
    private float enRojoDesde = -1f;
    private float proximoAviso;
    private float proximaRevision;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) jugador = p.transform;
        semaforos = FindObjectsByType<Semaforo>(FindObjectsSortMode.None);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.estado != GameManager.Estado.Jugando || gm.modoRecorrido) return;
        if (jugador == null || RedVial.Instancia == null) return;

        if (Time.time < proximaRevision) return;
        proximaRevision = Time.time + 0.25f;

        // ¿Está sobre el asfalto?
        if (!RedVial.Instancia.SobreLaPista(jugador.position)) { enRojoDesde = -1f; return; }

        // ¿Hay un semáforo cerca y en verde para los autos?
        Semaforo cerca = MasCercano();
        if (cerca == null || cerca.PasoPeatonal) { enRojoDesde = -1f; return; }

        if (enRojoDesde < 0f) { enRojoDesde = Time.time; return; }
        if (Time.time - enRojoDesde < graciaSegundos) return;
        if (Time.time < proximoAviso) return;

        proximoAviso = Time.time + esperaEntreAvisos;
        enRojoDesde = Time.time;
        gm.CruceIndebido();
    }

    private Semaforo MasCercano()
    {
        if (semaforos == null) return null;
        Semaforo mejor = null;
        float d2 = radioSemaforo * radioSemaforo;

        for (int i = 0; i < semaforos.Length; i++)
        {
            Semaforo s = semaforos[i];
            if (s == null) continue;
            Vector3 d = s.transform.position - jugador.position; d.y = 0f;
            float m = d.sqrMagnitude;
            if (m < d2) { d2 = m; mejor = s; }
        }
        return mejor;
    }
}
