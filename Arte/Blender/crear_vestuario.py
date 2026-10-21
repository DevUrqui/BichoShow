"""
Bicho Show — vestuario de Bichete (gorros, cosas de la cara y del cuerpo) para Unity.

Uso (sin abrir Blender):
    blender -b --factory-startup -P crear_vestuario.py -- <salida.fbx> <salida.blend> [carpeta_vistas]
    (con rutas completas: si son relativas, Blender las toma desde C:\\)

Cada pieza tiene el origen en su punto de anclaje del esqueleto de Bichete (crear_bichete.py):
  - Gorros: AnclaGorro, en lo alto de la cabeza (la pieza se apoya ahí).
  - Cara (gafas, bigote, barba…): AnclaCara, justo delante de los ojos (x = 0, a la altura de los ojos).
  - Cuerpo (pajarita, medalla, capa, mochila…): AnclaPecho, sobre la barriga. Desde ahí, el centro
    del cuerpo está en (0, +0.455, +0.12) y la espalda (con el caparazón), en y ≈ +1.0.
Lo que mira a -Y aquí mira hacia delante en Unity. "HeliceAspas" va aparte: Unity la hace girar.
Al final se renderiza un "probador": un maniquí de Bichete con varios conjuntos puestos, de
frente y de espaldas, para ver que todo encaja (el maniquí no se exporta).
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

from kit_comun import Pieza, argumentos, crear_materiales, exportar, vistas  # noqa: E402

RUTA_FBX, RUTA_BLEND, CARPETA_VISTAS = argumentos()

COLORES = {
    "Oro": (1.00, 0.80, 0.22), "GemaRoja": (0.95, 0.12, 0.25), "GemaAzul": (0.2, 0.45, 1.0),
    "GorraAzul": (0.2, 0.45, 0.95), "Blanco": (0.95, 0.93, 0.88), "Negro": (0.08, 0.07, 0.1),
    "CintaRoja": (0.9, 0.15, 0.2), "FiestaRosa": (1.0, 0.45, 0.75), "FiestaAmarilla": (1.0, 0.85, 0.25),
    "Acero": (0.55, 0.57, 0.62), "Cuerno": (0.98, 0.92, 0.78), "HeliceRoja": (0.95, 0.25, 0.25),
    "HeliceAzul": (0.25, 0.55, 1.0), "Petalo": (1.0, 0.98, 0.95), "Polen": (1.0, 0.78, 0.15),
    "Tallo": (0.3, 0.7, 0.3), "CristalOscuro": (0.15, 0.13, 0.2), "CristalClaro": (0.7, 0.88, 1.0),
    "Montura": (0.75, 0.35, 0.15), "Bigote": (0.32, 0.18, 0.1), "NarizPayaso": (1.0, 0.1, 0.12),
    "PajaritaRoja": (0.85, 0.12, 0.25),
    # Nuevos
    "Cuero": (0.62, 0.38, 0.17), "CueroOscuro": (0.36, 0.2, 0.1), "MoradoBruja": (0.45, 0.22, 0.65),
    "LanaVerde": (0.3, 0.72, 0.45), "LanaCrema": (0.97, 0.93, 0.82), "LanaRoja": (0.9, 0.22, 0.25),
    "SetaRoja": (0.92, 0.15, 0.15), "Yema": (1.0, 0.72, 0.1), "CristalRosa": (1.0, 0.45, 0.7),
    "Celeste": (0.55, 0.8, 1.0), "BarbaBlanca": (0.96, 0.96, 0.97), "Perla": (0.98, 0.95, 0.92),
    "CintaAzul": (0.2, 0.42, 0.92), "CorbataAzul": (0.2, 0.3, 0.75), "CapaRoja": (0.8, 0.1, 0.18),
    "MochilaVerde": (0.36, 0.62, 0.3), "MascaraAzul": (0.15, 0.45, 0.85),
    # Solo para el probador (no se exporta)
    "ManiquiPiel": (1.0, 0.42, 0.36), "ManiquiCaparazon": (0.82, 0.18, 0.2), "ManiquiOjo": (0.98, 0.98, 0.98),
    "ManiquiPupila": (0.07, 0.07, 0.09), "ManiquiBarriga": (1.0, 0.93, 0.78),
}
crear_materiales(COLORES, brillantes=("Oro", "GemaRoja", "GemaAzul", "CristalOscuro", "CristalClaro", "NarizPayaso", "Negro",
                                      "CristalRosa", "Perla", "Yema", "SetaRoja", "Celeste"))

objetos = []

# ---------------------------------------------------------------------------
# Gorros (origen: base, sobre la cabeza)
# ---------------------------------------------------------------------------

p = Pieza("Corona")
p.toro((0, 0, 0.04), 0.2, 0.05, "Oro", segmentos=24, segmentos_tubo=8)
p.cilindro((0, 0, 0.1), 0.21, 0.13, "Oro", radio_arriba=0.24, segmentos=24)
for i in range(5):
    a = 2 * math.pi * i / 5 + math.pi / 2
    x, y = 0.21 * math.cos(a), 0.21 * math.sin(a)
    p.cilindro((x, y, 0.23), 0.06, 0.18, "Oro", radio_arriba=0.01, segmentos=8)
    p.esfera((x * 1.05, y * 1.05, 0.33), (0.035, 0.035, 0.035), "Oro", segmentos=8, anillos=5)
    p.esfera((x * 1.1, y * 1.1, 0.11), (0.04, 0.025, 0.04), "GemaRoja" if i % 2 == 0 else "GemaAzul",
             rotacion=(90, 0, math.degrees(a) + 90), segmentos=10, anillos=6)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Gorra")
p.esfera((0, 0.02, 0.0), (0.27, 0.27, 0.2), "GorraAzul", segmentos=24, anillos=12)
p.esfera((0, -0.27, 0.03), (0.2, 0.17, 0.025), "GorraAzul", segmentos=20, anillos=8)          # visera
p.esfera((0, 0.02, 0.2), (0.035, 0.035, 0.025), "Blanco", segmentos=10, anillos=6)            # botón
p.cilindro((0, 0.02, 0.02), 0.272, 0.05, "Blanco", segmentos=24)                               # franja
objetos.append(p.crear(bisel=0.0))

p = Pieza("Chistera")
p.cilindro((0, 0, 0.02), 0.32, 0.03, "Negro", segmentos=28)                                    # ala
p.cilindro((0, 0, 0.22), 0.19, 0.4, "Negro", radio_arriba=0.21, segmentos=24)
p.cilindro((0, 0, 0.09), 0.195, 0.08, "CintaRoja", segmentos=24)
objetos.append(p.crear(bisel=0.01))

p = Pieza("GorroFiesta")
p.cilindro((0, 0, 0.24), 0.2, 0.48, "FiestaRosa", radio_arriba=0.01, segmentos=20, rayas=("FiestaRosa", "FiestaAmarilla", 8))
p.esfera((0, 0, 0.5), (0.07, 0.07, 0.07), "Blanco", segmentos=12, anillos=6)                    # pompón
p.toro((0, 0, 0.02), 0.2, 0.03, "FiestaAmarilla", segmentos=20, segmentos_tubo=6)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Vikingo")
p.esfera((0, 0, 0.0), (0.28, 0.28, 0.22), "Acero", segmentos=24, anillos=12)
p.toro((0, 0, 0.02), 0.275, 0.035, "Oro", segmentos=24, segmentos_tubo=6)
for s in (-1, 1):
    pts = [(0.24 * s, 0, 0.08), (0.36 * s, 0, 0.14), (0.44 * s, 0, 0.26), (0.46 * s, 0, 0.4)]
    for i in range(len(pts) - 1):
        p.tubo(pts[i], pts[i + 1], 0.065 - i * 0.02, "Cuerno", radio_final=0.065 - (i + 1) * 0.02, segmentos=10)
objetos.append(p.crear(bisel=0.0))

p = Pieza("GorroHelice")
p.esfera((0, 0, 0.0), (0.26, 0.26, 0.2), "HeliceAzul", segmentos=24, anillos=12)
p.cilindro((0, 0, 0.06), 0.262, 0.08, "FiestaAmarilla", segmentos=24, rayas=("FiestaAmarilla", "HeliceRoja", 8))
p.cilindro((0, 0, 0.25), 0.02, 0.12, "Acero", segmentos=8)                                    # eje
objetos.append(p.crear(bisel=0.0))

p = Pieza("HeliceAspas")  # origen: el centro del eje (Unity la hace girar)
p.esfera((0, 0, 0), (0.035, 0.035, 0.03), "HeliceRoja", segmentos=10, anillos=6)
for a in (0, 180):
    r = math.radians(a)
    p.esfera((0.16 * math.cos(r), 0.16 * math.sin(r), 0), (0.15, 0.045, 0.012), "HeliceRoja",
             rotacion=(18 if a == 0 else -18, 0, a), segmentos=14, anillos=6)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Flor")
p.tubo((0.0, 0, -0.02), (0.05, 0, 0.18), 0.02, "Tallo", segmentos=8)
for i in range(8):
    a = 2 * math.pi * i / 8
    p.esfera((0.05 + 0.11 * math.cos(a), 0.11 * math.sin(a) - 0.02, 0.22), (0.09, 0.04, 0.02), "Petalo",
             rotacion=(0, 0, math.degrees(a)), segmentos=12, anillos=6)
p.esfera((0.05, -0.02, 0.23), (0.06, 0.06, 0.035), "Polen", segmentos=14, anillos=8)
objetos.append(p.crear(bisel=0.0))

# --- Gorros nuevos ---

p = Pieza("GorroChef")                                                        # gorro de cocinero, alto y abombado
p.cilindro((0, 0, 0.08), 0.24, 0.16, "Blanco", segmentos=28)
p.cilindro((0, 0, 0.2), 0.23, 0.12, "Blanco", radio_arriba=0.27, segmentos=28)
p.esfera((0, 0, 0.36), (0.28, 0.28, 0.17), "Blanco", segmentos=24, anillos=12)
for i in range(6):
    a = 2 * math.pi * i / 6
    p.esfera((0.17 * math.cos(a), 0.17 * math.sin(a), 0.38), (0.13, 0.13, 0.12), "Blanco", segmentos=14, anillos=8)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Vaquero")                                                          # sombrero de vaquero
p.esfera((0, 0, 0.04), (0.5, 0.42, 0.05), "Cuero", segmentos=32, anillos=10)   # ala
p.toro((0, 0, 0.06), 0.4, 0.035, "Cuero", segmentos=32, segmentos_tubo=8)                       # borde del ala
p.cilindro((0, 0, 0.18), 0.22, 0.26, "Cuero", radio_arriba=0.18, segmentos=24)
p.esfera((0, 0, 0.3), (0.17, 0.2, 0.06), "CueroOscuro", segmentos=16, anillos=8)  # la hendidura de arriba
p.cilindro((0, 0, 0.1), 0.222, 0.06, "CueroOscuro", segmentos=24)                # cinta
objetos.append(p.crear(bisel=0.0))

p = Pieza("Bruja")                                                            # sombrero de bruja con la punta doblada
p.cilindro((0, 0, 0.02), 0.4, 0.03, "MoradoBruja", segmentos=32)
p.cilindro((0, 0, 0.22), 0.22, 0.38, "MoradoBruja", radio_arriba=0.12, segmentos=24)
p.tubo((0, 0, 0.4), (0.04, 0.08, 0.56), 0.12, "MoradoBruja", radio_final=0.07, segmentos=16)
p.tubo((0.04, 0.08, 0.56), (0.1, 0.22, 0.6), 0.07, "MoradoBruja", radio_final=0.015, segmentos=12)
p.cilindro((0, 0, 0.08), 0.215, 0.07, "Negro", segmentos=24)                    # cinta
p.caja((0, -0.215, 0.08), (0.1, 0.03, 0.09), "Oro")                            # hebilla
objetos.append(p.crear(bisel=0.0))

p = Pieza("GorroLana")                                                        # gorro de lana con pompón
p.esfera((0, 0, 0.02), (0.27, 0.27, 0.25), "LanaVerde", segmentos=24, anillos=12)
p.cilindro((0, 0, 0.04), 0.275, 0.1, "LanaCrema", segmentos=32, rayas=("LanaCrema", "LanaVerde", 20))   # vuelta acanalada
p.esfera((0, 0, 0.29), (0.11, 0.11, 0.1), "LanaCrema", segmentos=16, anillos=8)  # pompón
objetos.append(p.crear(bisel=0.0))

p = Pieza("OrejasConejo")                                                     # diadema con orejas de conejo
p.toro((0, 0, -0.08), 0.29, 0.025, "FiestaRosa", rotacion=(0, 90, 90), segmentos=24, segmentos_tubo=6, arco=180)
for s in (-1, 1):
    p.esfera((0.19 * s, 0.0, 0.33), (0.075, 0.04, 0.22), "Blanco", rotacion=(0, 14 * s, 0), segmentos=16, anillos=10)
    p.esfera((0.195 * s, -0.025, 0.33), (0.04, 0.02, 0.15), "FiestaRosa", rotacion=(0, 14 * s, 0), segmentos=12, anillos=8)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Seta")                                                             # sombrero de seta
p.esfera((0, 0, 0.06), (0.38, 0.38, 0.24), "SetaRoja", segmentos=28, anillos=14)
for x, y, r in ((0.0, -0.2, 0.06), (0.2, -0.12, 0.05), (-0.2, -0.1, 0.055), (0.12, 0.18, 0.06), (-0.15, 0.17, 0.05), (0.0, 0.0, 0.07)):
    z = 0.06 + 0.24 * math.sqrt(max(0.0, 1 - (x / 0.38) ** 2 - (y / 0.38) ** 2))
    p.esfera((x, y, z), (r, r, 0.02), "Blanco", segmentos=12, anillos=6)
objetos.append(p.crear(bisel=0.0))

p = Pieza("HuevoFrito")                                                       # un huevo frito en la cabeza
for x, y, rx, ry in ((0.0, 0.0, 0.26, 0.22), (0.12, 0.1, 0.18, 0.15), (-0.13, 0.08, 0.15, 0.14), (0.05, -0.14, 0.17, 0.12)):
    p.esfera((x, y, 0.04), (rx, ry, 0.035), "Blanco", segmentos=20, anillos=6)
p.esfera((0.02, -0.01, 0.08), (0.1, 0.1, 0.07), "Yema", segmentos=18, anillos=10)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Aureola")                                                          # aureola de oro flotando
p.toro((0, 0, 0.32), 0.21, 0.035, "Oro", segmentos=32, segmentos_tubo=8)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Pirata")                                                           # bicornio pirata con calavera
p.esfera((0, 0, 0.02), (0.27, 0.27, 0.18), "Negro", segmentos=24, anillos=12)
p.esfera((0, 0.02, 0.14), (0.46, 0.17, 0.2), "Negro", segmentos=28, anillos=12)
p.esfera((0, -0.16, 0.2), (0.06, 0.02, 0.055), "Blanco", segmentos=12, anillos=6)       # calavera
for s in (-1, 1):
    p.caja((0, -0.165, 0.12), (0.16, 0.015, 0.025), "Blanco", rotacion=(0, 35 * s, 0))  # huesos cruzados
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Cara (origen: delante de los ojos; los ojos están en x = ±0.17)
# ---------------------------------------------------------------------------

p = Pieza("GafasSol")
for s in (-1, 1):
    p.esfera((0.17 * s, -0.01, 0.0), (0.15, 0.025, 0.11), "CristalOscuro", segmentos=20, anillos=10)
    p.tubo((0.31 * s, 0.0, 0.03), (0.36 * s, 0.25, 0.04), 0.012, "Negro", segmentos=6)          # patillas
p.tubo((-0.04, -0.02, 0.04), (0.04, -0.02, 0.04), 0.014, "Negro", segmentos=6)                  # puente
objetos.append(p.crear(bisel=0.0))

p = Pieza("GafasRedondas")
for s in (-1, 1):
    p.toro((0.17 * s, -0.01, 0.0), 0.12, 0.014, "Montura", rotacion=(90, 0, 0), segmentos=24, segmentos_tubo=6)
    p.esfera((0.17 * s, 0.0, 0.0), (0.115, 0.008, 0.115), "CristalClaro", segmentos=18, anillos=6)
    p.tubo((0.29 * s, 0.0, 0.0), (0.36 * s, 0.25, 0.02), 0.01, "Montura", segmentos=6)
p.tubo((-0.05, -0.01, 0.02), (0.05, -0.01, 0.02), 0.012, "Montura", segmentos=6)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Bigote")
for s in (-1, 1):
    p.esfera((0.08 * s, -0.06, -0.18), (0.1, 0.04, 0.04), "Bigote", rotacion=(0, 12 * s, 0), segmentos=14, anillos=8)
    p.esfera((0.17 * s, -0.05, -0.15), (0.04, 0.03, 0.035), "Bigote", segmentos=10, anillos=6)        # rizo
objetos.append(p.crear(bisel=0.0))

p = Pieza("NarizPayaso")
p.esfera((0, -0.09, -0.12), (0.075, 0.07, 0.07), "NarizPayaso", segmentos=18, anillos=10)
objetos.append(p.crear(bisel=0.0))

# --- Cara nuevos ---

p = Pieza("GafasCorazon")                                                     # gafas de corazón
for s in (-1, 1):
    cx = 0.17 * s
    for dx in (-0.055, 0.055):
        p.esfera((cx + dx, -0.015, 0.035), (0.075, 0.02, 0.07), "CristalRosa", segmentos=16, anillos=8)
    p.caja((cx, -0.015, -0.035), (0.13, 0.03, 0.13), "CristalRosa", rotacion=(0, 45, 0))
    p.tubo((0.29 * s, 0.0, 0.03), (0.36 * s, 0.25, 0.04), 0.012, "FiestaRosa", segmentos=6)
p.tubo((-0.06, -0.02, 0.05), (0.06, -0.02, 0.05), 0.014, "FiestaRosa", segmentos=6)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Monoculo")                                                         # monóculo con cadenita
p.toro((0.17, -0.02, 0.0), 0.125, 0.016, "Oro", rotacion=(90, 0, 0), segmentos=28, segmentos_tubo=6)
p.esfera((0.17, -0.015, 0.0), (0.12, 0.008, 0.12), "CristalClaro", segmentos=18, anillos=6)
cadena = [(0.27, -0.02, -0.08), (0.3, -0.01, -0.2), (0.29, 0.0, -0.32), (0.25, 0.02, -0.42)]
for a, b in zip(cadena, cadena[1:]):
    p.tubo(a, b, 0.008, "Oro", segmentos=6)
objetos.append(p.crear(bisel=0.0))

p = Pieza("ParcheOjo")                                                        # parche de pirata en el ojo izquierdo
p.esfera((-0.17, -0.03, 0.0), (0.13, 0.03, 0.13), "Negro", segmentos=18, anillos=8)
p.tubo((-0.05, -0.03, 0.08), (0.34, 0.2, 0.18), 0.012, "Negro", segmentos=6)
p.tubo((-0.29, -0.02, -0.05), (-0.36, 0.22, -0.08), 0.012, "Negro", segmentos=6)
objetos.append(p.crear(bisel=0.0))

p = Pieza("GafasBucear")                                                      # gafas de bucear con tubo
p.caja((0, -0.02, 0.0), (0.56, 0.07, 0.26), "MascaraAzul")
p.caja((0, -0.06, 0.0), (0.48, 0.02, 0.19), "CristalClaro")
for s in (-1, 1):
    p.tubo((0.31 * s, 0.0, 0.0), (0.38 * s, 0.28, 0.02), 0.03, "MascaraAzul", segmentos=8)  # goma
p.tubo((0.33, -0.02, -0.12), (0.36, -0.01, 0.42), 0.035, "FiestaAmarilla", segmentos=10)   # tubo
p.esfera((0.36, -0.01, 0.43), (0.045, 0.045, 0.03), "FiestaAmarilla", segmentos=10, anillos=6)
p.tubo((0.33, -0.02, -0.12), (0.12, -0.08, -0.2), 0.03, "FiestaAmarilla", segmentos=10)    # boquilla
objetos.append(p.crear(bisel=0.03))

p = Pieza("Barba")                                                            # barbaza blanca de mago
for x, y, z, rx, ry, rz in ((0.0, -0.07, -0.24, 0.18, 0.07, 0.12), (0.13, -0.04, -0.2, 0.09, 0.07, 0.09),
                            (-0.13, -0.04, -0.2, 0.09, 0.07, 0.09), (0.0, -0.09, -0.37, 0.11, 0.07, 0.1),
                            (0.0, -0.09, -0.47, 0.06, 0.05, 0.06)):
    p.esfera((x, y, z), (rx, ry, rz), "BarbaBlanca", segmentos=16, anillos=10)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Chupete")                                                          # chupete
p.esfera((0, -0.06, -0.25), (0.11, 0.025, 0.075), "Celeste", segmentos=18, anillos=8)
p.toro((0, -0.105, -0.25), 0.05, 0.013, "Celeste", rotacion=(90, 0, 0), segmentos=18, segmentos_tubo=6)
p.esfera((0, -0.085, -0.25), (0.03, 0.02, 0.03), "Blanco", segmentos=10, anillos=6)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Cuerpo (origen: sobre la barriga; el cuello está en z ≈ +0.22, la espalda en y ≈ +1.0)
# ---------------------------------------------------------------------------

CUELLO = (0.0, 0.455, 0.22)   # centro del cuerpo a la altura del "cuello"

p = Pieza("Pajarita")
p.esfera((0, -0.03, 0), (0.04, 0.03, 0.04), "PajaritaRoja", segmentos=12, anillos=6)
for s in (-1, 1):
    p.cilindro((0.09 * s, -0.03, 0), 0.07, 0.15, "PajaritaRoja", radio_arriba=0.025, rotacion=(0, -90 * s, 0), segmentos=12)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Medalla")                                                          # medalla de oro con cinta
for s in (-1, 1):
    p.tubo((0.22 * s, 0.02, 0.27), (0.04 * s, -0.13, -0.02), 0.022, "CintaAzul", segmentos=8)
p.cilindro((0, -0.17, -0.08), 0.1, 0.03, "Oro", rotacion=(90, 0, 0), segmentos=24)
p.esfera((0, -0.19, -0.08), (0.045, 0.01, 0.045), "FiestaAmarilla", segmentos=5, anillos=4)
objetos.append(p.crear(bisel=0.0))

p = Pieza("CollarPerlas")                                                     # collar de perlas al cuello
for i in range(30):
    a = 2 * math.pi * i / 30
    p.esfera((CUELLO[0] + 0.53 * math.sin(a), CUELLO[1] - 0.53 * math.cos(a), CUELLO[2] - 0.06 * math.cos(a)),
             (0.04, 0.04, 0.04), "Perla", segmentos=10, anillos=6)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Babero")                                                           # babero (¡estamos en la cocina!)
p.esfera((0, -0.05, 0.05), (0.21, 0.02, 0.19), "CintaRoja", rotacion=(-22, 0, 0), segmentos=24, anillos=10)
p.esfera((0, -0.065, 0.05), (0.185, 0.02, 0.165), "Blanco", rotacion=(-22, 0, 0), segmentos=24, anillos=10)
for x, z in ((-0.07, 0.1), (0.08, 0.03), (-0.02, -0.05)):
    p.esfera((x, -0.085 - (0.05 - z) * 0.4, z), (0.028, 0.01, 0.028), "CintaRoja", rotacion=(-22, 0, 0), segmentos=10, anillos=5)
for s in (-1, 1):
    p.tubo((0.16 * s, -0.01, 0.2), (0.36 * s, 0.2, 0.28), 0.014, "CintaRoja", segmentos=6)          # cintas
objetos.append(p.crear(bisel=0.0))

p = Pieza("Corbata")                                                          # corbata a rayas
p.esfera((0, -0.03, 0.2), (0.055, 0.035, 0.045), "CorbataAzul", segmentos=14, anillos=8)        # nudo
p.caja((0, -0.1, -0.02), (0.11, 0.02, 0.4), "CorbataAzul", rotacion=(-17, 0, 0))
for z in (0.08, -0.04, -0.16):
    p.caja((0, -0.115 - (0.08 - z) * 0.3, z), (0.112, 0.012, 0.03), "FiestaAmarilla", rotacion=(-17, 25, 0))
p.caja((0, -0.165, -0.23), (0.08, 0.02, 0.08), "CorbataAzul", rotacion=(-17, 45, 0))             # punta
objetos.append(p.crear(bisel=0.0))

p = Pieza("Bufanda")                                                          # bufanda de lana a rayas
p.toro(CUELLO, 0.5, 0.06, "LanaRoja", segmentos=32, segmentos_tubo=10, rayas=("LanaRoja", "LanaCrema", 12))
p.caja((-0.2, -0.13, -0.02), (0.15, 0.05, 0.44), "LanaRoja", rotacion=(-10, 0, 8))
p.caja((-0.05, -0.14, 0.02), (0.14, 0.05, 0.36), "LanaRoja", rotacion=(-10, 0, -6))
for x, z in ((-0.23, -0.2), (-0.07, -0.14)):
    p.caja((x, -0.17, z), (0.155, 0.055, 0.04), "LanaCrema", rotacion=(-10, 0, 0))                 # flecos
objetos.append(p.crear(bisel=0.0))

p = Pieza("Capa")                                                             # capa de superhéroe a la espalda
for a in (-56, -28, 0, 28, 56):
    d = (math.sin(math.radians(a)), math.cos(math.radians(a)))
    arriba = (CUELLO[0] + d[0] * 0.53, CUELLO[1] + d[1] * 0.53, 0.3)                              # a ras del caparazón
    abajo = (CUELLO[0] + d[0] * 0.66, CUELLO[1] + d[1] * 0.66, -0.55)                              # cae un poco hacia fuera
    centro = tuple((u + v) / 2 for u, v in zip(arriba, abajo))
    alto = math.dist(arriba, abajo)
    inclinacion = math.degrees(math.atan2(0.13, 0.85))
    p.caja(centro, (0.3, 0.035, alto), "CapaRoja", rotacion=(inclinacion, 0, -a))
p.toro((CUELLO[0], CUELLO[1], 0.3), 0.53, 0.03, "Oro", segmentos=32, segmentos_tubo=6, arco=180, rotacion=(0, 0, 0))  # cuello de la capa
p.esfera((0, -0.03, 0.12), (0.055, 0.03, 0.055), "Oro", segmentos=12, anillos=6)                  # broche
for s in (-1, 1):
    p.tubo((0.05 * s, -0.02, 0.13), (0.42 * s, 0.5, 0.3), 0.018, "Oro", segmentos=6)              # cordón
    p.tubo((0.42 * s, 0.5, 0.3), (0.38 * s, 1.02, 0.27), 0.018, "Oro", segmentos=6)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Mochila")                                                          # mochila a la espalda
p.caja((0, 1.13, 0.0), (0.46, 0.24, 0.5), "MochilaVerde")
p.caja((0, 1.11, 0.2), (0.47, 0.26, 0.14), "LanaCrema")                                           # solapa
p.caja((0, 1.26, -0.08), (0.3, 0.06, 0.2), "MochilaVerde")                                         # bolsillo
p.caja((0, 1.295, -0.02), (0.06, 0.01, 0.06), "Oro")                                               # cierre
for s in (-1, 1):
    p.tubo((0.17 * s, 1.0, 0.24), (0.2 * s, 0.98, -0.2), 0.025, "LanaCrema", segmentos=6)          # asas
objetos.append(p.crear(bisel=0.04))

exportar(objetos, RUTA_FBX, RUTA_BLEND)

vistas(objetos, CARPETA_VISTAS, "vestuario", [
    ("gorros", ["Corona", "Gorra", "Chistera", "GorroFiesta", "Vikingo", "GorroHelice", "HeliceAspas", "Flor"], 0.25, (0, -5, 1.6), (0, 0, 0.15)),
    ("gorros2", ["GorroChef", "Vaquero", "Bruja", "GorroLana", "OrejasConejo", "Seta", "HuevoFrito", "Aureola", "Pirata"], 0.25, (0, -6, 1.8), (0, 0, 0.2)),
    ("cara", ["GafasSol", "GafasRedondas", "Bigote", "NarizPayaso", "GafasCorazon", "Monoculo", "ParcheOjo", "GafasBucear", "Barba", "Chupete"], 0.3, (0, -6, 0.6), (0, 0, -0.1)),
], color_fondo=(0.86, 0.85, 0.92))

# ---------------------------------------------------------------------------
# Probador: un maniquí de Bichete (mismas medidas que crear_bichete.py) con conjuntos puestos
# ---------------------------------------------------------------------------

CENTRO = Vector((0.0, 0.0, 0.88))
ANCLAS = {"gorro": Vector((0, 0, 1.37)), "cara": Vector((0, -0.411, 1.16)), "cuerpo": Vector((0, -0.455, 0.76))}

m = Pieza("Maniqui")
m.esfera(tuple(CENTRO), (0.5, 0.46, 0.58), "ManiquiPiel", segmentos=32, anillos=16)
m.esfera((0, 0.07, 0.92), (0.51, 0.45, 0.58), "ManiquiCaparazon", segmentos=32, anillos=16)    # caparazón (asoma por detrás)
m.esfera((0, -0.36, 0.6), (0.27, 0.1, 0.23), "ManiquiBarriga", segmentos=24, anillos=12)
for s in (-1, 1):
    m.esfera((0.17 * s, -0.29, 1.16), (0.145, 0.11, 0.165), "ManiquiOjo", segmentos=20, anillos=10)
    m.esfera((0.16 * s, -0.39, 1.145), (0.075, 0.03, 0.09), "ManiquiPupila", segmentos=14, anillos=8)
    m.tubo((0.12 * s, 0.06, 1.42), (0.2 * s, 0.02, 1.62), 0.03, "ManiquiPiel", segmentos=8)          # antenas
    m.tubo((0.2 * s, 0.02, 1.62), (0.3 * s, -0.05, 1.76), 0.025, "ManiquiPiel", segmentos=8)
    m.tubo((0.44 * s, 0.0, 0.86), (0.64 * s, -0.09, 0.58), 0.06, "ManiquiPiel", segmentos=8)          # brazos
    m.tubo((0.22 * s, 0.0, 0.4), (0.24 * s, -0.1, 0.06), 0.07, "ManiquiPiel", segmentos=8)            # patas
maniqui = m.crear(bisel=0.0)

CONJUNTOS = [
    ("chef", "GorroChef", "Bigote", "Babero"),
    ("pirata", "Pirata", "ParcheOjo", "Capa"),
    ("bruja", "Bruja", "GafasRedondas", "Bufanda"),
    ("excursion", "Vaquero", "GafasBucear", "Mochila"),
    ("fiesta", "OrejasConejo", "GafasCorazon", "CollarPerlas"),
    ("abuelo", "GorroLana", "Barba", "Corbata"),
    ("bebe", "HuevoFrito", "Chupete", "Medalla"),
    ("santo", "Aureola", "Monoculo", "Pajarita"),
    ("seta", "Seta", "NarizPayaso", "Capa"),
]

if CARPETA_VISTAS:
    escena = bpy.context.scene
    camara = escena.camera
    escena.render.resolution_x = 900
    escena.render.resolution_y = 600
    por_nombre = {o.name: o for o in objetos}
    for nombre, gorro, cara, cuerpo in CONJUNTOS:
        puestos = {por_nombre[gorro]: ANCLAS["gorro"], por_nombre[cara]: ANCLAS["cara"], por_nombre[cuerpo]: ANCLAS["cuerpo"]}
        for o in objetos:
            o.hide_render = o not in puestos
        for o, ancla in puestos.items():
            o.location = ancla
        maniqui.hide_render = False
        for lado, posicion in (("frente", (1.4, -3.6, 1.6)), ("espalda", (-1.6, 3.4, 1.8))):
            camara.location = posicion
            camara.rotation_euler = (Vector((0, 0, 0.95)) - Vector(posicion)).to_track_quat("-Z", "Y").to_euler()
            escena.render.filepath = os.path.join(CARPETA_VISTAS, f"probador_{nombre}_{lado}.png")
            bpy.ops.render.render(write_still=True)

print("Vestuario listo.")
