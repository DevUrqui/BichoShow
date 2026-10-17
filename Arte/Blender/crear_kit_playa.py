"""
Bicho Show — kit de objetos de la playa (Voley atados) para Unity.

Uso (sin abrir Blender):
    blender -b --factory-startup -P crear_kit_playa.py -- <salida.fbx> <salida.blend> [carpeta_vistas]

Mismas reglas que el resto de kits (ver kit_comun.py): origen en el centro de la base,
lo que mira a -Y aquí mira a +Z en Unity.
Excepciones: "Aspas" tiene el origen en el buje (Unity la hace girar sobre su eje Z) y los
objetos que flotan (Flotador, Pato, Colchoneta) lo tienen a la altura del agua.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from kit_comun import Pieza, argumentos, crear_materiales, exportar, vistas  # noqa: E402

RUTA_FBX, RUTA_BLEND, CARPETA_VISTAS = argumentos()

COLORES = {
    # Generales (mismo color que en el kit del barrio)
    "Blanco": (0.95, 0.93, 0.88), "Metal": (0.55, 0.57, 0.60),
    # Palmeras
    "TroncoPalmera": (0.72, 0.52, 0.32), "TroncoPalmeraClaro": (0.84, 0.66, 0.44),
    "HojaPalmera": (0.30, 0.76, 0.36), "HojaPalmeraOscura": (0.20, 0.58, 0.30), "Coco": (0.42, 0.28, 0.18),
    # Playa
    "RojoPlaya": (0.95, 0.30, 0.28), "AmarilloPlaya": (1.00, 0.82, 0.25), "AzulPlaya": (0.25, 0.58, 0.95),
    "TurquesaPlaya": (0.25, 0.80, 0.78), "RosaPlaya": (1.00, 0.55, 0.70),
    "Bambu": (0.80, 0.62, 0.38), "BambuOscuro": (0.60, 0.43, 0.25), "Paja": (0.93, 0.78, 0.45),
    "PajaOscura": (0.78, 0.60, 0.30), "ArenaCastillo": (0.90, 0.76, 0.50), "ArenaMojada": (0.78, 0.62, 0.40),
    "Roca": (0.60, 0.60, 0.64), "RocaOscura": (0.45, 0.45, 0.50),
    "Botella": (0.35, 0.80, 0.55), "BotellaAmbar": (0.95, 0.60, 0.20),
    # Hinchables
    "FlotadorRojo": (1.00, 0.30, 0.30), "PatoAmarillo": (1.00, 0.85, 0.20), "Pico": (1.00, 0.52, 0.12),
    # Bichos y trampas
    "CangrejoRojo": (0.95, 0.32, 0.20), "CangrejoClaro": (1.00, 0.62, 0.45),
    "OjoBlanco": (0.98, 0.98, 0.98), "PupilaNegra": (0.07, 0.07, 0.09),
    "VentiladorBase": (0.20, 0.62, 0.66), "Aspa": (0.95, 0.95, 0.97),
}

crear_materiales(COLORES, brillantes=("Botella", "BotellaAmbar", "FlotadorRojo", "PatoAmarillo", "OjoBlanco", "PupilaNegra"))

objetos = []

# ---------------------------------------------------------------------------
# Palmera: tronco curvado a anillos que se inclina hacia +X, hojas caídas y cocos
# ---------------------------------------------------------------------------


def palmera(nombre, alto, curva, hojas):
    p = Pieza(nombre)
    tramos = 8
    puntos = [(curva * (i / tramos) ** 2, 0, alto * i / tramos) for i in range(tramos + 1)]
    for i in range(tramos):
        r0 = 0.30 - 0.12 * i / tramos
        p.tubo(puntos[i], puntos[i + 1], r0, "TroncoPalmera" if i % 2 == 0 else "TroncoPalmeraClaro",
               radio_final=r0 * 0.8, segmentos=10)
    cx, _, cz = puntos[-1]
    p.esfera((cx, 0, cz + 0.05), (0.32, 0.32, 0.25), "HojaPalmeraOscura")
    for i in range(3):
        a = 2 * math.pi * i / 3 + 0.4
        p.esfera((cx + 0.22 * math.cos(a), 0.22 * math.sin(a), cz - 0.18), (0.19, 0.19, 0.2), "Coco")
    for i in range(hojas):
        a = 2 * math.pi * i / hojas + (0.3 if i % 2 else 0)
        grados = math.degrees(a)
        mat = "HojaPalmera" if i % 2 == 0 else "HojaPalmeraOscura"
        # Dos tramos por hoja: el de dentro sube un poco, el de fuera cae.
        for distancia, caida, largo in ((0.75, -12, 0.85), (1.95, 38, 0.8)):
            ca = math.radians(caida)
            centro = (cx + math.cos(a) * distancia * math.cos(ca), math.sin(a) * distancia * math.cos(ca),
                      cz + 0.15 - distancia * math.sin(ca) * 0.9)
            p.esfera(centro, (largo, 0.32, 0.05), mat, rotacion=(0, caida, grados), segmentos=12, anillos=6)
    return p.crear(bisel=0.0)


objetos.append(palmera("Palmera", 5.2, 1.0, 8))
objetos.append(palmera("PalmeraAlta", 6.6, 1.6, 7))

# ---------------------------------------------------------------------------
# Sombrilla, toalla, tumbona
# ---------------------------------------------------------------------------


def sombrilla(nombre, color):
    p = Pieza(nombre)
    p.cilindro((0, 0, 1.25), 0.045, 2.5, "Blanco", segmentos=10)
    p.cilindro((0, 0, 2.42), 1.35, 0.5, color, radio_arriba=0.06, segmentos=16, rayas=(color, "Blanco", 8))
    p.esfera((0, 0, 2.7), (0.08, 0.08, 0.08), color)
    return p.crear(bisel=0.0)


objetos.append(sombrilla("Sombrilla", "RojoPlaya"))
objetos.append(sombrilla("SombrillaAzul", "AzulPlaya"))


def toalla(nombre, colores):
    p = Pieza(nombre)
    franja = 1.8 / len(colores)
    for i, color in enumerate(colores):
        p.caja((0, -0.9 + franja * (i + 0.5), 0.015), (0.95, franja, 0.03), color)
    return p.crear(bisel=0.005)


objetos.append(toalla("Toalla", ("TurquesaPlaya", "Blanco", "TurquesaPlaya", "Blanco", "TurquesaPlaya")))
objetos.append(toalla("ToallaRosa", ("RosaPlaya", "AmarilloPlaya", "RosaPlaya", "AmarilloPlaya")))

p = Pieza("Tumbona")
p.caja((0, -0.25, 0.36), (0.72, 1.3, 0.07), "AzulPlaya")                       # asiento (-Y = pies)
p.caja((0, 0.62, 0.62), (0.72, 0.7, 0.07), "AzulPlaya", rotacion=(-50, 0, 0))   # respaldo
for x in (-0.33, 0.33):
    p.caja((x, -0.25, 0.32), (0.05, 1.32, 0.05), "Blanco")
    for y in (-0.8, 0.3):
        p.tubo((x, y, 0.0), (x, y, 0.33), 0.03, "Blanco")
objetos.append(p.crear(bisel=0.02))

# ---------------------------------------------------------------------------
# Chiringuito: barra de bambú, techo de paja y un coco gigante con pajita de reclamo
# El mostrador mira a -Y (en Unity, +Z).
# ---------------------------------------------------------------------------

p = Pieza("Chiringuito")
p.caja((0, 0, 0.15), (5.2, 4.2, 0.3), "BambuOscuro")                                # tarima
for x in (-2.35, 2.35):
    for y in (-1.85, 1.85):
        p.tubo((x, y, 0.3), (x, y, 3.0), 0.12, "Bambu", segmentos=10)
p.caja((0, -1.55, 0.85), (4.6, 0.6, 1.1), "Bambu")                                  # barra
p.caja((0, -1.6, 1.45), (4.9, 0.85, 0.1), "BambuOscuro")                            # encimera
for x in (-1.6, -0.55, 0.5, 1.55):
    p.caja((x, -1.86, 0.85), (0.08, 0.02, 1.0), "BambuOscuro")                      # tablas de la barra
p.caja((0, 1.7, 1.6), (4.6, 0.5, 2.6), "Bambu")                                     # pared del fondo
for x, mat, alto in ((-1.4, "Botella", 0.45), (-1.1, "BotellaAmbar", 0.38), (-0.8, "Botella", 0.5),
                     (0.9, "BotellaAmbar", 0.45), (1.2, "Botella", 0.38), (1.5, "BotellaAmbar", 0.5)):
    p.cilindro((x, 1.35, 1.95 + alto / 2), 0.09, alto, mat, radio_arriba=0.05, segmentos=10)
p.caja((0, 1.38, 1.92), (3.6, 0.3, 0.06), "BambuOscuro")                            # estante
p.cilindro((0, 0, 3.75), 4.0, 1.6, "Paja", radio_arriba=0.15, segmentos=4, rotacion=(0, 0, 45))  # techo
p.cilindro((0, 0, 2.95), 3.95, 0.3, "PajaOscura", radio_arriba=3.75, segmentos=4, rotacion=(0, 0, 45))  # flecos
p.esfera((0, 0, 4.85), (0.65, 0.65, 0.6), "Coco")                                   # coco gigante
p.cilindro((0, 0, 5.38), 0.55, 0.12, "Blanco", segmentos=16)
p.tubo((0.1, 0, 5.3), (0.45, 0.15, 6.3), 0.06, "RojoPlaya", segmentos=8)            # pajita
p.cilindro((-0.35, -0.1, 5.85), 0.45, 0.25, "AmarilloPlaya", radio_arriba=0.03, segmentos=8,
           rotacion=(0, -25, 0), rayas=("AmarilloPlaya", "RosaPlaya", 6))          # sombrillita
for x in (-1.5, 0, 1.5):
    p.tubo((x, -2.45, 0.0), (x, -2.45, 0.75), 0.05, "BambuOscuro")
    p.cilindro((x, -2.45, 0.78), 0.26, 0.1, "RojoPlaya", segmentos=14)              # taburetes
objetos.append(p.crear(bisel=0.04))

# ---------------------------------------------------------------------------
# Torre del socorrista (mira a -Y)
# ---------------------------------------------------------------------------

p = Pieza("TorreSocorrista")
for x in (-0.75, 0.75):
    for y in (-0.75, 0.75):
        p.tubo((x * 1.15, y * 1.15, 0), (x * 0.8, y * 0.8, 2.2), 0.08, "Blanco", segmentos=8)
p.caja((0, 0, 2.25), (1.9, 1.9, 0.15), "RojoPlaya")                                  # plataforma
p.caja((0, 0.15, 2.65), (1.0, 0.8, 0.12), "Blanco")                                  # asiento
p.caja((0, 0.55, 3.2), (1.0, 0.1, 1.0), "RojoPlaya")                                 # respaldo
for x in (-0.3, 0.3):
    p.tubo((x, -1.25, 0), (x, -0.9, 2.2), 0.04, "Blanco", segmentos=6)              # escalera
for i in range(5):
    t = (i + 0.5) / 5
    p.caja((0, -1.25 + 0.35 * t, 2.2 * t), (0.6, 0.06, 0.05), "Blanco")
p.tubo((0.85, 0.85, 2.3), (0.85, 0.85, 4.6), 0.035, "Metal", segmentos=8)            # mástil
p.caja((0.85, 0.55, 4.35), (0.03, 0.6, 0.4), "AmarilloPlaya")                        # bandera
p.cilindro((-0.85, 0.85, 4.1), 0.9, 0.35, "RojoPlaya", radio_arriba=0.04, segmentos=12,
           rayas=("RojoPlaya", "Blanco", 6))                                          # sombrilla
p.tubo((-0.85, 0.85, 2.3), (-0.85, 0.85, 4.0), 0.03, "Blanco", segmentos=6)
p.toro((0, -0.97, 1.75), 0.32, 0.09, "FlotadorRojo", rotacion=(90, 0, 0), segmentos=20, segmentos_tubo=8,
       rayas=("FlotadorRojo", "Blanco", 4))                                           # salvavidas
objetos.append(p.crear(bisel=0.02))

# ---------------------------------------------------------------------------
# Castillo de arena, cubo y pala, rocas, tabla de surf
# ---------------------------------------------------------------------------

p = Pieza("CastilloArena")
p.caja((0, 0, 0.25), (1.6, 1.6, 0.5), "ArenaMojada")
p.caja((0, 0, 0.75), (1.0, 1.0, 0.5), "ArenaCastillo")
p.cilindro((0, 0, 1.3), 0.32, 0.6, "ArenaCastillo", radio_arriba=0.26, segmentos=12)
p.cilindro((0, 0, 1.75), 0.22, 0.3, "ArenaMojada", radio_arriba=0.02, segmentos=12)
for x in (-0.7, 0.7):
    for y in (-0.7, 0.7):
        p.cilindro((x, y, 0.55), 0.24, 1.1, "ArenaCastillo", radio_arriba=0.2, segmentos=12)
        for i in range(4):
            a = math.pi / 2 * i + math.pi / 4
            p.caja((x + 0.17 * math.cos(a), y + 0.17 * math.sin(a), 1.16), (0.1, 0.1, 0.12), "ArenaCastillo")
p.caja((0, -0.81, 0.18), (0.32, 0.04, 0.36), "ArenaMojada")                          # puerta
p.tubo((0, 0, 1.85), (0, 0, 2.35), 0.015, "Blanco", segmentos=6)
p.caja((0, -0.13, 2.25), (0.02, 0.26, 0.16), "RojoPlaya")                            # banderita
objetos.append(p.crear(bisel=0.02))

p = Pieza("CuboPala")
p.cilindro((0, 0, 0.2), 0.2, 0.4, "AzulPlaya", radio_arriba=0.26, segmentos=16)
p.toro((0, 0, 0.42), 0.18, 0.012, "AmarilloPlaya", rotacion=(90, 0, 0), segmentos=16, segmentos_tubo=6)
p.tubo((0.45, 0.1, 0.0), (0.7, 0.1, 0.65), 0.025, "AmarilloPlaya", segmentos=6)
p.caja((0.42, 0.1, 0.06), (0.22, 0.04, 0.2), "AmarilloPlaya", rotacion=(0, -20, 0))
objetos.append(p.crear(bisel=0.01))

p = Pieza("Roca")
for centro, radios, mat in (((0, 0, 0.45), (1.0, 0.85, 0.65), "Roca"), ((0.75, 0.3, 0.3), (0.6, 0.55, 0.45), "RocaOscura"),
                            ((-0.6, -0.35, 0.25), (0.5, 0.45, 0.35), "RocaOscura")):
    p.esfera(centro, radios, mat, segmentos=9, anillos=6)
objetos.append(p.crear(bisel=0.0))

p = Pieza("TablaSurf")
p.esfera((0, 0, 1.0), (0.32, 0.07, 1.05), "TurquesaPlaya", rotacion=(0, 8, 0), segmentos=20, anillos=10)
p.esfera((0, -0.02, 1.0), (0.06, 0.06, 1.0), "Blanco", rotacion=(0, 8, 0), segmentos=8, anillos=6)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Hinchables que flotan en el mar (origen a la altura del agua)
# ---------------------------------------------------------------------------

p = Pieza("Flotador")
p.toro((0, 0, 0), 0.75, 0.3, "FlotadorRojo", rayas=("FlotadorRojo", "Blanco", 8))
objetos.append(p.crear(bisel=0.0))

p = Pieza("Pato")
p.esfera((0, 0.15, 0.15), (0.95, 1.2, 0.62), "PatoAmarillo")                          # cuerpo
p.esfera((0, 1.15, 0.55), (0.3, 0.3, 0.45), "PatoAmarillo", rotacion=(-35, 0, 0))     # cola
p.esfera((0, -0.65, 1.0), (0.58, 0.55, 0.55), "PatoAmarillo")                         # cabeza
p.esfera((0, -1.22, 0.9), (0.32, 0.3, 0.1), "Pico")                                   # pico
for s in (-1, 1):
    p.esfera((0.27 * s, -1.08, 1.15), (0.12, 0.08, 0.14), "OjoBlanco")
    p.esfera((0.28 * s, -1.15, 1.17), (0.06, 0.04, 0.08), "PupilaNegra")
    p.esfera((0.85 * s, 0.25, 0.35), (0.18, 0.55, 0.3), "PatoAmarillo", rotacion=(0, 15 * s, 0))  # alas
objetos.append(p.crear(bisel=0.0))

p = Pieza("Colchoneta")
for i in range(5):
    p.esfera((0, -0.8 + 0.4 * i, 0.05), (0.5, 0.25, 0.12), "RosaPlaya" if i % 2 == 0 else "AmarilloPlaya",
             segmentos=16, anillos=8)
p.esfera((0, 1.2, 0.12), (0.45, 0.22, 0.16), "RosaPlaya", segmentos=16, anillos=8)      # almohada
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Trampas: cangrejo (mira a -Y y corre de lado) y ventilador (+ sus aspas)
# El caparazón va primero: Unity hace destellar el primer material como aviso.
# ---------------------------------------------------------------------------

p = Pieza("Cangrejo")
p.esfera((0, 0.05, 0.5), (0.82, 0.58, 0.36), "CangrejoRojo")                          # caparazón
p.esfera((0, 0.0, 0.36), (0.7, 0.5, 0.2), "CangrejoClaro")                            # tripa
for s in (-1, 1):
    for i, y in enumerate((-0.15, 0.15, 0.42)):
        rodilla = (s * 0.95, y + 0.05, 0.55)
        p.tubo((s * 0.55, y, 0.4), rodilla, 0.07, "CangrejoRojo", radio_final=0.06, segmentos=8)
        p.tubo(rodilla, (s * 1.2, y + 0.1, 0.0), 0.06, "CangrejoRojo", radio_final=0.03, segmentos=8)
    codo = (s * 0.85, -0.45, 0.65)
    p.tubo((s * 0.5, -0.3, 0.5), codo, 0.09, "CangrejoRojo", segmentos=8)               # brazo
    p.esfera((s * 0.95, -0.75, 0.72), (0.3, 0.26, 0.24), "CangrejoRojo")              # pinza
    p.esfera((s * 0.85, -1.02, 0.8), (0.1, 0.18, 0.08), "CangrejoRojo", rotacion=(0, 0, -20 * s))
    p.esfera((s * 1.05, -1.0, 0.64), (0.09, 0.16, 0.07), "CangrejoClaro", rotacion=(0, 0, 20 * s))
    p.tubo((s * 0.22, -0.3, 0.75), (s * 0.27, -0.35, 1.05), 0.045, "CangrejoRojo", segmentos=8)  # antenas de ojo
    p.esfera((s * 0.27, -0.36, 1.12), (0.14, 0.14, 0.14), "OjoBlanco")
    p.esfera((s * 0.27, -0.48, 1.13), (0.07, 0.04, 0.08), "PupilaNegra")
p.caja((0, -0.6, 0.48), (0.28, 0.04, 0.05), "PupilaNegra", rotacion=(0, 0, 0))       # boca
objetos.append(p.crear(bisel=0.0))

p = Pieza("Ventilador")
p.caja((0, 0.1, 0.1), (1.2, 0.8, 0.2), "VentiladorBase")                               # pie
p.tubo((0, 0.15, 0.2), (0, 0.15, 1.65), 0.09, "Metal", segmentos=10)                   # columna
p.cilindro((0, 0.18, 1.9), 0.32, 0.55, "VentiladorBase", rotacion=(90, 0, 0), segmentos=16)  # motor
p.toro((0, -0.2, 1.9), 1.32, 0.06, "VentiladorBase", rotacion=(90, 0, 0), segmentos=40, segmentos_tubo=8)  # aro
for a in (0, 60, 120):
    p.caja((0, -0.2, 1.9), (2.6, 0.03, 0.03), "Metal", rotacion=(0, a, 0))             # rejilla
objetos.append(p.crear(bisel=0.02))

p = Pieza("Aspas")
p.esfera((0, -0.05, 0), (0.17, 0.12, 0.17), "VentiladorBase")
for i in range(3):
    a = 120 * i
    ra = math.radians(a)
    p.esfera((0.62 * math.cos(ra), 0, -0.62 * math.sin(ra)), (0.55, 0.035, 0.2), "Aspa", rotacion=(18, a, 0),
             segmentos=16, anillos=8)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Exportar y vistas
# ---------------------------------------------------------------------------

exportar(objetos, RUTA_FBX, RUTA_BLEND)

# Las aspas salen medio enterradas en la vista (su origen es el buje).
vistas(objetos, CARPETA_VISTAS, "kit_playa", [
    ("palmeras", ["Palmera", "PalmeraAlta", "Sombrilla", "SombrillaAzul", "Toalla", "ToallaRosa", "Tumbona"],
     0.8, (0, -30, 10), (0, 0, 2.2)),
    ("chiringuito", ["Chiringuito", "TorreSocorrista", "CastilloArena", "CuboPala", "Roca", "TablaSurf"],
     0.8, (0, -28, 9), (0, 0, 2.0)),
    ("mar", ["Flotador", "Pato", "Colchoneta", "Cangrejo", "Ventilador", "Aspas"], 0.8, (0, -16, 5), (0, 0, 0.9)),
], color_fondo=(0.70, 0.86, 1.0), material_suelo="ArenaCastillo")

print("Kit de la playa listo.")
