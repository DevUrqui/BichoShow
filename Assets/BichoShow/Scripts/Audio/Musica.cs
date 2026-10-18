using UnityEngine;
using static BichoShow.Sintetizador;

namespace BichoShow
{
    /// <summary>
    /// Música del show, compuesta por código: un bucle de 8 compases con bajo, batería, acordes
    /// y una melodía saltarina. Dos temas: el del plató (menú, marcadores) y el de las pruebas
    /// (más rápido). Cambia de uno a otro con un fundido. Musica.Poner(Musica.Tema.Prueba).
    /// </summary>
    public static class Musica
    {
        public enum Tema { Ninguno, Plato, Prueba }

        public const string ClaveVolumen = "BichoShow.VolumenMusica";
        const int FrecuenciaMusica = 22050;

        static Reproductor reproductor;
        static AudioClip clipPlato, clipPrueba;
        static float volumen = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimpiarEstaticos()
        {
            reproductor = null;
            volumen = -1f;
        }

        public static float Volumen
        {
            get
            {
                if (volumen < 0f) volumen = PlayerPrefs.GetFloat(ClaveVolumen, 0.5f);
                return volumen;
            }
            set
            {
                volumen = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(ClaveVolumen, volumen);
            }
        }

        public static void Poner(Tema tema) => Reproductor.Obtener().Cambiar(tema);

        static AudioClip ClipDe(Tema tema)
        {
            switch (tema)
            {
                case Tema.Plato:
                    if (clipPlato == null) clipPlato = Componer("MusicaPlato", 112f, 2, false);
                    return clipPlato;
                case Tema.Prueba:
                    if (clipPrueba == null) clipPrueba = Componer("MusicaPrueba", 138f, 5, true);
                    return clipPrueba;
            }
            return null;
        }

        // ------------------------------------------------------------------
        // Composición: Do - La m - Fa - Sol, dos compases cada acorde
        // ------------------------------------------------------------------

        static AudioClip Componer(string nombre, float tempo, int semilla, bool energica)
        {
            float negra = 60f / tempo;
            float corchea = negra / 2f;
            float semicorchea = negra / 4f;
            const int compases = 8;
            float duracion = compases * 4 * negra;

            int[] raices = { 48, 45, 41, 43 };                 // Do, La, Fa, Sol (graves)
            int[][] acordes =
            {
                new[] { 60, 64, 67 }, new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 },
            };

            // Melodía: notas del acorde (y alguna de paso), una por corchea, con silencios.
            var azar = new System.Random(semilla);
            int pasos = compases * 8;
            var melodia = new int[pasos];
            int anterior = 72;
            for (int i = 0; i < pasos; i++)
            {
                int acorde = (i / 16) % 4;
                bool silencio = azar.NextDouble() < (energica ? 0.3 : 0.45) || (i % 16 == 15);
                if (silencio) { melodia[i] = -1; continue; }
                var notas = acordes[acorde];
                int candidata = notas[azar.Next(notas.Length)] + 12;
                if (azar.NextDouble() < 0.25) candidata += azar.NextDouble() < 0.5 ? 2 : -1; // nota de paso
                // No saltar demasiado: se queda cerca de la anterior.
                while (candidata - anterior > 7) candidata -= 12;
                while (anterior - candidata > 7) candidata += 12;
                candidata = Mathf.Clamp(candidata, 67, 86);
                melodia[i] = candidata;
                anterior = candidata;
            }

            var bajo = new Oscilador(FrecuenciaMusica);
            var bajo2 = new Oscilador(FrecuenciaMusica);
            var mel = new Oscilador(FrecuenciaMusica);
            var mel2 = new Oscilador(FrecuenciaMusica);
            var arpegio = new Oscilador(FrecuenciaMusica);
            var bombo = new Oscilador(FrecuenciaMusica);
            var ruidoCaja = new Ruido(31);
            var ruidoPlatillo = new Ruido(32);
            var filtroBajo = new PasoBajo();
            var filtroArpegio = new PasoBajo();

            return Crear(nombre, duracion, t =>
            {
                float tiempoCompas = 4f * negra;
                int compas = (int)(t / tiempoCompas) % compases;
                int acorde = (compas / 2) % 4;
                float enNegra = t % negra;
                int negraIndice = (int)(t / negra);
                float enCorchea = t % corchea;
                int corcheaIndice = (int)(t / corchea);
                float enSemi = t % semicorchea;
                int semiIndice = (int)(t / semicorchea);

                // Bajo: raíz y octava alternando cada corchea.
                int notaBajo = raices[acorde] + (corcheaIndice % 2 == 1 ? 12 : 0);
                float vBajo = filtroBajo.Filtrar(bajo.Triangulo(Nota(notaBajo)) + bajo2.Cuadrada(Nota(notaBajo), 0.5f) * 0.15f, 0.3f)
                              * Golpe(enCorchea, 0.004f, 5f) * 0.55f;

                // Batería
                float vBombo = bombo.Seno(45f + 110f * Mathf.Exp(-enNegra * 30f)) * Golpe(enNegra, 0.002f, 12f)
                               * (negraIndice % 2 == 0 || energica ? 0.9f : 0.5f);
                bool tocaCaja = negraIndice % 2 == 1;
                float vCaja = tocaCaja ? ruidoCaja.Banda(0.05f, 0.6f) * Golpe(enNegra, 0.001f, 18f) * (energica ? 0.55f : 0.3f) : 0f;
                float vPlatillo = ruidoPlatillo.Agudo(0.5f) * Golpe(enCorchea, 0.001f, 45f) * (corcheaIndice % 2 == 1 ? 0.22f : 0.12f);

                // Arpegio suave de acompañamiento (semicorcheas)
                var notas = acordes[acorde];
                int notaArpegio = notas[semiIndice % notas.Length] + ((semiIndice / notas.Length) % 2 == 0 ? 0 : 12);
                float vArpegio = filtroArpegio.Filtrar(arpegio.Cuadrada(Nota(notaArpegio), 0.25f), 0.15f)
                                 * Golpe(enSemi, 0.002f, 14f) * (energica ? 0.14f : 0.18f);

                // Melodía
                int paso = corcheaIndice % pasos;
                float vMelodia = 0f;
                if (melodia[paso] >= 0)
                {
                    float f = Nota(melodia[paso]) * (1f + 0.004f * Mathf.Sin(t * 33f));
                    vMelodia = (mel.Cuadrada(f, 0.35f) * 0.35f + mel2.Triangulo(f) * 0.65f) * Golpe(enCorchea, 0.004f, 6f) * 0.32f;
                }

                return vBajo + vBombo + vCaja + vPlatillo + vArpegio + vMelodia;
            }, 0.7f, FrecuenciaMusica);
        }

        // ------------------------------------------------------------------
        // Reproductor con fundido entre temas
        // ------------------------------------------------------------------

        class Reproductor : MonoBehaviour
        {
            AudioSource a, b;
            Tema actual = Tema.Ninguno;
            float fundido = 1f;

            public static Reproductor Obtener()
            {
                if (reproductor != null) return reproductor;
                var go = new GameObject("Bicho Show: música");
                DontDestroyOnLoad(go);
                reproductor = go.AddComponent<Reproductor>();
                reproductor.a = Crear(go);
                reproductor.b = Crear(go);
                return reproductor;
            }

            static AudioSource Crear(GameObject go)
            {
                var f = go.AddComponent<AudioSource>();
                f.loop = true;
                f.playOnAwake = false;
                f.spatialBlend = 0f;
                f.volume = 0f;
                return f;
            }

            public void Cambiar(Tema tema)
            {
                if (tema == actual) return;
                actual = tema;
                (a, b) = (b, a); // "a" es la que entra, "b" la que se va
                a.clip = ClipDe(tema);
                a.volume = 0f;
                if (a.clip != null) a.Play();
                fundido = 0f;
            }

            void Update()
            {
                fundido = Mathf.MoveTowards(fundido, 1f, Time.unscaledDeltaTime / 1.2f);
                float v = Volumen * (actual == Tema.Prueba ? 0.55f : 0.75f);
                a.volume = v * fundido;
                b.volume = v * (1f - fundido) * (b.clip != null ? 1f : 0f);
                if (fundido >= 1f && b.isPlaying) b.Stop();
            }
        }
    }
}
