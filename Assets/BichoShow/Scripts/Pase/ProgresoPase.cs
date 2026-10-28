using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// El progreso del pase de la cuenta (Cuenta): experiencia, nivel y retos del día.
    /// Se guarda en este ordenador (para jugar sin conexión) y en la nube (Cloud Save), así
    /// no se pierde al reinstalar ni al jugar en otro ordenador con la misma cuenta. Si las dos
    /// copias no coinciden, gana la que tiene más experiencia.
    /// Lo desbloqueado sale del nivel: un premio es tuyo si tu nivel llega al suyo.
    /// </summary>
    public static class ProgresoPase
    {
        [Serializable]
        class Datos
        {
            public string temporada = Temporada.Id;
            public int xp;
            public string dia = "";          // yyyy-MM-dd de los retos
            public int[] retos = new int[3]; // progreso de cada reto del día
            public bool[] hechos = new bool[3];
            public bool pendienteDeSubir;    // ganado sin conexión: subirlo en cuanto se pueda
        }

        /// <summary>Lo ganado en una partida, para enseñarlo luego en el menú.</summary>
        public readonly struct Ganancia
        {
            public readonly string motivo;
            public readonly int xp;
            public Ganancia(string motivo, int xp) { this.motivo = motivo; this.xp = xp; }
        }

        static Datos datos = new Datos();
        static string cargadoPara;   // id del jugador (o "local") de los datos que hay en memoria
        static bool subiendo;

        /// <summary>Lo ganado desde la última vez que el menú lo enseñó.</summary>
        public static readonly List<Ganancia> Recientes = new List<Ganancia>();
        /// <summary>Niveles subidos desde la última vez que el menú lo celebró.</summary>
        public static readonly List<int> NivelesNuevos = new List<int>();
        public static event Action AlCambiar;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimpiarEstaticos()
        {
            datos = new Datos();
            cargadoPara = null;
            subiendo = false;
            Recientes.Clear();
            NivelesNuevos.Clear();
            AlCambiar = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AlArrancar()
        {
            Cuenta.AlCambiar += AlCambiarCuenta;
            CargarLocal(Clave);
        }

        // ------------------------------------------------------------------
        // Consultas
        // ------------------------------------------------------------------

        public static int Xp => datos.xp;
        public static int Nivel => Mathf.Min(Temporada.Niveles, datos.xp / Temporada.XpPorNivel);
        public static bool Completo => Nivel >= Temporada.Niveles;
        /// <summary>Experiencia dentro del nivel actual (0..XpPorNivel).</summary>
        public static int XpEnNivel => Completo ? Temporada.XpPorNivel : datos.xp % Temporada.XpPorNivel;

        /// <summary>¿Se puede poner esta prenda (o este título)? Lo que no es del pase, siempre.</summary>
        public static bool Desbloqueado(Temporada.Tipo tipo, string id)
        {
            int nivel = Temporada.NivelDe(tipo, id);
            return nivel == 0 || Nivel >= nivel;
        }

        /// <summary>Los tres retos de hoy, con su progreso.</summary>
        public static IEnumerable<(Temporada.Reto reto, int progreso, bool hecho)> RetosDeHoy()
        {
            PonerAlDia();
            var indices = Temporada.RetosDelDia(DateTime.Today);
            for (int k = 0; k < 3; k++)
                yield return (Temporada.RetosPosibles[indices[k]], datos.retos[k], datos.hechos[k]);
        }

        // ------------------------------------------------------------------
        // Ganar experiencia
        // ------------------------------------------------------------------

        /// <summary>Algo que cuenta para el pase le ha pasado al jugador 1 de este ordenador.</summary>
        public static void Evento(string evento, int cantidad = 1)
        {
            if (!Temporada.EnCurso || cantidad <= 0) return;
            PonerAlDia();
            int antes = Nivel;

            int xp = Temporada.XpDe(evento, cantidad);
            if (xp > 0) Sumar(MotivoDe(evento, cantidad), xp);

            var indices = Temporada.RetosDelDia(DateTime.Today);
            for (int k = 0; k < 3; k++)
            {
                var reto = Temporada.RetosPosibles[indices[k]];
                if (datos.hechos[k] || reto.evento != evento) continue;
                datos.retos[k] = Mathf.Min(reto.objetivo, datos.retos[k] + cantidad);
                if (datos.retos[k] >= reto.objetivo)
                {
                    datos.hechos[k] = true;
                    Sumar($"Reto: {reto.texto}", Temporada.XpReto);
                }
            }

            for (int n = antes + 1; n <= Nivel; n++) NivelesNuevos.Add(n);
            datos.pendienteDeSubir = true;
            GuardarLocal();
            AlCambiar?.Invoke();
        }

        /// <summary>Sube lo pendiente a la nube (al acabar un show, al volver al menú…).</summary>
        public static void Subir() => _ = SubirANube();

        static void Sumar(string motivo, int xp)
        {
            datos.xp = Mathf.Min(datos.xp + xp, Temporada.Niveles * Temporada.XpPorNivel);
            // Lo mismo seguido (las estrellas de cada ronda…) se junta en una línea.
            int i = Recientes.FindIndex(g => g.motivo == motivo);
            if (i >= 0) Recientes[i] = new Ganancia(motivo, Recientes[i].xp + xp);
            else Recientes.Add(new Ganancia(motivo, xp));
        }

        static string MotivoDe(string evento, int cantidad) => evento switch
        {
            Temporada.Eventos.Ronda => "Pruebas jugadas",
            Temporada.Eventos.Estrellas => "Estrellas",
            Temporada.Eventos.Victoria => "Pruebas ganadas",
            Temporada.Eventos.Show => "Show completo",
            Temporada.Eventos.GanaShow => "¡Show ganado!",
            _ => evento,
        };

        /// <summary>Al cambiar de día, retos nuevos.</summary>
        static void PonerAlDia()
        {
            string hoy = DateTime.Today.ToString("yyyy-MM-dd");
            if (datos.dia == hoy) return;
            datos.dia = hoy;
            datos.retos = new int[3];
            datos.hechos = new bool[3];
        }

        // ------------------------------------------------------------------
        // Guardar: en este ordenador y en la nube
        // ------------------------------------------------------------------

        static string ClaveNube => "pase_" + Temporada.Id;
        static string Clave => $"BichoShow.Pase.{Temporada.Id}.{Cuenta.IdJugador ?? "local"}";

        static void CargarLocal(string clave)
        {
            string json = PlayerPrefs.GetString(clave, "");
            datos = Leer(json) ?? new Datos();
            cargadoPara = clave;
        }

        static void GuardarLocal()
        {
            PlayerPrefs.SetString(Clave, JsonUtility.ToJson(datos));
            PlayerPrefs.Save();
        }

        static Datos Leer(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var d = JsonUtility.FromJson<Datos>(json);
                if (d == null || d.temporada != Temporada.Id) return null;
                if (d.retos == null || d.retos.Length != 3) d.retos = new int[3];
                if (d.hechos == null || d.hechos.Length != 3) d.hechos = new bool[3];
                return d;
            }
            catch { return null; }
        }

        /// <summary>Ha entrado (o cambiado) el jugador: su progreso de aquí y el de la nube, juntos.</summary>
        static async void AlCambiarCuenta()
        {
            if (Cuenta.IdJugador == null) return;
            string clave = Clave;
            if (clave != cargadoPara)
            {
                // Lo que se ganó como invitado sin conexión (clave "local") pasa a la cuenta.
                var anterior = datos;
                CargarLocal(clave);
                if (anterior != null && anterior.xp > datos.xp && anterior.pendienteDeSubir) datos = anterior;
                GuardarLocal();
            }

            try
            {
                var leido = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { ClaveNube });
                if (Clave != clave) return; // ha vuelto a cambiar mientras tanto
                Datos nube = null;
                if (leido.TryGetValue(ClaveNube, out var item)) nube = Leer(item.Value.GetAs<string>());
                if (nube != null && nube.xp > datos.xp)
                {
                    // La nube va por delante (otro ordenador): se queda la de la nube, con los retos de hoy que más lleven.
                    if (nube.dia == datos.dia)
                        for (int k = 0; k < 3; k++)
                        {
                            nube.retos[k] = Mathf.Max(nube.retos[k], datos.retos[k]);
                            nube.hechos[k] |= datos.hechos[k];
                        }
                    nube.pendienteDeSubir = false;
                    datos = nube;
                    GuardarLocal();
                }
                else if (nube == null || datos.xp > nube.xp || datos.pendienteDeSubir)
                {
                    await SubirANube();
                }
                AlCambiar?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Bicho Show: no se pudo leer el pase de la nube ({e.Message}).");
            }
        }

        static async Task SubirANube()
        {
            if (subiendo || Cuenta.IdJugador == null || !datos.pendienteDeSubir) return;
            subiendo = true;
            string clave = Clave;
            try
            {
                datos.pendienteDeSubir = false;
                string json = JsonUtility.ToJson(datos);
                await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { ClaveNube, json } });
                if (Clave == clave) GuardarLocal();
            }
            catch (Exception e)
            {
                datos.pendienteDeSubir = true; // otra vez será
                Debug.LogWarning($"Bicho Show: no se pudo subir el pase a la nube ({e.Message}).");
            }
            finally
            {
                subiendo = false;
            }
        }

#if UNITY_EDITOR
        /// <summary>Para probar: suma experiencia a mano.</summary>
        public static void SumarParaProbar(int xp)
        {
            int antes = Nivel;
            Sumar("Prueba", xp);
            for (int n = antes + 1; n <= Nivel; n++) NivelesNuevos.Add(n);
            datos.pendienteDeSubir = true;
            GuardarLocal();
            AlCambiar?.Invoke();
        }
#endif
    }
}
