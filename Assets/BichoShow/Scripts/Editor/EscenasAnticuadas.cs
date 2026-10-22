using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Al darle a Play, comprueba si alguna escena hecha por un generador es de una versión
    /// anterior de ese generador (el código ha cambiado, pero la escena no se ha vuelto a crear).
    /// Si es así, ofrece rehacerla antes de jugar. Cada generador guarda en su escena un objeto
    /// vacío llamado "Version del generador N". También avisa si faltan por preparar los
    /// personajes (Bichete, Zumbi, Concha…) y ofrece prepararlos.
    /// </summary>
    [InitializeOnLoad]
    static class EscenasAnticuadas
    {
        public const string Marca = "Version del generador ";

        /// <summary>Escenas con versión: ruta, versión actual del generador y cómo rehacerla.</summary>
        static readonly (string ruta, int version, System.Action crear)[] Generadas =
        {
            (CreadorCumbre.RutaEscena, CreadorCumbre.Version, CreadorCumbre.Crear),
            (CreadorDespensa.RutaEscena, CreadorDespensa.Version, CreadorDespensa.Crear),
        };

        static EscenasAnticuadas() => EditorApplication.playModeStateChanged += AlCambiarModo;

        static void AlCambiarModo(PlayModeStateChange cambio)
        {
            if (cambio != PlayModeStateChange.ExitingEditMode) return;
            if (FaltanPersonajes()) return;

            var viejas = new System.Collections.Generic.List<(string ruta, int version, System.Action crear)>();
            foreach (var g in Generadas)
                if (File.Exists(g.ruta) && VersionEnArchivo(g.ruta) < g.version) viejas.Add(g);
            if (viejas.Count == 0) return;

            string nombres = string.Join(", ", viejas.ConvertAll(v => Path.GetFileNameWithoutExtension(v.ruta)));
            int opcion = EditorUtility.DisplayDialogComplex("Bicho Show",
                $"Estas escenas se crearon con una versión anterior de su generador y no tienen los últimos cambios:\n\n{nombres}\n\n¿Rehacerlas antes de jugar?",
                "Rehacer y jugar", "Cancelar", "Jugar así");
            if (opcion == 2) return;

            EditorApplication.isPlaying = false;
            if (opcion == 1) return;

            string activa = SceneManager.GetActiveScene().path;
            EditorApplication.delayCall += () =>
            {
                foreach (var v in viejas) v.crear();
                if (!string.IsNullOrEmpty(activa) && File.Exists(activa)) EditorSceneManager.OpenScene(activa);
                // Solo se juega si de verdad se han rehecho (el generador puede cancelarse).
                foreach (var v in viejas) if (VersionEnArchivo(v.ruta) < v.version) return;
                EditorApplication.isPlaying = true;
            };
        }

        /// <summary>
        /// Si hay modelos de los personajes pero no están preparados, ofrece prepararlos (y entonces
        /// cancela este Play y vuelve a darle cuando acaba). Devuelve si ha cancelado el Play.
        /// </summary>
        static bool FaltanPersonajes()
        {
            if (!File.Exists(E.RutaPrefabBicho) || !File.Exists(E.Raiz + "/Modelos/Bichete.fbx")) return false;
            if (CreadorPersonajeAnimado.Preparados()) return false;
            int opcion = EditorUtility.DisplayDialogComplex("Bicho Show",
                "Los personajes (Bichete, Zumbi, Concha, Cornelio y Chispa) no están preparados o sus modelos han cambiado.\n\n¿Prepararlos ahora?",
                "Preparar y jugar", "Cancelar", "Jugar así");
            if (opcion == 2) return false;
            EditorApplication.isPlaying = false;
            if (opcion == 1) return true;
            EditorApplication.delayCall += () =>
            {
                CreadorPersonajeAnimado.AplicarBichete();
                if (CreadorPersonajeAnimado.Preparados()) EditorApplication.isPlaying = true;
            };
            return true;
        }

        /// <summary>La versión anotada en la escena (0 si no tiene, como las de antes de este aviso).</summary>
        static int VersionEnArchivo(string ruta)
        {
            var m = Regex.Match(File.ReadAllText(ruta), "m_Name: " + Regex.Escape(Marca) + @"(\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }
    }
}
