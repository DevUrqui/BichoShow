"""
Bicho Show — kit de "Costura a la fuga" (carrera sobre una mesa de costura) para Unity.

Uso (sin abrir Blender):
    blender -b --factory-startup -P crear_kit_costura.py -- <salida.fbx> <salida.blend> [carpeta_vistas]

Mismas reglas que el resto de kits (ver kit_comun.py): lo que mira a -Y aquí mira a +Z en
Unity, y Unity (x, y, z) = (-X, Z, -Y). "Equipo" es la tela o el hilo: Unity le pone el color
de cada sitio (coral, azul, turquesa, mostaza).

Las piezas que se mueven o se pisan tienen el tamaño exacto de su colisión en Unity
(Editor/CreadorCostura.cs) y su origen donde gira o se apoya:
- Carrete: tumbado, eje a lo largo de Y, origen en el centro (rueda sobre él). Hilo Ø2,5, tapas Ø3,2, 3,4 de largo.
- PanelAcordeon: 4 × 3 × 0,4, origen en el centro (gira sobre él). Arriba en Z = +0,2.
- MitadCremallera*: origen en la bisagra (borde de fuera, arriba). Se extiende hacia -X (Unity +x),
  con los dientes en el borde de dentro. Arriba en Z = 0.
- Alfiletero*: origen en el centro del bloque de 1,2 de alto (arriba en Z = +0,6).
- PuenteCinta: origen arriba, en el centro (6 × 2 × 0,25).
- El resto, decorado, con el origen en el centro de la base.
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
    # Costura
    "MaderaCostura": (0.88, 0.72, 0.52), "MaderaOscuraCostura": (0.62, 0.45, 0.3),
    "CremaCostura": (0.98, 0.94, 0.84), "AzulCostura": (0.14, 0.2, 0.42), "CoralCostura": (0.95, 0.45, 0.38),
    "MostazaCostura": (0.93, 0.68, 0.2), "AgujeroCostura": (0.2, 0.13, 0.1),
    "DedalCostura": (0.82, 0.82, 0.86), "HoyueloCostura": (0.55, 0.55, 0.6), "MarcaCinta": (0.12, 0.1, 0.1),
}

crear_materiales(COLORES, brillantes=("Oro", "Metal", "DedalCostura"))

objetos = []


def boton(p, centro, radio, material, normal="Z", agujeros="AgujeroCostura", hilo="CremaCostura"):
    """Botón de cuatro agujeros con su borde y el hilo en cruz. normal: hacia dónde mira la cara."""
    rot = {"Z": (0, 0, 0), "Y": (90, 0, 0), "-Y": (-90, 0, 0)}[normal]
    grosor = radio * 0.22
    p.cilindro(centro, radio, grosor, material, rotacion=rot, segmentos=28)
    # Borde levantado y agujeros en la cara (la de +normal).
    def en_cara(dx, dy, dz=0.0):
        cx, cy, cz = centro
        if normal == "Z":
            return (cx + dx, cy + dy, cz + grosor * 0.5 + dz)
        s = 1 if normal == "-Y" else -1
        return (cx + dx, cy + s * (grosor * 0.5 + dz), cz + dy)
    p.toro(en_cara(0, 0, 0.0), radio * 0.88, radio * 0.09, material, rotacion=rot, segmentos=28, segmentos_tubo=8)
    for ax, ay in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        p.cilindro(en_cara(ax * radio * 0.24, ay * radio * 0.24, 0.0), radio * 0.11, grosor * 0.25, agujeros, rotacion=rot, segmentos=12)
    for ax in (-1, 1):
        a = en_cara(-radio * 0.24, ax * -radio * 0.24, radio * 0.03)
        b = en_cara(radio * 0.24, ax * radio * 0.24, radio * 0.03)
        p.tubo(a, b, radio * 0.035, hilo, segmentos=6)


# ---------------------------------------------------------------------------
# Carrete viajero (tumbado, rueda sobre su eje Y)
# ---------------------------------------------------------------------------

p = Pieza("Carrete")
p.cilindro((0, 0, 0), 1.2, 2.9, "Equipo", rotacion=(90, 0, 0), segmentos=32)                 # hilo
for k in range(12):                                                                          # vueltas de hilo
    y = -1.32 + k * 0.24
    p.toro((0, y, 0), 1.2, 0.07, "Equipo", rotacion=(90, 0, 0), segmentos=32, segmentos_tubo=6)
for s in (-1, 1):
    p.cilindro((0, s * 1.56, 0), 1.6, 0.24, "MaderaCostura", rotacion=(90, 0, 0), segmentos=32)  # tapas
    p.toro((0, s * 1.56, 0), 1.56, 0.12, "MaderaCostura", rotacion=(90, 0, 0), segmentos=32, segmentos_tubo=8)
    p.cilindro((0, s * 1.69, 0), 0.38, 0.04, "AgujeroCostura", rotacion=(90, 0, 0), segmentos=18)   # agujero del eje
    for k in range(4):                                                                       # agujeros: se ve que gira
        a = math.radians(45 + 90 * k)
        p.cilindro((0.95 * math.cos(a), s * 1.69, 0.95 * math.sin(a)), 0.16, 0.04, "MaderaOscuraCostura", rotacion=(90, 0, 0), segmentos=12)
# Dos hilos cruzados por encima (también delatan el giro) y la hebra suelta.
p.tubo((1.15, -1.2, 0.35), (-0.35, 1.2, 1.15), 0.06, "CremaCostura", segmentos=6)
p.tubo((-1.15, -1.2, -0.35), (0.35, 1.2, -1.15), 0.06, "CremaCostura", segmentos=6)
p.tubo((0.2, 0.3, -1.22), (0.5, 0.45, -1.7), 0.05, "Equipo", segmentos=6)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Panel del puente acordeón: tabla de madera con tela acolchada y pespunte
# ---------------------------------------------------------------------------

p = Pieza("PanelAcordeon")
p.caja((0, 0, -0.08), (4.0, 3.0, 0.24), "MaderaCostura")
p.caja((0, 0, 0.0), (3.84, 2.84, 0.2), "Equipo")
for x in (-0.96, 0.96):                                                                      # dos almohadillas abultadas
    p.esfera((x, 0, 0.1), (0.98, 1.42, 0.14), "Equipo", segmentos=24, anillos=10)
for i in range(9):                                                                           # pespunte del borde
    x = -1.6 + i * 0.4
    for y in (-1.25, 1.25):
        p.caja((x, y, 0.205), (0.22, 0.05, 0.02), "CremaCostura")
for j in range(6):
    y = -1.0 + j * 0.4
    for x in (-1.75, 1.75):
        p.caja((x, y, 0.205), (0.05, 0.22, 0.02), "CremaCostura")
for x, y in ((-0.9, -0.6), (0.9, -0.6), (-0.9, 0.6), (0.9, 0.6), (0, 0)):                     # capitoné
    p.esfera((x, y, 0.19), (0.14, 0.14, 0.05), "Oro", segmentos=12, anillos=6)
for x in (-1.6, 1.6):                                                                        # bisagras de las juntas
    for y in (-1.52, 1.52):
        p.cilindro((x, y, -0.05), 0.08, 0.5, "Oro", rotacion=(0, 90, 0), segmentos=10)
objetos.append(p.crear(bisel=0.05))

# ---------------------------------------------------------------------------
# Cremallera: media mitad de tramo (la bisagra en el origen, se extiende hacia -X)
# ---------------------------------------------------------------------------


def mitad_cremallera(nombre, ancho, largo):
    p = Pieza(nombre)
    p.caja((-ancho / 2, 0, -0.2), (ancho, largo, 0.4), "Equipo")                              # cinta de tela
    p.cilindro((-0.08, 0, -0.2), 0.16, largo, "MaderaCostura", rotacion=(90, 0, 0), segmentos=12)  # bisagra
    n = int(largo / 0.24)
    for k in range(n):                                                                       # pespunte de fuera
        p.caja((-0.35, -largo / 2 + 0.15 + k * 0.24, 0.005), (0.04, 0.14, 0.02), "CremaCostura")
    k = 0
    y = -largo / 2 + 0.14
    while y < largo / 2 - 0.1:                                                               # dientes
        p.caja((-ancho + 0.08, y, -0.08), (0.34, 0.16, 0.2), "Oro")
        y += 0.36
        k += 1
    return p.crear(bisel=0.03)


objetos.append(mitad_cremallera("MitadCremalleraAncha", 2.17, 1.94))
objetos.append(mitad_cremallera("MitadCremalleraEstrecha", 1.17, 2.44))

p = Pieza("Tirador")
p.caja((0, 0, 0), (0.6, 1.1, 0.5), "Oro")
p.caja((0, 0, 0.3), (0.42, 0.8, 0.12), "Oro")
p.caja((-0.5, 0.15, 0.1), (0.7, 0.5, 0.12), "Oro", rotacion=(0, -15, 0))                    # lengüeta (hacia -X)
p.toro((-0.72, 0.15, 0.16), 0.12, 0.04, "Metal", segmentos=14, segmentos_tubo=6)
objetos.append(p.crear(bisel=0.04))

# ---------------------------------------------------------------------------
# Alfileteros acolchados (origen en el centro del bloque de 1,2 de alto)
# ---------------------------------------------------------------------------


def alfiletero(nombre, ancho, fondo):
    p = Pieza(nombre)
    p.caja((0, 0, -0.1), (ancho, fondo, 1.0), "Equipo")
    nx0, ny0 = max(2, round(ancho / 3.2)), max(2, round(fondo / 3.0))
    for i in range(nx0):                                                                     # almohadillas abultadas
        for j in range(ny0):
            x = -ancho / 2 + ancho * (i + 0.5) / nx0
            y = -fondo / 2 + fondo * (j + 0.5) / ny0
            p.esfera((x, y, 0.42), (ancho / nx0 * 0.53, fondo / ny0 * 0.53, 0.24), "Equipo", segmentos=24, anillos=10)
    p.caja((0, 0, -0.2), (ancho + 0.08, fondo + 0.08, 0.28), "AzulCostura")                 # franja de tela
    for s in (-1, 1):                                                                        # cordón del borde
        p.tubo((-ancho / 2 + 0.15, s * (fondo / 2 - 0.05), 0.55), (ancho / 2 - 0.15, s * (fondo / 2 - 0.05), 0.55), 0.09, "CremaCostura", segmentos=8)
        p.tubo((s * (ancho / 2 - 0.05), -fondo / 2 + 0.15, 0.55), (s * (ancho / 2 - 0.05), fondo / 2 - 0.15, 0.55), 0.09, "CremaCostura", segmentos=8)
    nx, ny = max(2, round(ancho / 3.2)), max(2, round(fondo / 3.0))
    for i in range(1, nx):                                                                   # botones donde se hunde la tela
        for j in range(1, ny):
            boton(p, (-ancho / 2 + ancho * i / nx, -fondo / 2 + fondo * j / ny, 0.44), 0.32, "Oro")
    return p.crear(bisel=0.25)


objetos.append(alfiletero("Alfiletero12x7", 12.0, 7.0))
objetos.append(alfiletero("Alfiletero14x6", 14.5, 6.5))

# ---------------------------------------------------------------------------
# Puente de cinta métrica (origen arriba en el centro: 6 de ancho, 2 de fondo)
# ---------------------------------------------------------------------------

p = Pieza("PuenteCinta")
p.caja((0, 0, -0.125), (6.0, 2.0, 0.25), "MostazaCostura")
for k in range(61):                                                                          # marcas cada 10 cm
    x = -3.0 + k * 0.1
    largo = 0.5 if k % 10 == 0 else 0.3 if k % 5 == 0 else 0.18
    for s in (-1, 1):
        p.caja((x, s * (1.0 - largo / 2 - 0.05), 0.005), (0.025, largo, 0.02), "MarcaCinta")
p.caja((0, 0, 0.005), (0.6, 0.35, 0.02), "CoralCostura")                                     # el "0" de la cinta
objetos.append(p.crear(bisel=0.03))

# ---------------------------------------------------------------------------
# Arco de tela (SALIDA / META): 18 de ancho; el texto lo pone Unity delante del cartel
# ---------------------------------------------------------------------------

p = Pieza("ArcoTela")
for s in (-1, 1):
    x = s * 9.0
    p.caja((x, 0, 0.3), (1.4, 1.4, 0.6), "MaderaCostura")                                   # pie
    p.cilindro((x, 0, 2.5), 0.62, 4.0, "Equipo", segmentos=24)                                # poste forrado
    for k in range(6):                                                                       # vueltas de hilo
        p.toro((x, 0, 1.0 + k * 0.6), 0.63, 0.06, "CremaCostura", segmentos=24, segmentos_tubo=6)
    p.esfera((x, 0, 4.55), (0.72, 0.72, 0.3), "Equipo", segmentos=24, anillos=10)           # remate
    boton(p, (x, 0.45, 4.7), 0.7, "MaderaCostura", normal="-Y")                              # botón delante del cartel
p.caja((0, 0, 4.7), (17.0, 0.5, 1.3), "CremaCostura")                                        # cartel
for z in (4.08, 5.32):                                                                       # ribete
    p.tubo((-8.4, 0, z), (8.4, 0, z), 0.09, "CoralCostura", segmentos=8)
for k in range(40):                                                                          # pespunte del cartel
    x = -8.0 + k * 0.41
    p.caja((x, -0.26, 4.2), (0.22, 0.02, 0.04), "CoralCostura")
    p.caja((x, -0.26, 5.2), (0.22, 0.02, 0.04), "CoralCostura")
objetos.append(p.crear(bisel=0.04))

# ---------------------------------------------------------------------------
# Decorado: dedal, banderín, flecha cosida, botón, carrete de pie, tijeras, cinta
# ---------------------------------------------------------------------------

p = Pieza("Dedal")
p.cilindro((0, 0, 2.2), 2.2, 4.4, "DedalCostura", radio_arriba=1.95, segmentos=40)
p.toro((0, 0, 0.12), 2.18, 0.16, "DedalCostura", segmentos=40, segmentos_tubo=8)           # reborde
p.esfera((0, 0, 4.4), (1.95, 1.95, 0.9), "DedalCostura", segmentos=40, anillos=16)          # cúpula
for fila in range(7):
    z = 0.7 + fila * 0.55
    r = 2.2 - (2.2 - 1.95) * (z / 4.4)
    for i in range(22):
        a = 2 * math.pi * (i + (fila % 2) * 0.5) / 22
        p.esfera((r * math.cos(a), r * math.sin(a), z), (0.13, 0.13, 0.13), "HoyueloCostura", segmentos=8, anillos=5)
for anillo, n in ((0.6, 8), (1.2, 14)):
    for i in range(n):
        a = 2 * math.pi * i / n
        p.esfera((anillo * math.cos(a), anillo * math.sin(a), 4.4 + 0.9 * math.sqrt(max(0, 1 - (anillo / 1.95) ** 2)) - 0.03),
                 (0.12, 0.12, 0.06), "HoyueloCostura", segmentos=8, anillos=5)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Banderin")
p.tubo((0, 0, 0), (0, 0, 1.9), 0.06, "MaderaCostura", segmentos=8)
p.esfera((0, 0, 1.95), (0.11, 0.11, 0.11), "Oro", segmentos=10, anillos=6)
p.cilindro((-0.4, 0, 1.55), 0.42, 0.05, "Equipo", rotacion=(90, 0, 0), segmentos=3)        # banderín triangular
p.caja((0, 0, 0.04), (0.4, 0.4, 0.08), "MaderaOscuraCostura")
objetos.append(p.crear(bisel=0.01))

p = Pieza("Flecha")                                                                          # apunta a -Y (Unity +Z)
for s in (-1, 1):
    giro = -s * 45
    p.caja((s * 0.38, 0.1, 0.02), (0.32, 1.2, 0.04), "Equipo", rotacion=(0, 0, giro))
    eje = (-math.sin(math.radians(giro)), math.cos(math.radians(giro)))                  # a lo largo del brazo
    for k in range(4):                                                                       # pespunte por el centro del brazo
        t = -0.42 + k * 0.28
        p.caja((s * 0.38 + eje[0] * t, 0.1 + eje[1] * t, 0.045), (0.05, 0.16, 0.02), "CremaCostura", rotacion=(0, 0, giro))
objetos.append(p.crear(bisel=0.015))

p = Pieza("Boton")
boton(p, (0, 0, 0.11), 1.0, "Equipo")
objetos.append(p.crear(bisel=0.0))

p = Pieza("CarreteDePie")
p.cilindro((0, 0, 1.0), 0.9, 1.7, "Equipo", segmentos=28)
for k in range(7):
    p.toro((0, 0, 0.3 + k * 0.23), 0.9, 0.05, "Equipo", segmentos=28, segmentos_tubo=6)
for z in (0.08, 1.92):
    p.cilindro((0, 0, z), 1.2, 0.16, "MaderaCostura", segmentos=28)
p.cilindro((0, 0, 2.0), 0.25, 0.02, "AgujeroCostura", segmentos=14)
p.tubo((0.85, 0, 1.4), (1.3, -0.5, 0.1), 0.035, "Equipo", segmentos=6)                      # hebra suelta
objetos.append(p.crear(bisel=0.0))

p = Pieza("Tijeras")
for s in (-1, 1):
    p.caja((s * 0.2, -1.6, 0.15), (0.5, 3.4, 0.12), "DedalCostura", rotacion=(0, 0, s * 8))  # hojas
    p.toro((s * 0.75, 1.05, 0.15), 0.5, 0.12, "Equipo", rotacion=(0, 0, 0), segmentos=22, segmentos_tubo=8)  # mangos
    p.tubo((s * 0.1, 0.05, 0.15), (s * 0.55, 0.65, 0.15), 0.13, "Equipo", segmentos=8)
p.cilindro((0, 0, 0.25), 0.15, 0.1, "Oro", segmentos=12)                                    # tornillo
objetos.append(p.crear(bisel=0.02))

p = Pieza("CintaRollo")
p.cilindro((0, 0, 0.25), 1.2, 0.5, "MostazaCostura", segmentos=32)
for k in range(24):
    a = 2 * math.pi * k / 24
    p.caja((1.2 * math.cos(a), 1.2 * math.sin(a), 0.25), (0.03, 0.03, 0.3), "MarcaCinta", rotacion=(0, 0, math.degrees(a)))
p.cilindro((0, 0, 0.52), 0.3, 0.05, "CoralCostura", segmentos=18)
p.caja((1.2, -1.6, 0.03), (0.9, 3.2, 0.06), "MostazaCostura")                               # tira desenrollada
objetos.append(p.crear(bisel=0.02))

# ---------------------------------------------------------------------------
# Exportar y vistas (una, desde arriba y atrás, como la cámara del juego)
# ---------------------------------------------------------------------------

exportar(objetos, RUTA_FBX, RUTA_BLEND)

vistas(objetos, CARPETA_VISTAS, "kit_costura", [
    ("obstaculos", ["Carrete", "PanelAcordeon", "MitadCremalleraAncha", "MitadCremalleraEstrecha", "Tirador"], 1.0, (0, -14, 6), (0, 0, 0.5)),
    ("camara_juego", ["Carrete", "PanelAcordeon", "MitadCremalleraAncha", "Banderin", "Flecha"], 1.0, (0, 13, 12), (0, 0, 0)),
    ("alfileteros", ["Alfiletero12x7", "PuenteCinta"], 1.0, (0, -22, 14), (0, 0, 0)),
    ("arco", ["ArcoTela", "Dedal"], 1.5, (0, -30, 8), (0, 0, 3)),
    ("decorado", ["Boton", "CarreteDePie", "Tijeras", "CintaRollo", "Banderin", "Flecha"], 1.0, (0, -14, 7), (0, 0, 0.6)),
], color_fondo=(0.86, 0.76, 0.64), material_suelo="MaderaCostura")

print("Kit de costura listo.")
