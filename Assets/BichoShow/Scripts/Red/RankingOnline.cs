using System;
using System.Collections.Generic;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// El ranking mundial de tiempos (Unity Leaderboards): un ranking por carrera, con id
    /// "tiempo-" + id de la carrera, que se crea una vez desde Unity (ventana Deployment, con los
    /// archivos de Assets/BichoShow/Rankings; ver Docs/Ranking.md). Guarda el mejor tiempo de cada
    /// jugador de Unity (cada vez que se abre el juego es uno nuevo: ver RedShow) con su nombre y su
    /// bicho; al leerlo se queda el mejor de cada nombre. Si no hay internet o falta crear el
    /// ranking, no pasa nada: el de este ordenador sigue funcionando.
    /// </summary>
    public static class RankingOnline
    {
        public struct Fila
        {
            public int puesto;
            public string nombre;
            public float segundos;
            public string bicho;
        }

        [Serializable]
        class Datos
        {
            public string nombre = "";
            public string bicho = "";
        }

        static readonly Dictionary<string, List<Fila>> filas = new Dictionary<string, List<Fila>>();
        static readonly Dictionary<string, string> estados = new Dictionary<string, string>();
        static readonly HashSet<string> pidiendo = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimpiarEstaticos()
        {
            filas.Clear();
            estados.Clear();
            pidiendo.Clear();
        }

        public static string IdRanking(string carrera) => "tiempo-" + carrera;

        /// <summary>Lo último que se ha leído de esa carrera (vacío si aún nada).</summary>
        public static IReadOnlyList<Fila> Filas(string carrera) =>
            filas.TryGetValue(carrera, out var l) ? l : (IReadOnlyList<Fila>)Array.Empty<Fila>();

        /// <summary>"Cargando…", "Sin conexión"… ("" si está bien).</summary>
        public static string Estado(string carrera) => estados.TryGetValue(carrera, out var e) ? e : "";

        public static async void Subir(string carrera, float segundos, string nombre, string bicho)
        {
            try
            {
                await RedShow.IniciarServicios();
                var opciones = new AddPlayerScoreOptions { Metadata = new Dictionary<string, string> { { "nombre", nombre }, { "bicho", bicho } } };
                await LeaderboardsService.Instance.AddPlayerScoreAsync(IdRanking(carrera), segundos, opciones);
                filas.Remove(carrera); // la próxima vez que se mire, se vuelve a pedir
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Bicho Show: no se pudo subir el tiempo al ranking mundial ({Explicar(e)}).");
            }
        }

        /// <summary>Pide a internet el ranking de esa carrera (los 10 mejores, uno por nombre).</summary>
        public static async void Pedir(string carrera)
        {
            if (!pidiendo.Add(carrera)) return;
            if (!filas.ContainsKey(carrera)) estados[carrera] = "Cargando…";
            try
            {
                await RedShow.IniciarServicios();
                var pagina = await LeaderboardsService.Instance.GetScoresAsync(IdRanking(carrera), new GetScoresOptions { Limit = 50, IncludeMetadata = true });
                var lista = new List<Fila>();
                var vistos = new HashSet<string>();
                foreach (var e in pagina.Results)
                {
                    Datos d = null;
                    try { if (!string.IsNullOrEmpty(e.Metadata)) d = JsonUtility.FromJson<Datos>(e.Metadata); } catch { }
                    string nombre = d != null && !string.IsNullOrEmpty(d.nombre) ? d.nombre : "?";
                    if (!vistos.Add(nombre)) continue; // el mismo nombre otro día: solo su mejor tiempo
                    lista.Add(new Fila { puesto = lista.Count + 1, nombre = nombre, segundos = (float)e.Score, bicho = d != null ? d.bicho : "" });
                    if (lista.Count == RankingTiempos.Puestos) break;
                }
                filas[carrera] = lista;
                estados[carrera] = lista.Count == 0 ? "Nadie todavía. ¡Sé el primero!" : "";
            }
            catch (Exception e)
            {
                estados[carrera] = Explicar(e);
                Debug.LogWarning($"Bicho Show: no se pudo leer el ranking mundial de {carrera} ({e.Message}).");
            }
            finally
            {
                pidiendo.Remove(carrera);
            }
        }

        static string Explicar(Exception e)
        {
            if (e is LeaderboardsException le)
            {
                switch (le.Reason)
                {
                    case LeaderboardsExceptionReason.LeaderboardNotFound:
                    case LeaderboardsExceptionReason.NotFound:
                        return "Falta crear el ranking (Docs/Ranking.md)";
                    case LeaderboardsExceptionReason.NoInternetConnection:
                        return "Sin conexión";
                    case LeaderboardsExceptionReason.TooManyRequests:
                        return "Demasiadas peticiones; prueba en un rato";
                }
            }
            return "Sin conexión";
        }
    }
}
