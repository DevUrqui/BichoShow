# La Cumbre del Girasol

Gran final del show, todos contra todos. Los bichos suben por una pista en espiral alrededor del tallo de un girasol gigante, dentro de un invernadero con público. Arriba, la cabeza del girasol gira despacio y, en su centro, el **arco de META** gira con ella: gana quien lo cruza primero por su boca.

No hay puntos de control. Desde el suelo sube la **savia**, y quien se queda por debajo se pega y queda eliminado.

## Crearla

En Unity: **BichoShow → Crear escena: La Cumbre del Girasol**.

- Se crea `Escenas/Cumbre.unity` y se añade al build.
- Es una final: cada show acaba ahora con **una** de las dos finales ("Juntos o nada" o esta), elegida al azar. No hace falta rehacer el menú.
- Para jugarla suelta, abre la escena, dale a Play, únete y pulsa START.

## Adaptado a nuestro juego

El diseño original era para 8-12 jugadores a 4,5 m/s. Aquí somos 4 y el bicho corre a 6 m/s, así que:

| Dato | Diseño original | En el juego |
|---|---|---|
| Pista | 320 m | 440 m en casi 4 vueltas |
| Subida | — | 56 m (pendiente del 13 %); los últimos 12 m son llanos, a ras de la cabeza |
| Ancho | — | 13 m → 9 → 7 → 6 (se estrecha como un embudo) |
| Savia | 0,5 m/s | 0,36 m/s, empieza a los 10 s y se para 2,5 m por debajo de la cima |
| Final | Coger la corona en el aire | Cruzar el arco de META, que gira |

Corriendo sin parar se tarda 73 s, y con las pruebas, unos dos minutos. La savia apenas aprieta a quien no se cae. El peligro está en caerse: a la vuelta de abajo pierdes 12 m de altura, y si caes al suelo vuelves a la salida.

## El recorrido

La pista avanza hacia la derecha vista desde fuera. Los carriles tienen colores: verde a los lados y amarillo y naranja en el centro, que es por donde bajan las bolas de polen.

| Sección | Metros | Ancho | Qué hay |
|---|---|---|---|
| 1. La base | 0–110 | 13 m | Diez mariquitas: cuatro quietas que vibran y seis que patrullan de lado a lado. Tres **hojas trampolín** en los bordes. Un tramo sin valla. |
| 2. El bombardeo | 110–230 | 9 m | **Bolas de polen** que bajan rodando por el centro, cada 3 s. Cinco tramos sin valla. Tres **puentes de hoja** cortan por dentro. |
| 3. El tallo ventoso | 230–340 | 7 m | Suelo de **rocío y néctar** que resbala. Seis mariquitas el doble de rápidas. Tres **ventiladores** con ráfagas, y sin valla donde soplan. |
| 4. Las teclas de pétalo | 340–428 | 6 m | La pista se rompe en seis **huecos**. Se cruzan sobre **pétalos piano** que suben y bajan; en dos de los huecos hay dos pétalos a contratiempo. |
| La cima | 428–440 y la cabeza | 6 m | Recta llana hasta la **cabeza giratoria**, con dos mariquitas guardianas y el **arco de META** en el centro. |

Cómo funcionan las pruebas:

- **Mariquitas:** rebotan a quien las toca, hacia fuera y un poco hacia arriba, y lo tumban un momento. Cerca del borde sin valla, te pueden tirar.
- **Hojas trampolín:** lanzan de 13 a 23 m pista arriba. Después se cierran 4 s y se oscurecen. Donde aterrizas hay una hoja pequeña pintada.
- **Bolas de polen:** salen del cañón del metro 229 y bajan por el centro hasta el metro 104. Si te pillan, te empujan cuesta abajo y te tumban. Por el carril de dentro se esquivan mejor.
- **Puentes de hoja:** atajos de 19 m por dentro de la espiral, de una curva a la siguiente. Se inclinan hacia el lado donde haya más peso: si vais dos por el mismo lado, la hoja se ladea.
- **Ventiladores:** cada 5 s avisan (las aspas aceleran) y soplan 1,4 s hacia fuera de la pista. En el néctar se patina, así que hay que ir pegado al lado de dentro.
- **Pétalos piano:** suben y bajan 0,8 m cada 3 s. Hay que saltar cuando el pétalo está bajo. Si caes por el hueco, aterrizas en la vuelta de abajo.
- **La cabeza giratoria:** gira 12° por segundo y te arrastra mientras vas encima. Las dos mariquitas guardianas patrullan del borde al centro y no giran con ella.
- **El arco de META:** gira con la cabeza. Solo cuenta cruzarlo por la boca, de un lado a otro, entre las patas; por los lados no vale. Hay que ponerse delante de la boca mientras da vueltas.

## La cámara

Gira alrededor del tallo y siempre mira hacia dentro, pero con calma para que no maree:

- Mientras el grupo siga cerca del centro del plano (16° a cada lado), no gira.
- Cuando gira, lo hace despacio, con un tope de 22° por segundo, y un poco por delante de los bichos.
- A quien está casi en el eje (en el centro de la cabeza) no le hace caso, así no da bandazos al final.

Si sigue mareando, en el Inspector de `Camara` sube `margenOrbita` (gira menos veces) o baja `velocidadMaximaOrbita` (gira más despacio).

## Eventos de show

- **La savia empieza a subir** (a los 10 s): aviso con alarma.
- **Fiebre del polen** (al minuto): durante 20 s caen el doble de bolas, las luces parpadean doradas y suena la sirena.
- **El abejorro con cámara:** si alguien saca 8 m de altura al segundo, el abejorro se le pone delante para grabarlo y el presentador avisa de que se escapa.
- **Cuando llega el primero:**
  - Cámara lenta y confeti en las gradas.
  - La **savia se desborda**: sube 2,5 veces más deprisa.
  - A los demás les quedan 30 s.

## Cómo se gana

- Por orden de llegada: 300 puntos el primero, 250 el segundo, 200 el tercero y 150 el cuarto (×2, por ser la final).
- Quien no llega se lleva su altura máxima en proporción, hasta 140. Siempre es menos que llegar.
- Pegarse en la savia no quita lo subido: cuenta la altura máxima a la que llegaste.
- Al cruzar la meta, el bicho celebra y deja la pista, para que la cámara siga a los que suben.
- La final acaba cuando todos han llegado o se han pegado, o cuando se acaba el tiempo (al menos 210 s).

## Probarla zona por zona

Juega solo, con los bots activados, y comprueba en orden:

1. **Salida y cámara.** Los 4 bichos caben en la salida. Al subir, la cámara gira con ellos sin marear y no se mete dentro de la pista de arriba.
2. **Sección 1.** El rebote de las mariquitas se nota pero no siempre te tira. Las hojas trampolín te dejan en la pista, no fuera.
3. **Sección 2.**
   - Las bolas se ven venir y se pueden esquivar por dentro.
   - En los huecos de la valla da miedo, pero se pasa.
   - Los puentes se cruzan solo, y con dos encima se ladean.
4. **Sección 3.** El néctar resbala sin ser imposible. La ráfaga se ve venir y, por el lado de dentro, no te tira siempre.
5. **Sección 4.** Los saltos a los pétalos salen esperando el momento. Si caes, aterrizas en la vuelta de abajo, no en el suelo.
6. **La cima.** Se pasa de la pista a la cabeza sin tropezar. Ponerse delante de la boca del arco cuesta un poco, pero no mucho.
7. **Tiempos.** Una final debería durar entre 90 y 180 s. Al acabar, mira `Estadisticas/`: apunta quién se pegó en la savia y dónde se cae la gente.

Si algo no va, estos son los valores a tocar. Están en el Inspector de cada objeto, dentro de `Mapa/Obstaculos` (la savia, en `Mapa/Savia`).

| Objeto | Qué tocar | Para qué |
|---|---|---|
| Savia | `velocidad`, `retraso`, `alturaMaxima` | Lo rápido que sube, cuándo empieza y dónde se para |
| Mariquita | `periodo` | Lo rápido que patrulla (más alto = más lenta) |
| Mariquita | `empujon`, `empujonArriba`, `tropiezo` | Fuerza del rebote y cuánto rato tumba |
| Hoja trampolín | `alturaVuelo`, `cierre` | Lo alto del salto y cuánto rato se cierra |
| Bolas de polen | `velocidad`, `cadencia` | Lo rápido que bajan y cada cuánto sale una |
| Bolas de polen | `empujon`, `tropiezo` | Fuerza del golpe |
| Puente de hoja | `gradosPorMetro`, `inclinacionMaxima` | Cuánto se inclina con el peso |
| Viento | `periodo`, `duracion`, `fuerza` | Cada cuánto sopla, cuánto rato y con qué fuerza |
| Néctar resbaladizo | `agarre` | 0 = hielo, 1 = suelo normal |
| Pétalo | `amplitud`, `periodo` | Cuánto suben y bajan y a qué ritmo |
| Cabeza del girasol | `velocidad` | Lo rápido que gira la cabeza (y el arco) |
| Arco de META | `mediaBoca` | Lo ancha que es la boca que cuenta |
| RondaCumbre | `tiempoTrasPrimero` | Tiempo que les queda a los demás cuando llega el primero |
| RondaCumbre | `saviaDesbordada` | Cuántas veces más deprisa sube la savia cuando llega el primero |
| RondaCumbre | `momentoFiebre`, `duracionFiebre` | Cuándo empieza la fiebre del polen y cuánto dura |
| RondaCumbre | `ventajaParaDron` | Ventaja de altura para que salga el abejorro |
| RondaCumbre | `duracionMinima` | Tiempo límite mínimo de la final |

Las medidas de la espiral están en `Editor/CreadorCumbre.cs`: el largo, el ancho de cada tramo, la pendiente, los huecos de la valla y de la pista, y el sitio de cada prueba. La pista y la valla son mallas que crea el propio generador y se guardan en `Modelos/Girasol/`. Si cambias una medida, vuelve a crear la escena.

## Los bots

- Siguen los puntos de `Mapa/RutaBots`: por el centro de la pista y, en la sección de las bolas, por el carril de dentro.
- Los hábiles cogen a veces los puentes de hoja.
- En los huecos esperan a que el pétalo esté a tiro y saltan de pétalo en pétalo.
- En la cabeza van a un punto delante del arco y a otro detrás. Los dos giran con el arco, así que acaban cruzándolo por la boca.
- Si se caen a una vuelta de abajo, siguen desde el punto de la ruta más cercano que esté por encima.

## Online

Usa las mismas piezas que el resto del show: los obstáculos, la savia y las bolas se mueven en el anfitrión y los invitados ven lo mismo.

## Los modelos de Blender

Lo que se ve sale de `Arte/Blender/crear_kit_girasol.py`, que genera `Modelos/KitGirasol.fbx`:

- **Bichos y obstáculos:** la mariquita, la bola de polen, el abejorro con cámara y sus alas, y el ventilador del plató con sus aspas.
- **Hojas y flor:** la hoja trampolín, la hoja larga de los puentes, las hojas del tallo que sujetan la espiral, los pétalos y la cabeza del girasol con sus semillas.
- **Invernadero:** las costillas de hierro de la cúpula y las banderolas.

El arco de META es el del plató (`KitPlato`), pintado de dorado.

Para regenerar el kit tras cambiar el script:

```
blender -b --factory-startup -P Arte/Blender/crear_kit_girasol.py -- Assets/BichoShow/Modelos/KitGirasol.fbx Arte/Blender/KitGirasol.blend Arte/Blender/vistas
```

Las vistas previas quedan en `Arte/Blender/vistas/kit_girasol_*.png`. Después, vuelve a crear la escena.

Alrededor de la espiral:

- Seis gradas con público, a distintas alturas, sobre plataformas.
- Tres cañones de confeti junto a las gradas, torres de focos, dos pantallas de BICHO SHOW y las banderolas colgando de la cúpula.
- El suelo de baldosas con un círculo de césped al pie del tallo.
