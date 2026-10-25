# La gran final: Juntos o nada

Final por parejas: rojos (jugadores 1 y 3) contra azules (2 y 4). Cada pareja tiene su carril y solo avanza cooperando. Gana la primera pareja que llega a la meta, y cada prueba superada da puntos. Las estrellas valen el doble.

## Crearla

En Unity: **BichoShow → Crear escena: Gran final (Juntos o nada)**.

- Si ya la tenías creada, al darle a Play sale un aviso para rehacerla con la versión nueva (versión 3).
- Dura al menos **4 minutos**, aunque el show pida rondas más cortas.

## El carril (de la salida a la meta, unos 150 m)

| # | Prueba | Cómo se supera |
|---|---|---|
| 1 | **El bloque** | Un pasillo tapado por un bloque pesado: hay que empujarlo entre los dos hasta que caiga al foso. Un techo bajo (invisible, con vigas amarillas) impide subirse al bloque y pasar por encima. |
| 2 | **Puente levadizo** | Solo está bajado mientras alguien pisa una de sus placas: uno pisa, el otro cruza y pisa la del otro lado. Hay una barredora. |
| 3 | **Puerta doble** | Se abre unos segundos al pisar a la vez dos placas separadas. Hay un empujador. |
| 4 | **Pasillos cruzados** | Cada uno va por un pasillo, separados por una pared. Las compuertas de tu pasillo las abren las placas del otro: hay que avanzar a saltos de rana, uno abriéndole paso al otro. El último no puede soltar su placa hasta que salga su pareja. |
| 5 | **Camino de baldosas** | Un hueco con un camino de tres tramos. Cada tramo solo está mientras se pisa su placa (1, 2 y 3, en la orilla). Al soltarla, el tramo parpadea en rojo y desaparece. El que cruza pisa luego la placa del otro lado, que enciende todo el camino para que pase su pareja. Un empujador molesta al que pisa las placas. |
| 6 | **Balancín** | Una tabla de 20 m sobre el vacío que se inclina hacia donde hay más peso. Si se inclina mucho, se resbala hacia el lado que baja. Uno cruza y el otro hace de contrapeso, colocándose en el lado contrario. Al llegar, el primero pisa el **freno**, que deja la tabla recta para que pase su pareja. Antes hay una barredora. |
| 7 | **Ascensor** | Solo sube con los dos encima, hasta la meta en lo alto. Hay un empujador. |

- Si te caes, vuelves al principio de la prueba en la que está tu pareja.
- Si te quedas atrapado (delante del bloque o en el fondo de un foso), a los 3 s vuelves solo al principio de la prueba.
- Si un equipo tiene un solo jugador, todo se le pone fácil: el puente bajado, las compuertas abiertas, el camino de baldosas encendido, el balancín frenado y el ascensor sube con uno.
- Las barredoras y los empujadores son trampas: con saboteador se mueven solo cuando él las activa.

## Bots

Cada bot pregunta a su prueba qué hacer según su papel (rol 0 o 1) y lo que hace su pareja:

- **Pasillos:** cada uno va por un pasillo. Si su compuerta está abierta, avanza a la siguiente placa; si no, espera sobre la suya.
- **Baldosas:** uno cruza tramo a tramo; el otro pisa la placa del tramo por el que va su pareja.
- **Balancín:** uno cruza, parándose si la tabla se le inclina hacia delante; el otro se coloca en el espejo del que cruza.

Cuando esperan encima de una placa, o junto al bloque, no dan saltitos para pedir ayuda: en el aire soltarían la placa, y junto al bloque se subían encima. Antes de cruzar las baldosas o el balancín se ponen en línea con el camino, porque en diagonal se caían.

En una prueba solo con bots, al doble de velocidad, las dos parejas llegan a la meta en unos 60-80 s de juego. Antes, con cuatro pruebas, tardaban unos 40. Las personas, que se tienen que coordinar, tardan bastante más.

## Dónde está

- `Editor/CreadorFinal.cs`: el mapa.
- `Final/`: la ronda (`RondaFinal`), los bots (`BotFinal`) y cada prueba:
  - `PasoBloque`, `PasoPuente`, `PasoPuerta`, `PasoPasillos`, `PasoBaldosas`, `PasoBalancin` y `PasoAscensor`.
  - Sus piezas: `PuenteLevadizo`, `PuertaDoble`, `Compuerta`, `CaminoBaldosas`, `Balancin`, `Ascensor` y `PlacaPresion`.
