"""
Bicho Show — los concursantes: Bichete (mariquita) y sus compañeros, con esqueleto y animaciones.

Uso (sin abrir Blender; rutas completas):
    blender -b --factory-startup -P crear_personaje.py -- <salida.fbx> <salida.blend> <carpeta_vistas> <especie>
    especie: bichete · abeja · caracol · escarabajo · luciernaga · oruga · arana · hormiga · saltamontes

Todas las especies comparten los nombres de los huesos, los anclajes del vestuario, los materiales
(mismo orden) y las animaciones: en Unity se cambian unas por otras sin tocar nada más. Cada una
tiene su diseño (DISENOS: tipo de cuerpo, tronco, cabeza, cara, ojos, boca, antenas…) y lo suyo:
  - bichete: huevo redondo con caparazón de mariquita, raya y lunares.
  - abeja: cabeza redonda aparte con ojos negros de insecto, tronco pequeño con raya, collar de
    pelusa en el cuello, abdomen grande a rayas con aguijón, brazos y patas negros y dos pares de
    alas grandes (huesos AlaIzq/AlaDer: Unity las hace aletear).
  - caracol: alto y estrecho con la base ancha, ojos en lo alto de las antenas y una concha enorme
    en espiral (material "Concha": no se tiñe).
  - escarabajo: bajito y ancho de hombros, brazos y patas gordos, élitros grandes, cejo de
    gruñón y un cuerno de rinoceronte.
  - luciernaga: flaca, ojos grandes, capucha oscura, alas y un farolillo enorme en el culete
    (material "Luz": brilla), y lucecitas en las antenas.
  - oruga: segmentos apilados con lunares y patitas de bolita, cabeza grande con cresta.
  - arana: cuerpo bajo con un culete enorme detrás y por encima, seis ojos, colmillos, pelos de
    punta y cuatro patas más; sin antenas.
  - hormiga: cabezona con ojos de insecto, tórax pequeñito, cintura finísima y culete gordo con
    anillos; mandíbulas y antenas acodadas.
  - saltamontes: cara de caballo con dientes de conejo, alas plegadas a la espalda, antenas muy
    largas y las patas saltadoras con la rodilla por encima del cuerpo.

Diseño pensado para la cámara del juego (alta y desde atrás) y para que al girar no parezca
un recortable: cuerpo redondo como un huevo, caparazón de mariquita a la espalda (del color
del jugador, con lunares: es lo que más se ve desde arriba), antenas con bolitas (Unity las
hace bambolearse), ojos grandes, boca abierta con lengua, coloretes, brazos y patas cortos
y gorditos, manoplas y zapatillas.

Una sola malla con pesos y un esqueleto:
    Raiz > Cadera > Cuerpo > (OjoIzq/Der, AntenaIzq1 > AntenaIzq2, Brazo > Antebrazo > Mano)
                 > Muslo > Espinilla > Pie
El cuerpo se reparte entre Cadera y Cuerpo según la altura: se dobla como una gominola.
Materiales (orden fijo): Piel (Bichete: se tiñe con el color del jugador), Caparazon (Bichete:
se tiñe más oscuro), Barriga, Mancha, Ojo, Pupila, Brillo, Boca, Lengua, Rubor, Bota, Suela, Luz, Ala,
Concha, Pelo, Guante (manoplas: siempre del color del jugador; en los demás bichos, también
las zapatillas).
Animaciones (en bucle): Quieto, Andar, Correr, Saltar, Caer, Tumbado, Celebrar.

Convenciones: Z arriba, mira hacia -Y, pies en Z = 0, metros. "Izq" = lado de X negativa.
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

argumentos = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
RUTA_FBX = argumentos[0] if len(argumentos) > 0 else None
RUTA_BLEND = argumentos[1] if len(argumentos) > 1 else None
CARPETA_VISTAS = argumentos[2] if len(argumentos) > 2 else None
ESPECIE = argumentos[3] if len(argumentos) > 3 else "bichete"
CON_ALAS = ESPECIE in ("abeja", "luciernaga")

bpy.ops.wm.read_factory_settings(use_empty=True)
escena = bpy.context.scene
escena.render.fps = 30
LADOS = ((-1, "Izq"), (1, "Der"))

# ---------------------------------------------------------------------------
# Medidas
# ---------------------------------------------------------------------------

# Cada bicho tiene su diseño:
#   tipo: "huevo" (la cara va en el propio cuerpo, como Bichete), "cabeza" (cabeza aparte encima de
#         un tronco) u "oruga" (segmentos apilados y cabeza).
#   tronco: centro (altura), radios (medio ancho, medio fondo, medio alto) y pera (cuánto más ancho
#           es abajo que arriba; negativo: más ancho arriba). De aquí salen brazos, barriga y pecho.
#   cabeza: centro y radios (si no es "huevo").  cara: altura de ojos, boca y coloretes (de -1 a 1 en
#   la cabeza) y separación de los ojos.  grosor de brazos y patas, tamaño y estilo de ojos, boca,
#   antenas (codo y punta desde la base), y material de brazos/patas y de antenas.
_HUEVO = dict(tipo="huevo", cara=(0.483, 0.086, 0.207, 0.34), ojos="normal", boca="abierta", barriga=True,
              extremidad="Piel", mat_antena="Piel", antenas=True)
DISENOS = {
    "bichete":    dict(_HUEVO, tronco=(0.88, (0.50, 0.46, 0.58), 0.10), grosor=1.00, tam_ojos=1.00,
                       antena=((0.08, -0.04, 0.20), (0.18, -0.11, 0.34))),
    "caracol":    dict(_HUEVO, tronco=(0.95, (0.40, 0.40, 0.69), 0.26), grosor=0.90, tam_ojos=1.00,
                       antena=((0.04, -0.03, 0.20), (0.10, -0.08, 0.36))),                    # alto, ojos en antenas
    "escarabajo": dict(_HUEVO, tronco=(0.80, (0.62, 0.50, 0.50), -0.12), grosor=1.30, tam_ojos=0.85,
                       antena=((0.10, 0.00, 0.10), (0.20, -0.05, 0.15))),                     # bajito y ancho
    "luciernaga": dict(_HUEVO, tronco=(0.95, (0.42, 0.40, 0.55), 0.04), grosor=0.85, tam_ojos=1.15,
                       antena=((0.10, -0.10, 0.24), (0.14, -0.32, 0.30))),                    # flaca, farol enorme
    "arana":      dict(_HUEVO, tronco=(0.78, (0.46, 0.42, 0.38), 0.08), grosor=0.75, tam_ojos=0.8,
                       cara=(0.42, -0.1, 0.12, 0.3), ojos="seis", boca="colmillos", barriga=False,
                       extremidad="Mancha", antenas=False,
                       antena=((0.06, 0.0, 0.10), (0.12, 0.0, 0.16))),                        # bajita, culo enorme, 6 ojos
    "abeja":      dict(_HUEVO, tipo="cabeza", tronco=(0.66, (0.40, 0.38, 0.32), 0.0),
                       cabeza=((0.0, -0.04, 1.2), (0.43, 0.39, 0.37)), cara=(0.14, -0.3, -0.12, 0.42),
                       grosor=0.80, tam_ojos=1.15, ojos="insecto", boca="sonrisa", barriga=False,
                       extremidad="Mancha", mat_antena="Mancha",
                       antena=((0.12, -0.02, 0.16), (0.22, -0.16, 0.22))),
    "hormiga":    dict(_HUEVO, tipo="cabeza", tronco=(0.70, (0.26, 0.25, 0.26), 0.0),
                       cabeza=((0.0, -0.08, 1.2), (0.45, 0.41, 0.4)), cara=(0.15, -0.42, -0.2, 0.4),
                       grosor=0.70, tam_ojos=0.9, ojos="insecto", boca="sonrisa", barriga=False,
                       extremidad="Mancha", mat_antena="Mancha",
                       antena=((0.04, -0.02, 0.26), (0.26, -0.22, 0.32))),                    # antenas acodadas
    "oruga":      dict(_HUEVO, tipo="oruga", tronco=(0.74, (0.37, 0.35, 0.26), 0.0),
                       cabeza=((0.0, -0.03, 1.17), (0.44, 0.4, 0.4)), cara=(0.2, -0.38, -0.12, 0.36),
                       grosor=0.80, tam_ojos=1.1, barriga=False,
                       antena=((0.04, -0.02, 0.12), (0.09, -0.06, 0.2))),
    "saltamontes": dict(_HUEVO, tipo="cabeza", tronco=(0.74, (0.30, 0.30, 0.34), 0.0),
                       cabeza=((0.0, -0.1, 1.25), (0.34, 0.42, 0.4)), cara=(0.38, -0.4, -0.1, 0.52),
                       grosor=0.75, tam_ojos=1.0, boca="dientes",
                       antena=((0.08, 0.12, 0.30), (0.18, 0.45, 0.5))),                       # cara de caballo, antenas largas
}
D = DISENOS[ESPECIE]
TIPO = D["tipo"]
CENTRO = Vector((0.0, 0.0, D["tronco"][0]))
RADIOS = Vector(D["tronco"][1])
PERA = D["tronco"][2]
GROSOR = D["grosor"]
OJOS = D["tam_ojos"]
if TIPO == "huevo":
    CAB_C, CAB_R = CENTRO, RADIOS
else:
    CAB_C, CAB_R = Vector(D["cabeza"][0]), Vector(D["cabeza"][1])
ARRIBA = CAB_C.z + CAB_R.z             # lo más alto de la cabeza
ANCHO = max(0.85, RADIOS.x / 0.5)      # separación de las patas respecto a Bichete


def altura(k):
    """Altura relativa al tronco: 0 = centro, 1 = arriba del todo, -1 = abajo del todo."""
    return CENTRO.z + k * RADIOS.z


def altura_cara(k):
    """Altura relativa a la cabeza (en los "huevo", la cabeza es el cuerpo)."""
    return CAB_C.z + k * CAB_R.z


HOMBRO = Vector((RADIOS.x * 0.88, 0.0, CENTRO.z - 0.02))
CODO = HOMBRO + Vector((0.14, -0.05, -0.14))
MUNECA = HOMBRO + Vector((0.20, -0.09, -0.28))
PUNTA_MANO = HOMBRO + Vector((0.22, -0.11, -0.40))
CADERA = Vector((0.20 * ANCHO, 0.0, 0.40))
RODILLA = Vector((0.22 * ANCHO, -0.03, 0.26))
TOBILLO = Vector((0.24 * ANCHO, 0.0, 0.13))
PUNTA_PIE = Vector((0.24 * ANCHO, -0.22, 0.04))
ANTENA_BASE = Vector((0.24 * CAB_R.x + (0.06 if ESPECIE == "caracol" else 0.0), CAB_C.y + 0.06, ARRIBA - 0.04))
ANTENA_MEDIO = ANTENA_BASE + Vector(D["antena"][0])
ANTENA_PUNTA = ANTENA_BASE + Vector(D["antena"][1])
ALA_BASE = Vector((0.32 * RADIOS.x, RADIOS.y * 0.9, altura(0.24)))
ALA_PUNTA = ALA_BASE + Vector((0.46, 0.20, 0.20))
OJO_X, OJO_Z = D["cara"][3] * CAB_R.x, altura_cara(D["cara"][0])
BOCA_Z, RUBOR_Z = altura_cara(D["cara"][1]), altura_cara(D["cara"][2])

def espejo(v, lado):
    return Vector((v.x * lado, v.y, v.z))


def ensanche(z):
    """Factor de anchura según la altura (forma de pera)."""
    t = (z - (CENTRO.z - RADIOS.z)) / (2 * RADIOS.z)
    return 1.0 + PERA * (0.5 - max(0.0, min(1.0, t)))


def superficie(direccion):
    """Punto de la superficie del cuerpo en esa dirección (desde el centro)."""
    d = Vector(direccion).normalized()
    r = 1.0 / math.sqrt((d.x / RADIOS.x) ** 2 + (d.y / RADIOS.y) ** 2 + (d.z / RADIOS.z) ** 2)
    p = CENTRO + d * r
    k = ensanche(p.z)
    return Vector((p.x * k, p.y * k, p.z))


def delante(x, z, salir=0.0):
    """Punto de la parte de delante del tronco a una altura y un lado."""
    dz = (z - CENTRO.z) / RADIOS.z
    k = ensanche(z)
    resto = max(0.0, 1.0 - dz * dz - (x / (RADIOS.x * k)) ** 2)
    return Vector((x, -RADIOS.y * k * math.sqrt(resto) - salir, z))


def cara(x, z, salir=0.0):
    """Punto de la cara (delante de la cabeza) a una altura y un lado."""
    if TIPO == "huevo":
        return delante(x, z, salir)
    dz = (z - CAB_C.z) / CAB_R.z
    resto = max(0.0, 1.0 - dz * dz - (x / CAB_R.x) ** 2)
    return Vector((x, CAB_C.y - CAB_R.y * math.sqrt(resto) - salir, z))

# ---------------------------------------------------------------------------
# Materiales
# ---------------------------------------------------------------------------


def srgb_a_lineal(c):
    return tuple((v / 12.92) if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in c)


def material(nombre, color, rugosidad=0.6):
    m = bpy.data.materials.new(nombre)
    lineal = (*srgb_a_lineal(color), 1.0)
    m.diffuse_color = lineal
    try:
        m.use_nodes = True
    except Exception:
        pass
    arbol = getattr(m, "node_tree", None)
    if arbol is not None:
        bsdf = next((n for n in arbol.nodes if n.type == "BSDF_PRINCIPLED"), None)
        if bsdf is not None:
            bsdf.inputs["Base Color"].default_value = lineal
            bsdf.inputs["Roughness"].default_value = rugosidad
    return m


COLORES = {
    "Piel": ((1.0, 0.42, 0.36), 0.55), "Caparazon": ((0.82, 0.18, 0.2), 0.3), "Barriga": ((1.0, 0.93, 0.78), 0.6),
    "Mancha": ((0.16, 0.08, 0.14), 0.4), "Ojo": ((0.98, 0.97, 0.95), 0.3), "Pupila": ((0.06, 0.04, 0.06), 0.2),
    "Brillo": ((1.0, 1.0, 1.0), 0.1), "Boca": ((0.28, 0.06, 0.1), 0.6), "Lengua": ((1.0, 0.45, 0.55), 0.5),
    "Rubor": ((1.0, 0.55, 0.6), 0.7), "Bota": ((0.2, 0.75, 0.75), 0.5), "Suela": ((0.95, 0.95, 0.92), 0.7),
    "Luz": ((1.0, 0.95, 0.4), 0.3), "Ala": ((0.85, 0.95, 1.0), 0.15),
    "Concha": ((0.93, 0.72, 0.45), 0.35), "Pelo": ((1.0, 0.95, 0.8), 0.9),   # estos dos no se tiñen
    "Guante": ((1.0, 0.42, 0.36), 0.55),   # las manoplas: siempre del color del jugador
}
# Bichete se tiñe entero del color del jugador; los demás llevan sus colores de siempre y el
# color del jugador va en las manoplas y las zapatillas (en las vistas, un rojo de ejemplo).
if ESPECIE != "bichete":
    COLORES["Guante"] = COLORES["Bota"] = ((0.92, 0.25, 0.3), 0.5)
    COLORES["Piel"] = {"abeja": ((1.0, 0.8, 0.15), 0.5), "caracol": ((0.45, 0.8, 0.78), 0.55),
                       "escarabajo": ((0.36, 0.5, 0.95), 0.45), "luciernaga": ((0.62, 0.52, 0.92), 0.5),
                       "hormiga": ((0.9, 0.38, 0.2), 0.45), "arana": ((0.42, 0.3, 0.58), 0.6),
                       "oruga": ((0.98, 0.55, 0.75), 0.5), "saltamontes": ((0.6, 0.86, 0.2), 0.5)}[ESPECIE]
if ESPECIE == "hormiga":
    COLORES["Caparazon"] = ((0.62, 0.2, 0.12), 0.25)   # el culete
    COLORES["Mancha"] = ((0.22, 0.1, 0.08), 0.35)
if ESPECIE == "arana":
    COLORES["Caparazon"] = ((0.3, 0.2, 0.42), 0.5)     # el culete
    COLORES["Mancha"] = ((0.14, 0.1, 0.18), 0.5)       # patas
    COLORES["Concha"] = ((1.0, 0.6, 0.15), 0.4)        # manchas naranjas
    COLORES["Pelo"] = ((0.22, 0.16, 0.3), 0.9)
if ESPECIE == "oruga":
    COLORES["Concha"] = ((1.0, 0.85, 0.3), 0.45)       # lunares amarillos
    COLORES["Mancha"] = ((0.55, 0.25, 0.6), 0.5)       # patitas
if ESPECIE == "saltamontes":
    COLORES["Caparazon"] = ((0.3, 0.56, 0.16), 0.4)    # alas plegadas
    COLORES["Barriga"] = ((0.95, 0.95, 0.6), 0.6)
    COLORES["Mancha"] = ((0.2, 0.35, 0.12), 0.5)
if ESPECIE == "escarabajo":
    COLORES["Caparazon"] = ((0.12, 0.6, 0.45), 0.15)   # verde metalizado
if ESPECIE == "caracol":
    COLORES["Mancha"] = ((0.5, 0.3, 0.18), 0.4)
if ESPECIE == "abeja":
    COLORES["Concha"] = ((1.0, 0.8, 0.12), 0.45)     # el amarillo del abdomen (no se tiñe)
    COLORES["Mancha"] = ((0.1, 0.07, 0.06), 0.4)
ORDEN_MATERIALES = list(COLORES.keys())
MAT = {n: material(n, c, r) for n, (c, r) in COLORES.items()}

# ---------------------------------------------------------------------------
# Esqueleto
# ---------------------------------------------------------------------------

datos = bpy.data.armatures.new("Esqueleto")
esqueleto = bpy.data.objects.new("Esqueleto", datos)
escena.collection.objects.link(esqueleto)
bpy.context.view_layer.objects.active = esqueleto
esqueleto.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")


def hueso(nombre, cabeza, cola, padre=None, conectado=False):
    b = datos.edit_bones.new(nombre)
    b.head, b.tail, b.roll = Vector(cabeza), Vector(cola), 0.0
    if padre:
        b.parent = datos.edit_bones[padre]
        b.use_connect = conectado
    return b


hueso("Raiz", (0, 0, 0), (0, 0, 0.2))
hueso("Cadera", (0, 0, 0.40), (0, 0, 0.70), "Raiz")
hueso("Cuerpo", (0, 0, 0.70), (0, 0, 1.40), "Cadera")
# Anclajes del vestuario (sin pesos): Unity cuelga de aquí gorros y complementos.
#   AnclaGorro: en lo alto de la cabeza · AnclaCara: delante de los ojos · AnclaPecho: encima de la barriga
_ojo = cara(OJO_X, OJO_Z, -0.07)
hueso("AnclaGorro", (0, 0.0, ARRIBA - 0.09), (0, 0.0, ARRIBA + 0.09), "Cuerpo")
hueso("AnclaCara", (0, _ojo.y - 0.12, OJO_Z), (0, _ojo.y - 0.25, OJO_Z), "Cuerpo")
_pecho = delante(0.0, altura(-0.207), 0.0)
hueso("AnclaPecho", _pecho, _pecho + Vector((0, -0.12, 0)), "Cuerpo")
print("Anclas: gorro z =", round(ARRIBA - 0.09, 3), "· cara y =", round(_ojo.y - 0.12, 3), "· pecho =", tuple(round(v, 3) for v in _pecho))
for lado, s in LADOS:
    ojo = cara(OJO_X * lado, OJO_Z, 0.0)
    hueso(f"Ojo{s}", ojo, ojo + Vector((0, -0.12, 0)), "Cuerpo")
    hueso(f"Antena{s}1", espejo(ANTENA_BASE, lado), espejo(ANTENA_MEDIO, lado), "Cuerpo")
    hueso(f"Antena{s}2", espejo(ANTENA_MEDIO, lado), espejo(ANTENA_PUNTA, lado), f"Antena{s}1", True)
    hueso(f"Brazo{s}", espejo(HOMBRO, lado), espejo(CODO, lado), "Cuerpo")
    hueso(f"Antebrazo{s}", espejo(CODO, lado), espejo(MUNECA, lado), f"Brazo{s}", True)
    hueso(f"Mano{s}", espejo(MUNECA, lado), espejo(PUNTA_MANO, lado), f"Antebrazo{s}", True)
    hueso(f"Muslo{s}", espejo(CADERA, lado), espejo(RODILLA, lado), "Cadera")
    hueso(f"Espinilla{s}", espejo(RODILLA, lado), espejo(TOBILLO, lado), f"Muslo{s}", True)
    hueso(f"Pie{s}", espejo(TOBILLO, lado), espejo(PUNTA_PIE, lado), f"Espinilla{s}", True)
    if CON_ALAS:
        hueso(f"Ala{s}", espejo(ALA_BASE, lado), espejo(ALA_PUNTA, lado), "Cuerpo")
bpy.ops.object.mode_set(mode="OBJECT")

# ---------------------------------------------------------------------------
# Piezas (cada una con sus pesos; al final, una sola malla)
# ---------------------------------------------------------------------------

piezas = []


def nuevo(nombre, bm, materiales):
    me = bpy.data.meshes.new(nombre)
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    for m in materiales:
        me.materials.append(MAT[m])
    obj = bpy.data.objects.new(nombre, me)
    escena.collection.objects.link(obj)
    return obj


def congelar(obj):
    if not obj.modifiers:
        return
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    viejo = obj.data
    obj.modifiers.clear()
    obj.data = me
    bpy.data.meshes.remove(viejo)


def pesar_todo(obj, nombre_hueso):
    obj.vertex_groups.new(name=nombre_hueso).add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    piezas.append(obj)


def pesar_por_altura(obj, z0=0.55, z1=0.85):
    """Abajo, la cadera; arriba, el cuerpo; en medio, mezcla (se dobla como una gominola)."""
    abajo = obj.vertex_groups.new(name="Cadera")
    arriba = obj.vertex_groups.new(name="Cuerpo")
    for v in obj.data.vertices:
        k = max(0.0, min(1.0, (v.co.z - z0) / (z1 - z0)))
        k = k * k * (3 - 2 * k)
        if k < 0.999:
            abajo.add([v.index], 1 - k, "REPLACE")
        if k > 0.001:
            arriba.add([v.index], k, "REPLACE")
    piezas.append(obj)


def esfera(bm, centro, radios, indice=0, segmentos=28, anillos=14, rot=None):
    m = Matrix.Translation(Vector(centro))
    if rot is not None:
        m = m @ rot
    m = m @ Matrix.Diagonal((*radios, 1.0))
    r = bmesh.ops.create_uvsphere(bm, u_segments=segmentos, v_segments=anillos, radius=1.0, matrix=m)
    for f in {f for v in r["verts"] for f in v.link_faces}:
        f.material_index = indice
    return r["verts"]


def hacia(normal):
    """Giro que lleva el eje Z a esa dirección (para pegar cosas planas a la superficie)."""
    return Vector((0, 0, 1)).rotation_difference(Vector(normal).normalized()).to_matrix().to_4x4()


def deformar_pera(verts):
    for v in verts:
        k = ensanche(v.co.z)
        v.co.x *= k
        v.co.y *= k


# --- Cuerpo: el huevo (la cara va en él), o un tronco (o segmentos) y una cabeza aparte ---
bm = bmesh.new()
if TIPO == "oruga":
    SEGMENTOS = [(Vector((0, 0.03, 0.42)), Vector((0.40, 0.37, 0.27))), (Vector((0, -0.01, 0.74)), RADIOS)]
    for c, rr in SEGMENTOS:
        esfera(bm, c, rr, 0, 36, 18)
else:
    deformar_pera(esfera(bm, CENTRO, RADIOS, 0, 40, 24))
cuerpo = nuevo("Cuerpo", bm, ["Piel"])
pesar_por_altura(cuerpo)
if TIPO != "huevo":
    bm = bmesh.new()
    esfera(bm, CAB_C, CAB_R, 0, 40, 24)
    cabeza = nuevo("Cabeza", bm, ["Piel"])
    pesar_todo(cabeza, "Cuerpo")

def casquete(nombre, material, quitar, escala=1.06, grosor=0.045, cortes=()):
    """Una capa sobre el cuerpo (caparazón, rayas, capucha): se quitan los vértices que cumplan "quitar"
    y lo que quede al otro lado de cada corte (punto, normal: se quita hacia donde apunta), con borde limpio."""
    bm = bmesh.new()
    verts = esfera(bm, CENTRO + Vector((0, 0.01, 0.02)), RADIOS * escala, 0, 48, 48)
    deformar_pera(verts)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if quitar(v.co)], context="VERTS")
    for punto, normal in cortes:
        bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=Vector(punto), plane_no=Vector(normal), clear_outer=True)
    obj = nuevo(nombre, bm, [material])
    mod = obj.modifiers.new("Grosor", "SOLIDIFY")
    mod.thickness = grosor
    mod.offset = 1.0
    mod = obj.modifiers.new("Suave", "SUBSURF")
    mod.levels = 1
    congelar(obj)
    pesar_por_altura(obj)
    return obj


if ESPECIE == "bichete":
    # --- Caparazón de mariquita en la espalda, con raya y lunares ---
    casquete("Caparazon", "Caparazon", lambda c: c.y < 0.10 or c.z < altura(-0.79))
    bm = bmesh.new()
    for i in range(16):
        elev = -0.55 + 1.35 * i / 15  # de abajo (espalda baja) a arriba (nuca)
        d = Vector((0, math.cos(elev), math.sin(elev)))
        p = superficie(d) + d * 0.075
        esfera(bm, p, (0.018, 0.018, 0.05), 0, 10, 6, rot=hacia(d) @ Matrix.Rotation(math.pi / 2, 4, "X"))
    raya = nuevo("Raya", bm, ["Mancha"])
    pesar_por_altura(raya)

    bm = bmesh.new()
    for lado, _ in LADOS:
        for azimut, elev, radio in ((35, 0.35, 0.10), (60, -0.05, 0.085), (28, -0.3, 0.075), (70, 0.45, 0.06)):
            a = math.radians(azimut)
            d = Vector((math.sin(a) * lado * math.cos(elev), math.cos(a) * math.cos(elev), math.sin(elev)))
            p = superficie(d) + d * 0.07
            esfera(bm, p, (radio, radio, 0.025), 0, 16, 8, rot=hacia(d))
    lunares = nuevo("Lunares", bm, ["Mancha"])
    pesar_por_altura(lunares)

if ESPECIE == "escarabajo":
    # --- Caparazón enorme partido en dos élitros que le cubren espalda y costados ---
    for lado, s in LADOS:
        casquete(f"Elitro{s}", "Caparazon", lambda c: False, escala=1.1, grosor=0.08,
                 cortes=(((lado * 0.025, 0, 0), (-lado, 0, 0)), ((0, -0.1, 0), (0, -1, 0)), ((0, 0, altura(-0.88)), (0, 0, -1))))

if ESPECIE == "abeja":
    # --- Abdomen: un bombo grande detrás, a rayas amarillas y negras, que acaba en punta (con el
    # aguijón). Va con la cadera y no se tiñe: es lo que la hace abeja aunque el cuerpo sea del
    # color del jugador. ---
    ABD_EJE = Vector((0, math.cos(math.radians(-15)), math.sin(math.radians(-15))))   # hacia atrás y un poco abajo
    ABD_LARGO, ABD_ANCHO, ABD_ALTO = 0.6, 0.52, 0.44
    ABD_CENTRO = Vector((0, RADIOS.y * 0.55, altura(0.3))) + ABD_EJE * ABD_LARGO * 0.95
    _arriba = (Vector((0, 0, 1)) - ABD_EJE * ABD_EJE.z).normalized()
    # Los polos de la esfera, en las puntas del abdomen: los anillos quedan de través y las rayas, rectas.
    _giro = Matrix((Vector((1, 0, 0)), ABD_EJE.cross(Vector((1, 0, 0))), ABD_EJE)).transposed().to_4x4()
    bm = bmesh.new()
    esfera(bm, ABD_CENTRO, (ABD_ANCHO, ABD_ALTO, ABD_LARGO), 0, 40, 36, rot=_giro)
    for v in bm.verts:                                     # más fino hacia la punta, como una gota
        d = v.co - ABD_CENTRO
        k = d.dot(ABD_EJE) / ABD_LARGO
        if k > 0:
            a_lo_largo = ABD_EJE * d.dot(ABD_EJE)
            v.co = ABD_CENTRO + a_lo_largo + (d - a_lo_largo) * (1 - 0.5 * k * k)
    for cara_ in bm.faces:                                 # rayas: amarillo, negro, amarillo… y la punta negra
        k = (cara_.calc_center_median() - ABD_CENTRO).dot(ABD_EJE) / ABD_LARGO
        cara_.material_index = int(max(0.0, min(5.99, (k + 1.0) * 3.0))) % 2
    abdomen = nuevo("Abdomen", bm, ["Concha", "Mancha"])
    pesar_todo(abdomen, "Cadera")

    # --- Raya negra en el tronco y collar de pelusa en el cuello (entre la cabeza y el tronco) ---
    casquete("RayaTronco", "Mancha", lambda c: c.z < altura(-0.35) or c.z > altura(0.2), escala=1.04, grosor=0.02)
    bm = bmesh.new()
    for i in range(16):
        a = 2 * math.pi * i / 16
        p = Vector((math.cos(a) * 0.4, math.sin(a) * 0.37 + 0.01, altura(0.72) + 0.02 * math.sin(4 * a)))
        r = 0.12 + 0.02 * math.sin(3 * a)
        esfera(bm, p, (r, r, r * 0.9), 0, 14, 8)
    collar = nuevo("CollarPelusa", bm, ["Pelo"])
    pesar_por_altura(collar)
    # --- Pelusa en lo alto de la cabeza ---
    bm = bmesh.new()
    for x, y, z, r in ((0.0, 0.02, 0.05, 0.12), (-0.1, 0.06, 0.0, 0.09), (0.1, 0.06, 0.0, 0.09), (0.0, -0.08, 0.0, 0.08)):
        esfera(bm, (x, y, ARRIBA + z - 0.04), (r, r, r * 0.85), 0, 16, 8)
    pelusa = nuevo("Pelusa", bm, ["Pelo"])
    pesar_todo(pelusa, "Cuerpo")

if ESPECIE == "hormiga":
    # --- Culete gordo detrás (unido por una cintura finísima, más abajo con los tubos) ---
    GASTER_EJE = Vector((0, math.cos(math.radians(-20)), math.sin(math.radians(-20))))
    GASTER_CENTRO = Vector((0, RADIOS.y + 0.5, altura(-0.05)))
    _giro = Matrix((Vector((1, 0, 0)), GASTER_EJE.cross(Vector((1, 0, 0))), GASTER_EJE)).transposed().to_4x4()
    bm = bmesh.new()
    esfera(bm, GASTER_CENTRO, (0.36, 0.34, 0.44), 0, 36, 30, rot=_giro)
    for cara_ in bm.faces:                                 # anillos más oscuros
        k = (cara_.calc_center_median() - GASTER_CENTRO).dot(GASTER_EJE) / 0.44
        cara_.material_index = 1 if -0.35 < k < -0.2 or 0.15 < k < 0.3 else 0
    gaster = nuevo("Culete", bm, ["Caparazon", "Mancha"])
    pesar_todo(gaster, "Cadera")

if ESPECIE == "arana":
    # --- Culete enorme detrás y por encima, con manchas naranjas, y pelos de punta en la cabeza ---
    ABD_C = Vector((0, RADIOS.y + 0.3, altura(0.55)))
    bm = bmesh.new()
    esfera(bm, ABD_C, (0.5, 0.5, 0.47), 0, 40, 24)
    culete = nuevo("Culete", bm, ["Caparazon"])
    pesar_todo(culete, "Cadera")
    bm = bmesh.new()
    for azimut, elev, radio in ((0, 0.55, 0.13), (35, 0.2, 0.09), (-35, 0.2, 0.09), (0, 0.0, 0.11), (60, 0.5, 0.07), (-60, 0.5, 0.07)):
        a = math.radians(azimut)
        d = Vector((math.sin(a) * math.cos(elev), math.cos(a) * math.cos(elev), math.sin(elev)))
        p = ABD_C + Vector((d.x * 0.5, d.y * 0.5, d.z * 0.47)) * 0.985
        esfera(bm, p, (radio, radio, 0.03), 0, 16, 8, rot=hacia(d))
    manchas = nuevo("Manchas", bm, ["Concha"])
    pesar_todo(manchas, "Cadera")
    bm = bmesh.new()
    for x, y, inclina in ((0.0, 0.05, 0.0), (-0.13, 0.08, -0.4), (0.13, 0.08, 0.4), (-0.07, 0.18, -0.2), (0.07, 0.18, 0.2)):
        base = Vector((x, y, ARRIBA - 0.05))
        esfera(bm, base + Vector((inclina * 0.12, 0, 0.1)), (0.035, 0.035, 0.13), 0, 10, 8,
               rot=Matrix.Rotation(inclina, 4, "Y"))
    pelos = nuevo("Pelos", bm, ["Pelo"])
    pesar_todo(pelos, "Cuerpo")

if ESPECIE == "oruga":
    # --- Lunares amarillos en los segmentos y una cresta en la cabeza ---
    bm = bmesh.new()
    for c, rr in SEGMENTOS:
        for azimut, elev, radio in ((90, 0.1, 0.07), (-90, 0.1, 0.07), (150, 0.35, 0.06), (-150, 0.35, 0.06), (180, 0.0, 0.08)):
            a = math.radians(azimut)
            d = Vector((math.sin(a) * math.cos(elev), -math.cos(a) * math.cos(elev), math.sin(elev)))
            p = c + Vector((d.x * rr.x, d.y * rr.y, d.z * rr.z)) * 0.99
            esfera(bm, p, (radio, radio, 0.025), 0, 14, 8, rot=hacia(d))
    lunares = nuevo("Lunares", bm, ["Concha"])
    pesar_por_altura(lunares)
    bm = bmesh.new()
    for y, z, r in ((-0.12, 0.0, 0.08), (0.0, 0.03, 0.1), (0.13, 0.0, 0.08)):
        esfera(bm, (0, y + CAB_C.y, ARRIBA + z - 0.03), (r * 0.8, r, r), 0, 14, 8)
    cresta = nuevo("Cresta", bm, ["Concha"])
    pesar_todo(cresta, "Cuerpo")

if ESPECIE == "saltamontes":
    # --- Alas plegadas a la espalda, como una capa larga ---
    bm = bmesh.new()
    for lado, _ in LADOS:
        c = Vector((lado * 0.11, RADIOS.y + 0.04, altura(0.05)))
        esfera(bm, c, (0.15, 0.05, 0.5), 0, 20, 16,
               rot=Matrix.Rotation(math.radians(-14), 4, "X") @ Matrix.Rotation(lado * math.radians(-6), 4, "Y"))
    alas = nuevo("AlasPlegadas", bm, ["Caparazon"])
    pesar_por_altura(alas)

if ESPECIE == "luciernaga":
    # --- Capucha oscura y un farolillo enorme que brilla en el culete ---
    casquete("Capucha", "Mancha", lambda c: c.z < altura(0.72), escala=1.03, grosor=0.03)
    farol_centro = Vector((0, RADIOS.y + 0.16, altura(-0.6)))
    bm = bmesh.new()
    esfera(bm, farol_centro, (0.36, 0.42, 0.36), 0, 32, 16)
    farol = nuevo("Farol", bm, ["Luz"])
    pesar_todo(farol, "Cadera")
    bm = bmesh.new()
    for i, (dy, r, rz) in enumerate(((-0.2, 0.37, 0.33), (0.08, 0.385, 0.35))):   # aros oscuros: parece un farolillo
        esfera(bm, farol_centro + Vector((0, dy, 0)), (r, 0.05, rz), 0, 32, 10)
    aro = nuevo("AroFarol", bm, ["Mancha"])
    pesar_todo(aro, "Cadera")

if CON_ALAS:
    # --- Alas (cada lado con su hueso; la abeja lleva dos pares grandes) ---
    for lado, s in LADOS:
        bm = bmesh.new()
        base = espejo(ALA_BASE, lado)
        direccion = (espejo(ALA_PUNTA, lado) - base).normalized()
        pares = ((0.44, 0.22, 0.0), (0.3, 0.15, -0.2)) if ESPECIE == "abeja" else ((0.36, 0.15, 0.0), (0.26, 0.11, -0.14))
        for largo, ancho, baja in pares:
            centro = base + direccion * (largo * 0.95) + Vector((0, 0, baja))
            arriba = (Vector((0, 0, 1)) - direccion * direccion.z).normalized()
            lado_ala = arriba.cross(direccion)
            giro = Matrix((direccion, lado_ala, arriba)).transposed().to_4x4()   # X: hacia la punta; Z: hacia arriba
            esfera(bm, centro, (largo, 0.012, ancho), 0, 24, 10, rot=giro)
        ala = nuevo(f"Ala{s}", bm, ["Ala"])
        pesar_todo(ala, f"Ala{s}")


# --- Barriga ---
if D["barriga"]:
    bm = bmesh.new()
    p = delante(0.0, altura(-0.483), -0.06)
    esfera(bm, p, (0.54 * RADIOS.x, 0.1, 0.4 * RADIOS.z), 0, 28, 14)
    barriga = nuevo("Barriga", bm, ["Barriga"])
    pesar_por_altura(barriga)


# --- Ojos (hueso propio, para parpadear) ---
def ojo_entero(bm, centro, k, lado):
    """Ojo blanco con pupila y dos brillos, mirando al frente (-Y); k = tamaño."""
    esfera(bm, centro, (0.145 * k, 0.11 * k, 0.165 * k), 0)
    pupila = centro + Vector((-0.012 * lado, -0.1 * k, -0.015 * k))
    esfera(bm, pupila, (0.075 * k, 0.03 * k, 0.09 * k), 1, 20, 10)
    esfera(bm, pupila + Vector((0.025, -0.022, 0.035)) * k, (0.026 * k, 0.012 * k, 0.026 * k), 2, 12, 6)
    esfera(bm, pupila + Vector((-0.02, -0.024, -0.035)) * k, (0.012 * k, 0.008 * k, 0.012 * k), 2, 10, 6)


def ojo_insecto(bm, centro, k, lado):
    """Ojo de insecto: un óvalo grande negro y brillante, sin blanco, con dos reflejos."""
    esfera(bm, centro, (0.15 * k, 0.11 * k, 0.19 * k), 1, 28, 14)
    esfera(bm, centro + Vector((-0.04, -0.095, 0.075)) * k, (0.045 * k, 0.018 * k, 0.055 * k), 2, 14, 8)
    esfera(bm, centro + Vector((0.035, -0.1, -0.06)) * k, (0.02 * k, 0.01 * k, 0.022 * k), 2, 10, 6)


if ESPECIE != "caracol":   # (el caracol los lleva en la punta de las antenas)
    for lado, s in LADOS:
        bm = bmesh.new()
        if D["ojos"] == "insecto":
            # Un poco hacia los lados, como los de una abeja de verdad.
            c = cara(OJO_X * lado, OJO_Z, -0.06) + Vector((lado * 0.03, 0.0, 0.0))
            ojo_insecto(bm, c, OJOS, lado)
        else:
            ojo_entero(bm, cara(OJO_X * lado, OJO_Z, -0.07), OJOS, lado)
        ojo = nuevo(f"Ojo{s}", bm, ["Ojo", "Pupila", "Brillo"])
        pesar_todo(ojo, f"Ojo{s}")
    if D["ojos"] == "seis":
        # La araña: cuatro ojitos más, encima de los grandes.
        bm = bmesh.new()
        for lado, _ in LADOS:
            for x, dz, k in ((0.09, 0.2, 0.45), (0.25, 0.15, 0.4)):
                ojo_entero(bm, cara(x * lado, OJO_Z + dz, -0.03), k * OJOS, lado)
        ojitos = nuevo("Ojitos", bm, ["Ojo", "Pupila", "Brillo"])
        pesar_todo(ojitos, "Cuerpo")

# --- Boca y coloretes ---
bm = bmesh.new()
p = cara(0.0, BOCA_Z, 0.0)
if D["boca"] in ("abierta", "dientes"):
    for v in esfera(bm, p, (0.15, 0.03, 0.085), 0, 24, 12):                     # hueco de la boca
        if v.co.z > p.z:
            v.co.z = p.z + (v.co.z - p.z) * 0.18                                   # arriba plana: una "D" sonriente
    esfera(bm, p + Vector((0.0, -0.012, -0.045)), (0.075, 0.025, 0.035), 1, 16, 8)  # lengua
    if D["boca"] == "dientes":
        for lado, _ in LADOS:                                                      # dientes de conejo
            esfera(bm, p + Vector((lado * 0.033, -0.022, -0.035)), (0.03, 0.018, 0.045), 2, 12, 8)
    boca = nuevo("Boca", bm, ["Boca", "Lengua", "Ojo"])
    pesar_por_altura(boca)
else:
    # Sonrisa cerrada: una curva fina.
    puntos = 17
    for i in range(puntos):
        x = (-1 + 2 * i / (puntos - 1)) * 0.11
        q = cara(x, BOCA_Z - 0.035 * (1 - (x / 0.11) ** 2), -0.005)
        esfera(bm, q, (0.024, 0.02, 0.022), 0, 12, 6)
    boca = nuevo("Boca", bm, ["Boca"])
    pesar_por_altura(boca)
for lado, s in LADOS:
    bm = bmesh.new()
    p = cara(0.64 * CAB_R.x * lado, RUBOR_Z, -0.005)
    esfera(bm, p, (0.075, 0.025, 0.045), 0, 16, 8, rot=hacia(Vector((0.55 * lado, -1, 0))) @ Matrix.Rotation(math.pi / 2, 4, "X"))
    rubor = nuevo(f"Rubor{s}", bm, ["Rubor"])
    pesar_por_altura(rubor)
# --- Manoplas y zapatillas ---
for lado, s in LADOS:
    bm = bmesh.new()
    c = espejo(HOMBRO + Vector((0.22, -0.1, -0.36)), lado)
    g = 1.0 + (GROSOR - 1.0) * 0.6
    esfera(bm, c, (0.11 * g, 0.1 * g, 0.115 * g), 0)
    esfera(bm, c + Vector((-0.05 * lado, -0.07, 0.045)) * g, (0.045 * g, 0.04 * g, 0.05 * g), 0, 16, 8)
    mano = nuevo(f"Mano{s}", bm, ["Guante"])
    pesar_todo(mano, f"Mano{s}")

    bm = bmesh.new()
    pie = espejo(Vector((TOBILLO.x, 0, 0)), lado)
    esfera(bm, pie + Vector((0, -0.06, 0.1)), (0.13, 0.2, 0.095), 0)
    esfera(bm, pie + Vector((0, -0.17, 0.085)), (0.115, 0.1, 0.075), 0)
    suela = esfera(bm, pie + Vector((0, -0.07, 0.035)), (0.135, 0.215, 0.035), 1, 24, 10)  # suela redondeada
    zapatilla = nuevo(f"Zapatilla{s}", bm, ["Bota", "Suela"])
    mod = zapatilla.modifiers.new("Bisel", "BEVEL")
    mod.width = 0.02
    mod.segments = 3
    mod.limit_method = "ANGLE"
    mod = zapatilla.modifiers.new("Suave", "SUBSURF")
    mod.levels = 1
    congelar(zapatilla)
    pesar_todo(zapatilla, f"Pie{s}")

# --- Brazos, patas y antenas: tubos con pesos que se reparten en las articulaciones ---


def catmull(p0, p1, p2, p3, t):
    t2, t3 = t * t, t * t * t
    return 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3)


def tubo(nombre, puntos, radio, pesos_por_t, anillos=24, lados=12, radio_final=None, mat="Piel"):
    ext = [puntos[0] + (puntos[0] - puntos[1])] + puntos + [puntos[-1] + (puntos[-1] - puntos[-2])]
    tramos = len(puntos) - 1
    centros = []
    for i in range(anillos + 1):
        u = i / anillos * tramos
        k = min(int(u), tramos - 1)
        centros.append(catmull(ext[k], ext[k + 1], ext[k + 2], ext[k + 3], u - k))
    bm = bmesh.new()
    aros = []
    normal_prev = None
    for i, c in enumerate(centros):
        tangente = (centros[min(i + 1, anillos)] - centros[max(i - 1, 0)]).normalized()
        if normal_prev is None:
            normal_prev = tangente.cross(Vector((0, 1, 0.3))).normalized()
        normal = (normal_prev - tangente * normal_prev.dot(tangente)).normalized()
        binormal = tangente.cross(normal)
        normal_prev = normal
        rr = radio if radio_final is None else radio + (radio_final - radio) * i / anillos
        aros.append([bm.verts.new(c + (normal * math.cos(2 * math.pi * j / lados) + binormal * math.sin(2 * math.pi * j / lados)) * rr)
                     for j in range(lados)])
    for i in range(anillos):
        for j in range(lados):
            k = (j + 1) % lados
            bm.faces.new((aros[i][j], aros[i][k], aros[i + 1][k], aros[i + 1][j]))
    for aro in (aros[0], aros[-1]):
        bm.faces.new(aro)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.verts.index_update()
    indices = [[v.index for v in aro] for aro in aros]
    obj = nuevo(nombre, bm, [mat])
    grupos = {}
    for i, idx in enumerate(indices):
        for nombre_hueso, peso in pesos_por_t(i / anillos):
            if peso <= 0.001:
                continue
            if nombre_hueso not in grupos:
                grupos[nombre_hueso] = obj.vertex_groups.new(name=nombre_hueso)
            grupos[nombre_hueso].add(idx, peso, "REPLACE")
    piezas.append(obj)
    return obj


def mezcla(t, t0, ancho, a, b):
    k = max(0.0, min(1.0, (t - (t0 - ancho)) / (2 * ancho)))
    k = k * k * (3 - 2 * k)
    return [(a, 1 - k), (b, k)]


for lado, s in LADOS:
    pts = [espejo(HOMBRO + Vector((-0.08, 0.0, 0.02)), lado), espejo(HOMBRO, lado), espejo(CODO, lado), espejo(MUNECA, lado),
           espejo(HOMBRO + Vector((0.21, -0.1, -0.34)), lado)]
    # Brazos y patas: del color del cuerpo, o negros de insecto (abeja, hormiga, araña).
    extremidad = D["extremidad"]
    tubo(f"Brazo{s}", pts, 0.062 * GROSOR,
         lambda t, s=s: mezcla(t, 0.15, 0.08, "Cuerpo", f"Brazo{s}") if t < 0.3 else
         (mezcla(t, 0.52, 0.09, f"Brazo{s}", f"Antebrazo{s}") if t < 0.85 else mezcla(t, 0.9, 0.05, f"Antebrazo{s}", f"Mano{s}")),
         radio_final=0.055 * GROSOR, mat=extremidad)

    pts = [espejo(Vector((0.16 * ANCHO, 0.0, 0.50)), lado), espejo(CADERA, lado), espejo(RODILLA, lado), espejo(TOBILLO, lado),
           espejo(Vector((TOBILLO.x, 0.0, 0.08)), lado)]
    tubo(f"Pata{s}", pts, 0.075 * GROSOR,
         lambda t, s=s: mezcla(t, 0.15, 0.08, "Cadera", f"Muslo{s}") if t < 0.3 else
         (mezcla(t, 0.5, 0.08, f"Muslo{s}", f"Espinilla{s}") if t < 0.85 else mezcla(t, 0.88, 0.05, f"Espinilla{s}", f"Pie{s}")),
         mat=extremidad)

    if not D["antenas"]:
        continue   # la araña no tiene (los huesos sí, sin malla)
    pts = [espejo(ANTENA_BASE + Vector((0, 0.02, -0.08)), lado), espejo(ANTENA_BASE, lado), espejo(ANTENA_MEDIO, lado),
           espejo(ANTENA_PUNTA, lado)]
    grosor_antena = {"caracol": 0.045, "escarabajo": 0.032}.get(ESPECIE, 0.026)
    tubo(f"Antena{s}", pts, grosor_antena,
         lambda t, s=s: mezcla(t, 0.2, 0.08, "Cuerpo", f"Antena{s}1") if t < 0.45 else mezcla(t, 0.62, 0.1, f"Antena{s}1", f"Antena{s}2"),
         anillos=18, lados=10, radio_final=grosor_antena * 0.8, mat=D["mat_antena"])
    bm = bmesh.new()
    if ESPECIE == "caracol":
        # Los ojos del caracol, en lo alto de las antenas.
        ojo_entero(bm, espejo(ANTENA_PUNTA, lado) + Vector((0, -0.02, 0.06)), 0.8, lado)
        bolita = nuevo(f"OjoAntena{s}", bm, ["Ojo", "Pupila", "Brillo"])
    else:
        radio_bolita = {"abeja": 0.065, "escarabajo": 0.05, "luciernaga": 0.075, "hormiga": 0.035,
                        "saltamontes": 0.025, "oruga": 0.085}.get(ESPECIE, 0.07)
        esfera(bm, espejo(ANTENA_PUNTA, lado), (radio_bolita, radio_bolita, radio_bolita), 0, 20, 10)
        bolita = nuevo(f"Bolita{s}", bm, [{"bichete": "Piel", "luciernaga": "Luz", "oruga": "Concha",
                                           "saltamontes": "Piel"}.get(ESPECIE, "Mancha")])
    pesar_todo(bolita, f"Antena{s}2")

if ESPECIE == "abeja":
    _punta = ABD_CENTRO + ABD_EJE * ABD_LARGO * 0.9
    puntas = [_punta, _punta + ABD_EJE * 0.14, _punta + ABD_EJE * 0.28]
    tubo("Aguijon", puntas, 0.08, lambda t: [("Cadera", 1.0)], anillos=10, lados=12, radio_final=0.005, mat="Mancha")

if ESPECIE == "hormiga":
    # Cintura finísima hasta el culete, cuello y mandíbulas.
    cintura = [Vector((0, RADIOS.y * 0.7, altura(-0.1))), Vector((0, RADIOS.y + 0.12, altura(-0.15))),
               GASTER_CENTRO - GASTER_EJE * 0.36]
    tubo("Cintura", cintura, 0.07, lambda t: [("Cadera", 1.0)], anillos=10, lados=12, mat="Caparazon")
    cuello = [Vector((0, -0.02, altura(0.7))), Vector((0, -0.05, CAB_C.z - CAB_R.z * 0.6))]
    tubo("Cuello", cuello, 0.11, lambda t: [("Cuerpo", 1.0)], anillos=6, lados=14)
    for lado, s in LADOS:
        m0 = cara(lado * 0.17, BOCA_Z - 0.02, -0.02)
        mandibula = [m0, m0 + Vector((lado * 0.05, -0.09, -0.03)), m0 + Vector((-lado * 0.05, -0.15, -0.06))]
        tubo(f"Mandibula{s}", mandibula, 0.045, lambda t: [("Cuerpo", 1.0)], anillos=10, lados=10, radio_final=0.012, mat="Mancha")

if ESPECIE == "arana":
    # Cuatro patas más (dos por lado), finas y con la rodilla alta, y dos colmillos.
    for lado, s in LADOS:
        for i, y in enumerate((-0.16, 0.16)):
            p0 = Vector((lado * RADIOS.x * 0.8, y, CENTRO.z - 0.06))
            pata = [p0, Vector((lado * (RADIOS.x + 0.3), y * 1.7, CENTRO.z + 0.32)),
                    Vector((lado * (RADIOS.x + 0.52), y * 2.4, 0.3)), Vector((lado * (RADIOS.x + 0.56), y * 2.6, 0.04))]
            tubo(f"PataExtra{s}{i}", pata, 0.055, lambda t: [("Cadera", 1.0)], anillos=20, lados=10, radio_final=0.03, mat="Mancha")
    for lado, s in LADOS:
        c0 = cara(lado * 0.06, BOCA_Z - 0.01, 0.0)
        tubo(f"Colmillo{s}", [c0, c0 + Vector((0, -0.03, -0.05)), c0 + Vector((0, -0.02, -0.11))], 0.032,
             lambda t: [("Cuerpo", 1.0)], anillos=8, lados=10, radio_final=0.004, mat="Ojo")

if ESPECIE == "saltamontes":
    # Las patas saltadoras: muslo gordo con la rodilla por encima del cuerpo, y la espinilla hasta el suelo.
    for lado, s in LADOS:
        cadera_ = Vector((lado * (RADIOS.x * 0.8), RADIOS.y * 0.4, altura(-0.55)))
        rodilla_ = Vector((lado * (RADIOS.x + 0.12), RADIOS.y + 0.18, altura(0.95)))
        tobillo_ = Vector((lado * (RADIOS.x + 0.16), RADIOS.y + 0.42, 0.08))
        tubo(f"Muslo{s}Salto", [cadera_, (cadera_ + rodilla_) * 0.5 + Vector((lado * 0.05, 0.06, 0)), rodilla_], 0.16,
             lambda t: [("Cadera", 1.0)], anillos=16, lados=14, radio_final=0.06)
        tubo(f"Espinilla{s}Salto", [rodilla_, (rodilla_ + tobillo_) * 0.5 + Vector((0, 0.05, 0)), tobillo_], 0.055,
             lambda t: [("Cadera", 1.0)], anillos=16, lados=10, radio_final=0.035, mat="Mancha")
        bm = bmesh.new()
        esfera(bm, rodilla_, (0.075, 0.075, 0.075), 0, 14, 8)
        rod = nuevo(f"Rodilla{s}", bm, ["Mancha"])
        pesar_todo(rod, "Cadera")

if ESPECIE == "oruga":
    # Patitas de bolita a los lados de los segmentos.
    bm = bmesh.new()
    for c, rr in SEGMENTOS:
        for lado, _ in LADOS:
            for dy in (-0.12, 0.12):
                esfera(bm, c + Vector((lado * rr.x * 0.92, dy, -rr.z * 0.45)), (0.07, 0.07, 0.06), 0, 12, 8)
    patitas = nuevo("Patitas", bm, ["Mancha"])
    pesar_por_altura(patitas)

if ESPECIE == "escarabajo":
    # Cejo gordo y oscuro encima de los ojos: cara de forzudo gruñón.
    cejo = []
    for i in range(7):
        x = (-1 + 2 * i / 6) * OJO_X * 1.55
        cejo.append(delante(x, OJO_Z + 0.16 - 0.05 * abs(x) / OJO_X, -0.03))
    tubo("Cejo", cejo, 0.05, lambda t: [("Cuerpo", 1.0)], anillos=24, lados=12, mat="Mancha")
    # Cuerno de rinoceronte, grande y curvado hacia arriba, en la frente.
    base = delante(0.0, altura(0.66), 0.0) + Vector((0, 0.08, 0))
    cuerno = [base, base + Vector((0, -0.18, 0.14)), base + Vector((0, -0.26, 0.38)), base + Vector((0, -0.16, 0.6))]
    tubo("Cuerno", cuerno, 0.13, lambda t: [("Cuerpo", 1.0)], anillos=18, lados=16, radio_final=0.02, mat="Mancha")

if ESPECIE == "caracol":
    # Concha en espiral a la espalda, grande (asoma por encima de la cabeza): un tubo que se
    # enrosca hacia dentro y adelgaza, con la espiral marcada en oscuro a los dos lados.
    centro_concha = Vector((0, RADIOS.y + 0.24, CENTRO.z + 0.12))
    vueltas, r0, caida = 3.2 * math.pi, 0.5, 0.17
    grueso0, grueso1 = 0.3, 0.04
    ANCHO_CONCHA = 1.55   # más ancha que alta de sección: desde atrás se ve como una concha, no como una rueda

    def espiral(u):
        a = u * vueltas
        r = r0 * math.exp(-caida * a)
        return centro_concha + Vector((0, r * math.sin(a), -r * math.cos(a)))

    tubo("Concha", [espiral(i / 14) for i in range(15)], grueso0, lambda t: [("Cuerpo", 1.0)],
         anillos=60, lados=20, radio_final=grueso1, mat="Concha")
    concha = piezas[-1]
    for v in concha.data.vertices:
        v.co.x *= ANCHO_CONCHA
    for lado, s in LADOS:
        linea = [espiral(i / 40) + Vector((lado * 0.95 * ANCHO_CONCHA * (grueso0 + (grueso1 - grueso0) * i / 40), 0, 0)) for i in range(41)]
        tubo(f"Espiral{s}", linea, 0.03, lambda t: [("Cuerpo", 1.0)], anillos=80, lados=8, radio_final=0.012, mat="Mancha")

# ---------------------------------------------------------------------------
# Una sola malla, materiales en orden fijo, con el esqueleto
# ---------------------------------------------------------------------------

bpy.ops.object.select_all(action="DESELECT")
for p in piezas:
    p.select_set(True)
bpy.context.view_layer.objects.active = cuerpo
bpy.ops.object.join()
malla = cuerpo
malla.name = malla.data.name = "Bichete" if ESPECIE == "bichete" else ESPECIE.capitalize()

viejos = [m.name.split(".")[0] for m in malla.data.materials]
nuevos_indices = [ORDEN_MATERIALES.index(viejos[p.material_index]) for p in malla.data.polygons]
malla.data.materials.clear()
for n in ORDEN_MATERIALES:
    malla.data.materials.append(MAT[n])
malla.data.polygons.foreach_set("material_index", nuevos_indices)
malla.data.update()
print("Caras por material:", {n: nuevos_indices.count(i) for i, n in enumerate(ORDEN_MATERIALES)})

malla.parent = esqueleto
malla.modifiers.new("Esqueleto", "ARMATURE").object = esqueleto

# ---------------------------------------------------------------------------
# Animaciones (rotaciones en ejes del mundo en reposo: X adelante/atrás, Y de lado, Z girar)
# ---------------------------------------------------------------------------

esqueleto.animation_data_create()
REPOSO = {}
for pb in esqueleto.pose.bones:
    pb.rotation_mode = "QUATERNION"
    REPOSO[pb.name] = pb.bone.matrix_local.to_3x3()
acciones = []


def poner(nombre_hueso, cuadro, rot=(0, 0, 0), loc=(0, 0, 0)):
    pb = esqueleto.pose.bones[nombre_hueso]
    r = REPOSO[nombre_hueso]
    giro = Euler([math.radians(v) for v in rot], "XYZ").to_matrix()
    pb.rotation_quaternion = (r.inverted() @ giro @ r).to_quaternion()
    pb.location = r.inverted() @ Vector(loc)
    pb.keyframe_insert("rotation_quaternion", frame=cuadro)
    pb.keyframe_insert("location", frame=cuadro)


def animacion(nombre, cuadros, pose, paso=2):
    accion = bpy.data.actions.new(nombre)
    accion.use_fake_user = True
    esqueleto.animation_data.action = accion
    for pb in esqueleto.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    for c in list(range(0, cuadros, paso)) + [cuadros]:
        t = (c % cuadros) / cuadros
        pose(c, t, 2 * math.pi * t)
    try:
        accion.use_frame_range = True
        accion.frame_range = (0, cuadros)
    except Exception:
        pass
    acciones.append(accion)


def brazo(c, s, lado, abrir=12.0, adelante=0.0, codo=15.0, antebrazo_abrir=0.0):
    poner(f"Brazo{s}", c, (-adelante, -lado * abrir, 0))
    poner(f"Antebrazo{s}", c, (-codo, -lado * antebrazo_abrir, 0))
    poner(f"Mano{s}", c, (0, 0, 0))


def pata(c, s, lado, adelante=0.0, rodilla=0.0, abrir=0.0):
    poner(f"Muslo{s}", c, (-adelante, -lado * abrir, 0))
    poner(f"Espinilla{s}", c, (rodilla, 0, 0))
    poner(f"Pie{s}", c, (adelante - rodilla, 0, 0))


def pose_quieto(c, t, ph):
    poner("Cadera", c, (0, 0, 0), (0, 0, -0.01 - 0.012 * math.sin(ph)))
    poner("Cuerpo", c, (2.5 * math.sin(ph), 3.0 * math.sin(ph + 1), 2.0 * math.sin(ph * 0.5 * 2)))
    for lado, s in LADOS:
        brazo(c, s, lado, abrir=14 + 4 * math.sin(ph + lado), adelante=4 * math.sin(ph), codo=20 + 6 * math.sin(ph))
        pata(c, s, lado)


animacion("Quieto", 60, pose_quieto, 5)


# Andar: pasitos cortos y contoneo de pato (la cadera se balancea hacia la pata que pisa)
def pose_andar(c, t, ph, zancada=32.0, rodilla=40.0, bote=0.05, contoneo=8.0, inclinacion=5.0, brazos=32.0, codo=20.0, giro=8.0):
    poner("Cadera", c, (0, contoneo * math.sin(ph), giro * math.sin(ph)), (0, 0, -bote * abs(math.sin(ph))))
    poner("Cuerpo", c, (inclinacion + 3 * math.cos(2 * ph), -0.6 * contoneo * math.sin(ph), -1.2 * giro * math.sin(ph)))
    for lado, s in LADOS:
        fase = ph + (0.0 if lado < 0 else math.pi)
        pata(c, s, lado, adelante=zancada * math.sin(fase), rodilla=10 + rodilla * max(0.0, math.cos(fase)))
        brazo(c, s, lado, abrir=18, adelante=-brazos * math.sin(fase), codo=codo + 15 * max(0.0, -math.sin(fase)))


animacion("Andar", 20, pose_andar)


def pose_correr(c, t, ph):
    pose_andar(c, t, ph, zancada=52.0, rodilla=75.0, bote=0.08, contoneo=6.0, inclinacion=16.0, brazos=55.0, codo=60.0, giro=12.0)


animacion("Correr", 14, pose_correr)


def pose_saltar(c, t, ph):
    poner("Cadera", c, (0, 0, 0), (0, 0, 0.03))
    poner("Cuerpo", c, (-8 + 2 * math.sin(ph), 0, 0))
    for lado, s in LADOS:
        brazo(c, s, lado, abrir=75 + 8 * math.sin(ph + lado), adelante=10, codo=10, antebrazo_abrir=30)
        pata(c, s, lado, adelante=35 + 5 * math.sin(ph + lado), rodilla=70)


animacion("Saltar", 20, pose_saltar, 5)


def pose_caer(c, t, ph):
    poner("Cadera", c, (0, 0, 0), (0, 0, 0.03))
    poner("Cuerpo", c, (6 * math.sin(ph), 5 * math.sin(2 * ph), 0))
    for lado, s in LADOS:
        brazo(c, s, lado, abrir=95 + 30 * math.sin(2 * ph + lado), adelante=30 * math.sin(2 * ph + lado * 1.5), codo=20, antebrazo_abrir=25)
        pata(c, s, lado, adelante=30 * math.sin(2 * ph + lado * 1.6), rodilla=35 + 30 * math.sin(2 * ph + lado))


animacion("Caer", 12, pose_caer)


def pose_tumbado(c, t, ph):
    poner("Cuerpo", c, (8 * math.sin(2 * ph), 12 * math.sin(ph), 0))
    for lado, s in LADOS:
        brazo(c, s, lado, abrir=70 + 50 * math.sin(2 * ph + lado), adelante=55 * math.sin(2 * ph + lado * 1.3), codo=40 + 30 * math.sin(2 * ph))
        pata(c, s, lado, adelante=45 * math.sin(2 * ph + (0 if lado < 0 else math.pi)), rodilla=55 + 30 * math.sin(2 * ph + lado))


animacion("Tumbado", 12, pose_tumbado)


def pose_celebrar(c, t, ph):
    bote = abs(math.sin(2 * ph))
    poner("Cadera", c, (0, 8 * math.sin(ph), 0), (0, 0, -0.05 + 0.12 * bote))
    poner("Cuerpo", c, (-6, 10 * math.sin(ph), 6 * math.sin(ph)))
    for lado, s in LADOS:
        brazo(c, s, lado, abrir=70 + 15 * math.sin(2 * ph + lado), adelante=10, codo=5, antebrazo_abrir=55 + 15 * math.sin(2 * ph + lado))
        pata(c, s, lado, adelante=15 * (1 - bote), rodilla=35 * (1 - bote))


animacion("Celebrar", 24, pose_celebrar)

for accion in acciones:
    try:
        curvas = list(accion.fcurves)
    except AttributeError:
        curvas = [fc for capa in accion.layers for tira in capa.strips for bolsa in tira.channelbags for fc in bolsa.fcurves]
    for fc in curvas:
        for k in fc.keyframe_points:
            k.interpolation = "BEZIER"
            k.handle_left_type = k.handle_right_type = "AUTO_CLAMPED"

esqueleto.animation_data.action = acciones[0]

# ---------------------------------------------------------------------------
# Guardar y exportar
# ---------------------------------------------------------------------------

if RUTA_BLEND:
    os.makedirs(os.path.dirname(RUTA_BLEND), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=RUTA_BLEND)

if RUTA_FBX:
    os.makedirs(os.path.dirname(RUTA_FBX), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    esqueleto.select_set(True)
    malla.select_set(True)
    bpy.context.view_layer.objects.active = esqueleto
    bpy.ops.export_scene.fbx(
        filepath=RUTA_FBX, use_selection=True, object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=True, mesh_smooth_type="FACE", add_leaf_bones=False,
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", bake_space_transform=False,
        use_armature_deform_only=False, armature_nodetype="NULL",
        bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True, bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.5,
        path_mode="STRIP",
    )
    print(f"Exportado: {RUTA_FBX}")

# ---------------------------------------------------------------------------
# Vistas previas: de frente, 3/4, de lado, desde la cámara del juego y una por animación
# ---------------------------------------------------------------------------

if CARPETA_VISTAS:
    os.makedirs(CARPETA_VISTAS, exist_ok=True)
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
            fondo.inputs["Color"].default_value = (*srgb_a_lineal((0.86, 0.85, 0.92)), 1.0)
            fondo.inputs["Strength"].default_value = 0.85
    sol = bpy.data.objects.new("Sol", bpy.data.lights.new("Sol", "SUN"))
    sol.data.energy = 3.5
    sol.rotation_euler = (math.radians(50), 0, math.radians(-35))
    escena.collection.objects.link(sol)
    relleno = bpy.data.objects.new("Relleno", bpy.data.lights.new("Relleno", "AREA"))
    relleno.data.energy = 300
    relleno.data.size = 4
    relleno.location = (-3, -4, 3)
    relleno.rotation_euler = (math.radians(60), 0, math.radians(-35))
    escena.collection.objects.link(relleno)
    suelo = bpy.data.objects.new("Suelo", bpy.data.meshes.new("Suelo"))
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=20)
    bm.to_mesh(suelo.data)
    bm.free()
    suelo.data.materials.append(material("SueloVista", (0.82, 0.82, 0.88)))
    escena.collection.objects.link(suelo)
    camara = bpy.data.objects.new("Camara", bpy.data.cameras.new("Camara"))
    camara.data.type = "ORTHO"
    escena.collection.objects.link(camara)
    escena.camera = camara
    escena.render.resolution_x = escena.render.resolution_y = 600

    def foto(nombre, accion, cuadro, posicion, mirar=(0, 0, 1.0), escala=2.7):
        esqueleto.animation_data.action = accion
        escena.frame_set(cuadro)
        camara.data.ortho_scale = escala
        camara.location = posicion
        camara.rotation_euler = (Vector(mirar) - Vector(posicion)).to_track_quat("-Z", "Y").to_euler()
        escena.render.filepath = os.path.join(CARPETA_VISTAS, f"personaje_{ESPECIE}_{nombre}.png")
        bpy.ops.render.render(write_still=True)

    a = {x.name: x for x in acciones}
    foto("frontal", a["Quieto"], 0, (0, -6, 0.9))
    foto("tres_cuartos", a["Quieto"], 0, (4.2, -4.2, 1.8))
    foto("lateral", a["Quieto"], 0, (6, 0, 0.9))
    foto("camara_juego", a["Andar"], 5, (2.5, 6, 7))          # desde atrás y arriba, como en el juego
    foto("espalda", a["Quieto"], 0, (-3.5, 4.5, 1.9))
    for nombre, cuadro in (("correr", 3), ("saltar", 0)):
        foto(nombre, a[nombre.capitalize()], cuadro, (4.2, -4.2, 1.8))

print(f"{ESPECIE} listo:", [x.name for x in acciones])
