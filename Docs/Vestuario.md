# Vestuario

Cada jugador elige su bicho y lo viste en **Vestuario**, desde el menú (los bichos están en [Personajes](Personajes.md)). Hay tres huecos de ropa y se puede llevar una cosa de cada a la vez. Lo que elige cada uno se guarda, y los bots se visten al azar. Online, cada uno ve la ropa de los demás.

## Lo que hay

| Hueco | Prendas |
|---|---|
| **Gorro** (16) | Corona, gorra, chistera, gorro de fiesta, casco vikingo, gorro con hélice, flor, **gorro de chef**, **sombrero vaquero**, **sombrero de bruja**, **gorro de lana**, **orejas de conejo**, **seta**, **huevo frito**, **aureola**, **sombrero pirata** |
| **Cara** (10) | Gafas de sol, gafas redondas, bigote, nariz de payaso, **gafas de corazón**, **monóculo**, **parche pirata**, **gafas de bucear** (con tubo), **barba de mago**, **chupete** |
| **Cuerpo** (8) | Pajarita, **medalla de oro**, **collar de perlas**, **babero**, **corbata**, **bufanda**, **capa**, **mochila** |

En negrita, las nuevas. La pajarita estaba antes en el hueco de la cara y ahora está en el de cuerpo, así que se puede llevar con gafas. Quien la tuviera puesta tendrá que volver a ponérsela.

La capa y la mochila van a la espalda. Con la cámara de las carreras, que va detrás del bicho, son lo que más se ve.

## Cómo se elige

En **Vestuario**, la cámara se acerca a los bichos y los pone grandes, arriba. Cada jugador tiene su panel justo debajo de su bicho, con un piquito que lo señala, así que se ve cómo le queda lo que elige. Cada uno lo maneja con su mando:

- Arriba y abajo cambian de fila (bicho, gorro, cara, cuerpo).
- Izquierda y derecha cambian el bicho o la prenda.
- **A**: listo.
- **START**: volver.

## Cómo le queda a cada bicho

La ropa está hecha para Bichete. Cada bicho la lleva a su **talla** y, además, con un **ajuste** propio en cada zona: gorro, cara, delante, espalda y alrededor (collar, bufanda, flotador). Todo está en .

- **Lo de la cara va a la medida de sus ojos**: se agranda o se encoge según lo separados que los tenga respecto a Bichete. Así las gafas caen siempre sobre los ojos.
- **Ajustes por bicho** (desplazamientos en metros: x a su derecha, y arriba, z adelante):

  | Bicho | Qué se le ajusta |
  |---|---|
  | **Croqueta** (rana) | Lo de la cara, más adelante (sus ojos salen mucho y la tapaban). La nariz de payaso, más pequeña. |
  | **Cornelio** (escarabajo) | El gorro, más atrás y echado hacia atrás, para que no lo atraviese el cuerno. |
  | **Serafín** (serpiente) | Lo del cuerpo, más pequeño: es muy estrecho. |
  | **Zumbi** (abeja), **Migas** (hormiga) y **Ocho** (araña) | Lo de la espalda va **encima del abdomen**, que lo tapaba. A Ocho, la capa le envuelve el abdomen. |
  | **Concha** (caracol) | Lo de la espalda, encima de la concha. |

- Para retocar uno: en , cambia el desplazamiento o la talla de su zona. Si solo falla una prenda, añádela en  de ese bicho. La capa y la nariz ya están así.
- Lo de la espalda es lo que más se ve en el juego, porque la cámara va detrás y por encima.

## Añadir más prendas

1. Modélala en `Arte/Blender/crear_vestuario.py`, con el origen en su anclaje:
   - **Gorro:** en lo alto de la cabeza.
   - **Cara:** delante de los ojos.
   - **Cuerpo:** sobre la barriga. Desde ahí, la espalda con el caparazón queda en y ≈ +1.0.
2. Si va a la espalda o alrededor del cuerpo, añádela a  o  en .
3. Añade su nombre a la lista que toque en `Scripts/Personaje/Vestuario.cs` (`Gorros`, `Complementos` o `Cuerpos`) y el nombre que se ve en el menú (`NombresGorros`…).
4. Vuelve a generar el kit con rutas completas:

```
blender -b --factory-startup -P Arte/Blender/crear_vestuario.py -- F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Assets/BichoShow/Modelos/KitVestuario.fbx F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Arte/Blender/KitVestuario.blend F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Arte/Blender/vistas
```

Al volver a Unity, las prendas se rehacen solas al importar el kit. Si no, usa **BichoShow → Personaje: preparar vestuario**.

El script también renderiza un **probador**: un maniquí de Bichete con varios conjuntos puestos, de frente y de espaldas. Las imágenes quedan en `Arte/Blender/vistas/probador_*.png` y sirven para ver que nada atraviesa el cuerpo sin abrir Unity. Para probar otras combinaciones, cambia la lista `CONJUNTOS` del final del script.
