# Bicho Show · Probar con amigos por Parsec

Tú ejecutas el juego en tu PC y tus amigos se conectan por Parsec: ven tu pantalla y **sus
mandos aparecen en tu PC como si estuvieran enchufados ahí**. El juego no nota la diferencia.

---

## 1. Compila el juego (una vez, y cada vez que cambiemos algo)

1. En Unity: menú **BichoShow → Compilar el juego para Windows**.
2. Se abre la carpeta `Builds/Windows` con `BichoShow.exe`.
3. Ábrelo y prueba una ronda tú solo para comprobar que todo va (sonido, bots, el show entero).

> Si copias el juego a otro sitio, copia la carpeta entera (el `.exe` necesita la carpeta `BichoShow_Data`).

## 2. Prepara Parsec

**Tú (quien hospeda):**
1. Descarga Parsec de su web oficial (parsec.app) e instálalo. Crea tu cuenta.
2. En los ajustes de **Host**, comprueba que el hospedaje está activado.
3. Si Parsec te ofrece instalar el **controlador de mando virtual** (*virtual gamepad*), acéptalo:
   es lo que hace que los mandos de tus amigos aparezcan en tu PC.

**Cada amigo:**
1. Instala Parsec y se crea su propia cuenta.
2. Conecta **un mando** a su PC (Xbox, PlayStation o similar). Con teclado no sirve: todos los
   invitados manejarían el mismo teclado, el tuyo.

## 3. Conecta a tus amigos

1. En Parsec, usa la opción de **compartir / invitar** (*Share*) y manda el enlace a tus amigos
   (por WhatsApp, Discord…). Es vuestra "sala con código".
2. Cuando entren, acepta su conexión y deja activado **solo el mando** (ni teclado ni ratón: si no,
   te pueden mover el ratón sin querer).
3. Hablad por **Discord** (o lo que uséis). El sonido del juego ya les llega por Parsec.

## 4. A jugar

1. Abre `BichoShow.exe` (pantalla completa).
2. En el menú, **cada uno pulsa un botón de su mando** para unirse: J1, J2, J3, J4.
   - **1 y 3 = equipo Rojo · 2 y 4 = equipo Azul.**
   - Los huecos libres los rellenan bots (se puede quitar en Opciones).
3. Empezad el show: Mudanza frágil → Voley atados → La gran final.

### Si algo falla
- **Va a tirones o con retraso:** usa cable de red en vez de wifi, cierra programas pesados y
  baja la resolución o el ancho de banda en los ajustes de Host de Parsec.
- **El mando de un amigo no hace nada:** revisa en Parsec que tiene permiso de mando. En Windows,
  en *Configurar dispositivos USB de juego* (`joy.cpl`), debería aparecer un mando de Xbox por cada invitado.
- **Se queda quieto al cambiar de ventana:** no debería (el juego sigue leyendo los mandos sin el
  foco); si pasa, avísame.

---

## 5. Graba los momentazos

- **Win + Alt + R**: empieza o para de grabar (Xbox Game Bar de Windows).
- **Win + Alt + G**: guarda los últimos 30 s (activa antes en la Game Bar *grabar lo que ha pasado*).
- O usa OBS si ya lo tienes.

Cada fallo gracioso es un clip para TikTok / Shorts (*"Día X haciendo un juego donde…"*).

## 6. Qué observar mientras juegan

Apunta con la hora aproximada (las estadísticas también la apuntan):

| Mira esto | Ejemplo |
|---|---|
| **Risas**: ¿cuándo se ríen a carcajadas? | "al romperse la tele en la rampa" |
| **Ratos muertos**: ¿cuándo se callan o se aburren? | "esperando en el ascensor de la final" |
| **Confusión**: ¿cuándo no saben qué hacer? | "no entendían cómo empujar el bloque" |
| **Frustración**: ¿la torpeza hace gracia o enfada? | "se caía del puente y se quejaba" |
| **Justicia**: ¿gana siempre el mismo? ¿el saboteador da rabia buena o mala? | |

## 7. Al acabar, pregúntales (2 minutos)

1. Del 1 al 5, ¿cuánto os habéis reído?
2. ¿Qué prueba os ha gustado más? ¿Y menos? ¿Por qué?
3. ¿Ha habido algún rato aburrido?
4. ¿Se entendía qué había que hacer en cada prueba?
5. ¿Los controles torpes hacen gracia o frustran?
6. ¿Volveríais a jugar otra partida ahora mismo?

## 8. Mándame

- El archivo de estadísticas: carpeta **`Estadisticas`** junto a `BichoShow.exe`
  (`partida_FECHA_HORA.txt`). Tiene, por ronda: saltos, placajes, derribos y caídas de cada
  jugador, objetos rotos, trampas usadas, puntos, cuánto duró y **el rato más largo sin nada destacable**.
- Tus notas y sus respuestas.
- Si quieres, algún clip.

Con eso ajustamos tiempos, fuerzas y puntuaciones, y decidimos si toca el online propio.
