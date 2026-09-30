using System;
using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Central de eventos de juego.
///
/// Los scripts de juego (TrashItem, RecycleBin, GameManager, Contaminante) solo
/// AVISAN lo que pasó; los sistemas de retroalimentación (VFX, animaciones,
/// textos flotantes, sacudida de cámara, mezcla de audio) se suscriben aquí.
/// Así la lógica no depende de los efectos y cada efecto se puede apagar sin
/// romper el juego (patrón Observer).
/// </summary>
public static class GuardianEventos
{
    /// <summary>El Guardián levantó un residuo (posición del residuo, tipo).</summary>
    public static event Action<Vector3, TipoResiduo> Recogio;
    /// <summary>Depósito correcto (posición del contenedor, tipo, cuántos, puntos).</summary>
    public static event Action<Vector3, TipoResiduo, int, int> Deposito;
    /// <summary>Se equivocó de contenedor (posición del contenedor, tipo del contenedor).</summary>
    public static event Action<Vector3, TipoResiduo> Error;
    /// <summary>Un carro golpeó al Guardián (posición del golpe).</summary>
    public static event Action<Vector3> Golpe;
    /// <summary>Terminó el nivel (true = ganó).</summary>
    public static event Action<bool> FinNivel;
    /// <summary>Mochila llena al intentar recoger.</summary>
    public static event Action<Vector3> MochilaLlena;

    public static void AvisarRecogio(Vector3 p, TipoResiduo t)            { Recogio?.Invoke(p, t); }
    public static void AvisarDeposito(Vector3 p, TipoResiduo t, int n, int pts) { Deposito?.Invoke(p, t, n, pts); }
    public static void AvisarError(Vector3 p, TipoResiduo t)              { Error?.Invoke(p, t); }
    public static void AvisarGolpe(Vector3 p)                             { Golpe?.Invoke(p); }
    public static void AvisarFin(bool gano)                               { FinNivel?.Invoke(gano); }
    public static void AvisarMochilaLlena(Vector3 p)                      { MochilaLlena?.Invoke(p); }

    /// <summary>Limpia suscriptores al entrar a Play (Enter Play Mode sin recargar dominio).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Limpiar()
    {
        Recogio = null; Deposito = null; Error = null; Golpe = null; FinNivel = null; MochilaLlena = null;
    }
}
