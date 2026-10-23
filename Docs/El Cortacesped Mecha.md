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
| **1. Calentamiento** | primer 28 % | Onda láser, aspiración y lluvia de tuercas, de uno en uno. |
| **2. ¡Saca las cuchillas!** | hasta el 62 % | Se suman el **barrido de cuchillas** y el **corte de césped**, y llegan variantes: aros láser **altos** y la aspiración que de golpe **sopla**. Descansos más cortos. |
| **3. ¡Modo furia!** | el resto | Cara de furia y robot más rápido, casi sin descanso, y a menudo **dos ataques a la vez** (combos). Cada tanda aguantada vale **el doble**. |

## Los ataques

El robot anuncia cada ataque con la cara de su pantalla (y un aviso en grande), así que se puede ver venir:

| Cara | Ataque | Qué hacer |
|---|---|---|
| Ojos enfadados con una raya rosa | **Onda láser:** aros que salen del robot y crecen hasta el borde, de dos a cinco seguidos. Los **rosas** van a ras de suelo; desde la fase 2 también salen **azules**, a la altura de la cabeza. | Saltar los **rosas** y **no saltar** los azules. Si te pilla, te tumba. |
| Ojos de risa y boca abierta | **Aspiración invertida:** la turbina tira de todos hacia el robot, más fuerte cuanto más cerca. Desde la fase 2, a mitad puede dar la vuelta y **soplar** hacia fuera ("¡AHORA SOPLA!"). Se ve de dónde viene el aire: un remolino en espiral pintado en el suelo que gira hacia donde va el aire, un ciclón alrededor del robot que sube a la turbina, rachas de viento en espiral, recortes de césped volando y flechas en el césped. Todo aparece, más flojo, durante el aviso. | Correr hacia fuera; si sopla, hacia dentro. |
| Ojos enfadados con cejas en V | **Lluvia de tuercas:** escupe tuercas y pilas de goma que rebotan mientras el robot gira. Cada vez escupe más deprisa. | Esquivarlas: te tumban y te empujan hacia donde iban. |
| Ojos en raya, dientes y humo | **Barrido de cuchillas** (fase 2): una cuchilla a rayas a ras de suelo gira por todo el césped (vuelta y media; en la fase 3, dos cuchillas opuestas). | Saltarla justo cuando llega. |
| Ojos mirando abajo y boca de sierra | **Corte de césped** (fase 2): cuatro o cinco porciones del suelo crujen, se ponen rojas (latiendo cada vez más), tiemblan y se hunden un poco; luego se abren de golpe como una trampilla (la bisagra, en el borde del foso) y al rato se vuelven a cerrar. Nunca son todas seguidas. | Salir de las rojas antes de que se abran: si te pilla, resbalas al vacío. |

En el modo furia los combos posibles son: láser + tuercas, cuchillas + tuercas, corte + láser, corte + aspiración y cuchillas + corte.

Al acabar la ronda el robot se marea (cara de espirales), se para y el césped vuelve a su sitio.

## Puntos

- Cada tanda de ataques que termina da **+1** (**+2** en el modo furia) a cada bicho que no se ha caído durante esa tanda.
- Gana quien más aguante. Con placajes (X) se puede echar a los demás al foso.
- Los bots se quedan por el medio del césped. Saltan los aros bajos y las cuchillas (los hábiles, más justos) y no saltan con un aro alto cerca. Además, corren hacia fuera al aspirar y hacia dentro al soplar, se apartan de las tuercas y salen del césped que se pone rojo.

## Si hay que ajustarla

Todo está en el componente **CortacespedMecha** del robot:

- **Velocidad de los aros:** al principio y al final de la ronda (8 y 12 m/s).
- **Fuerza de la aspiración:** 3,6 y 4,8 m/s; el bicho corre a 6.
- **Velocidad de las cuchillas:** 70 y 115 °/s.
- **Cuándo cambian las fases:** en el componente de la ronda (**Cambios de fase**: 0,28 y 0,62).
- **Duración del anuncio:** 1,3 s.
- **Giro del robot:** 18 °/s; casi el doble mientras ataca y más en el modo furia.

Las medidas de la arena (radios del robot, del foso y del césped) están en `CreadorCortacesped.cs`.

## Los efectos (hechos en Blender)

Las texturas de los efectos (racha de viento, remolino, ciclón, brillo del láser y recorte de césped) y sus mallas las hace `Arte/Blender/crear_efectos_cortacesped.py`, con el ruido de Blender para los jirones. Deja las texturas en `Assets/BichoShow/Texturas/Efectos` y las mallas en `Arte/Efectos/mallas_cortacesped.json`. Se puede lanzar por el MCP de Blender (el complemento "MCP for Blender") o desde la consola. Después, vuelve a crear la escena.

Los materiales de los efectos son URP Unlit, transparentes y de las dos caras; los que brillan son aditivos (con el bloom de la noche, relucen).
