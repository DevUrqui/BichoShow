using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Luz, cielo, niebla y post-procesado por tema, para que los mapas no se vean planos.
    ///  - Barrio (Mudanza): tarde cálida.
    ///  - Playa (Voley): sol fuerte, cielo azul intenso.
    ///  - Plató (Final y menú): noche morada con brillos de neón.
    /// Menú "BichoShow > Ambiente: aplicar a todas las escenas", y los generadores lo usan al crear.
    /// </summary>
    public static class Ambientes
    {
        public enum Tema { Barrio, Playa, Plato }

        const string Carpeta = E.Raiz + "/Ambiente";

        class Ajustes
        {
            public Color sol, relleno, cieloTinte, cieloSuelo, niebla, ambienteCielo, ambienteHorizonte, ambienteSuelo;
            public float intensidadSol, intensidadRelleno, exposicionCielo, nieblaInicio, nieblaFin;
            public Vector3 rotacionSol, rotacionRelleno;
            public float bloom, umbralBloom, contraste, saturacion, temperatura, vineta, exposicion;
        }

        static Ajustes Para(Tema tema)
        {
            switch (tema)
            {
                case Tema.Playa:
                    return new Ajustes
                    {
                        sol = new Color(1f, 0.95f, 0.86f), intensidadSol = 1.6f, rotacionSol = new Vector3(55f, -25f, 0f),
                        relleno = new Color(0.55f, 0.75f, 1f), intensidadRelleno = 0.35f, rotacionRelleno = new Vector3(30f, 150f, 0f),
                        cieloTinte = new Color(0.35f, 0.62f, 1f), cieloSuelo = new Color(0.55f, 0.75f, 0.85f), exposicionCielo = 1.3f,
                        niebla = new Color(0.72f, 0.86f, 1f), nieblaInicio = 60f, nieblaFin = 220f,
                        ambienteCielo = new Color(0.78f, 0.86f, 1f), ambienteHorizonte = new Color(0.7f, 0.72f, 0.72f), ambienteSuelo = new Color(0.45f, 0.42f, 0.38f),
                        bloom = 0.35f, umbralBloom = 1f, contraste = 10f, saturacion = 22f, temperatura = 3f, vineta = 0.18f, exposicion = 0.05f,
                    };
                case Tema.Plato:
                    return new Ajustes
                    {
                        sol = new Color(1f, 0.92f, 0.96f), intensidadSol = 1.15f, rotacionSol = new Vector3(60f, -20f, 0f),
                        relleno = new Color(0.75f, 0.45f, 1f), intensidadRelleno = 0.45f, rotacionRelleno = new Vector3(25f, 160f, 0f),
                        cieloTinte = new Color(0.45f, 0.25f, 0.8f), cieloSuelo = new Color(0.12f, 0.06f, 0.2f), exposicionCielo = 0.45f,
                        niebla = new Color(0.25f, 0.15f, 0.4f), nieblaInicio = 45f, nieblaFin = 160f,
                        ambienteCielo = new Color(0.62f, 0.52f, 0.85f), ambienteHorizonte = new Color(0.45f, 0.38f, 0.55f), ambienteSuelo = new Color(0.22f, 0.16f, 0.28f),
                        bloom = 0.6f, umbralBloom = 0.9f, contraste = 15f, saturacion = 20f, temperatura = -2f, vineta = 0.3f, exposicion = 0.1f,
                    };
                default: // Barrio
                    return new Ajustes
                    {
                        sol = new Color(1f, 0.9f, 0.78f), intensidadSol = 1.45f, rotacionSol = new Vector3(45f, -38f, 0f),
                        relleno = new Color(0.55f, 0.65f, 1f), intensidadRelleno = 0.3f, rotacionRelleno = new Vector3(30f, 145f, 0f),
                        cieloTinte = new Color(0.5f, 0.62f, 0.95f), cieloSuelo = new Color(0.55f, 0.48f, 0.55f), exposicionCielo = 1.15f,
                        niebla = new Color(0.8f, 0.8f, 0.92f), nieblaInicio = 55f, nieblaFin = 170f,
                        ambienteCielo = new Color(0.75f, 0.8f, 0.95f), ambienteHorizonte = new Color(0.65f, 0.6f, 0.62f), ambienteSuelo = new Color(0.36f, 0.32f, 0.34f),
                        bloom = 0.35f, umbralBloom = 1f, contraste = 12f, saturacion = 18f, temperatura = 8f, vineta = 0.22f, exposicion = 0.05f,
                    };
            }
        }

        // ------------------------------------------------------------------
        // Menú: todas las escenas de BichoShow
        // ------------------------------------------------------------------

        [MenuItem("BichoShow/Ambiente: aplicar a todas las escenas")]
        public static void AplicarATodas()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string abierta = EditorSceneManager.GetActiveScene().path;

            var hechas = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { E.CarpetaEscenas }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                var escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
                Aplicar(TemaDeEscena(escena.name));
                EditorSceneManager.SaveScene(escena);
                hechas.Add(escena.name);
            }

            if (!string.IsNullOrEmpty(abierta)) EditorSceneManager.OpenScene(abierta, OpenSceneMode.Single);
            Debug.Log($"Bicho Show: ambiente aplicado a {string.Join(", ", hechas)}.");
        }

        public static Tema TemaDeEscena(string nombre)
        {
            if (nombre.Contains("Voley")) return Tema.Playa;
            if (nombre.Contains("Final") || nombre.Contains("Menu")) return Tema.Plato;
            return Tema.Barrio;
        }

        // ------------------------------------------------------------------
        // Aplicar a la escena abierta
        // ------------------------------------------------------------------

        /// <summary>Aplica el tema a la escena abierta: luces, cielo, niebla, ambiente, post-procesado y cámaras.</summary>
        public static void Aplicar(Tema tema)
        {
            var a = Para(tema);
            E.CrearCarpeta(E.Raiz, "Ambiente");
            AjustarPipeline();

            // --- Sol (reutiliza la luz direccional que ya haya) ---
            Light sol = null;
            foreach (var l in Object.FindObjectsByType<Light>())
                if (l.type == LightType.Directional && l.name != "Relleno") { sol = l; break; }
            if (sol == null) sol = new GameObject("Sol").AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.color = a.sol;
            sol.intensity = a.intensidadSol;
            sol.shadows = LightShadows.Soft;
            sol.shadowStrength = 0.85f;
            sol.transform.rotation = Quaternion.Euler(a.rotacionSol);

            // --- Relleno de color, sin sombras, desde el lado contrario ---
            var rellenoGo = GameObject.Find("Relleno") ?? new GameObject("Relleno");
            if (!rellenoGo.TryGetComponent(out Light relleno)) relleno = rellenoGo.AddComponent<Light>();
            relleno.type = LightType.Directional;
            relleno.color = a.relleno;
            relleno.intensity = a.intensidadRelleno;
            relleno.shadows = LightShadows.None;
            rellenoGo.transform.rotation = Quaternion.Euler(a.rotacionRelleno);

            // --- Cielo con degradado y niebla ---
            var cielo = CieloPara(tema, a);
            RenderSettings.skybox = cielo;
            RenderSettings.sun = sol;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = a.niebla;
            RenderSettings.fogStartDistance = a.nieblaInicio;
            RenderSettings.fogEndDistance = a.nieblaFin;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = a.ambienteCielo;
            RenderSettings.ambientEquatorColor = a.ambienteHorizonte;
            RenderSettings.ambientGroundColor = a.ambienteSuelo;

            // --- Post-procesado global ---
            var volumenGo = GameObject.Find("PostProcesado") ?? new GameObject("PostProcesado");
            if (!volumenGo.TryGetComponent(out Volume volumen)) volumen = volumenGo.AddComponent<Volume>();
            volumen.isGlobal = true;
            volumen.priority = 1f;
            volumen.sharedProfile = PerfilPara(tema, a);

            // --- Cámaras: cielo de fondo, post-procesado y antialiasing ---
            foreach (var cam in Object.FindObjectsByType<Camera>())
                PrepararCamara(cam);
        }

        /// <summary>Lo que necesita una cámara para verse bien con el ambiente.</summary>
        public static void PrepararCamara(Camera cam)
        {
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, 400f);
            var datos = cam.GetUniversalAdditionalCameraData();
            datos.renderPostProcessing = true;
            datos.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            datos.antialiasingQuality = AntialiasingQuality.Medium;
            EditorUtility.SetDirty(cam);
            EditorUtility.SetDirty(datos);
        }

        static Material CieloPara(Tema tema, Ajustes a)
        {
            string ruta = $"{Carpeta}/Cielo{tema}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(mat, ruta);
            }
            mat.SetFloat("_SunDisk", 2f);          // sol de alta calidad
            mat.SetFloat("_SunSize", 0.035f);
            mat.SetFloat("_SunSizeConvergence", 6f);
            mat.SetFloat("_AtmosphereThickness", tema == Tema.Plato ? 0.6f : 1f);
            mat.SetColor("_SkyTint", a.cieloTinte);
            mat.SetColor("_GroundColor", a.cieloSuelo);
            mat.SetFloat("_Exposure", a.exposicionCielo);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static VolumeProfile PerfilPara(Tema tema, Ajustes a)
        {
            string ruta = $"{Carpeta}/Perfil{tema}.asset";
            var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
            if (perfil == null)
            {
                perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(perfil, ruta);
            }

            var tono = Componente<Tonemapping>(perfil);
            tono.mode.Override(TonemappingMode.Neutral);

            var bloom = Componente<Bloom>(perfil);
            bloom.intensity.Override(a.bloom);
            bloom.threshold.Override(a.umbralBloom);
            bloom.scatter.Override(0.7f);

            var color = Componente<ColorAdjustments>(perfil);
            color.postExposure.Override(a.exposicion);
            color.contrast.Override(a.contraste);
            color.saturation.Override(a.saturacion);

            var balance = Componente<WhiteBalance>(perfil);
            balance.temperature.Override(a.temperatura);

            var vineta = Componente<Vignette>(perfil);
            vineta.intensity.Override(a.vineta);
            vineta.smoothness.Override(0.45f);

            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();
            return perfil;
        }

        /// <summary>Busca el efecto en el perfil o lo añade (guardado dentro del propio asset).</summary>
        static T Componente<T>(VolumeProfile perfil) where T : VolumeComponent
        {
            if (perfil.TryGet(out T existente)) return existente;
            var nuevo = perfil.Add<T>(true);
            nuevo.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(nuevo, perfil);
            return nuevo;
        }

        /// <summary>Sombras hasta más lejos: los mapas miden hasta 80 m.</summary>
        static void AjustarPipeline()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null || asset.shadowDistance >= 90f) continue;
                asset.shadowDistance = 90f;
                EditorUtility.SetDirty(asset);
            }
        }
    }
}
