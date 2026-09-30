# Guardián de Huancayo — Estructura del proyecto

Videojuego educativo 3D (Unity 6, URP) sobre el **ODS 11: Ciudades y Comunidades
Sostenibles**, enfocado en los residuos sólidos y la contaminación del **río Shullcas**
en Huancayo. El jugador es un Guardián que recoge los residuos de la ciudad y los
**segrega en el contenedor del color que les corresponde** según la NTP 900.058-2019,
antes de que se acabe el tiempo o la contaminación llegue al máximo.

> La mecánica de segregación está explicada a detalle en **`SEGREGACION_NTP.md`**
> (mismo folder). Ese documento es el que conviene citar en el informe del curso.

## Carpetas del proyecto

```
Assets/
├── Guardian/                 <- TODO lo propio del juego
│   ├── Scripts/              Código del juego (runtime)
│   ├── Editor/               Herramientas del menú "Tools > Guardián de Huancayo"
│   ├── Prefabs/              Objetos reutilizables propios
│   ├── Materiales/           Materiales propios (ej. Agua_Shullcas.mat)
│   ├── Imagenes/             Capturas, íconos y texturas propias
│   └── Documentacion/        Este documento y el informe del curso
│
├── SimplePoly City - Low Poly Assets/   Ciudad (pack importado)
├── DenysAlmaral/CityPeople/             Personajes (pack importado)
├── Hash Game studios/Trash Bag & ...    Basura y contenedores (pack importado)
└── npc_casual_set_00/                   Peatones NPC (pack importado)
```

Los packs importados **no se mueven de sitio**: las herramientas del editor los
buscan por su ruta original.

## Scripts (Assets/Guardian/Scripts)

| Script | Qué hace |
|---|---|
| `GameManager.cs` | Cerebro del juego: estados (menú, jugando, ganado, perdido), niveles por zona, tiempo, vidas, puntaje, contaminación y récord. |
| `PlayerController.cs` | Movimiento del Guardián con WASD y animación de caminar. |
| `GuardianCameraHUD.cs` | Cámara que sigue y orbita con el mouse + todo el HUD y los menús (inicio, pausa, fin). |
| `Residuo.cs` | Tipos de residuo (plástico, vidrio, papel, metal, orgánico) con su color, nombre y ejemplos según la **NTP 900.058-2019**. |
| `TrashItem.cs` | Un residuo recogible. Guarda su **tipo** y a qué **zona** pertenece. |
| `RecycleBin.cs` | Contenedor de un solo tipo: acepta lo que le corresponde y rechaza (enseñando) lo demás. |
| `AguaRio.cs` | Agua del Shullcas: corre, ondea y **se ensucia** cuando sube la contaminación. |
| `Contaminante.cs` | Carro enemigo. Recorre una **ruta fija** dentro de su zona y quita vida al chocar. |
| `RoadNetwork.cs` | Detecta las calles del pack para que los carros circulen por la vía. |
| `Semaforo.cs` | Semáforos funcionales; los carros se detienen en rojo. |
| `Aliado.cs` | Policías que ayudan a recoger basura. |
| `Peaton.cs` | Peatones NPC ambientales. |
| `GuardianFX.cs` | Partículas y sonidos al recoger y reciclar. |
| `GuardianMusic.cs` | Música de fondo generada por código. |
| `UIManager.cs` | Utilidades de interfaz. |

## Niveles por zonas

La ciudad se divide en 3 zonas y **cada nivel se juega en una zona distinta**, con
menos tiempo y más contaminación a medida que avanzas:

| Nivel | Zona | Tráfico |
|---|---|---|
| 1 | Centro de la ciudad | **Zona peatonal** (sin carros) |
| 2 | Barrio / Mercado | Con tráfico |
| 3 | Ribera del Shullcas | Con tráfico |

Al empezar un nivel solo se activa la basura de esa zona y el Guardián es llevado
allí. Los carros pertenecen a su zona y solo aparecen y circulan en ella, siguiendo
un **circuito fijo y predecible** por las calles de la zona.

## Segregación por colores (NTP 900.058-2019)

Cada zona tiene **5 contenedores repartidos a su alrededor**, con su cartel:

| Color | Tipo | Ejemplos en el juego |
|---|---|---|
| Blanco | Plástico | botellas PET, bidones, galoneras |
| Verde | Vidrio | botellas y frascos de vidrio |
| Azul | Papel y cartón | cajas de pizza, cartones |
| Amarillo | Metales | latas de gaseosa y de conserva |
| Marrón | Orgánicos | restos de fruta y verdura del mercado |

Acertar el contenedor da **+15 puntos** y baja la contaminación; equivocarse no
rompe nada, pero cuenta como error y baja la **tasa de segregación** que se muestra
al final del nivel. El HUD marca en pantalla, con su color y su distancia en metros,
el contenedor que hace falta para lo que llevas en la mochila.

## Herramientas del editor

Menú **Tools > Guardián de Huancayo**:

- **★ ARMAR JUEGO COMPLETO** — reconstruye todo el nivel de una vez (jugador, basura,
  contenedores, carros, peatones, policías, río, piso y semáforos) y deja la
  jerarquía ordenada.
- **★ ORDENAR ESCENA** — agrupa la jerarquía en carpetas con nombre.
- **📁 Crear carpetas del proyecto** — crea la estructura de carpetas de arriba.

Todo lo que hacen estas herramientas es reversible con **Ctrl+Z**.

## Jerarquía ordenada de la escena

```
=== GUARDIAN DE HUANCAYO ===
├── Sistema                  GameManager, cámara/HUD, red de calles
├── Jugador                  Guardian
├── Zona 0 - Centro (peatonal)      Basura / Contenedores
├── Zona 1 - Mercado (trafico)      Basura / Contenedores / Carros
├── Zona 2 - Ribera (trafico)       Basura / Contenedores / Carros
├── Peatones (NPC)
├── Aliados (Policias)
└── Escenario                Piso de seguridad, Río Shullcas

=== CIUDAD (SimplePoly) ===
├── Vias · Edificios · Props · Vehiculos decorativos · Naturaleza · Otros

Main Camera · Directional Light        (se quedan en la raíz)
```

## Cómo se juega

`WASD` moverse · `mouse` girar la cámara · `ESC` pausa.
Recoge residuos (mochila de hasta 6), mira de qué color son en el HUD y llévalos al
contenedor de ese color, evitando los carros. Si se acaba el tiempo, pierdes las 3
vidas o la contaminación llega a 100, el nivel se reinicia.

Desde el menú se puede entrar directo a cualquiera de las 3 zonas (útil para
mostrar el juego al docente) y ver la pantalla de **Créditos y licencias**.
