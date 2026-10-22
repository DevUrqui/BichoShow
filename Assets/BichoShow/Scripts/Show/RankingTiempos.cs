using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Ranking de tiempos de las carreras: lo que tarda cada jugador (no los bots) desde la salida
    /// hasta cruzar la meta. Se guarda en este ordenador (ranking.json, el mejor tiempo de cada
    /// nombre, los <see cref="Puestos"/> primeros) y se sube al ranking mundial
    /// (<see cref="RankingOnline"/>, Unity Leaderboards) si hay internet.
    /// </summary>
    public static class RankingTiempos
    {
        public const int Puestos = 10;

        /// <summary>Las carreras con ranking: id (el del ranking online es "tiempo-" + id) y nombre.</summary>
        public static readonly (string id, string nombre)[] Carreras =
        {
            ("costura", "Costura a la fuga"),
            ("despensa", "Despensa Deslizante"),
            ("cumbre", "La Cumbre del Girasol"),
        };

        [Serializable]
        public class Marca
        {
            public string nombre;
            public float segundos;
            public string bicho;
            public string fecha;
        }

        [Serializable]
        class Tabla
        {
            public string id;
            public List<Marca> marcas = new List<Marca>();
        }

        [Serializable]
        class Archivo
        {
            public List<Tabla> tablas = new List<Tabla>();
        }

        static Archivo datos;

        static string Ruta => Path.Combine(Application.persistentDataPath, "ranking.json");

        static Archivo Datos
        {
            get
            {
                if (datos != null) return datos;
                try
                {
                    if (File.Exists(Ruta)) datos = JsonUtility.FromJson<Archivo>(File.ReadAllText(Ruta));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Bicho Show: no se pudo leer el ranking ({e.Message}). Empieza uno nuevo.");
                }
                if (datos == null) datos = new Archivo();
                return datos;
            }
        }

        static Tabla TablaDe(string id)
        {
            foreach (var t in Datos.tablas) if (t.id == id) return t;
            var nueva = new Tabla { id = id };
            Datos.tablas.Add(nueva);
            return nueva;
        }

        /// <summary>Los mejores tiempos de esa carrera en este ordenador (del más rápido al más lento).</summary>
        public static IReadOnlyList<Marca> Locales(string id) => TablaDe(id).marcas;

        /// <summary>
        /// Apunta un tiempo (y lo sube al ranking mundial). Devuelve el puesto en el ranking de este
        /// ordenador (1 = récord) o 0 si no entra o ese nombre ya tenía un tiempo mejor.
        /// </summary>
        public static int Apuntar(string id, string nombre, float segundos, string bicho)
        {
            if (string.IsNullOrEmpty(id) || segundos <= 0f) return 0;
            RankingOnline.Subir(id, segundos, nombre, bicho);

            var marcas = TablaDe(id).marcas;
            var anterior = marcas.Find(m => m.nombre == nombre);
            if (anterior != null)
            {
                if (anterior.segundos <= segundos) return 0;
                marcas.Remove(anterior);
            }
            var marca = new Marca { nombre = nombre, segundos = segundos, bicho = bicho, fecha = DateTime.Now.ToString("yyyy-MM-dd") };
            int puesto = marcas.FindIndex(m => m.segundos > segundos);
            if (puesto < 0) puesto = marcas.Count;
            marcas.Insert(puesto, marca);
            if (marcas.Count > Puestos) marcas.RemoveRange(Puestos, marcas.Count - Puestos);
            Guardar();
            return puesto < Puestos ? puesto + 1 : 0;
        }

        static void Guardar()
        {
            try
            {
                File.WriteAllText(Ruta, JsonUtility.ToJson(Datos, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Bicho Show: no se pudo guardar el ranking ({e.Message}).");
            }
        }

        /// <summary>1:05.23</summary>
        public static string Formato(float segundos)
        {
            int minutos = Mathf.FloorToInt(segundos / 60f);
            return $"{minutos}:{segundos - minutos * 60f:00.00}";
        }
    }
}
