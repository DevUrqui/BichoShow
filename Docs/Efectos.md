# Efectos y partículas

Las partículas usan texturas dibujadas en Blender. Son blancas con transparencia, y cada partícula pone su color.

## Las texturas

`Arte/Blender/crear_texturas_particulas.py` las deja en `Assets/BichoShow/Texturas/Efectos`. Se puede lanzar por el MCP de Blender (el complemento "MCP for Blender") o desde la consola.

| Textura | Para qué |
|---|---|
| Nube (hoja 2x2) | polvo de aterrizajes y porrazos: nubecitas que giran |
| Estrella | estrellitas de los golpes |
| Destello, Onda, Chispa | el golpe: un fogonazo, un aro que se abre y chispas que se estiran |
| Confeti (hoja 2x2) | tiras, círculos, estrellas y serpentinas |
| Burbuja | la espuma (las pompas suben un poco) |
| Gota | gotas y salpicaduras |
| Polen | el géiser de polen (brilla) |
| Cristal | los granos de sal |

Las del Cortacésped (viento, remolino, láser…) las hace `crear_efectos_cortacesped.py`; ver `El Cortacesped Mecha.md`.

## En Unity

- `Editor/RecursosEfectos.cs` crea solo los materiales `Resources/Efectos*.mat` al abrir el proyecto o al recompilar. Destello, onda, chispa y polen son aditivos: brillan.
- `Efectos.Polvo` y `Efectos.Impacto` los usan nada más darle a Play. No hay que rehacer nada.
- Para las partículas que ya están guardadas en los mapas (confeti, espuma, gotas, sal, polen), usa el menú **BichoShow → Efectos: poner las partículas nuevas en todas las escenas**. Solo cambia esas partículas y guarda las escenas, sin rehacer los mapas.
- Los mapas que se crean de nuevo ya salen con las partículas nuevas.
