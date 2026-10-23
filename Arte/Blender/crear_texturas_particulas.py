"""
Bicho Show — texturas de partículas para todo el juego, hechas en Blender (por el MCP de Blender
o con la consola). Blancas (el color lo pone cada partícula) con transparencia, en
Assets/BichoShow/Texturas/Efectos:

  Nube.png      4 nubecitas de dibujo animado (hoja 2x2): polvo de aterrizajes y porrazos
  Estrella.png  estrella de cinco puntas redondeadas, con el centro brillante y halo
  Destello.png  destello redondo con cruz de luz (golpes)
  Onda.png      aro fino que se abre (onda expansiva de los golpes)
  Chispa.png    chispa alargada (se estira con la velocidad)
  Confeti.png   4 formas de confeti (hoja 2x2): tira, círculo, estrella y serpentina
  Burbuja.png   pompa de jabón (borde, brillo y centro casi transparente)
  Gota.png      gota de agua con brillo
  Polen.png     bolita de polen peluda que brilla
  Cristal.png   grano de sal (cristalito con caras)

Usa el ruido de Blender (mathutils.noise) para los bordes de las nubes y el pelo del polen.
"""

import math
import os

import bpy
import numpy as np
from mathutils import Vector, noise

CARPETA = r"F:/Proyectos/UnityProyectos/BichoShow/BichoShow/Assets/BichoShow/Texturas/Efectos"
os.makedirs(CARPETA, exist_ok=True)


def suave(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def coords(n):
    c = (np.arange(n) + 0.5) / n * 2.0 - 1.0
    x, y = np.meshgrid(c, c)
    return x, y


def ruido2(x, y, escala, semilla):
    out = np.zeros_like(x)
    h, w = x.shape
    for j in range(h):
        for i in range(w):
            out[j, i] = noise.noise(Vector((x[j, i] * escala, y[j, i] * escala, semilla)))
    return out


def guardar(nombre, rgb, alfa):
    h, w = alfa.shape
    rgba = np.ones((h, w, 4), dtype=np.float32)
    rgba[..., 0] = rgba[..., 1] = rgba[..., 2] = np.clip(rgb, 0, 1) if np.ndim(rgb) == 2 else rgb
    rgba[..., 3] = np.clip(alfa, 0, 1)
    img = bpy.data.images.get(nombre) or bpy.data.images.new(nombre, w, h, alpha=True)
    if tuple(img.size) != (w, h):
        img.scale(w, h)
    img.pixels.foreach_set(rgba.ravel())
    img.filepath_raw = os.path.join(CARPETA, nombre + ".png")
    img.file_format = "PNG"
    img.save()
    return nombre


def hoja(celdas):
    """Junta 4 celdas cuadradas (rgb, alfa) en una hoja 2x2 (abajo-izquierda primero, como Unity)."""
    n = celdas[0][1].shape[0]
    rgb = np.ones((n * 2, n * 2), dtype=np.float32)
    alfa = np.zeros((n * 2, n * 2), dtype=np.float32)
    for k, (c_rgb, c_a) in enumerate(celdas):
        fila, col = k // 2, k % 2
        rgb[fila * n:(fila + 1) * n, col * n:(col + 1) * n] = c_rgb
        alfa[fila * n:(fila + 1) * n, col * n:(col + 1) * n] = c_a
    return rgb, alfa


hechas = []

# --- Nube: bolas que se juntan, borde con ruido, sombreado suave (más clara arriba) ---
celdas = []
for k, bolas in enumerate([
    [(0, -0.1, 0.5), (-0.38, -0.2, 0.36), (0.38, -0.18, 0.38), (0.05, 0.28, 0.4)],
    [(-0.15, -0.1, 0.48), (0.35, -0.05, 0.42), (-0.1, 0.32, 0.34), (0.3, 0.3, 0.28)],
    [(0, 0, 0.55), (-0.42, 0.1, 0.32), (0.42, -0.1, 0.3)],
    [(-0.25, -0.15, 0.42), (0.25, -0.15, 0.42), (0, 0.25, 0.42), (0.45, 0.25, 0.22)],
]):
    x, y = coords(128)
    campo = np.full_like(x, -1.0)
    for bx, by, r in bolas:
        campo = np.maximum(campo, 1.0 - np.sqrt((x - bx) ** 2 + (y - by) ** 2) / r)
    campo += ruido2(x, y, 6.0, 2.0 + k) * 0.12 * (campo > -0.3)
    alfa = suave(0.0, 0.12, campo)
    luz = 0.78 + 0.22 * suave(-0.6, 0.6, y) + 0.08 * ruido2(x, y, 9.0, 7.0 + k)
    celdas.append((luz, alfa))
hechas.append(guardar("Nube", *hoja(celdas)))

# --- Estrella: cinco puntas de lados rectos (gorditas), centro brillante y halo ---
def estrella(x, y, punta, valle):
    r = np.sqrt(x * x + y * y)
    ang = (np.arctan2(y, x) - math.pi / 2) % (2 * math.pi / 5)
    ang = np.minimum(ang, 2 * math.pi / 5 - ang)                     # plegado: 0 en la punta, pi/5 en el valle
    px, py = r * np.cos(ang), r * np.sin(ang)
    tx, ty = punta, 0.0
    vx, vy = valle * math.cos(math.pi / 5), valle * math.sin(math.pi / 5)
    dx, dy = vx - tx, vy - ty
    return (dx * (py - ty) - dy * (px - tx)) / math.hypot(dx, dy)   # > 0 dentro (distancia al borde)


x, y = coords(128)
r = np.sqrt(x * x + y * y)
cuerpo = suave(-0.02, 0.03, estrella(x, y, 0.92, 0.48))
halo = np.exp(-(r / 0.65) ** 2) * 0.35
brillo = 0.8 + 0.2 * suave(0.5, 0.0, r)
hechas.append(guardar("Estrella", brillo, np.maximum(cuerpo, halo)))

# --- Destello: núcleo redondo y cruz de luz ---
x, y = coords(128)
r = np.sqrt(x * x + y * y)
nucleo = np.exp(-(r / 0.22) ** 2)
cruz = np.exp(-(np.abs(x) / 0.04) ** 2) * np.exp(-(np.abs(y) / 0.9) ** 2) + np.exp(-(np.abs(y) / 0.04) ** 2) * np.exp(-(np.abs(x) / 0.9) ** 2)
halo = np.exp(-(r / 0.6) ** 2) * 0.4
hechas.append(guardar("Destello", np.ones_like(r), np.clip(nucleo + cruz * 0.8 + halo, 0, 1)))

# --- Onda: aro fino con el borde de fuera nítido y el de dentro difuso ---
x, y = coords(128)
r = np.sqrt(x * x + y * y)
aro = suave(0.62, 0.86, r) * (1.0 - suave(0.88, 0.96, r))
hechas.append(guardar("Onda", np.ones_like(r), aro))

# --- Chispa: alargada (a lo largo de y), cabeza brillante ---
x, y = coords(64)
chispa = np.exp(-(x / 0.18) ** 2) * suave(-1.0, -0.2, y) * (1.0 - suave(0.55, 1.0, y))
hechas.append(guardar("Chispa", np.ones_like(x), np.clip(chispa * 1.3, 0, 1)))

# --- Confeti: tira, círculo, estrella y serpentina (cada uno, en su celda) ---
celdas = []
x, y = coords(64)
celdas.append((np.ones_like(x), (np.abs(x) < 0.32) * (np.abs(y) < 0.78) * 1.0))                       # tira
celdas.append((np.ones_like(x), suave(0.0, 0.06, 0.72 - np.sqrt(x * x + y * y))))                   # círculo
r = np.sqrt(x * x + y * y)
ang = np.arctan2(y, x) + math.pi / 2
sector = np.abs(((ang / (2 * math.pi / 5)) % 1.0) - 0.5) * 2.0
celdas.append((np.ones_like(x), suave(0.0, 0.05, 0.38 + 0.5 * sector ** 1.5 - r)))                 # estrella
onda = np.abs(x - 0.35 * np.sin(y * 4.0)) < 0.2
celdas.append((np.ones_like(x), (onda * (np.abs(y) < 0.85)) * 1.0))                                 # serpentina
hechas.append(guardar("Confeti", *hoja(celdas)))

# --- Burbuja: borde, brillo arriba a la izquierda, centro casi transparente ---
x, y = coords(128)
r = np.sqrt(x * x + y * y)
borde = suave(0.7, 0.92, r) * (1.0 - suave(0.92, 0.98, r))
interior = (1.0 - suave(0.9, 0.95, r)) * 0.12
brillo = np.exp(-(((x + 0.35) / 0.18) ** 2 + ((y - 0.38) / 0.12) ** 2)) * 0.95
hechas.append(guardar("Burbuja", np.ones_like(r), np.clip(borde * 0.85 + interior + brillo, 0, 1)))

# --- Gota: lágrima (punta arriba) con brillo ---
x, y = coords(64)
ancho = 0.55 * np.sqrt(np.clip((0.95 - y) / 1.7, 0, 1)) * np.clip((y + 0.95) * 1.4, 0, 1)
gota = suave(0.0, 0.08, ancho - np.abs(x))
brillo = np.exp(-(((x + 0.15) / 0.1) ** 2 + ((y + 0.25) / 0.16) ** 2))
luz = 0.75 + 0.25 * brillo
hechas.append(guardar("Gota", luz, np.clip(gota * 0.9 + brillo * 0.3, 0, 1)))

# --- Polen: bolita peluda que brilla ---
x, y = coords(64)
r = np.sqrt(x * x + y * y)
ang = np.arctan2(y, x)
pelos = 0.62 + 0.12 * np.array([[noise.noise(Vector((math.cos(a) * 3, math.sin(a) * 3, 1.5))) for a in fila] for fila in ang])
bola = suave(0.0, 0.08, pelos - r)
halo = np.exp(-(r / 0.75) ** 2) * 0.5
hechas.append(guardar("Polen", 0.85 + 0.15 * suave(0.5, 0.0, r), np.maximum(bola, halo)))

# --- Cristal: grano de sal (rombo con caras de distinto brillo) ---
x, y = coords(64)
dentro = (np.abs(x) + np.abs(y)) < 0.8
cara = np.where(x > 0, np.where(y > 0, 1.0, 0.75), np.where(y > 0, 0.9, 0.65))
hechas.append(guardar("Cristal", cara, dentro * 1.0))

result = {"texturas": hechas, "carpeta": CARPETA}
