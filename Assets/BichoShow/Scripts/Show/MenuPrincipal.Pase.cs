using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// El pase de temporada en el menú: la pantalla del pase (nivel, premios, retos del día y la
    /// cuenta), la insignia con el nivel en el botón y el resumen de lo ganado al volver de un show.
    /// </summary>
    public partial class MenuPrincipal
    {
        static readonly Color ColorPase = new Color(1f, 0.55f, 0.22f);

        int nivelVisto;               // el premio que se mira en la fila de niveles
        float finResumen = -1f;       // hasta cuándo se ve el resumen de lo ganado
        readonly List<ProgresoPase.Ganancia> resumen = new List<ProgresoPase.Ganancia>();
        readonly List<int> nivelesResumen = new List<int>();

        void EntrarPase()
        {
            pantalla = Pantalla.Pase;
            nivelVisto = Mathf.Clamp(ProgresoPase.Nivel + 1, 1, Temporada.Niveles);
        }

        void ActualizarPase()
        {
            bool teclaVolver = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            foreach (var entrada in PlayerInput.all)
            {
                if (entrada.actions == null) continue;
                var mover = entrada.actions.FindAction("Mover");
                Vector2 stick = mover != null ? mover.ReadValue<Vector2>() : Vector2.zero;
                stickAnterior.TryGetValue(entrada, out Vector2 antes);
                stickAnterior[entrada] = stick;
                int sentido = stick.x > 0.6f && antes.x <= 0.6f ? 1 : stick.x < -0.6f && antes.x >= -0.6f ? -1 : 0;
                if (sentido != 0)
                {
                    nivelVisto = Mathf.Clamp(nivelVisto + sentido, 1, Temporada.Niveles);
                    Sonidos.Reproducir(Sonido.MenuMover);
                }

                var saltar = entrada.actions.FindAction("Saltar");
                var lanzarse = entrada.actions.FindAction("Lanzarse");
                var empezar = entrada.actions.FindAction("Empezar");
                if (saltar != null && saltar.WasPressedThisFrame())
                {
                    // A: lo que toque con la cuenta (entrar, cancelar la espera, reintentar).
                    Sonidos.Reproducir(Sonido.MenuAceptar);
                    if (Cuenta.EsperandoNavegador) Cuenta.CancelarInicio();
                    else if (Cuenta.EstadoActual == Cuenta.Estado.Invitado) Cuenta.IniciarSesion();
                    else if (Cuenta.EstadoActual == Cuenta.Estado.SinConexion) _ = Cuenta.Iniciar();
                }
                if (lanzarse != null && lanzarse.WasPressedThisFrame() && Cuenta.EstadoActual == Cuenta.Estado.ConCuenta)
                {
                    Sonidos.Reproducir(Sonido.MenuMover);
                    Cuenta.CerrarSesion();
                }
                if (empezar != null && empezar.WasPressedThisFrame()) teclaVolver = true;
            }
            if (teclaVolver)
            {
                pantalla = Pantalla.Principal;
                seleccion = 3;
                Sonidos.Reproducir(Sonido.MenuAceptar);
            }
        }

        // ---------------------------------------------------------------------
        // Pantalla del pase
        // ---------------------------------------------------------------------

        static readonly Color CartaPrenda = new Color(0.62f, 0.36f, 0.22f);
        static readonly Color CartaTitulo = new Color(0.42f, 0.3f, 0.72f);
        static readonly Dictionary<string, Texture2D> iconosPase = new Dictionary<string, Texture2D>();

        /// <summary>Los dibujos del pase (Arte/Blender/crear_iconos_pase.py), por nombre.</summary>
        static void IconoPase(Rect r, string nombre, float alfa = 1f)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!iconosPase.TryGetValue(nombre, out var textura))
            {
                textura = Resources.Load<Texture2D>("Pase/Iconos/" + nombre);
                iconosPase[nombre] = textura;
            }
            if (textura != null) GUI.DrawTexture(r, textura, ScaleMode.ScaleToFit, true, 0f, new Color(1f, 1f, 1f, alfa), 0f, 0f);
        }

        void DibujarPase(float w, float h)
        {
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));
            EstiloShow.FondoPlato(0.9f, new Vector2(w * 0.5f, h * 0.1f), ColorPase);

            // --- Logo del show, arriba a la izquierda (si cabe) ---
            float anchoCinta = Mathf.Min(px(860f), w - px(620f));
            if (anchoCinta < px(560f)) anchoCinta = Mathf.Min(px(860f), w - px(60f)); // estrecho: sin logo
            else DibujarLogo(new Rect(px(26f), px(14f), px(250f), px(190f)));

            // --- Cabecera: la cinta grande, la temporada y lo que queda ---
            var cinta = new Rect(w * 0.5f - anchoCinta * 0.5f, px(22f), anchoCinta, px(116f));
            EstiloShow.Panel(EstiloShow.Escalar(cinta, 1.02f), EstiloShow.Oro, cinta.height * 0.5f);
            EstiloShow.Panel(cinta, ColorPase, cinta.height * 0.5f);
            EstiloShow.Texto(cinta, "PASE DE TEMPORADA", tam(anchoCinta < px(700f) ? 60f : 74f), Color.white, TextAnchor.MiddleCenter,
                             EstiloShow.Fuente.Titulo, false, 1.3f);
            for (int s = -1; s <= 1; s += 2)
            {
                float lado = px(78f);
                EstiloShow.Estrella(new Rect(cinta.center.x + s * (cinta.width * 0.5f) - lado * 0.5f, cinta.center.y - lado * 0.5f, lado, lado),
                                    EstiloShow.Oro, Mathf.Sin(Time.unscaledTime * 2f) * 12f * s);
            }
            var subtitulo = new Rect(w * 0.5f - px(330f), cinta.yMax - px(18f), px(660f), px(54f));
            EstiloShow.Panel(subtitulo, EstiloShow.MoradoOscuro.WithAlpha(0.95f), subtitulo.height * 0.5f);
            EstiloShow.Texto(subtitulo, Temporada.Nombre, tam(30f), EstiloShow.Oro);
            var queda = new Rect(w * 0.5f - px(170f), subtitulo.yMax + px(8f), px(340f), px(46f));
            EstiloShow.Panel(queda, EstiloShow.Morado.WithAlpha(0.9f), queda.height * 0.5f, -1f, false);
            IconoPase(new Rect(queda.x + px(10f), queda.y + px(3f), px(40f), px(40f)), "Reloj");
            EstiloShow.Texto(new Rect(queda.x + px(50f), queda.y, queda.width - px(60f), queda.height), TextoQueda(), tam(22f), Color.white,
                             TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto);

            // --- Nivel y barra de experiencia ---
            float margen = px(22f);
            float y = px(238f);
            int nivel = ProgresoPase.Nivel;
            var insignia = new Rect(Mathf.Max(margen, w * 0.5f - px(560f)), y, px(176f), px(92f));
            EstiloShow.Panel(EstiloShow.Escalar(insignia, 1.03f), EstiloShow.Lila, px(24f));
            EstiloShow.Panel(insignia, EstiloShow.Morado, px(24f));
            EstiloShow.Texto(new Rect(insignia.x, insignia.y + px(4f), insignia.width, px(34f)), "NIVEL", tam(26f), EstiloShow.Oro);
            EstiloShow.Texto(new Rect(insignia.x, insignia.y + px(26f), insignia.width, px(64f)), nivel.ToString(), tam(58f), Color.white);
            var barra = new Rect(insignia.xMax + px(16f), y + px(21f), Mathf.Min(px(900f), w - margen - insignia.xMax - px(16f)), px(50f));
            EstiloShow.Panel(EstiloShow.Escalar(barra, 1.02f), EstiloShow.MoradoOscuro, barra.height * 0.5f, -1f, false);
            float lleno = ProgresoPase.Completo ? 1f : ProgresoPase.XpEnNivel / (float)Temporada.XpPorNivel;
            EstiloShow.BarraBrillo(barra, lleno, ColorPase);
            string textoBarra = ProgresoPase.Completo ? "¡PASE COMPLETO!" : $"{ProgresoPase.XpEnNivel} / {Temporada.XpPorNivel} XP";
            EstiloShow.Texto(barra, textoBarra, tam(28f), Color.white);

            // --- Los premios: cuatro cartas, la mirada primero y su descripción debajo ---
            y = px(350f);
            var zonaCartas = new Rect(margen, y, w - margen * 2f, px(352f));
            var cartaVista = DibujarPremios(zonaCartas);
            DibujarDescripcion(cartaVista, w);

            // --- Retos de hoy y la cuenta ---
            float yAbajo = px(820f), altoAbajo = px(186f);
            float anchoCuenta = Mathf.Min(px(410f), w * 0.3f);
            DibujarRetos(new Rect(margen, yAbajo, w - margen * 3f - anchoCuenta, altoAbajo));
            DibujarCuenta(new Rect(w - margen - anchoCuenta, yAbajo, anchoCuenta, altoAbajo));

            var ayuda = new List<(string, Color, string)> { ("Stick", EstiloShow.GlifoTecla, "ver premios") };
            if (Cuenta.EsperandoNavegador) ayuda.Add(("A", EstiloShow.GlifoA, "cancelar"));
            else if (Cuenta.EstadoActual == Cuenta.Estado.Invitado) ayuda.Add(("A", EstiloShow.GlifoA, "iniciar sesión"));
            else if (Cuenta.EstadoActual == Cuenta.Estado.SinConexion) ayuda.Add(("A", EstiloShow.GlifoA, "reintentar"));
            if (Cuenta.EstadoActual == Cuenta.Estado.ConCuenta) ayuda.Add(("X", EstiloShow.GlifoX, "cerrar sesión"));
            ayuda.Add(("START", EstiloShow.GlifoTecla, "volver"));
            EstiloShow.Ayuda(new Rect(0, h - px(60f), w, px(44f)), ayuda.ToArray());
        }

        /// <summary>El logo del show: una estrella dorada detrás y "BICHO SHOW" en dos líneas.</summary>
        static void DibujarLogo(Rect r)
        {
            float e = r.height * 1.05f;
            EstiloShow.Estrella(new Rect(r.center.x - e * 0.5f, r.center.y - e * 0.5f, e, e), EstiloShow.Oro, Mathf.Sin(Time.unscaledTime * 0.8f) * 6f);
            float e2 = r.height * 0.8f;
            EstiloShow.Estrella(new Rect(r.center.x - e2 * 0.5f, r.center.y - e2 * 0.5f, e2, e2), EstiloShow.Rosa, -Mathf.Sin(Time.unscaledTime * 0.8f) * 6f);
            int tamano = Mathf.RoundToInt(r.height * 0.36f);
            EstiloShow.Texto(new Rect(r.x, r.y + r.height * 0.12f, r.width, r.height * 0.4f), "BICHO", tamano, EstiloShow.Oro, TextAnchor.MiddleCenter,
                             EstiloShow.Fuente.Titulo, false, 1.3f);
            EstiloShow.Texto(new Rect(r.x, r.y + r.height * 0.48f, r.width, r.height * 0.4f), "SHOW", tamano, Color.white, TextAnchor.MiddleCenter,
                             EstiloShow.Fuente.Titulo, false, 1.3f);
        }

        static string TextoQueda()
        {
            if (Temporada.SinEmpezar) return $"Empieza en {Mathf.CeilToInt((float)Temporada.Queda.TotalDays)} días";
            if (!Temporada.EnCurso) return "Terminada: lo ganado es tuyo";
            var queda = Temporada.Queda;
            return queda.TotalDays >= 1 ? $"Quedan {Mathf.FloorToInt((float)queda.TotalDays)} días y {queda.Hours} h"
                                        : $"¡Últimas {Mathf.Max(1, Mathf.CeilToInt((float)queda.TotalHours))} horas!";
        }

        /// <summary>Cuatro cartas a partir de la mirada (con flechas entre ellas). Devuelve dónde quedó la mirada.</summary>
        Rect DibujarPremios(Rect zona)
        {
            System.Func<float, float> px = EstiloShow.Px;
            const int visibles = 4;
            float flecha = px(38f);
            float ancho = (zona.width - flecha * (visibles - 1)) / visibles;
            int primero = Mathf.Clamp(nivelVisto, 1, Temporada.Niveles - visibles + 1);
            Rect vista = default;
            for (int k = 0; k < visibles; k++)
            {
                int n = primero + k;
                var r = new Rect(zona.x + k * (ancho + flecha), zona.y, ancho, zona.height);
                if (n == nivelVisto) vista = r;
                var premio = Temporada.PremioDe(n);
                if (premio.HasValue) DibujarCarta(r, premio.Value, n == nivelVisto);
                if (k < visibles - 1)
                {
                    float g = px(34f);
                    EstiloShow.Glifo(new Rect(r.xMax + (flecha - g) * 0.5f, r.center.y - g * 0.5f, g, g), ">", EstiloShow.MoradoOscuro);
                }
            }
            // Si hay más a los lados, una flecha grande que lo avisa.
            float gl = px(46f);
            if (primero > 1) EstiloShow.Glifo(new Rect(zona.x - px(10f), zona.center.y - gl * 0.5f, gl, gl), "<", ColorPase);
            if (primero + visibles - 1 < Temporada.Niveles)
                EstiloShow.Glifo(new Rect(zona.xMax - gl + px(10f), zona.center.y - gl * 0.5f, gl, gl), ">", ColorPase);
            return vista;
        }

        static string NombreTipo(Temporada.Tipo tipo) => tipo switch
        {
            Temporada.Tipo.Gorro => "GORRO",
            Temporada.Tipo.Cara => "CARA",
            Temporada.Tipo.Cuerpo => "CUERPO",
            _ => "TÍTULO",
        };

        static void DibujarCarta(Rect r, Temporada.Premio p, bool vista)
        {
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));
            bool tuyo = ProgresoPase.Nivel >= p.nivel;
            Color fondo = p.EsPrenda ? CartaPrenda : CartaTitulo;
            if (vista)
            {
                // La mirada: borde dorado que brilla.
                float brillo = 0.75f + Mathf.Sin(Time.unscaledTime * 4f) * 0.25f;
                EstiloShow.Panel(EstiloShow.Escalar(r, 1.035f), EstiloShow.Oro.WithAlpha(brillo), px(30f));
                fondo = Color.Lerp(fondo, EstiloShow.Morado, 0.45f);
            }
            EstiloShow.Panel(r, fondo, px(26f), -1f, !vista);
            // Por dentro, un degradado más oscuro abajo (donde van el tipo y el nombre).
            EstiloShow.Panel(new Rect(r.x + px(8f), r.yMax - px(100f), r.width - px(16f), px(92f)), Color.black.WithAlpha(0.18f), px(20f), -1f, false);

            EstiloShow.TextoEnPastilla(new Vector2(r.center.x, r.y + px(30f)), $"NIVEL {p.nivel}", tam(26f), vista ? EstiloShow.Oro : Color.white,
                                       EstiloShow.MoradoOscuro.WithAlpha(0.95f), EstiloShow.Fuente.Titulo);
            float esquina = px(46f);
            if (!tuyo) IconoPase(new Rect(r.xMax - esquina - px(8f), r.y + px(8f), esquina, esquina), "Candado");
            else EstiloShow.TextoEnPastilla(new Vector2(r.xMax - px(46f), r.y + px(30f)), "¡TUYO!", tam(18f), Color.white, EstiloShow.Verde);

            // El dibujo: la prenda de Blender, o el título como un cartel.
            var arte = new Rect(r.x + px(10f), r.y + px(52f), r.width - px(20f), r.height - px(52f) - px(104f));
            if (vista) arte = EstiloShow.Escalar(arte, 1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.03f);
            if (p.EsPrenda) IconoPase(arte, p.id, tuyo || vista ? 1f : 0.85f);
            else CartelTitulo(arte, p.nombre);

            var tipo = new Vector2(r.center.x, r.yMax - px(74f));
            EstiloShow.TextoEnPastilla(tipo, NombreTipo(p.tipo), tam(24f), p.EsPrenda ? EstiloShow.Oro : EstiloShow.Lila,
                                       (p.EsPrenda ? new Color(0.4f, 0.2f, 0.12f) : new Color(0.25f, 0.15f, 0.45f)).WithAlpha(0.95f), EstiloShow.Fuente.Titulo);
            EstiloShow.Texto(new Rect(r.x + px(8f), r.yMax - px(54f), r.width - px(16f), px(46f)), p.EsPrenda ? p.nombre : $"«{p.nombre}»",
                             tam(p.nombre.Length > 18 ? 22f : 27f), Color.white, TextAnchor.MiddleCenter, EstiloShow.Fuente.Titulo, true);
        }

        /// <summary>Un título como cartel de la tele: estrella detrás y el texto en dos líneas.</summary>
        static void CartelTitulo(Rect r, string titulo)
        {
            float e = Mathf.Min(r.width, r.height) * 0.95f;
            EstiloShow.Estrella(new Rect(r.center.x - e * 0.5f, r.center.y - e * 0.5f, e, e), EstiloShow.Rosa.WithAlpha(0.85f), Mathf.Sin(Time.unscaledTime) * 4f);
            string uno = titulo.ToUpper(), dos = "";
            int corte = -1;
            for (int i = 0; i < uno.Length; i++)
                if (uno[i] == ' ' && (corte < 0 || Mathf.Abs(i - uno.Length / 2) < Mathf.Abs(corte - uno.Length / 2))) corte = i;
            if (corte > 0 && uno.Length > 9) { dos = uno.Substring(corte + 1); uno = uno.Substring(0, corte); }
            int largo = Mathf.Max(uno.Length, dos.Length);
            int tamano = Mathf.RoundToInt(Mathf.Min(r.height * 0.3f, r.width * 1.6f / Mathf.Max(5, largo)));
            if (dos.Length == 0)
            {
                EstiloShow.Texto(r, uno, tamano, EstiloShow.Oro, TextAnchor.MiddleCenter, EstiloShow.Fuente.Titulo, false, 1.4f);
                return;
            }
            EstiloShow.Texto(new Rect(r.x, r.center.y - tamano * 1.05f, r.width, tamano * 1.1f), uno, tamano, EstiloShow.Oro, TextAnchor.MiddleCenter,
                             EstiloShow.Fuente.Titulo, false, 1.4f);
            EstiloShow.Texto(new Rect(r.x, r.center.y, r.width, tamano * 1.1f), dos, Mathf.RoundToInt(tamano * 0.85f), Color.white, TextAnchor.MiddleCenter,
                             EstiloShow.Fuente.Titulo, false, 1.4f);
        }

        /// <summary>Debajo de la carta que se mira: qué es y su frase.</summary>
        void DibujarDescripcion(Rect carta, float w)
        {
            if (carta.width <= 0f) return;
            var premio = Temporada.PremioDe(nivelVisto);
            if (!premio.HasValue) return;
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));
            float ancho = Mathf.Max(carta.width, px(340f));
            var r = new Rect(Mathf.Clamp(carta.center.x - ancho * 0.5f, px(10f), w - ancho - px(10f)), carta.yMax + px(16f), ancho, px(84f));
            var matriz = GUI.matrix;
            float pico = px(26f);
            var rombo = new Rect(carta.center.x - pico * 0.5f, r.y - pico * 0.45f, pico, pico);
            GUIUtility.RotateAroundPivot(45f, rombo.center);
            EstiloShow.Rectangulo(rombo, EstiloShow.Oro);
            GUI.matrix = matriz;
            EstiloShow.Panel(EstiloShow.Escalar(r, 1.02f), EstiloShow.Oro, px(22f), -1f, false);
            EstiloShow.Panel(r, EstiloShow.MoradoOscuro, px(20f), -1f, false);
            string estado = ProgresoPase.Nivel >= premio.Value.nivel ? "¡YA ES TUYO!" : $"SE GANA EN EL NIVEL {premio.Value.nivel}";
            EstiloShow.Texto(new Rect(r.x, r.y + px(4f), r.width, px(34f)), $"{NombreTipo(premio.Value.tipo)} · {estado}", tam(22f), EstiloShow.Oro);
            EstiloShow.Texto(new Rect(r.x + px(14f), r.y + px(34f), r.width - px(28f), px(46f)), premio.Value.descripcion, tam(20f), Color.white,
                             TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto, true);
        }

        void DibujarRetos(Rect r)
        {
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));
            EstiloShow.Panel(EstiloShow.Escalar(r, 1.01f), EstiloShow.Morado, px(26f), -1f, false);
            EstiloShow.Panel(r, EstiloShow.MoradoOscuro.WithAlpha(0.95f), px(24f));
            IconoPase(new Rect(r.x + px(16f), r.y + px(6f), px(54f), px(54f)), "Reto_ronda");
            EstiloShow.Texto(new Rect(r.x + px(80f), r.y + px(8f), r.width, px(52f)), "RETOS DE HOY", tam(40f), EstiloShow.Oro, TextAnchor.MiddleLeft,
                             EstiloShow.Fuente.Titulo, false, 1.2f);
            EstiloShow.Texto(new Rect(r.x + px(380f), r.y + px(8f), r.width - px(400f), px(52f)), $"+{Temporada.XpReto} XP cada uno", tam(22f), Color.white,
                             TextAnchor.MiddleLeft, EstiloShow.Fuente.Texto);

            float hueco = px(12f);
            float ancho = (r.width - px(28f) - hueco * 2f) / 3f;
            int k = 0;
            foreach (var (reto, progreso, hecho) in ProgresoPase.RetosDeHoy())
            {
                var c = new Rect(r.x + px(14f) + k * (ancho + hueco), r.y + px(66f), ancho, r.height - px(80f));
                EstiloShow.Panel(c, (hecho ? Color.Lerp(EstiloShow.Verde, EstiloShow.MoradoOscuro, 0.35f) : EstiloShow.Morado).WithAlpha(0.95f), px(18f), -1f, false);
                float lado = Mathf.Min(c.height - px(16f), px(84f));
                var circulo = new Rect(c.x + px(10f), c.center.y - lado * 0.5f, lado, lado);
                EstiloShow.Panel(circulo, EstiloShow.MoradoOscuro.WithAlpha(0.7f), lado * 0.5f, -1f, false);
                IconoPase(EstiloShow.Escalar(circulo, 0.8f), "Reto_" + reto.evento);
                float x = circulo.xMax + px(10f);
                EstiloShow.Texto(new Rect(x, c.y + px(6f), c.xMax - x - px(8f), c.height * 0.52f), reto.texto, tam(20f), Color.white,
                                 TextAnchor.MiddleLeft, EstiloShow.Fuente.Texto, true);
                var b = new Rect(x, c.yMax - px(36f), c.xMax - x - px(12f), px(26f));
                EstiloShow.Barra(b, reto.objetivo > 0 ? progreso / (float)reto.objetivo : 1f, hecho ? EstiloShow.Oro : ColorPase);
                EstiloShow.Texto(b, hecho ? "¡HECHO!" : $"{progreso} / {reto.objetivo}", tam(18f), Color.white);
                k++;
            }
        }

        void DibujarCuenta(Rect r)
        {
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));
            EstiloShow.Panel(EstiloShow.Escalar(r, 1.02f), EstiloShow.Morado, px(26f), -1f, false);
            EstiloShow.Panel(r, EstiloShow.MoradoOscuro.WithAlpha(0.95f), px(24f));

            string estado, detalle = Cuenta.Aviso, boton = null;
            switch (Cuenta.EstadoActual)
            {
                case Cuenta.Estado.ConCuenta:
                    estado = "Con cuenta";
                    if (string.IsNullOrEmpty(detalle))
                        detalle = Cuenta.Correo.Length > 0 ? Cuenta.Correo : "Tu progreso se guarda en tu cuenta.";
                    boton = "Cerrar sesión";
                    break;
                case Cuenta.Estado.Invitado:
                    estado = "Invitado";
                    detalle = "Inicia sesión para no perder el progreso.";
                    boton = "Iniciar sesión";
                    break;
                case Cuenta.Estado.Conectando:
                    estado = "Conectando…";
                    if (Cuenta.EsperandoNavegador) boton = "Cancelar";
                    break;
                default:
                    estado = "Sin conexión";
                    boton = "Reintentar";
                    break;
            }

            float lado = px(76f);
            var avatar = new Rect(r.x + px(14f), r.y + px(14f), lado, lado);
            EstiloShow.Panel(avatar, EstiloShow.Lila.WithAlpha(0.9f), lado * 0.5f, -1f, false);
            IconoPase(EstiloShow.Escalar(avatar, 0.9f), "Invitado");
            float x = avatar.xMax + px(12f);
            EstiloShow.Texto(new Rect(x, r.y + px(10f), r.xMax - x - px(10f), px(42f)), estado, tam(32f), EstiloShow.Oro, TextAnchor.MiddleLeft,
                             EstiloShow.Fuente.Titulo, false, 1.2f);
            EstiloShow.Texto(new Rect(x, r.y + px(50f), r.xMax - x - px(10f), px(56f)), detalle, tam(17f), Color.white, TextAnchor.UpperLeft,
                             EstiloShow.Fuente.Texto, true);

            if (boton == null) return;
            var b = new Rect(r.x + px(18f), r.yMax - px(66f), r.width - px(36f), px(52f));
            bool cerrar = Cuenta.EstadoActual == Cuenta.Estado.ConCuenta;
            EstiloShow.Panel(b, cerrar ? EstiloShow.Morado : EstiloShow.Oro, b.height * 0.5f);
            float g = px(38f);
            EstiloShow.Glifo(new Rect(b.x + px(10f), b.center.y - g * 0.5f, g, g), cerrar ? "X" : "A", cerrar ? EstiloShow.GlifoX : EstiloShow.GlifoA);
            EstiloShow.Texto(new Rect(b.x + px(40f), b.y, b.width - px(40f), b.height), boton, tam(28f), cerrar ? Color.white : EstiloShow.MoradoOscuro,
                             TextAnchor.MiddleCenter, EstiloShow.Fuente.Titulo, false, cerrar ? 1f : 0f);
            if (!Cuenta.Principal && Cuenta.EstadoActual != Cuenta.Estado.Conectando)
                EstiloShow.Texto(new Rect(r.x, r.yMax + px(2f), r.width, px(24f)), "(otra copia abierta: esta no recuerda la sesión)", tam(14f),
                                 new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto);
        }

        // ---------------------------------------------------------------------
        // En la pantalla principal
        // ---------------------------------------------------------------------

        /// <summary>El nivel del pase en una pastilla, en la esquina de arriba a la derecha del botón.</summary>
        static void InsigniaPase(Rect boton)
        {
            int tamano = Mathf.RoundToInt(EstiloShow.Px(20f));
            EstiloShow.TextoEnPastilla(new Vector2(boton.xMax - EstiloShow.Px(30f), boton.y + EstiloShow.Px(2f)), $"Nv {ProgresoPase.Nivel}",
                                       tamano, Color.white, ColorPase);
        }

        /// <summary>Al llegar al menú: si se ha ganado experiencia, se enseña un rato (y se sube a la nube).</summary>
        void EmpezarResumenPase()
        {
            ProgresoPase.Subir();
            if (ProgresoPase.Recientes.Count == 0 && ProgresoPase.NivelesNuevos.Count == 0) return;
            resumen.Clear();
            resumen.AddRange(ProgresoPase.Recientes);
            nivelesResumen.Clear();
            nivelesResumen.AddRange(ProgresoPase.NivelesNuevos);
            ProgresoPase.Recientes.Clear();
            ProgresoPase.NivelesNuevos.Clear();
            finResumen = Time.unscaledTime + 9f;
            if (nivelesResumen.Count > 0) Sonidos.Reproducir(Sonido.Logro);
        }

        void DibujarResumenPase(float w, float h)
        {
            if (finResumen < 0f || Time.unscaledTime > finResumen) return;
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));
            int total = 0;
            foreach (var g in resumen) total += g.xp;
            int lineas = Mathf.Min(resumen.Count, 5) + Mathf.Min(nivelesResumen.Count, 3);
            float alto = px(110f) + lineas * px(38f);
            float entra = Mathf.Clamp01((Time.unscaledTime - (finResumen - 9f)) * 3f) * Mathf.Clamp01((finResumen - Time.unscaledTime) * 2f);
            var r = new Rect(w - px(560f) + (1f - entra) * px(600f), h * 0.03f + px(246f), px(520f), alto);
            EstiloShow.Panel(r, Color.Lerp(ColorPase, EstiloShow.MoradoOscuro, 0.35f).WithAlpha(0.96f), px(26f));
            EstiloShow.Texto(new Rect(r.x, r.y + px(10f), r.width, px(60f)), $"PASE  +{total} XP", tam(42f), EstiloShow.Oro);
            float y = r.y + px(72f);
            for (int k = 0; k < resumen.Count && k < 5; k++)
            {
                EstiloShow.Texto(new Rect(r.x + px(24f), y, r.width * 0.7f, px(36f)), resumen[k].motivo, tam(21f), Color.white, TextAnchor.MiddleLeft, EstiloShow.Fuente.Texto, true);
                EstiloShow.Texto(new Rect(r.x, y, r.width - px(24f), px(36f)), $"+{resumen[k].xp}", tam(22f), EstiloShow.Oro, TextAnchor.MiddleRight);
                y += px(38f);
            }
            for (int k = 0; k < nivelesResumen.Count && k < 3; k++)
            {
                var premio = Temporada.PremioDe(nivelesResumen[k]);
                string texto = $"¡NIVEL {nivelesResumen[k]}!" + (premio.HasValue ? $"  {premio.Value.nombre}" : "");
                EstiloShow.TextoEnPastilla(new Vector2(r.center.x, y + px(18f)), texto, tam(22f), Color.white, EstiloShow.Verde.WithAlpha(0.95f));
                y += px(38f);
            }
            if (nivelesResumen.Count > 0) EstiloShow.Confeti(40, entra);
        }
    }
}
