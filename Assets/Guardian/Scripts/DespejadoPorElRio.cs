using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Marca en una construcción que se apartó porque quedaba
/// dentro del cauce del Shullcas.
///
/// Sirve para poder deshacerlo. Sin esta marca, si el río se recalcula y cambia
/// de sitio, las casas que se apagaron en el cálculo anterior se quedarían
/// apagadas para siempre y la ciudad iría llenándose de huecos. Con la marca, lo
/// primero que hace la herramienta antes de recalcular el cauce es devolver a su
/// sitio todo lo que había apartado.
/// </summary>
public class DespejadoPorElRio : MonoBehaviour
{
}
