using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Menú "BichoShow > Crear escena de prueba del bicho".
    /// Genera materiales, el prefab del bicho y una escena para probarlo con mandos,
    /// para no tener que montarlo todo a mano en el editor. Se puede ejecutar varias
    /// veces: rehace el prefab y la escena con la versión actual.
    /// </summary>
    public static class CreadorEscenaPrueba
    {
        internal const string Raiz = "Assets/BichoShow";
        internal const string CarpetaMateriales = Raiz + "/Materiales";
        internal const string CarpetaPrefabs = Raiz + "/Prefabs";
        internal const string CarpetaEscenas = Raiz + "/Escenas";
        internal const string RutaControles = Raiz + "/Input/BichoControles.inputactions";
        internal const string RutaPrefabBicho = CarpetaPrefabs + "/Bicho.prefab";
        const string RutaEscena = CarpetaEscenas + "/PruebaBicho.unity";

        [MenuItem("BichoShow/Crear escena de prueba del bicho")]
        public static void Crear()
        {
            var controles = AssetDatabase.LoadAssetAtPath<InputActionAsset>(RutaControles);
            if (controles == null)
            {
                EditorUtility.DisplayDialog("Bicho Show", $"No encuentro los controles en:\n{RutaControles}", "Vale");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) != null &&
                !EditorUtility.DisplayDialog("Bicho Show",
                    "La escena de prueba ya existe. ¿Rehacerla? Se perderán los cambios que hayas hecho en ella.",
                    "Rehacer", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CrearCarpeta(Raiz, "Materiales");
            CrearCarpeta(Raiz, "Prefabs");
            CrearCarpeta(Raiz, "Escenas");

            var materiales = CrearMateriales();
            var prefab = CrearPrefabBicho(controles, materiales);
            CrearEscena(prefab, materiales);

            Debug.Log("Bicho Show: escena de prueba creada. Dale a Play y pulsa un botón en cada mando para unirte.");
        }

        // ------------------------------------------------------------------
        // Materiales
        // ------------------------------------------------------------------

        internal class Materiales
        {
            public Material bicho, ojo, pupila, suelo, borde, obstaculo, caja, pelota;
        }

        internal static Materiales CrearMateriales()
        {
            return new Materiales
            {
                bicho = CrearMaterial("Bicho", Color.white, 0.35f),
                ojo = CrearMaterial("Ojo", Color.white, 0.8f),
                pupila = CrearMaterial("Pupila", new Color(0.08f, 0.08f, 0.1f), 0.9f),
                suelo = CrearMaterial("Suelo", new Color(0.62f, 0.86f, 0.74f), 0.1f),
                borde = CrearMaterial("Borde", new Color(0.95f, 0.95f, 0.98f), 0.1f),
                obstaculo = CrearMaterial("Obstaculo", new Color(1f, 0.55f, 0.25f), 0.3f),
                caja = CrearMaterial("Caja", new Color(0.85f, 0.65f, 0.42f), 0.15f),
                pelota = CrearMaterial("Pelota", new Color(0.85f, 0.4f, 0.95f), 0.5f),
            };
        }

        internal static Material CrearMaterial(string nombre, Color color, float brillo)
        {
            string ruta = $"{CarpetaMateriales}/{nombre}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, ruta);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", brillo);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------
        // Prefab del bicho
        // ------------------------------------------------------------------

        internal static GameObject CrearPrefabBicho(InputActionAsset controles, Materiales m)
        {
            var raiz = new GameObject("Bicho");

            // Físicas: el pivote está en los pies; la cápsula mide 1.2 m de alto.
            var rb = raiz.AddComponent<Rigidbody>();
            rb.mass = 5f;
            rb.linearDamping = 0.2f;
            rb.angularDamping = 1.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var capsula = raiz.AddComponent<CapsuleCollider>();
            capsula.radius = 0.45f;
            capsula.height = 1.2f;
            capsula.center = new Vector3(0f, 0.6f, 0f);

            // Aspecto: un "casi esfera" con orejas y ojos saltones. Sin colliders propios.
            var cuerpo = Pieza(PrimitiveType.Capsule, "Cuerpo", raiz.transform,
                new Vector3(0f, 0.6f, 0f), new Vector3(0.9f, 0.6f, 0.9f), m.bicho);

            var orejaIzq = Pieza(PrimitiveType.Sphere, "OrejaIzq", raiz.transform,
                new Vector3(-0.22f, 1.18f, -0.05f), new Vector3(0.18f, 0.38f, 0.12f), m.bicho);
            orejaIzq.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            var orejaDer = Pieza(PrimitiveType.Sphere, "OrejaDer", raiz.transform,
                new Vector3(0.22f, 1.18f, -0.05f), new Vector3(0.18f, 0.38f, 0.12f), m.bicho);
            orejaDer.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);

            foreach (float lado in new[] { -1f, 1f })
            {
                var ojo = Pieza(PrimitiveType.Sphere, lado < 0 ? "OjoIzq" : "OjoDer", raiz.transform,
                    new Vector3(0.16f * lado, 0.82f, 0.36f), Vector3.one * 0.24f, m.ojo);
                Pieza(PrimitiveType.Sphere, "Pupila", ojo.transform,
                    new Vector3(0f, 0f, 0.38f), Vector3.one * 0.5f, m.pupila);
            }

            // Controles
            var entrada = raiz.AddComponent<PlayerInput>();
            entrada.actions = controles;
            entrada.defaultActionMap = "Jugador";
            entrada.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            var controlador = raiz.AddComponent<ControladorBicho>();
            controlador.renderersColor = new[]
            {
                cuerpo.GetComponent<Renderer>(),
                orejaIzq.GetComponent<Renderer>(),
                orejaDer.GetComponent<Renderer>(),
            };

            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBicho);
            Object.DestroyImmediate(raiz);
            return prefab;
        }

        internal static GameObject Pieza(PrimitiveType tipo, string nombre, Transform padre,
                                Vector3 posicion, Vector3 escala, Material material, bool conCollider = false)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = escala;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!conCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        // ------------------------------------------------------------------
        // Escena
        // ------------------------------------------------------------------

        static void CrearEscena(GameObject prefabBicho, Materiales m)
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Luz y ambiente
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.75f, 0.78f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.6f, 0.6f, 0.65f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.33f, 0.35f);

            var luz = new GameObject("Luz").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.3f;
            luz.shadows = LightShadows.Soft;
            luz.color = new Color(1f, 0.96f, 0.88f);
            luz.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            // Cámara
            var camGo = new GameObject("Camara");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.45f, 0.75f);
            cam.fieldOfView = 45f;
            camGo.AddComponent<AudioListener>();
            var camaraGrupo = camGo.AddComponent<CamaraGrupo>();
            camaraGrupo.ColocarAlInstante();

            // Plató: una plataforma flotante. Si te caes por el borde, reapareces.
            var plato = new GameObject("Plato").transform;
            Bloque("Suelo", plato, new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 30f), m.suelo);
            Bloque("BordeFrente", plato, new Vector3(0f, -0.45f, -15.1f), new Vector3(30.4f, 1.1f, 0.2f), m.borde);
            Bloque("BordeFondo", plato, new Vector3(0f, -0.45f, 15.1f), new Vector3(30.4f, 1.1f, 0.2f), m.borde);
            Bloque("BordeIzq", plato, new Vector3(-15.1f, -0.45f, 0f), new Vector3(0.2f, 1.1f, 30f), m.borde);
            Bloque("BordeDer", plato, new Vector3(15.1f, -0.45f, 0f), new Vector3(0.2f, 1.1f, 30f), m.borde);

            // Rampa y tarima para probar saltos y caídas
            var rampa = Bloque("Rampa", plato, new Vector3(8f, 0.8f, 2.5f), new Vector3(4f, 0.3f, 6f), m.obstaculo);
            rampa.transform.rotation = Quaternion.Euler(-18f, 0f, 0f);
            Bloque("Tarima", plato, new Vector3(8f, 0.9f, 7.4f), new Vector3(4f, 1.8f, 4f), m.obstaculo);
            Bloque("Escalon1", plato, new Vector3(-9f, 0.25f, -6f), new Vector3(3f, 0.5f, 3f), m.obstaculo);
            Bloque("Escalon2", plato, new Vector3(-9f, 0.6f, -9f), new Vector3(3f, 1.2f, 3f), m.obstaculo);

            // Cosas para agarrar y empujar
            var objetos = new GameObject("Objetos").transform;
            var posicionesCajas = new[]
            {
                new Vector3(-3f, 0.4f, 3f), new Vector3(-1.5f, 0.4f, 4f), new Vector3(0f, 0.4f, 3f),
                new Vector3(1.5f, 0.4f, 4f), new Vector3(3f, 0.4f, 3f), new Vector3(0f, 1.2f, 3.5f),
            };
            for (int i = 0; i < posicionesCajas.Length; i++)
            {
                var caja = Bloque($"Caja{i + 1}", objetos, posicionesCajas[i], Vector3.one * 0.8f, m.caja);
                caja.AddComponent<Rigidbody>().mass = 2f;
            }

            var pelota = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pelota.name = "PelotaGigante";
            pelota.transform.SetParent(objetos, false);
            pelota.transform.position = new Vector3(5f, 1f, -5f);
            pelota.transform.localScale = Vector3.one * 1.8f;
            pelota.GetComponent<Renderer>().sharedMaterial = m.pelota;
            var rbPelota = pelota.AddComponent<Rigidbody>();
            rbPelota.mass = 3f;
            rbPelota.angularDamping = 0.3f;

            // Trampa: barra giratoria
            var barra = new GameObject("BarraGiratoria");
            barra.transform.SetParent(plato, false);
            barra.transform.position = new Vector3(-8f, 0f, 6f);
            barra.AddComponent<Rigidbody>().isKinematic = true;
            barra.AddComponent<BarraGiratoria>().velocidadGiro = 80f;
            var eje = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            eje.name = "Eje";
            eje.transform.SetParent(barra.transform, false);
            eje.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            eje.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            eje.GetComponent<Renderer>().sharedMaterial = m.borde;
            Bloque("Brazo", barra.transform, new Vector3(0f, 0.55f, 0f), new Vector3(9f, 0.35f, 0.35f), m.obstaculo, local: true);

            // Jugadores: PlayerInputManager + gestor + puntos de aparición
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
            var puntos = new List<Transform>();
            float[] xs = { -3f, -1f, 1f, 3f };
            for (int i = 0; i < xs.Length; i++)
            {
                var punto = new GameObject($"Aparicion{i + 1}").transform;
                punto.SetParent(jugadoresGo.transform, false);
                punto.position = new Vector3(xs[i], 0.2f, -5f);
                puntos.Add(punto);
            }
            gestor.puntosAparicion = puntos.ToArray();

            // Guardar y añadir a la lista de escenas del build
            Ambientes.Aplicar(Ambientes.Tema.Barrio);
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AnadirAlBuild(RutaEscena);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = jugadoresGo;
        }

        internal static GameObject Bloque(string nombre, Transform padre, Vector3 posicion, Vector3 escala,
                                 Material material, bool local = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            if (local) go.transform.localPosition = posicion;
            else go.transform.position = posicion;
            go.transform.localScale = escala;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------

        internal static void CrearCarpeta(string padre, string nombre)
        {
            if (!AssetDatabase.IsValidFolder($"{padre}/{nombre}"))
                AssetDatabase.CreateFolder(padre, nombre);
        }

        internal static void AnadirAlBuild(string ruta)
        {
            var escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (escenas.Exists(e => e.path == ruta)) return;
            escenas.Insert(0, new EditorBuildSettingsScene(ruta, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }
    }
}
