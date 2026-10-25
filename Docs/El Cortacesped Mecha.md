# El Cortacésped Mecha

Prueba de supervivencia en un jardín de noche convertido en plató. En el centro hay un robot cortacésped gigante, con una pantalla LED por cara, que va lanzando ataques. Cada ataque que aguantas sin caerte suma un punto.

Esta es la **versión de prueba**, hecha con bloques y mallas sencillas. Cuando las medidas estén bien, el robot y la chatarra pasarán a modelos de Blender.

## Crearla

En Unity: **BichoShow → Crear escena: El Cortacésped Mecha**.

- Se crea `Escenas/Cortacesped.unity`, se añade al build y entra sola en el sorteo del show.
- Para jugarla suelta, abre la escena, dale a Play, únete y pulsa START.
- Si ya la tenías creada, al darle a Play sale un aviso para rehacerla con la versión nueva.

## La arena (vista desde arriba)

| Zona | Radio | Qué es |
|---|---|---|
| Isla del robot | 0–4,2 m | El robot, que gira sobre sí mismo. Su parachoques acolchado no deja subirse. |
| Foso de chatarra | 4,2–7 m | Un foso de 5 m con pilas y tuercas viejas y una luz roja. Si caes, vuelves a tu sitio y pierdes el punto de ese ataque. |
| Césped | 7–22 m | Donde se juega: doce porciones de colores, sin barandillas. Un borde de neón rosa marca el foso y uno cian, el exterior. |
| Fuera | más de 22 m | El jardín, muy abajo. Caerse también cuesta el punto. |

Alrededor hay gradas con público, focos, hierba gigante, dientes de león y el letrero de neón "BichoShow" detrás del robot.

## Las fases

La pelea va por fases según lo que va de ronda. Cada cambio de fase se anuncia en grande y la cámara tiembla:

| Fase | Cuándo | Qué cambia |
|---|---|---|
| **1. Calentamiento** | primer 22 % | Onda láser, aspiración y lluvia de chatarra (tuercas, pilas y muelles), de uno en uno. |
| **2. ¡Saca las cuchillas!** | hasta el 55 % | Se suman el **barrido de cuchillas** y el **corte de césped**. Llegan variantes: aros láser **altos**, la aspiración que de golpe **sopla** y la cuchilla que **cambia de sentido**. A veces, dos ataques a la vez. Descansos más cortos. |
| **3. ¡Modo furia!** | hasta el 85 % | Cara de furia y robot más rápido, casi sin descanso, y casi siempre **dos ataques a la vez** (combos). La aspiración sopla y a veces **vuelve a aspirar**, el corte de césped puede tener una **segunda oleada** y las tuercas salen de dos en dos. Cada tanda aguantada vale **el doble**. |
| **¡Último asalto!** | el final | Como la furia, pero a menudo con **tres ataques a la vez**. Cada tanda aguantada vale **el triple**. |

## Los ataques

El robot anuncia cada ataque con la cara de su pantalla (y un aviso en grande), así que se puede ver venir:

| Cara | Ataque | Qué hacer |
|---|---|---|
| Ojos enfadados con una raya rosa | **Onda láser:** aros que salen del robot y crecen hasta el borde, de tres a siete seguidos. Los **rosas** van a ras de suelo; desde la fase 2 también salen **azules**, a la altura de la cabeza. | Saltar los **rosas** y **no saltar** los azules. Si te pilla, te tumba. |
| Ojos de risa y boca abierta | **Aspiración invertida:** la turbina tira de todos hacia el robot, más fuerte cuanto más cerca. Desde la fase 2, a mitad puede dar la vuelta y **soplar** hacia fuera ("¡AHORA SOPLA!"); en la furia, a veces vuelve a aspirar ("¡Y VUELVE A ASPIRAR!"). Se ve de dónde viene el aire: un remolino en espiral pintado en el suelo que gira hacia donde va el aire, un ciclón alrededor del robot que sube a la turbina, rachas de viento en espiral, recortes de césped volando y flechas en el césped. Todo aparece, más flojo, durante el aviso. | Correr hacia fuera; si sopla, hacia dentro. |
| Ojos enfadados con cejas en V | **Lluvia de chatarra:** escupe tuercas, pilas y **muelles** de goma que rebotan mientras el robot gira, cada vez más deprisa. Desde la fase 2 también suelta **neumáticos** que salen rodando del borde del foso hacia fuera, **latas de aceite** que al caer dejan un **charco resbaladizo** (7 s) y, de vez en cuando, un **abanico** de cinco tuercas. En la furia salen de dos en dos y caen **pernos gigantes**: primero sale una **diana roja** en el césped (muchas veces, donde estás tú) y al poco el perno se clava y tumba a los que estén dentro. | Esquivar la chatarra, **saltar** los neumáticos, **salir de las dianas** y no pisar los charcos. |
| Ojos en raya, dientes y humo | **Barrido de cuchillas** (fase 2): una cuchilla a rayas a ras de suelo gira por todo el césped (una vuelta y tres cuartos; en la fase 3, dos cuchillas opuestas y vuelta y media). A veces frena en seco y **gira al revés** ("¡LA CUCHILLA CAMBIA DE SENTIDO!"). | Saltarla justo cuando llega, y estar atento si da la vuelta. |
| Ojos mirando abajo y boca de sierra | **Corte de césped** (fase 2): cinco o seis porciones del suelo crujen, se ponen rojas (latiendo cada vez más), tiemblan y se hunden un poco; luego se abren de golpe como una trampilla (la bisagra, en el borde del foso) y al rato se vuelven a cerrar. Nunca son todas seguidas. En la furia, a veces, cuando se cierran, se ponen rojas las de al lado ("¡OTRO CORTE!"). | Salir de las rojas antes de que se abran (y cambiar otra vez en el segundo corte): si te pilla, resbalas al vacío. |

Los combos posibles son: láser + chatarra, cuchillas + chatarra, corte + láser, corte + aspiración, cuchillas + corte y corte + chatarra. Los tríos del último asalto son láser + chatarra + corte y cuchillas + chatarra + corte.

Los golpes empujan más que antes (más aún en la furia): cerca del borde o del foso, un golpe te puede tirar.

Al acabar la ronda el robot se marea (cara de espirales), se para y el césped vuelve a su sitio.

## Puntos

- Cada tanda de ataques que termina da **+1** (**+2** en el modo furia y **+3** en el último asalto) a cada bicho que no se ha caído durante esa tanda.
- Gana quien más aguante. Con placajes (X) se puede echar a los demás al foso.
- Los bots se quedan por el medio del césped. Saltan los aros bajos y las cuchillas (los hábiles, más justos) y no saltan con un aro alto cerca. Además, corren hacia fuera al aspirar y hacia dentro al soplar, se apartan de las tuercas y salen del césped que se pone rojo. También saltan los neumáticos y salen de las dianas y de los charcos.

## El villano

En el show, el humano que va último no juega esta ronda: **maneja el robot**. Jugando la prueba suelta, es villano quien esté en la cabina del saboteador al pulsar START.

- **Elige el siguiente ataque** entre tres cartas que salen abajo en la pantalla. **X** cambia de carta y **A** lo lanza ya, sin esperar al final del descanso. Si no pulsa nada, sale la carta elegida cuando acaba el descanso. En la furia, las cartas traen combos; en el último asalto, también tríos.
- **Tres poderes por ataque**, con **A** (con un respiro de 1,2 s entre uno y otro):

| Ataque | Poder con A | Con el stick |
|---|---|---|
| Onda láser | el siguiente aro sale **alto** (azul) | |
| Aspiración | **sopla** o **vuelve a aspirar** | |
| Lluvia de chatarra | **ráfaga** de tres tuercas | **apunta** hacia dónde escupe, hacia dónde salen los neumáticos y dónde caen los pernos |
| Barrido de cuchillas | la cuchilla **gira al revés** | |
| Corte de césped | **otro corte** en las porciones de al lado | **elige dónde corta** (mientras lo anuncia) |

- Con el villano, el robot no hace nada de esto solo: los aros altos, los soplidos, los cambios de sentido y el segundo corte los decide él.
- **Puntos del villano:** 1 por cada bicho que tumba (aros, cuchillas, chatarra, neumáticos o pernos) y 3 por cada uno que se cae al foso o fuera. En el show: 25 puntos dan 2 estrellas y 60, 3.
- Los demás lo ven todo: la cara del robot avisa del ataque y las cartas del villano salen en pantalla.
- Si al villano se le corta la conexión, el robot vuelve a ir solo hasta que vuelva.

En el show, solo se anuncia saboteador o villano si la siguiente prueba lo admite. Ahora lo admiten Mudanza, Costura y "Juntos o nada" (con trampas) y el Cortacésped (con villano). En las demás pruebas, el último juega normal.

## Si hay que ajustarla

Todo está en el componente **CortacespedMecha** del robot:

- **Velocidad de los aros:** al principio y al final de la ronda (9 y 14 m/s).
- **Fuerza de la aspiración:** 4 y 5,2 m/s (cerca del foso tira un 25 % más); el bicho corre a 6.
- **Velocidad de las cuchillas:** 85 y 140 °/s.
- **Cuándo cambian las fases:** en el componente de la ronda (**Cambios de fase**: 0,22 y 0,55); el último asalto, en el robot (**Ultimo asalto**: 0,85).
- **Duración del anuncio:** 1,15 s (un 15 % menos en la furia).
- **Giro del robot:** 18 °/s; casi el doble mientras ataca y más en el modo furia.

Las medidas de la arena (radios del robot, del foso y del césped) están en `CreadorCortacesped.cs`.

## Los efectos (hechos en Blender)

**El viento** de la aspiración se ve desde la cámara de juego:

- **Anillos** en el césped con flechas en V. Al aspirar son cian y se cierran hacia el robot; al soplar son amarillos y se abren.
- **Líneas de velocidad** que van en espiral hacia la turbina (o salen de ella), y **polvo** arrastrado por el césped. Son partículas que se mueven solas, así que también se ven online.
- Las rachas, el remolino del suelo, el ciclón, las flechas y los recortes de césped, más grandes y brillantes que antes.

**La chatarra nueva** (muelle, neumático con su llanta, lata de aceite con etiqueta y perno gigante con rosca) y los anillos y su textura (`Succion.png`) los hace `Arte/Blender/crear_chatarra_cortacesped.py`. Añade las mallas, con sus normales, al mismo `mallas_cortacesped.json`. Se puede lanzar por el MCP de Blender o por consola: `blender -b --python Arte/Blender/crear_chatarra_cortacesped.py`.

Las texturas de los efectos (racha de viento, remolino, ciclón, brillo del láser y recorte de césped) y sus mallas las hace `Arte/Blender/crear_efectos_cortacesped.py`, con el ruido de Blender para los jirones. Deja las texturas en `Assets/BichoShow/Texturas/Efectos` y las mallas en `Arte/Efectos/mallas_cortacesped.json`. Se puede lanzar por el MCP de Blender (el complemento "MCP for Blender") o desde la consola. Después, vuelve a crear la escena.

Los materiales de los efectos son URP Unlit, transparentes y de las dos caras; los que brillan son aditivos (con el bloom de la noche, relucen).
