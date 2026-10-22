# Personajes

Cada jugador elige su bicho en **Vestuario**, en la primera fila ("Bicho"). La ropa se queda puesta al cambiar de bicho. Lo que elige cada uno se guarda, los bots salen con un bicho al azar y online cada uno ve el bicho de los demás.

| Bicho | Qué es | Lo suyo |
|---|---|---|
| **Bichete** | Mariquita | Huevo redondo con caparazón de lunares y raya |
| **Zumbi** | Abeja | Cabeza aparte con ojos negros de insecto, collar de pelusa en el cuello, tronco con raya, abdomen grande a rayas con aguijón, brazos y patas negros y dos pares de alas que aletean |
| **Concha** | Caracol | Alto y estrecho, con los ojos en lo alto de las antenas y una concha enorme en espiral a la espalda |
| **Cornelio** | Escarabajo | Bajito, ancho y forzudo: brazos y patas gordos, élitros grandes, cejo de gruñón y un cuerno de rinoceronte |
| **Gusi** | Oruga | Segmentos rosas apilados con lunares amarillos y patitas de bolita, cabezona con cresta |
| **Ocho** | Araña | Bajita y morada, con un culete enorme con manchas naranjas, seis ojos, colmillos y cuatro patas de más |
| **Migas** | Hormiga | Cabezona con ojos negros de insecto, cintura finísima, culete gordo, mandíbulas y antenas acodadas |
| **Brinco** | Saltamontes | Verde lima, cara alargada con dientes de conejo, alas plegadas y patas saltadoras con la rodilla por encima del cuerpo |
| **Chispa** | Luciérnaga | Flaca y de ojos grandes, con capucha oscura, alas, lucecitas en las antenas y un farolillo enorme en el culete que alumbra de verdad |

Cada uno tiene su propio cuerpo y su cara (unos son un "huevo" con la cara en el cuerpo, otros tienen la cabeza aparte o van por segmentos), pero los mismos huesos y animaciones, así que juegan exactamente igual: el bicho solo cambia el aspecto. **El color del jugador:** Bichete se tiñe entero, como siempre. Los demás llevan siempre sus colores (abeja amarilla, caracol aguamarina, escarabajo azul con caparazón verde metalizado, luciérnaga lila, oruga rosa, araña morada, hormiga naranja, saltamontes verde lima) y el color del jugador va en las manoplas y las zapatillas. La ropa se ajusta a la talla de cada uno.

## Prepararlos en Unity (una vez)

Usa **BichoShow → Personaje: aplicar Bichete (el concursante)**. Hace dos cosas:

1. Prepara todos los bichos y guarda cada uno en `Assets/BichoShow/Resources/Personajes/` (con su controlador de animaciones en `Animaciones/` y sus materiales).
2. Pone a Bichete en el prefab del bicho y le añade lo necesario para cambiar de bicho en la partida.

Si solo cambias los modelos, basta con **BichoShow → Personaje: preparar los personajes**.

Si se te olvida, al darle a Play sale un aviso que ofrece prepararlos.

## Cambiar o añadir un bicho

Los modelos los hace `Arte/Blender/crear_personaje.py`. El cuarto argumento es el bicho: `bichete`, `abeja`, `caracol`, `escarabajo`, `luciernaga`, `oruga`, `arana`, `hormiga` o `saltamontes`. Usa siempre rutas completas, por ejemplo para la abeja:

```
blender -b --factory-startup -P Arte/Blender/crear_personaje.py -- F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Assets/BichoShow/Modelos/Zumbi.fbx F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Arte/Blender/Zumbi.blend F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Arte/Blender/vistas abeja
```

Las vistas de prueba salen en `Arte/Blender/vistas/personaje_<bicho>_*.png`.

Para añadir uno nuevo:

1. Añade su forma en `crear_personaje.py` (su entrada en `DISENOS` y lo suyo), sin cambiar los nombres de los huesos ni el orden de los materiales.
2. Genera su `.fbx` en `Assets/BichoShow/Modelos/` con el nombre del bicho.
3. Añádelo a `Especies` y `Nombres` en `Scripts/Personaje/PersonajeBicho.cs`, y su paleta (`Paleta`) y su talla de ropa en `Scripts/Editor/CreadorPersonajeAnimado.cs`.
4. En Unity, **BichoShow → Personaje: preparar los personajes**.

Online solo cabe un número de bicho por jugador (hasta 255), así que no hace falta tocar nada más.
