# Jugar online con salas

Cada uno juega desde su casa con su mando o su teclado. Uno crea una sala y los demás entran con un código de 6 letras.

## Qué funciona ya (fase 1)

- **Jugar online → Crear una sala**: el juego te da un código y lo copia solo al portapapeles. Pégalo en el chat de tus amigos.
- **Jugar online → Unirse con código**: escribe el código con el teclado (también vale Ctrl+V) o con el mando, como en las recreativas: arriba y abajo cambian la letra, izquierda y derecha mueven el cursor y A entra.
- En la sala todos corren, saltan, se agarran y se placan por el plató. Se ven los colores y la ropa de cada uno y se oyen los saltos y los golpes.
- **START → Salir de la sala** (o **Cerrar la sala**, si eres el anfitrión). Si el anfitrión cierra, los demás vuelven al menú con un aviso.
- Caben 4 jugadores en total. Desde un mismo ordenador pueden jugar varios, pero tienen que unirse (pulsar un botón) **antes** de crear la sala o de entrar en ella.

Todavía no se puede empezar el show online: eso es la fase 2.

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

1. Compila el juego (**BichoShow → Compilar el juego**) y pásales la carpeta, igual que para Parsec.
2. Tú creas la sala y les mandas el código; ellos entran con **Unirse con código**.

Quien crea la sala hace de anfitrión: su ordenador mueve el juego entero. Conviene que lo cree quien tenga el mejor ordenador y la mejor conexión.

## Cómo está hecho (para seguir con la fase 2)

- `Scripts/Red/RedShow.cs`: salas (Multiplayer Services con Relay) y mensajes de Netcode, sin NetworkObjects.
  - El invitado manda `BS.Mando` (stick ya girado con su cámara, botones como contadores y ropa).
  - El anfitrión manda `BS.Estado` 30 veces por segundo (posición y pose de cada bicho) y `BS.Evento` cuando pasa algo (saltos, golpes, reapariciones).
- `EntradaRemota`: en el anfitrión, el mando de un jugador de fuera, como si fuera un bot.
- `MandoRemoto`: en el invitado, esconde su bicho y solo lee el mando.
- `MarionetaBicho`: en el invitado, la copia de cada bicho. Se dibuja 0,1 s en el pasado, interpolando entre dos fotos.

Lo que falta:

- **Fase 2: el show online.**
  - Que el anfitrión ordene cargar cada escena.
  - Que en los invitados no corra la lógica de las pruebas.
  - Pasar a los invitados los marcadores, el reloj y los mensajes.
- **Fase 3: los objetos de cada prueba.** Muebles, pelota, bomba, corona y pintura, con el mismo sistema de fotos.
- **Fase 4: desconexiones a mitad del show.**
