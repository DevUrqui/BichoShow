"""
Bicho Show — kit del plató de tele (Gran final y menú) para Unity.

Uso (sin abrir Blender):
    blender -b --factory-startup -P crear_kit_plato.py -- <salida.fbx> <salida.blend> [carpeta_vistas]

Mismas reglas que el resto de kits (ver kit_comun.py): origen en el centro de la base,
lo que mira a -Y aquí mira a +Z en Unity.
- Grada: el público lo pone Unity encima (pieza "Publico"/"PublicoBrazos", con el color en
  el material "Equipo"). Asiento de cada fila i: Unity local (x, 0.5·(i+1), 1.55 − 1.1·i).
- Grada, TorreFocos y Pantalla tienen estructura por debajo del origen (el plató flota).
- Guante: el puño mira a -Y; Unity lo gira hacia donde empuja.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from kit_comun import Pieza, argumentos, crear_materiales, exportar, vistas  # noqa: E402

RUTA_FBX, RUTA_BLEND, CARPETA_VISTAS = argumentos()

COLORES = {
    # Compartidos con otros kits (mismo color)
    "Blanco": (0.95, 0.93, 0.88), "Metal": (0.55, 0.57, 0.60), "Equipo": (0.62, 0.62, 0.66),
    "Luz": (1.00, 0.88, 0.55), "OjoBlanco": (0.98, 0.98, 0.98), "PupilaNegra": (0.07, 0.07, 0.09),
    # Plató
    "GradaA": (0.42, 0.28, 0.62), "GradaB": (0.30, 0.19, 0.48), "Asiento": (0.95, 0.35, 0.45),
    "Oro": (1.00, 0.80, 0.22), "Boca": (0.35, 0.08, 0.12), "Truss": (0.78, 0.79, 0.84),
    "FocoCarcasa": (0.13, 0.12, 0.16), "MarcoPantalla": (0.18, 0.13, 0.27), "PantallaPlato": (0.38, 0.26, 0.80),
    "CamaraCuerpo": (0.24, 0.24, 0.28), "Lente": (0.25, 0.45, 0.70), "LuzRoja": (1.00, 0.15, 0.15),
    "Pedestal": (0.92, 0.90, 0.98), "Altavoz": (0.10, 0.10, 0.12), "ConoAltavoz": (0.38, 0.38, 0.42),
    "GuanteRojo": (0.92, 0.18, 0.20), "Peligro": (1.00, 0.82, 0.15), "PeligroNegro": (0.10, 0.10, 0.10),
    "Espuma": (0.30, 0.85, 0.75), "RosaPlato": (1.00, 0.45, 0.75),
    "BombaNegra": (0.13, 0.12, 0.16), "Mecha": (0.78, 0.62, 0.40), "Chispa": (1.00, 0.62, 0.15), "Seta": (0.95, 0.25, 0.30),
    "GemaRoja": (0.95, 0.12, 0.25), "GemaAzul": (0.2, 0.45, 1.0), "Cera": (1.0, 0.86, 0.9), "CeraRaya": (0.98, 0.55, 0.7),
    "Llama": (1.0, 0.7, 0.2), "Hierro": (0.32, 0.33, 0.38), "PianoNegro": (0.1, 0.09, 0.12),
}

crear_materiales(COLORES, brillantes=("Oro", "OjoBlanco", "PupilaNegra", "Lente", "GuanteRojo", "Metal", "BombaNegra", "Seta", "GemaRoja", "GemaAzul", "PianoNegro"),
                 emisivos=("Luz", "LuzRoja", "PantallaPlato", "Chispa", "Llama"))

objetos = []

# ---------------------------------------------------------------------------
# Grada de público: 8 m de ancho, 4 filas que suben hacia atrás (+Y), estructura debajo
# ---------------------------------------------------------------------------

p = Pieza("Grada")
p.caja((0, 0.25, -2.0), (8.0, 4.9, 4.0), "GradaB")                               # estructura
for i in range(4):
    y = -1.65 + 1.1 * i
    alto = 0.5 * (i + 1)
    p.caja((0, y, alto / 2), (8.0, 1.1, alto), "GradaA" if i % 2 == 0 else "GradaB")   # escalón
    p.caja((0, y + 0.3, alto + 0.05), (7.8, 0.45, 0.1), "Asiento")                    # banco
    for s in (-1, 1):
        p.caja((s * 4.05, y, (alto + 0.35) / 2), (0.14, 1.1, alto + 0.35), "Oro")       # laterales
p.caja((0, 2.6, 1.75), (8.0, 0.2, 3.5), "GradaA")                                 # pared del fondo
for k in range(9):
    p.esfera((-3.6 + 0.9 * k, 2.45, 3.4), (0.13, 0.13, 0.13), "Luz", segmentos=10, anillos=6)
for k in range(9):
    x = -3.9 + 7.8 * k / 8
    p.tubo((x, -2.15, 0.0), (x, -2.15, 0.85), 0.04, "Oro", segmentos=6)               # barandilla
p.caja((0, -2.15, 0.87), (8.0, 0.08, 0.08), "Oro")
objetos.append(p.crear(bisel=0.04))


def publico(nombre, brazos_arriba):
    p = Pieza(nombre)
    p.esfera((0, 0, 0.42), (0.3, 0.26, 0.42), "Equipo", segmentos=16, anillos=10)      # cuerpo
    for s in (-1, 1):
        p.esfera((s * 0.11, -0.2, 0.6), (0.085, 0.06, 0.1), "OjoBlanco", segmentos=10, anillos=6)
        p.esfera((s * 0.11, -0.25, 0.6), (0.042, 0.03, 0.055), "PupilaNegra", segmentos=8, anillos=5)
        if brazos_arriba:
            p.tubo((s * 0.24, 0, 0.5), (s * 0.4, -0.05, 0.95), 0.05, "Equipo", segmentos=8)
            p.esfera((s * 0.42, -0.05, 1.0), (0.08, 0.08, 0.08), "Equipo", segmentos=8, anillos=5)
        else:
            p.esfera((s * 0.29, -0.05, 0.32), (0.07, 0.07, 0.12), "Equipo", segmentos=8, anillos=5)
    p.esfera((0, -0.24, 0.43), (0.09, 0.03, 0.06 if not brazos_arriba else 0.09), "Boca", segmentos=10, anillos=6)
    return p.crear(bisel=0.0)


objetos.append(publico("Publico", False))
objetos.append(publico("PublicoBrazos", True))

# ---------------------------------------------------------------------------
# Torre de focos (truss), con patas que bajan por debajo del plató
# ---------------------------------------------------------------------------

p = Pieza("TorreFocos")
h = 0.3
for sx in (-1, 1):
    for sy in (-1, 1):
        p.tubo((sx * h, sy * h, -4.0), (sx * h, sy * h, 7.0), 0.045, "Truss", segmentos=6)
for k in range(-4, 7):
    z0, z1 = k, k + 1
    for (ax, ay), (bx, by) in (((-1, -1), (1, -1)), ((1, -1), (1, 1)), ((1, 1), (-1, 1)), ((-1, 1), (-1, -1))):
        if k % 2 == 0:
            p.tubo((ax * h, ay * h, z0), (bx * h, by * h, z1), 0.025, "Truss", segmentos=5)
        else:
            p.tubo((bx * h, by * h, z0), (ax * h, ay * h, z1), 0.025, "Truss", segmentos=5)
p.caja((0, 0, 0.05), (1.0, 1.0, 0.1), "FocoCarcasa")
p.caja((0, 0, 7.1), (2.8, 0.35, 0.25), "Truss")
for x in (-1.1, 0, 1.1):
    p.cilindro((x, -0.35, 6.75), 0.24, 0.6, "FocoCarcasa", rotacion=(-60, 0, 0), segmentos=14)   # foco que mira abajo-delante
    p.cilindro((x, -0.62, 6.6), 0.2, 0.05, "Luz", rotacion=(-60, 0, 0), segmentos=14)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Pantalla gigante con bombillas alrededor (origen = borde de abajo de la pantalla)
# ---------------------------------------------------------------------------

p = Pieza("Pantalla")
p.caja((0, 0, 4.6), (16.8, 0.6, 9.4), "MarcoPantalla")
p.caja((0, -0.31, 4.6), (16.0, 0.05, 8.6), "PantallaPlato")
for k in range(17):
    x = -8.0 + k
    for z in (-0.1, 9.3):
        p.esfera((x, -0.35, z), (0.14, 0.14, 0.14), "Luz", segmentos=10, anillos=6)
for k in range(9):
    for s in (-1, 1):
        p.esfera((s * 8.4, -0.35, 0.4 + k * 1.05), (0.14, 0.14, 0.14), "Luz", segmentos=10, anillos=6)
for s in (-1, 1):
    p.caja((s * 6.0, 0.6, -5.0), (0.8, 0.6, 10.0), "MarcoPantalla")                 # patas hacia abajo
objetos.append(p.crear(bisel=0.08))

# ---------------------------------------------------------------------------
# Cámara de tele en trípode (el objetivo mira a -Y)
# ---------------------------------------------------------------------------

p = Pieza("CamaraTV")
for k in range(3):
    a = math.radians(90 + 120 * k)
    p.tubo((0.55 * math.cos(a), 0.55 * math.sin(a), 0), (0, 0, 1.15), 0.035, "Metal", segmentos=6)
p.cilindro((0, 0, 1.2), 0.12, 0.12, "CamaraCuerpo", segmentos=12)
p.caja((0, 0.05, 1.45), (0.42, 0.75, 0.42), "CamaraCuerpo")
p.cilindro((0, -0.45, 1.45), 0.17, 0.3, "CamaraCuerpo", rotacion=(90, 0, 0), segmentos=16)
p.cilindro((0, -0.61, 1.45), 0.14, 0.03, "Lente", rotacion=(90, 0, 0), segmentos=16)
p.caja((0.27, 0.15, 1.55), (0.12, 0.3, 0.22), "CamaraCuerpo")                       # visor
p.esfera((0, -0.2, 1.7), (0.05, 0.05, 0.05), "LuzRoja", segmentos=8, anillos=5)      # piloto "en directo"
p.tubo((-0.1, 0.4, 1.4), (-0.35, 0.95, 1.2), 0.025, "Metal", segmentos=6)            # mango
objetos.append(p.crear(bisel=0.02))

# ---------------------------------------------------------------------------
# Meta: arco hinchable (color del equipo), trofeo y cañón de confeti
# ---------------------------------------------------------------------------

p = Pieza("ArcoMeta")
# Medio rosco de pie (plano XZ) con gajos de color; las patas se meten un poco en el suelo.
p.toro((0, 0, -0.2), 4.7, 0.5, "Equipo", rotacion=(90, 0, 0), segmentos=36, segmentos_tubo=14, arco=180,
       rayas=("Equipo", "Blanco", 18))
for s in (-1, 1):
    p.cilindro((s * 4.7, 0, 0.05), 0.6, 0.1, "Blanco", segmentos=16)                 # pies
p.caja((0, -0.45, 4.15), (3.4, 0.2, 1.0), "Blanco")                                  # cartel de META
p.caja((0, -0.56, 4.15), (3.0, 0.02, 0.7), "Equipo")
objetos.append(p.crear(bisel=0.02))

p = Pieza("Trofeo")
p.caja((0, 0, 0.3), (1.2, 1.2, 0.6), "Pedestal")
p.caja((0, 0, 0.65), (0.8, 0.8, 0.1), "Oro")
p.cilindro((0, 0, 0.85), 0.12, 0.3, "Oro", radio_arriba=0.08, segmentos=14)
p.esfera((0, 0, 1.25), (0.45, 0.45, 0.4), "Oro", segmentos=20, anillos=10)
p.cilindro((0, 0, 1.55), 0.45, 0.5, "Oro", radio_arriba=0.55, segmentos=20)
for s in (-1, 1):
    p.toro((s * 0.52, 0, 1.4), 0.2, 0.05, "Oro", rotacion=(90, 0, 0), segmentos=16, segmentos_tubo=6)
p.esfera((0, 0, 2.0), (0.12, 0.12, 0.12), "Oro", segmentos=10, anillos=6)
objetos.append(p.crear(bisel=0.02))

p = Pieza("CanonConfeti")
p.caja((0, 0, 0.15), (0.8, 0.8, 0.3), "FocoCarcasa")
p.cilindro((0, -0.15, 0.85), 0.24, 1.3, "RosaPlato", radio_arriba=0.3, rotacion=(-20, 0, 0), segmentos=16,
           rayas=("RosaPlato", "Oro", 8))
p.toro((0, -0.37, 1.47), 0.29, 0.05, "Oro", rotacion=(-20, 0, 0), segmentos=18, segmentos_tubo=6)
objetos.append(p.crear(bisel=0.02))

p = Pieza("Altavoz")
p.caja((0, 0, 0.8), (1.0, 0.8, 1.6), "Altavoz")
p.cilindro((0, -0.41, 0.5), 0.33, 0.04, "ConoAltavoz", rotacion=(90, 0, 0), segmentos=18)
p.cilindro((0, -0.41, 1.2), 0.2, 0.04, "ConoAltavoz", rotacion=(90, 0, 0), segmentos=18)
for z, r in ((0.5, 0.12), (1.2, 0.07)):
    p.esfera((0, -0.43, z), (r, 0.05, r), "Metal", segmentos=10, anillos=6)
objetos.append(p.crear(bisel=0.04))

# ---------------------------------------------------------------------------
# Trampas: guante de boxeo con pistón (empujador) y barredora de espuma
# El primer material es el que destella como aviso.
# ---------------------------------------------------------------------------

p = Pieza("Guante")
p.esfera((0, -0.35, 0.7), (0.72, 0.75, 0.68), "GuanteRojo", segmentos=22, anillos=12)       # puño
p.esfera((0.55, -0.45, 0.55), (0.25, 0.35, 0.22), "GuanteRojo", rotacion=(0, 0, -20))       # pulgar
p.cilindro((0, 0.45, 0.7), 0.52, 0.45, "Blanco", rotacion=(90, 0, 0), segmentos=20)          # puño de tela
p.tubo((0, 0.65, 0.7), (0, 1.5, 0.7), 0.16, "Metal", segmentos=12)                           # pistón
p.cilindro((0, 1.55, 0.7), 0.32, 0.25, "FocoCarcasa", rotacion=(90, 0, 0), segmentos=16)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Barredora")
for k in range(7):
    x0 = -3.5 + k
    p.tubo((x0, 0, 0.55), (x0 + 1, 0, 0.55), 0.24, "Espuma" if k % 2 == 0 else "Blanco", segmentos=14)
for s in (-1, 1):
    p.esfera((s * 3.5, 0, 0.55), (0.32, 0.32, 0.32), "Espuma", segmentos=14, anillos=8)
p.cilindro((0, 0, 0.6), 0.4, 1.2, "Peligro", segmentos=16, rayas=("Peligro", "PeligroNegro", 8))
p.cilindro((0, 0, 1.25), 0.45, 0.1, "Metal", segmentos=16)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Patata caliente: bomba de dibujos (origen en el centro de la bola) y seta rebotadora
# ---------------------------------------------------------------------------

p = Pieza("Bomba")
p.esfera((0, 0, 0), (0.34, 0.34, 0.34), "BombaNegra", segmentos=28, anillos=14)
p.esfera((-0.12, -0.2, 0.16), (0.08, 0.05, 0.06), "OjoBlanco", segmentos=12, anillos=6)           # brillo
p.cilindro((0, 0, 0.36), 0.11, 0.1, "Metal", segmentos=16)                                         # tapón
puntos_mecha = [(0, 0, 0.4), (0.03, 0, 0.5), (0.1, 0, 0.58), (0.18, 0, 0.6)]
for i in range(len(puntos_mecha) - 1):
    p.tubo(puntos_mecha[i], puntos_mecha[i + 1], 0.025, "Mecha", segmentos=8)
p.esfera(puntos_mecha[-1], (0.06, 0.06, 0.06), "Chispa", segmentos=12, anillos=6)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Seta")
p.cilindro((0, 0, 0.35), 0.32, 0.7, "Pedestal", radio_arriba=0.26, segmentos=20)                 # tallo
p.esfera((0, 0, 0.78), (1.0, 1.0, 0.45), "Seta", segmentos=32, anillos=14)                        # sombrero
for a, r in ((0, 0.6), (120, 0.6), (240, 0.6), (60, 0.2), (200, 0.32), (300, 0.8), (160, 0.82)):
    z = 0.78 + 0.45 * math.sqrt(max(0.0, 1 - r * r)) - 0.015  # sobre la superficie del sombrero
    ang = math.radians(a)
    p.esfera((r * math.cos(ang), r * math.sin(ang), z), (0.13, 0.13, 0.06), "Pedestal", segmentos=14, anillos=6)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# La Corona, Lluvia de muebles y Pintabichos
# ---------------------------------------------------------------------------

p = Pieza("Corona")
p.toro((0, 0, 0.1), 0.34, 0.09, "Oro", segmentos=28, segmentos_tubo=10)                        # aro de abajo
p.cilindro((0, 0, 0.22), 0.36, 0.26, "Oro", radio_arriba=0.4, segmentos=28)                      # banda
for i in range(5):
    a = 2 * math.pi * i / 5
    x, y = 0.36 * math.cos(a), 0.36 * math.sin(a)
    p.cilindro((x, y, 0.47), 0.1, 0.32, "Oro", radio_arriba=0.02, segmentos=10)                 # pico
    p.esfera((x * 1.05, y * 1.05, 0.66), (0.06, 0.06, 0.06), "Oro", segmentos=10, anillos=6)
    p.esfera((x * 1.07, y * 1.07, 0.24), (0.07, 0.04, 0.07), "GemaRoja" if i % 2 == 0 else "GemaAzul",
             rotacion=(90, 0, math.degrees(a) + 90), segmentos=12, anillos=6)                      # joya
objetos.append(p.crear(bisel=0.0))

p = Pieza("Vela")
p.cilindro((0, 0, 1.2), 0.38, 2.4, "Cera", segmentos=24, rayas=("Cera", "CeraRaya", 10))
p.esfera((0, 0, 2.4), (0.38, 0.38, 0.08), "Cera", segmentos=24, anillos=8)                        # borde derretido
for a in (20, 110, 230, 300):
    r = math.radians(a)
    p.esfera((0.36 * math.cos(r), 0.36 * math.sin(r), 2.2), (0.08, 0.08, 0.25), "Cera", segmentos=10, anillos=6)  # gotas
p.tubo((0, 0, 2.42), (0, 0, 2.6), 0.025, "PupilaNegra", segmentos=6)                              # mecha
p.esfera((0, 0, 2.82), (0.13, 0.13, 0.24), "Llama", segmentos=14, anillos=8)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Yunque")
p.caja((0, 0, 0.12), (0.7, 0.45, 0.24), "Hierro")
p.caja((0, 0, 0.36), (0.36, 0.28, 0.26), "Hierro")
p.caja((0.1, 0, 0.62), (1.0, 0.42, 0.28), "Hierro")
p.cilindro((-0.62, 0, 0.66), 0.17, 0.5, "Hierro", radio_arriba=0.01, rotacion=(0, -90, 0), segmentos=16)  # cuerno
p.caja((0.1, 0, 0.765), (1.0, 0.42, 0.01), "Metal")
objetos.append(p.crear(bisel=0.03))

p = Pieza("Piano")
p.caja((0, 0, 0.75), (1.7, 1.1, 0.36), "PianoNegro")                                              # caja
p.caja((0, -0.62, 0.62), (1.5, 0.22, 0.1), "Blanco")                                              # teclas
for k in range(10):
    p.caja((-0.62 + k * 0.14, -0.66, 0.68), (0.06, 0.12, 0.05), "PianoNegro")
p.caja((0.1, 0.25, 1.25), (1.6, 0.95, 0.04), "PianoNegro", rotacion=(-30, 0, 0))                # tapa abierta
p.tubo((0.6, 0.55, 0.95), (0.6, 0.45, 1.5), 0.02, "Oro", segmentos=6)                              # varilla
for x, y in ((-0.7, -0.4), (0.7, -0.4), (0.0, 0.45)):
    p.cilindro((x, y, 0.3), 0.07, 0.6, "PianoNegro", radio_arriba=0.05, segmentos=10)
    p.esfera((x, y, 0.04), (0.08, 0.08, 0.04), "Oro", segmentos=10, anillos=6)
objetos.append(p.crear(bisel=0.04))

p = Pieza("BotePintura")
p.cilindro((0, 0, 0.25), 0.3, 0.5, "Blanco", segmentos=24)
p.cilindro((0, 0, 0.52), 0.31, 0.05, "Metal", segmentos=24)                                       # tapa
p.toro((0, 0, 0.6), 0.16, 0.02, "Metal", rotacion=(90, 0, 0), segmentos=16, segmentos_tubo=6, arco=180)  # asa
for a, largo, mat in ((0, 0.28, "RosaPlato"), (70, 0.18, "Oro"), (150, 0.32, "Espuma"), (230, 0.22, "RosaPlato"), (300, 0.26, "Oro")):
    r = math.radians(a)
    p.esfera((0.3 * math.cos(r), 0.3 * math.sin(r), 0.5 - largo * 0.5), (0.06, 0.06, largo * 0.55), mat, segmentos=10, anillos=6)  # chorretones
p.cilindro((0, 0, 0.25), 0.302, 0.18, "Oro", segmentos=24, rayas=("RosaPlato", "Espuma", 6))      # etiqueta
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Exportar y vistas
# ---------------------------------------------------------------------------

exportar(objetos, RUTA_FBX, RUTA_BLEND)

vistas(objetos, CARPETA_VISTAS, "kit_plato", [
    ("gradas", ["TorreFocos", "Grada", "Publico", "PublicoBrazos", "CamaraTV", "Altavoz"], 0.8, (0, -30, 10), (0, 0, 2.5)),
    ("meta", ["ArcoMeta", "Trofeo", "CanonConfeti", "Guante", "Barredora"], 0.8, (0, -26, 7), (0, 0, 2.0)),
    ("patata", ["Bomba", "Seta"], 0.8, (0, -8, 3), (0, 0, 0.6)),
    ("fiesta", ["Corona", "Vela", "Yunque", "Piano", "BotePintura"], 0.8, (0, -12, 4), (0, 0, 1.0)),
    ("pantalla", ["Pantalla"], 0.8, (0, -26, 7), (0, 0, 4.5)),
], color_fondo=(0.30, 0.20, 0.45), material_suelo="GradaB")

print("Kit del plató listo.")
