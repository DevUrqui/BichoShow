"""
Bicho Show — "El Cortacésped Mecha": la chatarra nueva que escupe el robot y los anillos de
succión del viento, hechos en Blender (por el MCP de Blender o con la consola).

  Textura (Assets/BichoShow/Texturas/Efectos, blanca: el color lo pone el material):
    Succion.png   flechas en V con estela (se repite a lo ancho): apuntan hacia v = 0
  Mallas (se añaden a Arte/Efectos/mallas_cortacesped.json, en coordenadas de Unity: Y arriba):
    AnilloSuccion  anillo plano de radio 0,78-1 (16 flechas) (se escala al radio que toque), flechas hacia dentro
    AnilloSoplido  el mismo, con las flechas hacia fuera
    Neumatico      rueda de goma con tacos, eje X, radio ~1
    Llanta         el centro de la rueda (cilindro, eje X)
    Muelle         muelle de 5 vueltas, eje Y, 1 m de alto
    PernoCabeza    cabeza hexagonal del perno gigante (arriba)
    PernoRosca     vástago con rosca, arandela y punta (hacia abajo)
    Lata           lata de aceite (cuerpo con rebordes y boca)
    LataEtiqueta   banda de la etiqueta
  Las mallas llevan normales (n): aristas marcadas en lo anguloso y suaves en lo redondo.

No toca la escena abierta: trabaja con datos sueltos y no guarda el .blend.
"""

import json
import math
import os

import bpy
import bmesh
import numpy as np

RAIZ = r"F:/Proyectos/UnityProyectos/BichoShow/BichoShow"
CARPETA_TEX = os.path.join(RAIZ, "Assets/BichoShow/Texturas/Efectos")
RUTA_MALLAS = os.path.join(RAIZ, "Arte/Efectos/mallas_cortacesped.json")


def suave(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def guardar_textura(nombre, alfa):
    alto, ancho = alfa.shape
    rgba = np.ones((alto, ancho, 4), dtype=np.float32)
    rgba[..., 3] = np.clip(alfa, 0.0, 1.0)
    img = bpy.data.images.get(nombre) or bpy.data.images.new(nombre, ancho, alto, alpha=True)
    if tuple(img.size) != (ancho, alto):
        img.scale(ancho, alto)
    img.pixels.foreach_set(rgba.ravel())
    img.filepath_raw = os.path.join(CARPETA_TEX, nombre + ".png")
    img.file_format = "PNG"
    img.save()
    return nombre


# ---------------------------------------------------------------------------
# Textura: flechas en V hacia v = 0, con una estela detrás
# ---------------------------------------------------------------------------
n = 128
u, v = np.meshgrid((np.arange(n) + 0.5) / n, (np.arange(n) + 0.5) / n)
du = np.abs(u - 0.5)
linea = 0.22 + 1.05 * du                      # el centro de la V (la punta, abajo)
d = v - linea                                  # > 0: detrás de la flecha
flecha = 1.0 - suave(0.05, 0.085, np.abs(d))
estela = np.where(d > 0, np.exp(-d / 0.22) * 0.45, 0.0) * (1.0 - suave(0.32, 0.5, du))
alfa = np.maximum(flecha, estela) * suave(0.0, 0.08, v) * (1.0 - suave(0.9, 1.0, v))
texturas = [guardar_textura("Succion", alfa)]


# ---------------------------------------------------------------------------
# Mallas
# ---------------------------------------------------------------------------

def a_unity(p):
    """Blender (Z arriba) → Unity (Y arriba)."""
    return (p[0], p[2], p[1])


def exportar(nombre, me, angulo_duro=None):
    """Una malla de Blender a JSON de Unity: triángulos con sus normales (aristas duras por ángulo)."""
    me.shade_smooth()
    if angulo_duro is not None:
        me.set_sharp_from_angle(angle=angulo_duro)
    me.calc_loop_triangles()
    normales = me.corner_normals
    uv_capa = me.uv_layers.active.data if me.uv_layers.active else None
    verts, norms, uvs, tris = [], [], [], []
    vistos = {}
    for tri in me.loop_triangles:
        indices = []
        for li in tri.loops:
            co = me.vertices[me.loops[li].vertex_index].co
            no = normales[li].vector
            uv = tuple(uv_capa[li].uv) if uv_capa else (0.0, 0.0)
            clave = (round(co.x, 4), round(co.y, 4), round(co.z, 4), round(no.x, 2), round(no.y, 2), round(no.z, 2), round(uv[0], 3), round(uv[1], 3))
            if clave not in vistos:
                vistos[clave] = len(verts)
                verts.append(a_unity(co))
                norms.append(a_unity(no))
                uvs.append(uv)
            indices.append(vistos[clave])
        # Cambiar de mano (Blender → Unity) invierte el sentido de los triángulos.
        tris += [indices[0], indices[2], indices[1]]
    return {
        "nombre": nombre,
        "v": [round(c, 5) for p in verts for c in p],
        "n": [round(c, 4) for p in norms for c in p],
        "uv": [round(c, 5) for p in uvs for c in p],
        "t": tris,
    }


def malla_de(verts, caras, uvs=None):
    me = bpy.data.meshes.new("ChatarraTemporal")
    me.from_pydata(verts, [], caras)
    if uvs is not None:
        capa = me.uv_layers.new()
        for poly in me.polygons:
            for li in poly.loop_indices:
                capa.data[li].uv = uvs[me.loops[li].vertex_index]
    me.update()
    return me


def torno(perfil, segmentos, eje="Z"):
    """Revoluciona un perfil [(radio, altura)] alrededor de un eje. Tapa los extremos con radio 0."""
    verts, caras = [], []
    for (r, h) in perfil:
        for i in range(segmentos):
            a = i / segmentos * 2 * math.pi
            c, s = math.cos(a) * r, math.sin(a) * r
            verts.append((c, s, h) if eje == "Z" else (h, c, s))
    for j in range(len(perfil) - 1):
        for i in range(segmentos):
            a = j * segmentos + i
            b = j * segmentos + (i + 1) % segmentos
            caras.append((a, b, b + segmentos, a + segmentos))
    return malla_de(verts, caras)


def tapar(me):
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.holes_fill(bm, edges=bm.edges, sides=0)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    return me


mallas_nuevas = []

# Anillos de succión (radio 0,86-1; u = 24 flechas alrededor; v de dentro a fuera).
for nombre, invertir in (("AnilloSuccion", False), ("AnilloSoplido", True)):
    nA = 96
    verts, uvs_l, caras = [], [], []
    for j, r in enumerate((0.78, 1.0)):
        for i in range(nA + 1):
            a = i / nA * 2 * math.pi
            verts.append((math.cos(a) * r, math.sin(a) * r, 0.0))
            vv = float(j)
            uvs_l.append((i / nA * 16.0, 1.0 - vv if invertir else vv))
    for i in range(nA):
        caras.append((i, i + 1, nA + 2 + i, nA + 1 + i))
    me = malla_de(verts, caras, uvs_l)
    mallas_nuevas.append(exportar(nombre, me))
    bpy.data.meshes.remove(me)

# Neumático: sección redondeada (superelipse), tacos en la banda; eje X.
nA, nB, R = 48, 16, 0.62
verts, caras = [], []
for i in range(nA):
    a = i / nA * 2 * math.pi
    taco = 0.07 if (i % 4) < 2 else 0.0
    for j in range(nB):
        b = j / nB * 2 * math.pi
        cb, sb = math.cos(b), math.sin(b)
        w = 0.4 * math.copysign(abs(sb) ** 0.6, sb)
        h = 0.3 * math.copysign(abs(cb) ** 0.6, cb)
        if cb > 0.75:
            h += taco
        radio = R + h
        verts.append((w, math.cos(a) * radio, math.sin(a) * radio))
for i in range(nA):
    for j in range(nB):
        a0, a1 = i * nB + j, ((i + 1) % nA) * nB + j
        caras.append((a0, a1, ((i + 1) % nA) * nB + (j + 1) % nB, i * nB + (j + 1) % nB))
me = malla_de(verts, caras)
mallas_nuevas.append(exportar("Neumatico", me, angulo_duro=math.radians(50)))
bpy.data.meshes.remove(me)

# Llanta: cilindro achatado con borde, eje X.
me = tapar(torno([(0.0, -0.3), (0.5, -0.3), (0.5, -0.24), (0.36, -0.2), (0.36, 0.2), (0.5, 0.24), (0.5, 0.3), (0.0, 0.3)], 24, eje="X"))
mallas_nuevas.append(exportar("Llanta", me, angulo_duro=math.radians(40)))
bpy.data.meshes.remove(me)

# Muelle: hélice de 5 vueltas (radio 0,38), tubo de 0,075, eje Z (Unity Y).
vueltas, pasos, lados, rt = 5, 260, 10, 0.075
verts, caras = [], []
for k in range(pasos + 1):
    t = k / pasos
    th = t * vueltas * 2 * math.pi
    z = -0.5 + t
    c = np.array((math.cos(th) * 0.38, math.sin(th) * 0.38, z))
    tangente = np.array((-math.sin(th) * 0.38 * vueltas * 2 * math.pi, math.cos(th) * 0.38 * vueltas * 2 * math.pi, 1.0))
    tangente /= np.linalg.norm(tangente)
    normal = np.array((math.cos(th), math.sin(th), 0.0))
    binormal = np.cross(tangente, normal)
    for s in range(lados):
        g = s / lados * 2 * math.pi
        p = c + (normal * math.cos(g) + binormal * math.sin(g)) * rt
        verts.append(tuple(p))
for k in range(pasos):
    for s in range(lados):
        caras.append((k * lados + s, k * lados + (s + 1) % lados, (k + 1) * lados + (s + 1) % lados, (k + 1) * lados + s))
me = tapar(malla_de(verts, caras))
mallas_nuevas.append(exportar("Muelle", me))
bpy.data.meshes.remove(me)

# Perno gigante: cabeza hexagonal arriba; arandela, rosca y punta hacia abajo (eje Z).
me = tapar(torno([(0.0, 0.62), (0.78, 0.62), (0.78, 1.1), (0.0, 1.1)], 6))
mallas_nuevas.append(exportar("PernoCabeza", me, angulo_duro=math.radians(30)))
bpy.data.meshes.remove(me)
perfil = [(0.0, 0.62), (0.9, 0.62), (0.9, 0.5), (0.34, 0.5)]
z = 0.5
while z > -1.05:
    perfil += [(0.34, z), (0.42, z - 0.05), (0.34, z - 0.1)]
    z -= 0.12
perfil += [(0.3, z), (0.0, z - 0.45)]
me = torno(perfil, 24)
tapar(me)
mallas_nuevas.append(exportar("PernoRosca", me, angulo_duro=math.radians(55)))
bpy.data.meshes.remove(me)

# Lata de aceite: cuerpo con rebordes, hombro y boca (eje Z, base en 0).
me = tapar(torno([(0.0, 0.0), (0.33, 0.0), (0.36, 0.04), (0.36, 0.08), (0.34, 0.1), (0.34, 0.68), (0.36, 0.7), (0.36, 0.74),
                  (0.3, 0.8), (0.14, 0.86), (0.14, 0.98), (0.17, 1.0), (0.17, 1.04), (0.0, 1.04)], 28))
mallas_nuevas.append(exportar("Lata", me, angulo_duro=math.radians(35)))
bpy.data.meshes.remove(me)
me = torno([(0.346, 0.2), (0.346, 0.58)], 28)
bm = bmesh.new()
bm.from_mesh(me)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
bm.to_mesh(me)
bm.free()
mallas_nuevas.append(exportar("LataEtiqueta", me))
bpy.data.meshes.remove(me)

# Se añaden (o sustituyen) en el archivo de mallas del Cortacésped.
with open(RUTA_MALLAS, encoding="utf-8") as f:
    datos = json.load(f)
nombres = {m["nombre"] for m in mallas_nuevas}
datos["mallas"] = [m for m in datos["mallas"] if m["nombre"] not in nombres] + mallas_nuevas
with open(RUTA_MALLAS, "w", encoding="utf-8") as f:
    json.dump(datos, f)

result = {"texturas": texturas, "mallas": {m["nombre"]: len(m["v"]) // 3 for m in mallas_nuevas}}
