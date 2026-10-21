# La Cumbre del Girasol

Gran final del show, todos contra todos. Los bichos suben por una pista en espiral alrededor del tallo de un girasol gigante, dentro de un invernadero con público. Arriba, sobre el disco del girasol, flota la **Corona Dorada**: gana quien la coge.

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
| Pista | 320 m | 300 m en 3 vueltas, más el anillo de pétalos |
| Subida | — | 45 m (pendiente del 15 %); el disco está a 50 m |
| Ancho | — | 13 m → 9 → 7 → 6 (se estrecha como un embudo) |
| Savia | 0,5 m/s | 0,38 m/s, empieza a los 8 s y se para a 47 m |

Subiendo sin caerse se llega arriba en menos de un minuto. La savia apenas aprieta a quien no se cae; el peligro está en caerse.

## El recorrido

La pista avanza hacia la derecha vista desde fuera. La cámara orbita alrededor del tallo y siempre mira hacia dentro.

Los carriles tienen colores: verde a los lados y amarillo y naranja en el centro, que es por donde bajan las bolas de polen.

| Sección | Metros | Ancho | Qué hay |
|---|---|---|---|
| 1. La base | 0–80 | 13 m | Mariquitas: tres quietas que vibran y tres que patrullan de lado a lado. **Hojas trampolín** en los bordes. |
| 2. El bombardeo | 80–180 | 9 m | **Bolas de polen** que bajan rodando por el centro. La valla de fuera falta en algunos tramos. Dos **puentes de hoja** cortan por dentro. |
| 3. El tallo ventoso | 180–270 | 7 m | Suelo de **rocío y néctar** que resbala. Cuatro mariquitas el doble de rápidas. Dos **ventiladores** con ráfagas. |
| 4. La corona | 270–300 y la cima | 6 m | Seis **pétalos piano** hasta el disco, y la corona sobre el géiser de polen. |

Cómo funcionan las pruebas:

- **Mariquitas:** rebotan a quien las toca, hacia fuera y un poco hacia arriba, y lo tumban un momento. Cerca del borde sin valla, te pueden tirar.
- **Hojas trampolín:** lanzan unos 20 m pista arriba (un atajo de ~3 s). Después se cierran 4 s y se oscurecen. Donde aterrizas hay una hoja pequeña pintada.
- **Bolas de polen:** salen del cañón del metro 179 cada 3,5 s y bajan por el centro hasta el metro 74. Si te pillan, te empujan cuesta abajo y te tumban. Por el carril de dentro se esquivan mejor.
- **Puentes de hoja:** atajos por dentro de la espiral, de una curva a la siguiente. Se inclinan hacia el lado donde haya más peso: si vais dos por el mismo lado, la hoja se ladea.
- **Ventiladores:** cada 5 s avisan (las aspas aceleran) y soplan 1,4 s hacia fuera de la pista. En el néctar se patina, así que hay que agarrarse al lado de dentro.
- **Pétalos piano:** suben y bajan como teclas, cada uno un poco después del anterior. Se salta de uno a otro cuando el siguiente está bajo.
- **La corona:** sube y baja y gira sobre el disco. Se coge saltando con **Agarrar** (la mano estirada).

## Eventos de show

- **La savia empieza a subir** (a los 8 s): aviso con alarma.
- **Fiebre del polen** (al minuto): durante 20 s caen el doble de bolas, las luces parpadean doradas y suena la sirena.
- **El abejorro con cámara:** si alguien saca 7 m de altura al segundo, el abejorro se le pone delante para grabarlo, y el presentador avisa de que está a punto de coronar.
- **Al coronar:** cámara lenta, confeti en las gradas y fin de la final.

## Cómo se gana

- Coronar da 300 puntos (×2, por ser la final).
- Los demás se llevan su altura máxima en proporción: hasta 250 si se llega arriba sin coger la corona.
- Pegarse en la savia no quita lo subido: cuenta la altura máxima a la que llegaste.
- La final acaba cuando alguien corona o cuando no queda nadie subiendo. El tiempo límite es de al menos 150 s.

## Probarla zona por zona

Juega solo, con los bots activados, y comprueba en orden:

1. **Salida y cámara.** Los 4 bichos caben en la salida. Al subir, la cámara gira con ellos alrededor del tallo y no se mete dentro de la pista de arriba.
2. **Sección 1.** El rebote de las mariquitas se nota pero no siempre te tira. Las hojas trampolín te dejan en la pista, no fuera.
3. **Sección 2.** Las bolas se ven venir y se pueden esquivar por dentro. En los huecos de la valla da miedo pero se pasa. Los puentes se cruzan solo, y con dos encima se ladean.
4. **Sección 3.** El néctar resbala sin ser imposible. La ráfaga se ve venir y, por el lado de dentro, no te tira siempre.
5. **Sección 4.** Los saltos entre pétalos salen esperando el momento. La corona se alcanza de un salto desde el disco con Agarrar.
6. **Tiempos.** Una final debería durar entre 60 y 150 s. Al acabar, mira `Estadisticas/`: apunta quién se pegó en la savia y dónde se cae la gente.

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
| Corona Dorada | `alcance`, `amplitud` | A qué distancia se coge y cuánto se mueve |
| RondaCumbre | `momentoFiebre`, `duracionFiebre` | Cuándo empieza la fiebre del polen y cuánto dura |
| RondaCumbre | `ventajaParaDron` | Ventaja de altura para que salga el abejorro |
| RondaCumbre | `duracionMinima` | Tiempo límite mínimo de la final |

Las medidas de la espiral (ancho por tramos, pendiente, huecos de la valla y sitio de cada prueba) están en `Editor/CreadorCumbre.cs`. La pista y la valla son mallas que crea el propio generador y se guardan en `Modelos/Girasol/`. Si cambias una medida, vuelve a crear la escena.

## Los bots

- Siguen los puntos de `Mapa/RutaBots`: por el centro de la pista y, en la sección de las bolas, por el carril de dentro.
- Los hábiles cogen a veces los puentes de hoja.
- En los pétalos esperan a que el siguiente esté a tiro. En el disco saltan con la mano estirada hasta coger la corona.
- Si se caen a una vuelta de abajo, siguen desde el punto de la ruta más cercano que esté por encima.

## Online

Usa las mismas piezas que el resto del show: los obstáculos, la savia y las bolas se mueven en el anfitrión y los invitados ven lo mismo.

## Los modelos de Blender

Lo que se ve sale de `Arte/Blender/crear_kit_girasol.py`, que genera `Modelos/KitGirasol.fbx`:

- **Bichos y obstáculos:** la mariquita, la bola de polen, el abejorro con cámara y sus alas, y el ventilador del plató con sus aspas.
- **Hojas y flor:** la hoja trampolín, la hoja larga de los puentes, las hojas del tallo que sujetan la espiral, los pétalos y la cabeza del girasol con sus semillas.
- **Invernadero:** las costillas de hierro de la cúpula y las banderolas.

Para regenerar el kit tras cambiar el script:

```
blender -b --factory-startup -P Arte/Blender/crear_kit_girasol.py -- Assets/BichoShow/Modelos/KitGirasol.fbx Arte/Blender/KitGirasol.blend Arte/Blender/vistas
```

Las vistas previas quedan en `Arte/Blender/vistas/kit_girasol_*.png`. Después, vuelve a crear la escena.

Alrededor de la espiral:

- Seis gradas con público, a distintas alturas, sobre plataformas.
- Tres cañones de confeti, torres de focos, dos pantallas de BICHO SHOW y las banderolas colgando de la cúpula.
- El suelo de baldosas con un círculo de césped al pie del tallo.
