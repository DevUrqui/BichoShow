"""
Bicho Show — kit de objetos del barrio (Mudanza frágil) para Unity.

Uso (sin abrir Blender):
    blender -b --factory-startup -P crear_kit_barrio.py -- <salida.fbx> <salida.blend> [carpeta_vistas]

Cada objeto se exporta con su origen en el centro de su base (Z = 0), y todos en el origen
de la escena: Unity los coge por nombre y los coloca él.

Orientación: lo que aquí mira hacia -Y, en Unity mira hacia +Z (hacia delante, hacia el
recorrido). Los colores salen de los materiales (Unity los copia por nombre);
"Equipo" lo cambia Unity por el color de cada equipo.
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

argumentos = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
RUTA_FBX = argumentos[0] if len(argumentos) > 0 else None
RUTA_BLEND = argumentos[1] if len(argumentos) > 1 else None
CARPETA_VISTAS = argumentos[2] if len(argumentos) > 2 else None

bpy.ops.wm.read_factory_settings(use_empty=True)

# ---------------------------------------------------------------------------
# Materiales
# ---------------------------------------------------------------------------

COLORES = {
    "Blanco": (0.95, 0.93, 0.88), "Equipo": (0.62, 0.62, 0.66), "Hueco": (0.12, 0.10, 0.12),
    "Cristal": (0.60, 0.82, 0.95), "Metal": (0.55, 0.57, 0.60), "Neumatico": (0.10, 0.10, 0.11),
    "Llanta": (0.78, 0.78, 0.80), "Faro": (1.00, 0.92, 0.55), "Madera": (0.62, 0.43, 0.26),
    "Naranja": (1.00, 0.45, 0.10), "Raya": (0.98, 0.98, 0.98), "Farola": (0.12, 0.30, 0.32),
    "Luz": (1.00, 0.88, 0.55), "Tronco": (0.45, 0.30, 0.20), "Hoja": (0.38, 0.74, 0.32),
    "HojaOscura": (0.26, 0.56, 0.26), "Carton": (0.80, 0.60, 0.38), "Cinta": (0.88, 0.80, 0.60),
    "BuzonRojo": (0.90, 0.25, 0.22), "TeleCarcasa": (0.26, 0.25, 0.30), "Pantalla": (0.45, 0.78, 0.98),
    "Jarron": (0.25, 0.72, 0.72), "JarronFranja": (0.98, 0.85, 0.40), "Tallo": (0.30, 0.70, 0.25),
    "Flor": (1.00, 0.45, 0.70), "Sofa": (0.55, 0.35, 0.78), "Cojin": (0.75, 0.60, 0.92),
    "Pata": (0.15, 0.12, 0.12), "FachadaA": (0.95, 0.62, 0.60), "FachadaB": (0.98, 0.85, 0.50),
    "FachadaC": (0.60, 0.85, 0.75), "Ventana": (0.55, 0.75, 0.95), "Tejado": (0.45, 0.35, 0.50),
    # Piezas del recorrido
    "BarreraRoja": (0.92, 0.22, 0.20), "Goma": (0.16, 0.15, 0.17), "Baliza": (1.00, 0.55, 0.10),
    "Peligro": (1.00, 0.82, 0.15), "PeligroNegro": (0.10, 0.10, 0.10), "Hierro": (0.32, 0.33, 0.38),
    "LuzRoja": (1.00, 0.25, 0.20), "Tabla": (0.72, 0.50, 0.30), "TablaClara": (0.80, 0.60, 0.38),
    "TablaOscura": (0.48, 0.32, 0.20), "Cuerda": (0.85, 0.75, 0.55), "Roca": (0.60, 0.60, 0.64),
    "RocaOscura": (0.45, 0.45, 0.50), "Junco": (0.42, 0.62, 0.28), "Nenufar": (0.30, 0.68, 0.35),
    "Piquete": (0.97, 0.96, 0.92), "ToldoBlanco": (0.98, 0.97, 0.94), "Terracota": (0.82, 0.45, 0.30),
    "Geranio": (0.95, 0.20, 0.30),
}


def srgb_a_lineal(c):
    return tuple((v / 12.92) if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in c)


MATERIALES = {}
for nombre, color in COLORES.items():
    m = bpy.data.materials.new(nombre)
    lineal = (*srgb_a_lineal(color), 1.0)
    m.diffuse_color = lineal
    arbol = getattr(m, "node_tree", None)
    if arbol is None:
        try:
            m.use_nodes = True
            arbol = m.node_tree
        except Exception:
            arbol = None
    if arbol is not None:
        bsdf = next((n for n in arbol.nodes if n.type == "BSDF_PRINCIPLED"), None)
        if bsdf is not None:
            bsdf.inputs["Base Color"].default_value = lineal
            bsdf.inputs["Roughness"].default_value = 0.25 if nombre in ("Cristal", "Ventana", "Pantalla") else 0.7
            if nombre in ("Faro", "Luz", "Baliza", "LuzRoja"):
                bsdf.inputs["Emission Color"].default_value = lineal
                bsdf.inputs["Emission Strength"].default_value = 3.0
    MATERIALES[nombre] = m

# ---------------------------------------------------------------------------
# Construcción con bmesh: cada objeto es una sola malla con varios materiales
# ---------------------------------------------------------------------------


class Pieza:
    """Una malla en construcción: se le añaden primitivas, cada una con su material."""

    def __init__(self, nombre):
        self.nombre = nombre
        self.bm = bmesh.new()
        self.materiales = []

    def _indice(self, material):
        if material not in self.materiales:
            self.materiales.append(material)
        return self.materiales.index(material)

    def _asignar(self, verts, material):
        indice = self._indice(material)
        for f in {f for v in verts for f in v.link_faces}:
            f.material_index = indice

    def caja(self, centro, tamano, material, rotacion=(0, 0, 0)):
        m = Matrix.Translation(Vector(centro)) @ _rot(rotacion) @ Matrix.Diagonal((*tamano, 1.0))
        r = bmesh.ops.create_cube(self.bm, size=1.0, matrix=m)
        self._asignar(r["verts"], material)

    def esfera(self, centro, radios, material, segmentos=24, anillos=12):
        m = Matrix.Translation(Vector(centro)) @ Matrix.Diagonal((*radios, 1.0))
        r = bmesh.ops.create_uvsphere(self.bm, u_segments=segmentos, v_segments=anillos, radius=1.0, matrix=m)
        self._asignar(r["verts"], material)

    def cilindro(self, centro, radio, alto, material, rotacion=(0, 0, 0), radio_arriba=None, segmentos=20):
        m = Matrix.Translation(Vector(centro)) @ _rot(rotacion)
        r = bmesh.ops.create_cone(self.bm, cap_ends=True, segments=segmentos, radius1=radio,
                                  radius2=radio if radio_arriba is None else radio_arriba, depth=alto, matrix=m)
        self._asignar(r["verts"], material)

    def crear(self, bisel=0.04):
        me = bpy.data.meshes.new(self.nombre)
        self.bm.to_mesh(me)
        self.bm.free()
        for p in me.polygons:
            p.use_smooth = True
        for m in self.materiales:
            me.materials.append(MATERIALES[m])
        obj = bpy.data.objects.new(self.nombre, me)
        bpy.context.scene.collection.objects.link(obj)
        if bisel > 0:
            mod = obj.modifiers.new("Bisel", "BEVEL")
            mod.width = bisel
            mod.segments = 3
            mod.limit_method = "ANGLE"
            mod.angle_limit = math.radians(40)
            mod.harden_normals = False
        # Suavizado por ángulo para que las caras planas sigan planas y las curvas, suaves.
        try:
            me.set_sharp_from_angle(angle=math.radians(40))
        except Exception:
            pass
        return obj


def _rot(r):
    return (Matrix.Rotation(math.radians(r[2]), 4, "Z") @ Matrix.Rotation(math.radians(r[1]), 4, "Y")
            @ Matrix.Rotation(math.radians(r[0]), 4, "X"))


objetos = []

# ---------------------------------------------------------------------------
# Camión: caja de carga (puerta abierta hacia -Y = el recorrido en Unity) y cabina detrás
# Medidas pensadas para encajar con las colisiones del mapa: caja 3.4 x 5 x 2.6, cabina 3.2 x 1.9.
# ---------------------------------------------------------------------------

p = Pieza("Camion")
p.caja((0, 1.0, 0.5), (2.9, 7.4, 0.3), "Metal")                       # chasis
p.caja((0, 0.0, 1.95), (3.4, 5.0, 2.6), "Blanco")                     # caja de carga
p.caja((0, 0.0, 2.15), (3.46, 5.06, 0.5), "Equipo")                   # franja del equipo
p.caja((0, -2.51, 1.9), (2.8, 0.05, 2.1), "Hueco")                    # puerta abierta
p.caja((0, 3.55, 1.6), (3.1, 1.9, 2.0), "Equipo")                     # cabina
p.caja((0, 3.75, 2.75), (2.8, 1.2, 0.3), "Equipo")                    # techo de la cabina
p.caja((0, 4.51, 2.0), (2.6, 0.05, 0.85), "Cristal")                  # parabrisas
for s in (-1, 1):
    p.caja((1.56 * s, 3.55, 2.0), (0.05, 1.1, 0.75), "Cristal")       # ventanillas
    p.cilindro((1.1 * s, 4.55, 1.0), 0.17, 0.1, "Faro", rotacion=(90, 0, 0))
    for y in (-1.6, -0.3, 3.6):
        p.cilindro((1.55 * s, y, 0.48), 0.48, 0.42, "Neumatico", rotacion=(0, 90, 0))
        p.cilindro((1.58 * s, y, 0.48), 0.22, 0.44, "Llanta", rotacion=(0, 90, 0))
p.caja((0, 4.6, 0.75), (3.2, 0.2, 0.35), "Metal")                     # parachoques
p.caja((0, -3.4, 0.33), (2.2, 1.95, 0.1), "Madera", rotacion=(19.9, 0, 0))  # rampa trasera
objetos.append(p.crear(bisel=0.06))

# ---------------------------------------------------------------------------
# Cono de tráfico (0.7 m de alto)
# ---------------------------------------------------------------------------

p = Pieza("Cono")
p.caja((0, 0, 0.03), (0.55, 0.55, 0.06), "Naranja")
p.cilindro((0, 0, 0.4), 0.21, 0.68, "Naranja", radio_arriba=0.045)
p.cilindro((0, 0, 0.42), 0.128, 0.12, "Raya", radio_arriba=0.106)
objetos.append(p.crear(bisel=0.015))

# ---------------------------------------------------------------------------
# Farola con luz
# ---------------------------------------------------------------------------

p = Pieza("Farola")
p.cilindro((0, 0, 0.1), 0.2, 0.2, "Farola")
p.cilindro((0, 0, 1.9), 0.07, 3.4, "Farola", radio_arriba=0.055)
p.caja((0, -0.4, 3.58), (0.1, 0.9, 0.1), "Farola")
p.cilindro((0, -0.82, 3.48), 0.26, 0.18, "Farola", radio_arriba=0.08)
p.esfera((0, -0.82, 3.33), (0.17, 0.17, 0.14), "Luz")
objetos.append(p.crear(bisel=0.02))

# ---------------------------------------------------------------------------
# Árbol redondo y arbusto
# ---------------------------------------------------------------------------

p = Pieza("Arbol")
p.cilindro((0, 0, 0.8), 0.22, 1.6, "Tronco", radio_arriba=0.14)
for centro, radio, mat in (((0, 0, 2.2), 1.0, "Hoja"), ((0.6, 0.2, 1.85), 0.7, "HojaOscura"),
                           ((-0.55, -0.3, 1.9), 0.68, "Hoja"), ((0.1, -0.45, 2.75), 0.62, "HojaOscura")):
    p.esfera(centro, (radio, radio, radio * 0.92), mat)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Arbusto")
for centro, radio, mat in (((0, 0, 0.4), 0.5, "Hoja"), ((0.45, 0.1, 0.32), 0.38, "HojaOscura"), ((-0.4, -0.12, 0.3), 0.36, "Hoja")):
    p.esfera(centro, (radio, radio, radio * 0.85), mat)
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Banco, buzón y cajas de cartón
# ---------------------------------------------------------------------------

p = Pieza("Banco")
p.caja((0, 0, 0.45), (1.6, 0.45, 0.08), "Madera")
p.caja((0, 0.22, 0.75), (1.6, 0.06, 0.38), "Madera")
for x in (-0.68, 0.68):
    for y in (-0.15, 0.15):
        p.caja((x, y, 0.22), (0.07, 0.07, 0.44), "Metal")
objetos.append(p.crear(bisel=0.015))

p = Pieza("Buzon")
p.caja((0, 0, 0.55), (0.09, 0.09, 1.1), "Metal")
p.caja((0, 0, 1.25), (0.4, 0.55, 0.32), "BuzonRojo")
p.cilindro((0, 0, 1.41), 0.2, 0.55, "BuzonRojo", rotacion=(90, 0, 0))
objetos.append(p.crear(bisel=0.02))

p = Pieza("CajaCarton")
p.caja((0, 0, 0.35), (0.72, 0.72, 0.7), "Carton")
p.caja((0, 0, 0.705), (0.74, 0.14, 0.012), "Cinta")
objetos.append(p.crear(bisel=0.02))

# ---------------------------------------------------------------------------
# Objetos de la mudanza (mismas medidas que sus colisiones en Unity)
#   Tele 1.3 x 1.0 x 0.7 · Jarrón 0.76 x 1.2 · Sofá 2.2 x 1.1 x 1.0
# La pantalla de la tele mira a +Y (en Unity, hacia -Z: hacia la cámara).
# ---------------------------------------------------------------------------

p = Pieza("Tele")
p.caja((0, 0, 0.53), (1.3, 0.7, 0.92), "TeleCarcasa")
p.caja((-0.12, 0.36, 0.55), (0.92, 0.04, 0.68), "Pantalla")
for z in (0.72, 0.5):
    p.cilindro((0.5, 0.37, z), 0.055, 0.05, "Metal", rotacion=(90, 0, 0))
for x in (-0.5, 0.5):
    p.caja((x, 0, 0.035), (0.12, 0.5, 0.07), "Pata")
for s in (-1, 1):
    p.cilindro((0.12 * s, 0, 1.15), 0.018, 0.5, "Metal", rotacion=(0, 25 * s, 0))
    p.esfera((0.23 * s, 0, 1.38), (0.04, 0.04, 0.04), "Metal")
objetos.append(p.crear(bisel=0.05))

p = Pieza("Jarron")
p.cilindro((0, 0, 0.05), 0.24, 0.1, "Jarron")
p.esfera((0, 0, 0.48), (0.38, 0.38, 0.42), "Jarron")
p.cilindro((0, 0, 0.5), 0.383, 0.1, "JarronFranja")
p.cilindro((0, 0, 0.98), 0.15, 0.24, "Jarron", radio_arriba=0.17)
p.cilindro((0, 0, 1.12), 0.22, 0.05, "Jarron")
p.cilindro((0, 0, 1.36), 0.025, 0.45, "Tallo")
p.esfera((0, 0, 1.6), (0.15, 0.15, 0.12), "Flor")
p.esfera((0, 0, 1.6), (0.07, 0.07, 0.13), "JarronFranja")
objetos.append(p.crear(bisel=0.01))

p = Pieza("Sofa")
p.caja((0, 0, 0.3), (2.2, 1.0, 0.45), "Sofa")
p.caja((0, -0.375, 0.85), (2.2, 0.25, 0.75), "Sofa")
for s in (-1, 1):
    p.caja((0.975 * s, 0, 0.65), (0.25, 1.0, 0.55), "Sofa")
    p.caja((0.43 * s, 0.08, 0.6), (0.82, 0.74, 0.18), "Cojin")
    for y in (-0.4, 0.4):
        p.caja((0.95 * s, y, 0.04), (0.12, 0.12, 0.08), "Pata")
objetos.append(p.crear(bisel=0.07))

# ---------------------------------------------------------------------------
# Edificios de fondo (se ven a lo lejos, entre la niebla)
# ---------------------------------------------------------------------------


def edificio(nombre, ancho, fondo, alto, fachada):
    p = Pieza(nombre)
    p.caja((0, 0, alto / 2), (ancho, fondo, alto), fachada)
    p.caja((0, 0, alto + 0.15), (ancho + 0.3, fondo + 0.3, 0.3), "Tejado")
    p.cilindro((ancho * 0.25, fondo * 0.2, alto + 0.9), 0.6, 1.2, "Tejado")
    plantas = int(alto // 2.4)
    for planta in range(plantas):
        z = 1.6 + planta * 2.4
        for eje, medida, otra in ((0, ancho, fondo), (1, fondo, ancho)):
            columnas = max(1, int(medida // 1.8))
            for c in range(columnas):
                u = -medida / 2 + medida * (c + 0.5) / columnas
                for lado in (-1, 1):
                    if eje == 0:
                        p.caja((u, lado * (otra / 2 + 0.02), z), (0.9, 0.06, 1.1), "Ventana")
                    else:
                        p.caja((lado * (otra / 2 + 0.02), u, z), (0.06, 0.9, 1.1), "Ventana")
    return p.crear(bisel=0.12)


objetos.append(edificio("EdificioA", 6, 6, 9.6, "FachadaA"))
objetos.append(edificio("EdificioB", 4.5, 4.5, 12, "FachadaB"))
objetos.append(edificio("EdificioC", 8, 5, 7.2, "FachadaC"))

# ---------------------------------------------------------------------------
# Piezas del recorrido (para vestir las colisiones del mapa; Unity las coloca encima)
# ---------------------------------------------------------------------------

# Barredora: brazo de 14 m a rayas rojas y blancas con topes de goma, y su eje con una baliza.
p = Pieza("BarredoraBrazo")
for i in range(14):
    x = -6.5 + i
    p.cilindro((x, 0, 0.55), 0.21, 1.0, "BarreraRoja" if i % 2 == 0 else "Blanco", rotacion=(0, 90, 0))
for s in (-1, 1):
    p.esfera((7.0 * s, 0, 0.55), (0.3, 0.3, 0.3), "Goma")
    p.cilindro((6.3 * s, 0, 0.55), 0.24, 0.12, "Goma", rotacion=(0, 90, 0))
objetos.append(p.crear(bisel=0.0))

p = Pieza("BarredoraEje")
p.cilindro((0, 0, 0.08), 0.85, 0.16, "PeligroNegro")
p.cilindro((0, 0, 0.55), 0.45, 0.8, "Peligro")
for z in (0.32, 0.62):
    p.cilindro((0, 0, z), 0.46, 0.1, "PeligroNegro")
p.cilindro((0, 0, 1.0), 0.5, 0.12, "Metal")
p.cilindro((0, 0, 1.12), 0.18, 0.14, "Metal")
p.esfera((0, 0, 1.22), (0.2, 0.2, 0.24), "Baliza")
objetos.append(p.crear(bisel=0.03))

# Empujador hidráulico (2.5 de ancho x 2 de fondo x 1.4 de alto). Empuja hacia -Y (en Unity, +Z).
p = Pieza("Empujador")
p.caja((0, 0.25, 0.7), (2.4, 1.5, 1.3), "Peligro")                       # cuerpo
p.caja((0, -0.75, 0.7), (2.5, 0.5, 1.4), "PeligroNegro")                 # cara que empuja
for x in (-0.8, -0.4, 0.0, 0.4, 0.8):                                      # rayas de peligro en la cara
    p.caja((x, -1.01, 0.7), (0.17, 0.04, 1.05), "Peligro", rotacion=(0, 35, 0))
for x in (-0.7, 0.7):                                                     # vástagos hacia atrás
    p.cilindro((x, 1.25, 0.7), 0.16, 0.5, "Metal", rotacion=(90, 0, 0))
p.cilindro((0, 0.25, 1.36), 0.22, 0.1, "Metal")                           # tapa de arriba
p.esfera((0, 0.25, 1.44), (0.14, 0.14, 0.12), "Baliza")
objetos.append(p.crear(bisel=0.1))

# Garaje del empujador (3 de ancho x 0.8 de fondo x 1.8): el hueco mira hacia -Y (el recorrido).
p = Pieza("CarcasaEmpujador")
p.caja((0, 0, 0.9), (3.0, 0.8, 1.8), "Hierro")
p.caja((0, -0.39, 0.8), (2.6, 0.04, 1.5), "Hueco")
for i in range(8):                                                        # franja de peligro arriba
    x = -1.31 + i * 0.375
    p.caja((x, -0.41, 1.66), (0.375, 0.03, 0.24), "Peligro" if i % 2 == 0 else "PeligroNegro")
p.cilindro((1.2, 0, 1.88), 0.12, 0.14, "Metal")
p.esfera((1.2, 0, 1.98), (0.13, 0.13, 0.15), "LuzRoja")
objetos.append(p.crear(bisel=0.06))

# Puente de tablas (1.8 x 8.1, 0.4 de grueso; arriba en Z = 0.4): tablas cruzadas sobre dos vigas.
p = Pieza("PuenteTablas")
for s in (-1, 1):
    p.caja((0.62 * s, 0, 0.12), (0.3, 8.1, 0.24), "TablaOscura")
tablas = 18
for i in range(tablas):
    y = -4.05 + 8.1 * (i + 0.5) / tablas
    p.caja((0.015 * (i % 3 - 1), y, 0.32), (1.8, 8.1 / tablas - 0.05, 0.16), "Tabla" if i % 2 == 0 else "TablaClara")
for s in (-1, 1):
    for y in (-3.9, 3.9):
        p.cilindro((0.86 * s, y, 0.55), 0.07, 0.7, "TablaOscura")
        p.esfera((0.86 * s, y, 0.9), (0.09, 0.09, 0.07), "Cuerda")
objetos.append(p.crear(bisel=0.02))

# Balsa basculante (5 x 7.9, 0.4 de grueso, centrada en Z = 0): tablones a lo largo y bordes de aviso.
p = Pieza("BalsaTablas")
n = 9
for i in range(n):
    x = -2.5 + 5.0 * (i + 0.5) / n
    p.caja((x, 0.02 * ((i * 7) % 3 - 1), 0.08), (5.0 / n - 0.05, 7.9, 0.24), "Tabla" if i % 2 == 0 else "TablaClara")
for y in (-3.2, 0.0, 3.2):
    p.caja((0, y, -0.12), (5.0, 0.5, 0.16), "TablaOscura")
for s in (-1, 1):
    for k in range(10):
        y = -3.95 + 7.9 * (k + 0.5) / 10
        p.caja((2.42 * s, y, 0.215), (0.16, 0.79, 0.03), "Peligro" if k % 2 == 0 else "PeligroNegro")
objetos.append(p.crear(bisel=0.02))

# Orillas del río: rocas redondas, juncos y nenúfares.
p = Pieza("RocasRio")
for centro, radios, mat in (((0, 0, 0.3), (0.8, 0.65, 0.5), "Roca"), ((0.75, 0.35, 0.2), (0.5, 0.45, 0.35), "RocaOscura"),
                            ((-0.65, -0.3, 0.18), (0.45, 0.4, 0.3), "RocaOscura"), ((0.2, -0.6, 0.12), (0.3, 0.3, 0.2), "Roca")):
    p.esfera(centro, radios, mat, segmentos=10, anillos=6)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Juncos")
for i, (x, y, alto) in enumerate(((0, 0, 1.3), (0.25, 0.1, 1.0), (-0.2, 0.15, 1.1), (0.1, -0.22, 0.9), (-0.28, -0.12, 0.8), (0.3, -0.15, 1.15))):
    inclinacion = ((i * 37) % 21) - 10
    p.cilindro((x, y, alto / 2), 0.035, alto, "Junco", rotacion=(inclinacion, inclinacion * 0.6, 0), segmentos=6)
    if i % 2 == 0:
        p.cilindro((x + inclinacion * 0.004 * alto, y, alto + 0.05), 0.07, 0.3, "Tronco", segmentos=8)
objetos.append(p.crear(bisel=0.0))

p = Pieza("Nenufar")
p.cilindro((0, 0, 0.02), 0.55, 0.04, "Nenufar", segmentos=18)
p.esfera((-0.12, 0.08, 0.1), (0.13, 0.13, 0.09), "Flor")
p.esfera((-0.12, 0.08, 0.16), (0.05, 0.05, 0.04), "JarronFranja")
objetos.append(p.crear(bisel=0.0))

# Valla blanca de 2 m (a lo largo de X), 0.75 de alto.
p = Pieza("Valla")
for z in (0.25, 0.55):
    p.caja((0, 0, z), (2.0, 0.05, 0.1), "Piquete")
for i in range(7):
    x = -0.9 + i * 0.3
    p.caja((x, -0.05, 0.36), (0.13, 0.05, 0.72), "Piquete")
    p.cilindro((x, -0.05, 0.76), 0.092, 0.12, "Piquete", radio_arriba=0.0, segmentos=4, rotacion=(0, 0, 45))
objetos.append(p.crear(bisel=0.015))

# Toldo a rayas (del color del equipo y blanco): 7.4 de ancho, sale 0.9 hacia -Y bajando 0.45.
p = Pieza("Toldo")
franjas = 10
inclinacion = math.degrees(math.atan2(0.45, 0.9))
for i in range(franjas):
    x = -3.7 + 7.4 * (i + 0.5) / franjas
    mat = "Equipo" if i % 2 == 0 else "ToldoBlanco"
    p.caja((x, -0.45, -0.22), (7.4 / franjas, 1.0, 0.05), mat, rotacion=(inclinacion, 0, 0))
    p.esfera((x, -0.93, -0.56), (0.36, 0.025, 0.2), mat, segmentos=16, anillos=8)   # festón
for s in (-1, 1):
    p.cilindro((3.66 * s, -0.45, -0.22), 0.03, 1.0, "Metal", rotacion=(90 + inclinacion, 0, 0), segmentos=6)
objetos.append(p.crear(bisel=0.0))

# Ventana con contraventanas del equipo y jardinera con geranios (mira a -Y, pegada a la pared).
p = Pieza("VentanaCasa")
p.caja((0, 0, 1.75), (1.3, 0.1, 1.2), "Piquete")
p.caja((0, -0.04, 1.75), (1.1, 0.06, 1.0), "Ventana")
p.caja((0, -0.08, 1.75), (0.06, 0.04, 1.0), "Piquete")
p.caja((0, -0.08, 1.75), (1.1, 0.04, 0.06), "Piquete")
for s in (-1, 1):
    p.caja((0.92 * s, -0.06, 1.75), (0.5, 0.06, 1.2), "Equipo")
    for z in (1.45, 1.75, 2.05):
        p.caja((0.92 * s, -0.1, z), (0.42, 0.03, 0.05), "ToldoBlanco")
p.caja((0, -0.22, 1.08), (1.4, 0.36, 0.3), "Terracota")
for i, x in enumerate((-0.5, -0.17, 0.17, 0.5)):
    p.esfera((x, -0.22, 1.3), (0.2, 0.17, 0.15), "Hoja")
    p.esfera((x + 0.05, -0.3, 1.4), (0.08, 0.08, 0.07), "Geranio" if i % 2 == 0 else "Flor")
objetos.append(p.crear(bisel=0.02))

# Maceta con arbusto redondo y flores.
p = Pieza("Maceta")
p.cilindro((0, 0, 0.3), 0.32, 0.6, "Terracota", radio_arriba=0.4)
p.cilindro((0, 0, 0.62), 0.43, 0.08, "Terracota")
p.esfera((0, 0, 1.0), (0.45, 0.45, 0.42), "Hoja")
for ang in range(0, 360, 72):
    a = math.radians(ang)
    p.esfera((0.33 * math.cos(a), 0.33 * math.sin(a), 1.15), (0.09, 0.09, 0.08), "Geranio")
objetos.append(p.crear(bisel=0.02))

# Hoja de la puerta giratoria (2.9 x 2.6): cristal con marco, barra para empujar y un zócalo.
p = Pieza("HojaPuerta")
p.caja((0, 0, 1.3), (2.82, 0.06, 2.5), "Cristal")
for z in (0.05, 2.55):
    p.caja((0, 0, z), (2.9, 0.12, 0.1), "Metal")
for s in (-1, 1):
    p.caja((1.4 * s, 0, 1.3), (0.1, 0.12, 2.6), "Metal")
p.caja((0, 0, 0.22), (2.82, 0.1, 0.32), "Metal")
for s in (-1, 1):
    p.cilindro((0.3, 0.13 * s, 1.15), 0.035, 1.8, "Metal", rotacion=(0, 90, 0), segmentos=8)
objetos.append(p.crear(bisel=0.015))

# Rodillo del extremo de una cinta (7.4 a lo largo de X), con sus soportes.
p = Pieza("RodilloCinta")
p.cilindro((0, 0, 0.0), 0.22, 7.0, "Metal", rotacion=(0, 90, 0))
for s in (-1, 1):
    p.caja((3.6 * s, 0, 0.05), (0.25, 0.6, 0.3), "Peligro")
    p.cilindro((3.75 * s, 0, 0.05), 0.12, 0.08, "Metal", rotacion=(0, 90, 0), segmentos=10)
objetos.append(p.crear(bisel=0.02))

# Palé (1.6 x 1.6, 0.14 de alto) para debajo de las pilas de cajas.
p = Pieza("Palet")
for x in (-0.65, 0.0, 0.65):
    p.caja((x, 0, 0.05), (0.18, 1.6, 0.1), "TablaOscura")
for i in range(6):
    y = -0.7 + i * 0.28
    p.caja((0, y, 0.12), (1.6, 0.18, 0.04), "Tabla")
objetos.append(p.crear(bisel=0.01))

# Tapa de alcantarilla (para el asfalto).
p = Pieza("Alcantarilla")
p.cilindro((0, 0, 0.01), 0.55, 0.02, "Hierro", segmentos=24)
for x in (-0.3, -0.1, 0.1, 0.3):
    p.caja((x, 0, 0.025), (0.06, 0.8 - abs(x) * 0.9, 0.012), "PeligroNegro")
objetos.append(p.crear(bisel=0.0))

# ---------------------------------------------------------------------------
# Guardar y exportar (todos en el origen)
# ---------------------------------------------------------------------------

if RUTA_BLEND:
    os.makedirs(os.path.dirname(RUTA_BLEND), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=RUTA_BLEND)

if RUTA_FBX:
    os.makedirs(os.path.dirname(RUTA_FBX), exist_ok=True)
    for obj in bpy.context.scene.objects:
        obj.select_set(obj in objetos)
    bpy.context.view_layer.objects.active = objetos[0]
    bpy.ops.export_scene.fbx(
        filepath=RUTA_FBX, use_selection=True, object_types={"MESH"}, use_mesh_modifiers=True,
        mesh_smooth_type="FACE", apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        bake_space_transform=True, add_leaf_bones=False, path_mode="STRIP",
    )
    print(f"Exportado: {RUTA_FBX}")

# ---------------------------------------------------------------------------
# Vistas previas: los objetos en fila (ya exportados en el origen)
# ---------------------------------------------------------------------------

if CARPETA_VISTAS:
    os.makedirs(CARPETA_VISTAS, exist_ok=True)
    escena = bpy.context.scene
    for candidato in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "BLENDER_WORKBENCH"):
        try:
            escena.render.engine = candidato
            break
        except Exception:
            continue

    mundo = bpy.data.worlds.new("Fondo")
    escena.world = mundo
    try:
        mundo.use_nodes = True
    except Exception:
        pass
    if getattr(mundo, "node_tree", None):
        fondo = next((n for n in mundo.node_tree.nodes if n.type == "BACKGROUND"), None)
        if fondo:
            fondo.inputs["Color"].default_value = (*srgb_a_lineal((0.86, 0.88, 0.95)), 1.0)
            fondo.inputs["Strength"].default_value = 0.9

    sol = bpy.data.objects.new("Sol", bpy.data.lights.new("Sol", "SUN"))
    sol.data.energy = 3.5
    sol.rotation_euler = (math.radians(50), 0, math.radians(-35))
    escena.collection.objects.link(sol)

    suelo = bpy.data.objects.new("Suelo", bpy.data.meshes.new("Suelo"))
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=60)
    bm.to_mesh(suelo.data)
    bm.free()
    suelo.data.materials.append(MATERIALES["Cinta"])
    escena.collection.objects.link(suelo)

    camara = bpy.data.objects.new("Camara", bpy.data.cameras.new("Camara"))
    camara.data.lens = 45
    escena.collection.objects.link(camara)
    escena.camera = camara
    escena.render.resolution_x = 1400
    escena.render.resolution_y = 700

    def vista(nombre, lista, separacion, posicion_camara, mirar):
        x = 0.0
        for obj in lista:
            ancho = obj.dimensions.x
            obj.location = (x + ancho / 2, 0, 0)
            x += ancho + separacion
        desplazamiento = -x / 2
        for obj in lista:
            obj.location.x += desplazamiento
        camara.location = posicion_camara
        camara.rotation_euler = (Vector(mirar) - Vector(posicion_camara)).to_track_quat("-Z", "Y").to_euler()
        for obj in objetos:
            obj.hide_render = obj not in lista
        escena.render.filepath = os.path.join(CARPETA_VISTAS, f"kit_{nombre}.png")
        bpy.ops.render.render(write_still=True)
        print(f"Vista: {escena.render.filepath}")

    porNombre = {o.name: o for o in objetos}
    vista("barrio", [porNombre[n] for n in ("Camion", "Farola", "Arbol", "Arbusto", "Banco", "Buzon", "CajaCarton", "Cono")],
          1.0, (0, 19, 7), (0, 0, 1.5))
    vista("mudanza", [porNombre[n] for n in ("Tele", "Jarron", "Sofa")], 0.6, (0, 5.5, 2.2), (0, 0, 0.6))
    vista("recorrido", [porNombre[n] for n in ("BarredoraEje", "Empujador", "CarcasaEmpujador", "PuenteTablas", "RocasRio", "Juncos", "Nenufar", "Palet", "Alcantarilla")],
          0.8, (0, 17, 7), (0, 0, 0.8))
    vista("casa", [porNombre[n] for n in ("Valla", "Toldo", "VentanaCasa", "Maceta", "HojaPuerta", "RodilloCinta")],
          0.8, (0, 17, 6), (0, 0, 1.2))
    vista("edificios", [porNombre[n] for n in ("EdificioA", "EdificioB", "EdificioC")], 2.0, (0, 34, 12), (0, 0, 5))

print("Kit del barrio listo.")
