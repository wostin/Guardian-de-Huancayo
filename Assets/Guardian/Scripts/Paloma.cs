using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Paloma de la plaza.
/// Da saltitos picoteando el suelo y, cuando alguien se le acerca, levanta vuelo
/// en parábola y se posa unos metros más allá. Es pura ambientación —no toca la
/// jugabilidad— pero es lo que hace que la Plaza Constitución se sienta viva.
/// </summary>
public class Paloma : MonoBehaviour
{
    public float radio = 4.5f;          // qué tan lejos deambula de su sitio
    public float velocidad = 1.0f;
    public float distanciaSusto = 5f;

    private Vector3 casa, destino, origen;
    private float sueloY, espera, tVuelo, durVuelo, fase;
    private bool volando;
    private Transform jugador;
    private Transform alaIzq, alaDer;

    void Start()
    {
        casa = transform.position;
        sueloY = casa.y;
        fase = Random.Range(0f, 10f);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) jugador = p.transform;

        alaIzq = transform.Find("AlaIzq");
        alaDer = transform.Find("AlaDer");

        NuevoDestino();
        espera = Random.Range(0f, 2f);
    }

    private void NuevoDestino()
    {
        Vector2 r = Random.insideUnitCircle * radio;
        destino = new Vector3(casa.x + r.x, sueloY, casa.z + r.y);
    }

    private void Despegar(Vector3 desdeDondeViene)
    {
        volando = true;
        origen = transform.position;

        Vector3 huir = desdeDondeViene.sqrMagnitude > 0.01f
            ? -desdeDondeViene.normalized : Random.insideUnitSphere;
        huir.y = 0f;
        if (huir.sqrMagnitude < 0.01f) huir = Vector3.forward;
        huir.Normalize();

        Vector2 r = Random.insideUnitCircle * 4f;
        destino = new Vector3(origen.x + huir.x * 9f + r.x, sueloY,
                              origen.z + huir.z * 9f + r.y);
        casa = destino;                                   // se muda a donde cayó
        durVuelo = Mathf.Max(0.8f, Vector3.Distance(origen, destino) / 9f);
        tVuelo = 0f;
    }

    void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.estado != GameManager.Estado.Jugando) return;

        if (volando) { Volar(); return; }

        // ¿Se le acercó alguien?
        if (jugador != null)
        {
            Vector3 dj = jugador.position - transform.position; dj.y = 0f;
            if (dj.sqrMagnitude < distanciaSusto * distanciaSusto) { Despegar(dj); return; }
        }

        Alas(0f);

        if (espera > 0f)
        {
            espera -= Time.deltaTime;
            // Picoteo: baja y sube la cabeza.
            float pic = Mathf.Sin((Time.time + fase) * 6f);
            transform.localRotation = Quaternion.Euler(pic > 0.6f ? 22f : 0f,
                                                       transform.localEulerAngles.y, 0f);
            return;
        }

        Vector3 d = destino - transform.position; d.y = 0f;
        if (d.magnitude < 0.25f)
        {
            espera = Random.Range(1.2f, 3.5f);
            NuevoDestino();
            return;
        }

        // Saltitos: avanza con un rebote corto.
        Vector3 pos = transform.position + d.normalized * velocidad * Time.deltaTime;
        pos.y = sueloY + Mathf.Abs(Mathf.Sin((Time.time + fase) * 9f)) * 0.07f;
        transform.position = pos;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(d), 6f * Time.deltaTime);
    }

    private void Volar()
    {
        tVuelo += Time.deltaTime;
        float k = Mathf.Clamp01(tVuelo / durVuelo);

        Vector3 p = Vector3.Lerp(origen, destino, k);
        p.y = sueloY + Mathf.Sin(k * Mathf.PI) * 3.2f;
        transform.position = p;

        Vector3 mira = destino - origen; mira.y = 0f;
        if (mira.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(mira);

        Alas(Mathf.Sin(Time.time * 26f) * 48f);

        if (k >= 1f)
        {
            volando = false;
            Alas(0f);
            espera = Random.Range(0.6f, 1.8f);
            NuevoDestino();
        }
    }

    private void Alas(float grados)
    {
        if (alaIzq != null) alaIzq.localRotation = Quaternion.Euler(0f, 0f, -grados);
        if (alaDer != null) alaDer.localRotation = Quaternion.Euler(0f, 0f,  grados);
    }
}
