"""
Bicho Show — kit de "La Cumbre del Girasol" (gran final en espiral) para Unity.

Uso (sin abrir Blender):
    blender -b --factory-startup -P crear_kit_girasol.py -- <salida.fbx> <salida.blend> [carpeta_vistas]

Mismas reglas que el resto de kits (ver kit_comun.py): lo que mira a -Y aquí mira a +Z en Unity,
Unity (x, y, z) = (-X, Z, -Y). "Equipo" lo pinta Unity (la hoja, el pétalo, la banderola…).
Orígenes:
- Mariquita, Ventilador, HojaTallo, Banderola, ArcoInvernadero: centro de la base.
- BolaPolen, Abejorro, AbejorroAla, AspasVentilador: en su centro (giran o vuelan).
- HojaRedonda: centro de la base (plana, 0,15 de grueso).
- HojaLarga: centro (3 × 14, el largo a lo largo de Y; arriba en Z = +0,15).
- Petalo: centro (2,2 × 4,5, el largo a lo largo de Y; arriba en Z = +0,15).
- CabezaGirasol: centro de la cara de arriba del disco (radio 6), que está en Z = 0.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from kit_comun import Pieza, argumentos, crear_materiales, exportar, vistas  # noqa: E402

RUTA_FBX, RUTA_BLEND, CARPETA_VISTAS = argumentos()

COLORES = {
    # Compartidos con otros kits (mismo color)
    "Equipo": (0.62, 0.62, 0.66), "Oro": (1.00, 0.80, 0.22), "Metal": (0.55, 0.57, 0.60),
    "OjoBlanco": (0.98, 0.98, 0.98), "PupilaNegra": (0.07, 0.07, 0.09), "Lente": (0.25, 0.45, 0.70),
    "CamaraCuerpo": (0.24, 0.24, 0.28), "LuzRoja": (1.00, 0.15, 0.15),
    # Girasol
    "MariquitaRoja": (0.92, 0.16, 0.14), "MariquitaNegra": (0.1, 0.08, 0.1), "MariquitaMejilla": (1.0, 0.55, 0.6),
    "Polen": (1.0, 0.7, 0.08), "PolenPunta": (1.0, 0.9, 0.4),
    "HojaVerdeClara": (0.48, 0.78, 0.3), "Nervio": (0.75, 0.92, 0.5),
    "PetaloAmarillo": (1.0, 0.8, 0.12), "PetaloNaranja": (1.0, 0.6, 0.12),
    "Semilla": (0.45, 0.25, 0.12), "SemillaClara": (0.72, 0.45, 0.18), "Caliz": (0.3, 0.55, 0.22),
    "AbejorroAmarillo": (1.0, 0.78, 0.15), "AbejorroNegro": (0.12, 0.1, 0.12), "AlaCristal": (0.85, 0.95, 1.0),
    "VentiladorCuerpo": (0.95, 0.92, 0.88), "VentiladorRejilla": (0.7, 0.72, 0.76),
    "HierroVerde": (0.22, 0.4, 0.36), "Estrella": (1.0, 0.86, 0.3),
}

crear_materiales(COLORES, brillantes=("Oro", "Metal", "OjoBlanco", "PupilaNegra", "Lente", "MariquitaRoja", "AlaCristal", "HierroVerde"),
                 emisivos=("LuzRoja",))

objetos = []

# ---------------------------------------------------------------------------
# Mariquita "bumper": acolchada, gorda y simpática (2,8 de largo)
# ---------------------------------------------------------------------------

p = Pieza("Mariquita")
p.esfera((0, 0.15, 0.55), (1.35, 1.5, 1.05), "MariquitaRoja", segmentos=32, anillos=16)          # caparazón
p.caja((0, 0.15, 1.58), (0.06, 2.6, 0.05), "MariquitaNegra")                                       # raya entre alas
for x, y, r in ((-0.6, 0.6, 0.3), (0.6, 0.6, 0.3), (-0.75, -0.2, 0.25), (0.75, -0.2, 0.25), (-0.4, 1.2, 0.22), (0.4, 1.2, 0.22), (0, -0.55, 0.2)):
    z = 0.55 + 1.05 * math.sqrt(max(0.0, 1 - (x / 1.35) ** 2 - ((y - 0.15) / 1.5) ** 2)) - 0.05
    p.esfera((x, y, z), (r, r, 0.08), "MariquitaNegra", segmentos=14, anillos=6)                    # lunares
p.esfera((0, -1.25, 0.55), (0.78, 0.6, 0.62), "MariquitaNegra", segmentos=24, anillos=12)        # cabeza
for s in (-1, 1):
    p.esfera((s * 0.3, -1.72, 0.75), (0.2, 0.12, 0.24), "OjoBlanco", segmentos=14, anillos=8)
    p.esfera((s * 0.3, -1.82, 0.72), (0.1, 0.06, 0.12), "PupilaNegra", segmentos=10, anillos=6)
    p.esfera((s * 0.55, -1.7, 0.42), (0.12, 0.05, 0.08), "MariquitaMejilla", segmentos=10, anillos=6)
    p.tubo((s * 0.25, -1.4, 1.05), (s * 0.55, -1.75, 1.6), 0.04, "MariquitaNegra", segmentos=6)   # antenas
    p.esfera((s * 0.55, -1.75, 1.62), (0.11, 0.11, 0.11), "MariquitaNegra", segmentos=10, anillos=6)
    for k in range(3):                                                                              # patitas
        p.esfera((s * 1.1, -0.5 + k * 0.6, 0.12), (0.18, 0.12, 0.12), "MariquitaNegra", segmentos=10, anillos=6)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Bola de polen (radio 1,2, centro en el origen): peluda y brillante
# ---------------------------------------------------------------------------

p = Pieza("BolaPolen")
p.esfera((0, 0, 0), (1.05, 1.05, 1.05), "Polen", segmentos=28, anillos=14)
n = 70
for i in range(n):                                                                                  # pinchitos en espiral áurea
    y = 1 - 2 * (i + 0.5) / n
    r = math.sqrt(1 - y * y)
    a = i * 2.399963
    d = (r * math.cos(a), r * math.sin(a), y)
    base = tuple(1.0 * c for c in d)
    punta = tuple(1.25 * c for c in d)
    p.tubo(base, punta, 0.09, "Polen", radio_final=0.02, segmentos=6)
    p.esfera(punta, (0.05, 0.05, 0.05), "PolenPunta", segmentos=6, anillos=4)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Hojas: trampolín (redonda, plana), puente (larga) y del tallo (decorado)
# ---------------------------------------------------------------------------


def nervios(p, largo, ancho, z, n):
    p.caja((0, 0, z), (0.14, largo * 0.9, 0.05), "Nervio")
    for k in range(n):
        t = -0.4 + 0.8 * k / max(1, n - 1)
        for s in (-1, 1):
            p.caja((s * ancho * 0.22, t * largo, z), (ancho * 0.42, 0.08, 0.04), "Nervio", rotacion=(0, 0, s * 35))


p = Pieza("HojaRedonda")
p.esfera((0, 0, 0.08), (1.6, 1.6, 0.12), "Equipo", segmentos=32, anillos=10)
for k in range(8):
    a = math.radians(k * 45)
    p.caja((0.7 * math.cos(a), 0.7 * math.sin(a), 0.19), (1.3, 0.07, 0.03), "Nervio", rotacion=(0, 0, k * 45))
p.cilindro((0, 0, 0.19), 0.15, 0.04, "Nervio", segmentos=12)
objetos.append(p.crear(bisel=0.0))

p = Pieza("HojaLarga")
p.esfera((0, 0, 0), (1.5, 7.0, 0.15), "Equipo", segmentos=32, anillos=12)
for s in (-1, 1):                                                                                   # bordes un poco levantados
    p.esfera((s * 1.3, 0, 0.1), (0.3, 6.0, 0.12), "Equipo", segmentos=16, anillos=8)
nervios(p, 14.0, 3.0, 0.14, 7)
objetos.append(p.crear(bisel=0.0))

p = Pieza("HojaTallo")                                                                              # sale hacia -Y, 8 de largo
p.tubo((0, 0, 0), (0, -1.6, 0.6), 0.18, "Caliz", segmentos=10)
p.esfera((0, -4.8, 0.9), (2.0, 3.4, 0.14), "HojaVerdeClara", rotacion=(-10, 0, 0), segmentos=28, anillos=12)
p.caja((0, -4.8, 1.0), (0.12, 6.0, 0.05), "Nervio", rotacion=(-10, 0, 0))
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Pétalo pisable (2,2 × 4,5) y la cabeza del girasol (disco de radio 6)
# ---------------------------------------------------------------------------

p = Pieza("Petalo")
p.esfera((0, 0, 0), (1.1, 2.25, 0.15), "Equipo", segmentos=28, anillos=12)
p.caja((0, 0, 0.13), (0.08, 3.6, 0.03), "PetaloNaranja")
objetos.append(p.crear(bisel=0.0))

p = Pieza("CabezaGirasol")
p.cilindro((0, 0, -0.5), 6.0, 1.0, "Semilla", segmentos=48)
p.toro((0, 0, -0.05), 5.9, 0.25, "SemillaClara", segmentos=48, segmentos_tubo=8)
n = 260
for i in range(n):                                                                                  # semillas en espiral de Fibonacci
    r = 5.6 * math.sqrt((i + 0.5) / n)
    a = i * 2.399963
    p.esfera((r * math.cos(a), r * math.sin(a), 0.0), (0.2, 0.2, 0.1), "SemillaClara" if i % 3 == 0 else "Semilla", segmentos=8, anillos=4)
p.esfera((0, 0, -1.6), (5.2, 5.2, 1.6), "Caliz", segmentos=36, anillos=12)                       # cáliz por detrás
for k in range(18):                                                                                 # sépalos verdes
    a = math.radians(k * 20 + 10)
    p.esfera((6.0 * math.cos(a), 6.0 * math.sin(a), -1.2), (0.8, 0.35, 0.1), "Caliz", rotacion=(0, 0, k * 20 + 10), segmentos=12, anillos=6)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Abejorro con cámara de tele (el cuerpo) y su ala (Unity pone dos y las agita)
# ---------------------------------------------------------------------------

p = Pieza("Abejorro")
p.esfera((0, 0.2, 0), (0.8, 1.1, 0.8), "AbejorroAmarillo", segmentos=24, anillos=12)
for k in range(3):                                                                                  # rayas
    y = -0.1 + k * 0.4
    p.cilindro((0, y, 0), 0.82 - abs(y - 0.2) * 0.2, 0.15, "AbejorroNegro", rotacion=(90, 0, 0), segmentos=24)
p.esfera((0, -0.95, 0.15), (0.55, 0.5, 0.5), "AbejorroNegro", segmentos=20, anillos=10)          # cabeza
for s in (-1, 1):
    p.esfera((s * 0.25, -1.3, 0.3), (0.16, 0.1, 0.18), "OjoBlanco", segmentos=10, anillos=6)
    p.esfera((s * 0.25, -1.38, 0.28), (0.08, 0.05, 0.09), "PupilaNegra", segmentos=8, anillos=5)
    p.tubo((s * 0.2, -1.1, 0.55), (s * 0.4, -1.45, 0.95), 0.03, "AbejorroNegro", segmentos=6)
p.caja((0, -1.25, -0.45), (0.5, 0.55, 0.4), "CamaraCuerpo")                                       # cámara colgada
p.cilindro((0, -1.6, -0.45), 0.16, 0.25, "CamaraCuerpo", rotacion=(90, 0, 0), segmentos=14)
p.cilindro((0, -1.73, -0.45), 0.13, 0.02, "Lente", rotacion=(90, 0, 0), segmentos=14)
p.esfera((0.2, -1.2, -0.2), (0.05, 0.05, 0.05), "LuzRoja", segmentos=8, anillos=5)
p.cilindro((0, 1.35, 0), 0.06, 0.3, "AbejorroNegro", rotacion=(90, 0, 0), radio_arriba=0.0, segmentos=8)  # aguijón
objetos.append(p.crear(bisel=0.0))

p = Pieza("AbejorroAla")                                                                            # sale hacia +X desde el origen
p.esfera((0.65, 0.1, 0), (0.7, 0.35, 0.03), "AlaCristal", segmentos=16, anillos=6)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Ventilador del plató (sopla hacia -Y) y sus aspas
# ---------------------------------------------------------------------------

p = Pieza("Ventilador")
p.cilindro((0, 0, 0.1), 0.9, 0.2, "VentiladorCuerpo", segmentos=24)
p.tubo((0, 0, 0.2), (0, 0, 2.2), 0.12, "VentiladorCuerpo", segmentos=10)
p.cilindro((0, 0.35, 2.8), 0.45, 0.6, "VentiladorCuerpo", rotacion=(90, 0, 0), segmentos=18)     # motor
for k in range(2):
    p.toro((0, -0.05 - k * 0.3, 2.8), 1.35, 0.05, "VentiladorRejilla", rotacion=(90, 0, 0), segmentos=32, segmentos_tubo=6)
for k in range(8):                                                                                  # rejilla
    a = math.radians(k * 22.5)
    p.caja((0, -0.2, 2.8), (2.7, 0.03, 0.05), "VentiladorRejilla", rotacion=(0, k * 22.5, 0))
objetos.append(p.crear(bisel=0.0))

p = Pieza("AspasVentilador")                                                                        # giran sobre Y (Unity: sobre Z)
p.cilindro((0, 0, 0), 0.25, 0.2, "Metal", rotacion=(90, 0, 0), segmentos=14)
for k in range(4):
    a = math.radians(k * 90)
    p.esfera((0.7 * math.cos(a), 0, 0.7 * math.sin(a)), (0.55, 0.05, 0.28), "Equipo", rotacion=(0, -k * 90, 0), segmentos=14, anillos=6)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Invernadero: media costilla de hierro (radio 1, Unity la escala) y banderola colgante
# ---------------------------------------------------------------------------

p = Pieza("ArcoInvernadero")
p.toro((0, 0, 0), 1.0, 0.008, "HierroVerde", rotacion=(90, 0, 0), segmentos=64, segmentos_tubo=6, arco=180)
for k in range(1, 12):                                                                              # remaches/travesaños
    a = math.pi * k / 12
    p.esfera((math.cos(a), 0, math.sin(a)), (0.012, 0.012, 0.012), "HierroVerde", segmentos=6, anillos=4)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Banderola")                                                                              # 2 × 4, cuelga del travesaño
p.tubo((-1.2, 0, 4.2), (1.2, 0, 4.2), 0.06, "Oro", segmentos=8)
p.caja((0, 0, 2.3), (2.0, 0.05, 3.6), "Equipo")
p.cilindro((0, 0, 0.5), 1.0, 0.05, "Equipo", rotacion=(90, 0, 0), segmentos=3)                   # pico de abajo
p.esfera((0, -0.05, 2.6), (0.55, 0.03, 0.55), "Estrella", segmentos=5, anillos=4)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Exportar y vistas
# ---------------------------------------------------------------------------

exportar(objetos, RUTA_FBX, RUTA_BLEND)

vistas(objetos, CARPETA_VISTAS, "kit_girasol", [
    ("bichos", ["Mariquita", "BolaPolen", "Abejorro", "AbejorroAla"], 1.0, (0, -11, 4), (0, 0, 0.8)),
    ("hojas", ["HojaRedonda", "Petalo", "HojaTallo", "Ventilador"], 1.0, (0, -16, 8), (0, 0, 1.0)),
    ("cabeza", ["CabezaGirasol", "HojaLarga"], 2.0, (0, -26, 14), (0, 0, 0)),
    ("show", ["Banderola", "AspasVentilador", "ArcoInvernadero"], 1.0, (0, -9, 3), (0, 0, 1.5)),
], color_fondo=(0.95, 0.8, 0.65), material_suelo="Caliz")

print("Kit del girasol listo.")
