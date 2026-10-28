"""
Bicho Show — iconos del pase de temporada (para su pantalla en Unity).

Uso (sin abrir Blender; se abre el kit de vestuario para usar sus mismas prendas):
    blender -b <ruta>/Arte/Blender/KitVestuario.blend -P <ruta>/Arte/Blender/crear_iconos_pase.py -- <carpeta_salida>

Renderiza, con fondo transparente y en 3/4:
  - Cada prenda del pase (las mallas del vestuario, tal cual): <Prenda>.png
  - Los iconos de los retos y de la pantalla: Reto_<evento>.png, Candado.png, Reloj.png, Invitado.png…
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bmesh  # noqa: E402
import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import kit_comun  # noqa: E402
from kit_comun import Pieza, srgb_a_lineal  # noqa: E402

argumentos = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SALIDA = argumentos[0] if argumentos else os.path.join(os.path.dirname(os.path.abspath(__file__)), "vistas", "iconos_pase")
os.makedirs(SALIDA, exist_ok=True)

PRENDAS = ["Gafas3D", "CascoObra", "Flotador", "BigoteRizado", "Auriculares", "Guitarra",
           "Antifaz", "GorroCohete", "Banda", "GafasEstrella", "Tiara", "Alas"]

# Las prendas del kit (ya están en el .blend) y sus materiales.
prendas = {o.name: o for o in bpy.data.objects if o.type == "MESH"}
for m in bpy.data.materials:
    kit_comun.MATERIALES[m.name] = m


def material(nombre, color, rugosidad=0.6, emision=0.0):
    m = bpy.data.materials.get(nombre) or bpy.data.materials.new(nombre)
    lineal = (*srgb_a_lineal(color), 1.0)
    m.diffuse_color = lineal
    if getattr(m, "node_tree", None) is None:
        try:
            m.use_nodes = True
        except Exception:
            pass
    arbol = getattr(m, "node_tree", None)
    bsdf = next((n for n in arbol.nodes if n.type == "BSDF_PRINCIPLED"), None) if arbol else None
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = lineal
        bsdf.inputs["Roughness"].default_value = rugosidad
        if emision > 0:
            bsdf.inputs["Emission Color"].default_value = lineal
            bsdf.inputs["Emission Strength"].default_value = emision
    kit_comun.MATERIALES[nombre] = m


for nombre, color, rugosidad in (
    ("IcoOro", (1.0, 0.78, 0.18), 0.25), ("IcoOroOscuro", (0.85, 0.55, 0.1), 0.3), ("IcoBlanco", (0.97, 0.96, 0.94), 0.5),
    ("IcoNegro", (0.12, 0.11, 0.15), 0.4), ("IcoCarton", (0.82, 0.6, 0.36), 0.8), ("IcoCinta", (0.9, 0.8, 0.58), 0.7),
    ("IcoMorado", (0.6, 0.3, 0.95), 0.2), ("IcoRojo", (0.95, 0.28, 0.3), 0.4), ("IcoAzul", (0.3, 0.6, 1.0), 0.4),
    ("IcoMetal", (0.72, 0.74, 0.8), 0.25), ("IcoMadera", (0.62, 0.42, 0.25), 0.7), ("IcoPiel", (1.0, 0.42, 0.38), 0.5),
    ("IcoBarriga", (1.0, 0.93, 0.8), 0.6), ("IcoRosa", (1.0, 0.55, 0.75), 0.5), ("IcoVerde", (0.4, 0.85, 0.45), 0.5),
):
    material(nombre, color, rugosidad)


def estrella(p, centro, radio, grosor, mat, puntas=5, interior=0.45, rotacion=(90, 0, 0)):
    """Estrella con volumen (prisma de 2·puntas lados), en el plano XY local girado con rotacion."""
    m = Matrix.Translation(Vector(centro)) @ kit_comun.rot(rotacion)
    arriba, abajo = [], []
    for i in range(puntas * 2):
        a = math.pi / 2 + i * math.pi / puntas
        r = radio if i % 2 == 0 else radio * interior
        for z, lista in ((grosor / 2, arriba), (-grosor / 2, abajo)):
            lista.append(p.bm.verts.new(m @ Vector((r * math.cos(a), r * math.sin(a), z))))
    ca = p.bm.verts.new(m @ Vector((0, 0, grosor * 0.9)))
    cb = p.bm.verts.new(m @ Vector((0, 0, -grosor * 0.9)))
    n = len(arriba)
    for i in range(n):
        j = (i + 1) % n
        p.bm.faces.new((ca, arriba[i], arriba[j]))
        p.bm.faces.new((cb, abajo[j], abajo[i]))
        p.bm.faces.new((arriba[i], abajo[i], abajo[j], arriba[j]))
    p._asignar(arriba + abajo + [ca, cb], mat)


iconos = {}

# --- Retos -----------------------------------------------------------------

p = Pieza("Reto_estrellas")
estrella(p, (0, 0, 0.5), 0.5, 0.22, "IcoOro")
iconos[p.nombre] = p.crear(bisel=0.02)

p = Pieza("Reto_victoria")                                                     # trofeo
p.cilindro((0, 0, 0.05), 0.28, 0.1, "IcoOroOscuro")
p.cilindro((0, 0, 0.16), 0.2, 0.14, "IcoMadera")
p.cilindro((0, 0, 0.32), 0.05, 0.2, "IcoOro")
p.esfera((0, 0, 0.62), (0.3, 0.3, 0.3), "IcoOro", segmentos=28, anillos=14)
p.cilindro((0, 0, 0.78), 0.3, 0.06, "IcoOro", segmentos=28)
for s in (-1, 1):
    p.toro((0.32 * s, 0, 0.66), 0.11, 0.03, "IcoOro", rotacion=(90, 0, 0), segmentos=16, segmentos_tubo=6)
estrella(p, (0, -0.29, 0.6), 0.1, 0.03, "IcoBlanco")
iconos[p.nombre] = p.crear(bisel=0.0)

p = Pieza("Reto_entrega")                                                      # caja de mudanza
p.caja((0, 0, 0.35), (0.7, 0.6, 0.6), "IcoCarton")
p.caja((0, 0, 0.655), (0.72, 0.14, 0.012), "IcoCinta")
p.caja((0, -0.301, 0.45), (0.14, 0.012, 0.4), "IcoCinta")
p.caja((-0.18, -0.302, 0.2), (0.2, 0.01, 0.12), "IcoBlanco")                  # etiqueta
iconos[p.nombre] = p.crear(bisel=0.02)

p = Pieza("Reto_final")                                                        # bandera de cuadros
p.cilindro((-0.32, 0, 0.5), 0.03, 1.0, "IcoMetal", segmentos=10)
p.esfera((-0.32, 0, 1.02), (0.05, 0.05, 0.05), "IcoOro", segmentos=10, anillos=6)
for fila in range(4):
    for col in range(5):
        x = -0.27 + 0.11 * col + 0.055
        z = 0.95 - 0.1 * fila - 0.05
        onda = 0.03 * math.sin(col * 1.3)
        p.caja((x, onda, z), (0.11, 0.02, 0.1), "IcoNegro" if (fila + col) % 2 == 0 else "IcoBlanco")
iconos[p.nombre] = p.crear(bisel=0.0)

p = Pieza("Reto_meta")                                                         # cronómetro
p.cilindro((0, 0, 0.42), 0.36, 0.14, "IcoRojo", rotacion=(90, 0, 0), segmentos=32)
p.cilindro((0, -0.075, 0.42), 0.3, 0.02, "IcoBlanco", rotacion=(90, 0, 0), segmentos=32)
p.cilindro((0, 0, 0.84), 0.05, 0.1, "IcoMetal", segmentos=12)
p.cilindro((0, 0, 0.91), 0.09, 0.05, "IcoRojo", segmentos=16)
p.caja((0, -0.09, 0.5), (0.03, 0.01, 0.18), "IcoNegro")
p.caja((0.06, -0.09, 0.42), (0.14, 0.01, 0.03), "IcoNegro", rotacion=(0, -30, 0))
iconos[p.nombre] = p.crear(bisel=0.0)

p = Pieza("Reto_sabotaje")                                                     # el rombo del saboteador
p.esfera((0, 0, 0.5), (0.36, 0.36, 0.5), "IcoMorado", segmentos=4, anillos=2)
iconos[p.nombre] = p.crear(bisel=0.01)

p = Pieza("Reto_ronda")                                                        # claqueta
p.caja((0, 0, 0.3), (0.8, 0.08, 0.5), "IcoNegro")
p.caja((0, 0, 0.62), (0.8, 0.08, 0.12), "IcoBlanco", rotacion=(0, -12, 0))
for i in range(4):
    p.caja((-0.3 + 0.2 * i, -0.045, 0.62 + 0.04 * (1.5 - i) * 0.21), (0.08, 0.01, 0.12), "IcoNegro", rotacion=(0, -12 - 30, 0))
p.caja((0, -0.045, 0.3), (0.6, 0.01, 0.03), "IcoBlanco")
p.caja((0, -0.045, 0.18), (0.6, 0.01, 0.03), "IcoBlanco")
iconos[p.nombre] = p.crear(bisel=0.01)

p = Pieza("Reto_show")                                                         # micrófono del presentador
p.cilindro((0, 0, 0.25), 0.07, 0.5, "IcoNegro", radio_arriba=0.09, segmentos=16)
p.esfera((0, 0, 0.62), (0.17, 0.17, 0.17), "IcoMetal", segmentos=20, anillos=10)
p.cilindro((0, 0, 0.5), 0.12, 0.06, "IcoRojo", segmentos=16)
iconos[p.nombre] = p.crear(bisel=0.0)

p = Pieza("Reto_ganaShow")                                                     # corona
p.cilindro((0, 0, 0.18), 0.36, 0.2, "IcoOro", segmentos=32)
for i in range(5):
    a = 2 * math.pi * i / 5
    x, y = 0.33 * math.sin(a), -0.33 * math.cos(a)
    p.cilindro((x, y, 0.4), 0.09, 0.26, "IcoOro", radio_arriba=0.0, segmentos=8)
    p.esfera((x, y, 0.55), (0.045, 0.045, 0.045), "IcoOro", segmentos=10, anillos=6)
p.esfera((0, -0.37, 0.2), (0.07, 0.03, 0.07), "IcoRojo", segmentos=12, anillos=6)
iconos[p.nombre] = p.crear(bisel=0.0)

# --- Pantalla ---------------------------------------------------------------

p = Pieza("Candado")
p.caja((0, 0, 0.3), (0.56, 0.24, 0.46), "IcoOro")
p.toro((0, 0, 0.53), 0.18, 0.05, "IcoMetal", rotacion=(90, 0, 0), segmentos=24, segmentos_tubo=8, arco=180)
for s in (-1, 1):
    p.cilindro((0.18 * s, 0, 0.53), 0.05, 0.06, "IcoMetal", segmentos=10)
p.cilindro((0, -0.125, 0.34), 0.05, 0.02, "IcoNegro", rotacion=(90, 0, 0), segmentos=12)
p.caja((0, -0.125, 0.25), (0.04, 0.02, 0.12), "IcoNegro")
iconos[p.nombre] = p.crear(bisel=0.03)

p = Pieza("Reloj")                                                             # despertador
p.cilindro((0, 0, 0.45), 0.36, 0.16, "IcoMorado", rotacion=(90, 0, 0), segmentos=32)
p.cilindro((0, -0.085, 0.45), 0.29, 0.02, "IcoBlanco", rotacion=(90, 0, 0), segmentos=32)
for s in (-1, 1):
    p.esfera((0.26 * s, 0, 0.8), (0.13, 0.08, 0.1), "IcoOro", rotacion=(0, 30 * s, 0), segmentos=14, anillos=8)
    p.cilindro((0.22 * s, 0, 0.07), 0.04, 0.14, "IcoMorado", rotacion=(0, 25 * s, 0), segmentos=8)
p.caja((0, -0.1, 0.52), (0.03, 0.01, 0.16), "IcoNegro")
p.caja((0.06, -0.1, 0.45), (0.13, 0.01, 0.03), "IcoNegro", rotacion=(0, 20, 0))
iconos[p.nombre] = p.crear(bisel=0.0)

p = Pieza("Invitado")                                                          # la cara de un bicho
p.esfera((0, 0, 0.5), (0.48, 0.44, 0.5), "IcoAzul", segmentos=28, anillos=14)
p.esfera((0, -0.33, 0.3), (0.26, 0.1, 0.2), "IcoBarriga", segmentos=20, anillos=10)
for s in (-1, 1):
    p.esfera((0.16 * s, -0.33, 0.62), (0.13, 0.08, 0.15), "IcoBlanco", segmentos=16, anillos=8)
    p.esfera((0.15 * s, -0.41, 0.6), (0.06, 0.03, 0.08), "IcoNegro", segmentos=12, anillos=6)
    p.esfera((0.31 * s, -0.33, 0.44), (0.07, 0.02, 0.04), "IcoRosa", segmentos=10, anillos=5)
    p.tubo((0.12 * s, 0.0, 0.95), (0.24 * s, -0.06, 1.15), 0.025, "IcoAzul", segmentos=8)
    p.esfera((0.25 * s, -0.07, 1.17), (0.06, 0.06, 0.06), "IcoAzul", segmentos=10, anillos=6)
iconos[p.nombre] = p.crear(bisel=0.0)

# ---------------------------------------------------------------------------
# Render: cada icono solo, en 3/4, con cámara ortográfica ajustada a su tamaño
# ---------------------------------------------------------------------------

escena = bpy.context.scene
for candidato in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "BLENDER_WORKBENCH"):
    try:
        escena.render.engine = candidato
        break
    except Exception:
        continue
escena.render.film_transparent = True
escena.render.resolution_x = escena.render.resolution_y = 384
escena.render.image_settings.file_format = "PNG"
escena.render.image_settings.color_mode = "RGBA"
try:
    escena.view_settings.view_transform = "Standard"   # colores vivos, como en el juego
except Exception:
    pass

mundo = bpy.data.worlds.new("Iconos")
escena.world = mundo
try:
    mundo.use_nodes = True
    fondo = next(n for n in mundo.node_tree.nodes if n.type == "BACKGROUND")
    fondo.inputs["Color"].default_value = (0.85, 0.82, 1.0, 1.0)
    fondo.inputs["Strength"].default_value = 0.55
except Exception:
    pass

for nombre, energia, giro in (("Clave", 3.2, (50, 0, -30)), ("Relleno", 1.2, (60, 0, 140))):
    luz = bpy.data.objects.new(nombre, bpy.data.lights.new(nombre, "SUN"))
    luz.data.energy = energia
    luz.rotation_euler = tuple(math.radians(v) for v in giro)
    escena.collection.objects.link(luz)

camara = bpy.data.objects.new("CamaraIconos", bpy.data.cameras.new("CamaraIconos"))
camara.data.type = "ORTHO"
escena.collection.objects.link(camara)
escena.camera = camara
DIRECCION = Vector((0.55, -1.0, 0.42)).normalized()   # desde delante, a la derecha y un poco arriba
# Lo que va a la espalda se ve desde detrás; lo de la cara, más de frente.
DIRECCIONES = {
    "Alas": Vector((-0.45, 1.0, 0.5)).normalized(), "Guitarra": Vector((-0.35, 1.0, 0.3)).normalized(),
    "BigoteRizado": Vector((0.2, -1.0, 0.15)).normalized(), "Antifaz": Vector((0.3, -1.0, 0.2)).normalized(),
    "Banda": Vector((0.25, -1.0, 0.2)).normalized(), "Tiara": Vector((0.3, -1.0, 0.6)).normalized(),
}


def renderizar(obj, nombre):
    for o in bpy.data.objects:
        if o.type == "MESH":
            o.hide_render = o is not obj
    obj.location = (0, 0, 0)
    bpy.context.view_layer.update()
    esquinas = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    centro = sum(esquinas, Vector()) / 8
    direccion = DIRECCIONES.get(nombre, DIRECCION)
    giro = direccion.to_track_quat("Z", "Y")          # la cámara mira hacia -direccion
    derecha, arriba = giro @ Vector((1, 0, 0)), giro @ Vector((0, 1, 0))
    ancho = max(abs((c - centro).dot(derecha)) for c in esquinas) * 2
    alto = max(abs((c - centro).dot(arriba)) for c in esquinas) * 2
    camara.location = centro + direccion * 10
    camara.rotation_euler = giro.to_euler()
    camara.data.ortho_scale = max(ancho, alto) * 1.12
    escena.render.filepath = os.path.join(SALIDA, f"{nombre}.png")
    bpy.ops.render.render(write_still=True)
    print(f"Icono: {escena.render.filepath}")


for nombre in PRENDAS:
    if nombre in prendas:
        renderizar(prendas[nombre], nombre)
    else:
        print(f"Falta la prenda {nombre} en el kit")
for nombre, obj in iconos.items():
    renderizar(obj, nombre)

print("Iconos del pase listos.")
