using UnityEditor;
using UnityEngine;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Telas con dibujo para "Costura a la fuga": cuadros vichy, lunares, rayas, la cuadrícula
    /// fina de la tabla de corte, vetas de madera y papel pintado. Las texturas se generan con
    /// código (en gris claro y blanco) y el material les da el color, así que una misma textura
    /// sirve para varios colores. Las losas redondeadas tienen las UV en metros: "metros" es lo
    /// que mide una repetición del dibujo.
    /// </summary>
    static class TelasCostura
    {
        const string Carpeta = E.Raiz + "/Texturas";
        const int Tamano = 256;

        public static Texture2D Vichy => Textura("TelaVichy", (u, v) =>
        {
            float a = Banda(u, 0.5f), b = Banda(v, 0.5f);
            return Mathf.Lerp(1f, 0.78f, Mathf.Max(a, b) * 0.6f + a * b * 0.4f) * Trama(u, v);
        });

        public static Texture2D Lunares => Textura("TelaLunares", (u, v) =>
        {
            float d = Mathf.Min(Distancia(u, v, 0.25f, 0.25f), Distancia(u, v, 0.75f, 0.75f));
            float lunar = 1f - Mathf.SmoothStep(0.15f, 0.17f, d);
            return Mathf.Lerp(0.84f, 1f, lunar) * Trama(u, v);
        });

        public static Texture2D Rayas => Textura("TelaRayas", (u, v) =>
        {
            float r = Mathf.Repeat(u + v, 0.5f) < 0.25f ? 1f : 0.82f;
            return r * Trama(u, v);
        });

        public static Texture2D TablaCorte => Textura("TablaCorte", (u, v) =>
        {
            // Rayas finas cada cuarto y una más marcada en el borde de cada repetición.
            float fina = Mathf.Max(Linea(u * 4f, 0.03f), Linea(v * 4f, 0.03f));
            float gruesa = Mathf.Max(Linea(u, 0.012f), Linea(v, 0.012f));
            float diagonal = Linea((u - v) * 2f, 0.02f) * 0.5f;
            return 1f - 0.12f * fina - 0.18f * gruesa - 0.08f * diagonal;
        });

        public static Texture2D Madera => Textura("MaderaVetas", (u, v) =>
        {
            const float doblePi = Mathf.PI * 2f; // todo periódico: que no se note la junta al repetir
            float veta = Mathf.Sin(u * doblePi * 6f + Mathf.Sin(v * doblePi) * 2.5f + Mathf.Sin(v * doblePi * 2f + u * doblePi) * 1.2f);
            float nudo = 1f - Mathf.SmoothStep(0.02f, 0.05f, Distancia(u, v, 0.7f, 0.3f));
            float tabla = Linea(u * 2f, 0.01f);
            return 0.9f + 0.07f * veta - 0.12f * nudo - 0.18f * tabla;
        });

        public static Texture2D PapelPintado => Textura("PapelPintado", (u, v) =>
        {
            float raya = Mathf.Repeat(u, 0.5f) < 0.06f ? 0.9f : 1f;
            float flor = 1f - Mathf.SmoothStep(0.05f, 0.07f, Distancia(u, v, 0.25f, 0.5f));
            return raya * Mathf.Lerp(1f, 0.86f, flor);
        });

        /// <summary>Material de tela: color × dibujo, con una repetición cada "metros".</summary>
        public static Material Tela(string nombre, Color color, Texture2D textura, float metros, float brillo = 0.2f)
        {
            var mat = E.CrearMaterial(nombre, color, brillo);
            var escala = Vector2.one / Mathf.Max(0.01f, metros);
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", textura);
                mat.SetTextureScale("_BaseMap", escala);
            }
            mat.mainTexture = textura;
            mat.mainTextureScale = escala;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------

        static Texture2D Textura(string nombre, System.Func<float, float, float> brillo)
        {
            E.CrearCarpeta(E.Raiz, "Texturas");
            string ruta = $"{Carpeta}/{nombre}.asset";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            bool nueva = tex == null;
            if (nueva) tex = new Texture2D(Tamano, Tamano, TextureFormat.RGBA32, true) { name = nombre };
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 4;
            var pixeles = new Color[Tamano * Tamano];
            for (int y = 0; y < Tamano; y++)
                for (int x = 0; x < Tamano; x++)
                {
                    float b = Mathf.Clamp01(brillo((x + 0.5f) / Tamano, (y + 0.5f) / Tamano));
                    pixeles[y * Tamano + x] = new Color(b, b, b, 1f);
                }
            tex.SetPixels(pixeles);
            tex.Apply(true);
            if (nueva) AssetDatabase.CreateAsset(tex, ruta);
            else EditorUtility.SetDirty(tex);
            return tex;
        }

        /// <summary>1 dentro de la mitad "ancho" de cada repetición (bordes suaves).</summary>
        static float Banda(float t, float ancho)
        {
            float f = Mathf.Repeat(t, 1f);
            return Mathf.SmoothStep(0f, 0.02f, f) * (1f - Mathf.SmoothStep(ancho - 0.02f, ancho, f));
        }

        /// <summary>1 sobre una línea en cada entero de t (grosor en fracción de repetición).</summary>
        static float Linea(float t, float grosor)
        {
            float f = Mathf.Repeat(t + 0.5f, 1f) - 0.5f;
            return 1f - Mathf.SmoothStep(grosor * 0.6f, grosor, Mathf.Abs(f));
        }

        /// <summary>Distancia al punto (cx, cy) dentro de una repetición (que se repite).</summary>
        static float Distancia(float u, float v, float cx, float cy)
        {
            float dx = Mathf.Repeat(u - cx + 0.5f, 1f) - 0.5f, dy = Mathf.Repeat(v - cy + 0.5f, 1f) - 0.5f;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>El tejido: una trama muy suave para que no parezca plástico.</summary>
        static float Trama(float u, float v) =>
            0.97f + 0.03f * Mathf.Sin(u * Mathf.PI * 2f * 48f) * Mathf.Sin(v * Mathf.PI * 2f * 48f);
    }
}
