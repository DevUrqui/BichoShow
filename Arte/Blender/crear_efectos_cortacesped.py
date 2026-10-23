"""
Bicho Show — efectos de "El Cortacésped Mecha" hechos en Blender (por MCP o con la consola):
texturas suaves (con jirones de ruido de Blender) y mallas para el viento y el láser.

  Texturas (Assets/BichoShow/Texturas/Efectos, blancas: el color lo pone el material):
    Racha.png     racha de viento: cabeza brillante y cola larga deshilachada
    Remolino.png  brazos en espiral (se repite a lo ancho) para el suelo
    Embudo.png    franjas que se retuercen para el ciclón que sube a la turbina
    Brillo.png    núcleo intenso y halo suave (aros láser)
    Hoja.png      recorte de césped (hoja estrecha y afilada)
  Mallas (Arte/Efectos/mallas_cortacesped.json, en coordenadas de Unity: Y arriba):
    RachaViento   dos cintas curvas cruzadas (se ven desde arriba y de lado), hacia +Z
    Remolino      anillo plano r 7,3-21,7 con UV polar (4 brazos)
    Embudo        tronco de cono alrededor del robot, de r 5,2 abajo a r 1,6 arriba
    Recorte       hoja de césped curvada, dos cintas cruzadas

No toca la escena abierta: trabaja en una escena aparte y no guarda el .blend.
"""

import json
import math
import os

import bpy
import numpy as np
from mathutils import Vector, noise

RAIZ = r"F:/Proyectos/UnityProyectos/BichoShow/BichoShow"
CARPETA_TEX = os.path.join(RAIZ, "Assets/BichoShow/Texturas/Efectos")
CARPETA_MALLAS = os.path.join(RAIZ, "Arte/Efectos")
os.makedirs(CARPETA_TEX, exist_ok=True)
os.makedirs(CARPETA_MALLAS, exist_ok=True)

# Una escena aparte para no tocar lo que haya abierto.
escena = bpy.data.scenes.get("EfectosBichoShow") or bpy.data.scenes.new("EfectosBichoShow")


def suave(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def ruido(u, v, escala_u, escala_v, periodico=False, semilla=0.0):
    """Ruido de Blender (turbulencia) en una rejilla. Si es periódico en u, se repite a lo ancho."""
    h, w = u.shape
    out = np.zeros((h, w), dtype=np.float32)
    for j in range(h):
        for i in range(w):
            if periodico:
                a = u[j, i] * 2.0 * math.pi
                p = Vector((math.cos(a) * escala_u, math.sin(a) * escala_u, v[j, i] * escala_v + semilla))
            else:
                p = Vector((u[j, i] * escala_u, v[j, i] * escala_v, semilla))
            out[j, i] = noise.turbulence(p, 3, False)
    return out


def guardar(nombre, alfa, ancho, alto):
    alfa = np.clip(alfa, 0.0, 1.0).astype(np.float32)
    rgba = np.ones((alto, ancho, 4), dtype=np.float32)
    rgba[..., 3] = alfa
    img = bpy.data.images.get(nombre) or bpy.data.images.new(nombre, ancho, alto, alpha=True)
    if img.size[0] != ancho or img.size[1] != alto:
        img.scale(ancho, alto)
    img.pixels.foreach_set(rgba.ravel())
    ruta = os.path.join(CARPETA_TEX, nombre + ".png")
    img.filepath_raw = ruta
    img.file_format = "PNG"
    img.save()
    return ruta


def rejilla(ancho, alto):
    u = (np.arange(ancho) + 0.5) / ancho
    v = (np.arange(alto) + 0.5) / alto
    return np.meshgrid(u, v)


texturas = []

# --- Racha: la cabeza (u = 1) brilla; la cola se deshilacha ---
u, v = rejilla(256, 64)
# Tres hebras finas (de distinto largo) y un halo suave: líneas de viento de dibujo animado.
racha = np.zeros_like(u)
for centro, inicio, fuerza in ((0.5, 0.05, 1.0), (0.3, 0.35, 0.7), (0.7, 0.25, 0.75)):
    hebra = np.exp(-((v - centro) / 0.045) ** 2)
    largo = suave(inicio, 0.8, u) * (1.0 - suave(0.94, 1.0, u))
    racha += hebra * largo * fuerza
halo = np.exp(-((v - 0.5) / 0.22) ** 2) * suave(0.2, 0.85, u) * (1.0 - suave(0.92, 1.0, u)) * 0.3
jirones = 0.55 + 0.45 * ruido(u, v, 9.0, 2.0, semilla=1.3)
texturas.append(guardar("Racha", (racha + halo) * jirones * 1.3, 256, 64))

# --- Remolino: brazos en espiral (diagonales en UV polar), se repite en u ---
u, v = rejilla(256, 128)
fase = (u + v * 0.55) % 1.0
brazo = np.exp(-((fase - 0.5) / 0.11) ** 2) * 0.6 + np.exp(-((fase - 0.5) / 0.035) ** 2)
borde = suave(0.0, 0.18, v) * (1.0 - suave(0.78, 1.0, v))
jirones = 0.5 + 0.5 * ruido(u, v, 3.0, 4.0, periodico=True, semilla=4.2)
texturas.append(guardar("Remolino", brazo * borde * jirones, 256, 128))

# --- Embudo: franjas que suben retorciéndose; se apaga arriba y abajo ---
u, v = rejilla(256, 128)
fase = (u * 2.0 + v * 1.1) % 1.0
franja = np.exp(-((fase - 0.5) / 0.13) ** 2) * 0.6 + np.exp(-((fase - 0.5) / 0.04) ** 2)
borde = suave(0.0, 0.25, v) * (1.0 - suave(0.7, 1.0, v))
jirones = 0.45 + 0.55 * ruido(u, v, 4.0, 3.0, periodico=True, semilla=7.7)
texturas.append(guardar("Embudo", franja * borde * jirones, 256, 128))

# --- Brillo: núcleo intenso y halo suave (de abajo a arriba del aro) ---
u, v = rejilla(16, 64)
brillo = np.exp(-((v - 0.5) / 0.07) ** 2) + 0.55 * np.exp(-((v - 0.5) / 0.26) ** 2)
texturas.append(guardar("Brillo", brillo, 16, 64))

# --- Hoja: recorte de césped, estrecho y afilado ---
u, v = rejilla(32, 64)
mitad = 0.48 * np.power(np.clip(1.0 - v, 0.0, 1.0), 0.6)
hoja = 1.0 - suave(mitad - 0.06, mitad, np.abs(u - 0.5))
texturas.append(guardar("Hoja", hoja, 32, 64))

# ---------------------------------------------------------------------------
# Mallas (Unity: X derecha, Y arriba, Z adelante). Las de efectos se ven por las dos caras.
# ---------------------------------------------------------------------------


def malla(nombre, verts, uvs, tris):
    return {"nombre": nombre, "v": [round(c, 5) for p in verts for c in p], "uv": [round(c, 5) for p in uvs for c in p], "t": tris}


def cinta(centros, normal_de, ancho, verts, uvs, tris, ancho_final=None):
    """Cinta a lo largo de una curva de puntos; normal_de(i) da hacia dónde se abre."""
    base = len(verts)
    n = len(centros)
    for i, c in enumerate(centros):
        k = i / (n - 1)
        a = ancho if ancho_final is None else ancho + (ancho_final - ancho) * k
        lado = normal_de(i)
        verts.append(tuple(c - lado * a * 0.5))
        verts.append(tuple(c + lado * a * 0.5))
        uvs.append((k, 0.0))
        uvs.append((k, 1.0))
    for i in range(n - 1):
        a = base + i * 2
        tris += [a, a + 1, a + 3, a, a + 3, a + 2]


mallas = []

# RachaViento: 3 m de largo hacia +Z, con una ligera curva hacia +X; dos cintas cruzadas.
verts, uvs, tris = [], [], []
puntos = [Vector((0.35 * math.sin(k * math.pi * 0.5) ** 2, 0.0, -1.5 + 3.0 * k)) for k in [i / 15 for i in range(16)]]
cinta(puntos, lambda i: Vector((1.0, 0.0, 0.0)), 0.32, verts, uvs, tris, 0.18)   # plana (se ve desde arriba)
cinta(puntos, lambda i: Vector((0.0, 1.0, 0.0)), 0.32, verts, uvs, tris, 0.18)   # de canto (se ve de lado)
mallas.append(malla("RachaViento", verts, uvs, tris))

# Remolino: anillo plano con UV polar.
verts, uvs, tris = [], [], []
r0, r1, nA, nR = 7.3, 21.7, 96, 8
for j in range(nR + 1):
    r = r0 + (r1 - r0) * j / nR
    for i in range(nA + 1):
        a = i / nA * 2 * math.pi
        verts.append((math.cos(a) * r, 0.0, math.sin(a) * r))
        uvs.append((i / nA * 4.0, j / nR))
for j in range(nR):
    for i in range(nA):
        a = j * (nA + 1) + i
        b = a + nA + 1
        tris += [a, b, b + 1, a, b + 1, a + 1]
mallas.append(malla("Remolino", verts, uvs, tris))

# Embudo: tronco de cono alrededor del robot.
verts, uvs, tris = [], [], []
nA, nY = 64, 10
for j in range(nY + 1):
    k = j / nY
    r = 5.2 + (1.6 - 5.2) * (k ** 0.8)
    y = 0.3 + 7.7 * k
    for i in range(nA + 1):
        a = i / nA * 2 * math.pi
        verts.append((math.cos(a) * r, y, math.sin(a) * r))
        uvs.append((i / nA * 3.0, k))
for j in range(nY):
    for i in range(nA):
        a = j * (nA + 1) + i
        b = a + nA + 1
        tris += [a, b, b + 1, a, b + 1, a + 1]
mallas.append(malla("Embudo", verts, uvs, tris))

# Recorte: hoja de césped curvada (0,6 m), dos cintas cruzadas.
verts, uvs, tris = [], [], []
puntos = [Vector((0.0, 0.08 * math.sin(k * math.pi), -0.3 + 0.6 * k)) for k in [i / 7 for i in range(8)]]
cinta(puntos, lambda i: Vector((1.0, 0.0, 0.0)), 0.14, verts, uvs, tris)
cinta(puntos, lambda i: Vector((0.0, 1.0, 0.0)), 0.14, verts, uvs, tris)
mallas.append(malla("Recorte", verts, uvs, tris))

ruta_mallas = os.path.join(CARPETA_MALLAS, "mallas_cortacesped.json")
with open(ruta_mallas, "w", encoding="utf-8") as f:
    json.dump({"mallas": mallas}, f)

result = {"texturas": texturas, "mallas": ruta_mallas, "piezas": [m["nombre"] for m in mallas], "escena": escena.name}
