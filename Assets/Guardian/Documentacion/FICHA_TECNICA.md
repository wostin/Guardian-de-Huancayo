# Guardián de Huancayo — Ficha técnica

**Curso:** Desarrollo de Videojuegos · Universidad Continental
**Docente:** Guillermo Peña García
**Autor:** Jhovani
**Fecha:** setiembre de 2026

---

## 1. Problema que aborda

**ODS 11 — Ciudades y Comunidades Sostenibles**, meta 11.6: *reducir el impacto ambiental
negativo per cápita de las ciudades, prestando especial atención a la gestión de los desechos
municipales*.

El caso concreto es la **contaminación del río Shullcas (Huancayo)** por residuos sólidos:
la basura que no se segrega en la casa termina en la calle, de la calle pasa a la quebrada y
de la quebrada al río. El juego no habla de "reciclar" en abstracto: obliga al jugador a
**segregar en la fuente** según la norma peruana.

## 2. Marco normativo que usa el juego

**NTP 900.058-2019** — Código de colores para el almacenamiento de residuos sólidos.
El juego usa los cinco colores de residuos aprovechables:

| Color | Residuo | Ejemplos en el juego |
|---|---|---|
| **Blanco** | Plástico | botellas, envases, bolsas |
| **Verde** | Vidrio | botellas, frascos |
| **Azul** | Papel y cartón | cajas, periódicos |
| **Amarillo** | Metales | latas, chatarra |
| **Marrón** | Orgánicos | restos de comida, cáscaras |

Cada contenedor **acepta un solo color**. Si el jugador llega con el residuo equivocado, el
contenedor no lo acepta y le dice de qué es ese color: el error enseña, no solo castiga.

## 3. Mecánica

- El Guardián recorre la ciudad en tercera persona y **recoge residuos** (mochila de 6).
- Cada residuo se ve en el HUD con el color de la norma que le corresponde.
- Hay que **caminar hasta el contenedor correcto**: los cinco están repartidos por distintas
  esquinas de la zona, no juntos. Un indicador en pantalla señala dónde está cada uno.
- Depositar bien: **+15 puntos** y **baja la contaminación**. Depositar mal: no entra y suma
  un error a la **tasa de segregación**.
- La **contaminación sube sola** mientras quede basura en el suelo. Si llega a 100 %, se pierde.
- **Los vecinos siguen botando basura** mientras juegas (hasta 4 por nivel): el problema es
  continuo, no se resuelve una sola vez.
- El tránsito (carros, combis, mototaxis) **quita vida** y echa humo: contaminación del aire.
- Cuando ya no queda basura, el nivel **no termina ahí**: hay que llevar la jornada al
  **PUNTO DE ACOPIO MUNICIPAL** (+50 puntos). Es el último paso del ciclo real: segregar
  sirve porque hay a dónde llevarlo. Mientras dura ese tramo el cronómetro se congela.
- Terminar un nivel **sin un solo error de contenedor** da otros +50 (segregación perfecta).
- Tres zonas = tres niveles: Centro (peatonal), Mercado (tráfico) y Ribera del Shullcas.
- **La ciudad se ensucia a la vista**: si la contaminación sube, la neblina del valle se vuelve
  parda, el sol se apaga, el agua del Shullcas se enturbia y baja más basura flotando. Al
  segregar bien, todo se vuelve a aclarar.

### Modo recorrido (para presentar el trabajo)

Desde el menú se entra a un modo sin cronómetro, sin contaminación y sin perder vidas, con
**las tres zonas visibles a la vez**. Ahí la tecla **C** sube o baja la contaminación de golpe:
sirve para mostrar en vivo, delante del aula, cómo la neblina se vuelve parda, el sol se apaga,
el agua del Shullcas se enturbia y aparece más basura flotando —y cómo todo se aclara al
limpiar. Es la demostración del mensaje del ODS 11 en treinta segundos.

**Indicadores que guarda el juego:** puntaje récord, **mejor tasa de segregación (%)** y zona
máxima alcanzada. La tasa de segregación es el indicador que sirve para el informe.

**Registro de pruebas:** cada partida escribe una línea en `guardian_resultados.csv`
(fecha, nivel, zona, resultado, puntaje, recicladas, aciertos, errores y tasa). La ruta
exacta aparece en la pantalla de resultados. Ese archivo se puede pegar tal cual en el
informe como evidencia de las pruebas realizadas.

## 4. Controles

| Tecla | Acción |
|---|---|
| WASD / flechas | caminar |
| Shift | correr |
| Mouse | girar la cámara |
| Espacio | saltar |
| Esc **o P** | pausa (la pausa muestra controles, tabla NTP y volumen) |
| M | silenciar / volver a activar el audio |
| F9 | ocultar el HUD (modo foto, para capturas del informe) |
| F10 | contador de FPS (para documentar el rendimiento) |
| C | solo en MODO RECORRIDO: ensucia o limpia la ciudad de golpe |

## 5. Arquitectura técnica

**Motor:** Unity 6 (6000.5.9f1) con **URP** (Universal Render Pipeline).
**Plataforma de entrega:** Windows 64 bits (`GuardianDeHuancayo.exe`).

### Scripts propios (`Assets/Guardian/Scripts/`)

| Script | Responsabilidad |
|---|---|
| `GameManager.cs` | Estado del juego, niveles/zonas, mochila, puntaje, contaminación, tasa de segregación, récords |
| `Residuo.cs` | Tipos de residuo y su equivalencia con la NTP 900.058-2019 |
| `TrashItem.cs` | Residuo recolectable (tipo, zona, reserva) |
| `RecycleBin.cs` | Contenedor: acepta un solo tipo, avisa el error |
| `PlayerController.cs` | Movimiento del Guardián (CharacterController) |
| `GuardianCameraHUD.cs` | Cámara en tercera persona, HUD, menús, indicadores de contenedor |
| `Peaton.cs` | Peatones y perro callejero que caminan por rutas de vereda |
| `Ensuciador.cs` | Vecino que bota basura a la calle durante la partida |
| `Aliado.cs` | Policía = limpieza pública: recoge solo lo que los vecinos botan, con descanso |
| `Contaminante.cs` | Vehículo que circula, frena, toca bocina y quita vida |
| `Semaforo.cs` | Semáforo de 3 luces (verde/ámbar/rojo) + luz peatonal |
| `ClimaContaminacion.cs` | Neblina, sol y cielo se ensucian según la contaminación |
| `BasuraEnElRio.cs` | Basura flotando en el Shullcas, proporcional a la contaminación |
| `Paloma.cs` | Palomas de la plaza (picotean, se asustan y vuelan) |
| `CampanaIglesia.cs` | Campana de la Plaza Constitución (audio sintetizado) |
| `CamionRecolector.cs` | Camión municipal que recoge los contenedores al pasar |
| `PuntoAcopioZona.cs` | Zona de entrega del acopio: cierra la jornada del nivel |
| `AmbienteCiudad.cs` | Viento del valle y rumor de tránsito (audio generado por código) |
| `GuardianMusic.cs` | Huayno generado por código, un tema por zona |
| `GuardianFX.cs` | Efectos de sonido y partículas |
| `AguaRio.cs` | Movimiento del agua del Shullcas |
| `RedVial.cs` | Mapa de la calzada horneado en la escena: permite preguntar en juego si un punto cae sobre el asfalto |
| `SeguridadVial.cs` | Penaliza quedarse en la pista con el semáforo en verde para los autos |
| `AnimarCaminar.cs` | Deja la animación de caminata puesta (el reciclador que empuja el triciclo) |
| `RoadNetwork.cs` | Red de vías leída de la ciudad |

### Herramienta de editor (`Assets/Guardian/Editor/GuardianSetupEditor.cs`)

Una sola herramienta arma toda la escena. Menú **Tools ▸ Guardián de Huancayo**:

| Opción | Qué hace |
|---|---|
| ★ ARMAR JUEGO COMPLETO | construye la escena entera (jugador, residuos, contenedores, NPCs, paisaje, letreros) |
| ★ ORDENAR ESCENA | agrupa la jerarquía en "carpetas" |
| 📁 Crear carpetas del proyecto | estructura `Assets/Guardian/...` |
| Paisaje: río Shullcas + cerros | río, ribera y cerros del valle |
| 📷 Fotos del escenario | 10 capturas listas para el informe (borra las anteriores) |
| 🏗 Compilar el juego (.exe) | build de Windows + `LEEME.txt`, limpia la carpeta de depuración |
| 📦 Aligerar texturas | baja las texturas a 1024 px para achicar el ejecutable |
| ℹ Datos del proyecto | cifras del proyecto para el informe |
| 🗜 Comprimir la entrega (.zip) | deja `GuardianDeHuancayo_Entrega.zip` listo para subir |

### Decisiones técnicas que vale la pena mencionar

- **Nada se coloca "a ojo".** La herramienta lee las posiciones reales de los tramos de vía
  de la ciudad, deduce la orientación de cada calle y calcula los **puntos de vereda**
  (el asfalto ocupa ±6.5 u del eje; la vereda va de 6.5 a 10 u). Todo lo que se coloca
  —puestos, contenedores, letreros, panel, triciclo— va sobre la vereda y mirando a la calle.
- **Los contenedores están modelados por código**, no son un prefab pintado: cuerpo del color
  de la norma, tapa, franja reflectiva, ruedas, pedal y **placa con el pictograma** del residuo.
- **Los pictogramas, las texturas del agua, del humo, del cerro y del mural, y el sonido
  ambiente se generan por código** (ruido Perlin, dibujo por píxel, síntesis de audio). No se
  descargó ninguna imagen ni audio de terceros para ellos.
- **Colisión doble** en vehículos, props y contenedores: un colisionador sólido (para que no se
  atraviesen) más un trigger (para detectar el golpe o la entrega).
- **El estado del ambiente es una variable del juego, no un decorado.** La misma cifra de
  contaminación mueve al mismo tiempo la neblina, la intensidad del sol, el tinte del cielo,
  el color del agua del Shullcas y cuánta basura baja flotando por él. El jugador ve el
  efecto de lo que hace sin que nadie se lo explique.
- **Tránsito con reglas**: los vehículos siguen su carril derecho, frenan en ámbar y rojo,
  guardan distancia con los de su misma zona y tocan bocina antes de atropellar al Guardián.
- **Minimapa** en el HUD (radar de 75 m, orientado a la cámara) con los contenedores por
  color de la norma y los residuos tirados.
- **Tres niveles de calidad** en el menú (sombras, antialiasing, distancia de dibujado) para
  que el juego corra también en una laptop modesta, sin cambiar nada del contenido.
- **Cámara con colisión**: un SphereCast entre el Guardián y la cámara acerca la cámara
  cuando hay un edificio en medio, en vez de meterse dentro de la pared.
- **Tutorial de tres pasos** en el primer nivel (recoger → leer el color → entregar bien),
  que no bloquea nada: solo acompaña las primeras acciones de quien agarra el juego por
  primera vez, que es justamente el caso de quien lo va a evaluar.
- **El jugador es quien segrega, y el marcador lo demuestra.** Probando el juego se
  descubrió que los policías aliados levantaban cualquier residuo sin pausa ni tope: con el
  jugador quieto, el marcador subía solo (5 residuos en 20 segundos) y el nivel llegaba a
  ganarse sin haber segregado nada. Se rediseñó su papel al que les toca en la vida real:
  son la **limpieza pública**, recogen únicamente lo que los vecinos van botando durante la
  partida, esperan entre 14 y 24 segundos entre recojo y recojo, y **no suman a "recicladas"**
  —bajan la meta al valor previo—, de modo que el marcador del HUD y el CSV del informe
  miden solo el trabajo del jugador. El aviso en pantalla lo dice: *limpiar no reemplaza segregar*.
- **La gente no empuja al Guardián.** Un `CharacterController` se sale solo de cualquier
  collider que lo toque, así que un peatón parado al lado lo iba corriendo de a poquitos
  (seis metros en un minuto, con el jugador sin tocar una tecla). Ahora los peatones ceden
  el paso y el jugador ignora la colisión con la gente: lo sólido de esta ciudad son los
  edificios, los carros y los contenedores.
- **Cruzar mal cuesta puntos.** El mapa de la calzada se hornea en la escena, así que el juego
  sabe cuándo el Guardián está parado sobre el asfalto; si además el semáforo más cercano está
  en verde para los autos, a los 2 segundos le avisa y le descuenta 5 puntos. Una ciudad
  sostenible (ODS 11) también es una ciudad en la que se puede caminar sin que te atropellen.
- **Todo el audio es sintetizado en tiempo de ejecución**: pasos, bocina, motor, campana,
  tic-tac del cronómetro, efectos de acierto/error y los tres temas de huayno.

## 6. Recursos de terceros usados

Paquetes con licencia de uso libre importados desde Unity Asset Store:
SimplePoly City – Low Poly Assets, CityPeople (Denys Almaral), npc_casual_set_00,
Lowpoly Forest Pack, Hill Rock Mountain Terrain, Trash Bag & Trash Box, FREE Food Pack,
Church 3D, LowPolyRoadVehicles.

**No se usó ninguna imagen ni sonido tomado de buscadores web.** Todo lo que no viene de esos
paquetes está generado por código dentro del propio proyecto.

## 7. Capturas para el informe

En `Assets/Guardian/Imagenes/Capturas/`:

| Archivo | Muestra |
|---|---|
| `01_iglesia.png` | Plaza Constitución y su atrio |
| `02_mercado.png` | Puestos del mercado mayorista con precios |
| `03_contenedor.png` | Contenedor NTP con pictograma y cartel |
| `04_rio_shullcas.png` | Río Shullcas con la basura bajando por el cauce |
| `05_valle.png` | Vista general de la ciudad en el valle |
| `06_panel_ODS11.png` | Panel del ODS 11 con el código de colores |
| `07_triciclo.png` | Triciclo del reciclador |
| `08_acopio.png` | Punto de acopio municipal con las cajas por color |
| `09_plaza.png` | Plaza con las palomas |
| `10_camion.png` | Camión recolector de la municipalidad en la avenida |

> Consejo: si quieres las capturas con los personajes animados (no en pose T),
> entra a **Play** y recién ahí usa *📷 Fotos del escenario*.

## 8. Entrega

El menú *🗜 Comprimir la entrega (.zip)* deja listo `GuardianDeHuancayo_Entrega.zip`
(**138 MB**) en la carpeta del proyecto: ese es el archivo que se sube al aula virtual.

Se entrega **toda la carpeta** `Build_GuardianHuancayo` comprimida en ZIP.
El `.exe` solo no arranca: necesita la carpeta `GuardianDeHuancayo_Data` al lado.
Dentro va un `LEEME.txt` con los controles y el objetivo.
