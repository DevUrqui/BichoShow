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

Cada zona deja una bandera coral a los dos lados. Es un **punto de control**: si te caes de la mesa, vuelves a la última que cruzaste.

| Zona | Z | Qué hay | Punto de control |
|---|---|---|---|
| 1. Salida: la Tabla de Corte | 0–23 | Losa de 18×14 m con cuadrícula y el arco de SALIDA. Después, un hueco de 1,5 m y un escalón de 0,6 m para aprender a saltar. | 1, en z 18 |
| 2. Los Carretes Viajeros | 23–44 | Tres franjas de 4 m. En cada una rueda un carrete de lado a lado, cada vez más rápido (5,5 s, 4,5 s y 3,4 s por ida y vuelta). La franja azul del suelo marca su recorrido. Entre franjas hay zonas seguras con pespunte. | 2, en z 46,5 |
| 3. El Puente Acordeón | 44–79 | Seis paneles seguidos de 3×4 m que se inclinan 30° de lado por turnos y vuelven a ponerse planos. Cada uno va 0,525 s después del anterior, lo que tarda un bicho en cruzar uno. El camino del lado es más largo, plano y con barandilla. | 3, en z 75,5 |
| 4. Las Cremalleras Gemelas | 79–108 | Izquierda: ancha (4,4 m) y lenta, en dos mitades con un descansillo. Derecha: estrecha (2,4 m) y de una tirada, con el tirador más rápido. | 4, en z 104,5 |
| 5. La Subida del Dedal | 108–147 | Rampa, alfiletero 1 (fijo), hueco de 2 m al alfiletero 2 (se hunde) y salto, o escalón y rampa del lado, al alfiletero 3. Después, la cinta métrica y la META. | 5, en z 113,5 |

Cómo funcionan los obstáculos:

- **Carretes:** empujan a quien tocan hacia el lado al que van, y lo tumban un momento.
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
2. **Carretes.**
   - Se entiende por dónde va cada carrete antes de entrar.
   - Se puede esperar tranquilo en las zonas seguras.
   - El empujón tira de la franja si te pilla cerca del borde, pero no siempre.
3. **Acordeón.**
   - Entrando en buen momento, se cruza seguido.
   - Si no, saltar un panel inclinado es posible.
   - El camino del lado tarda más que el puente bien hecho, pero no muchísimo más.
4. **Cremalleras.**
   - Se ve venir cuándo se abre cada tramo.
   - La ruta estrecha es más rápida si sales a tiempo.
   - La ancha nunca te tira si esperas en el descansillo.
5. **Dedal.**
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
