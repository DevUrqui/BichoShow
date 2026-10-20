using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Menú "BichoShow > Compilar el juego para Windows".
    /// Deja el juego listo para jugar sin Unity en la carpeta Builds/Windows del proyecto
    /// (BichoShow.exe y su carpeta _Data: hay que copiar las dos cosas juntas).
    /// Antes comprueba que estén las escenas del show, las ordena (el menú primero) y ajusta
    /// lo necesario para jugar por Parsec: que siga funcionando aunque la ventana no tenga el foco.
    /// </summary>
    public static class CompilarJuego
    {
        static readonly string[] EscenasDelShow = { "Menu", "Mudanza", "Patata", "Corona", "Lluvia", "Pintura", "Voley", "Final" };

        public static string CarpetaSalida =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "Windows");

        [MenuItem("BichoShow/Compilar el juego para Windows")]
        public static void Compilar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // --- Escenas: el menú primero y luego las pruebas ---
            var rutas = new List<string>();
            foreach (var nombre in EscenasDelShow)
            {
                string ruta = $"{E.CarpetaEscenas}/{nombre}.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta) == null)
                {
                    // Sin el menú no hay juego; sin alguna prueba, el show sortea entre las que haya.
                    if (nombre == "Menu" || nombre == "Final")
                    {
                        EditorUtility.DisplayDialog("Bicho Show",
                            $"Falta la escena \"{nombre}\". Créala desde el menú BichoShow (Crear escena…) y vuelve a compilar.", "Vale");
                        return;
                    }
                    Debug.LogWarning($"Bicho Show: la escena \"{nombre}\" no existe todavía; el juego compilado no la tendrá.");
                    continue;
                }
                rutas.Add(ruta);
            }
            OrdenarListaDelBuild(rutas);
            RecursosEfectos.Crear();

            // --- Ajustes del ejecutable ---
            PlayerSettings.productName = "Bicho Show";
            PlayerSettings.runInBackground = true;          // sigue jugando aunque la ventana pierda el foco (Parsec, Discord…)
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

            string exe = Path.Combine(CarpetaSalida, "BichoShow.exe");
            Directory.CreateDirectory(CarpetaSalida);

            var opciones = new BuildPlayerOptions
            {
                scenes = rutas.ToArray(),
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            var informe = BuildPipeline.BuildPlayer(opciones);
            var resumen = informe.summary;
            if (resumen.result == BuildResult.Succeeded)
            {
                float megas = resumen.totalSize / (1024f * 1024f);
                Debug.Log($"Bicho Show: juego compilado en {exe} ({megas:0} MB, {resumen.totalTime.TotalSeconds:0} s).");
                EditorUtility.RevealInFinder(exe);
                EditorUtility.DisplayDialog("Bicho Show",
                    $"¡Listo! El juego está en:\n{CarpetaSalida}\n\nAbre BichoShow.exe para jugar. Si lo copias a otro sitio, copia la carpeta entera.", "Vale");
            }
            else
            {
                var errores = new List<string>();
                foreach (var paso in informe.steps)
                    foreach (var mensaje in paso.messages)
                        if (mensaje.type == LogType.Error || mensaje.type == LogType.Exception)
                            errores.Add(mensaje.content);
                string texto = errores.Count > 0 ? string.Join("\n", errores.GetRange(0, Mathf.Min(5, errores.Count))) : resumen.result.ToString();
                Debug.LogError($"Bicho Show: no se pudo compilar.\n{string.Join("\n", errores)}");
                EditorUtility.DisplayDialog("Bicho Show", $"No se pudo compilar:\n\n{texto}\n\n(Más detalles en la consola.)", "Vale");
            }
        }

        /// <summary>Lista del build: las escenas del show, en orden y activadas; el resto se queda pero desactivado.</summary>
        static void OrdenarListaDelBuild(List<string> rutas)
        {
            var lista = new List<EditorBuildSettingsScene>();
            foreach (var r in rutas) lista.Add(new EditorBuildSettingsScene(r, true));
            foreach (var e in EditorBuildSettings.scenes)
                if (!rutas.Contains(e.path)) lista.Add(new EditorBuildSettingsScene(e.path, false));
            EditorBuildSettings.scenes = lista.ToArray();
        }
    }
}
