# Cámaras en las carreras

En las carreras (Costura a la fuga, Despensa Deslizante y La Cumbre del Girasol), cada jugador tiene su propia cámara detrás de su bicho, baja y cerca, mirando hacia delante, como en Fall Guys. En el resto de pruebas sigue la cámara de grupo de siempre.

No hace falta rehacer las escenas: la cámara de grupo detecta sola que la escena es una carrera (porque tiene una ruta de bots) y crea las cámaras de los jugadores.

## Cómo se controla

- **Mando:** el stick derecho gira la cámara alrededor del bicho y la sube o la baja.
- **Teclado:** el ratón hace lo mismo.
- **Si no la tocas:** al segundo y medio vuelve a colocarse sola, mirando hacia donde sigue la carrera. Así se puede jugar sin tocarla nunca.
- **El stick izquierdo** mueve el bicho según su cámara: arriba es "hacia donde mira mi cámara".
- **Si algo se pone en medio** (una pared, el fregadero, un rodillo…), la cámara se acerca. Si aun así algo tapa al bicho, se ve su silueta blanca.
- **Si tu bicho queda fuera** (por ejemplo, pegado en la savia de la Cumbre), tu cámara sigue al que va mejor de los que quedan.

## Cómo se reparte la pantalla

| Jugadores en este ordenador | Pantalla |
|---|---|
| 1 | Entera |
| 2 | Partida arriba y abajo |
| 3 | En cuartos; el cuarto hueco es la cámara del show (la de grupo) |
| 4 | En cuartos |

Los bots no tienen cámara.

## Online

Cada ordenador ve solo las cámaras de sus jugadores: un invitado ve su bicho desde detrás, no lo que ve el anfitrión. El stick de cada uno ya se manda girado según su propia cámara. Los marcadores y carteles del show siguen llegando del anfitrión como siempre.

## Ajustes

En el Inspector, durante el juego, cada `Camara del jugador N` lleva un componente `CamaraJugador`:

| Qué tocar | Para qué |
|---|---|
| `distancia`, `alturaMirada`, `inclinacion` | Lo lejos, a qué altura del bicho mira y lo inclinada que está al empezar |
| `campoVision` | Lo abierta que es la vista (60 por defecto) |
| `velocidadStick`, `sensibilidadRaton` | Lo rápido que gira con el stick y con el ratón |
| `esperaAuto`, `suavizadoAuto` | Cuándo vuelve a colocarse sola y lo que tarda |

Para que una carrera use la cámara de grupo, quita la marca `camaraPorJugadorEnCarreras` de la `CamaraGrupo` de su escena.
