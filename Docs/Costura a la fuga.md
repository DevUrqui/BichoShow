# Costura a la fuga

Carrera de obstáculos sobre una mesa de costura. Los bichos son del tamaño de insectos y corren de la Tabla de Corte hasta la meta, junto a un dedal gigante.

Esta es la **versión de prueba**: bloques redondeados y colores planos, con la paleta del mapa (turquesa, mostaza, coral y azul oscuro). Cuando las medidas estén bien, cambiaremos los bloques por modelos de Blender.

## Crearla

En Unity: **BichoShow → Crear escena: Costura a la fuga**.

- Se crea `Escenas/Costura.unity`, se añade al build y entra sola en el sorteo del show; no hace falta rehacer el menú.
- Para jugarla suelta, abre la escena, dale a Play, únete y pulsa START.

## Las medidas, según el bicho

| Dato del bicho | Valor |
|---|---|
| Velocidad corriendo | 6 m/s |
| Altura del salto | ~2,1 m |
| Largo del salto corriendo | ~6,5 m |
| Cuerpo | cápsula de 0,9 m de ancho y 1,4 m de alto |

Con eso, los huecos que hay que saltar miden entre 1,5 y 2,5 m y los escalones suben como mucho 1,2 m. Ningún paso obligatorio mide menos de 2,4 m de ancho.

## El recorrido (a lo largo del eje Z)

Después de la salida hay **cuatro cruces**. En cada uno se elige entre dos caminos con pruebas distintas, y los dos se juntan en un descanso. Los descansos están desplazados a un lado u otro, así que la carrera va zigzagueando.

En cada descanso hay una bandera coral a los dos lados. Es un **punto de control**: si te caes de la mesa, vuelves a la última que cruzaste.

| Tramo | Z | Camino rápido (más difícil) | Camino seguro | Punto de control |
|---|---|---|---|---|
| Salida | 0–23 | Tabla de corte con el arco de SALIDA, un saltito y el escalón ancho donde se elige camino | | 1, en z 18 |
| 1 | 23–46 | **Botones giratorios** (derecha): cuatro botones gigantes que giran; hay que saltar de uno a otro | **Carretes viajeros** (izquierda): tres franjas; hay que esperar el hueco | 2, Descanso 1 (z 48,5) |
| 2 | 53–72 | **Puente acordeón** (izquierda): seis paneles que se inclinan en ola | **Cinta métrica rodante** (derecha): va hacia atrás y la barren dos agujas de tejer que hay que saltar | 3, Descanso 2 (z 74,5) |
| 3 | 78–100 | **Cremallera estrecha** (derecha): de una tirada, tirador rápido | **Cremallera ancha** (izquierda): lenta, con descansillo | 4, Destino (z 103,5) |
| 4 | 107–137 | **Escalera de carretes** (derecha): carretes de pie, cada uno más alto; dos giran | **Alfileteros** (izquierda): el del medio se hunde; escalón y rampa del lado | 5: en el alfiletero 1 o en el carrete del medio |
| Meta | 137–146 | Plataforma ancha con el arco de META y el dedal gigante | | |

Cómo funcionan los obstáculos:

- **Botones giratorios y carretes de la escalera:** giran y te arrastran mientras vas encima. Hay que saltar al siguiente antes de que el giro te saque por el borde.
- **Carretes viajeros:** empujan a quien tocan hacia el lado al que van, y lo tumban un momento.
- **Cinta métrica rodante:** te lleva hacia atrás a 2,2 m/s (sus marcas y flechas se mueven). Las agujas de tejer giran a ras de la cinta: hay que saltarlas. Si en el show hay saboteador, puede acelerarlas.
- **Puente Acordeón:** quien entra justo cuando el primer panel se pone plano cruza en "ola verde". Si no, hay que saltar los paneles inclinados. Antes de inclinarse, cada panel tiembla y hace clic.
- **Cremalleras:** el tirador dorado va del principio al final abriendo los tramos que deja atrás: se doblan hacia abajo. Luego vuelve cerrándolos. Antes de abrirse, cada tramo tiembla y hace clic. Hay que salir justo cuando todo está cerrado e ir por delante del tirador.
- **Alfiletero 2:** se hunde 0,8 m en un segundo si te quedas encima y tarda casi 3 s en subir. Para saltar al 3 hay que hacerlo enseguida; si no, se va por el escalón y la rampa del lado.

Cómo se gana:

- El 1º se lleva 200 puntos, el 2º 170, el 3º 140 y el 4º 110.
- Quien no llega se lleva su porcentaje de carrera, que siempre es menos.
- Cuando llega el primero, a los demás les quedan 30 s.
- Al llegar, el bicho celebra y deja el circuito, para que la cámara siga a los que aún corren.

## Probarla zona por zona

Juega solo, con los bots activados, y comprueba en orden:

1. **Salida.** El salto al escalón sale sin pensar y caben los 4 bichos en la salida sin estorbarse.
2. **Botones y carretes.**
   - Los saltos entre botones salen sin pasarse de largo.
   - El giro se nota, pero no te tira solo.
   - En la otra ruta:
   - Se entiende por dónde va cada carrete antes de entrar.
   - Se puede esperar tranquilo en las zonas seguras.
   - El empujón tira de la franja si te pilla cerca del borde, pero no siempre.
3. **Acordeón y cinta.**
   - La cinta deja avanzar corriendo, y las agujas se pueden saltar.
   - En el acordeón:
   - Entrando en buen momento, se cruza seguido.
   - Si no, saltar un panel inclinado es posible.
   - La cinta tarda más que el puente bien hecho, pero no muchísimo más.
4. **Cremalleras.**
   - Se ve venir cuándo se abre cada tramo.
   - La ruta estrecha es más rápida si sales a tiempo.
   - La ancha nunca te tira si esperas en el descansillo.
5. **Escalera de carretes y alfileteros.**
   - En la escalera, cada salto sube poco y sale bien, también en los dos carretes que giran.
   - En los alfileteros:
   - El salto al alfiletero 2 sale siempre.
   - Del 2 al 3 se puede saltar enseguida; si esperas, ya no llegas.
   - La rampa del lado siempre funciona.
6. **Tiempos.** Una carrera debería durar entre 90 y 150 s. Al acabar, mira `Estadisticas/`: apunta las caídas y en qué punto de control va cada uno.

Si algo no va, estos son los valores a tocar. Están en el Inspector de cada objeto, dentro de `Mapa/Obstaculos`.

| Objeto | Qué tocar | Para qué |
|---|---|---|
| Carrete | `periodo` | Más alto = más lento |
| Carrete | `empujon` | Fuerza del golpe |
| Carrete | `recorrido` | Hasta dónde llega a cada lado |
| Botón o carrete que gira | `velocidad` | Grados por segundo (negativo = al otro lado) |
| Cinta rodante (motor) | `velocidad` | Lo rápido que te lleva hacia atrás |
| Aguja de tejer | `velocidadGiro` | Lo rápido que barre la cinta |
| Panel acordeón | `anguloMaximo` | Cuánto se inclina |
| Panel acordeón | `tiempoPlano` | Cuánto rato está plano |
| Panel acordeón | `retraso` | La ola entre paneles |
| Cremallera | `velocidad` | Velocidad del tirador |
| Cremallera | `pausaCerrada` | Cuánto espera con todo cerrado al principio |
| Cremallera | `pausaAbierta` | Cuánto espera con todo abierto al final |
| Alfiletero 2 (blando) | `hundimiento`, `velocidadBajar`, `velocidadSubir` | Cuánto y a qué ritmo se hunde y se recupera |
| RondaCostura | `duracionMinima` | Duración mínima de la carrera |
| RondaCostura | `tiempoTrasPrimero` | Tiempo que les queda a los demás cuando llega el primero |

Las medidas del recorrido, en cambio, están en `Editor/CreadorCostura.cs`, con un comentario en cada zona. Si cambias una distancia, vuelve a crear la escena.

## Los bots

- Siguen los puntos de `Mapa/RutaBots`, que en la vista de escena se ven como bolitas: naranja = saltar, roja = esperar.
- Esperan a que el carrete no vaya a pasar por su carril, a que el primer panel se ponga plano o a que la cremallera esté cerrada.
- Los hábiles cogen más el puente y la cremallera estrecha.
- Si se caen, siguen desde el punto de la ruta más cercano.

## Online

La escena usa las mismas piezas que el resto del show: los obstáculos se mueven en el anfitrión y los invitados ven lo mismo. No hay que hacer nada especial.

## Los modelos de Blender

Lo que se ve sale de `Arte/Blender/crear_kit_costura.py`, que genera `Modelos/KitCostura.fbx`. Las colisiones siguen siendo las cajas de la versión de prueba; solo cambia el dibujo.

Las piezas:

- **Obstáculos**:
  - Carrete con vueltas de hilo y tapas de madera. Los agujeros y los hilos cruzados dejan ver que rueda.
  - Panel del acordeón: tabla con dos almohadillas y pespunte.
  - Dos medias cremalleras (ancha y estrecha) con sus dientes, y el tirador.
- **Suelos**:
  - Dos alfileteros acolchados (12×7 y 14,5×6,5), con botones donde se hunde la tela.
  - El puente de cinta métrica.
- **Decorado**:
  - El arco de tela de SALIDA y META.
  - El dedal, los banderines y las flechas cosidas.
  - Al fondo: botones, carretes de pie, tijeras y el rollo de cinta métrica.

"Equipo" es la tela o el hilo; el generador le pone el color de cada sitio (coral, azul, turquesa o mostaza).

Para regenerar el kit tras cambiar el script:

```
blender -b --factory-startup -P Arte/Blender/crear_kit_costura.py -- Assets/BichoShow/Modelos/KitCostura.fbx Arte/Blender/KitCostura.blend Arte/Blender/vistas
```

Las vistas previas quedan en `Arte/Blender/vistas/kit_costura_*.png`; una de ellas está tomada desde la cámara del juego. Después, vuelve a crear la escena.

Las losas se visten en el propio generador (función `Vestir`), así valen para cualquier medida:

- Marco de madera debajo, ribete acolchado en el borde y botones cosidos en el frente.
- Los descansos y el camino lateral llevan parches de tela en damero, con su pespunte (alguno en coral).
- La tabla de corte y la meta llevan cuadrícula y regla.

Telas con dibujo (`Editor/TelasCostura.cs`). Las texturas se generan con código en gris y blanco, y el color lo pone cada material:

- Cuadros vichy en los descansos.
- Lunares en los suelos amarillos.
- Cuadrícula fina en la tabla de corte.
- Vetas en la madera.
- Papel pintado en la pared.
- Parches de lunares, vichy y rayas.

Alrededor del recorrido:

- Cuatro gradas con público de bichos (del kit del plató), encima de carretes gigantes, y torres de focos en la salida y la meta.
- En la mesa: la máquina de coser gigante, el acerico de tomate con alfileres, rollos de tela, un flexo, macetas, botones, tijeras y una estantería con rollos.
- En la salida, los descansos y la meta: guirnaldas de banderitas. La salida y la meta llevan además barandillas de hilo con postes de botón.
