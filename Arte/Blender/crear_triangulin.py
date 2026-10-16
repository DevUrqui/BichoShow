"""
Bicho Show — modela el personaje "Triangulín" en Blender y lo exporta para Unity.

Uso (sin abrir Blender):
    blender -b --factory-startup -P crear_triangulin.py -- <salida.fbx> <salida.blend> [carpeta_vistas]

Piezas que se exportan (los brazos y las piernas "de fideo" los genera Unity en tiempo real):
    Cuerpo, ParpadoIzq/Der, OjoIzq/Der, PupilaIzq/Der, BrilloIzq/Der, Boca, ManoIzq/Der, BotaIzq/Der

Convenciones: Z arriba, el personaje mira hacia -Y (de frente en la vista frontal de Blender),
los pies en Z = 0. Medidas en metros (1 m de Blender = 1 unidad de Unity).
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

# ---------------------------------------------------------------------------
# Argumentos
# ---------------------------------------------------------------------------

argumentos = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
RUTA_FBX = argumentos[0] if len(argumentos) > 0 else None
RUTA_BLEND = argumentos[1] if len(argumentos) > 1 else None
CARPETA_VISTAS = argumentos[2] if len(argumentos) > 2 else None

# ---------------------------------------------------------------------------
# Medidas del personaje (todas en metros)
# ---------------------------------------------------------------------------

CUERPO_ARRIBA = 1.50      # altura del borde de arriba del triángulo
CUERPO_PUNTA = 0.55       # altura de la punta de abajo
CUERPO_MEDIO_ANCHO = 0.55
GROSOR_ARRIBA = 0.07      # medio grosor (en Y) en el borde de arriba: de lado es fino arriba…
GROSOR_MEDIO = 0.20       # …más gordo a la altura de los ojos (perfil de cometa)…
ALTURA_GROSOR_MEDIO = 1.15
GROSOR_PUNTA = 0.04       # …y afilado en la punta

OJO_ALTURA = 1.13
OJO_SEPARACION = 0.19
OJO_RADIO = 0.125
BOCA_ALTURA = 0.97
MANO_POSICION = Vector((0.60, -0.04, 0.78))
BOTA_X = 0.20


def grosor_en(z):
    """Medio grosor del cuerpo a una altura: la cara de delante está en y = -grosor_en(z)."""
    if z <= ALTURA_GROSOR_MEDIO:
        t = (z - CUERPO_PUNTA) / (ALTURA_GROSOR_MEDIO - CUERPO_PUNTA)
        return GROSOR_PUNTA + (GROSOR_MEDIO - GROSOR_PUNTA) * max(0.0, min(1.0, t))
    t = (z - ALTURA_GROSOR_MEDIO) / (CUERPO_ARRIBA - ALTURA_GROSOR_MEDIO)
    return GROSOR_MEDIO + (GROSOR_ARRIBA - GROSOR_MEDIO) * max(0.0, min(1.0, t))


def medio_ancho_en(z):
    """Medio ancho del triángulo (visto de frente) a una altura."""
    return CUERPO_MEDIO_ANCHO * max(0.0, (z - CUERPO_PUNTA) / (CUERPO_ARRIBA - CUERPO_PUNTA))


# ---------------------------------------------------------------------------
# Utilidades
# ---------------------------------------------------------------------------

def srgb_a_lineal(c):
    return tuple((v / 12.92) if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in c)


def material(nombre, color_srgb, rugosidad=0.65):
    m = bpy.data.materials.new(nombre)
    color = (*srgb_a_lineal(color_srgb), 1.0)
    m.diffuse_color = color  # lo usa la vista Workbench
    try:
        m.use_nodes = True
    except Exception:
        pass
    arbol = getattr(m, "node_tree", None)
    if arbol is not None:
        bsdf = next((n for n in arbol.nodes if n.type == "BSDF_PRINCIPLED"), None)
        if bsdf is not None:
            bsdf.inputs["Base Color"].default_value = color
            bsdf.inputs["Roughness"].default_value = rugosidad
    return m


def nuevo_objeto(nombre, malla, ubicacion=(0, 0, 0), materiales=()):
    obj = bpy.data.objects.new(nombre, malla)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = ubicacion
    for m in materiales:
        malla.materials.append(m)
    return obj


def suavizar(malla):
    for p in malla.polygons:
        p.use_smooth = True


def esfera_en(bm, centro, radios, indice_material=0, segmentos=32, anillos=16):
    """Añade un elipsoide (radios x, y, z) al bmesh."""
    matriz = Matrix.Translation(Vector(centro)) @ Matrix.Diagonal((*radios, 1.0))
    r = bmesh.ops.create_uvsphere(bm, u_segments=segmentos, v_segments=anillos, radius=1.0, matrix=matriz)
    caras = {f for v in r["verts"] for f in v.link_faces}
    for f in caras:
        f.material_index = indice_material
    return r["verts"]


def caja_en(bm, centro, tamano, indice_material=0):
    matriz = Matrix.Translation(Vector(centro)) @ Matrix.Diagonal((*tamano, 1.0))
    r = bmesh.ops.create_cube(bm, size=1.0, matrix=matriz)
    caras = {f for v in r["verts"] for f in v.link_faces}
    for f in caras:
        f.material_index = indice_material


def cilindro_en(bm, centro, radio, alto, indice_material=0, segmentos=24):
    matriz = Matrix.Translation(Vector(centro))
    r = bmesh.ops.create_cone(bm, cap_ends=True, segments=segmentos, radius1=radio, radius2=radio,
                              depth=alto, matrix=matriz)
    caras = {f for v in r["verts"] for f in v.link_faces}
    for f in caras:
        f.material_index = indice_material


def malla_de_bmesh(nombre, bm):
    me = bpy.data.meshes.new(nombre)
    bm.to_mesh(me)
    bm.free()
    suavizar(me)
    return me


def curva_a_malla(obj_curva, nombre):
    """Convierte una curva con grosor en malla (el FBX solo lleva mallas)."""
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(obj_curva.evaluated_get(dg))
    me.name = nombre
    materiales = list(obj_curva.data.materials)
    bpy.data.objects.remove(obj_curva)
    obj = nuevo_objeto(nombre, me)
    for m in materiales:
        if m is not None and m.name not in [x.name for x in me.materials if x]:
            me.materials.append(m)
    suavizar(me)
    return obj


def tubo(nombre, puntos, radio, mat):
    cu = bpy.data.curves.new(nombre, "CURVE")
    cu.dimensions = "3D"
    cu.bevel_depth = radio
    cu.bevel_resolution = 4
    cu.use_fill_caps = True
    sp = cu.splines.new("BEZIER")
    sp.bezier_points.add(len(puntos) - 1)
    for i, p in enumerate(puntos):
        bp = sp.bezier_points[i]
        bp.co = p
        bp.handle_left_type = bp.handle_right_type = "AUTO"
    cu.materials.append(mat)
    obj = bpy.data.objects.new(nombre, cu)
    bpy.context.scene.collection.objects.link(obj)
    return obj


# ---------------------------------------------------------------------------
# Escena limpia y materiales
# ---------------------------------------------------------------------------

bpy.ops.wm.read_factory_settings(use_empty=True)

PIEL = material("Piel", (0.88, 0.30, 0.24), 0.75)
OJO = material("Ojo", (0.97, 0.94, 0.88), 0.35)
PUPILA = material("Pupila", (0.06, 0.04, 0.04), 0.2)
BRILLO = material("Brillo", (1.0, 1.0, 1.0), 0.1)
BOCA = material("Boca", (0.32, 0.08, 0.06), 0.6)
BOTA = material("Bota", (0.06, 0.42, 0.42), 0.6)
SUELA = material("Suela", (0.06, 0.30, 0.30), 0.8)

# Textura de "plastilina": irregularidades muy suaves en el cuerpo
textura_barro = bpy.data.textures.new("Barro", type="CLOUDS")
textura_barro.noise_scale = 0.06

exportar = []

# ---------------------------------------------------------------------------
# Cuerpo: triángulo invertido con grosor que se estrecha hacia la punta
# ---------------------------------------------------------------------------

bm = bmesh.new()
w, zt, zb = CUERPO_MEDIO_ANCHO, CUERPO_ARRIBA, CUERPO_PUNTA
zm, wm = ALTURA_GROSOR_MEDIO, medio_ancho_en(ALTURA_GROSOR_MEDIO)
gt, gm, gb = GROSOR_ARRIBA, GROSOR_MEDIO, GROSOR_PUNTA
# De frente es un triángulo; de lado, una cometa (fino arriba, gordo en el medio, afilado abajo).
ad_i, ad_d, at_i, at_d = [bm.verts.new(p) for p in [(-w, -gt, zt), (w, -gt, zt), (-w, gt, zt), (w, gt, zt)]]   # arriba
md_i, md_d, mt_i, mt_d = [bm.verts.new(p) for p in [(-wm, -gm, zm), (wm, -gm, zm), (-wm, gm, zm), (wm, gm, zm)]]  # medio
pd, pt = bm.verts.new((0, -gb, zb)), bm.verts.new((0, gb, zb))                                                    # punta
for cara in (
    [ad_i, ad_d, at_d, at_i],          # tapa de arriba
    [ad_i, md_i, md_d, ad_d],          # delante, parte alta
    [md_i, pd, md_d],                  # delante, parte baja
    [at_i, at_d, mt_d, mt_i],          # detrás, parte alta
    [mt_i, mt_d, pt],                  # detrás, parte baja
    [ad_i, at_i, mt_i, md_i],          # lado izquierdo alto
    [md_i, mt_i, pt, pd],              # lado izquierdo bajo
    [ad_d, md_d, mt_d, at_d],          # lado derecho alto
    [md_d, pd, pt, mt_d],              # lado derecho bajo
):
    bm.faces.new(cara)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
cuerpo = nuevo_objeto("Cuerpo", malla_de_bmesh("Cuerpo", bm), materiales=[PIEL])

bisel = cuerpo.modifiers.new("Bisel", "BEVEL")
bisel.width = 0.075
bisel.segments = 4
bisel.limit_method = "NONE"
sub = cuerpo.modifiers.new("Suave", "SUBSURF")
sub.levels = 2
sub.render_levels = 2
barro = cuerpo.modifiers.new("Barro", "DISPLACE")
barro.texture = textura_barro
barro.strength = 0.006
exportar.append(cuerpo)

# ---------------------------------------------------------------------------
# Cara: ojos, pupilas, brillos, párpados enfadados y sonrisa pícara
# ---------------------------------------------------------------------------

for lado, sufijo in ((-1, "Izq"), (1, "Der")):
    x = OJO_SEPARACION * lado
    y_cara = -grosor_en(OJO_ALTURA)
    centro_ojo = Vector((x, y_cara + 0.045, OJO_ALTURA))

    bm = bmesh.new()
    esfera_en(bm, (0, 0, 0), (OJO_RADIO, OJO_RADIO * 0.85, OJO_RADIO * 1.05))
    ojo = nuevo_objeto(f"Ojo{sufijo}", malla_de_bmesh(f"Ojo{sufijo}", bm), centro_ojo, [OJO])
    exportar.append(ojo)

    # Pupila un poco hacia dentro y abajo: mirada traviesa
    centro_pupila = centro_ojo + Vector((-0.025 * lado, -OJO_RADIO * 0.78, -0.01))
    bm = bmesh.new()
    esfera_en(bm, (0, 0, 0), (0.062, 0.035, 0.068))
    pupila = nuevo_objeto(f"Pupila{sufijo}", malla_de_bmesh(f"Pupila{sufijo}", bm), centro_pupila, [PUPILA])
    exportar.append(pupila)

    bm = bmesh.new()
    esfera_en(bm, (0, 0, 0), (0.02, 0.012, 0.02), segmentos=16, anillos=8)
    brillo = nuevo_objeto(f"Brillo{sufijo}", malla_de_bmesh(f"Brillo{sufijo}", bm),
                          centro_pupila + Vector((0.018, -0.03, 0.028)), [BRILLO])
    exportar.append(brillo)

    # Párpado: elipsoide del color del cuerpo que tapa la parte de arriba del ojo,
    # inclinado con el lado de dentro más bajo (cara de enfado).
    bm = bmesh.new()
    esfera_en(bm, (0, 0, 0), (0.165, 0.12, 0.055))
    parpado = nuevo_objeto(f"Parpado{sufijo}", malla_de_bmesh(f"Parpado{sufijo}", bm),
                           centro_ojo + Vector((0.0, 0.0, 0.08)), [PIEL])
    parpado.rotation_euler = (0.0, math.radians(-22.0 * lado), 0.0)
    exportar.append(parpado)

# Sonrisa: arco de abajo, un poco torcida (más alta por un lado)
y_boca = -grosor_en(BOCA_ALTURA) - 0.004
puntos = []
for i in range(9):
    a = math.radians(205 + (335 - 205) * i / 8)
    x = 0.015 + 0.085 * math.cos(a)
    z = BOCA_ALTURA + 0.055 * math.sin(a) + 0.012 * (i / 8)
    puntos.append(Vector((x, y_boca, z)))
boca = curva_a_malla(tubo("Boca", puntos, 0.013, BOCA), "Boca")
exportar.append(boca)

# ---------------------------------------------------------------------------
# Manoplas: puño redondo con pulgar
# ---------------------------------------------------------------------------

for lado, sufijo in ((-1, "Izq"), (1, "Der")):
    bm = bmesh.new()
    esfera_en(bm, (0, 0, 0), (0.125, 0.115, 0.135))
    esfera_en(bm, (-0.05 * lado, -0.085, 0.05), (0.055, 0.05, 0.058), segmentos=20, anillos=10)
    mano = nuevo_objeto(f"Mano{sufijo}", malla_de_bmesh(f"Mano{sufijo}", bm),
                        (MANO_POSICION.x * lado, MANO_POSICION.y, MANO_POSICION.z), [PIEL])
    exportar.append(mano)

# ---------------------------------------------------------------------------
# Botas: zapatón redondo, puño alrededor del tobillo y suela con bisel
# (origen de cada bota: en el suelo, bajo el tobillo)
# ---------------------------------------------------------------------------

for lado, sufijo in ((-1, "Izq"), (1, "Der")):
    bm = bmesh.new()
    esfera_en(bm, (0, -0.07, 0.12), (0.15, 0.24, 0.115), indice_material=0)
    esfera_en(bm, (0, -0.2, 0.1), (0.13, 0.12, 0.09), indice_material=0)          # puntera abombada
    cilindro_en(bm, (0, 0.0, 0.215), 0.07, 0.07, indice_material=0)                 # caña
    caja_en(bm, (0, -0.07, 0.03), (0.30, 0.50, 0.06), indice_material=1)            # suela
    bota = nuevo_objeto(f"Bota{sufijo}", malla_de_bmesh(f"Bota{sufijo}", bm),
                        (BOTA_X * lado, 0, 0), [BOTA, SUELA])
    bisel_bota = bota.modifiers.new("Bisel", "BEVEL")
    bisel_bota.width = 0.02
    bisel_bota.segments = 3
    bisel_bota.limit_method = "ANGLE"
    sub_bota = bota.modifiers.new("Suave", "SUBSURF")
    sub_bota.levels = 1
    sub_bota.render_levels = 1
    exportar.append(bota)

# ---------------------------------------------------------------------------
# Solo para las vistas previas: brazos y piernas de fideo (en Unity se generan en vivo)
# ---------------------------------------------------------------------------

solo_vista = []
for lado in (-1, 1):
    hombro = Vector((medio_ancho_en(1.05) * 0.9 * lado, 0.0, 1.05))
    mano = Vector((MANO_POSICION.x * lado, MANO_POSICION.y, MANO_POSICION.z))
    solo_vista.append(tubo("BrazoVista", [hombro, hombro + Vector((0.2 * lado, 0, 0.03)), mano + Vector((0, 0, 0.12)), mano], 0.036, PIEL))

    cadera = Vector((medio_ancho_en(0.74) * 0.8 * lado, 0.0, 0.74))
    tobillo = Vector((BOTA_X * lado, 0.0, 0.24))
    solo_vista.append(tubo("PiernaVista", [cadera, cadera + Vector((0.06 * lado, 0, -0.2)), tobillo], 0.036, PIEL))

# ---------------------------------------------------------------------------
# Exportar
# ---------------------------------------------------------------------------

if RUTA_BLEND:
    os.makedirs(os.path.dirname(RUTA_BLEND), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=RUTA_BLEND)
    print(f"Guardado: {RUTA_BLEND}")

if RUTA_FBX:
    os.makedirs(os.path.dirname(RUTA_FBX), exist_ok=True)
    for obj in bpy.context.scene.objects:
        obj.select_set(obj in exportar)
    bpy.context.view_layer.objects.active = cuerpo
    bpy.ops.export_scene.fbx(
        filepath=RUTA_FBX,
        use_selection=True,
        object_types={"MESH"},
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        add_leaf_bones=False,
        path_mode="STRIP",
    )
    print(f"Exportado: {RUTA_FBX}")

# ---------------------------------------------------------------------------
# Vistas previas (frontal, lateral, trasera y 3/4), como la hoja de referencia
# ---------------------------------------------------------------------------

if CARPETA_VISTAS:
    os.makedirs(CARPETA_VISTAS, exist_ok=True)
    escena = bpy.context.scene

    motor = None
    for candidato in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "BLENDER_WORKBENCH"):
        try:
            escena.render.engine = candidato
            motor = candidato
            break
        except Exception:
            continue
    print(f"Motor de render: {motor}")
    if motor == "BLENDER_WORKBENCH":
        escena.display.shading.light = "STUDIO"
        escena.display.shading.color_type = "MATERIAL"

    mundo = bpy.data.worlds.new("Fondo")
    escena.world = mundo
    try:
        mundo.use_nodes = True
    except Exception:
        pass
    if getattr(mundo, "node_tree", None):
        fondo = next((n for n in mundo.node_tree.nodes if n.type == "BACKGROUND"), None)
        if fondo:
            fondo.inputs["Color"].default_value = (*srgb_a_lineal((0.9, 0.9, 0.9)), 1.0)
            fondo.inputs["Strength"].default_value = 0.8
    mundo.color = srgb_a_lineal((0.9, 0.9, 0.9))

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

    camara = bpy.data.objects.new("Camara", bpy.data.cameras.new("Camara"))
    camara.data.type = "ORTHO"
    camara.data.ortho_scale = 2.1
    escena.collection.objects.link(camara)
    escena.camera = camara
    escena.render.resolution_x = 700
    escena.render.resolution_y = 700
    escena.render.film_transparent = False

    centro = Vector((0, 0, 0.8))
    vistas = {
        "frontal": Vector((0, -6, 0.8)),
        "lateral": Vector((6, 0, 0.8)),
        "trasera": Vector((0, 6, 0.8)),
        "tres_cuartos": Vector((4.2, -4.2, 1.6)),
    }
    for nombre, posicion in vistas.items():
        camara.location = posicion
        direccion = centro - posicion
        camara.rotation_euler = direccion.to_track_quat("-Z", "Y").to_euler()
        escena.render.filepath = os.path.join(CARPETA_VISTAS, f"triangulin_{nombre}.png")
        bpy.ops.render.render(write_still=True)
        print(f"Vista: {escena.render.filepath}")

print("Triangulín listo.")
