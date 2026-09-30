# Segregación de residuos en *Guardián de Huancayo*

**Proyecto:** Guardián de Huancayo — videojuego educativo 3D (Unity 6, URP)
**Curso:** Desarrollo de Videojuegos · Universidad Continental
**ODS:** 11 — Ciudades y Comunidades Sostenibles
**Meta trabajada:** 11.6 — *reducir el impacto ambiental negativo per cápita de las
ciudades, prestando especial atención a la calidad del aire y la gestión de los
desechos municipales.*

---

## 1. Por qué la segregación es la mecánica central

El problema que retrata el juego es el arrojo de residuos sólidos en la ciudad de
Huancayo y en las orillas del **río Shullcas**, que nace en el nevado Huaytapallana y
atraviesa la ciudad. Recoger basura, por sí solo, no enseña nada: lo que cambia el
destino de un residuo es **separarlo en la fuente**. Un residuo bien segregado puede
reaprovecharse; uno mezclado termina en el botadero o en el río.

Por eso en el juego **no basta con recoger**: cada residuo solo entra en el
contenedor de **su color**.

## 2. Código de colores (NTP 900.058-2019)

La Norma Técnica Peruana **NTP 900.058-2019 — *Gestión de residuos. Código de colores
para el almacenamiento de residuos sólidos*** define qué color de contenedor
corresponde a cada tipo de residuo. El juego usa los cinco colores de residuos
aprovechables:

| Color del contenedor | Tipo de residuo | Ejemplos en el juego |
|---|---|---|
| **Blanco** | Plástico | botellas PET, bidones, galoneras |
| **Verde** | Vidrio | botellas y frascos de vidrio |
| **Azul** | Papel y cartón | cajas, cartones doblados, cajas de pizza |
| **Amarillo** | Metales | latas de gaseosa, latas de conserva |
| **Marrón** | Orgánicos | restos de fruta y verdura del mercado |

> La norma contempla además **negro** (residuos generales no aprovechables) y **rojo**
> (residuos peligrosos). No se usan en el juego para no alargar el nivel; si los
> mencionas en el informe, cita la versión vigente de la norma.

## 3. Cómo funciona dentro del juego

1. El Guardián **recoge** un residuo al tocarlo. Se guarda en la *mochila* (máximo 6) y
   el HUD muestra una ficha del color que le corresponde.
2. Los **5 contenedores de cada zona están repartidos** alrededor de ella: hay que
   reconocer el color y caminar hasta el correcto.
3. Al llegar a un contenedor, **solo entran los residuos de su tipo**:
   - acierto → **+15 puntos** por residuo y **baja la contaminación**;
   - contenedor equivocado → no acepta nada y el cartel recuerda qué va ahí
     (se cuenta como error).
4. Al terminar el nivel, la pantalla de resultados muestra la **tasa de segregación
   correcta** (`aciertos / (aciertos + errores)`), que es el indicador de aprendizaje
   del juego.
5. Mientras quede basura en el suelo la **contaminación sube**; si llega a 100 se pierde
   el nivel, y el agua del río Shullcas se ve cada vez más turbia (`AguaRio.cs`).

## 4. Dónde está cada cosa en el proyecto

| Archivo | Qué hace |
|---|---|
| `Scripts/Residuo.cs` | Enum `TipoResiduo` + nombres, colores y ejemplos de la norma |
| `Scripts/TrashItem.cs` | Residuo recogible; guarda su tipo y su zona |
| `Scripts/RecycleBin.cs` | Contenedor; acepta un único `TipoResiduo` |
| `Scripts/GameManager.cs` | Mochila, puntaje, aciertos/errores, tasa de segregación |
| `Scripts/GuardianCameraHUD.cs` | HUD, tabla de colores, resultados y créditos |
| `Editor/GuardianSetupEditor.cs` | Genera los residuos por tipo y los 5 contenedores con su cartel |

## 5. Indicadores que puedes reportar

- **Tasa de segregación correcta (%)** por nivel — mide el aprendizaje del jugador.
- **Residuos reaprovechados** frente a residuos que quedaron en el suelo.
- **Nivel de contaminación final** del río Shullcas.
