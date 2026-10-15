using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;
using M = BichoShow.Herramientas.CreadorMudanza;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Menú "BichoShow > Crear escena: Menú del show".
    /// Un plató de tele: los bichos que se unen aparecen en el escenario y desde aquí empieza
    /// el show. Es la primera escena del juego (la deja la primera en la lista del build).
    /// </summary>
    public static class CreadorMenu
    {
        const string RutaEscena = E.CarpetaEscenas + "/Menu.unity";

        [MenuItem("BichoShow/Crear escena: Menú del show")]
        public static void Crear()
        {
            var controles = AssetDatabase.LoadAssetAtPath<InputActionAsset>(E.RutaControles);
            if (controles == null)
            {
                EditorUtility.DisplayDialog("Bicho Show", $"No encuentro los controles en:\n{E.RutaControles}", "Vale");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(E.CarpetaEscenas + "/Mudanza.unity") == null ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(E.CarpetaEscenas + "/Voley.unity") == null)
            {
                EditorUtility.DisplayDialog("Bicho Show",
                    "Antes crea las escenas de las pruebas (Mudanza frágil y Voley atados) desde el menú BichoShow: el show las necesita.",
                    "Vale");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) != null &&
                !EditorUtility.DisplayDialog("Bicho Show", "La escena del menú ya existe. ¿Rehacerla?", "Rehacer", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            E.CrearCarpeta(E.Raiz, "Materiales");
            E.CrearCarpeta(E.Raiz, "Escenas");
            var basicos = E.CrearMateriales();
            var bicho = AssetDatabase.LoadAssetAtPath<GameObject>(E.RutaPrefabBicho) ?? E.CrearPrefabBicho(controles, basicos);

            CrearEscena(bicho);
            PonerPrimeraEnElBuild(RutaEscena);
            Debug.Log("Bicho Show: escena del menú creada. Dale a Play aquí para jugar el show completo.");
        }

        static void CrearEscena(GameObject prefabBicho)
        {
            var suelo = E.CrearMaterial("Plato", new Color(0.22f, 0.16f, 0.34f), 0.6f);
            var escenario = E.CrearMaterial("Escenario", new Color(0.95f, 0.75f, 0.25f), 0.5f);
            var fondo = E.CrearMaterial("FondoPlato", new Color(0.35f, 0.2f, 0.55f), 0.2f);
            var foco = E.CrearMaterial("Foco", new Color(0.9f, 0.9f, 0.95f), 0.8f);
            var rojo = E.CrearMaterial("EquipoRojo", new Color(1f, 0.38f, 0.35f), 0.3f);
            var azul = E.CrearMaterial("EquipoAzul", new Color(0.3f, 0.6f, 1f), 0.3f);

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.45f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.32f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.15f, 0.25f);

            var luz = new GameObject("Luz").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 0.9f;
            luz.shadows = LightShadows.Soft;
            luz.transform.rotation = Quaternion.Euler(50f, 20f, 0f);

            // Focos de colores del plató
            (Vector3 pos, Color color)[] focos =
            {
                (new Vector3(-5f, 5f, -3f), new Color(1f, 0.4f, 0.6f)),
                (new Vector3(5f, 5f, -3f), new Color(0.4f, 0.7f, 1f)),
                (new Vector3(0f, 6f, 2f), new Color(1f, 0.9f, 0.5f)),
            };
            foreach (var f in focos)
            {
                var l = new GameObject("FocoColor").AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 14f;
                l.intensity = 3f;
                l.color = f.color;
                l.transform.position = f.pos;
            }

            var camGo = new GameObject("Camara");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.06f, 0.16f);
            cam.fieldOfView = 40f;
            camGo.AddComponent<AudioListener>();
            var camara = camGo.AddComponent<CamaraGrupo>();
            camara.direccion = new Vector3(0f, 0.45f, -1f);
            camara.inclinacionExtraMaxima = 0f;
            camara.distanciaMinima = 11f;
            camara.distanciaMaxima = 13f;
            camara.centroSinJugadores = new Vector3(0f, 0.5f, 0f);
            camara.pantallaDividida = false;
            camara.mostrarIndicadores = false;
            camara.ColocarAlInstante();

            var plato = new GameObject("Plato").transform;
            E.Bloque("Suelo", plato, new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 20f), suelo);
            E.Bloque("Escenario", plato, new Vector3(0f, 0.15f, 0.5f), new Vector3(10f, 0.3f, 4f), escenario);
            E.Bloque("Fondo", plato, new Vector3(0f, 3f, 4f), new Vector3(18f, 6f, 0.4f), fondo);
            for (int i = 0; i < 9; i++)
            {
                float x = -8f + i * 2f;
                M.Decorado(PrimitiveType.Cube, "Panel", plato, new Vector3(x, 3f, 3.75f), new Vector3(1.6f, 4.6f, 0.08f), i % 2 == 0 ? rojo : azul);
                M.Decorado(PrimitiveType.Sphere, "Bombilla", plato, new Vector3(x, 5.7f, 3.7f), Vector3.one * 0.35f, foco);
            }
            M.Decorado(PrimitiveType.Cube, "BordeEscenario", plato, new Vector3(0f, 0.31f, -1.45f), new Vector3(10f, 0.02f, 0.1f), foco);

            var vacio = M.ZonaTrigger<ZonaMortal>("ZonaVacio", plato, new Vector3(0f, -10f, 0f), new Vector3(100f, 4f, 100f));
            vacio.motivoObjeto = "se ha caído al vacío";

            // --- Jugadores persistentes: los que se unen aquí viajan por todo el show ---
            var jugadoresGo = new GameObject("Jugadores");
            var manager = jugadoresGo.AddComponent<PlayerInputManager>();
            manager.playerPrefab = prefabBicho;
            manager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
            manager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            var so = new SerializedObject(manager);
            var maxJugadores = so.FindProperty("m_MaxPlayerCount");
            if (maxJugadores != null) maxJugadores.intValue = 4;
            so.ApplyModifiedPropertiesWithoutUndo();

            var gestor = jugadoresGo.AddComponent<GestorJugadores>();
            gestor.persistente = true;
            gestor.mostrarAyuda = false;
            gestor.colores = new[]
            {
                new Color(1f, 0.38f, 0.35f), new Color(0.3f, 0.6f, 1f),
                new Color(1f, 0.68f, 0.2f), new Color(0.45f, 0.9f, 0.95f),
            };
            var puntos = new List<Transform>();
            float[] xs = { -3f, -1f, 1f, 3f };
            for (int i = 0; i < xs.Length; i++)
            {
                var p = new GameObject($"Aparicion{i + 1}").transform;
                p.SetParent(jugadoresGo.transform, false);
                p.position = new Vector3(xs[i], 0.5f, 0.5f);
                p.rotation = Quaternion.Euler(0f, 180f, 0f); // mirando a cámara
                puntos.Add(p);
            }
            gestor.puntosAparicion = puntos.ToArray();

            // --- Show y menú ---
            new GameObject("Show").AddComponent<ShowBicho>();
            new GameObject("Menu").AddComponent<MenuPrincipal>();

            EditorSceneManager.SaveScene(escena, RutaEscena);
            AssetDatabase.SaveAssets();
        }

        static void PonerPrimeraEnElBuild(string ruta)
        {
            var escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            escenas.RemoveAll(e => e.path == ruta);
            escenas.Insert(0, new EditorBuildSettingsScene(ruta, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }
    }
}
