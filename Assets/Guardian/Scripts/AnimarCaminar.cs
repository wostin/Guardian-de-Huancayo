using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Deja a un NPC con la animación de caminar puesta.
/// Se usa para el reciclador que empuja su triciclo: el objeto que lo mueve es el
/// triciclo, así que el personaje solo necesita mover las piernas.
/// </summary>
public class AnimarCaminar : MonoBehaviour
{
    [Range(0.3f, 1.5f)] public float velocidadAnimacion = 0.85f;

    void Start()
    {
        Animator a = GetComponentInChildren<Animator>();
        if (a == null || a.runtimeAnimatorController == null) return;

        foreach (AnimationClip c in a.runtimeAnimatorController.animationClips)
        {
            if (c == null) continue;
            string n = c.name.ToLower();
            if (n.Contains("walk") || n.Contains("jog"))
            {
                a.CrossFadeInFixedTime(c.name, 0.2f);
                a.speed = velocidadAnimacion;
                return;
            }
        }
    }
}
