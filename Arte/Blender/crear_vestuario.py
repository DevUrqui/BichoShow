"""
Bicho Show — vestuario de Bichete (gorros y complementos) para Unity.

Uso (sin abrir Blender):
    blender -b --factory-startup -P crear_vestuario.py -- <salida.fbx> <salida.blend> [carpeta_vistas]

Cada pieza tiene el origen en su punto de anclaje del esqueleto de Bichete (crear_bichete.py):
  - Gorros: AnclaGorro, en lo alto de la cabeza (la pieza se apoya ahí).
  - Gafas, bigote, nariz: AnclaCara, justo delante de los ojos (x = 0, a la altura de los ojos).
  - Pajarita: AnclaPecho, sobre la barriga.
Lo que mira a -Y aquí mira hacia delante en Unity. "HeliceAspas" va aparte: Unity la hace girar.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

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
}
crear_materiales(COLORES, brillantes=("Oro", "GemaRoja", "GemaAzul", "CristalOscuro", "CristalClaro", "NarizPayaso", "Negro"))

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

# ---------------------------------------------------------------------------
# Complementos de la cara (origen: delante de los ojos) y del pecho
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

p = Pieza("Pajarita")  # origen: sobre la barriga
p.esfera((0, -0.03, 0), (0.04, 0.03, 0.04), "PajaritaRoja", segmentos=12, anillos=6)
for s in (-1, 1):
    p.cilindro((0.09 * s, -0.03, 0), 0.07, 0.15, "PajaritaRoja", radio_arriba=0.025, rotacion=(0, -90 * s, 0), segmentos=12)
objetos.append(p.crear(bisel=0.0))

exportar(objetos, RUTA_FBX, RUTA_BLEND)

vistas(objetos, CARPETA_VISTAS, "vestuario", [
    ("gorros", ["Corona", "Gorra", "Chistera", "GorroFiesta", "Vikingo", "GorroHelice", "HeliceAspas", "Flor"], 0.25, (0, -5, 1.6), (0, 0, 0.15)),
    ("complementos", ["GafasSol", "GafasRedondas", "Bigote", "NarizPayaso", "Pajarita"], 0.3, (0, -4, 0.8), (0, 0, -0.05)),
], color_fondo=(0.86, 0.85, 0.92))

print("Vestuario listo.")
