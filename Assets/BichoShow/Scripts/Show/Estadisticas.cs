using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Apunta lo que pasa en cada ronda (saltos, placajes, derribos, caídas, objetos rotos,
    /// trampas, puntos…) y al acabar escribe un resumen en un archivo de texto, para ajustar
    /// el juego con datos después de cada prueba con amigos.
    ///
    /// Dónde: carpeta "Estadisticas" junto a BichoShow.exe (o en la carpeta del proyecto si se
    /// juega desde Unity). Un archivo por sesión: partida_AAAA-MM-DD_HH-MM.txt
    /// </summary>
    public static class Estadisticas
    {
        class Jugador
        {
            public bool bot;
            public readonly Dictionary<string, int> cuentas = new Dictionary<string, int>();
        }

        static readonly string[] ColumnasJugador = { "saltar", "placaje", "golpear", "agarrar", "lanzar", "derribado", "caida" };
        static readonly HashSet<string> SinCronologia = new HashSet<string> { "saltar", "placaje", "golpear", "agarrar", "lanzar" };
        const int MaximoCronologia = 250;

        static string archivo;
        static bool enRonda;
        static string nombreRonda;
        static float inicioRonda, ultimoEventoImportante, silencioMaximo, inicioSilencioMaximo;
        static readonly Dictionary<int, Jugador> jugadores = new Dictionary<int, Jugador>();
        static readonly Dictionary<string, int> cuentasRonda = new Dictionary<string, int>();
        static readonly List<string> cronologia = new List<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimpiarEstaticos()
        {
            archivo = null;
            enRonda = false;
            jugadores.Clear();
            cuentasRonda.Clear();
            cronologia.Clear();
        }

        public static string Carpeta =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Estadisticas");

        // ------------------------------------------------------------------
        // API
        // ------------------------------------------------------------------

        public static void InicioRonda(RondaBase ronda)
        {
            enRonda = true;
            nombreRonda = ronda.gameObject.scene.name;
            inicioRonda = ultimoEventoImportante = Time.time;
            silencioMaximo = 0f;
            inicioSilencioMaximo = 0f;
            jugadores.Clear();
            cuentasRonda.Clear();
            cronologia.Clear();
            foreach (var b in ControladorBicho.Todos)
                jugadores[b.IndiceJugador] = new Jugador { bot = b.EsBot };
        }

        /// <summary>Apunta algo que ha pasado. "bicho" (opcional) = a quién le ha pasado.</summary>
        public static void Evento(string tipo, ControladorBicho bicho = null, string detalle = null)
        {
            if (!enRonda) return;
            Sumar(cuentasRonda, tipo);

            if (bicho != null)
            {
                if (!jugadores.TryGetValue(bicho.IndiceJugador, out var j))
                    jugadores[bicho.IndiceJugador] = j = new Jugador { bot = bicho.EsBot };
                Sumar(j.cuentas, tipo);
            }

            if (SinCronologia.Contains(tipo)) return;

            // Lo "importante" (no saltar o correr): sirve para medir los ratos muertos.
            float ahora = Time.time;
            float silencio = ahora - ultimoEventoImportante;
            if (silencio > silencioMaximo)
            {
                silencioMaximo = silencio;
                inicioSilencioMaximo = ultimoEventoImportante - inicioRonda;
            }
            ultimoEventoImportante = ahora;

            if (cronologia.Count < MaximoCronologia)
            {
                string quien = bicho != null ? $" J{bicho.IndiceJugador + 1}{(bicho.EsBot ? " (bot)" : "")}" : "";
                cronologia.Add($"  {Reloj(ahora - inicioRonda)}  {tipo}{quien}{(string.IsNullOrEmpty(detalle) ? "" : "  · " + detalle)}");
            }
        }

        public static void FinRonda(RondaBase ronda)
        {
            if (!enRonda) return;
            enRonda = false;
            float duracion = Time.time - inicioRonda;
            float silencioFinal = Time.time - ultimoEventoImportante;
            if (silencioFinal > silencioMaximo)
            {
                silencioMaximo = silencioFinal;
                inicioSilencioMaximo = ultimoEventoImportante - inicioRonda;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"=== {nombreRonda} · {System.DateTime.Now:HH:mm} · duró {Reloj(duracion)} ===");
            sb.AppendLine($"Resultado: Rojo {ronda.PuntosEquipo(0)} · Azul {ronda.PuntosEquipo(1)}");

            // Tabla por jugador
            sb.Append("Jugador        ");
            foreach (var c in ColumnasJugador) sb.Append(c.PadLeft(10));
            sb.AppendLine();
            var indices = new List<int>(jugadores.Keys);
            indices.Sort();
            foreach (int i in indices)
            {
                var j = jugadores[i];
                sb.Append($"J{i + 1} {(j.bot ? "(bot)" : "(humano)"),-11}");
                foreach (var c in ColumnasJugador)
                    sb.Append((j.cuentas.TryGetValue(c, out int n) ? n : 0).ToString().PadLeft(10));
                sb.AppendLine();
            }

            // Totales de la ronda
            var tipos = new List<string>(cuentasRonda.Keys);
            tipos.Sort();
            var partes = new List<string>();
            foreach (var t in tipos) partes.Add($"{t} {cuentasRonda[t]}");
            sb.AppendLine($"Totales: {string.Join(" · ", partes)}");
            sb.AppendLine($"Rato más largo sin nada destacable: {silencioMaximo:0} s (a partir de {Reloj(inicioSilencioMaximo)})");

            sb.AppendLine("Cronología:");
            foreach (var linea in cronologia) sb.AppendLine(linea);
            sb.AppendLine();
            Escribir(sb.ToString());
        }

        /// <summary>Una línea suelta en el archivo (inicio del show, estrellas…).</summary>
        public static void Texto(string texto) => Escribir($"{System.DateTime.Now:HH:mm}  {texto}\n\n");

        // ------------------------------------------------------------------

        static void Sumar(Dictionary<string, int> d, string clave) => d[clave] = d.TryGetValue(clave, out int n) ? n + 1 : 1;

        static string Reloj(float segundos)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(segundos));
            return $"{s / 60:00}:{s % 60:00}";
        }

        static void Escribir(string texto)
        {
            try
            {
                if (archivo == null)
                {
                    Directory.CreateDirectory(Carpeta);
                    archivo = Path.Combine(Carpeta, $"partida_{System.DateTime.Now:yyyy-MM-dd_HH-mm}.txt");
                    File.AppendAllText(archivo, $"Bicho Show · estadísticas de la sesión del {System.DateTime.Now:dd/MM/yyyy HH:mm}\n\n");
                }
                File.AppendAllText(archivo, texto);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Bicho Show: no se pudieron guardar las estadísticas ({e.Message}).");
            }
        }
    }
}
