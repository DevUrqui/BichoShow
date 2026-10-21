# Despensa Deslizante

Carrera de obstáculos por la encimera de una cocina, todos contra todos. Hay cuatro zonas separadas por tres arcos de control. Si te caes a la encimera, al agua del fregadero o al cubo de la cinta, vuelves al último arco que cruzaste: pierdes unos segundos, pero no quedas eliminado.

## Crearla

En Unity: **BichoShow → Crear escena: Despensa Deslizante**.

- Se crea `Escenas/Despensa.unity`, se añade al build y entra sola en el sorteo del show; no hace falta rehacer el menú.
- Para jugarla suelta, abre la escena, dale a Play, únete y pulsa START.
- Si más adelante cambia el generador, al darle a Play Unity avisa y ofrece rehacerla.

## Adaptado a nuestro juego

El diseño original era una primera ronda para 40-60 jugadores, de la que se clasificaba el 60 %. Aquí somos 4, así que:

| Dato | Diseño original | En el juego |
|---|---|---|
| Pista | 360 × 35 m | Unos 215 m, de 14 a 24 m de ancho |
| Clasificación | El 60 % pasa | Puntos por orden de llegada: 200, 170, 140 y 110; quien no llega, según lo lejos que fue |
| Cierre | Alarma de horno cuando quedan 5 plazas | Cuando llega el primero suena el temporizador del horno (tic-tac cada vez más rápido) y quedan 25 s |
| Últimos segundos | El público golpea las gradas | Igual: en los últimos 20 s el público se vuelve loco y los saleros tiemblan el doble |

## La cámara

Como en todas las carreras, cada jugador lleva su cámara detrás de su bicho (ver `Docs/Camaras en las carreras.md`).

## El recorrido

| Zona | Z | Qué hay |
|---|---|---|
| 1. El mantel de los rodillos | 0–62 | Salida acolchada bajo el arco BICHO SHOW y una bajadita. Tres **rodillos de amasar** ruedan hacia la salida y vuelven, cada uno en su tramo. Seis **saleros** en los bordes. |
| 2. El fregadero espumoso | 62–112 | Agua jabonosa rosa. Por el centro, dos **esponjas**; por los lados, **tapones** pequeños que giran rápido. Dos **grifos gigantes**, uno en cada pared, barren el fregadero a destiempo. |
| 3. La rampa de mantequilla | 112–170 | Rampa resbaladiza de 50 m que sube 12 m. Bajan **tomates** por el centro y **arándanos** por los bordes, muy seguidos. Hay tres **muros de refugio** y una **lámpara de calor**. |
| 4. Las bandejas de postre | 166–236 | Cuatro **colchones elásticos** pequeños en dos filas, que **van y vienen de lado**, con huecos entre ellos y una **cinta** debajo. Dos saleros junto a la meta. Después, la meta bajo el marcador y el **podio**. |

Cómo funcionan las pruebas:

- **Rodillos de amasar:** ruedan hacia ti a 6 m/s, se paran medio segundo y vuelven a 4,5 m/s. Si te pillan, te aplastan y te echan atrás.
  - El cuerpo es gordo (1,8 m): se puede saltar, pero hay que clavar el momento.
  - Los mangos de los lados son finos y bajos: se saltan fácil.
  - Por las esquinas se puede bordear el rodillo, pero ahí están los saleros.
- **Saleros:** rebotan a quien los toca. Cada 3 s tiemblan y echan un círculo de sal de 4,5 m de radio en el que casi no hay agarre.
- **Esponjas (ruta rápida):**
  - Se hunden 0,3 m por cada bicho que llevan encima.
  - Se **empapan** si hay 2 bichos a la vez o si alguien se queda encima más de 2,2 s: tiemblan, sueltan espuma y se hunden bajo el agua 2 s.
  - Por el centro se va rápido, pero no te puedes parar.
- **Tapones (ruta lateral):** pequeños (radio 1,1 m), giran rápido y suben y bajan, pero no se hunden. Hay que ir saltando de uno a otro.
- **Grifos gigantes:**
  - Hay dos, uno en cada pared y a destiempo. Cada 4,5 s gotean para avisar y abren el chorro.
  - El de la derecha barre desde sus tapones hasta pasado el centro; el de la izquierda, desde los suyos hasta el otro lado del centro.
  - A quien pillan lo empujan de lado, normalmente al agua. Ninguna ruta queda a salvo.
- **Mantequilla:**
  - Se agarra la mitad.
  - Si saltas mientras subes, al caer patinas cuesta abajo: hay que subir corriendo.
- **Tomates y arándanos:**
  - Los tomates bajan rectos por el centro a 10 m/s, uno cada segundo más o menos.
  - Los arándanos bajan a 12 m/s por los bordes, dando botes de lado.
  - Si te dan, te derriban y patinas hasta abajo.
  - Al llegar a un muro de refugio, la fruta revienta contra él: detrás de los muros se puede esperar a ver pasar la fruta.
- **Lámpara de calor:** cada pocos segundos se enciende y baja por un raíl. Debajo de ella, una franja naranja y brillante de mantequilla derretida sube por la rampa. Dentro de la franja casi no hay agarre.
- **Colchones elásticos:**
  - Miden 4,8 m y van y vienen 2,4 m a cada lado, a destiempo: hay que apuntar.
  - Al pisarlos te lanzan hacia delante unos 14 m, justo hasta la siguiente fila.
  - El lado lo decides tú: se conserva lo que ibas de lado.
  - Si te quedas corto, caes a la **cinta** de abajo, que te lleva hacia atrás y te tira al cubo; de ahí vuelves al último arco.
- **Podio:** quien llega sube al podio (1º, 2º, 3º, y el 4º al lado) y se queda celebrando mientras los demás sufren. La cámara deja de encuadrarlo.

### Cosas propias para que no sea un Fall Guys

El diseño se inspira en mapas de Fall Guys y la rampa con fruta que rueda se parece mucho a uno de ellos. Para que tenga personalidad propia, hay cuatro cosas que no estaban en el diseño:

- La **lámpara de calor** que derrite franjas de mantequilla.
- Las **esponjas que se empapan** si te quedas quieto encima, no solo por el peso.
- La **fruta que revienta** contra los muros de refugio, lo que convierte los muros en escondites de verdad.
- Los **colchones** con control lateral.

## Eventos de show

- **Grúas de cámara:** en el borde de la encimera, dos grúas giran siguiendo al pelotón. Si un tomate te tira fuera de la rampa, sales volando contra la cámara y queda un **"¡PLAF!" de bicho espachurrado** en la pantalla de todos durante un segundo.
- **Temporizador del horno:** cuando llega el primero, empieza un tic-tac cada vez más rápido y quedan 25 s.
- **El público golpea las gradas:** en los últimos 20 s el público se vuelve loco y los saleros tiemblan cada 2 s.
- **Carteles de zona:** al cruzar cada arco sale "¡ZONA 2: el fregadero!", "¡ZONA 3…!", etc.

## Probarla zona por zona

Juega solo, con los bots activados, y comprueba en orden:

1. **Salida.** Caben los 4 y la bajada da velocidad sin tirar a nadie.
2. **Rodillos.**
   - El cuerpo se puede saltar con buen momento.
   - Los mangos se saltan fácil.
   - El empujón echa atrás, pero no fuera de la encimera.
3. **Saleros.** La sal se ve y se nota que patina. El rebote no te tira siempre de la encimera.
4. **Fregadero.**
   - El salto a la primera esponja y a los tapones sale.
   - La esponja se empapa si te paras, y avisa antes.
   - El chorro del grifo se ve venir.
5. **Mantequilla.**
   - Subiendo sin saltar se sube.
   - Saltando, se patina.
   - La fruta se esquiva, y detrás de los muros se está a salvo.
   - La franja derretida se ve.
6. **Colchones.** Desde la primera fila se llega a la segunda y desde la segunda, a la meta. Si caes, la cinta te devuelve.
7. **Tiempos.** Una carrera debería durar entre 90 y 120 s. Al acabar, mira `Estadisticas/`: apunta las caídas y dónde se pierde más tiempo.

Si algo no va, estos son los valores a tocar. Están en el Inspector de cada objeto, dentro de `Mapa/Obstaculos`.

| Objeto | Qué tocar | Para qué |
|---|---|---|
| Rodillo | `velocidadIda`, `velocidadVuelta`, `pausa` | Lo rápido que va y viene, y cuánto se para |
| Rodillo | `empujon`, `tumbado` | Fuerza del aplastón |
| Salero | `periodo`, `periodoPrisa` | Cada cuánto echa sal (normal y al final) |
| Salero | `radioSal`, `agarreSal`, `duracionSal` | Tamaño de la sal, cuánto resbala y cuánto dura |
| Esponja | `hundimientoPorBicho`, `limite`, `tiempoEmpapar`, `duracionEmpapada` | Cuánto se hunde, con cuántos se empapa, cuánto aguanta y cuánto rato se queda bajo el agua |
| Tapón | `velocidad`, `balanceo` | Lo rápido que gira y cuánto flota |
| Grifo | `periodo`, `duracionBarrido`, `anguloA`, `anguloB`, `empujon`, `desfase` | Cada cuánto barre, lo rápido, de dónde a dónde, con qué fuerza y su retraso respecto al otro |
| Mantequilla | `agarre`, `resbalonSalto` | Agarre normal y lo que patinas si saltas |
| Mantequilla | `agarreDerretida`, `largoFranja`, `duracionFranja`, `descanso` | La franja de la lámpara de calor |
| Tomates / Arándanos | `velocidad`, `cadencia`, `carril`, `zigzag` | Lo rápido que bajan, cada cuánto, por dónde y cuánto botan de lado |
| Colchón | `subida`, `avance`, `ladoMaximo` | Lo alto y lo lejos que lanza, y cuánto se puede ir de lado |
| Colchón | `vaiven`, `periodoVaiven` | Cuánto va y viene de lado y a qué ritmo |
| Cinta de abajo | `velocidad` | Lo rápido que te devuelve |
| RondaDespensa | `tiempoTrasPrimero` | Lo que dura el temporizador del horno |
| RondaDespensa | `finalConPrisa` | Los segundos finales en que el público golpea las gradas |
| RondaDespensa | `duracionMinima` | Tiempo mínimo de la carrera |

Las medidas del recorrido están en `Editor/CreadorDespensa.cs`. Si las cambias, sube `Version` y vuelve a crear la escena.

## Los bots

- **Rodillos:** esperan a que el rodillo no venga de frente y lo saltan por el mango. Los puntos de salto van pegados al rodillo y se mueven con él.
- **Colchones:** sus puntos van pegados a los colchones, que se mueven.
- **Fregadero:** los hábiles van por las esponjas (esperan a que no estén empapadas) y los demás, por los tapones de la derecha.
- **Mantequilla:** suben corriendo sin saltar, esquivando los muros.
- **Bandejas:** saltan al primer colchón y se dejan lanzar.

## Online

La escena usa las mismas piezas que el resto del show: todo se mueve en el anfitrión y los invitados ven lo mismo. La pegatina "¡PLAF!" se dibuja con la interfaz del show, así que también sale en las pantallas de los invitados.

## Los modelos de Blender

Lo que se ve sale de `Arte/Blender/crear_kit_despensa.py`, que genera `Modelos/KitDespensa.fbx`:

- **Zona 1:** el rodillo (con harina) y el salero de cristal.
- **Zona 2:** la esponja, el tapón, el grifo (base y caño) y los grumos de espuma.
- **Zona 3:** el tomate cherry, el arándano, el muro de refugio y la lámpara de calor con su bombilla.
- **Zona 4:** el colchón, el arco de control, el portal de meta con su marcador, la línea de cuadros y el podio.
- **Plató:** la grúa de cámara (base y brazo), la tostadora y el tarro.

Las banderitas de las guirnaldas y las gradas con el público son las de los kits de costura y del plató.

Para regenerar el kit tras cambiar el script, usa rutas completas (si son relativas, Blender guarda las vistas en `C:\Arte\…`):

```
blender -b --factory-startup -P Arte/Blender/crear_kit_despensa.py -- F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Assets/BichoShow/Modelos/KitDespensa.fbx F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Arte/Blender/KitDespensa.blend F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Arte/Blender/vistas
```

Las vistas previas quedan en `Arte/Blender/vistas/kit_despensa_*.png`.
