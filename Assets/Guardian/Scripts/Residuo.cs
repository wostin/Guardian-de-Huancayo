using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Tipos de residuo sólido.
/// Sigue el código de colores de la NTP 900.058-2019 (Norma Técnica Peruana)
/// que usan los municipios del Perú para la segregación en la fuente.
/// ODS 11.6: reducir el impacto ambiental de las ciudades mejorando la
/// gestión de los desechos municipales.
/// </summary>
public enum TipoResiduo
{
    Plastico = 0,   // BLANCO
    Vidrio   = 1,   // VERDE
    Papel    = 2,   // AZUL
    Metal    = 3,   // AMARILLO
    Organico = 4    // MARRÓN
}

public static class Residuo
{
    public const int TIPOS = 5;

    public static TipoResiduo Desde(int i)
    {
        return (TipoResiduo)Mathf.Clamp(i, 0, TIPOS - 1);
    }

    public static string Nombre(TipoResiduo t)
    {
        switch (t)
        {
            case TipoResiduo.Plastico: return "PLÁSTICO";
            case TipoResiduo.Vidrio:   return "VIDRIO";
            case TipoResiduo.Papel:    return "PAPEL Y CARTÓN";
            case TipoResiduo.Metal:    return "METALES";
            default:                   return "ORGÁNICOS";
        }
    }

    /// <summary>Color del contenedor según la NTP 900.058-2019.</summary>
    public static string ColorNTP(TipoResiduo t)
    {
        switch (t)
        {
            case TipoResiduo.Plastico: return "BLANCO";
            case TipoResiduo.Vidrio:   return "VERDE";
            case TipoResiduo.Papel:    return "AZUL";
            case TipoResiduo.Metal:    return "AMARILLO";
            default:                   return "MARRÓN";
        }
    }

    /// <summary>Color para pintar el HUD y las etiquetas de los contenedores.</summary>
    public static Color Tinte(TipoResiduo t)
    {
        switch (t)
        {
            case TipoResiduo.Plastico: return new Color(0.94f, 0.94f, 0.94f);
            case TipoResiduo.Vidrio:   return new Color(0.20f, 0.72f, 0.33f);
            case TipoResiduo.Papel:    return new Color(0.18f, 0.48f, 0.85f);
            case TipoResiduo.Metal:    return new Color(0.96f, 0.78f, 0.13f);
            default:                   return new Color(0.55f, 0.36f, 0.20f);
        }
    }

    /// <summary>
    /// POR QUE importa ese contenedor y no otro.
    ///
    /// Hasta ahora, al equivocarse, el juego decia cual era el contenedor
    /// correcto y nada mas. Eso ensena a memorizar colores, que es la mitad de la
    /// leccion. Lo que hay detras de la NTP 900.058-2019 no es el color: es que
    /// un residuo mezclado deja de ser aprovechable y termina en el botadero o en
    /// el rio. Esa frase es la que conviene que quede.
    /// </summary>
    public static string PorQue(TipoResiduo t)
    {
        switch (t)
        {
            case TipoResiduo.Plastico:
                return "El plástico limpio se muele y vuelve como tubería o fibra. "
                     + "Con restos de comida encima ya nadie lo compra.";
            case TipoResiduo.Vidrio:
                return "Un vidrio roto entre el papel arruina toda la paca "
                     + "y corta las manos del reciclador que la abre.";
            case TipoResiduo.Papel:
                return "El papel mojado o grasoso pierde la fibra: "
                     + "si se moja en el camión, se va entero al botadero.";
            case TipoResiduo.Metal:
                return "La lata se refunde una y otra vez sin perder calidad. "
                     + "Tirada al Shullcas se queda ahí veinte años oxidándose.";
            default:
                return "Los restos de comida hacen compost en semanas. "
                     + "Enterrados con el resto producen metano y lixiviados.";
        }
    }

    /// <summary>Consejo corto para el aviso de error, sin ocupar tres renglones.</summary>
    public static string Clave(TipoResiduo t)
    {
        switch (t)
        {
            case TipoResiduo.Plastico: return "enjuágalo antes de botarlo";
            case TipoResiduo.Vidrio:   return "entero y separado del papel";
            case TipoResiduo.Papel:    return "seco, o no sirve";
            case TipoResiduo.Metal:    return "se recicla infinitas veces";
            default:                   return "van a compost, no al relleno";
        }
    }

    public static string Ejemplos(TipoResiduo t)
    {
        switch (t)
        {
            case TipoResiduo.Plastico: return "botellas, bidones, bolsas";
            case TipoResiduo.Vidrio:   return "botellas y frascos de vidrio";
            case TipoResiduo.Papel:    return "cajas, cartón, periódico";
            case TipoResiduo.Metal:    return "latas de gaseosa y conservas";
            default:                   return "restos de frutas y verduras";
        }
    }
}
