# Bicho Show en el móvil (Android)

## Sacar la APK

1. En Unity Hub, el módulo **Android Build Support** (con OpenJDK y el SDK y NDK) tiene que estar instalado en la 6000.6.3f1.
2. En Unity: **BichoShow → Móvil: compilar la APK de prueba (Android)**.
   - La primera vez tarda bastante: Unity cambia la plataforma a Android y vuelve a importar el proyecto, y IL2CPP compila todo el código a C++.
   - Sale en `Builds/Android/BichoShow.apk`.
3. Pasa la APK al móvil (por cable, Drive…) y ábrela. La primera vez, Android pide permiso para instalar apps de esa fuente.

También hay **BichoShow → Móvil: compilar e instalar en el móvil conectado por USB**. Necesita el móvil con las **opciones de desarrollador** y la **depuración USB** activadas. La instala y la abre sola.

Lo que pone la compilación:

- Identificador `com.devurqui.bichoshow`.
- IL2CPP y ARM64: los móviles nuevos solo admiten 64 bits.
- Solo en horizontal.
- Permiso de internet, para las salas online y el ranking.

Después, para volver a compilar para Windows, **Compilar el juego para Windows** cambia la plataforma otra vez (tarda un poco).

## Controles táctiles

`Scripts/Jugador/ControlesTactiles.cs` es un **mando virtual**: el juego lo trata como un mando más, así que sirve para unirse (A), para los menús y para jugar.

| Dónde | Qué |
|---|---|
| Mitad izquierda | Stick: aparece donde pones el dedo y lo sigue si te pasas del borde |
| Abajo a la derecha | **A** (saltar / aceptar), **X** (lanzarse) y **AGARRAR** (mantener) |
| Arriba a la derecha | **START** |

- Si conectas un mando de verdad (Bluetooth), los botones táctiles se esconden.
- Se dibujan solo en tu pantalla: en una partida online no se mandan a los demás.
- En el móvil, el juego va a 60 imágenes por segundo y la pantalla no se apaga.
- Android usa el nivel de calidad **Mobile** (Mobile_RPAsset), que ya venía en el proyecto.

**Probarlos en el PC:** **BichoShow → Móvil: probar los controles táctiles en el editor**, y luego Play. El ratón hace de dedo.

## Qué falta

- Escribir el nombre y el código de sala con el teclado del móvil. Ahora se hace con el stick, como en las recreativas.
- Mover la cámara con el dedo (el stick derecho).
- Ajustar el rendimiento según cómo vaya en un móvil de verdad.
- iPhone: hace falta un Mac con Xcode y la cuenta de desarrollador de Apple.
