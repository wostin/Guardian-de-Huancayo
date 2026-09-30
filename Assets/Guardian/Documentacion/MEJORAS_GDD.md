# Guardián de Huancayo — Mejoras para el GDD (29/09/2026)

Todo se activa solo al dar Play (scripts con `RuntimeInitializeOnLoadMethod`); no hace falta volver a armar la escena.
Respaldo de los scripts anteriores: carpeta `Respaldo_antes_mejoras_29sep/` (fuera de Assets).

## Cámara (GuardianCameraHUD.cs)
| Tecla | Modo | Uso |
|---|---|---|
| 1 | 1ª persona | Ojos del Guardián (1.62 m), balanceo de cabeza al caminar, mira central; el cuerpo solo proyecta sombra |
| 2 | Aérea | Vista de estratega (22–48 m, 45–85°): ubicar basura y contenedores |
| 3 | 3ª persona | Por defecto; colisión con paredes, FOV se abre al correr |
| V | Alterna | Cicla los tres modos (se guarda entre partidas) |
Extras: transición suave entre modos, sacudida por "trauma" (ruido Perlin) en golpes/errores, vuelo de cámara detrás del menú.

## Flechas guía 3D (GuardianFlechas.cs) — tecla G
- Brújula alrededor del Guardián: mochila vacía → residuo más cercano (color del residuo); con carga → contenedor de ESE color; zona limpia → punto de acopio.
- Segunda flecha (más chica) sigue marcando basura mientras aún cabe en la mochila.
- Baliza que rebota y gira sobre el objetivo. Contorno oscuro para contraste y brillo (Bloom).

## Carros (Contaminante.cs)
- Dirección *pure pursuit* (persigue un punto adelante en su ruta) con giro limitado: curvas suaves.
- Frenan antes de las esquinas y de forma progresiva ante semáforo, otro carro o el jugador.
- Inclinación de carrocería en curvas y cabeceo al frenar; ruedas que giran; pegados al suelo por raycast.

## Iluminación y sombras (GuardianIluminacion.cs)
- Luz clave: sol direccional con sombras suaves (bias/normal bias ajustados).
- Luz de relleno azulada sin sombras desde el lado opuesto; ambiente controlado por ClimaContaminacion.
- Sombras URP según calidad: Bajo 35 m/1 cascada · Medio 70 m/2 · Alto 120 m/4.
- Post-proceso: ACES, Bloom, ajuste de color, viñeta. La saturación baja con la contaminación.

## Animación (GuardianAnimaciones.cs + PlayerController.cs)
- Residuo vuela en arco a la mochila; contenedor hace squash & stretch al acertar y tiembla al fallar.
- Residuos cercanos "respiran"; el Guardián parpadea y retrocede al ser golpeado.
- Usa clips del Animator si existen (pick, jump, hit, cheer).

## Flujo de señal de audio (GuardianMezcla.cs) — tecla F8
Fuentes → Bus (Música · Ambiente · Efectos 3D · Interfaz) → Proceso (ducking, pasa-bajos en pausa, atenuación 3D) → Master (AudioListener.volume) → AudioListener.
Sliders por bus en el menú de pausa; panel F8 con medidores en vivo.

## Interfaz (UI/UX)
- Botones con esquinas redondeadas y 3 estados; menú translúcido sobre la ciudad; título con sombra y latido.
- Puntaje que cuenta hacia arriba con "pop"; barra de contaminación que se desliza.
- Indicador de cámara, aviso grande al cambiar de modo y selector de cámara en el menú.
- Textos flotantes en el mundo (+puntos, +1 Plástico, ✖ …).

## VFX de retroalimentación (GuardianVFX.cs + GuardianParticulas.cs)
| Acción | Efecto | Mensaje |
|---|---|---|
| Recoger | Chispas del color + onda + "+1" | Va al contenedor de ese color |
| Acierto | Confeti + anillos + destello verde + "+pts" | ¡Bien segregado! |
| Error | Humo rojo + "✖" + viñeta roja + sacudida | Ese no es su color |
| Golpe de carro | Polvo + estrellas + sacudida fuerte | Cuidado con la pista |
| Correr | Polvo en los pies | Velocidad |
| Ganar / Perder | Fuegos artificiales NTP / humo gris | Celebración / ciudad sucia |
Todo se conecta por eventos (`GuardianEventos`, patrón Observer).
