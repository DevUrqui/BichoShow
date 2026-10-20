using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Aspecto de los menús y carteles del show (todo con OnGUI):
    ///  - Texto "de concurso": fuente redonda y gorda, contorno oscuro, sombra y brillo de dos tonos.
    ///  - Paneles redondeados con sombra, brillo y borde; botones tipo píldora; glifos de mando.
    ///  - Estrellas, títulos con letras que rebotan y rayos de fondo de plató.
    /// Fuentes (SIL Open Font License, en Resources/Fuentes): Lilita One para títulos y
    /// Fredoka para el texto. Si faltan, usa la de Unity.
    /// Los tamaños se dan "a 1080p" y se escalan con la pantalla (EstiloShow.Px).
    /// </summary>
    public static class EstiloShow
    {
        public enum Fuente { Titulo, Texto }

        // Paleta del plató
        public static readonly Color Morado = new Color(0.33f, 0.16f, 0.55f);
        public static readonly Color MoradoOscuro = new Color(0.12f, 0.05f, 0.22f);
        public static readonly Color Oro = new Color(1f, 0.8f, 0.22f);
        public static readonly Color Rosa = new Color(1f, 0.42f, 0.66f);
        public static readonly Color Cian = new Color(0.36f, 0.86f, 0.96f);
        public static readonly Color Lila = new Color(0.82f, 0.6f, 1f);
        public static readonly Color Verde = new Color(0.45f, 0.9f, 0.45f);

        static Font fuenteTitulo, fuenteTexto;
        static bool fuentesCargadas;
        static GUIStyle estilo;
        static readonly GUIContent contenido = new GUIContent();
        static Texture2D texEstrella, texRayos, texDegradado;

        /// <summary>Convierte un tamaño pensado para 1080p al de la pantalla actual.</summary>
        public static float Px(float tamano1080) => tamano1080 * Screen.height / 1080f;

        static bool Pintando => Event.current != null && Event.current.type == EventType.Repaint;

        static void Preparar()
        {
            if (!fuentesCargadas)
            {
                fuentesCargadas = true;
                fuenteTitulo = Resources.Load<Font>("Fuentes/LilitaOne");
                fuenteTexto = Resources.Load<Font>("Fuentes/Fredoka");
            }
            if (estilo == null)
                // Ojo: con clipping Overflow Unity ignora la alineación (todo salía pegado a la izquierda).
                estilo = new GUIStyle(GUI.skin.label) { clipping = TextClipping.Clip, richText = false, padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0) };
        }

        static void Configurar(int tamano, TextAnchor alineacion, Fuente fuente, bool ajustar)
        {
            Preparar();
            estilo.font = fuente == Fuente.Titulo ? fuenteTitulo : fuenteTexto;
            estilo.fontStyle = fuente == Fuente.Texto && fuenteTexto != null ? FontStyle.Bold : (estilo.font == null ? FontStyle.Bold : FontStyle.Normal);
            estilo.fontSize = Mathf.Max(8, tamano);
            estilo.alignment = alineacion;
            estilo.wordWrap = ajustar;
        }

        // ------------------------------------------------------------------
        // Texto
        // ------------------------------------------------------------------

        /// <summary>
        /// Texto con contorno, sombra y brillo (mitad de arriba más clara). "tamano" en píxeles
        /// de pantalla (usa Px para escalarlo).
        /// </summary>
        public static void Texto(Rect r, string texto, int tamano, Color color, TextAnchor alineacion = TextAnchor.MiddleCenter,
                                 Fuente fuente = Fuente.Titulo, bool ajustar = false, float grosor = 1f, Color? contorno = null)
        {
            if (!Pintando || string.IsNullOrEmpty(texto) || color.a <= 0.005f) return;
            Configurar(tamano, alineacion, fuente, ajustar);
            contenido.text = texto;
            r = Agrandar(r, alineacion, ajustar);

            float a = color.a;
            Color colorContorno = contorno ?? MoradoOscuro;
            colorContorno.a = a;
            float g = Mathf.Max(2f, tamano * 0.085f) * grosor;

            // Sombra
            Dibujar(new Rect(r.x + g * 0.5f, r.y + g * 1.7f, r.width, r.height), new Color(0f, 0f, 0f, 0.45f * a));
            // Contorno
            int pasos = tamano >= 28 ? 12 : 8;
            for (int i = 0; i < pasos; i++)
            {
                float ang = i * Mathf.PI * 2f / pasos;
                Dibujar(new Rect(r.x + Mathf.Cos(ang) * g, r.y + Mathf.Sin(ang) * g, r.width, r.height), colorContorno);
            }
            // Relleno: abajo un poco más oscuro, arriba más claro (brillo de "plástico")
            Dibujar(r, Color.Lerp(color, Color.black, 0.12f).WithAlpha(a));
            float alto = estilo.CalcHeight(contenido, r.width);
            float centro = alineacion == TextAnchor.UpperLeft || alineacion == TextAnchor.UpperCenter || alineacion == TextAnchor.UpperRight
                ? r.y + alto * 0.5f
                : alineacion == TextAnchor.LowerLeft || alineacion == TextAnchor.LowerCenter || alineacion == TextAnchor.LowerRight
                    ? r.yMax - alto * 0.5f
                    : r.center.y;
            const float margen = 200f;
            GUI.BeginGroup(new Rect(r.x - margen, r.y - margen, r.width + margen * 2f, centro - (r.y - margen)));
            Dibujar(new Rect(margen, margen, r.width, r.height), Color.Lerp(color, Color.white, 0.32f).WithAlpha(a));
            GUI.EndGroup();
        }

        /// <summary>
        /// Si el texto no cabe en su caja, la agranda (sin mover el texto según su alineación),
        /// para que el recorte no le corte las letras.
        /// </summary>
        static Rect Agrandar(Rect r, TextAnchor alineacion, bool ajustar)
        {
            Vector2 tamano = ajustar ? new Vector2(r.width, estilo.CalcHeight(contenido, r.width)) : estilo.CalcSize(contenido);
            tamano += Vector2.one * estilo.fontSize * 0.3f; // holgura para el contorno
            int columna = (int)alineacion % 3; // 0 izquierda, 1 centro, 2 derecha
            int fila = (int)alineacion / 3;    // 0 arriba, 1 medio, 2 abajo
            if (!ajustar && tamano.x > r.width)
            {
                float extra = tamano.x - r.width;
                r.x -= columna == 0 ? 0f : columna == 1 ? extra * 0.5f : extra;
                r.width = tamano.x;
            }
            if (tamano.y > r.height)
            {
                float extra = tamano.y - r.height;
                r.y -= fila == 0 ? 0f : fila == 1 ? extra * 0.5f : extra;
                r.height = tamano.y;
            }
            return r;
        }

        /// <summary>
        /// Texto sobre una pastilla oscura del tamaño justo (mensajes sueltos encima del juego).
        /// La pastilla se centra en "centro".
        /// </summary>
        public static void TextoEnPastilla(Vector2 centro, string texto, int tamano, Color color, Color? fondo = null,
                                           Fuente fuente = Fuente.Texto)
        {
            if (!Pintando || string.IsNullOrEmpty(texto) || color.a <= 0.005f) return;
            float ancho = Ancho(texto, tamano, fuente) + tamano * 1.6f;
            float alto = tamano * 1.75f;
            var r = new Rect(centro.x - ancho * 0.5f, centro.y - alto * 0.5f, ancho, alto);
            Panel(r, (fondo ?? MoradoOscuro.WithAlpha(0.8f)).WithAlpha((fondo?.a ?? 0.8f) * color.a), alto * 0.5f, Px(2.5f), false);
            Texto(r, texto, tamano, color, TextAnchor.MiddleCenter, fuente);
        }

        static void Dibujar(Rect r, Color c)
        {
            estilo.normal.textColor = c;
            GUI.Label(r, contenido, estilo);
        }

        /// <summary>Ancho que ocupa un texto con ese tamaño y fuente.</summary>
        public static float Ancho(string texto, int tamano, Fuente fuente = Fuente.Titulo)
        {
            Configurar(tamano, TextAnchor.MiddleLeft, fuente, false);
            contenido.text = texto;
            return estilo.CalcSize(contenido).x;
        }

        /// <summary>Título con las letras rebotando y cambiando de color, cada una un poco torcida.</summary>
        public static void TituloRebote(Rect r, string texto, int tamano, Color[] colores, float amplitud = 1f)
        {
            if (!Pintando) return;
            float t = Time.unscaledTime;
            float espacio = Ancho(" ", tamano) * 0.6f;
            var anchos = new float[texto.Length];
            float total = 0f;
            for (int i = 0; i < texto.Length; i++)
            {
                anchos[i] = texto[i] == ' ' ? espacio : Ancho(texto[i].ToString(), tamano) * 0.96f;
                total += anchos[i];
            }

            float x = r.center.x - total * 0.5f;
            int colorIndice = 0;
            for (int i = 0; i < texto.Length; i++)
            {
                if (texto[i] == ' ') { x += anchos[i]; continue; }
                float bote = Mathf.Sin(t * 3.2f - i * 0.55f) * tamano * 0.07f * amplitud;
                float giro = Mathf.Sin(t * 2.1f + i * 1.3f) * 5f * amplitud;
                var letra = new Rect(x - tamano * 0.3f, r.y + bote, anchos[i] + tamano * 0.6f, r.height);
                var matriz = GUI.matrix;
                GUIUtility.RotateAroundPivot(giro, letra.center);
                Texto(letra, texto[i].ToString(), tamano, colores[colorIndice++ % colores.Length], TextAnchor.MiddleCenter, Fuente.Titulo, false, 1.3f);
                GUI.matrix = matriz;
                x += anchos[i];
            }
        }

        // ------------------------------------------------------------------
        // Paneles, botones y adornos
        // ------------------------------------------------------------------

        /// <summary>Panel redondeado con sombra, brillo arriba y borde claro.</summary>
        public static void Panel(Rect r, Color color, float radio = -1f, float borde = -1f, bool sombra = true)
        {
            if (!Pintando) return;
            if (radio < 0f) radio = Mathf.Min(r.height, r.width) * 0.22f;
            radio = Mathf.Min(radio, Mathf.Min(r.width, r.height) * 0.5f);
            if (borde < 0f) borde = Mathf.Max(2f, Px(3.5f));
            var blanco = Texture2D.whiteTexture;

            if (sombra)
                GUI.DrawTexture(new Rect(r.x, r.y + Px(7f), r.width, r.height), blanco, ScaleMode.StretchToFill, true, 0f, new Color(0f, 0f, 0f, 0.35f * color.a), 0f, radio);
            GUI.DrawTexture(r, blanco, ScaleMode.StretchToFill, true, 0f, color, 0f, radio);
            float m = borde + Px(3f);
            GUI.DrawTexture(new Rect(r.x + m, r.y + m, r.width - m * 2f, (r.height - m * 2f) * 0.45f), blanco, ScaleMode.StretchToFill, true, 0f,
                            new Color(1f, 1f, 1f, 0.13f * color.a), 0f, Mathf.Max(0f, radio - m));
            if (borde > 0f)
                GUI.DrawTexture(r, blanco, ScaleMode.StretchToFill, true, 0f, Color.Lerp(color, Color.white, 0.4f).WithAlpha(color.a), borde, radio);
        }

        /// <summary>Botón tipo píldora. El elegido es dorado, más grande y late.</summary>
        public static void Boton(Rect r, string texto, bool elegido, int tamano)
        {
            if (elegido)
            {
                float latido = 1.05f + Mathf.Sin(Time.unscaledTime * 6f) * 0.015f;
                r = Escalar(r, latido);
                Panel(r, Oro, r.height * 0.5f);
                Texto(r, texto, Mathf.RoundToInt(tamano * 1.06f), Color.white, TextAnchor.MiddleCenter, Fuente.Titulo, false, 1.1f);
            }
            else
            {
                Panel(r, Morado.WithAlpha(0.88f), r.height * 0.5f);
                Texto(r, texto, tamano, new Color(1f, 1f, 1f, 0.92f));
            }
        }

        /// <summary>Botón de mando redondo con su letra (A verde, X azul…).</summary>
        public static void Glifo(Rect r, string letra, Color color)
        {
            if (!Pintando) return;
            Panel(r, color, r.height * 0.5f, Px(2.5f), false);
            Texto(r, letra, Mathf.RoundToInt(r.height * 0.58f), Color.white, TextAnchor.MiddleCenter, Fuente.Titulo, false, 0.8f);
        }

        public static readonly Color GlifoA = new Color(0.3f, 0.78f, 0.35f);
        public static readonly Color GlifoX = new Color(0.25f, 0.55f, 0.95f);
        public static readonly Color GlifoTecla = new Color(0.45f, 0.42f, 0.55f);

        /// <summary>
        /// Línea de ayuda con glifos: pasos = pares (glifo, texto), p. ej. ("A", "aceptar").
        /// Se centra en r.
        /// </summary>
        public static void Ayuda(Rect r, params (string glifo, Color color, string texto)[] pasos)
        {
            if (!Pintando) return;
            int tamano = Mathf.RoundToInt(r.height * 0.5f);
            float glifo = r.height * 0.82f, hueco = r.height * 0.25f, separacion = r.height * 0.9f;
            float total = 0f;
            foreach (var p in pasos)
                total += Mathf.Max(glifo, Ancho(p.glifo, Mathf.RoundToInt(glifo * 0.58f)) + glifo * 0.5f) + hueco + Ancho(p.texto, tamano, Fuente.Texto) + separacion;
            total -= separacion;

            float x = r.center.x - total * 0.5f;
            foreach (var p in pasos)
            {
                float anchoGlifo = Mathf.Max(glifo, Ancho(p.glifo, Mathf.RoundToInt(glifo * 0.58f)) + glifo * 0.5f);
                Glifo(new Rect(x, r.center.y - glifo * 0.5f, anchoGlifo, glifo), p.glifo, p.color);
                x += anchoGlifo + hueco;
                float anchoTexto = Ancho(p.texto, tamano, Fuente.Texto);
                Texto(new Rect(x, r.y, anchoTexto + 4f, r.height), p.texto, tamano, Color.white, TextAnchor.MiddleLeft, Fuente.Texto, false, 0.8f);
                x += anchoTexto + separacion;
            }
        }

        /// <summary>Barra redondeada (progreso, volumen, integridad…).</summary>
        public static void Barra(Rect r, float valor, Color color)
        {
            if (!Pintando) return;
            float radio = r.height * 0.5f;
            var blanco = Texture2D.whiteTexture;
            GUI.DrawTexture(r, blanco, ScaleMode.StretchToFill, true, 0f, MoradoOscuro.WithAlpha(0.75f), 0f, radio);
            float m = Mathf.Max(2f, r.height * 0.14f);
            var dentro = new Rect(r.x + m, r.y + m, (r.width - m * 2f) * Mathf.Clamp01(valor), r.height - m * 2f);
            if (dentro.width >= dentro.height * 0.6f)
            {
                GUI.DrawTexture(dentro, blanco, ScaleMode.StretchToFill, true, 0f, color, 0f, dentro.height * 0.5f);
                GUI.DrawTexture(new Rect(dentro.x + m, dentro.y + m * 0.5f, dentro.width - m * 2f, dentro.height * 0.4f), blanco,
                                ScaleMode.StretchToFill, true, 0f, new Color(1f, 1f, 1f, 0.3f), 0f, dentro.height * 0.2f);
            }
            GUI.DrawTexture(r, blanco, ScaleMode.StretchToFill, true, 0f, new Color(1f, 1f, 1f, 0.35f), Mathf.Max(1.5f, Px(2f)), radio);
        }

        /// <summary>Estrella (la misma textura que las partículas), con contorno oscuro.</summary>
        public static void Estrella(Rect r, Color color, float giro = 0f)
        {
            if (!Pintando) return;
            if (texEstrella == null)
                texEstrella = Resources.Load<Texture2D>(Efectos.RecursoEstrella + "Textura") ?? Efectos.CrearTexturaEstrella();
            var matriz = GUI.matrix;
            if (giro != 0f) GUIUtility.RotateAroundPivot(giro, r.center);
            GUI.DrawTexture(Escalar(r, 1.3f), texEstrella, ScaleMode.ScaleToFit, true, 0f, MoradoOscuro.WithAlpha(color.a), 0f, 0f);
            GUI.DrawTexture(r, texEstrella, ScaleMode.ScaleToFit, true, 0f, color, 0f, 0f);
            GUI.matrix = matriz;
        }

        /// <summary>Fondo de plató: degradado morado y rayos de luz girando despacio detrás.</summary>
        public static void FondoPlato(float opacidad = 0.92f, Vector2? centroRayos = null, Color? colorRayos = null)
        {
            if (!Pintando) return;
            if (texDegradado == null) texDegradado = CrearDegradado();
            if (texRayos == null) texRayos = CrearRayos();

            var pantalla = new Rect(0, 0, Screen.width, Screen.height);
            GUI.DrawTexture(pantalla, texDegradado, ScaleMode.StretchToFill, true, 0f, new Color(1f, 1f, 1f, opacidad), 0f, 0f);

            Vector2 c = centroRayos ?? new Vector2(Screen.width * 0.5f, Screen.height * 0.3f);
            float lado = Mathf.Max(Screen.width, Screen.height) * 1.9f;
            var matriz = GUI.matrix;
            GUIUtility.RotateAroundPivot(Time.unscaledTime * 6f, c);
            GUI.DrawTexture(new Rect(c.x - lado * 0.5f, c.y - lado * 0.5f, lado, lado), texRayos, ScaleMode.StretchToFill, true, 0f,
                            (colorRayos ?? Oro).WithAlpha(0.13f * opacidad), 0f, 0f);
            GUI.matrix = matriz;
        }

        /// <summary>Una banda horizontal semitransparente (para carteles sobre el juego).</summary>
        public static void Banda(Rect r, Color color)
        {
            if (!Pintando) return;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
            float b = Mathf.Max(2f, Px(4f));
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, b), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, Oro.WithAlpha(color.a), 0f, 0f);
            GUI.DrawTexture(new Rect(r.x, r.yMax - b, r.width, b), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, Oro.WithAlpha(color.a), 0f, 0f);
        }

        public static Rect Escalar(Rect r, float k) =>
            new Rect(r.center.x - r.width * k * 0.5f, r.center.y - r.height * k * 0.5f, r.width * k, r.height * k);

        public static Color WithAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);

        // ------------------------------------------------------------------
        // Texturas generadas
        // ------------------------------------------------------------------

        static Texture2D CrearDegradado()
        {
            var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "DegradadoPlato" };
            for (int y = 0; y < 64; y++)
            {
                float k = y / 63f; // 0 abajo, 1 arriba
                tex.SetPixel(0, y, Color.Lerp(new Color(0.2f, 0.08f, 0.36f, 0.97f), new Color(0.1f, 0.04f, 0.2f, 0.9f), k));
            }
            tex.Apply();
            return tex;
        }

        static Texture2D CrearRayos()
        {
            const int n = 256, rayos = 14;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "RayosPlato" };
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);
                float rayo = Mathf.Repeat(ang / (Mathf.PI * 2f) * rayos, 1f) < 0.5f ? 1f : 0f;
                float desvanecer = Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 6f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, rayo * desvanecer));
            }
            tex.Apply();
            return tex;
        }
    }
}
