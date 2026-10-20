using System.Collections.Generic;
using UnityEngine;
using static BichoShow.Sintetizador;

namespace BichoShow
{
    /// <summary>Todos los sonidos del juego (se fabrican por código la primera vez que suenan).</summary>
    public enum Sonido
    {
        // Bichos
        Salto, Aterrizaje, Paso, Golpe, Derribo, Placaje, Caida, Agarrar, Lanzar, Aparecer, Estrellitas,
        // Objetos y pelota
        Grieta, Rotura, Entrega, Pelota, Bote,
        // Ronda y show
        Silbato, Bocina, Pitido, PitidoSalida, Aplausos, Ooh, Logro, Victoria,
        // Trampas y mecanismos
        Alarma, Muelle, Cangrejo, Viento, Ola, Click, Mecanismo,
        // Menú
        MenuMover, MenuAceptar,
    }

    /// <summary>
    /// Reproduce los sonidos del juego desde cualquier sitio: Sonidos.Reproducir(Sonido.Salto, posicion).
    /// Los sonidos son 2D (con la pantalla dividida no hay un único oyente), con un poco de
    /// panorama según dónde estén en pantalla y una pizca de variación de tono para que no
    /// suenen siempre igual. El volumen general se guarda en las opciones.
    /// </summary>
    public static class Sonidos
    {
        public const string ClaveVolumen = "BichoShow.VolumenEfectos";
        const int NumeroAltavoces = 24;

        static Altavoces altavoces;
        static readonly Dictionary<Sonido, AudioClip> clips = new Dictionary<Sonido, AudioClip>();
        static readonly Dictionary<Sonido, float> ultimaVez = new Dictionary<Sonido, float>();
        static float volumen = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimpiarEstaticos()
        {
            altavoces = null;
            ultimaVez.Clear();
            volumen = -1f;
        }

        /// <summary>Volumen general de los efectos (0-1), guardado en las opciones.</summary>
        public static float Volumen
        {
            get
            {
                if (volumen < 0f) volumen = PlayerPrefs.GetFloat(ClaveVolumen, 0.8f);
                return volumen;
            }
            set
            {
                volumen = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(ClaveVolumen, volumen);
            }
        }

        /// <summary>Hace sonar un efecto. "posicion" (opcional) solo sirve para el panorama izquierda/derecha.</summary>
        public static void Reproducir(Sonido sonido, Vector3? posicion = null, float volumenExtra = 1f, float tono = 1f, float variacion = 0.07f)
        {
            // Partida online: el anfitrión se lo cuenta a los invitados, y ellos solo oyen lo que les llega.
            if (!RedShow.SonidoLocal(sonido, posicion, volumenExtra, tono, variacion)) return;
            if (Volumen <= 0.001f || volumenExtra <= 0.001f) return;

            var def = Definicion(sonido);
            float ahora = Time.unscaledTime;
            if (ultimaVez.TryGetValue(sonido, out float antes) && ahora - antes < def.separacion) return;
            ultimaVez[sonido] = ahora;

            var fuente = Altavoces.Obtener().Libre();
            if (fuente == null) return;

            fuente.clip = Clip(sonido);
            fuente.volume = Mathf.Clamp01(def.volumen * volumenExtra * Volumen);
            fuente.pitch = tono * (1f + Random.Range(-variacion, variacion));
            fuente.panStereo = posicion.HasValue ? Panorama(posicion.Value) : 0f;
            fuente.Play();
        }

        static float Panorama(Vector3 posicion)
        {
            var cam = Camera.main;
            if (cam == null) return 0f;
            Vector3 v = cam.WorldToViewportPoint(posicion);
            if (v.z < 0f) return 0f;
            return Mathf.Clamp((v.x - 0.5f) * 1.2f, -0.7f, 0.7f);
        }

        static AudioClip Clip(Sonido sonido)
        {
            if (clips.TryGetValue(sonido, out var clip) && clip != null) return clip;
            var def = Definicion(sonido);
            clip = Crear(sonido.ToString(), def.duracion, def.fabricar());
            clips[sonido] = clip;
            return clip;
        }

        /// <summary>Fabrica ya todos los clips (para que el primer salto no tarde ni un fotograma).</summary>
        public static void Precargar()
        {
            foreach (Sonido s in System.Enum.GetValues(typeof(Sonido))) Clip(s);
        }

        // ------------------------------------------------------------------
        // Catálogo: duración, volumen base, separación mínima entre dos iguales y la receta
        // ------------------------------------------------------------------

        struct Def
        {
            public float duracion, volumen, separacion;
            public System.Func<System.Func<float, float>> fabricar;
            public Def(float duracion, float volumen, float separacion, System.Func<System.Func<float, float>> fabricar)
            {
                this.duracion = duracion; this.volumen = volumen; this.separacion = separacion; this.fabricar = fabricar;
            }
        }

        static Def Definicion(Sonido s)
        {
            switch (s)
            {
                // --- Bichos ---------------------------------------------------
                case Sonido.Salto: return new Def(0.2f, 0.35f, 0.04f, () =>
                {
                    var o = new Oscilador(); var o2 = new Oscilador();
                    return t =>
                    {
                        float f = 260f + 560f * Mathf.Pow(t / 0.2f, 0.6f);
                        return (o.Seno(f) + o2.Cuadrada(f * 2f) * 0.08f) * Golpe(t, 0.005f, 11f);
                    };
                });
                case Sonido.Aterrizaje: return new Def(0.18f, 0.45f, 0.05f, () =>
                {
                    var o = new Oscilador(); var r = new Ruido(2);
                    return t => o.Seno(50f + 120f * Mathf.Exp(-t * 14f)) * Golpe(t, 0.003f, 20f)
                                + r.Grave(0.12f) * Golpe(t, 0.001f, 30f) * 1.2f;
                });
                case Sonido.Paso: return new Def(0.07f, 0.12f, 0.03f, () =>
                {
                    var o = new Oscilador(); var r = new Ruido(3);
                    return t => r.Grave(0.3f) * Golpe(t, 0.001f, 70f) + o.Seno(200f * Mathf.Exp(-t * 20f)) * Golpe(t, 0.002f, 50f) * 0.5f;
                });
                case Sonido.Golpe: return new Def(0.25f, 0.7f, 0.04f, () =>
                {
                    var o = new Oscilador(); var r = new Ruido(4); var r2 = new Ruido(5);
                    return t => r.Grave(0.45f) * Golpe(t, 0.001f, 28f)
                                + o.Seno(60f + 160f * Mathf.Exp(-t * 9f)) * Golpe(t, 0.002f, 13f) * 0.9f
                                + r2.Blanco() * Golpe(t, 0.0005f, 140f) * 0.5f;
                });
                case Sonido.Derribo: return new Def(0.45f, 0.55f, 0.08f, () =>
                {
                    // "¡Bonk!" de dibujos animados
                    var o = new Oscilador(); var o2 = new Oscilador(); var r = new Ruido(6);
                    return t =>
                    {
                        float f = 540f * Mathf.Exp(-t * 3.5f);
                        return (o.Seno(f) + o2.Seno(f * 1.5f) * 0.45f) * Golpe(t, 0.002f, 8f)
                               + r.Blanco() * Golpe(t, 0.0005f, 90f) * 0.4f;
                    };
                });
                case Sonido.Placaje: return new Def(0.35f, 0.35f, 0.05f, () =>
                {
                    var r = new Ruido(7);
                    return t => r.Banda(0.02f, 0.05f + 0.3f * Campana(t, 0.35f)) * Campana(t, 0.35f) * 3f;
                });
                case Sonido.Caida: return new Def(0.95f, 0.5f, 0.1f, () =>
                {
                    // Silbido que baja (cayendo al vacío) y un "plop" al final
                    var o = new Oscilador(); var o2 = new Oscilador();
                    return t =>
                    {
                        float silbido = t < 0.75f
                            ? o.Seno(1500f * Mathf.Exp(-t * 1.8f) * (1f + 0.012f * Mathf.Sin(t * 40f))) * Meseta(t, 0.75f, 0.04f, 0.1f) * 0.6f
                            : 0f;
                        float plop = t > 0.72f ? o2.Seno(250f + 900f * (t - 0.72f) * 5f) * Golpe(t - 0.72f, 0.004f, 18f) : 0f;
                        return silbido + plop;
                    };
                });
                case Sonido.Agarrar: return new Def(0.08f, 0.3f, 0.05f, () =>
                {
                    var o = new Oscilador();
                    return t => o.Seno(450f + 1500f * t / 0.08f) * Golpe(t, 0.002f, 45f);
                });
                case Sonido.Lanzar: return new Def(0.28f, 0.4f, 0.05f, () =>
                {
                    var r = new Ruido(8); var o = new Oscilador();
                    return t => r.Banda(0.03f, 0.1f + 0.4f * t) * Campana(t, 0.28f) * 2.5f + o.Seno(300f + 900f * t) * Golpe(t, 0.01f, 15f) * 0.3f;
                });
                case Sonido.Aparecer: return new Def(0.16f, 0.3f, 0.05f, () =>
                {
                    var o = new Oscilador();
                    return t => o.Seno(300f + 1400f * Mathf.Pow(t / 0.16f, 0.5f)) * Golpe(t, 0.004f, 22f);
                });
                case Sonido.Estrellitas: return new Def(0.75f, 0.25f, 0.2f, () =>
                {
                    // Tres píos agudos (pajaritos alrededor de la cabeza)
                    var o = new Oscilador();
                    return t =>
                    {
                        float local = t % 0.22f;
                        if (t > 0.66f) return 0f;
                        float f = 2300f + 900f * Mathf.Sin(local / 0.12f * Mathf.PI);
                        return o.Seno(f) * Meseta(local, 0.12f, 0.01f, 0.04f);
                    };
                });

                // --- Objetos y pelota -----------------------------------------
                case Sonido.Grieta: return new Def(0.14f, 0.5f, 0.06f, () =>
                {
                    var r = new Ruido(9); var o = new Oscilador();
                    return t => r.Agudo(0.2f) * Golpe(t, 0.0005f, 55f) + o.Cuadrada(800f * Mathf.Exp(-t * 15f)) * Golpe(t, 0.001f, 40f) * 0.15f;
                });
                case Sonido.Rotura: return new Def(1.0f, 0.75f, 0.1f, () =>
                {
                    // Estallido + cristalitos que tintinean
                    var r = new Ruido(10); var azar = new System.Random(11);
                    const int trozos = 9;
                    var inicio = new float[trozos]; var frec = new float[trozos];
                    var osc = new Oscilador[trozos];
                    for (int i = 0; i < trozos; i++)
                    {
                        inicio[i] = (float)azar.NextDouble() * 0.45f;
                        frec[i] = 2200f + (float)azar.NextDouble() * 3800f;
                        osc[i] = new Oscilador();
                    }
                    var o = new Oscilador();
                    return t =>
                    {
                        float v = r.Agudo(0.08f) * Golpe(t, 0.001f, 9f) * 0.9f + o.Seno(90f * Mathf.Exp(-t * 6f)) * Golpe(t, 0.001f, 14f) * 0.6f;
                        for (int i = 0; i < trozos; i++)
                            v += osc[i].Seno(frec[i]) * Golpe(t - inicio[i], 0.001f, 22f) * 0.22f;
                        return v;
                    };
                });
                case Sonido.Entrega: return new Def(1.1f, 0.5f, 0.2f, () =>
                {
                    // Campanita: Do - Mi - Sol
                    var o = new Oscilador[6];
                    for (int i = 0; i < o.Length; i++) o[i] = new Oscilador();
                    float[] notas = { 84, 88, 91 };
                    return t =>
                    {
                        float v = 0f;
                        for (int i = 0; i < 3; i++)
                        {
                            float tl = t - i * 0.11f;
                            float f = Nota(notas[i]);
                            v += (o[i * 2].Seno(f) + o[i * 2 + 1].Seno(f * 2.76f) * 0.25f) * Golpe(tl, 0.003f, 4.5f);
                        }
                        return v;
                    };
                });
                case Sonido.Pelota: return new Def(0.28f, 0.45f, 0.04f, () =>
                {
                    // "Boing" gomoso
                    var o = new Oscilador(); var o2 = new Oscilador();
                    return t =>
                    {
                        float f = 170f * (1f + 0.7f * Mathf.Exp(-t * 22f)) * (1f + 0.04f * Mathf.Sin(t * 190f));
                        return (o.Seno(f) + o2.Triangulo(f * 2f) * 0.25f) * Golpe(t, 0.002f, 11f);
                    };
                });
                case Sonido.Bote: return new Def(0.2f, 0.35f, 0.05f, () =>
                {
                    var o = new Oscilador(); var r = new Ruido(12);
                    return t => o.Seno(110f * (1f + Mathf.Exp(-t * 30f))) * Golpe(t, 0.002f, 16f) + r.Grave(0.2f) * Golpe(t, 0.001f, 40f) * 0.8f;
                });

                // --- Ronda y show ---------------------------------------------
                case Sonido.Silbato: return new Def(0.6f, 0.45f, 0.2f, () =>
                {
                    var o = new Oscilador(); var r = new Ruido(13);
                    return t =>
                    {
                        float trino = Mathf.Sin(t * 2f * Mathf.PI * 30f) > 0f ? 1f : 0.93f;
                        return (o.Seno(2700f * trino) + r.Blanco() * 0.06f) * Meseta(t, 0.6f, 0.015f, 0.08f);
                    };
                });
                case Sonido.Bocina: return new Def(1.0f, 0.45f, 0.3f, () =>
                {
                    var a = new Oscilador(); var b = new Oscilador(); var c = new Oscilador(); var filtro = new PasoBajo();
                    return t => filtro.Filtrar(a.Sierra(233f) + b.Sierra(294f) + c.Sierra(349f), 0.12f) * Meseta(t, 1f, 0.03f, 0.2f);
                });
                case Sonido.Pitido: return new Def(0.15f, 0.3f, 0.2f, () =>
                {
                    var o = new Oscilador(); var filtro = new PasoBajo();
                    return t => filtro.Filtrar(o.Cuadrada(880f), 0.25f) * Meseta(t, 0.15f, 0.005f, 0.04f);
                });
                case Sonido.PitidoSalida: return new Def(0.5f, 0.35f, 0.2f, () =>
                {
                    var o = new Oscilador(); var filtro = new PasoBajo();
                    return t => filtro.Filtrar(o.Cuadrada(1320f), 0.25f) * Meseta(t, 0.5f, 0.005f, 0.15f);
                });
                case Sonido.Aplausos: return new Def(2.4f, 0.4f, 1.0f, () =>
                {
                    // Palmas sueltas (muchas, luego menos) + "¡bieeen!" de fondo
                    var r = new Ruido(14); var r2 = new Ruido(15); var azar = new System.Random(16);
                    var envolventes = new float[10]; var ganancias = new float[10];
                    int siguiente = 0;
                    float dt = 1f / Frecuencia;
                    var voces = new Oscilador[5]; var tonos = new float[5]; var filtro = new PasoBajo();
                    for (int i = 0; i < voces.Length; i++) { voces[i] = new Oscilador(); tonos[i] = 190f + (float)azar.NextDouble() * 160f; }
                    return t =>
                    {
                        float densidad = 45f * Meseta(t, 2.4f, 0.15f, 1.4f);
                        if (azar.NextDouble() < densidad * dt)
                        {
                            envolventes[siguiente] = 1f;
                            ganancias[siguiente] = 0.4f + (float)azar.NextDouble() * 0.6f;
                            siguiente = (siguiente + 1) % envolventes.Length;
                        }
                        float palmas = 0f;
                        for (int i = 0; i < envolventes.Length; i++)
                        {
                            palmas += envolventes[i] * ganancias[i];
                            envolventes[i] *= 0.9985f;
                        }
                        float ruido = r.Banda(0.08f, 0.5f) * palmas;
                        float gente = 0f;
                        for (int i = 0; i < voces.Length; i++) gente += voces[i].Sierra(tonos[i] * (1f + 0.02f * Mathf.Sin(t * (5f + i))));
                        gente = filtro.Filtrar(gente, 0.06f) * Meseta(t, 2.4f, 0.2f, 1.2f) * 0.35f;
                        return ruido + gente + r2.Grave(0.1f) * Meseta(t, 2.4f, 0.2f, 1.5f) * 0.3f;
                    };
                });
                case Sonido.Ooh: return new Def(1.3f, 0.35f, 2.0f, () =>
                {
                    // "¡Ooooh!" del público
                    var azar = new System.Random(17); var filtro = new PasoBajo(); var filtro2 = new PasoBajo();
                    var voces = new Oscilador[8]; var tonos = new float[8];
                    for (int i = 0; i < voces.Length; i++) { voces[i] = new Oscilador(); tonos[i] = 150f + (float)azar.NextDouble() * 130f; }
                    return t =>
                    {
                        float v = 0f;
                        float caida = 1f - 0.12f * t;
                        for (int i = 0; i < voces.Length; i++)
                            v += voces[i].Sierra(tonos[i] * caida * (1f + 0.015f * Mathf.Sin(t * (4f + i))));
                        return filtro2.Filtrar(filtro.Filtrar(v, 0.05f), 0.08f) * Meseta(t, 1.3f, 0.12f, 0.6f);
                    };
                });
                case Sonido.Logro: return new Def(0.75f, 0.4f, 0.3f, () =>
                {
                    var o = new Oscilador(); var o2 = new Oscilador();
                    float[] notas = { 72, 76, 79, 84 };
                    return t =>
                    {
                        int i = Mathf.Min(3, (int)(t / 0.09f));
                        float tl = t - i * 0.09f;
                        float f = Nota(notas[i]);
                        float env = i < 3 ? Meseta(tl, 0.09f, 0.004f, 0.02f) : Golpe(tl, 0.004f, 5f);
                        return (o.Cuadrada(f, 0.25f) * 0.5f + o2.Triangulo(f)) * env;
                    };
                });
                case Sonido.Victoria: return new Def(2.0f, 0.5f, 1.0f, () =>
                {
                    // Fanfarria: Sol - Do - Mi - Sol ... y acorde final
                    var o = new Oscilador[5];
                    for (int i = 0; i < o.Length; i++) o[i] = new Oscilador();
                    float[] notas = { 67, 72, 76, 79 };
                    float[] acorde = { 72, 76, 79, 84 };
                    return t =>
                    {
                        if (t < 0.52f)
                        {
                            int i = Mathf.Min(3, (int)(t / 0.13f));
                            float f = Nota(notas[i]);
                            return (o[0].Cuadrada(f, 0.3f) * 0.4f + o[1].Triangulo(f)) * Meseta(t - i * 0.13f, 0.13f, 0.004f, 0.03f);
                        }
                        float tl = t - 0.52f;
                        float v = 0f;
                        for (int i = 0; i < 4; i++) v += o[i + 1].Triangulo(Nota(acorde[i]) * (1f + 0.003f * Mathf.Sin(tl * 30f)));
                        return v * 0.35f * Meseta(tl, 1.48f, 0.01f, 0.7f) * (1f + 0.15f * Mathf.Sin(tl * 40f));
                    };
                });

                // --- Trampas y mecanismos -------------------------------------
                case Sonido.Alarma: return new Def(0.6f, 0.3f, 0.3f, () =>
                {
                    var o = new Oscilador(); var filtro = new PasoBajo();
                    return t => filtro.Filtrar(o.Cuadrada((int)(t / 0.1f) % 2 == 0 ? 660f : 880f), 0.2f) * Meseta(t, 0.6f, 0.01f, 0.05f);
                });
                case Sonido.Muelle: return new Def(0.35f, 0.45f, 0.08f, () =>
                {
                    // ¡Boiiing! de muelle
                    var o = new Oscilador();
                    return t => o.Triangulo((140f + 650f * t / 0.35f) * (1f + 0.22f * Mathf.Sin(t * 2f * Mathf.PI * 18f))) * Golpe(t, 0.003f, 6f);
                });
                case Sonido.Cangrejo: return new Def(0.55f, 0.35f, 0.3f, () =>
                {
                    var o = new Oscilador(); var r = new Ruido(18);
                    return t =>
                    {
                        float tl = t % 0.13f;
                        if (t > 0.52f) return 0f;
                        return (o.Cuadrada(1700f) * 0.4f + r.Agudo(0.3f)) * Golpe(tl, 0.0005f, 120f);
                    };
                });
                case Sonido.Viento: return new Def(1.8f, 0.4f, 0.5f, () =>
                {
                    var r = new Ruido(19);
                    return t => r.Banda(0.01f, 0.06f + 0.05f * Mathf.Sin(t * 7f)) * Campana(t, 1.8f) * 4f;
                });
                case Sonido.Ola: return new Def(1.8f, 0.45f, 0.5f, () =>
                {
                    var r = new Ruido(20); var r2 = new Ruido(21);
                    return t => r.Grave(0.04f + 0.08f * Campana(t, 1.8f)) * Golpe(t, 0.5f, 2.5f) * 3f
                                + r2.Agudo(0.3f) * Golpe(t - 0.4f, 0.2f, 3f) * 0.3f;
                });
                case Sonido.Click: return new Def(0.12f, 0.35f, 0.05f, () =>
                {
                    var o = new Oscilador(); var o2 = new Oscilador();
                    return t => o.Cuadrada(1300f) * Golpe(t, 0.0005f, 90f) * 0.5f + o2.Seno(90f) * Golpe(t, 0.002f, 30f);
                });
                case Sonido.Mecanismo: return new Def(0.9f, 0.35f, 0.3f, () =>
                {
                    var o = new Oscilador(); var r = new Ruido(22); var filtro = new PasoBajo();
                    return t => filtro.Filtrar(o.Sierra(55f) + r.Blanco() * 0.4f, 0.05f) * Meseta(t, 0.9f, 0.08f, 0.25f) * 2f;
                });

                // --- Menú -----------------------------------------------------
                case Sonido.MenuMover: return new Def(0.06f, 0.2f, 0.03f, () =>
                {
                    var o = new Oscilador();
                    return t => o.Cuadrada(660f + 330f * t / 0.06f, 0.3f) * Golpe(t, 0.002f, 40f);
                });
                case Sonido.MenuAceptar: return new Def(0.18f, 0.3f, 0.1f, () =>
                {
                    var o = new Oscilador();
                    return t => o.Cuadrada(t < 0.07f ? 660f : 990f, 0.3f) * Meseta(t, 0.18f, 0.003f, 0.06f);
                });
            }
            return new Def(0.1f, 0f, 1f, () => t => 0f);
        }

        // ------------------------------------------------------------------
        // Altavoces: un objeto que no se destruye entre escenas, con varias AudioSource
        // ------------------------------------------------------------------

        class Altavoces : MonoBehaviour
        {
            AudioSource[] fuentes;
            int siguiente;

            public static Altavoces Obtener()
            {
                if (altavoces != null) return altavoces;
                var go = new GameObject("Bicho Show: altavoces");
                DontDestroyOnLoad(go);
                altavoces = go.AddComponent<Altavoces>();
                altavoces.fuentes = new AudioSource[NumeroAltavoces];
                for (int i = 0; i < NumeroAltavoces; i++)
                {
                    var f = go.AddComponent<AudioSource>();
                    f.playOnAwake = false;
                    f.spatialBlend = 0f;
                    altavoces.fuentes[i] = f;
                }
                return altavoces;
            }

            public AudioSource Libre()
            {
                for (int i = 0; i < fuentes.Length; i++)
                {
                    var f = fuentes[(siguiente + i) % fuentes.Length];
                    if (!f.isPlaying)
                    {
                        siguiente = (siguiente + i + 1) % fuentes.Length;
                        return f;
                    }
                }
                // Todas ocupadas: se reutiliza la más antigua.
                var vieja = fuentes[siguiente];
                siguiente = (siguiente + 1) % fuentes.Length;
                return vieja;
            }
        }
    }
}
