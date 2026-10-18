using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Herramientas para fabricar sonidos por código, sin archivos de audio: osciladores,
    /// ruido filtrado y envolventes. Cada sonido es una función que devuelve la muestra en el
    /// instante t; Crear la evalúa entera, la normaliza y la convierte en AudioClip.
    /// </summary>
    public static class Sintetizador
    {
        public const int Frecuencia = 44100;
        const float DosPi = Mathf.PI * 2f;

        /// <summary>Genera un clip evaluando "muestra(t)" para cada instante. Se normaliza al volumen indicado.</summary>
        public static AudioClip Crear(string nombre, float duracion, System.Func<float, float> muestra,
                                      float pico = 0.9f, int frecuencia = Frecuencia)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(duracion * frecuencia));
            var datos = new float[n];
            float maximo = 1e-6f;
            for (int i = 0; i < n; i++)
            {
                float v = muestra(i / (float)frecuencia);
                if (float.IsNaN(v) || float.IsInfinity(v)) v = 0f;
                datos[i] = v;
                maximo = Mathf.Max(maximo, Mathf.Abs(v));
            }

            // Normalizar y suavizar el principio y el final (sin chasquidos).
            float escala = pico / maximo;
            int borde = Mathf.Min(n / 4, frecuencia / 200);
            for (int i = 0; i < n; i++)
            {
                float k = 1f;
                if (i < borde) k = i / (float)borde;
                else if (i > n - 1 - borde) k = (n - 1 - i) / (float)borde;
                datos[i] *= escala * k;
            }

            var clip = AudioClip.Create(nombre, n, 1, frecuencia, false);
            clip.SetData(datos, 0);
            return clip;
        }

        // ------------------------------------------------------------------
        // Envolventes
        // ------------------------------------------------------------------

        /// <summary>Sube en "ataque" segundos y luego cae exponencialmente ("caida" = rapidez).</summary>
        public static float Golpe(float t, float ataque, float caida)
        {
            if (t < 0f) return 0f;
            return t < ataque ? t / ataque : Mathf.Exp(-(t - ataque) * caida);
        }

        /// <summary>Sube, se mantiene y baja: 0 fuera de [0, duracion].</summary>
        public static float Meseta(float t, float duracion, float ataque, float salida)
        {
            if (t < 0f || t > duracion) return 0f;
            if (t < ataque) return t / ataque;
            if (t > duracion - salida) return (duracion - t) / salida;
            return 1f;
        }

        /// <summary>Campana suave de 0 a duracion (sube y baja como un seno).</summary>
        public static float Campana(float t, float duracion) =>
            t < 0f || t > duracion ? 0f : Mathf.Sin(Mathf.PI * t / duracion);

        /// <summary>Frecuencia de una nota MIDI (69 = La 440).</summary>
        public static float Nota(float midi) => 440f * Mathf.Pow(2f, (midi - 69f) / 12f);

        // ------------------------------------------------------------------
        // Oscilador: acumula la fase, así se pueden hacer barridos de frecuencia sin saltos
        // ------------------------------------------------------------------

        public class Oscilador
        {
            float fase;
            readonly float dt;

            public Oscilador(int frecuencia = Frecuencia) => dt = 1f / frecuencia;

            float Avanzar(float hz)
            {
                float f = fase;
                fase += hz * dt;
                fase -= Mathf.Floor(fase);
                return f;
            }

            public float Seno(float hz) => Mathf.Sin(Avanzar(hz) * DosPi);
            public float Cuadrada(float hz, float ciclo = 0.5f) => Avanzar(hz) < ciclo ? 1f : -1f;
            public float Sierra(float hz) => Avanzar(hz) * 2f - 1f;
            public float Triangulo(float hz)
            {
                float f = Avanzar(hz);
                return f < 0.5f ? f * 4f - 1f : 3f - f * 4f;
            }
        }

        // ------------------------------------------------------------------
        // Ruido con filtros de un polo (grave = paso bajo, agudo = paso alto)
        // ------------------------------------------------------------------

        public class Ruido
        {
            readonly System.Random azar;
            float bajo, bajo2;

            public Ruido(int semilla = 1) => azar = new System.Random(semilla);

            public float Blanco() => (float)(azar.NextDouble() * 2.0 - 1.0);

            /// <summary>Ruido grave. corte 0..1 (0.02 = muy sordo, 0.5 = casi blanco).</summary>
            public float Grave(float corte)
            {
                bajo += (Blanco() - bajo) * Mathf.Clamp01(corte);
                return bajo;
            }

            /// <summary>Ruido agudo (quita los graves).</summary>
            public float Agudo(float corte)
            {
                float b = Blanco();
                bajo2 += (b - bajo2) * Mathf.Clamp01(corte);
                return b - bajo2;
            }

            /// <summary>Ruido de banda: entre dos cortes.</summary>
            public float Banda(float corteBajo, float corteAlto)
            {
                float b = Blanco();
                bajo += (b - bajo) * Mathf.Clamp01(corteAlto);
                bajo2 += (bajo - bajo2) * Mathf.Clamp01(corteBajo);
                return bajo - bajo2;
            }

            public float Rango(float min, float max) => min + (float)azar.NextDouble() * (max - min);
        }

        /// <summary>Filtro de paso bajo de un polo para suavizar cualquier señal.</summary>
        public class PasoBajo
        {
            float valor;
            public float Filtrar(float entrada, float corte)
            {
                valor += (entrada - valor) * Mathf.Clamp01(corte);
                return valor;
            }
        }
    }
}
