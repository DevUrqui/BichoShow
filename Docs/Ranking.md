# Nombres y ranking de tiempos

## Tu nombre

En **Vestuario**, la primera fila es **Nombre**. Sale en los avisos ("¡Ivan llega 1º!"), en las fichas del menú y en el ranking. Online, los demás también lo ven.

- **Con mando**, como en las recreativas:
  - Pulsa **A** en la fila "Nombre" para empezar a escribir.
  - **Arriba y abajo** cambian la letra.
  - **Izquierda y derecha** mueven el cursor.
  - **X** borra.
  - **A** guarda el nombre.
- **Con teclado:** escribe directamente. **Retroceso** borra y **Enter** guarda.

Cada mando de este ordenador tiene su nombre, de hasta 12 letras. Quien no escribe ninguno sale como J1, J2…

## El ranking

En las carreras (**Costura a la fuga**, **Despensa Deslizante** y **La Cumbre del Girasol**) se cronometra a cada jugador desde la salida hasta la meta. Los bots no cuentan. Al llegar sale tu tiempo y, si entras en el ranking, el puesto (o "¡NUEVO RÉCORD!").

En el menú, **Ranking** enseña dos tablas por carrera (izquierda y derecha cambian de carrera):

- **En este ordenador:** los 10 mejores tiempos que se han hecho aquí, el mejor de cada nombre. Se guarda en `ranking.json`, en la carpeta de datos del juego.
- **Mundial:** los 10 mejores de todo el mundo, el mejor de cada nombre. Necesita internet y crear los rankings una vez (abajo).

Online, a cada jugador se le apunta el tiempo en su propio ordenador, con su nombre, y se sube al mundial.

## Crear el ranking mundial (una vez)

Usa **Unity Leaderboards**, del mismo proyecto de Unity Cloud que las salas online.

1. Abre el proyecto en Unity. La primera vez se instala solo el paquete **Leaderboards**; puede tardar un poco.
2. Abre la ventana de despliegue: menú **Services → Deployment**. Si no está ahí, búscala en **Window → Services → Deployment**.
3. Verás los tres rankings de `Assets/BichoShow/Rankings`: `tiempo-costura`, `tiempo-despensa` y `tiempo-cumbre`. Márcalos y pulsa **Deploy**.

Ya está: el juego sube los tiempos y los lee solo.

**Otra forma:** en [cloud.unity.com](https://cloud.unity.com), en tu proyecto, ve a **Leaderboards** y crea tres rankings con esos ids exactos. Configúralos con orden **ascendente** (menos tiempo, mejor) y guardando la **mejor** puntuación (keep best).

Si el ranking aún no está creado, la tabla mundial dirá "Falta crear el ranking". El de este ordenador funciona igual.

**Ten en cuenta:** cada vez que abres el juego, Unity te da un jugador anónimo nuevo; así se pueden abrir dos copias en el mismo PC para probar el online. Por eso en el mundial se junta el mejor tiempo de cada **nombre**: usa siempre el mismo.

## Añadir el ranking a otra carrera

1. En su ronda, sobrescribe `IdRanking` (por ejemplo `public override string IdRanking => "pista";`). La ronda tiene que heredar de `RondaRuta` y llamar a `ApuntarTiempo(b)` al cruzar la meta. `RondaCarrera` ya lo hace.
2. Añádela a `RankingTiempos.Carreras` con su nombre.
3. Copia uno de los `.lb` de `Assets/BichoShow/Rankings` como `tiempo-pista.lb` y despliégalo.
