"""
Bicho Show — "Bichete", el concursante (bicho con caparazón), con esqueleto y animaciones.

Uso (sin abrir Blender):
    blender -b --factory-startup -P crear_bichete.py -- <salida.fbx> <salida.blend> [carpeta_vistas]

Diseño pensado para la cámara del juego (alta y desde atrás) y para que al girar no parezca
un recortable: cuerpo redondo como un huevo, caparazón de mariquita a la espalda (del color
del jugador, con lunares: es lo que más se ve desde arriba), antenas con bolitas (Unity las
hace bambolearse), ojos grandes, boca abierta con lengua, coloretes, brazos y patas cortos
y gorditos, manoplas y zapatillas.

Una sola malla con pesos y un esqueleto:
    Raiz > Cadera > Cuerpo > (OjoIzq/Der, AntenaIzq1 > AntenaIzq2, Brazo > Antebrazo > Mano)
                 > Muslo > Espinilla > Pie
El cuerpo se reparte entre Cadera y Cuerpo según la altura: se dobla como una gominola.
Materiales (orden fijo): Piel (se tiñe con el color del jugador), Caparazon (se tiñe más
oscuro), Barriga, Mancha, Ojo, Pupila, Brillo, Boca, Lengua, Rubor, Bota, Suela.
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

bpy.ops.wm.read_factory_settings(use_empty=True)
escena = bpy.context.scene
escena.render.fps = 30
LADOS = ((-1, "Izq"), (1, "Der"))

# ---------------------------------------------------------------------------
# Medidas
# ---------------------------------------------------------------------------

CENTRO = Vector((0.0, 0.0, 0.88))     # centro del cuerpo-huevo
RADIOS = Vector((0.50, 0.46, 0.58))   # medio ancho, medio fondo, medio alto (abajo es un poco más ancho)
PERA = 0.10                           # cuánto más ancho es abajo que arriba

HOMBRO = Vector((0.44, 0.0, 0.86))
CODO = Vector((0.58, -0.05, 0.72))
MUNECA = Vector((0.64, -0.09, 0.58))
PUNTA_MANO = Vector((0.66, -0.11, 0.46))
CADERA = Vector((0.20, 0.0, 0.40))
RODILLA = Vector((0.22, -0.03, 0.26))
TOBILLO = Vector((0.24, 0.0, 0.13))
PUNTA_PIE = Vector((0.24, -0.22, 0.04))
ANTENA_BASE = Vector((0.12, 0.06, 1.42))
ANTENA_MEDIO = Vector((0.20, 0.02, 1.62))
ANTENA_PUNTA = Vector((0.30, -0.05, 1.76))


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
    """Punto de la cara de delante del cuerpo a una altura y un lado."""
    dz = (z - CENTRO.z) / RADIOS.z
    k = ensanche(z)
    resto = max(0.0, 1.0 - dz * dz - (x / (RADIOS.x * k)) ** 2)
    return Vector((x, -RADIOS.y * k * math.sqrt(resto) - salir, z))


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
}
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
_ojo = delante(0.17, 1.16, -0.07)
hueso("AnclaGorro", (0, 0.0, 1.37), (0, 0.0, 1.55), "Cuerpo")
hueso("AnclaCara", (0, _ojo.y - 0.12, 1.16), (0, _ojo.y - 0.25, 1.16), "Cuerpo")
_pecho = delante(0.0, 0.76, 0.0)
hueso("AnclaPecho", _pecho, _pecho + Vector((0, -0.12, 0)), "Cuerpo")
print("Anclas: cara y =", round(_ojo.y - 0.12, 3), "· pecho =", tuple(round(v, 3) for v in _pecho))
for lado, s in LADOS:
    ojo = delante(0.17 * lado, 1.16, 0.0)
    hueso(f"Ojo{s}", ojo, ojo + Vector((0, -0.12, 0)), "Cuerpo")
    hueso(f"Antena{s}1", espejo(ANTENA_BASE, lado), espejo(ANTENA_MEDIO, lado), "Cuerpo")
    hueso(f"Antena{s}2", espejo(ANTENA_MEDIO, lado), espejo(ANTENA_PUNTA, lado), f"Antena{s}1", True)
    hueso(f"Brazo{s}", espejo(HOMBRO, lado), espejo(CODO, lado), "Cuerpo")
    hueso(f"Antebrazo{s}", espejo(CODO, lado), espejo(MUNECA, lado), f"Brazo{s}", True)
    hueso(f"Mano{s}", espejo(MUNECA, lado), espejo(PUNTA_MANO, lado), f"Antebrazo{s}", True)
    hueso(f"Muslo{s}", espejo(CADERA, lado), espejo(RODILLA, lado), "Cadera")
    hueso(f"Espinilla{s}", espejo(RODILLA, lado), espejo(TOBILLO, lado), f"Muslo{s}", True)
    hueso(f"Pie{s}", espejo(TOBILLO, lado), espejo(PUNTA_PIE, lado), f"Espinilla{s}", True)
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


# --- Cuerpo (huevo) ---
bm = bmesh.new()
deformar_pera(esfera(bm, CENTRO, RADIOS, 0, 40, 24))
cuerpo = nuevo("Cuerpo", bm, ["Piel"])
pesar_por_altura(cuerpo)

# --- Caparazón: casquete en la espalda, con grosor, y la raya del medio ---
bm = bmesh.new()
verts = esfera(bm, CENTRO + Vector((0, 0.01, 0.02)), RADIOS * 1.06, 0, 40, 24)
deformar_pera(verts)
bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.y < 0.10 or v.co.z < 0.42], context="VERTS")
caparazon = nuevo("Caparazon", bm, ["Caparazon"])
mod = caparazon.modifiers.new("Grosor", "SOLIDIFY")
mod.thickness = 0.045
mod.offset = 1.0
mod = caparazon.modifiers.new("Suave", "SUBSURF")
mod.levels = 1
congelar(caparazon)
pesar_por_altura(caparazon)

bm = bmesh.new()
for i in range(16):
    elev = -0.55 + 1.35 * i / 15  # de abajo (espalda baja) a arriba (nuca)
    d = Vector((0, math.cos(elev), math.sin(elev)))
    p = superficie(d) + d * 0.075
    esfera(bm, p, (0.018, 0.018, 0.05), 0, 10, 6, rot=hacia(d) @ Matrix.Rotation(math.pi / 2, 4, "X"))
raya = nuevo("Raya", bm, ["Mancha"])
pesar_por_altura(raya)

# --- Lunares del caparazón ---
bm = bmesh.new()
for lado, _ in LADOS:
    for azimut, elev, radio in ((35, 0.35, 0.10), (60, -0.05, 0.085), (28, -0.3, 0.075), (70, 0.45, 0.06)):
        a = math.radians(azimut)
        d = Vector((math.sin(a) * lado * math.cos(elev), math.cos(a) * math.cos(elev), math.sin(elev)))
        p = superficie(d) + d * 0.07
        esfera(bm, p, (radio, radio, 0.025), 0, 16, 8, rot=hacia(d))
lunares = nuevo("Lunares", bm, ["Mancha"])
pesar_por_altura(lunares)

# --- Barriga ---
bm = bmesh.new()
p = delante(0.0, 0.60, -0.06)
esfera(bm, p, (0.27, 0.1, 0.23), 0, 28, 14)
barriga = nuevo("Barriga", bm, ["Barriga"])
pesar_por_altura(barriga)

# --- Ojos (hueso propio, para parpadear) ---
for lado, s in LADOS:
    centro = delante(0.17 * lado, 1.16, -0.07)
    bm = bmesh.new()
    esfera(bm, centro, (0.145, 0.11, 0.165), 0)
    pupila = centro + Vector((-0.012 * lado, -0.1, -0.015))
    esfera(bm, pupila, (0.075, 0.03, 0.09), 1, 20, 10)
    esfera(bm, pupila + Vector((0.025, -0.022, 0.035)), (0.026, 0.012, 0.026), 2, 12, 6)
    esfera(bm, pupila + Vector((-0.02, -0.024, -0.035)), (0.012, 0.008, 0.012), 2, 10, 6)
    ojo = nuevo(f"Ojo{s}", bm, ["Ojo", "Pupila", "Brillo"])
    pesar_todo(ojo, f"Ojo{s}")

# --- Boca abierta con lengua, y coloretes ---
bm = bmesh.new()
p = delante(0.0, 0.93, 0.0)
for v in esfera(bm, p, (0.15, 0.03, 0.085), 0, 24, 12):                         # hueco de la boca
    if v.co.z > p.z:
        v.co.z = p.z + (v.co.z - p.z) * 0.18                                       # arriba plana: una "D" sonriente
esfera(bm, p + Vector((0.0, -0.012, -0.045)), (0.075, 0.025, 0.035), 1, 16, 8)  # lengua
boca = nuevo("Boca", bm, ["Boca", "Lengua"])
pesar_por_altura(boca)
for lado, s in LADOS:
    bm = bmesh.new()
    p = delante(0.32 * lado, 1.0, -0.005)
    esfera(bm, p, (0.075, 0.025, 0.045), 0, 16, 8, rot=hacia(Vector((0.55 * lado, -1, 0))) @ Matrix.Rotation(math.pi / 2, 4, "X"))
    rubor = nuevo(f"Rubor{s}", bm, ["Rubor"])
    pesar_por_altura(rubor)

# --- Manoplas y zapatillas ---
for lado, s in LADOS:
    bm = bmesh.new()
    c = espejo(Vector((0.66, -0.1, 0.5)), lado)
    esfera(bm, c, (0.11, 0.1, 0.115), 0)
    esfera(bm, c + Vector((-0.05 * lado, -0.07, 0.045)), (0.045, 0.04, 0.05), 0, 16, 8)
    mano = nuevo(f"Mano{s}", bm, ["Piel"])
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
    pts = [espejo(Vector((0.36, 0.0, 0.88)), lado), espejo(HOMBRO, lado), espejo(CODO, lado), espejo(MUNECA, lado),
           espejo(Vector((0.65, -0.1, 0.52)), lado)]
    tubo(f"Brazo{s}", pts, 0.062,
         lambda t, s=s: mezcla(t, 0.15, 0.08, "Cuerpo", f"Brazo{s}") if t < 0.3 else
         (mezcla(t, 0.52, 0.09, f"Brazo{s}", f"Antebrazo{s}") if t < 0.85 else mezcla(t, 0.9, 0.05, f"Antebrazo{s}", f"Mano{s}")),
         radio_final=0.055)

    pts = [espejo(Vector((0.16, 0.0, 0.50)), lado), espejo(CADERA, lado), espejo(RODILLA, lado), espejo(TOBILLO, lado),
           espejo(Vector((0.24, 0.0, 0.08)), lado)]
    tubo(f"Pata{s}", pts, 0.075,
         lambda t, s=s: mezcla(t, 0.15, 0.08, "Cadera", f"Muslo{s}") if t < 0.3 else
         (mezcla(t, 0.5, 0.08, f"Muslo{s}", f"Espinilla{s}") if t < 0.85 else mezcla(t, 0.88, 0.05, f"Espinilla{s}", f"Pie{s}")))

    pts = [espejo(ANTENA_BASE + Vector((0, 0.02, -0.08)), lado), espejo(ANTENA_BASE, lado), espejo(ANTENA_MEDIO, lado),
           espejo(ANTENA_PUNTA, lado)]
    tubo(f"Antena{s}", pts, 0.026,
         lambda t, s=s: mezcla(t, 0.2, 0.08, "Cuerpo", f"Antena{s}1") if t < 0.45 else mezcla(t, 0.62, 0.1, f"Antena{s}1", f"Antena{s}2"),
         anillos=18, lados=10, radio_final=0.02)
    bm = bmesh.new()
    esfera(bm, espejo(ANTENA_PUNTA, lado), (0.07, 0.07, 0.07), 0, 20, 10)
    bolita = nuevo(f"Bolita{s}", bm, ["Piel"])
    pesar_todo(bolita, f"Antena{s}2")

# ---------------------------------------------------------------------------
# Una sola malla, materiales en orden fijo, con el esqueleto
# ---------------------------------------------------------------------------

bpy.ops.object.select_all(action="DESELECT")
for p in piezas:
    p.select_set(True)
bpy.context.view_layer.objects.active = cuerpo
bpy.ops.object.join()
malla = cuerpo
malla.name = malla.data.name = "Bichete"

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

    def foto(nombre, accion, cuadro, posicion, mirar=(0, 0, 0.85), escala=2.3):
        esqueleto.animation_data.action = accion
        escena.frame_set(cuadro)
        camara.data.ortho_scale = escala
        camara.location = posicion
        camara.rotation_euler = (Vector(mirar) - Vector(posicion)).to_track_quat("-Z", "Y").to_euler()
        escena.render.filepath = os.path.join(CARPETA_VISTAS, f"bichete_{nombre}.png")
        bpy.ops.render.render(write_still=True)

    a = {x.name: x for x in acciones}
    foto("frontal", a["Quieto"], 0, (0, -6, 0.9))
    foto("tres_cuartos", a["Quieto"], 0, (4.2, -4.2, 1.8))
    foto("lateral", a["Quieto"], 0, (6, 0, 0.9))
    foto("camara_juego", a["Andar"], 5, (2.5, 6, 7))          # desde atrás y arriba, como en el juego
    for nombre, cuadro in (("andar", 5), ("correr", 3), ("saltar", 0), ("caer", 3), ("tumbado", 3), ("celebrar", 6)):
        foto(nombre, a[nombre.capitalize()], cuadro, (4.2, -4.2, 1.8))

print("Bichete listo:", [x.name for x in acciones])
