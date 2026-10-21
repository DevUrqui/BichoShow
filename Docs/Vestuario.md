# Vestuario

Cada jugador viste a su Bichete en **Vestuario**, desde el menú. Hay tres huecos y se puede llevar una cosa de cada a la vez. Lo que elige cada uno se guarda, y los bots se visten al azar. Online, cada uno ve la ropa de los demás.

## Lo que hay

| Hueco | Prendas |
|---|---|
| **Gorro** (16) | Corona, gorra, chistera, gorro de fiesta, casco vikingo, gorro con hélice, flor, **gorro de chef**, **sombrero vaquero**, **sombrero de bruja**, **gorro de lana**, **orejas de conejo**, **seta**, **huevo frito**, **aureola**, **sombrero pirata** |
| **Cara** (10) | Gafas de sol, gafas redondas, bigote, nariz de payaso, **gafas de corazón**, **monóculo**, **parche pirata**, **gafas de bucear** (con tubo), **barba de mago**, **chupete** |
| **Cuerpo** (8) | Pajarita, **medalla de oro**, **collar de perlas**, **babero**, **corbata**, **bufanda**, **capa**, **mochila** |

En negrita, las nuevas. La pajarita estaba antes en el hueco de la cara y ahora está en el de cuerpo, así que se puede llevar con gafas. Quien la tuviera puesta tendrá que volver a ponérsela.

La capa y la mochila van a la espalda. Con la cámara de las carreras, que va detrás del bicho, son lo que más se ve.

## Cómo se elige

En **Vestuario**, cada jugador con su mando:

- Arriba y abajo cambian de fila (gorro, cara, cuerpo).
- Izquierda y derecha cambian la prenda.
- **A**: listo.
- **START**: volver.

## Añadir más prendas

1. Modélala en `Arte/Blender/crear_vestuario.py`, con el origen en su anclaje:
   - **Gorro:** en lo alto de la cabeza.
   - **Cara:** delante de los ojos.
   - **Cuerpo:** sobre la barriga. Desde ahí, la espalda con el caparazón queda en y ≈ +1.0.
2. Añade su nombre a la lista que toque en `Scripts/Personaje/Vestuario.cs` (`Gorros`, `Complementos` o `Cuerpos`) y el nombre que se ve en el menú (`NombresGorros`…).
3. Vuelve a generar el kit con rutas completas:

```
blender -b --factory-startup -P Arte/Blender/crear_vestuario.py -- F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Assets/BichoShow/Modelos/KitVestuario.fbx F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Arte/Blender/KitVestuario.blend F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Arte/Blender/vistas
```

Al volver a Unity, las prendas se rehacen solas al importar el kit. Si no, usa **BichoShow → Personaje: preparar vestuario**.

El script también renderiza un **probador**: un maniquí de Bichete con varios conjuntos puestos, de frente y de espaldas. Las imágenes quedan en `Arte/Blender/vistas/probador_*.png` y sirven para ver que nada atraviesa el cuerpo sin abrir Unity. Para probar otras combinaciones, cambia la lista `CONJUNTOS` del final del script.
