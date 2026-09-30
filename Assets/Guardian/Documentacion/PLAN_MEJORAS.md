# Plan de mejoras — Guardián de Huancayo

Última actualización: **15/09/2026**

---

## 0. Por qué los puestos salían en medio de la pista (causa real)

Se midió la ciudad del pack **SimplePoly City** leyendo directamente el archivo de la escena
y sacando las posiciones reales de los 78 tramos de vía y de los props del propio pack:

| Dato medido | Valor |
|---|---|
| Cuadrícula de la ciudad | múltiplos de **20 u** en X y Z |
| Tamaño de un tramo de vía | **20 × 20 u** |
| 126 postes de luz (`Props_Street Light`) | todos a **7.5 u** del eje de la calle |
| 25 hidrantes (`Props_Hydrant`) | a **7.6 u** del eje |
| 43 semáforos (`Props_Traffic Signal_big`) | a **7.5 u** en las esquinas |

Conclusión: el **asfalto ocupa ±6.5 u** desde el eje y la **vereda va de 6.5 a 10 u**
(centro en **7.5–8.3**). No hay objetos "vereda" separados: vienen dentro del prefab de la calle.

---

## 1. Hecho ✔

### Colocación lógica (nada sobre el asfalto)
- `AnalizarVias()`, `SobreLaPista()`, `PuntosDeVereda()`, `TomarVereda()`, `TomarVeredaSuave()`.
- Puestos, contenedores, letreros, mural, carretilla, panel del ODS, triciclo, punto de inicio
  y rutas de peatones: **todo sobre la vereda y mirando a la calle**.

### Colisiones
- Vehículos, props, puestos y contenedores con **colisionador sólido + trigger**.
- `Contaminante.cs` enciende y apaga también el sólido al cambiar de zona.
- Peatones y policías con cápsula + Rigidbody cinemático.

### Segregación NTP 900.058-2019
- 5 tipos de residuo, 5 contenedores por zona, error que **enseña** en vez de solo castigar.
- HUD con la mochila por colores, tabla de colores e indicadores hacia cada contenedor.
- Se guarda la **mejor tasa de segregación** y la zona máxima, no solo el puntaje.

### Contenedores modelados por código  *(ronda 15/09)*
- Cuerpo del color de la norma, tapa más oscura, borde, agarradera, franja reflectiva,
  ruedas, pedal y barras.
- **Placa con el pictograma** del residuo (botella, copa, hoja, lata, manzana), dibujado
  píxel por píxel por código, con marco del color de la norma.
- Cartel NTP sobre **dos postes** detrás del contenedor, para no tapar el pictograma.
- El contenedor **ya no se atraviesa**.

### Ambientación
- Río Shullcas con ribera arbolada, cerros con cumbre nevada, suelo del valle, neblina.
- Iglesia de la Plaza Constitución con atrio, cruz, bancas, banderas y luz de fachada.
- Mercado con toldos, banderines, pizarras de precios y vendedores.
- Mototaxis, combis, mural wanka, carretilla de emoliente.
- Viento del valle y rumor de tránsito generados por código.

### Educativo  *(ronda 15/09)*
- **Panel del ODS 11** en la plaza: título, la norma, los 5 colores con su rótulo y el
  mensaje sobre el río. Con luz propia para que se lea a contraluz.
- **Triciclo del reciclador** estacionado en la vereda de cada zona.
- **Vecinos que botan basura** durante la partida (`Ensuciador.cs`, hasta 4 por nivel):
  la meta del nivel sube con cada residuo botado.

### Entrega
- `🏗 Compilar el juego (.exe)` → build de Windows, borra la carpeta `_DoNotShip`,
  escribe `LEEME.txt` y reporta el **peso real de la carpeta**.
- `📦 Aligerar texturas` → 788 de 863 texturas a 1024 px y comprimidas.
  **1037 MB → 379 MB.**
- `📷 Fotos del escenario` → 7 capturas para el informe.
- `ℹ Datos del proyecto` → cifras del proyecto para el informe.
- `FICHA_TECNICA.md` con todo lo que pide el trabajo.

---

### Ronda del 15/09 (tarde)
- **La ciudad se ensucia a la vista** (`ClimaContaminacion.cs`): neblina parda, sol apagado,
  cielo y ambiente sucios según la contaminación; se aclara al reciclar.
- **Basura flotando en el Shullcas** (`BasuraEnElRio.cs`): botellas, bolsas, latas y tecnopor
  bajando por el cauce; se ven más cuantos más residuos queden tirados.
- **Semáforo de 3 luces** con fase ámbar y luz peatonal (`Semaforo.cs` reescrito).
- **Tránsito con reglas** (`Contaminante.cs`): frena en ámbar/rojo solo del semáforo que tiene
  adelante, guarda distancia solo con los carros de su zona (antes se trababa contra carros
  invisibles de otra zona) y toca bocina antes de atropellar al Guardián.
- **Minimapa** en el HUD: radar de 75 m orientado a la cámara, con contenedores por color y
  residuos; resuelve el problema de perderse buscando el contenedor correcto.
- **Punto de acopio municipal** por zona, con las 5 cajas de la norma sobre el mostrador.
- **Palomas** en el atrio (picotean, se asustan y vuelan) y **perro callejero** por zona.
- **Campana de la plaza** generada por código.
- **Resultados** con la equivalencia en kg y el nombre de la zona siguiente.
- **Presentación de zona** al empezar cada nivel, con el dato real del problema.
- Tiempo de nivel 180 s → **210 s**.
- Las capturas se regeneran limpias (borra duplicados) y el río tiene su propia foto.

### Ronda del 15/09 (noche)
- **Cámara que no atraviesa paredes**: SphereCast entre el Guardián y la cámara; si hay un
  edificio en medio, la cámara se acerca. Antes se metía dentro de las casas.
- **Correr con Shift** (×1.55) con la animación acelerada, y **pasos con sonido** generado
  por código. La documentación ya decía "Shift = correr" pero no estaba implementado.
- **Tutorial de 3 pasos** en el primer nivel: recoger → leer el color → entregar bien.
  No bloquea nada, solo acompaña las primeras acciones.
- **Tic-tac** en los últimos 15 segundos del nivel.
- **F9 = modo foto**: oculta todo el HUD para sacar capturas limpias del informe.
- **Pantalla "¡CIUDAD LIMPIA!"** al terminar las tres zonas, con botón de volver al inicio.
  También se puede volver al menú tras perder.
- **Música distinta por zona** (`GuardianMusic.cs`): Centro tranquilo, Mercado más rápido y
  un tono arriba con el charango presente, Ribera más lento y grave.
- **Reciclador de pie junto a su triciclo** en cada zona.

### Ronda del 15/09 (cierre)
- **MODO RECORRIDO** en el menú: toda la ciudad visible a la vez, sin cronómetro, sin
  contaminación y sin perder vidas. Es el modo para presentar el trabajo con calma.
  Dentro de ese modo, la **tecla C ensucia o limpia la ciudad de golpe**: sirve para mostrar
  en vivo cómo cambian la neblina, el sol, el río y la basura que baja por él.
- **Camión recolector de la municipalidad** por zona (`CamionRecolector.cs`): cabina naranja,
  compactadora con franja verde, tolva y rótulo. Circula como cualquier vehículo y al pasar
  junto a un contenedor lo recoge, con aviso incluido.
- **Resultados por color**: cuántos residuos de cada tipo se segregaron bien, en fichas.
- **Alarma al 80 % de contaminación**: aviso + borde rojo latiendo en pantalla.
- **Seguridad vial**: si un carro tiene que frenar por el Guardián, además de la bocina
  pierde 3 puntos y le recuerda cruzar por el crucero peatonal.
- **Selector de calidad** (Bajo / Medio / Alto) en el menú: sombras, antialiasing y distancia
  de dibujado, para que corra también en una laptop modesta.
- Las fotos del informe encienden los vehículos de todas las zonas, aunque se tomen en Play.

### Ronda del 15/09 (última)
- **El nivel se cierra entregando en el PUNTO DE ACOPIO.** Cuando ya no queda basura, el
  nivel no termina de golpe: aparece "¡Zona limpia! Lleva la jornada al punto de acopio",
  el cronómetro y la contaminación se congelan, el acopio se marca en pantalla y en el
  minimapa, y al llegar se cierra la jornada con **+50 puntos**. Si el jugador no lo
  encuentra, a los 80 s cierra solo (red de seguridad).
- **Bono de segregación perfecta**: +50 si el nivel se termina sin un solo error de contenedor.
- **Registro de partidas en CSV** (`guardian_resultados.csv` en la carpeta de datos del
  juego): fecha, nivel, zona, resultado, puntaje, recicladas, aciertos, errores y tasa.
  Sirve como evidencia de las pruebas para el informe; la ruta sale en la pantalla final.

### Ronda del 15/09 (madrugada) — jugabilidad y equilibrio

- **Red vial consultable en juego** (`RedVial.cs`): la herramienta del editor ya sabía qué
  tramos de vía existen y hacia dónde corre cada uno; ahora ese mapa se hornea en la escena
  (`BakearRedVial`) y el juego puede preguntar, mientras se juega, si un punto cae sobre el
  asfalto. Sin esto habría que adivinarlo con raycasts contra la malla de la ciudad, que no
  distingue pista de vereda.
- **Regla del semáforo peatonal** (`SeguridadVial.cs`): si el Guardián se queda parado en la
  pista de un cruce mientras el semáforo está en **verde para los autos**, el juego se lo
  advierte y le cuesta **5 puntos**. Da 2 segundos de gracia, así que entrar y salir no
  penaliza, y si cruza con el peatonal en verde no pasa nada. Cierra el punto 1 de la lista
  de pendientes: antes solo reaccionaba el carro (bocina y −3), no el semáforo.
- **El triciclo del reciclador ya circula** por la vereda en vez de estar estacionado, con su
  reciclador enganchado detrás empujándolo (`AnimarCaminar.cs` le deja la animación de
  caminata puesta). Cierra el punto 3 de la lista de pendientes.
- **Contador de FPS con F10**, para poder documentar el rendimiento en el informe.

#### Dos errores de juego encontrados probando, y cómo se arreglaron

1. **El nivel se ganaba solo.** Los policías aliados levantaban *cualquier* residuo que
   vieran, sin pausa ni tope: en la primera prueba, con el jugador sin tocar una sola tecla,
   el marcador subió a **5 residuos "reciclados" en 20 segundos**. Con dos o tres policías el
   nivel se terminaba sin que nadie hubiera segregado nada — justo lo contrario de lo que el
   juego tiene que enseñar. Ahora el policía cumple el papel que le toca en la vida real: es
   la **limpieza pública**, recoge solo lo que los vecinos van botando durante la partida
   (los residuos `deReserva`), espera entre 14 y 24 segundos entre recojo y recojo, y no
   arranca hasta pasados los primeros segundos del nivel. Además **no suma a "recicladas"**:
   baja la meta al valor que tenía antes de que botaran ese residuo (`LimpiezaMunicipal`),
   de modo que el marcador y el CSV siguen midiendo **solo lo que el jugador segregó con sus
   manos**, que es lo que el informe tiene que poder demostrar. Da 2 puntos y avisa
   *"limpiar no reemplaza segregar"*.
2. **A la gente le daba por empujar al Guardián.** Un `CharacterController` se sale solo de
   cualquier collider que lo esté tocando, así que bastaba con que un peatón se le parara al
   lado para irlo corriendo de a poquitos: en una prueba de un minuto, quieto y sin tocar una
   tecla, el Guardián terminó **seis metros más allá**, y en una zona con tráfico eso lo
   habría metido a la pista. Se arregló por dos lados: los peatones y policías **ceden el
   paso** cuando el jugador se les cruza por delante (y lo rodean si se queda plantado más de
   3 segundos), y el jugador **ignora la colisión** con la gente, porque lo sólido de esta
   ciudad son los edificios, los carros y los contenedores, no el decorado.


#### Comodidad de uso (la misma ronda)

- **Control de volumen.** El juego sintetiza toda su música y todos sus efectos, pero no
  había manera de bajarle: ahora hay un control en el menú y otro en la pausa (🔊 · − · % · +),
  la tecla **M** silencia y vuelve a activar, y la elección se guarda entre partidas. Quien
  evalúe el trabajo puede estar en un salón lleno o con audífonos.
- **Pantalla de pausa de verdad.** Antes solo tenía REANUDAR y REINICIAR. Ahora muestra en
  qué zona va y cuántos residuos lleva segregados, la lista de controles, la tabla de colores
  de la NTP con su leyenda, el control de volumen y un botón para **volver al menú**. Es la
  pantalla que mira quien abre el juego por primera vez y no recuerda cómo se jugaba.
- **La pausa también responde a la tecla P.** El Game View de Unity se queda con ESC (la usa
  para soltar el foco), así que probando dentro del editor la pausa parecía no funcionar.
  Con P se comprueba en el editor y en el .exe por igual.


### Ronda del 16/09 — río, carteles, audio del proyecto y bicicletas

- **El río ya no se mete debajo de las casas.** El cauce se ubicaba a 32 unidades de la
  última **vía**, pero los edificios de la última manzana sobresalen bastante más allá del
  asfalto, así que el agua les pasaba por debajo y se veían flotando. Ahora el eje del cauce
  se mide contra **lo construido** (`EjeDelRio` + `BordeConstruido`), con 16 u de ribera
  entre la última casa y la orilla, y el pasto se recorta del lado de la ciudad en vez de
  entrar bajo la manzana. Lleva un tope de 45 u que evita que el cauce se escape: sin él, la
  vegetación que el propio río planta contaba como "ciudad" en el cálculo siguiente y el río
  se alejaba un tramo más cada vez que se armaba el juego (llegó a irse 300 u).
- **El nivel 3 se juega de verdad en la ribera.** La zona "Ribera del Shullcas" estaba en el
  extremo **opuesto** al río (rel z 0.75, y el río va en z bajo): el nivel se llamaba así y el
  río no se veía. Ahora está pegada al cauce, y **la mitad de sus residuos aparecen en la
  orilla**, entre la ciudad y el agua, que es donde termina lo que la ciudad no recoge.
  Limpiar el río pasa a ser el objetivo literal de ese nivel.
- **Los carteles ya no se desbordan.** El tablero tenía ancho fijo y el tamaño de letra salía
  de la **altura**, no del largo del texto, así que "HUANCAYO - CIUDAD INCONTRASTABLE" se
  salía por los dos lados. Ahora se mide el texto con las métricas reales de la fuente
  (`Font.GetCharacterInfo`): el tablero crece hasta 1.6× y, si aun así no entra, se achica la
  letra. Sobre 7 u de ancho se ponen **dos postes**, porque un cartel largo sobre uno solo
  se ve flotando.
- **Audio del proyecto enganchado** (`AudioDelProyecto`): `Title_Screen` en el menú,
  `Town_Theme` en el Centro, `Shufflin-Through-Central-Park` en el Mercado, `Deep_Forest` en
  la Ribera, `Victory_Fanfare` al ganar, y `paso1`/`paso2`/`salto` en el Guardián, con cruce
  suave entre pistas. Se dejan en **Vorbis + streaming**: en WAV crudo esos temas suman más
  de 200 MB y se irían enteros al .exe.
- **Banco de efectos nuevo** (`GuardianAudio`): acierto, error, entrega en el acopio, golpe,
  bonus y timbre, todos sintetizados. Con **variación de tono** en cada disparo y 3D con
  alcance definido. Antes se creaba un `AudioClip` nuevo en cada recojo, siempre con el mismo
  tono exacto, que a los veinte residuos sonaba a metrónomo.
- **Bicicletas** (`Bicicleta.cs`, prefab Sir_bike), dos por zona sobre la vereda: **E** para
  subir —va 1.85× más rápido— y **T** para el timbre. Es la meta 11.2 del ODS 11 (transporte
  sostenible) y de paso acorta los viajes hasta el contenedor.
- **Colisiones y Static de toda la ciudad** con menú propio: agrega los colliders que falten
  y marca como estático lo que no se mueve, dejando fuera carros, peatones, bici y triciclo.
- **Error de consola resuelto**: el `SocketException` que salía en cada recompilación venía
  del paquete de terceros `com.justinpbarnett.unity-mcp`, que abre un puerto TCP fijo desde
  un `[InitializeOnLoad]`. Se quitó del `manifest.json` (queda respaldo al lado). No tenía
  relación con el juego y no entraba al build.

#### Lo que se intentó y no quedó: el río POR DENTRO de la ciudad

El Shullcas real es urbano —separa Huancayo de El Tambo, y por eso le cae la basura de la
ciudad—, así que se probó meterlo dentro: ocupaba una calle este-oeste entera, con malecones
de concreto, 9 puentes en las calles perpendiculares y 145 construcciones apartadas del
cauce. El despeje y los puentes funcionaban, pero **el agua nunca llegó a verse**, por tres
causas encadenadas que costó separar: el suelo del pack no está en Y 0 sino que el asfalto
tiene su propio grosor (Y 0.40), el `MeshRenderer` de las losas de calle cuelga de un hijo
con otro nombre —así que el filtro que medía la altura no las veía—, y el material del agua
arrastraba una textura generada en memoria que **muere en cada recarga de dominio**, dejando
una referencia rota que lo pintaba de gris. Se corrigieron las tres (`MaterialSimple` ahora
**limpia** la textura vieja en vez de dejarla), pero el resultado seguía leyéndose como una
franja de concreto cruzando Huancayo, así que se volvió al río de la ribera, que se ve bien
y se entiende. Todo lo aprendido queda aplicado: **la altura del suelo se mide, no se supone**.


---

## 2. Pendiente (queda poco y es menor)

1. **Marca pintada en la vereda** bajo cada contenedor: se probó y la vereda la tapaba;
   habría que apoyarla con un raycast propio en vez de colgarla del contenedor.

---

## 3. Cómo se ejecuta una ronda de cambios

1. Editar `Assets/Guardian/Editor/GuardianSetupEditor.cs` y los scripts de `Assets/Guardian/Scripts/`.
2. En Unity: `Ctrl+R` y **esperar a que termine de compilar** (la barra de estado lo dice).
3. **Tools ▸ Guardián de Huancayo ▸ ★ ARMAR JUEGO COMPLETO** → `Ctrl+S`.
4. *📷 Fotos del escenario* para revisar cómo quedó.
5. *🏗 Compilar el juego (.exe)* solo cuando la escena ya esté como se quiere.

---

## Ronda del 16/09 — paisaje, sonido y cámara

### Lo que se veía mal y por qué

**El pasto y la pista parecían de dos mundos distintos.** No era un problema de
la pista: eran DOS verdes planos superpuestos. El prado del valle era un plano
liso de color `(0.44, 0.52, 0.30)` a `y = -0.35` y la ribera del Shullcas otro
verde `(0.33, 0.55, 0.24)` a `y = 0.02`. Donde se cruzaban aparecía esa cuña
oscura de bordes rectos que no correspondía a ningún accidente del terreno.
Ahora los dos usan la MISMA textura (`Grass_37` del pack *Free Stylized
Textures*) y quedan casi al ras, así que el verde se lee como un solo prado.

**Cerros.** Eran mallas generadas por código: conos con ruido de Perlin. Cumplían
para cerrar el horizonte, pero un cono nunca va a tener la silueta de una montaña
modelada. Ahora se instancian modelos reales del proyecto, y el buscador va por
NOMBRE (`autumn`, `mountain`, `hill`, `peak`, `cliff`, `alpine`), no por ruta
fija: basta con soltar un pack nuevo de montañas en `Assets/` y el siguiente
ARMAR los usa sin tocar una línea de código.

**El río no se movía.** `AguaRio` desplaza la textura del material, pero el
material no tenía textura —se le había quitado porque la generada por código
moría al recargar el dominio—, así que el desplazamiento no se veía. Con
`Water_6` del pack por fin corre.

**Carteles en espejo.** El material que trae la fuente de Unity usa
`GUI/Text Shader`, que dibuja con `ZTest Always`: la letra se pinta siempre
encima, atraviese lo que atraviese. Por eso el código anterior ponía texto en una
sola cara, y desde atrás el cartel se leía al revés. Se resolvió con un Unlit de
URP recortado por alfa: la letra escribe y consulta profundidad como cualquier
sólido, el tablero opaco tapa la cara de atrás, y ahora hay texto en ambos lados.

**108 objetos flotando.** Los adornos se colocaban midiendo el suelo con un rayo
sobre el PIVOTE, pero muchos prefabs no tienen el pivote en la base. Apoyar el
pivote no es apoyar el objeto. La pasada nueva mide la caja real de cada uno,
tira un rayo desde arriba ignorando sus propios colisionadores y lo baja la
diferencia. Deja fuera a propósito lo que sí debe flotar: palomas, basura del
río, humo.

### Sonido: se estaba usando menos de la mitad de lo que había

| Archivo | Dónde suena ahora |
|---|---|
| `Title_Screen` / `Stratosphere_Looping` | menú, alternándose |
| `Town_Theme` | Zona 0 · Centro |
| `Shuffliin-Through-Central-Park` | Zona 1 · Mercado |
| `Deep_Forest` | Zona 2 · Ribera |
| `Underwater-World` | al acercarse al Shullcas (< 46 m) |
| `Boss_Battle_Intro` → `Boss_Battle_Loop` | contaminación sobre el 78 % |
| `Escape` | últimos 32 segundos del cronómetro |
| `Cool-Adventure-Intro` | cortina al estrenar zona |
| `Victory_Fanfare_Intro` → `Victory_Fanfare_Loop` | victoria |
| `Time_Cave` | derrota |
| Strings · Woodwinds · Piano · Harp | capa de ambiente al 30 % bajo el tema |
| `Footstep_Shoe_On_Street_*` + `Sneakers_*` | pisadas en vereda y pista |
| `Foostep_Grass_*` + `Grass_Alternative_*` | pisadas en pasto, cerros y ribera |
| `Footstep_Sand_*` | pisadas en la orilla del río |
| `Footstep_Wood_*` / `Metal_*` | puentes / rejillas |
| `Footstep_Deep_*` | aterrizaje después de un salto |
| `idle` + `med_on` + `startup` | motor de cada carro, en dos capas |

La superficie se detecta con un rayo hacia abajo tres veces por segundo (no por
cuadro: es un dato que casi nunca cambia y entre dos pasos no da tiempo de
cambiar de piso).

El motor iba con una sola capa: el ralentí con el tono subido. Un carro a fondo
seguía sonando a carro parado. Ahora se cruzan ralentí y aceleración según la
velocidad, más el arranque la primera vez que se mueve.

### Cámara

Solo tenía `yaw`: el brazo era el vector fijo `(0, 11, -11)` y no había forma de
levantar ni bajar la vista. Ahora el brazo se arma con `yaw + pitch`
(−14° a 80°), la rueda acerca y aleja (6 a 28 u) y un rayo evita que se entierre
en el piso.

### Mapa

Eran 200 px fijos pegados al borde: en ventana angosta se cortaba. Ahora el
tamaño sale de la ventana y el recuadro se mantiene siempre dentro de la
pantalla. Además **TAB** abre el plano grande centrado, con 160 m de alcance.

### Racha

Lo que faltaba para que se sintiera juego. Cada segregación correcta seguida
sube la racha; cada tres, el multiplicador sube un punto hasta x5. Un error la
tira a cero. El tono del acierto sube con la racha, así que se oye que vas bien
sin mirar el HUD. Premia justo lo que el curso quiere enseñar: segregar BIEN, no
solo recoger.

### Carros

Se usaban 4 de los 22 vehículos del pack, y el camión recolector estaba armado a
mano con 18 cubos: al lado de los carros del pack —misma paleta, mismas
proporciones, mismo atlas— cantaba a kilómetros. Ahora el tráfico rota entre 16
vehículos y el camión parte del `Vehicle_Container` del pack, con el rótulo de la
municipalidad y la baliza ámbar encima.

---

## Ronda del 22/09 — lo que le faltaba al juego

Seis cosas, por orden de impacto.

### 1. El río por fin se juega

Era lo más grave. La basura que baja por el Shullcas era **decorado**: cambiaba de
cantidad según la contaminación de la ciudad, pero el jugador no podía tocarla. Un
juego que se llama *Guardián de Huancayo*, sobre el río Shullcas, en el que miras
pasar la basura sin poder hacer nada, enseña exactamente lo contrario de lo que
pide el ODS 11.

Ahora cada pieza es un residuo de verdad, del tipo que le corresponde, y los cinco
tipos de la NTP rotan por el cauce: botella, bolsa, lata, caja mojada, frasco de
vidrio y resto orgánico. Se puede **vadear**: el agua no tiene colisionador y el
piso de seguridad está justo debajo, así que el Guardián entra al cauce con el agua
por el tobillo y saca lo que baja. Valen 18 puntos en vez de 10, porque hay que
meterse; y el trigger es ancho (1.45 m) porque atrapar algo que va con la corriente
ya es bastante difícil.

`BasuraEnElRio` dejó de encender y apagar las piezas según la contaminación: eso
habría hecho reaparecer basura que el jugador ya sacó del agua.

### 2. Los errores ahora enseñan

Antes, al equivocarse de contenedor, el juego decía cuál era el correcto. Eso
enseña a memorizar colores, que es la mitad de la lección. Lo que hay detrás de la
NTP 900.058-2019 no es el color: es que un residuo mezclado **deja de ser
aprovechable**. Se agregó `Residuo.PorQue(tipo)` y una tarjeta que aparece siete
segundos bajo el aviso:

- Vidrio: «un vidrio roto entre el papel arruina toda la paca y corta las manos del reciclador que la abre»
- Papel: «el papel mojado o grasoso pierde la fibra: si se moja en el camión, se va entero al botadero»
- Metal: «la lata se refunde una y otra vez sin perder calidad; tirada al Shullcas se queda ahí veinte años»
- Plástico: «el plástico limpio se muele y vuelve como tubería o fibra; con restos de comida encima ya nadie lo compra»
- Orgánico: «los restos hacen compost en semanas; enterrados con el resto producen metano y lixiviados»

### 3. Cada zona se juega distinto

Las tres eran la misma partida con los carros más veloces. Ahora:

| Zona | Tiempo | Se ensucia | Carácter |
|---|---|---|---|
| Centro · Plaza Constitución | ×1.00 | ×0.80 | peatonal, sin carros: se aprende |
| Mercado Mayorista | ×0.78 | ×1.45 | reloj apretado, se ensucia rápido |
| Ribera del Shullcas | ×1.12 | ×1.10 | más tiempo, pero la basura se escapa río abajo |

Y el aviso de entrada dice CÓMO se juega la zona, no solo dónde queda.

### 4. Al vecino se le puede alcanzar

Botaba su basura y seguía caminando: el jugador se enteraba por un aviso y
encontraba una botella en el piso, sin nadie a quien reclamarle. Ahora hay once
segundos para alcanzarlo; si llegas a menos de 3.6 m, el vecino recoge lo suyo:
**+25 puntos**, más que recogerlo tú. Evitar que ensucien vale más que limpiar
después, y el juego ahora lo dice con los puntos y no solo con un texto.

### 5. Rango final

La pantalla de resultados abre con el rango, calculado sobre la **tasa de
segregación**, no sobre el puntaje bruto: se puede recoger mucho y separar mal, y
eso es justo lo que el juego no debe premiar.

- Guardián de Oro del Shullcas — 95 % sin un solo error y racha de 8
- Guardián de Plata — 85 %
- Guardián de Bronce — 70 %
- Promotor Ambiental — 50 %
- Aprendiz de Guardián — por debajo

### 6. El .exe

Recompilado con todo lo de las últimas rondas. **575 MB** la carpeta,
**263 MB** el ZIP de entrega (`GuardianDeHuancayo_Entrega.zip`, al lado de
`Assets/`). Cuatro minutos y medio de compilación, casi todo en variantes de
shader de URP.
