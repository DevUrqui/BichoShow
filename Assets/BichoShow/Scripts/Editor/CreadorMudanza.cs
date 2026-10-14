using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Menú "BichoShow > Crear escena: Mudanza frágil".
    ///
    /// Recorrido (de atrás hacia delante, eje +Z):
    ///   Salida con camiones → Barredora y conos → El río (puente estrecho o plataformas que se
    ///   inclinan) → Calle de los empujadores y badenes → Casas con puerta giratoria.
    /// Todo con primitivas: es el prototipo para ver si divierte.
    /// </summary>
    public static class CreadorMudanza
    {
        const string CarpetaObjetos = E.CarpetaPrefabs + "/Objetos";
        const string RutaEscena = E.CarpetaEscenas + "/Mudanza.unity";

        static readonly Color ColorRojo = new Color(1f, 0.38f, 0.35f);
        static readonly Color ColorAzul = new Color(0.3f, 0.6f, 1f);

        class Mats
        {
            public E.Materiales basicos;
            public Material agua, madera, camion, rueda, cono, peligro, linea, cristal, marcador,
                tele, pantalla, jarron, tallo, flor, sofa, cojin, particula,
                rojo, azul, paredRoja, paredAzul;
        }

        [MenuItem("BichoShow/Crear escena: Mudanza frágil")]
        public static void Crear()
        {
            var controles = AssetDatabase.LoadAssetAtPath<InputActionAsset>(E.RutaControles);
            if (controles == null)
            {
                EditorUtility.DisplayDialog("Bicho Show", $"No encuentro los controles en:\n{E.RutaControles}", "Vale");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) != null &&
                !EditorUtility.DisplayDialog("Bicho Show",
                    "La escena de la Mudanza ya existe. ¿Rehacerla? Se perderán los cambios hechos en ella (el prefab del bicho no se toca).",
                    "Rehacer", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            E.CrearCarpeta(E.Raiz, "Materiales");
            E.CrearCarpeta(E.Raiz, "Prefabs");
            E.CrearCarpeta(E.Raiz, "Escenas");
            E.CrearCarpeta(E.CarpetaPrefabs, "Objetos");

            var m = CrearMateriales();

            // El bicho se reutiliza tal cual (con los ajustes que hayas hecho en el Inspector).
            var bicho = AssetDatabase.LoadAssetAtPath<GameObject>(E.RutaPrefabBicho)
                        ?? E.CrearPrefabBicho(controles, m.basicos);

            var objetos = new[]
            {
                CrearTele(m),
                CrearJarron(m),
                CrearSofa(m),
            };

            CrearEscena(bicho, objetos, m);
            Debug.Log("Bicho Show: escena de la Mudanza creada. Uníos con un botón y pulsad START (o ENTER) para empezar.");
        }

        // ------------------------------------------------------------------
        // Materiales
        // ------------------------------------------------------------------

        static Mats CrearMateriales()
        {
            var m = new Mats
            {
                basicos = E.CrearMateriales(),
                agua = E.CrearMaterial("Agua", new Color(0.25f, 0.55f, 0.95f), 0.9f),
                madera = E.CrearMaterial("Madera", new Color(0.62f, 0.43f, 0.26f), 0.15f),
                camion = E.CrearMaterial("Camion", new Color(0.95f, 0.95f, 0.92f), 0.4f),
                rueda = E.CrearMaterial("Rueda", new Color(0.12f, 0.12f, 0.14f), 0.2f),
                cono = E.CrearMaterial("Cono", new Color(1f, 0.5f, 0.1f), 0.3f),
                peligro = E.CrearMaterial("Peligro", new Color(1f, 0.82f, 0.15f), 0.3f),
                linea = E.CrearMaterial("Linea", Color.white, 0.1f),
                cristal = E.CrearMaterial("Cristal", new Color(0.7f, 0.9f, 1f), 0.95f),
                marcador = E.CrearMaterial("Marcador", Color.white, 0.6f),
                tele = E.CrearMaterial("Tele", new Color(0.22f, 0.22f, 0.25f), 0.4f),
                pantalla = E.CrearMaterial("Pantalla", new Color(0.45f, 0.78f, 0.98f), 0.95f),
                jarron = E.CrearMaterial("Jarron", new Color(0.25f, 0.72f, 0.72f), 0.85f),
                tallo = E.CrearMaterial("Tallo", new Color(0.3f, 0.7f, 0.25f), 0.2f),
                flor = E.CrearMaterial("Flor", new Color(1f, 0.45f, 0.7f), 0.4f),
                sofa = E.CrearMaterial("Sofa", new Color(0.55f, 0.35f, 0.78f), 0.2f),
                cojin = E.CrearMaterial("Cojin", new Color(0.75f, 0.6f, 0.92f), 0.2f),
                rojo = E.CrearMaterial("EquipoRojo", ColorRojo, 0.3f),
                azul = E.CrearMaterial("EquipoAzul", ColorAzul, 0.3f),
                paredRoja = E.CrearMaterial("ParedRoja", Color.Lerp(ColorRojo, Color.white, 0.55f), 0.1f),
                paredAzul = E.CrearMaterial("ParedAzul", Color.Lerp(ColorAzul, Color.white, 0.55f), 0.1f),
            };

            // Material para partículas (confeti y salpicaduras): sin luz, colores de vértice.
            string ruta = $"{E.CarpetaMateriales}/Particula.mat";
            m.particula = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m.particula == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
                m.particula = new Material(shader);
                AssetDatabase.CreateAsset(m.particula, ruta);
            }
            return m;
        }

        // ------------------------------------------------------------------
        // Objetos frágiles
        // ------------------------------------------------------------------

        static ObjetoFragil CrearTele(Mats m)
        {
            var raiz = RaizObjeto("Tele", masa: 7f, out var modelo);
            var col = raiz.AddComponent<BoxCollider>();
            col.size = new Vector3(1.3f, 1f, 0.7f);
            col.center = new Vector3(0f, 0.5f, 0f);

            E.Pieza(PrimitiveType.Cube, "Carcasa", modelo, new Vector3(0f, 0.5f, 0f), new Vector3(1.3f, 1f, 0.7f), m.tele);
            E.Pieza(PrimitiveType.Cube, "Pantalla", modelo, new Vector3(0f, 0.52f, -0.36f), new Vector3(1.05f, 0.75f, 0.04f), m.pantalla);
            var antena1 = E.Pieza(PrimitiveType.Cylinder, "Antena", modelo, new Vector3(-0.15f, 1.25f, 0f), new Vector3(0.03f, 0.3f, 0.03f), m.tele);
            antena1.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            var antena2 = E.Pieza(PrimitiveType.Cylinder, "Antena", modelo, new Vector3(0.15f, 1.25f, 0f), new Vector3(0.03f, 0.3f, 0.03f), m.tele);
            antena2.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);

            return GuardarObjeto(raiz, modelo, m, "la tele", puntos: 100, fragilidad: 1f, alturaMarcador: 2f);
        }

        static ObjetoFragil CrearJarron(Mats m)
        {
            var raiz = RaizObjeto("Jarron", masa: 4f, out var modelo);
            var col = raiz.AddComponent<CapsuleCollider>();
            col.radius = 0.38f;
            col.height = 1.2f;
            col.center = new Vector3(0f, 0.6f, 0f);

            E.Pieza(PrimitiveType.Cylinder, "Base", modelo, new Vector3(0f, 0.06f, 0f), new Vector3(0.45f, 0.06f, 0.45f), m.jarron);
            E.Pieza(PrimitiveType.Sphere, "Panza", modelo, new Vector3(0f, 0.48f, 0f), new Vector3(0.76f, 0.82f, 0.76f), m.jarron);
            E.Pieza(PrimitiveType.Cylinder, "Cuello", modelo, new Vector3(0f, 0.98f, 0f), new Vector3(0.3f, 0.2f, 0.3f), m.jarron);
            E.Pieza(PrimitiveType.Cylinder, "Boca", modelo, new Vector3(0f, 1.18f, 0f), new Vector3(0.42f, 0.04f, 0.42f), m.jarron);
            E.Pieza(PrimitiveType.Cylinder, "Tallo", modelo, new Vector3(0f, 1.38f, 0f), new Vector3(0.05f, 0.25f, 0.05f), m.tallo);
            E.Pieza(PrimitiveType.Sphere, "Flor", modelo, new Vector3(0f, 1.62f, 0f), Vector3.one * 0.3f, m.flor);

            return GuardarObjeto(raiz, modelo, m, "el jarrón", puntos: 120, fragilidad: 1.6f, alturaMarcador: 2.3f);
        }

        static ObjetoFragil CrearSofa(Mats m)
        {
            var raiz = RaizObjeto("Sofa", masa: 12f, out var modelo);
            var col = raiz.AddComponent<BoxCollider>();
            col.size = new Vector3(2.2f, 1.1f, 1f);
            col.center = new Vector3(0f, 0.55f, 0f);

            E.Pieza(PrimitiveType.Cube, "Asiento", modelo, new Vector3(0f, 0.3f, 0f), new Vector3(2.2f, 0.5f, 1f), m.sofa);
            E.Pieza(PrimitiveType.Cube, "Respaldo", modelo, new Vector3(0f, 0.85f, 0.375f), new Vector3(2.2f, 0.7f, 0.25f), m.sofa);
            E.Pieza(PrimitiveType.Cube, "BrazoIzq", modelo, new Vector3(-0.975f, 0.65f, 0f), new Vector3(0.25f, 0.45f, 1f), m.sofa);
            E.Pieza(PrimitiveType.Cube, "BrazoDer", modelo, new Vector3(0.975f, 0.65f, 0f), new Vector3(0.25f, 0.45f, 1f), m.sofa);
            E.Pieza(PrimitiveType.Cube, "CojinIzq", modelo, new Vector3(-0.42f, 0.6f, -0.08f), new Vector3(0.82f, 0.14f, 0.72f), m.cojin);
            E.Pieza(PrimitiveType.Cube, "CojinDer", modelo, new Vector3(0.42f, 0.6f, -0.08f), new Vector3(0.82f, 0.14f, 0.72f), m.cojin);
            foreach (var x in new[] { -0.95f, 0.95f })
            foreach (var z in new[] { -0.4f, 0.4f })
                E.Pieza(PrimitiveType.Cube, "Pata", modelo, new Vector3(x, 0.03f, z), new Vector3(0.12f, 0.08f, 0.12f), m.rueda);

            return GuardarObjeto(raiz, modelo, m, "el sofá", puntos: 150, fragilidad: 0.6f, alturaMarcador: 1.9f);
        }

        static GameObject RaizObjeto(string nombre, float masa, out Transform modelo)
        {
            var raiz = new GameObject(nombre);
            var rb = raiz.AddComponent<Rigidbody>();
            rb.mass = masa;
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.6f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            modelo = new GameObject("Modelo").transform;
            modelo.SetParent(raiz.transform, false);
            return raiz;
        }

        static ObjetoFragil GuardarObjeto(GameObject raiz, Transform modelo, Mats m, string nombreObjeto,
                                          int puntos, float fragilidad, float alturaMarcador)
        {
            var marcador = E.Pieza(PrimitiveType.Cube, "Marcador", raiz.transform,
                new Vector3(0f, alturaMarcador, 0f), Vector3.one * 0.3f, m.marcador);

            var fragil = raiz.AddComponent<ObjetoFragil>();
            fragil.nombreObjeto = nombreObjeto;
            fragil.puntosBase = puntos;
            fragil.fragilidad = fragilidad;
            fragil.modelo = modelo;
            fragil.renderersDano = modelo.GetComponentsInChildren<Renderer>();
            fragil.marcador = marcador.GetComponent<Renderer>();
            fragil.alturaMarcador = alturaMarcador;

            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, $"{CarpetaObjetos}/{raiz.name}.prefab");
            Object.DestroyImmediate(raiz);
            return prefab.GetComponent<ObjetoFragil>();
        }

        // ------------------------------------------------------------------
        // Escena
        // ------------------------------------------------------------------

        static void CrearEscena(GameObject prefabBicho, ObjetoFragil[] objetos, Mats m)
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var b = m.basicos;

            // --- Luz, cámara y ambiente de plató ---
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.78f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.6f, 0.68f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.32f, 0.38f);

            var luz = new GameObject("Luz").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.3f;
            luz.shadows = LightShadows.Soft;
            luz.color = new Color(1f, 0.96f, 0.88f);
            luz.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

            var camGo = new GameObject("Camara");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.5f, 0.38f, 0.72f);
            cam.fieldOfView = 45f;
            cam.farClipPlane = 300f;
            camGo.AddComponent<AudioListener>();
            var camara = camGo.AddComponent<CamaraGrupo>();
            camara.direccion = new Vector3(0f, 1.25f, -1f);
            camara.distanciaMinima = 13f;
            camara.distanciaMaxima = 45f;
            camara.centroSinJugadores = new Vector3(0f, 0f, -27f);
            camara.ColocarAlInstante();

            var mapa = new GameObject("Mapa").transform;

            // --- Suelos (arriba en y=0, 3 m de grosor para que parezcan orillas) ---
            E.Bloque("SueloSalida", mapa, new Vector3(0f, -1.5f, -27f), new Vector3(20f, 3f, 22f), b.suelo);
            E.Bloque("SueloPrincipal", mapa, new Vector3(0f, -1.5f, 13f), new Vector3(20f, 3f, 42f), b.suelo);
            for (float z = -6f; z <= 22f; z += 2.5f)
                Decorado(PrimitiveType.Cube, "Linea", mapa, new Vector3(0f, 0.01f, z), new Vector3(0.25f, 0.02f, 1.2f), m.linea);

            CrearSalida(mapa, m);
            CrearBarredora(mapa, m);
            var zonasMortales = CrearRio(mapa, m);
            CrearEmpujadores(mapa, m);
            var casaRoja = CrearCasa(mapa, m, 0, -5f, "CASA ROJA", m.rojo, m.paredRoja, ColorRojo, 35f);
            var casaAzul = CrearCasa(mapa, m, 1, 5f, "CASA AZUL", m.azul, m.paredAzul, ColorAzul, -35f);

            Cartel(mapa, "MUDANZA FRÁGIL", new Vector3(0f, 9f, 40f), 3f, Color.white);
            Cartel(mapa, "SALIDA", new Vector3(0f, 5f, -39f), 1.5f, Color.white);

            // --- Jugadores ---
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
            gestor.mostrarAyuda = false; // la ronda dibuja su propia pantalla
            // Jugadores 1 y 3 = equipo rojo (rojo y naranja); 2 y 4 = equipo azul (azul y celeste).
            gestor.colores = new[]
            {
                ColorRojo,
                ColorAzul,
                new Color(1f, 0.68f, 0.2f),
                new Color(0.45f, 0.9f, 0.95f),
            };
            Vector3[] apariciones =
            {
                new Vector3(-6.6f, 0.2f, -28.5f), new Vector3(3.4f, 0.2f, -28.5f),
                new Vector3(-3.4f, 0.2f, -28.5f), new Vector3(6.6f, 0.2f, -28.5f),
            };
            var puntos = new List<Transform>();
            for (int i = 0; i < apariciones.Length; i++)
            {
                var p = new GameObject($"Aparicion{i + 1}").transform;
                p.SetParent(jugadoresGo.transform, false);
                p.position = apariciones[i];
                puntos.Add(p);
            }
            gestor.puntosAparicion = puntos.ToArray();

            // --- Ronda ---
            var rondaGo = new GameObject("RondaMudanza");
            var ronda = rondaGo.AddComponent<RondaMudanza>();
            ronda.secuenciaObjetos = objetos;
            ronda.gestor = gestor;
            ronda.equipos = new[]
            {
                new RondaMudanza.Equipo { nombre = "Rojo", color = ColorRojo, casa = casaRoja, aparicionObjeto = PuntoObjeto(rondaGo.transform, "ObjetoRojo", -5f), rutaBots = RutaBots(rondaGo.transform, "RutaRojo", -1f) },
                new RondaMudanza.Equipo { nombre = "Azul", color = ColorAzul, casa = casaAzul, aparicionObjeto = PuntoObjeto(rondaGo.transform, "ObjetoAzul", 5f), rutaBots = RutaBots(rondaGo.transform, "RutaAzul", 1f) },
            };

            EditorSceneManager.SaveScene(escena, RutaEscena);
            E.AnadirAlBuild(RutaEscena);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rondaGo;
        }

        /// <summary>
        /// Ruta de los bots para un equipo (lado = -1 rojo, +1 azul). Siempre avanza en +Z:
        /// salida → su plataforma basculante (por el centro, donde menos se inclina) →
        /// calle de empujadores (cerca del centro, donde casi no llegan) → puerta → alfombra.
        /// </summary>
        static Transform[] RutaBots(Transform padre, string nombre, float lado)
        {
            Vector2[] puntos =
            {
                new Vector2(5f, -24f), new Vector2(6f, -17.3f), new Vector2(6f, -12f), new Vector2(6f, -6.8f),
                new Vector2(2.5f, -3f), new Vector2(2.5f, 6f), new Vector2(2.5f, 16f),
                new Vector2(5f, 21f), new Vector2(5f, 28.6f),
            };
            var grupo = new GameObject(nombre).transform;
            grupo.SetParent(padre, false);
            var ruta = new Transform[puntos.Length];
            for (int i = 0; i < puntos.Length; i++)
            {
                ruta[i] = new GameObject($"Punto{i + 1}").transform;
                ruta[i].SetParent(grupo, false);
                ruta[i].position = new Vector3(puntos[i].x * lado, 0f, puntos[i].y);
            }
            return ruta;
        }

        static Transform PuntoObjeto(Transform padre, string nombre, float x)
        {
            var p = new GameObject(nombre).transform;
            p.SetParent(padre, false);
            p.position = new Vector3(x, 2.2f, -30.3f);
            return p;
        }

        // --- Zona 1: salida con camiones y conos ---
        static void CrearSalida(Transform mapa, Mats m)
        {
            var salida = Grupo(mapa, "Salida");
            CrearCamion(salida, -5f, m.rojo, m);
            CrearCamion(salida, 5f, m.azul, m);

            Vector3[] conos =
            {
                new Vector3(-7f, 0f, -24f), new Vector3(-2.5f, 0f, -25f), new Vector3(2.5f, 0f, -25f), new Vector3(7f, 0f, -24f),
                new Vector3(-5f, 0f, -21f), new Vector3(0f, 0f, -23.5f), new Vector3(5f, 0f, -21f),
                new Vector3(-8f, 0f, -18f), new Vector3(-1.5f, 0f, -18f), new Vector3(8f, 0f, -18f),
            };
            foreach (var p in conos)
            {
                var cono = E.Pieza(PrimitiveType.Cylinder, "Cono", salida, p + Vector3.up * 0.35f, new Vector3(0.45f, 0.35f, 0.45f), m.cono, conCollider: true);
                cono.AddComponent<Rigidbody>().mass = 0.5f;
            }
        }

        static void CrearCamion(Transform padre, float x, Material colorEquipo, Mats m)
        {
            var camion = Grupo(padre, colorEquipo == m.rojo ? "CamionRojo" : "CamionAzul");
            E.Bloque("Caja", camion, new Vector3(x, 1.6f, -34.5f), new Vector3(3.4f, 3f, 5f), m.camion);
            Decorado(PrimitiveType.Cube, "Franja", camion, new Vector3(x, 1.9f, -34.5f), new Vector3(3.45f, 0.5f, 5.05f), colorEquipo);
            Decorado(PrimitiveType.Cube, "PuertaAbierta", camion, new Vector3(x, 1.6f, -31.98f), new Vector3(2.8f, 2.5f, 0.05f), m.rueda);
            E.Bloque("Cabina", camion, new Vector3(x, 1.2f, -38f), new Vector3(3.2f, 2.2f, 1.8f), colorEquipo);
            Decorado(PrimitiveType.Cube, "Parabrisas", camion, new Vector3(x, 1.7f, -38.92f), new Vector3(2.6f, 0.8f, 0.05f), m.cristal);
            foreach (float z in new[] { -38f, -36f, -33f })
            foreach (float lado in new[] { -1f, 1f })
            {
                var rueda = Decorado(PrimitiveType.Cylinder, "Rueda", camion, new Vector3(x + 1.75f * lado, 0.45f, z), new Vector3(0.9f, 0.15f, 0.9f), m.rueda);
                rueda.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }
        }

        // --- Zona 2: barredora gigante ---
        static void CrearBarredora(Transform mapa, Mats m)
        {
            var barredora = new GameObject("Barredora");
            barredora.transform.SetParent(mapa, false);
            barredora.transform.position = new Vector3(0f, 0f, -20f);
            barredora.AddComponent<Rigidbody>().isKinematic = true;
            barredora.AddComponent<BarraGiratoria>().velocidadGiro = 65f;
            E.Pieza(PrimitiveType.Cylinder, "Eje", barredora.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.8f, 0.6f, 0.8f), m.peligro, conCollider: true);
            E.Bloque("Brazo", barredora.transform, new Vector3(0f, 0.55f, 0f), new Vector3(14f, 0.4f, 0.4f), m.basicos.obstaculo, local: true);
        }

        // --- Zona 3: el río ---
        static ZonaMortal[] CrearRio(Transform mapa, Mats m)
        {
            var rio = Grupo(mapa, "Rio");
            Decorado(PrimitiveType.Cube, "Agua", rio, new Vector3(0f, -3f, -12f), new Vector3(26f, 0.3f, 8f), m.agua);

            // Puente central: estrecho y fijo. El camino seguro… por el que van todos.
            E.Bloque("PuenteCentral", rio, new Vector3(0f, -0.2f, -12f), new Vector3(1.8f, 0.4f, 8.1f), m.madera);

            // Plataformas basculantes a los lados: anchas, pero se inclinan según dónde piséis.
            foreach (float x in new[] { -6f, 6f })
            {
                var plataforma = E.Bloque(x < 0 ? "BasculanteIzq" : "BasculanteDer", rio, new Vector3(x, -0.2f, -12f), new Vector3(5f, 0.4f, 7.9f), m.madera);
                Decorado(PrimitiveType.Cube, "BordeAviso", plataforma.transform, new Vector3(-0.47f, 0.52f, 0f), new Vector3(0.04f, 0.05f, 1f), m.peligro, local: true);
                Decorado(PrimitiveType.Cube, "BordeAviso", plataforma.transform, new Vector3(0.47f, 0.52f, 0f), new Vector3(0.04f, 0.05f, 1f), m.peligro, local: true);
                var tronco = Decorado(PrimitiveType.Cylinder, "Eje", rio, new Vector3(x, -0.6f, -12f), new Vector3(0.5f, 4f, 0.5f), m.rueda);
                tronco.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var rb = plataforma.AddComponent<Rigidbody>();
                rb.mass = 25f;
                rb.angularDamping = 1f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                var bisagra = plataforma.AddComponent<HingeJoint>();
                bisagra.axis = Vector3.forward;
                bisagra.anchor = Vector3.zero;
                bisagra.useLimits = true;
                bisagra.limits = new JointLimits { min = -20f, max = 20f };
                bisagra.useSpring = true;
                bisagra.spring = new JointSpring { spring = 400f, damper = 60f, targetPosition = 0f };
            }

            var salpicadura = Particulas("Salpicadura", rio, new Vector3(0f, -3f, -12f),
                new Color(0.75f, 0.9f, 1f), new Color(0.35f, 0.65f, 1f), 50, 3f, 6f, m.particula);

            var zonaRio = ZonaTrigger<ZonaMortal>("ZonaRio", rio, new Vector3(0f, -4.5f, -12f), new Vector3(30f, 2f, 8f));
            zonaRio.motivoObjeto = "se ha caído al río";
            zonaRio.salpicadura = salpicadura;
            zonaRio.alturaSuperficie = -2.85f;

            var vacio = ZonaTrigger<ZonaMortal>("ZonaVacio", mapa, new Vector3(0f, -14f, 0f), new Vector3(140f, 4f, 180f));
            vacio.motivoObjeto = "se ha caído al vacío";
            return new[] { zonaRio, vacio };
        }

        // --- Zona 4: calle de los empujadores y badenes ---
        static void CrearEmpujadores(Transform mapa, Mats m)
        {
            var calle = Grupo(mapa, "CalleEmpujadores");

            foreach (float z in new[] { -4f, 15f })
            {
                var baden = E.Pieza(PrimitiveType.Cylinder, "Baden", calle, new Vector3(0f, 0f, z), new Vector3(0.7f, 10f, 0.7f), m.peligro, conCollider: true);
                baden.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }

            // (z, lado del que sale, desfase)
            (float z, float lado, float desfase)[] empujadores = { (1f, -1f, 0f), (5.5f, 1f, 1.3f), (10f, -1f, 2.6f) };
            foreach (var e in empujadores)
            {
                var go = E.Bloque(e.lado < 0 ? "EmpujadorIzq" : "EmpujadorDer", calle,
                    new Vector3(9f * e.lado, 0.7f, e.z), new Vector3(2f, 1.4f, 2.5f), m.peligro);
                go.AddComponent<Rigidbody>().isKinematic = true;
                var empujador = go.AddComponent<Empujador>();
                empujador.recorrido = new Vector3(-5f * e.lado, 0f, 0f);
                empujador.periodo = 4f;
                empujador.desfase = e.desfase;
                empujador.cuerpoVisible = go.GetComponent<Renderer>();
                // Carcasa detrás, para que se vea de dónde sale.
                E.Bloque("Carcasa", calle, new Vector3(10.4f * e.lado, 0.9f, e.z), new Vector3(0.8f, 1.8f, 3f), m.rueda);
            }
        }

        // --- Zona 5: casa de cada equipo con puerta giratoria ---
        static ZonaEntrega CrearCasa(Transform mapa, Mats m, int equipo, float cx, string titulo,
                                     Material colorEquipo, Material pared, Color color, float giroPuerta)
        {
            var casa = Grupo(mapa, equipo == 0 ? "CasaRoja" : "CasaAzul");
            const float frente = 24f, fondo = 32f, alto = 3.2f;
            float centroZ = (frente + fondo) * 0.5f;

            E.Bloque("ParedFondo", casa, new Vector3(cx, alto / 2f, fondo), new Vector3(8f, alto, 0.3f), pared);
            E.Bloque("ParedIzq", casa, new Vector3(cx - 4f, alto / 2f, centroZ), new Vector3(0.3f, alto, 8f), pared);
            E.Bloque("ParedDer", casa, new Vector3(cx + 4f, alto / 2f, centroZ), new Vector3(0.3f, alto, 8f), pared);
            E.Bloque("FrenteIzq", casa, new Vector3(cx - 3.2f, alto / 2f, frente), new Vector3(1.6f, alto, 0.3f), pared);
            E.Bloque("FrenteDer", casa, new Vector3(cx + 3.2f, alto / 2f, frente), new Vector3(1.6f, alto, 0.3f), pared);
            E.Bloque("Dintel", casa, new Vector3(cx, 2.9f, frente), new Vector3(4.8f, 0.6f, 0.3f), pared);
            E.Bloque("Letrero", casa, new Vector3(cx, 4.1f, frente), new Vector3(6f, 1.2f, 0.25f), colorEquipo);
            Cartel(casa, titulo, new Vector3(cx, 4.1f, frente - 0.2f), 0.75f, Color.white);

            // Puerta giratoria: cuatro hojas de cristal que no paran.
            var puerta = new GameObject("PuertaGiratoria");
            puerta.transform.SetParent(casa, false);
            puerta.transform.position = new Vector3(cx, 0f, frente);
            puerta.AddComponent<Rigidbody>().isKinematic = true;
            puerta.AddComponent<BarraGiratoria>().velocidadGiro = giroPuerta;
            E.Pieza(PrimitiveType.Cylinder, "Columna", puerta.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.35f, 1.25f, 0.35f), m.rueda, conCollider: true);
            for (int i = 0; i < 4; i++)
            {
                var rot = Quaternion.Euler(0f, 90f * i, 0f);
                var hoja = E.Pieza(PrimitiveType.Cube, "Hoja", puerta.transform, rot * new Vector3(1.15f, 1.2f, 0f), new Vector3(2.05f, 2.3f, 0.12f), m.cristal, conCollider: true);
                hoja.transform.localRotation = rot;
            }

            // Zona de entrega dentro de la casa
            Decorado(PrimitiveType.Cube, "Alfombra", casa, new Vector3(cx, 0.02f, 28.6f), new Vector3(6f, 0.04f, 5f), colorEquipo);
            var zona = ZonaTrigger<ZonaEntrega>("ZonaEntrega", casa, new Vector3(cx, 1f, 28.6f), new Vector3(6.5f, 2f, 5.5f));
            zona.equipo = equipo;
            zona.confeti = Particulas("Confeti", casa, new Vector3(cx, 0.3f, 28.6f),
                color, new Color(1f, 0.92f, 0.3f), 140, 5f, 10f, m.particula);
            return zona;
        }

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------

        static Transform Grupo(Transform padre, string nombre)
        {
            var t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        /// <summary>Pieza solo visual (sin collider).</summary>
        static GameObject Decorado(PrimitiveType tipo, string nombre, Transform padre, Vector3 posicion, Vector3 escala,
                                   Material material, bool local = false)
        {
            if (material == null) return null;
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            if (local) go.transform.localPosition = posicion;
            else go.transform.position = posicion;
            go.transform.localScale = escala;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static T ZonaTrigger<T>(string nombre, Transform padre, Vector3 posicion, Vector3 tamano) where T : Component
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = posicion;
            var caja = go.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.size = tamano;
            return go.AddComponent<T>();
        }

        static void Cartel(Transform padre, string texto, Vector3 posicion, float altura, Color color)
        {
            var go = new GameObject($"Cartel {texto}");
            go.transform.SetParent(padre, false);
            go.transform.position = posicion;
            var tm = go.AddComponent<TextMesh>();
            var fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = fuente;
            tm.text = texto;
            tm.fontSize = 64;
            tm.characterSize = altura * 10f / tm.fontSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;
            if (fuente != null) go.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
        }

        static ParticleSystem Particulas(string nombre, Transform padre, Vector3 posicion, Color colorA, Color colorB,
                                         short cantidad, float velocidadMin, float velocidadMax, Material material)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = posicion;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // el cono apunta hacia arriba

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(velocidadMin, velocidadMax);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = 0.9f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;

            var emision = ps.emission;
            emision.rateOverTime = 0f;
            emision.SetBursts(new[] { new ParticleSystem.Burst(0f, cantidad) });

            var forma = ps.shape;
            forma.shapeType = ParticleSystemShapeType.Cone;
            forma.angle = 35f;
            forma.radius = 0.6f;

            var rotacion = ps.rotationOverLifetime;
            rotacion.enabled = true;
            rotacion.z = new ParticleSystem.MinMaxCurve(-4f, 4f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            return ps;
        }
    }
}
