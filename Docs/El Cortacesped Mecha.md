# El Cortacésped Mecha

Prueba de supervivencia en un jardín de noche convertido en plató. En el centro hay un robot cortacésped gigante, con una pantalla LED por cara, que va lanzando ataques. Cada ataque que aguantas sin caerte suma un punto.

Esta es la **versión de prueba**, hecha con bloques y mallas sencillas. Cuando las medidas estén bien, el robot y la chatarra pasarán a modelos de Blender.

## Crearla

En Unity: **BichoShow → Crear escena: El Cortacésped Mecha**.

- Se crea `Escenas/Cortacesped.unity`, se añade al build y entra sola en el sorteo del show.
- Para jugarla suelta, abre la escena, dale a Play, únete y pulsa START.

## La arena (vista desde arriba)

| Zona | Radio | Qué es |
|---|---|---|
| Isla del robot | 0–4,2 m | El robot, que gira sobre sí mismo. Su parachoques acolchado no deja subirse. |
| Foso de chatarra | 4,2–7 m | Un foso de 5 m con pilas y tuercas viejas y una luz roja. Si caes, vuelves a tu sitio y pierdes el punto de ese ataque. |
| Césped | 7–22 m | Donde se juega: doce porciones de colores, sin barandillas. Un borde de neón rosa marca el foso y uno cian, el exterior. |
| Fuera | más de 22 m | El jardín, muy abajo. Caerse también cuesta el punto. |

Alrededor hay gradas con público, focos, hierba gigante, dientes de león y el letrero de neón "BichoShow" detrás del robot.

## Los ataques

El robot descansa un momento y luego **anuncia** el siguiente ataque con la cara de su pantalla (y un aviso en grande), así que se puede ver venir:

| Cara | Ataque | Qué hacer |
|---|---|---|
| Ojos enfadados con una raya rosa | **Onda láser:** aros rosas a ras de suelo que salen del robot y crecen hasta el borde. Con la ronda avanzada salen dos o tres seguidos, y más rápido. | **Saltar** justo cuando llega el aro. Si te pilla en el suelo, te tumba y te empuja un poco hacia fuera. |
| Ojos de risa y boca abierta | **Aspiración invertida:** la turbina de arriba gira a tope y tira de todos hacia el robot, más fuerte cuanto más cerca. Se ve el polvo correr hacia él. | **Correr hacia fuera.** Se puede, pero cuesta: si te paras, acabas en el foso. |
| Ojos enfadados con cejas en V | **Lluvia de tuercas:** escupe tuercas y pilas de goma, unas gordas y otras pequeñas, que rebotan por el césped mientras el robot gira. | **Esquivarlas.** Si te dan, te tumban y te empujan hacia donde iban, quizá fuera del jardín. |

Entre ataque y ataque hay unos segundos de calma, cada vez menos. Al acabar la ronda, el robot se marea (cara de espirales) y se para.

## Puntos

- Cada ataque que termina, +1 a cada bicho que no se ha caído durante ese ataque.
- Gana quien más ataques aguante. Con placajes (X) se puede echar a los demás al foso.
- Los bots se quedan en el medio del césped, saltan los aros (los hábiles, más justos), corren hacia fuera con la aspiración y se apartan de las tuercas.

## Si hay que ajustarla

Todo está en el componente **CortacespedMecha** del robot:

- **Velocidad de los aros:** al principio y al final de la ronda (7 y 10 m/s).
- **Fuerza de la aspiración:** 3 y 4,3 m/s; el bicho corre a 6.
- **Duración del anuncio:** 1,4 s.
- **Giro del robot:** 16 °/s; el doble mientras ataca.

Las medidas de la arena (radios del robot, del foso y del césped) están en `CreadorCortacesped.cs`.
