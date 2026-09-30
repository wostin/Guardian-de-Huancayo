# Guardián de Huancayo

Videojuego educativo 3D en **Unity 6 (URP)** sobre la segregación de residuos sólidos en Huancayo, Perú.
Alineado al **ODS 11 · Ciudades y Comunidades Sostenibles**.

> Curso: Desarrollo de Videojuegos · Universidad Continental · Prof. Guillermo Peña García
> Desarrollado por: **Jhovani Jumpa Fierro**, estudiante de la Universidad Continental

## De qué trata
La ciudad se llena de basura y el río Shullcas se contamina. El **Guardián** recoge los residuos y los lleva al contenedor de su color, según la **NTP 900.058-2019**, antes de que la contaminación llegue al 100 %.

| Color | Residuo |
|---|---|
| Blanco | Plástico |
| Verde | Vidrio |
| Azul | Papel y cartón |
| Amarillo | Metales |
| Marrón | Orgánicos |

## Zonas
1. **Plaza Constitución**: zona peatonal donde se aprende a segregar.
2. **Mercado Mayorista El Tambo**: menos tiempo, más basura y tráfico.
3. **Ribera del Shullcas**: basura en el río, humo tóxico y el jefe.

## Enemigos
| Enemigo | Mecánica | Cómo se vence |
|---|---|---|
| Rata Basurera | Te persigue y te roba un residuo de la mochila | Saltar cerca (+10) o correr |
| Humo Tóxico | Te frena y sube la contaminación | Segregar bien cerca (+15) |
| Rey Basurón (jefe) | Lanza bolsas (sombra roja de aviso) y da pisotones | Cada residuo bien segregado le quita vida (+150) |
| Carros y mototaxis | Golpe = −1 vida | Cruzar por el crucero |

## Controles
| PC | Celular | Acción |
|---|---|---|
| W A S D | Joystick izquierdo | Mover |
| Shift | CORRER / joystick al borde | Correr |
| Espacio | SALTAR | Saltar / espantar ratas |
| Mouse | Arrastrar a la derecha | Mirar |
| 1 · 2 · 3 · V | CÁM | Cámara 1ª persona · aérea · 3ª persona |
| G | — | Flechas guía |
| TAB | MAPA | Mapa grande |
| F8 | — | Flujo de señal de audio |
| ESC / P | II | Pausa y mezclador |

## Sistemas técnicos
- Cámaras en 1ª persona, aérea y 3ª persona, con sacudida y FOV dinámico.
- Flechas guía 3D hacia la basura, el contenedor y el punto de acopio.
- IA de tráfico con *pure pursuit*, frenado progresivo y semáforos.
- Iluminación URP con sombras suaves, luz de relleno y post-proceso (la ciudad pierde color con la contaminación).
- VFX de retroalimentación: chispas, confeti, humo, polvo y fuegos artificiales.
- Flujo de señal de audio por buses (Música, Ambiente, Efectos 3D, Interfaz) → Master, con *ducking* y pasa-bajos en pausa.
- Controles táctiles para celular (versión Web).
- Eventos de gameplay con patrón Observer (`GuardianEventos`).

## Qué hay en este repositorio
Solo lo que el juego necesita para abrir y jugar:
- `Assets/Guardian/`: todo el código propio (Scripts y Editor), materiales, imágenes y documentación.
- `Assets/SimplePoly City - Low Poly Assets/Demo/`: la escena principal del juego.
- Los modelos, texturas, materiales y sonidos de los packs gratuitos de la Asset Store **que la escena usa** (lo que no se usa no se sube).
- `ProjectSettings/` y `Packages/`: configuración del proyecto (Unity 6, URP, Input System).

## Cómo abrirlo
1. Clonar el repositorio (o *Code → Download ZIP*) y abrir la carpeta con Unity **6000.5.9f1** o superior desde Unity Hub (*Add project from disk*).
2. Abrir la escena `Assets/SimplePoly City - Low Poly Assets/Demo/SimplePoly City - Low Poly Assets_Demo Scene` y darle Play.

Para jugarlo sin instalar nada, usar el enlace de Unity Play.

## Enlaces
- Juego en Unity Play: *(pegar enlace)*
- GDD: *(pegar enlace)*
- Trello: *(pegar enlace)*
