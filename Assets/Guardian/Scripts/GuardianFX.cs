using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Efectos rápidos (partículas + sonido) sin assets externos.
/// </summary>
public static class GuardianFX
{
    public static void Efecto(Vector3 pos, Color color, float freq = 660f)
    {
        Particulas(pos, color);
        Sonido(pos, freq);
    }

    /// <summary>
    /// Capa base de chispas (círculo suave, transparente, con desvanecido). Antes
    /// eran cuadrados opacos; ahora usa la biblioteca GuardianParticulas y las
    /// capas grandes (confeti, anillos, humo) las agrega GuardianVFX.
    /// </summary>
    private static void Particulas(Vector3 pos, Color color)
    {
        GuardianParticulas.Rafaga(pos + Vector3.up * 0.4f, color, Color.Lerp(color, Color.white, 0.5f),
                                  16, 3.2f, 0.6f, 0.22f, 0.4f);
    }

    /// <summary>
    /// Antes esto armaba un AudioClip NUEVO en cada llamada —miles de floats y un
    /// objeto por cada residuo recogido— y siempre con el mismo tono exacto, que
    /// después de veinte recojos suena a metrónomo. Ahora el clip se pide al banco
    /// (que lo guarda) y se reproduce con el tono movido al azar y en 3D.
    /// </summary>
    private static void Sonido(Vector3 pos, float freq)
    {
        GuardianAudio.EnPunto(GuardianAudio.Pitido(freq), pos, 0.6f, 0.90f, 1.12f, 28f);
    }

    /// <summary>Partículas y un sonido concreto del banco, en vez de un pitido.</summary>
    public static void Efecto(Vector3 pos, Color color, AudioClip clip, float volumen = 0.75f)
    {
        Particulas(pos, color);
        GuardianAudio.EnPunto(clip, pos, volumen, 0.94f, 1.06f, 30f);
    }
}
