# Jugar online con salas

Cada uno juega desde su casa con su mando o su teclado. Uno crea una sala y los demás entran con un código de 6 letras.

## Qué funciona ya

- **Jugar online → Crear una sala**: el juego te da un código y lo copia solo al portapapeles. Pégalo en el chat de tus amigos.
- **Jugar online → Unirse con código**: escribe el código con el teclado (también vale Ctrl+V) o con el mando, como en las recreativas: arriba y abajo cambian la letra, izquierda y derecha mueven el cursor y A entra.
- En la sala todos corren, saltan, se agarran y se placan por el plató. Se ven los colores y la ropa de cada uno y se oyen los saltos y los golpes.
- **START → Salir de la sala** (o **Cerrar la sala**, si eres el anfitrión). Si el anfitrión cierra, los demás vuelven al menú con un aviso.
- Caben 4 jugadores en total. Desde un mismo ordenador pueden jugar varios, pero tienen que unirse (pulsar un botón) **antes** de crear la sala o de entrar en ella.
- **El show entero, online**: cuando estéis todos, el anfitrión pulsa START → **¡Empezar el show!**. Se juegan las mismas pruebas que en local, con bots en los huecos, saboteadores, marcadores y la tabla de estrellas. Entre rondas, cualquiera puede darle a A para seguir.
- Los invitados ven los mismos carteles y oyen los mismos sonidos que el anfitrión. La cámara es la misma en las pruebas de arena; en las carreras, cada uno ve la suya, detrás de su bicho.
- Si alguien entra en la sala con el show ya empezado, mira hasta que se vuelve al menú y juega en el siguiente.
- **Si a alguien se le corta internet**:
  - Si su mando deja de llegar más de 1,5 s, un bot de la prueba le lleva el bicho y sale un cartel. En cuanto vuelve la conexión, vuelve a mandar él.
  - Si se cae del todo, su bicho sigue en el show con el bot y sus estrellas se guardan. Al volver al menú tendrá el código ya escrito: **Unirse con código → A** y recupera su bicho donde esté. Tiene que ser con la misma copia del juego abierta; si la cierra, entra como nuevo y mira hasta el menú.
  - Si se cae el anfitrión, los demás vuelven al menú con un aviso.

## Partida rápida (sin código)

**Jugar online → Partida rápida** te junta con quien esté buscando partida, como en Fall Guys:

1. El juego busca durante unos segundos una partida rápida pública con hueco y de la misma versión del juego. Si la encuentra, entras como invitado.
2. Si no hay ninguna, crea una y tú haces de anfitrión: los siguientes que busquen entrarán en la tuya.
3. Mientras se espera, todos corren por el plató y se ve una cuenta atrás de 40 s. Si se llena (4 ordenadores), baja a 5 s.
4. Al llegar a cero, el show empieza solo, con bots en los huecos. El anfitrión puede adelantarlo con **START → ¡Empezar ya!**.
5. Durante el show nadie más puede entrar. Al volver al menú se abre otra vez y vuelve a contar para el siguiente show.

Mientras busca, **START**, **X** o **Esc** cancelan la búsqueda. Si el anfitrión sale, la partida se cierra para todos (no se pasa a otro).

**Para probarla tú solo:** abre dos o más copias del juego (o del editor con Multiplayer Play Mode) y dale a **Partida rápida** en todas. La primera crea la partida y las demás entran en ella. Con amigos, que la busquen a la vez que tú.

Las partidas con código y las rápidas no se mezclan: las salas con código son privadas y la partida rápida solo encuentra partidas rápidas.

## Antes de la primera vez: Unity Cloud

Las salas usan los servicios **Lobby** y **Relay** de Unity, que son gratis para pocos jugadores.

1. Entra en [cloud.unity.com](https://cloud.unity.com) y abre el proyecto **BichoShow**.
2. En **Products**, activa **Lobby** y **Relay** si no lo están.

Si falta alguno, al crear la sala sale el aviso rosa *"No se puede conectar con los servicios de Unity…"*.

## Probarlo tú solo, en el editor

1. **Window → Multiplayer → Multiplayer Play Mode** y marca **Player 2**. Se abre una segunda ventana del juego.
2. Dale a Play. En las dos ventanas, pulsa una tecla para unirte.
3. En la principal: **Jugar online → Crear una sala**. El código sale en grande.
4. En la de Player 2: **Jugar online → Unirse con código** y escríbelo.

También vale con dos copias del `.exe` en el mismo PC.

## Con amigos

1. Compila el juego (**BichoShow → Compilar el juego para Windows**) y pásales la carpeta, igual que para Parsec.
2. Tú creas la sala y les mandas el código; ellos entran con **Unirse con código**.

Quien crea la sala hace de anfitrión: su ordenador mueve el juego entero. Conviene que lo cree quien tenga el mejor ordenador y la mejor conexión.

## Cómo está hecho

- `Scripts/Red/RedShow.cs`: salas (Multiplayer Services con Relay) y mensajes de Netcode, sin NetworkObjects.
  - El invitado manda `BS.Mando` (stick ya girado con su cámara, botones como contadores y ropa).
  - El anfitrión manda `BS.Estado` 30 veces por segundo (posición y pose de cada bicho) y `BS.Evento` cuando pasa algo (saltos, golpes, reapariciones).
- `EntradaRemota`: en el anfitrión, el mando de un jugador de fuera, como si fuera un bot.
- `MandoRemoto`: en el invitado, esconde su bicho y solo lee el mando.
- `MarionetaBicho`: en el invitado, la copia de cada bicho. Se dibuja 0,1 s en el pasado, interpolando entre dos fotos.
- `EspejoEscena`: el show. El anfitrión manda `BS.Escena` y los invitados cargan la misma prueba, pero sin su lógica (se apagan los scripts del juego y los cuerpos físicos quedan quietos). Luego el anfitrión manda:
  - `BS.Mov`: lo que se mueve (vigila `transform.hasChanged`).
  - `BS.Cambios`: objetos que aparecen, con su malla y sus materiales, o que desaparecen, y colores que cambian.
  - Cada objeto de la escena se reconoce por su ruta en la jerarquía.
- `GrabadorPantalla`: todo lo que dibuja `EstiloShow` en el anfitrión se graba como órdenes de dibujo (`BS.Pantalla`, 12 por segundo) y los invitados lo reproducen escalado a su pantalla.
- Sonidos, partículas, cámara lenta y música van en `BS.Avisos`. En un invitado solo suena lo que llega del anfitrión, más los ruiditos de su propio menú.
- Para que algo nuevo se vea online no hay que hacer nada especial. La única condición es dibujar la interfaz con `EstiloShow` (`Texto`, `Panel`, `Rectangulo`, `Etiqueta`…) y no con `GUI.Label` ni `GUI.DrawTexture` directamente.

- Conexiones que se cortan:
  - `EntradaRemota` tiene un **suplente**: el cerebro de bot de la prueba (`RondaBase.CrearSuplente`), puesto en el mismo bicho. Se usa mientras no llega el mando.
  - Al volver, el invitado manda `BS.Hola` con su id de jugador de Unity, y así recupera su bicho.

Lo que falta:

- Que los invitados vean las partículas que lanzan las propias pruebas, si alguna no pasa por `Efectos`.
- Si se cae el anfitrión, el show se acaba para todos (no hay cambio de anfitrión).
