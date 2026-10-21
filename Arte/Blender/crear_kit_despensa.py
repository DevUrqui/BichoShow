"""
Bicho Show — kit de "Despensa Deslizante" (carrera por la encimera de la cocina) para Unity.

Uso (sin abrir Blender):
    blender -b --factory-startup -P crear_kit_despensa.py -- <salida.fbx> <salida.blend> [carpeta_vistas]

Mismas reglas que el resto de kits (ver kit_comun.py): lo que mira a -Y aquí mira a +Z en Unity,
Unity (x, y, z) = (-X, Z, -Y). "Equipo" lo pinta Unity (el tapón, el arco, la tapa del colchón…).
Orígenes:
- Rodillo, Tomate, Arandano, BombillaCalor: en su centro (ruedan, giran o se encienden).
- Esponja, Tapon: centro de la cara de arriba (Z = 0); flotan y se hunden.
- GrifoCano: en el eje del grifo, arriba; el caño va hacia -Y (Unity +Z) y gotea por la punta.
- GruaBrazo: en el pivote de arriba de la columna; la cámara mira hacia -Y.
- Todo lo demás: centro de la base.
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
    "Blanco": (0.95, 0.93, 0.88), "Negro": (0.08, 0.07, 0.1), "Cristal": (0.60, 0.82, 0.95),
    "CamaraCuerpo": (0.24, 0.24, 0.28), "Lente": (0.25, 0.45, 0.70), "LuzRoja": (1.00, 0.15, 0.15),
    # Despensa
    "MaderaRodillo": (0.92, 0.7, 0.45), "MaderaMango": (0.72, 0.46, 0.26), "Harina": (0.98, 0.96, 0.9),
    "Sal": (0.97, 0.97, 0.99),
    "EsponjaAmarilla": (1.0, 0.86, 0.28), "EsponjaPoro": (0.88, 0.66, 0.14), "EsponjaVerde": (0.35, 0.72, 0.35),
    "TaponInterior": (0.92, 0.92, 0.95),
    "Cromo": (0.82, 0.86, 0.92), "LlaveRoja": (0.95, 0.25, 0.25), "LlaveAzul": (0.25, 0.5, 0.95),
    "TomateRojo": (0.9, 0.12, 0.08), "TomateHoja": (0.25, 0.6, 0.18), "TomateBrillo": (1.0, 0.55, 0.5),
    "Arandano": (0.22, 0.28, 0.62), "ArandanoCorona": (0.14, 0.14, 0.32), "ArandanoPolvo": (0.5, 0.58, 0.85),
    "MoradoAcolchado": (0.55, 0.3, 0.8), "BotonMorado": (0.4, 0.18, 0.6),
    "ColchonAmarillo": (1.0, 0.82, 0.3), "ColchonNaranja": (1.0, 0.55, 0.2),
    "Bombilla": (1.0, 0.85, 0.5), "LamparaRoja": (0.88, 0.22, 0.18),
    "PantallaCocina": (0.35, 0.3, 0.85), "OroOscuro": (0.8, 0.55, 0.12),
    "Tostadora": (0.92, 0.88, 0.8), "Ranura": (0.18, 0.16, 0.16), "MaderaTapa": (0.78, 0.56, 0.34),
    "EspumaJabon": (1.0, 0.88, 0.95),
}

crear_materiales(COLORES,
                 brillantes=("Oro", "Metal", "Cristal", "Lente", "Cromo", "TomateRojo", "TomateBrillo", "Arandano",
                             "Tostadora", "EspumaJabon"),
                 emisivos=("LuzRoja", "Bombilla", "PantallaCocina"))

objetos = []

# ---------------------------------------------------------------------------
# Zona 1: rodillo de amasar y salero
# ---------------------------------------------------------------------------

# Rodillo (eje a lo largo de X): cuerpo gordo de 11 m (radio 0,9) y mangos finos (radio 0,28).
p = Pieza("Rodillo")
p.cilindro((0, 0, 0), 0.9, 11.0, "MaderaRodillo", rotacion=(0, 90, 0), segmentos=32)
for s in (-1, 1):
    p.toro((s * 5.45, 0, 0), 0.82, 0.1, "MaderaMango", rotacion=(0, 90, 0), segmentos=32, segmentos_tubo=8)   # filo
    p.cilindro((s * 6.6, 0, 0), 0.28, 2.3, "MaderaMango", rotacion=(0, 90, 0), segmentos=16)               # mango
    p.esfera((s * 7.85, 0, 0), (0.38, 0.38, 0.38), "MaderaMango", segmentos=16, anillos=8)                 # pomo
for x, y, z, r in ((-2.5, 0.2, 0.85, 0.35), (1.0, -0.4, 0.8, 0.45), (3.4, 0.5, 0.75, 0.3), (-0.6, 0.6, -0.7, 0.3)):
    p.esfera((x, y, z), (r, r * 0.7, 0.06), "Harina", segmentos=10, anillos=4)                               # harina
objetos.append(p.crear(bisel=0.0))

# Salero de cristal (radio 1, alto 3,2) con la sal dentro y la tapa de metal agujereada.
p = Pieza("Salero")
p.cilindro((0, 0, 1.3), 1.0, 2.6, "Cristal", segmentos=28)
p.cilindro((0, 0, 0.75), 0.92, 1.4, "Sal", segmentos=28)
p.cilindro((0, 0, 2.75), 1.04, 0.3, "Metal", segmentos=28)
p.esfera((0, 0, 2.9), (1.0, 1.0, 0.45), "Metal", segmentos=28, anillos=10)
for k in range(7):
    a = k * 2 * math.pi / 7
    r = 0 if k == 0 else 0.45
    p.esfera((r * math.cos(a), r * math.sin(a), 3.33), (0.09, 0.09, 0.04), "Negro", segmentos=8, anillos=4)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Zona 2: esponja, tapón y grifo
# ---------------------------------------------------------------------------

# Esponja (6 de ancho × 11 de largo × 1,2 de alto), con la capa verde de frotar abajo y poros.
p = Pieza("Esponja")
p.caja((0, 0, -0.45), (6.0, 11.0, 0.9), "EsponjaAmarilla")
p.caja((0, 0, -1.05), (6.0, 11.0, 0.3), "EsponjaVerde")
for i in range(26):
    x = -2.5 + (i * 1.37) % 5.0
    y = -5.0 + (i * 2.71) % 10.0
    r = 0.12 + (i % 3) * 0.06
    p.esfera((x, y, 0.0), (r, r, 0.05), "EsponjaPoro", segmentos=10, anillos=4)
objetos.append(p.crear(bisel=0.15))

# Tapón de botella flotando boca abajo (radio 1,4, alto 0,5): estrías y un aro interior.
p = Pieza("Tapon")
p.cilindro((0, 0, -0.25), 1.4, 0.5, "Equipo", segmentos=32, rayas=("Equipo", "TaponInterior", 32))
p.cilindro((0, 0, -0.01), 1.32, 0.03, "Equipo", segmentos=32)
p.toro((0, 0, 0.0), 1.0, 0.05, "TaponInterior", segmentos=32, segmentos_tubo=6)
objetos.append(p.crear(bisel=0.04))

# Grifo cromado gigante: base y columna (4,6 m) con las llaves de agua fría y caliente.
p = Pieza("GrifoBase")
p.cilindro((0, 0, 0.3), 1.1, 0.6, "Cromo", segmentos=28)
p.cilindro((0, 0, 2.6), 0.55, 4.2, "Cromo", segmentos=24)
p.esfera((0, 0, 4.7), (0.62, 0.62, 0.5), "Cromo", segmentos=24, anillos=10)
for s, mat in ((-1, "LlaveRoja"), (1, "LlaveAzul")):
    p.tubo((0, 0, 1.4), (s * 1.6, 0, 1.6), 0.18, "Cromo", segmentos=10)
    p.cilindro((s * 1.75, 0, 1.75), 0.45, 0.35, "Cromo", segmentos=16)
    p.esfera((s * 1.75, 0, 2.05), (0.25, 0.25, 0.15), mat, segmentos=12, anillos=6)
objetos.append(p.crear(bisel=0.0))

# Caño (gira): sale del eje hacia -Y, 16 m, y acaba en una boquilla que mira abajo.
p = Pieza("GrifoCano")
p.esfera((0, 0, 0), (0.6, 0.6, 0.6), "Cromo", segmentos=20, anillos=10)
p.tubo((0, 0, 0.1), (0, -2.5, 0.9), 0.38, "Cromo", segmentos=16)
p.tubo((0, -2.5, 0.9), (0, -14.5, 0.9), 0.38, "Cromo", segmentos=16)
p.tubo((0, -14.5, 0.9), (0, -16.0, 0.2), 0.38, "Cromo", segmentos=16)
p.cilindro((0, -16.0, -0.15), 0.55, 0.7, "Cromo", radio_arriba=0.45, segmentos=18)
p.cilindro((0, -16.0, -0.52), 0.42, 0.06, "Metal", segmentos=18)
objetos.append(p.crear(bisel=0.0))

# Grumo de espuma (para salpicar el agua del fregadero).
p = Pieza("Espuma")
for x, y, z, r in ((0, 0, 0.15, 0.6), (0.7, 0.2, 0.1, 0.45), (-0.6, 0.3, 0.1, 0.4), (0.2, -0.6, 0.05, 0.38), (0.3, 0.2, 0.55, 0.35), (-0.3, -0.3, 0.4, 0.3)):
    p.esfera((x, y, z), (r, r, r * 0.85), "EspumaJabon", segmentos=14, anillos=8)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Zona 3: tomate, arándano, muro de refugio y lámpara de calor
# ---------------------------------------------------------------------------

p = Pieza("Tomate")
p.esfera((0, 0, 0), (1.0, 1.0, 0.92), "TomateRojo", segmentos=28, anillos=14)
p.esfera((0.35, -0.45, 0.55), (0.18, 0.12, 0.1), "TomateBrillo", segmentos=10, anillos=5)
for k in range(5):
    a = k * 2 * math.pi / 5
    p.esfera((0.32 * math.cos(a), 0.32 * math.sin(a), 0.9), (0.34, 0.1, 0.05), "TomateHoja", rotacion=(0, 0, math.degrees(a)), segmentos=10, anillos=4)
p.tubo((0, 0, 0.88), (0.05, 0.05, 1.2), 0.07, "TomateHoja", segmentos=8)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Arandano")
p.esfera((0, 0, 0), (0.7, 0.7, 0.66), "Arandano", segmentos=24, anillos=12)
p.esfera((-0.2, -0.35, 0.3), (0.2, 0.15, 0.12), "ArandanoPolvo", segmentos=10, anillos=5)
p.toro((0, 0, 0.6), 0.16, 0.06, "ArandanoCorona", segmentos=16, segmentos_tubo=6)
for k in range(5):
    a = k * 2 * math.pi / 5
    p.esfera((0.18 * math.cos(a), 0.18 * math.sin(a), 0.66), (0.08, 0.05, 0.05), "ArandanoCorona", rotacion=(0, 0, math.degrees(a)), segmentos=8, anillos=4)
objetos.append(p.crear(bisel=0.0))

# Muro de refugio acolchado (3 de ancho × 1,6 de alto × 1,2 de fondo), con botones.
p = Pieza("MuroRefugio")
p.caja((0, 0, 0.8), (3.0, 1.2, 1.6), "MoradoAcolchado")
for x in (-0.9, 0.0, 0.9):
    for z in (0.5, 1.15):
        p.esfera((x, -0.6, z), (0.12, 0.06, 0.12), "BotonMorado", segmentos=10, anillos=5)
objetos.append(p.crear(bisel=0.3))

# Lámpara de calor: campana roja colgante (la bombilla va aparte, para encenderla).
p = Pieza("LamparaCalor")
p.cilindro((0, 0, 0.6), 1.6, 1.2, "LamparaRoja", radio_arriba=0.5, segmentos=28)
p.cilindro((0, 0, 1.4), 0.2, 0.6, "Metal", segmentos=12)
p.toro((0, 0, 0.02), 1.6, 0.08, "Metal", segmentos=32, segmentos_tubo=6)
objetos.append(p.crear(bisel=0.0))

p = Pieza("BombillaCalor")
p.esfera((0, 0, 0), (0.7, 0.7, 0.6), "Bombilla", segmentos=20, anillos=10)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Zona 4: colchón elástico, arcos, meta y podio
# ---------------------------------------------------------------------------

# Colchón de bandeja (6 × 6 × 0,8): marco morado y centro amarillo con un rombo naranja.
p = Pieza("Colchon")
p.caja((0, 0, 0.35), (6.0, 6.0, 0.7), "MoradoAcolchado")
p.caja((0, 0, 0.72), (4.8, 4.8, 0.16), "ColchonAmarillo")
p.caja((0, 0, 0.79), (2.6, 2.6, 0.04), "ColchonNaranja", rotacion=(0, 0, 45))
for x in (-2.7, 2.7):
    for y in (-2.7, 2.7):
        p.esfera((x, y, 0.72), (0.2, 0.2, 0.12), "BotonMorado", segmentos=10, anillos=5)
objetos.append(p.crear(bisel=0.25))

# Arco de control (18 m de luz, 6,5 de alto): postes con bombillas y un cartel (Equipo) arriba.
p = Pieza("ArcoControl")
for s in (-1, 1):
    p.caja((s * 9.2, 0, 3.25), (0.8, 0.8, 6.5), "Equipo")
    for k in range(6):
        p.esfera((s * 9.2, -0.42, 0.8 + k * 1.0), (0.16, 0.08, 0.16), "Bombilla", segmentos=10, anillos=5)
p.caja((0, 0, 6.3), (19.2, 0.9, 0.9), "Equipo")
p.caja((0, -0.1, 7.3), (8.0, 0.6, 1.6), "Blanco")
for k in range(16):
    p.esfera((-8.5 + k * 1.13, -0.48, 6.3), (0.15, 0.08, 0.15), "Bombilla", segmentos=10, anillos=5)
objetos.append(p.crear(bisel=0.05))

# Portal de meta: postes de focos y un marcador con pantallas y la corona.
p = Pieza("PortalMeta")
for s in (-1, 1):
    p.caja((s * 9.3, 0, 4.2), (1.0, 1.0, 8.4), "Negro")
    for k in range(8):
        p.esfera((s * 9.3, -0.52, 0.8 + k * 0.95), (0.18, 0.08, 0.18), "Bombilla", segmentos=10, anillos=5)
    p.caja((s * 6.0, -0.1, 8.6), (3.6, 0.5, 2.4), "PantallaCocina")
p.caja((0, 0, 8.6), (19.6, 1.0, 1.2), "Negro")
p.caja((0, -0.1, 9.6), (5.4, 0.9, 3.4), "Equipo")
p.caja((0, -0.4, 9.6), (4.6, 0.3, 2.6), "PantallaCocina")
# La corona del marcador (de luz).
p.caja((0, -0.6, 9.2), (2.2, 0.1, 0.5), "Oro")
for x in (-0.9, 0.0, 0.9):
    p.cilindro((x, -0.6, 9.75), 0.32, 0.8, "Oro", rotacion=(90, 0, 0), radio_arriba=0.32, segmentos=3)
objetos.append(p.crear(bisel=0.04))

# Línea de meta a cuadros (18 × 1,6), a ras del suelo.
p = Pieza("LineaMeta")
for i in range(18):
    for j in range(2):
        p.caja((-8.5 + i, -0.4 + j * 0.8, 0.02), (1.0, 0.8, 0.04), "Negro" if (i + j) % 2 else "Blanco")
objetos.append(p.crear(bisel=0.0))

# Podio dorado: 1º en el centro (1,5 m), 2º a su izquierda (1 m) y 3º a la derecha (0,7 m).
p = Pieza("Podio")
for x, alto in ((0.0, 1.5), (-2.3, 1.0), (2.3, 0.7)):
    p.caja((x, 0, alto / 2), (2.2, 2.2, alto), "OroOscuro")
    p.caja((x, 0, alto + 0.04), (2.1, 2.1, 0.08), "Oro")
    p.caja((x, -1.12, alto * 0.55), (0.9, 0.04, 0.6), "Blanco")
objetos.append(p.crear(bisel=0.06))

# ---------------------------------------------------------------------------
# Plató y cocina: grúa de cámara, tostadora y tarro
# ---------------------------------------------------------------------------

p = Pieza("GruaBase")
for k in range(3):
    a = k * 2 * math.pi / 3
    p.tubo((0, 0, 1.0), (1.6 * math.cos(a), 1.6 * math.sin(a), 0.1), 0.12, "Negro", segmentos=8)
    p.cilindro((1.6 * math.cos(a), 1.6 * math.sin(a), 0.15), 0.25, 0.3, "CamaraCuerpo", segmentos=12)
p.cilindro((0, 0, 2.6), 0.25, 3.4, "Negro", segmentos=14)
p.cilindro((0, 0, 4.4), 0.45, 0.3, "Metal", segmentos=16)
objetos.append(p.crear(bisel=0.0))

p = Pieza("GruaBrazo")
p.tubo((0, 3.0, 0.3), (0, -8.0, 1.6), 0.18, "Negro", segmentos=10)
p.caja((0, 3.4, 0.2), (0.9, 1.0, 0.9), "CamaraCuerpo")                                            # contrapeso
p.caja((0, -8.3, 1.3), (0.8, 1.4, 0.9), "CamaraCuerpo")                                           # cámara
p.cilindro((0, -9.15, 1.3), 0.35, 0.5, "Negro", rotacion=(90, 0, 0), segmentos=16)
p.cilindro((0, -9.42, 1.3), 0.3, 0.06, "Lente", rotacion=(90, 0, 0), segmentos=16)
p.esfera((0.3, -7.7, 1.85), (0.08, 0.08, 0.08), "LuzRoja", segmentos=8, anillos=4)
objetos.append(p.crear(bisel=0.02))

p = Pieza("Tostadora")
p.caja((0, 0, 2.2), (6.0, 3.4, 4.4), "Tostadora")
for x in (-1.3, 1.3):
    p.caja((x, 0, 4.42), (1.6, 2.4, 0.06), "Ranura")
p.caja((3.05, 0, 3.0), (0.3, 0.8, 0.5), "Negro")
p.cilindro((0, -1.72, 1.2), 0.3, 0.1, "Metal", rotacion=(90, 0, 0), segmentos=16)
objetos.append(p.crear(bisel=0.4))

p = Pieza("Tarro")
p.cilindro((0, 0, 2.3), 2.0, 4.6, "Blanco", segmentos=32)
p.cilindro((0, 0, 4.85), 2.05, 0.5, "MaderaTapa", segmentos=32)
p.esfera((0, 0, 5.2), (0.5, 0.5, 0.35), "MaderaTapa", segmentos=16, anillos=8)
p.caja((0, -2.0, 2.6), (1.8, 0.05, 1.0), "Equipo")
objetos.append(p.crear(bisel=0.1))

# ---------------------------------------------------------------------------
# Exportar y vistas
# ---------------------------------------------------------------------------

exportar(objetos, RUTA_FBX, RUTA_BLEND)

vistas(objetos, CARPETA_VISTAS, "kit_despensa", [
    ("zona1", ["Rodillo", "Salero"], 1.0, (0, -30, 12), (0, 0, 1.0)),
    ("zona2", ["Esponja", "Tapon", "GrifoBase", "GrifoCano", "Espuma"], 1.0, (0, -30, 14), (0, 0, 1.0)),
    ("zona3", ["Tomate", "Arandano", "MuroRefugio", "LamparaCalor", "BombillaCalor"], 1.0, (0, -18, 6), (0, 0, 0.8)),
    ("zona4", ["Colchon", "Podio", "LineaMeta"], 1.0, (0, -24, 11), (0, 0, 0.8)),
    ("arcos", ["ArcoControl", "PortalMeta"], 1.0, (0, -52, 16), (0, 0, 4.0)),
    ("plato", ["GruaBase", "GruaBrazo", "Tostadora", "Tarro"], 1.0, (0, -26, 10), (0, 0, 2.5)),
], color_fondo=(0.98, 0.9, 0.78), material_suelo="MaderaRodillo")

print("Kit de la despensa listo.")
