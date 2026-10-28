# Pase de temporada

Un pase **gratis** de 30 niveles que dura 20 días. Jugando shows se gana experiencia (XP). Cada nivel da un premio, que puede ser ropa nueva para el vestuario o un título para debajo del nombre. Lo ganado es tuyo para siempre, también cuando acaba la temporada.

## Cómo se juega

- En el menú, **Pase de temporada** (el botón lleva una pastilla con tu nivel). Ahí ves:
  - Tu nivel y la barra de experiencia.
  - Los premios. Con el stick se pasa de uno a otro; los que ya tienes dicen «¡TUYO!».
  - Los **retos de hoy**.
  - **Tu cuenta**.
- Al volver al menú después de un show, sale a la derecha lo que has ganado y, si has subido de nivel, el premio con confeti.
- En el **Vestuario**:
  - La ropa del pase sale en gris con el nivel que hace falta («Casco de obra · nivel 4»). Se puede **probar**, pero no se queda puesta hasta que la ganas.
  - Hay una fila nueva, **Título**, para elegir uno de los títulos ganados. Sale debajo del nombre en las fichas del menú.
- Los bots nunca llevan ropa del pase: es de quien la gana.

### Quién avanza

- **Solo el jugador 1 de cada ordenador**, el dueño de la cuenta.
- Si sois varios en el sofá con una sola copia del juego, los demás juegan con la ropa y los títulos que tenga desbloqueados esa cuenta.
- **Online**, cada ordenador tiene su cuenta, así que avanza el jugador 1 de cada uno. El anfitrión le manda a cada invitado lo que ha ganado.

## Experiencia

| Qué | XP |
|---|---|
| Jugar una prueba | 40 |
| Cada estrella | 15 |
| Ganar una prueba | 30 |
| Acabar un show | 150 |
| Ganar el show (el que más estrellas tiene, sin empate) | 100 |
| Cada reto del día completado | 200 |

- Cada nivel pide **600 XP**, así que el pase entero son 18.000 XP.
- Un show normal da unos 500 o 600 XP, más o menos un nivel.
- Con un show al día y los tres retos, se termina hacia el día 15.

**Retos del día:** son tres cada día, los mismos para todo el mundo, y cambian a medianoche. Salen de esta lista:
- Juega 2 shows enteros.
- Consigue 8 estrellas.
- Gana 2 pruebas.
- Entrega 3 objetos en Mudanza frágil.
- Llega a la meta en 2 carreras.
- Consigue 25 puntos saboteando en una prueba.
- Juega una gran final.
- Juega 5 pruebas.
- Gana un show.

## Premios de la temporada 1 («¡Gran estreno!»)

- **Ropa nueva** (modelada en `Arte/Blender/crear_vestuario.py`):

  | Nivel | Prenda | Tipo |
  |---|---|---|
  | 2 | Gafas 3D | Cara |
  | 4 | Casco de obra | Gorro |
  | 6 | Flotador de pato | Cuerpo |
  | 8 | Bigote rizado | Cara |
  | 10 | Cascos de DJ | Gorro |
  | 12 | Guitarra | Cuerpo |
  | 15 | Antifaz | Cara |
  | 18 | Gorro cohete | Gorro |
  | 21 | Banda de campeón | Cuerpo |
  | 24 | Gafas de estrella | Cara |
  | 27 | Tiara de estrella | Gorro |
  | 30 | Alas de mariposa | Cuerpo |

- **Títulos**, en los demás niveles: «Novato del plató», «Mudancero», «Rey del sofá», «Fenómeno de la tele»…

Se ven puestas en `Arte/Blender/vistas/probador_pase_*.png` y en `vestuario_pase.png`.

## La cuenta

- Al abrir el juego entras solo:
  - Con tu sesión guardada, si la tienes.
  - Si no, como **invitado**: ya avanzas, pero si reinstalas el juego pierdes el progreso.
- En la pantalla del pase, con **A → Iniciar sesión**, se abre el navegador con la **cuenta de Unity**, donde se puede entrar con **Google**, Apple o email.
  - Si eras invitado, tu progreso pasa a la cuenta.
  - Si la cuenta ya tenía progreso (de otro ordenador o de antes de reinstalar), vuelves a él.
- Con la sesión iniciada, **X** la cierra.
- El progreso se guarda en este ordenador, para jugar sin conexión, y en la nube, con **Cloud Save**. Si las dos copias no coinciden, gana la que tiene más XP.
- Si abres **dos copias** del juego en el mismo ordenador (para probar el online), solo la primera recuerda la sesión. La otra entra cada vez como un invitado nuevo.

### Activar el inicio de sesión (una vez)

Cloud Save ya funciona. Para que funcione el botón de iniciar sesión hay que darle a Unity el permiso de las cuentas:

1. En Unity: **Edit → Project Settings → Services → Authentication**.
2. En **Identity Providers**, añade **Unity Player Accounts**. Unity crea la configuración y rellena solo el *Client ID* en `Assets/Resources/UnityPlayerAccountSettings.asset`. Ahora mismo está vacío.
3. En [cloud.unity.com](https://cloud.unity.com), en tu proyecto, mira los ajustes de **Unity Player Accounts**. Comprueba que **Google** está entre las formas de entrar.
4. Prueba: **Play → Pase de temporada → A**. Se abre el navegador, entras con Google, vuelves al juego y pone «Sesión iniciada».

Si falta el paso 2, el juego avisa: «Faltan por activar las cuentas de Unity en el proyecto».

## Cambiar de temporada

Todo está en `Scripts/Pase/Temporada.cs`:
- `Id`, `Nombre`, `Inicio`, `Dias`.
- Los niveles y la XP por nivel.
- Los premios y la lista de retos.

Antes de publicar:
- **Pon la fecha de `Inicio` de verdad.** Ahora está en el 26/10/2026 para poder probarlo ya.
- La siguiente temporada necesita un `Id` nuevo (`t2`). Con eso empieza de cero.
- Para que no se pierda lo ganado en la temporada 1, `ProgresoPase.Desbloqueado` tendrá que mirar también los niveles de las temporadas anteriores. Ahora solo existe una.

## A tener en cuenta

- La XP la da el propio juego, que también escribe en la nube. Para un pase gratis vale. Si algún día tiene premios de pago, habría que darla desde el servidor (Cloud Code) para que nadie se la invente.
- Los títulos aún no se ven online: los demás jugadores ven tu nombre, pero no tu título.
