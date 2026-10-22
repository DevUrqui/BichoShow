# Personajes

Cada jugador elige su bicho en **Vestuario**, en la primera fila ("Bicho"). La ropa se queda puesta al cambiar de bicho. Lo que elige cada uno se guarda, los bots salen con un bicho al azar y online cada uno ve el bicho de los demás.

| Bicho | Qué es | Lo suyo |
|---|---|---|
| **Bichete** | Mariquita | Caparazón con lunares y raya |
| **Zumbi** | Abeja | Rayas negras, aguijón y dos pares de alitas que aletean (más rápido en el aire) |
| **Concha** | Caracol | Concha en espiral a la espalda y antenas con bolas gordas |
| **Cornelio** | Escarabajo | Caparazón partido en dos y cuerno de rinoceronte |
| **Chispa** | Luciérnaga | Capucha oscura, alitas y un farolillo en el culete que brilla y da luz de verdad |

Todos tienen el mismo esqueleto, las mismas animaciones y los mismos anclajes para la ropa, así que juegan exactamente igual: el bicho solo cambia el aspecto. La piel y el caparazón (o la concha) se tiñen del color del jugador, como siempre.

## Prepararlos en Unity (una vez)

Usa **BichoShow → Personaje: aplicar Bichete (el concursante)**. Hace dos cosas:

1. Prepara los cinco bichos y guarda cada uno en `Assets/BichoShow/Resources/Personajes/` (con su controlador de animaciones en `Animaciones/` y sus materiales).
2. Pone a Bichete en el prefab del bicho y le añade lo necesario para cambiar de bicho en la partida.

Si solo cambias los modelos, basta con **BichoShow → Personaje: preparar los personajes**.

Si se te olvida, al darle a Play sale un aviso que ofrece prepararlos.

## Cambiar o añadir un bicho

Los modelos los hace `Arte/Blender/crear_personaje.py`. El cuarto argumento es el bicho: `bichete`, `abeja`, `caracol`, `escarabajo` o `luciernaga`. Usa siempre rutas completas, por ejemplo para la abeja:

```
blender -b --factory-startup -P Arte/Blender/crear_personaje.py -- F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Assets/BichoShow/Modelos/Zumbi.fbx F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Arte/Blender/Zumbi.blend F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Arte/Blender/vistas abeja
```

Las vistas de prueba salen en `Arte/Blender/vistas/personaje_<bicho>_*.png`.

Para añadir uno nuevo:

1. Añade su forma en `crear_personaje.py`, sin cambiar los huesos ni el orden de los materiales.
2. Genera su `.fbx` en `Assets/BichoShow/Modelos/` con el nombre del bicho.
3. Añádelo a `Especies` y `Nombres` en `Scripts/Personaje/PersonajeBicho.cs`, y su paleta en `Paleta` de `Scripts/Editor/CreadorPersonajeAnimado.cs`.
4. En Unity, **BichoShow → Personaje: preparar los personajes**.

Online solo cabe un número de bicho por jugador (hasta 255), así que no hace falta tocar nada más.
