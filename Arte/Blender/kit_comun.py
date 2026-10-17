"""
Bicho Show — utilidades comunes de los kits de objetos (crear_kit_*.py).

Cada kit es un .py que se lanza sin abrir Blender:
    blender -b --factory-startup -P crear_kit_X.py -- <salida.fbx> <salida.blend> [carpeta_vistas]

Cada objeto se exporta con su origen en el centro de su base (Z = 0), y todos en el origen
de la escena: Unity los coge por nombre y los coloca él (Editor/KitObjetos.cs).

Orientación: lo que aquí mira hacia -Y, en Unity mira hacia +Z. Unity (x, y, z) = (-X, Z, -Y).
Los colores salen de los materiales (Unity los copia por nombre); "Equipo" lo cambia Unity
por el color de cada equipo. Ojo: Unity guarda los materiales por nombre (Materiales/Kit),
así que un mismo nombre en dos kits tiene que llevar el mismo color.
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector


def argumentos():
    """(ruta_fbx, ruta_blend, carpeta_vistas) de la línea de comandos (lo que va tras '--')."""
    a = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    return tuple(a[i] if len(a) > i else None for i in range(3))


# ---------------------------------------------------------------------------
# Materiales
# ---------------------------------------------------------------------------

MATERIALES = {}


def srgb_a_lineal(c):
    return tuple((v / 12.92) if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in c)


def crear_materiales(colores, brillantes=(), emisivos=()):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    MATERIALES.clear()
    for nombre, color in colores.items():
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
                bsdf.inputs["Roughness"].default_value = 0.25 if nombre in brillantes else 0.7
                if nombre in emisivos:
                    bsdf.inputs["Emission Color"].default_value = lineal
                    bsdf.inputs["Emission Strength"].default_value = 3.0
        MATERIALES[nombre] = m
    return MATERIALES


# ---------------------------------------------------------------------------
# Construcción con bmesh: cada objeto es una sola malla con varios materiales
# ---------------------------------------------------------------------------


def rot(r):
    """Matriz de giro a partir de grados (X, Y, Z), aplicados en ese orden."""
    return (Matrix.Rotation(math.radians(r[2]), 4, "Z") @ Matrix.Rotation(math.radians(r[1]), 4, "Y")
            @ Matrix.Rotation(math.radians(r[0]), 4, "X"))


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

    def _rayas(self, verts, matriz, rayas):
        """Gajos alternos alrededor del eje Z local de la primitiva: rayas = (matA, matB, número)."""
        a, b, n = rayas
        ia, ib = self._indice(a), self._indice(b)
        inversa = matriz.inverted()
        for f in {f for v in verts for f in v.link_faces}:
            c = inversa @ f.calc_center_median()
            angulo = math.atan2(c.y, c.x) % (2 * math.pi)
            f.material_index = ia if int(angulo / (2 * math.pi) * n) % 2 == 0 else ib

    def caja(self, centro, tamano, material, rotacion=(0, 0, 0)):
        m = Matrix.Translation(Vector(centro)) @ rot(rotacion) @ Matrix.Diagonal((*tamano, 1.0))
        r = bmesh.ops.create_cube(self.bm, size=1.0, matrix=m)
        self._asignar(r["verts"], material)

    def esfera(self, centro, radios, material, rotacion=(0, 0, 0), segmentos=24, anillos=12):
        m = Matrix.Translation(Vector(centro)) @ rot(rotacion) @ Matrix.Diagonal((*radios, 1.0))
        r = bmesh.ops.create_uvsphere(self.bm, u_segments=segmentos, v_segments=anillos, radius=1.0, matrix=m)
        self._asignar(r["verts"], material)

    def cilindro(self, centro, radio, alto, material, rotacion=(0, 0, 0), radio_arriba=None, segmentos=20, rayas=None):
        m = Matrix.Translation(Vector(centro)) @ rot(rotacion)
        r = bmesh.ops.create_cone(self.bm, cap_ends=True, segments=segmentos, radius1=radio,
                                  radius2=radio if radio_arriba is None else radio_arriba, depth=alto, matrix=m)
        self._asignar(r["verts"], material)
        if rayas:
            self._rayas(r["verts"], m, rayas)

    def tubo(self, desde, hasta, radio, material, radio_final=None, segmentos=12):
        """Cilindro que va de un punto a otro (patas, troncos, varillas)."""
        a, b = Vector(desde), Vector(hasta)
        eje = b - a
        giro = Vector((0, 0, 1)).rotation_difference(eje.normalized()).to_matrix().to_4x4()
        m = Matrix.Translation((a + b) / 2) @ giro
        r = bmesh.ops.create_cone(self.bm, cap_ends=True, segments=segmentos, radius1=radio,
                                  radius2=radio if radio_final is None else radio_final, depth=eje.length, matrix=m)
        self._asignar(r["verts"], material)

    def toro(self, centro, radio, grosor, material, rotacion=(0, 0, 0), segmentos=32, segmentos_tubo=12, rayas=None,
             arco=360):
        """Rosco (flotador, aro) en el plano XY local. Con arco < 360, solo ese trozo (empieza en +X)."""
        m = Matrix.Translation(Vector(centro)) @ rot(rotacion)
        cerrado = arco >= 360
        anillos = []
        for i in range(segmentos if cerrado else segmentos + 1):
            u = math.radians(arco) * i / segmentos
            anillo = []
            for j in range(segmentos_tubo):
                v = 2 * math.pi * j / segmentos_tubo
                d = radio + grosor * math.cos(v)
                anillo.append(self.bm.verts.new(m @ Vector((d * math.cos(u), d * math.sin(u), grosor * math.sin(v)))))
            anillos.append(anillo)
        for i in range(segmentos):
            a, b = anillos[i], anillos[(i + 1) % len(anillos)]
            for j in range(segmentos_tubo):
                k = (j + 1) % segmentos_tubo
                self.bm.faces.new((a[j], b[j], b[k], a[k]))
        if not cerrado:
            for anillo in (anillos[0], anillos[-1]):
                self.bm.faces.new(anillo)
        verts = [v for anillo in anillos for v in anillo]
        self._asignar(verts, material)
        if rayas:
            self._rayas(verts, m, rayas)

    def crear(self, bisel=0.04):
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
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


# ---------------------------------------------------------------------------
# Guardar, exportar y vistas previas
# ---------------------------------------------------------------------------


def exportar(objetos, ruta_fbx, ruta_blend):
    if ruta_blend:
        os.makedirs(os.path.dirname(ruta_blend), exist_ok=True)
        bpy.ops.wm.save_as_mainfile(filepath=ruta_blend)

    if ruta_fbx:
        os.makedirs(os.path.dirname(ruta_fbx), exist_ok=True)
        for obj in bpy.context.scene.objects:
            obj.select_set(obj in objetos)
        bpy.context.view_layer.objects.active = objetos[0]
        bpy.ops.export_scene.fbx(
            filepath=ruta_fbx, use_selection=True, object_types={"MESH"}, use_mesh_modifiers=True,
            mesh_smooth_type="FACE", apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
            bake_space_transform=True, add_leaf_bones=False, path_mode="STRIP",
        )
        print(f"Exportado: {ruta_fbx}")


def vistas(objetos, carpeta, prefijo, lista_vistas, color_fondo=(0.86, 0.88, 0.95), material_suelo=None):
    """
    Renderiza vistas previas: cada vista pone en fila los objetos que nombra.
    lista_vistas = [(nombre, [nombres de objetos], separación, posición cámara, punto al que mira)]
    """
    if not carpeta:
        return
    os.makedirs(carpeta, exist_ok=True)
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
            fondo.inputs["Color"].default_value = (*srgb_a_lineal(color_fondo), 1.0)
            fondo.inputs["Strength"].default_value = 0.9

    sol = bpy.data.objects.new("Sol", bpy.data.lights.new("Sol", "SUN"))
    sol.data.energy = 3.5
    sol.rotation_euler = (math.radians(50), 0, math.radians(-35))
    escena.collection.objects.link(sol)

    suelo = bpy.data.objects.new("Suelo", bpy.data.meshes.new("Suelo"))
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=80)
    bm.to_mesh(suelo.data)
    bm.free()
    if material_suelo:
        suelo.data.materials.append(MATERIALES[material_suelo])
    escena.collection.objects.link(suelo)

    camara = bpy.data.objects.new("Camara", bpy.data.cameras.new("Camara"))
    camara.data.lens = 45
    escena.collection.objects.link(camara)
    escena.camera = camara
    escena.render.resolution_x = 1400
    escena.render.resolution_y = 700

    por_nombre = {o.name: o for o in objetos}
    for nombre, nombres, separacion, posicion_camara, mirar in lista_vistas:
        lista = [por_nombre[n] for n in nombres]
        x = 0.0
        for obj in lista:
            ancho = obj.dimensions.x
            obj.location = (x + ancho / 2, 0, 0)
            x += ancho + separacion
        for obj in lista:
            obj.location.x -= x / 2
        camara.location = posicion_camara
        camara.rotation_euler = (Vector(mirar) - Vector(posicion_camara)).to_track_quat("-Z", "Y").to_euler()
        for obj in objetos:
            obj.hide_render = obj not in lista
        escena.render.filepath = os.path.join(carpeta, f"{prefijo}_{nombre}.png")
        bpy.ops.render.render(write_still=True)
        print(f"Vista: {escena.render.filepath}")
