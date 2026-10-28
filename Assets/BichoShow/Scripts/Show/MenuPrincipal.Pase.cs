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

        void DibujarPase(float w, float h)
        {
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));
            EstiloShow.FondoPlato(0.9f, new Vector2(w * 0.5f, h * 0.1f), ColorPase);

            // --- Cabecera: nombre de la temporada y lo que queda ---
            var cinta = new Rect(w * 0.5f - px(470f), px(26f), px(940f), px(84f));
            EstiloShow.Panel(cinta, ColorPase.WithAlpha(0.97f), cinta.height * 0.5f);
            EstiloShow.Texto(cinta, "PASE DE TEMPORADA", tam(56f), Color.white, TextAnchor.MiddleCenter, EstiloShow.Fuente.Titulo, false, 1.2f);
            for (int s = -1; s <= 1; s += 2)
            {
                float lado = px(66f);
                EstiloShow.Estrella(new Rect(cinta.center.x + s * (cinta.width * 0.5f + lado * 0.15f) - lado * 0.5f, cinta.center.y - lado * 0.5f, lado, lado),
                                    EstiloShow.Oro, Time.unscaledTime * 50f * s);
            }
            EstiloShow.Texto(new Rect(0, cinta.yMax + px(6f), w, px(46f)), Temporada.Nombre, tam(32f), EstiloShow.Oro);
            EstiloShow.TextoEnPastilla(new Vector2(w * 0.5f, cinta.yMax + px(76f)), TextoQueda(), tam(24f), Color.white, EstiloShow.MoradoOscuro.WithAlpha(0.85f));

            // --- Nivel y barra de experiencia ---
            float y = px(236f);
            int nivel = ProgresoPase.Nivel;
            float anchoBarra = Mathf.Min(px(920f), w - px(260f));
            var insignia = new Rect(w * 0.5f - (px(200f) + anchoBarra) * 0.5f, y, px(170f), px(110f));
            EstiloShow.Panel(insignia, EstiloShow.Morado, px(30f));
            EstiloShow.Texto(new Rect(insignia.x, insignia.y + px(4f), insignia.width, px(36f)), "NIVEL", tam(26f), EstiloShow.Oro);
            EstiloShow.Texto(new Rect(insignia.x, insignia.y + px(30f), insignia.width, px(80f)), nivel.ToString(), tam(66f), Color.white);
            var barra = new Rect(insignia.xMax + px(30f), y + px(30f), anchoBarra, px(50f));
            float lleno = ProgresoPase.Completo ? 1f : ProgresoPase.XpEnNivel / (float)Temporada.XpPorNivel;
            EstiloShow.BarraBrillo(barra, lleno, ColorPase);
            string textoBarra = ProgresoPase.Completo ? "¡PASE COMPLETO!" : $"{ProgresoPase.XpEnNivel} / {Temporada.XpPorNivel} XP para el nivel {nivel + 1}";
            EstiloShow.Texto(barra, textoBarra, tam(26f), Color.white);

            // --- Los premios: una fila de tarjetas, la mirada en grande ---
            y += px(150f);
            DibujarPremios(new Rect(0, y, w, px(250f)));

            // --- Retos de hoy y cuenta, lado a lado ---
            y += px(280f);
            // (en pantallas estrechas, más estrechos: que quepan los dos)
            float total = Mathf.Min(w - px(40f), px(1740f)), hueco = px(30f);
            float x0 = w * 0.5f - total * 0.5f, anchoRetos = (total - hueco) * 0.57f;
            DibujarRetos(new Rect(x0, y, anchoRetos, px(300f)));
            DibujarCuenta(new Rect(x0 + anchoRetos + hueco, y, total - hueco - anchoRetos, px(300f)));

            var ayuda = new List<(string, Color, string)> { ("Stick", EstiloShow.GlifoTecla, "ver premios") };
            if (Cuenta.EsperandoNavegador) ayuda.Add(("A", EstiloShow.GlifoA, "cancelar"));
            else if (Cuenta.EstadoActual == Cuenta.Estado.Invitado) ayuda.Add(("A", EstiloShow.GlifoA, "iniciar sesión"));
            else if (Cuenta.EstadoActual == Cuenta.Estado.SinConexion) ayuda.Add(("A", EstiloShow.GlifoA, "reintentar"));
            if (Cuenta.EstadoActual == Cuenta.Estado.ConCuenta) ayuda.Add(("X", EstiloShow.GlifoX, "cerrar sesión"));
            ayuda.Add(("START", EstiloShow.GlifoTecla, "volver"));
            EstiloShow.Ayuda(new Rect(0, h - px(80f), w, px(48f)), ayuda.ToArray());
        }

        static string TextoQueda()
        {
            if (Temporada.SinEmpezar) return $"Empieza en {Mathf.CeilToInt((float)Temporada.Queda.TotalDays)} días";
            if (!Temporada.EnCurso) return "La temporada ha terminado: lo ganado es tuyo para siempre";
            var queda = Temporada.Queda;
            return queda.TotalDays >= 1 ? $"Quedan {Mathf.FloorToInt((float)queda.TotalDays)} días y {queda.Hours} h"
                                        : $"¡Últimas {Mathf.Max(1, Mathf.CeilToInt((float)queda.TotalHours))} horas!";
        }

        void DibujarPremios(Rect zona)
        {
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));
            float ancho = px(190f), hueco = px(18f);
            int nivel = ProgresoPase.Nivel;
            float centro = zona.center.x;
            for (int d = -4; d <= 4; d++)
            {
                int n = nivelVisto + d;
                if (n < 1 || n > Temporada.Niveles) continue;
                var premio = Temporada.PremioDe(n);
                if (!premio.HasValue) continue;
                var p = premio.Value;
                bool visto = d == 0, tuyo = nivel >= n;
                var r = new Rect(centro + d * (ancho + hueco) - ancho * 0.5f, zona.y + px(20f), ancho, zona.height - px(40f));
                if (visto) r = EstiloShow.Escalar(r, 1.14f + Mathf.Sin(Time.unscaledTime * 4f) * 0.01f);
                float alfa = Mathf.Abs(d) >= 4 ? 0.45f : 1f;
                Color fondo = tuyo ? Color.Lerp(EstiloShow.Verde, EstiloShow.MoradoOscuro, 0.35f)
                            : p.EsPrenda ? Color.Lerp(ColorPase, EstiloShow.MoradoOscuro, 0.45f)
                            : Color.Lerp(EstiloShow.Lila, EstiloShow.MoradoOscuro, 0.55f);
                EstiloShow.Panel(r, fondo.WithAlpha(0.95f * alfa), px(22f), -1f, visto);

                // Arriba, el nivel; en medio, qué es; abajo, el nombre y si ya es tuyo.
                EstiloShow.TextoEnPastilla(new Vector2(r.center.x, r.y + px(26f)), $"NIVEL {n}", tam(20f), Color.white.WithAlpha(alfa),
                                           (tuyo ? EstiloShow.Verde : EstiloShow.MoradoOscuro).WithAlpha(alfa));
                string tipo = p.tipo switch
                {
                    Temporada.Tipo.Gorro => "GORRO",
                    Temporada.Tipo.Cara => "CARA",
                    Temporada.Tipo.Cuerpo => "CUERPO",
                    _ => "TÍTULO",
                };
                EstiloShow.Texto(new Rect(r.x, r.y + px(52f), r.width, px(40f)), tipo, tam(visto ? 30f : 26f),
                                 (p.EsPrenda ? EstiloShow.Oro : EstiloShow.Cian).WithAlpha(alfa));
                if (p.EsPrenda)
                {
                    float e = px(visto ? 54f : 44f);
                    EstiloShow.Estrella(new Rect(r.center.x - e * 0.5f, r.y + px(94f), e, e), EstiloShow.Oro.WithAlpha(alfa), visto ? Time.unscaledTime * 40f : 0f);
                }
                EstiloShow.Texto(new Rect(r.x + px(8f), r.y + px(p.EsPrenda ? 150f : 98f), r.width - px(16f), px(p.EsPrenda ? 48f : 90f)),
                                 p.EsPrenda ? p.nombre : $"«{p.nombre}»", tam(visto ? 24f : 21f), Color.white.WithAlpha(alfa),
                                 TextAnchor.MiddleCenter, EstiloShow.Fuente.Titulo, true);
                if (tuyo)
                    EstiloShow.TextoEnPastilla(new Vector2(r.center.x, r.yMax - px(22f)), "¡TUYO!", tam(20f), Color.white.WithAlpha(alfa), EstiloShow.Verde.WithAlpha(alfa));
            }
            // Flechas a los lados.
            float g = px(52f);
            EstiloShow.Glifo(new Rect(px(40f), zona.center.y - g * 0.5f, g, g), "<", EstiloShow.GlifoTecla);
            EstiloShow.Glifo(new Rect(zona.width - px(40f) - g, zona.center.y - g * 0.5f, g, g), ">", EstiloShow.GlifoTecla);
        }

        void DibujarRetos(Rect r)
        {
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));
            EstiloShow.Panel(r, EstiloShow.MoradoOscuro.WithAlpha(0.88f), px(28f));
            EstiloShow.Texto(new Rect(r.x + px(24f), r.y + px(10f), r.width, px(50f)), "RETOS DE HOY", tam(34f), EstiloShow.Oro, TextAnchor.MiddleLeft);
            EstiloShow.Texto(new Rect(r.x, r.y + px(10f), r.width - px(24f), px(50f)), $"+{Temporada.XpReto} XP cada uno", tam(22f), Color.white,
                             TextAnchor.MiddleRight, EstiloShow.Fuente.Texto);
            float y = r.y + px(70f);
            foreach (var (reto, progreso, hecho) in ProgresoPase.RetosDeHoy())
            {
                var fila = new Rect(r.x + px(20f), y, r.width - px(40f), px(64f));
                EstiloShow.Panel(fila, (hecho ? EstiloShow.Verde : EstiloShow.Morado).WithAlpha(0.85f), fila.height * 0.5f, -1f, false);
                EstiloShow.Texto(new Rect(fila.x + px(24f), fila.y, fila.width * 0.66f, fila.height), reto.texto, tam(23f), Color.white,
                                 TextAnchor.MiddleLeft, EstiloShow.Fuente.Texto, true);
                var b = new Rect(fila.x + fila.width * 0.68f, fila.center.y - px(14f), fila.width * 0.29f, px(28f));
                EstiloShow.Barra(b, reto.objetivo > 0 ? progreso / (float)reto.objetivo : 1f, hecho ? EstiloShow.Oro : ColorPase);
                EstiloShow.Texto(b, hecho ? "¡HECHO!" : $"{progreso} / {reto.objetivo}", tam(19f), Color.white);
                y += px(74f);
            }
        }

        void DibujarCuenta(Rect r)
        {
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));
            EstiloShow.Panel(r, EstiloShow.MoradoOscuro.WithAlpha(0.88f), px(28f));
            EstiloShow.Texto(new Rect(r.x + px(24f), r.y + px(10f), r.width, px(50f)), "TU CUENTA", tam(34f), EstiloShow.Oro, TextAnchor.MiddleLeft);

            string estado, detalle = Cuenta.Aviso;
            Color color;
            switch (Cuenta.EstadoActual)
            {
                case Cuenta.Estado.ConCuenta:
                    estado = "Sesión iniciada";
                    color = EstiloShow.Verde;
                    if (string.IsNullOrEmpty(detalle))
                        detalle = Cuenta.Correo.Length > 0 ? $"{Cuenta.Correo}\nTu progreso se guarda en tu cuenta." : "Tu progreso se guarda en tu cuenta.";
                    break;
                case Cuenta.Estado.Invitado:
                    estado = "Invitado";
                    color = EstiloShow.Oro;
                    break;
                case Cuenta.Estado.Conectando:
                    estado = "Conectando…";
                    color = EstiloShow.Cian;
                    break;
                default:
                    estado = "Sin conexión";
                    color = new Color(1f, 0.5f, 0.45f);
                    break;
            }
            EstiloShow.TextoEnPastilla(new Vector2(r.center.x, r.y + px(100f)), estado, tam(30f), Color.white, color.WithAlpha(0.9f));
            EstiloShow.Texto(new Rect(r.x + px(30f), r.y + px(136f), r.width - px(60f), px(110f)), detalle, tam(22f), Color.white,
                             TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto, true);
            if (!Cuenta.Principal && Cuenta.EstadoActual != Cuenta.Estado.Conectando)
                EstiloShow.Texto(new Rect(r.x + px(30f), r.yMax - px(56f), r.width - px(60f), px(44f)),
                                 "(Hay otra copia del juego abierta: esta no recuerda la sesión.)", tam(18f), new Color(1f, 1f, 1f, 0.75f),
                                 TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto, true);
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
