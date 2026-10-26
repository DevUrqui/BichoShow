# Mudanza frágil

Dos equipos, rojos (jugadores 1 y 3) contra azules (2 y 4), llevan objetos frágiles de su camión a su casa. Los objetos son una tele, un jarrón y un sofá. Cada entrega suma puntos según lo entero que llegue. Si un objeto se rompe, sale otro del camión, y el equipo se lleva una parte de los puntos según lo lejos que lo llevó.

## Crearla

En Unity: **BichoShow → Crear escena: Mudanza frágil**.

- Si ya la tenías creada, al darle a Play sale un aviso para rehacerla con la versión nueva (versión 3).
- Dura al menos **2 minutos y medio**, aunque el show pida rondas más cortas: el recorrido es largo.

## El recorrido (del camión a la casa, unos 90 m)

| # | Zona | Qué pasa |
|---|---|---|
| 1 | **Salida** | Camiones, conos y cajas. La cabina morada del centro es la del saboteador. |
| 2 | **Barredora** | Un brazo de 14 m que gira más rápido que antes. En el lado azul barre hacia atrás: hay que esperar a que pase o cruzar por fuera de su alcance, pegado al borde. |
| 3 | **El río** | Un puente central estrecho, o plataformas a los lados que se inclinan según dónde se pise. |
| 4 | **Calle de los empujadores** | Ahora son **cuatro** empujadores, que salen de lados alternos, y hay un badén a cada extremo. |
| 5 | **Almacén** *(nuevo)* | Dos cintas transportadoras que llevan hacia atrás a todo lo que las pisa, objetos incluidos, y pilas de cajas por medio. |
| 6 | **Patio de los ventiladores** *(nuevo)* | Dos ventiladores gigantes soplan de lado cada 5 s. Antes de soplar, las aspas aceleran y suena el aviso. Empujan a los bichos y a los objetos sueltos. Enfrente de cada ventilador, la valla tiene un hueco marcado en amarillo: por ahí se sale volando. |
| 7 | **Casas con puerta giratoria** | Las casas son más anchas. La puerta tiene **tres hojas grandes**, en lugar de cuatro pequeñas, y gira más despacio, así que cabe un sofá en cada hueco. Las hojas están **acolchadas**, con un rulo blando de color en la punta: un golpe de la puerta le hace al objeto solo un 35 % del daño normal. Conviene entrar por el lado en el que las hojas van hacia dentro. |

- La alfombra de dentro de la casa es la zona de entrega.
- Las barredoras, los empujadores, las plataformas y las puertas son trampas: con saboteador se mueven solo cuando él las activa. Los ventiladores y las cintas van siempre solos.

## Aspecto

Las piezas nuevas están en el kit del barrio: `Arte/Blender/crear_kit_barrio.py` genera `KitBarrio.fbx` sin abrir Blender. Las colisiones son las de siempre, invisibles; encima va el dibujo de Blender.

- **Suelos de cada zona**:
  - **Salida**: pavimento, con las plazas de los camiones pintadas y una zona rayada donde sale cada objeto.
  - **Calle**: asfalto, aceras, bordillos, pasos de cebra y alcantarillas.
  - **Almacén**: hormigón, con una franja de cinta de peligro a la entrada y a la salida.
  - **Patio**: baldosas a cuadros.
  - **Casas**: césped con caminos hasta cada puerta, y parquet por dentro.
  - Por los lados de las losas se ve tierra, como en las islas.
- **Barredora**: brazo a rayas rojas y blancas con topes de goma, y un eje con baliza naranja.
- **Empujadores**: prensas hidráulicas amarillas, con rayas de peligro en la cara que empuja y una baliza encima. Salen de un garaje con luz roja.
- **Río**: el puente central es de tablas y las plataformas son balsas de tablones con los bordes de aviso. Hay juncos en las orillas, rocas y nenúfares.
- **Almacén**: rodillos en los extremos de las cintas, y las pilas de cajas van sobre palés.
- **Patio**: valla de piquetes blancos.
- **Casas**:
  - Por fuera: remate del color del equipo encima de las paredes, toldo a rayas sobre la puerta, ventanas con contraventanas y geranios, y una maceta junto a la puerta.
  - La puerta giratoria: hojas de cristal con marco y barras para empujar.

## Bots

- Siguen una ruta por su lado del mapa:
  1. Cruzan la barredora por fuera de su brazo.
  2. Cruzan el río por el centro de su plataforma.
  3. Recorren la calle y el almacén por su carril, entre las pilas de cajas.
  4. Entran por el lado de la puerta en el que las hojas van hacia dentro.
- El que tira del objeto mira el siguiente punto de la ruta desde él mismo cuando va por delante. Antes se quedaban parados en un punto con el objeto justo detrás.
- En una partida de prueba solo con bots (150 s), los rojos entregaron 4 objetos y los azules 1. La mayoría llegan con la mitad de vida, y se rompen bastantes por el camino.
