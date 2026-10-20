"""
Bicho Show — Triangulín con esqueleto y animaciones, para Unity.

Uso (sin abrir Blender):
    blender -b --factory-startup -P crear_triangulin_animado.py -- <salida.fbx> <salida.blend> [carpeta_vistas]

Qué hace:
  1. Modela a Triangulín como en la hoja de referencia: cuerpo de triángulo invertido (de lado,
     una cometa), ojos grandes con párpados de enfado, sonrisa, brazos y piernas de fideo,
     manoplas y botas.
  2. Le pone un esqueleto: Raiz > Cadera > Cuerpo (> OjoIzq, OjoDer, Brazo > Antebrazo > Mano)
     y Cadera > Muslo > Espinilla > Pie. Todo es UNA malla con pesos (brazos y piernas se doblan
     en codos y rodillas); el material "Piel" va el primero (Unity lo tiñe con el color del jugador).
  3. Anima por código: Quieto, Andar, Correr, Saltar, Caer, Tumbado y Celebrar (todas en bucle).

Convenciones: Z arriba, mira hacia -Y, pies en Z = 0, metros. "Izq" = lado de X negativa.
Las rotaciones de las animaciones se escriben en ejes del mundo (X = adelante/atrás,
Y = de lado, Z = girar) y el script las pasa al espacio de cada hueso.
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

# ---------------------------------------------------------------------------
# Medidas (metros). Proporciones de la hoja de referencia: el cuerpo ocupa casi 3/4 de la altura.
# ---------------------------------------------------------------------------

CUERPO_ARRIBA = 1.48
CUERPO_PUNTA = 0.40
CUERPO_MEDIO_ANCHO = 0.60
GROSOR_ARRIBA = 0.08
GROSOR_MEDIO = 0.19
ALTURA_GROSOR_MEDIO = 1.02
GROSOR_PUNTA = 0.05

OJO_ALTURA = 1.13
OJO_SEPARACION = 0.20
OJO_RADIO = 0.14
BOCA_ALTURA = 0.93

RADIO_BRAZO = 0.048
RADIO_PIERNA = 0.055

# Articulaciones (lado derecho, X positiva; el izquierdo es su espejo)
HOMBRO = Vector((0.30, 0.0, 0.98))
CODO = Vector((0.50, -0.02, 0.86))
MUNECA = Vector((0.60, -0.04, 0.70))
PUNTA_MANO = Vector((0.63, -0.05, 0.56))
CADERA = Vector((0.13, 0.0, 0.62))
RODILLA = Vector((0.16, -0.03, 0.40))
TOBILLO = Vector((0.19, 0.0, 0.19))
PUNTA_PIE = Vector((0.19, -0.25, 0.05))


def grosor_en(z):
    if z <= ALTURA_GROSOR_MEDIO:
        t = (z - CUERPO_PUNTA) / (ALTURA_GROSOR_MEDIO - CUERPO_PUNTA)
        return GROSOR_PUNTA + (GROSOR_MEDIO - GROSOR_PUNTA) * max(0.0, min(1.0, t))
    t = (z - ALTURA_GROSOR_MEDIO) / (CUERPO_ARRIBA - ALTURA_GROSOR_MEDIO)
    return GROSOR_MEDIO + (GROSOR_ARRIBA - GROSOR_MEDIO) * max(0.0, min(1.0, t))


def medio_ancho_en(z):
    return CUERPO_MEDIO_ANCHO * max(0.0, (z - CUERPO_PUNTA) / (CUERPO_ARRIBA - CUERPO_PUNTA))


def espejo(v, lado):
    return Vector((v.x * lado, v.y, v.z))


LADOS = ((-1, "Izq"), (1, "Der"))

# ---------------------------------------------------------------------------
# Materiales (el orden importa: Piel primero)
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


MAT = {
    "Piel": material("Piel", (0.88, 0.30, 0.24), 0.75),
    "Ojo": material("Ojo", (0.97, 0.94, 0.88), 0.35),
    "Pupila": material("Pupila", (0.06, 0.04, 0.04), 0.2),
    "Brillo": material("Brillo", (1.0, 1.0, 1.0), 0.1),
    "Boca": material("Boca", (0.32, 0.08, 0.06), 0.6),
    "Bota": material("Bota", (0.06, 0.42, 0.42), 0.6),
    "Suela": material("Suela", (0.05, 0.28, 0.28), 0.8),
}
ORDEN_MATERIALES = ["Piel", "Ojo", "Pupila", "Brillo", "Boca", "Bota", "Suela"]

# ---------------------------------------------------------------------------
# Esqueleto
# ---------------------------------------------------------------------------

datos_esqueleto = bpy.data.armatures.new("Esqueleto")
esqueleto = bpy.data.objects.new("Esqueleto", datos_esqueleto)
escena.collection.objects.link(esqueleto)
bpy.context.view_layer.objects.active = esqueleto
esqueleto.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")

huesos = datos_esqueleto.edit_bones


def hueso(nombre, cabeza, cola, padre=None, conectado=False):
    b = huesos.new(nombre)
    b.head = Vector(cabeza)
    b.tail = Vector(cola)
    b.roll = 0.0
    if padre:
        b.parent = huesos[padre]
        b.use_connect = conectado
    return b


hueso("Raiz", (0, 0, 0), (0, 0, 0.25))
hueso("Cadera", (0, 0, 0.56), (0, 0, 0.80), "Raiz")
hueso("Cuerpo", (0, 0, 0.80), (0, 0, 1.40), "Cadera")
for lado, s in LADOS:
    y_ojo = -grosor_en(OJO_ALTURA) - 0.005
    hueso(f"Ojo{s}", (OJO_SEPARACION * lado, y_ojo, OJO_ALTURA), (OJO_SEPARACION * lado, y_ojo - 0.12, OJO_ALTURA), "Cuerpo")
    hueso(f"Brazo{s}", espejo(HOMBRO, lado), espejo(CODO, lado), "Cuerpo")
    hueso(f"Antebrazo{s}", espejo(CODO, lado), espejo(MUNECA, lado), f"Brazo{s}", True)
    hueso(f"Mano{s}", espejo(MUNECA, lado), espejo(PUNTA_MANO, lado), f"Antebrazo{s}", True)
    hueso(f"Muslo{s}", espejo(CADERA, lado), espejo(RODILLA, lado), "Cadera")
    hueso(f"Espinilla{s}", espejo(RODILLA, lado), espejo(TOBILLO, lado), f"Muslo{s}", True)
    hueso(f"Pie{s}", espejo(TOBILLO, lado), espejo(PUNTA_PIE, lado), f"Espinilla{s}", True)

bpy.ops.object.mode_set(mode="OBJECT")

# ---------------------------------------------------------------------------
# Piezas: cada una es un objeto con su grupo de vértices; al final se unen en una malla
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


def aplicar_modificadores(obj):
    """Congela los modificadores (bisel, subdivisión…) en la malla."""
    if not obj.modifiers:
        return
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    viejo = obj.data
    obj.modifiers.clear()
    obj.data = me
    bpy.data.meshes.remove(viejo)


def pesar_todo(obj, hueso_nombre):
    vg = obj.vertex_groups.new(name=hueso_nombre)
    vg.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    piezas.append(obj)


def esfera(bm, centro, radios, indice=0, segmentos=28, anillos=14, rot=None):
    m = Matrix.Translation(Vector(centro))
    if rot is not None:
        m = m @ rot
    m = m @ Matrix.Diagonal((*radios, 1.0))
    r = bmesh.ops.create_uvsphere(bm, u_segments=segmentos, v_segments=anillos, radius=1.0, matrix=m)
    for f in {f for v in r["verts"] for f in v.link_faces}:
        f.material_index = indice


# --- Cuerpo ---
bm = bmesh.new()
w, zt, zb, zm = CUERPO_MEDIO_ANCHO, CUERPO_ARRIBA, CUERPO_PUNTA, ALTURA_GROSOR_MEDIO
wm = medio_ancho_en(zm)
gt, gm, gb = GROSOR_ARRIBA, GROSOR_MEDIO, GROSOR_PUNTA
ad_i, ad_d, at_i, at_d = [bm.verts.new(p) for p in [(-w, -gt, zt), (w, -gt, zt), (-w, gt, zt), (w, gt, zt)]]
md_i, md_d, mt_i, mt_d = [bm.verts.new(p) for p in [(-wm, -gm, zm), (wm, -gm, zm), (-wm, gm, zm), (wm, gm, zm)]]
pd, pt = bm.verts.new((0, -gb, zb)), bm.verts.new((0, gb, zb))
for cara in ([ad_i, ad_d, at_d, at_i], [ad_i, md_i, md_d, ad_d], [md_i, pd, md_d], [at_i, at_d, mt_d, mt_i],
             [mt_i, mt_d, pt], [ad_i, at_i, mt_i, md_i], [md_i, mt_i, pt, pd], [ad_d, md_d, mt_d, at_d], [md_d, pd, pt, mt_d]):
    bm.faces.new(cara)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
cuerpo = nuevo("Cuerpo", bm, ["Piel"])
mod = cuerpo.modifiers.new("Bisel", "BEVEL")
mod.width = 0.09
mod.segments = 4
mod.limit_method = "NONE"
mod = cuerpo.modifiers.new("Suave", "SUBSURF")
mod.levels = 2
mod.render_levels = 2
textura = bpy.data.textures.new("Barro", type="CLOUDS")
textura.noise_scale = 0.06
mod = cuerpo.modifiers.new("Barro", "DISPLACE")
mod.texture = textura
mod.strength = 0.006
aplicar_modificadores(cuerpo)
pesar_todo(cuerpo, "Cuerpo")

# --- Ojos (blanco, pupila y brillo: hueso del ojo, para parpadear) y párpados de enfado ---
for lado, s in LADOS:
    x = OJO_SEPARACION * lado
    centro = Vector((x, -grosor_en(OJO_ALTURA) - 0.005, OJO_ALTURA))  # bien salidos, como en la referencia
    bm = bmesh.new()
    esfera(bm, centro, (OJO_RADIO, OJO_RADIO * 0.8, OJO_RADIO * 1.08), 0)
    pupila = centro + Vector((-0.028 * lado, -OJO_RADIO * 0.76, -0.012))
    esfera(bm, pupila, (0.066, 0.036, 0.074), 1)
    esfera(bm, pupila + Vector((0.02, -0.032, 0.03)), (0.021, 0.012, 0.021), 2, 14, 8)
    ojo = nuevo(f"Ojo{s}", bm, ["Ojo", "Pupila", "Brillo"])
    pesar_todo(ojo, f"Ojo{s}")

    bm = bmesh.new()
    esfera(bm, centro + Vector((0, 0.0, 0.1)), (0.19, 0.15, 0.068), 0,
           rot=Matrix.Rotation(math.radians(-22.0 * lado), 4, "Y"))
    parpado = nuevo(f"Parpado{s}", bm, ["Piel"])
    pesar_todo(parpado, "Cuerpo")

# --- Boca: sonrisa pícara ---
y_boca = -grosor_en(BOCA_ALTURA) - 0.006
puntos = []
for i in range(9):
    a = math.radians(205 + (335 - 205) * i / 8)
    puntos.append(Vector((0.015 + 0.09 * math.cos(a), y_boca, BOCA_ALTURA + 0.058 * math.sin(a) + 0.012 * i / 8)))
curva = bpy.data.curves.new("Boca", "CURVE")
curva.dimensions = "3D"
curva.bevel_depth = 0.014
curva.bevel_resolution = 4
curva.use_fill_caps = True
spline = curva.splines.new("BEZIER")
spline.bezier_points.add(len(puntos) - 1)
for i, p in enumerate(puntos):
    spline.bezier_points[i].co = p
    spline.bezier_points[i].handle_left_type = spline.bezier_points[i].handle_right_type = "AUTO"
obj_curva = bpy.data.objects.new("BocaCurva", curva)
escena.collection.objects.link(obj_curva)
dg = bpy.context.evaluated_depsgraph_get()
me_boca = bpy.data.meshes.new_from_object(obj_curva.evaluated_get(dg))
bpy.data.objects.remove(obj_curva)
me_boca.materials.clear()
me_boca.materials.append(MAT["Boca"])
for p in me_boca.polygons:
    p.use_smooth = True
boca = bpy.data.objects.new("Boca", me_boca)
escena.collection.objects.link(boca)
pesar_todo(boca, "Cuerpo")

# --- Manoplas y botas ---
for lado, s in LADOS:
    bm = bmesh.new()
    centro = espejo(Vector((0.63, -0.05, 0.60)), lado)
    esfera(bm, centro, (0.14, 0.13, 0.15), 0)
    esfera(bm, centro + Vector((-0.06 * lado, -0.095, 0.055)), (0.06, 0.055, 0.065), 0, 20, 10)  # pulgar
    esfera(bm, centro + Vector((0, 0, 0.12)), (0.085, 0.085, 0.05), 0, 18, 8)                    # puño de la manopla
    mano = nuevo(f"Mano{s}", bm, ["Piel"])
    pesar_todo(mano, f"Mano{s}")

    bm = bmesh.new()
    pie = espejo(Vector((TOBILLO.x, 0, 0)), lado)
    esfera(bm, pie + Vector((0, -0.08, 0.125)), (0.165, 0.26, 0.13), 0)
    esfera(bm, pie + Vector((0, -0.22, 0.105)), (0.145, 0.13, 0.10), 0)
    r = bmesh.ops.create_cone(bm, cap_ends=True, segments=20, radius1=0.08, radius2=0.075, depth=0.1,
                              matrix=Matrix.Translation(pie + Vector((0, 0, 0.22))))
    r = bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(pie + Vector((0, -0.08, 0.035))) @ Matrix.Diagonal((0.29, 0.5, 0.07, 1.0)))
    for f in {f for v in r["verts"] for f in v.link_faces}:
        f.material_index = 1
    bota = nuevo(f"Bota{s}", bm, ["Bota", "Suela"])
    mod = bota.modifiers.new("Bisel", "BEVEL")
    mod.width = 0.03
    mod.segments = 3
    mod.limit_method = "ANGLE"
    mod = bota.modifiers.new("Suave", "SUBSURF")
    mod.levels = 1
    aplicar_modificadores(bota)
    pesar_todo(bota, f"Pie{s}")

# --- Brazos y piernas de fideo: tubos con pesos que se reparten en el codo y la rodilla ---


def catmull(p0, p1, p2, p3, t):
    t2, t3 = t * t, t * t * t
    return 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3)


def tubo(nombre, puntos, radio, pesos_por_t, anillos=26, lados=12, radio_final=None):
    """
    Tubo suave que pasa por los puntos. pesos_por_t(t) devuelve [(hueso, peso), ...] para la
    posición t (0 al principio, 1 al final) de cada anillo.
    """
    ext = [puntos[0] + (puntos[0] - puntos[1])] + puntos + [puntos[-1] + (puntos[-1] - puntos[-2])]
    tramos = len(puntos) - 1
    centros = []
    for i in range(anillos + 1):
        u = i / anillos * tramos
        k = min(int(u), tramos - 1)
        centros.append(catmull(ext[k], ext[k + 1], ext[k + 2], ext[k + 3], u - k))

    bm = bmesh.new()
    anillos_verts = []
    normal_prev = None
    for i, c in enumerate(centros):
        tangente = (centros[min(i + 1, anillos)] - centros[max(i - 1, 0)]).normalized()
        if normal_prev is None:
            normal_prev = tangente.cross(Vector((0, 1, 0.3))).normalized()
        normal = (normal_prev - tangente * normal_prev.dot(tangente)).normalized()  # transporte paralelo
        binormal = tangente.cross(normal)
        normal_prev = normal
        t = i / anillos
        rr = radio if radio_final is None else radio + (radio_final - radio) * t
        anillo = []
        for j in range(lados):
            a = 2 * math.pi * j / lados
            anillo.append(bm.verts.new(c + (normal * math.cos(a) + binormal * math.sin(a)) * rr))
        anillos_verts.append(anillo)
    for i in range(anillos):
        for j in range(lados):
            k = (j + 1) % lados
            bm.faces.new((anillos_verts[i][j], anillos_verts[i][k], anillos_verts[i + 1][k], anillos_verts[i + 1][j]))
    for anillo in (anillos_verts[0], anillos_verts[-1]):
        bm.faces.new(anillo)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.verts.index_update()
    indices = [[v.index for v in anillo] for anillo in anillos_verts]
    obj = nuevo(nombre, bm, ["Piel"])

    grupos = {}
    for i, idx in enumerate(indices):
        for nombre_hueso, peso in pesos_por_t(i / anillos):
            if peso <= 0.001:
                continue
            if nombre_hueso not in grupos:
                grupos[nombre_hueso] = obj.vertex_groups.new(name=nombre_hueso)
            grupos[nombre_hueso].add(idx, peso, "REPLACE")
    piezas.append(obj)


def mezcla(t, t0, ancho, a, b):
    """Pesos que pasan de 'a' a 'b' alrededor de t0."""
    k = max(0.0, min(1.0, (t - (t0 - ancho)) / (2 * ancho)))
    k = k * k * (3 - 2 * k)
    return [(a, 1 - k), (b, k)]


for lado, s in LADOS:
    # Brazo: sale de dentro del cuerpo, codo, muñeca (acaba dentro de la manopla)
    inicio = espejo(Vector((0.22, 0.0, 1.0)), lado)
    pts = [inicio, espejo(HOMBRO, lado), espejo(CODO, lado), espejo(MUNECA, lado), espejo(Vector((0.62, -0.05, 0.64)), lado)]

    def pesos_brazo(t, s=s):
        if t < 0.3:
            return mezcla(t, 0.18, 0.1, "Cuerpo", f"Brazo{s}")
        if t < 0.85:
            return mezcla(t, 0.52, 0.09, f"Brazo{s}", f"Antebrazo{s}")
        return mezcla(t, 0.9, 0.05, f"Antebrazo{s}", f"Mano{s}")

    tubo(f"Brazo{s}", pts, RADIO_BRAZO, pesos_brazo, radio_final=RADIO_BRAZO * 0.9)

    # Pierna: sale de dentro del cuerpo, rodilla, tobillo (acaba dentro de la bota)
    inicio = espejo(Vector((0.09, 0.0, 0.70)), lado)
    pts = [inicio, espejo(CADERA, lado), espejo(RODILLA, lado), espejo(TOBILLO, lado), espejo(Vector((0.19, 0.0, 0.12)), lado)]

    def pesos_pierna(t, s=s):
        if t < 0.3:
            return mezcla(t, 0.16, 0.08, "Cadera", f"Muslo{s}")
        if t < 0.85:
            return mezcla(t, 0.5, 0.08, f"Muslo{s}", f"Espinilla{s}")
        return mezcla(t, 0.88, 0.05, f"Espinilla{s}", f"Pie{s}")

    tubo(f"Pierna{s}", pts, RADIO_PIERNA, pesos_pierna)

# ---------------------------------------------------------------------------
# Unir todo en una malla "Triangulin" con el esqueleto
# ---------------------------------------------------------------------------

bpy.ops.object.select_all(action="DESELECT")
for p in piezas:
    p.select_set(True)
bpy.context.view_layer.objects.active = cuerpo
bpy.ops.object.join()
malla = cuerpo
malla.name = "Triangulin"
malla.data.name = "Triangulin"

# Materiales en el orden fijo (Piel primero), reasignando los índices de las caras.
# (Los índices se leen ANTES de vaciar la lista: al vaciarla, Blender los pone todos a 0.)
viejos = [m.name.split(".")[0] for m in malla.data.materials]
nuevos_indices = [ORDEN_MATERIALES.index(viejos[p.material_index]) for p in malla.data.polygons]
malla.data.materials.clear()
for n in ORDEN_MATERIALES:
    malla.data.materials.append(MAT[n])
malla.data.polygons.foreach_set("material_index", nuevos_indices)
malla.data.update()
print("Caras por material:", {n: nuevos_indices.count(i) for i, n in enumerate(ORDEN_MATERIALES)})

malla.parent = esqueleto
modificador = malla.modifiers.new("Esqueleto", "ARMATURE")
modificador.object = esqueleto

# ---------------------------------------------------------------------------
# Animaciones
# ---------------------------------------------------------------------------

esqueleto.animation_data_create()
REPOSO = {}
for pb in esqueleto.pose.bones:
    pb.rotation_mode = "QUATERNION"
    REPOSO[pb.name] = pb.bone.matrix_local.to_3x3()

acciones = []


def poner(nombre_hueso, cuadro, rot=(0, 0, 0), loc=(0, 0, 0)):
    """Rota el hueso (grados, ejes del mundo en reposo, orden X-Y-Z) y lo desplaza (metros)."""
    pb = esqueleto.pose.bones[nombre_hueso]
    r = REPOSO[nombre_hueso]
    giro = Euler([math.radians(v) for v in rot], "XYZ").to_matrix()
    pb.rotation_quaternion = (r.inverted() @ giro @ r).to_quaternion()
    pb.location = r.inverted() @ Vector(loc)
    pb.keyframe_insert("rotation_quaternion", frame=cuadro)
    pb.keyframe_insert("location", frame=cuadro)


def animacion(nombre, cuadros, pose, pasos=None):
    """pose(t, ph) recibe t en [0, 1) y la fase ph = 2πt y llama a poner() para ese instante."""
    accion = bpy.data.actions.new(nombre)
    accion.use_fake_user = True
    esqueleto.animation_data.action = accion
    for pb in esqueleto.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    paso = pasos or max(1, cuadros // 12)
    marcas = list(range(0, cuadros, paso)) + [cuadros]
    for c in marcas:
        t = (c % cuadros) / cuadros
        pose(c, t, 2 * math.pi * t)
    try:
        accion.use_frame_range = True
        accion.frame_range = (0, cuadros)
    except Exception:
        pass
    acciones.append(accion)
    return accion


def brazo(c, s, abrir=10.0, adelante=0.0, codo=15.0, mano=0.0, lado=1, antebrazo_abrir=0.0):
    """abrir: separar del cuerpo hacia arriba; adelante: llevar hacia delante; codo: doblar."""
    poner(f"Brazo{s}", c, (-adelante, -lado * abrir, 0))
    poner(f"Antebrazo{s}", c, (-codo, -lado * antebrazo_abrir, 0))
    poner(f"Mano{s}", c, (-mano, 0, 0))


def pierna(c, s, adelante=0.0, rodilla=0.0, abrir=0.0, lado=1):
    poner(f"Muslo{s}", c, (-adelante, -lado * abrir, 0))
    poner(f"Espinilla{s}", c, (rodilla, 0, 0))
    poner(f"Pie{s}", c, (adelante - rodilla, 0, 0))  # el pie, plano en el suelo


def todo_quieto(c):
    for lado, s in LADOS:
        brazo(c, s, lado=lado)
        pierna(c, s, lado=lado)


# Quieto: respira y se balancea un poco
def pose_quieto(c, t, ph):
    poner("Cadera", c, (0, 0, 0), (0, 0, -0.012 - 0.012 * math.sin(ph)))
    poner("Cuerpo", c, (1.5 * math.sin(ph), 2.0 * math.sin(ph + 1), 0))
    for lado, s in LADOS:
        brazo(c, s, abrir=9 + 3 * math.sin(ph + lado), adelante=3 * math.sin(ph), codo=18 + 4 * math.sin(ph), lado=lado)
        pierna(c, s, lado=lado)


animacion("Quieto", 60, pose_quieto, 5)


# Andar: zancada, rebote doble, vaivén de cadera y brazos al contrario que las piernas
def pose_andar(c, t, ph, zancada=28.0, rodilla=34.0, bote=0.045, inclinacion=6.0, brazos=26.0, codo=8.0, giro=6.0):
    poner("Cadera", c, (0, 4 * math.sin(ph), giro * math.sin(ph)), (0, 0, -bote * abs(math.sin(ph))))
    poner("Cuerpo", c, (inclinacion + 2 * math.cos(2 * ph), -3 * math.sin(ph), -1.4 * giro * math.sin(ph)))
    for lado, s in LADOS:
        fase = ph + (0.0 if lado < 0 else math.pi)
        adelante = zancada * math.sin(fase)
        doblar = 8 + rodilla * max(0.0, math.cos(fase))  # dobla la rodilla al traer la pierna
        pierna(c, s, adelante=adelante, rodilla=doblar, lado=lado)
        brazo(c, s, abrir=12, adelante=-brazos * math.sin(fase), codo=codo + 12 * max(0.0, -math.sin(fase)), lado=lado)


animacion("Andar", 24, pose_andar, 2)


def pose_correr(c, t, ph):
    pose_andar(c, t, ph, zancada=46.0, rodilla=70.0, bote=0.075, inclinacion=15.0, brazos=48.0, codo=50.0, giro=10.0)


animacion("Correr", 16, pose_correr, 2)


# Saltar: piernas recogidas y brazos arriba
def pose_saltar(c, t, ph):
    poner("Cadera", c, (0, 0, 0), (0, 0, 0.03))
    poner("Cuerpo", c, (-6 + 2 * math.sin(ph), 0, 0))
    for lado, s in LADOS:
        brazo(c, s, abrir=72 + 6 * math.sin(ph + lado), adelante=12, codo=30, lado=lado)
        pierna(c, s, adelante=32 + 4 * math.sin(ph + lado), rodilla=58, lado=lado)


animacion("Saltar", 20, pose_saltar, 5)


# Caer: brazos como aspas y piernas pataleando
def pose_caer(c, t, ph):
    poner("Cadera", c, (0, 0, 0), (0, 0, 0.02))
    poner("Cuerpo", c, (5 * math.sin(ph), 4 * math.sin(2 * ph), 0))
    for lado, s in LADOS:
        brazo(c, s, abrir=105 + 25 * math.sin(ph + lado), adelante=25 * math.sin(2 * ph + lado), codo=35 + 15 * math.sin(ph), lado=lado)
        pierna(c, s, adelante=26 * math.sin(ph + lado * 1.6), rodilla=30 + 25 * math.sin(ph + lado), lado=lado)


animacion("Caer", 12, pose_caer, 2)


# Tumbado: se agita como un bicho panza arriba
def pose_tumbado(c, t, ph):
    poner("Cuerpo", c, (6 * math.sin(2 * ph), 10 * math.sin(ph), 0))
    for lado, s in LADOS:
        brazo(c, s, abrir=65 + 50 * math.sin(ph + lado), adelante=50 * math.sin(2 * ph + lado * 1.3), codo=45 + 30 * math.sin(ph * 2), lado=lado)
        pierna(c, s, adelante=40 * math.sin(2 * ph + (0 if lado < 0 else math.pi)), rodilla=50 + 30 * math.sin(2 * ph + lado), lado=lado)


animacion("Tumbado", 12, pose_tumbado, 2)


# Celebrar: saltitos con los brazos arriba
def pose_celebrar(c, t, ph):
    bote = abs(math.sin(2 * ph))
    poner("Cadera", c, (0, 6 * math.sin(ph), 0), (0, 0, -0.05 + 0.1 * bote))
    poner("Cuerpo", c, (-4, 8 * math.sin(ph), 0))
    for lado, s in LADOS:
        brazo(c, s, abrir=62 + 14 * math.sin(2 * ph + lado), adelante=12, codo=5, antebrazo_abrir=55 + 15 * math.sin(2 * ph + lado), lado=lado)
        pierna(c, s, adelante=12 * (1 - bote), rodilla=30 * (1 - bote), lado=lado)


animacion("Celebrar", 24, pose_celebrar, 2)

# Curvas suaves y en bucle
for accion in acciones:
    try:
        curvas = accion.fcurves
    except AttributeError:
        curvas = [fc for capa in accion.layers for tira in capa.strips for bolsa in tira.channelbags for fc in bolsa.fcurves]
    for fc in curvas:
        for k in fc.keyframe_points:
            k.interpolation = "BEZIER"
            k.handle_left_type = k.handle_right_type = "AUTO_CLAMPED"

esqueleto.animation_data.action = acciones[0]
for pb in esqueleto.pose.bones:
    pb.rotation_quaternion = (1, 0, 0, 0)
    pb.location = (0, 0, 0)

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
# Vistas: modelo en reposo (4 lados) y una tira con poses de cada animación
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
            fondo.inputs["Color"].default_value = (*srgb_a_lineal((0.88, 0.88, 0.9)), 1.0)
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
    suelo.data.materials.append(material("SueloVista", (0.8, 0.8, 0.84)))
    escena.collection.objects.link(suelo)

    camara = bpy.data.objects.new("Camara", bpy.data.cameras.new("Camara"))
    camara.data.type = "ORTHO"
    escena.collection.objects.link(camara)
    escena.camera = camara

    def foto(nombre, accion, cuadro, posicion, escala=2.2, ancho=600, alto=600):
        esqueleto.animation_data.action = accion
        escena.frame_set(cuadro)
        camara.data.ortho_scale = escala
        camara.location = posicion
        camara.rotation_euler = (Vector((0, 0, 0.78)) - Vector(posicion)).to_track_quat("-Z", "Y").to_euler()
        escena.render.resolution_x = ancho
        escena.render.resolution_y = alto
        escena.render.filepath = os.path.join(CARPETA_VISTAS, f"triangulin_anim_{nombre}.png")
        bpy.ops.render.render(write_still=True)
        print(f"Vista: {escena.render.filepath}")

    por_nombre = {a.name: a for a in acciones}
    foto("frontal", por_nombre["Quieto"], 0, (0, -6, 0.8))
    foto("tres_cuartos", por_nombre["Quieto"], 0, (4.2, -4.2, 1.6))
    foto("lateral", por_nombre["Quieto"], 0, (6, 0, 0.8))
    for nombre, cuadro in (("andar", 6), ("correr", 4), ("saltar", 0), ("caer", 3), ("tumbado", 3), ("celebrar", 6)):
        foto(nombre, por_nombre[nombre.capitalize()], cuadro, (4.2, -4.2, 1.6))

print("Triangulín animado listo:", [a.name for a in acciones])
