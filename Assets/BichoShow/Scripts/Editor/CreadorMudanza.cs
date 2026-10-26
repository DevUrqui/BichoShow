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
    ///   inclinan) → Calle de los empujadores y badenes → Almacén con cintas que van hacia atrás →
    ///   Patio de los ventiladores → Casas con puerta giratoria (grande, lenta y acolchada).
    /// Todo con primitivas: es el prototipo para ver si divierte.
    /// </summary>
    public static class CreadorMudanza
    {
        const string CarpetaObjetos = E.CarpetaPrefabs + "/Objetos";
        public const string RutaEscena = E.CarpetaEscenas + "/Mudanza.unity";
        /// <summary>Súbelo al cambiar el recorrido: la escena guardada se marca como anticuada.</summary>
        public const int Version = 2;

        // Las casas: centro de cada una (en X, a cada lado), su fachada y la alfombra de entrega.
        const float XCasa = 6.2f, FrenteCasa = 54f, ZAlfombra = 60.2f;

        static readonly Color ColorRojo = new Color(1f, 0.38f, 0.35f);
        static readonly Color ColorAzul = new Color(0.3f, 0.6f, 1f);

        class Mats
        {
            public E.Materiales basicos;
            public Material agua, madera, camion, rueda, sabotaje, cono, peligro, linea, cristal, marcador,
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
                sabotaje = E.CrearMaterial("Sabotaje", new Color(0.62f, 0.3f, 0.9f), 0.5f),
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

            KitObjetos.Poner("Tele", modelo, Vector3.zero, local: true);

            return GuardarObjeto(raiz, modelo, m, "la tele", puntos: 100, fragilidad: 1f, alturaMarcador: 2f);
        }

        static ObjetoFragil CrearJarron(Mats m)
        {
            var raiz = RaizObjeto("Jarron", masa: 4f, out var modelo);
            var col = raiz.AddComponent<CapsuleCollider>();
            col.radius = 0.38f;
            col.height = 1.2f;
            col.center = new Vector3(0f, 0.6f, 0f);

            KitObjetos.Poner("Jarron", modelo, Vector3.zero, local: true);

            return GuardarObjeto(raiz, modelo, m, "el jarrón", puntos: 120, fragilidad: 1.6f, alturaMarcador: 2.3f);
        }

        static ObjetoFragil CrearSofa(Mats m)
        {
            var raiz = RaizObjeto("Sofa", masa: 12f, out var modelo);
            var col = raiz.AddComponent<BoxCollider>();
            col.size = new Vector3(2.2f, 1.1f, 1f);
            col.center = new Vector3(0f, 0.55f, 0f);

            KitObjetos.Poner("Sofa", modelo, Vector3.zero, local: true);

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

            string ruta = $"{CarpetaObjetos}/{raiz.name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            Object.DestroyImmediate(raiz);
            // Se vuelve a cargar del disco: la referencia que devuelve SaveAsPrefabAsset puede quedar
            // anulada cuando Unity reimporta el prefab (y la ronda se quedaba sin objetos que sacar).
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(ruta).GetComponent<ObjetoFragil>();
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
            camara.distanciaMaxima = 58f; // el recorrido es largo: un equipo puede ir muy por delante
            camara.centroSinJugadores = new Vector3(0f, 0f, -27f);
            camara.ColocarAlInstante();

            var mapa = new GameObject("Mapa").transform;
            new GameObject(EscenasAnticuadas.Marca + Version);

            // --- Suelos (arriba en y=0, 3 m de grosor para que parezcan orillas) ---
            E.Bloque("SueloSalida", mapa, new Vector3(0f, -1.5f, -27f), new Vector3(20f, 3f, 22f), b.suelo);
            E.Bloque("SueloPrincipal", mapa, new Vector3(0f, -1.5f, 19f), new Vector3(20f, 3f, 54f), b.suelo);
            E.Bloque("SueloCasas", mapa, new Vector3(0f, -1.5f, 56f), new Vector3(28f, 3f, 20f), b.suelo);
            for (float z = -6f; z <= 15f; z += 2.5f)
                Decorado(PrimitiveType.Cube, "Linea", mapa, new Vector3(0f, 0.01f, z), new Vector3(0.25f, 0.02f, 1.2f), m.linea);

            CrearSalida(mapa, m);
            var zonaSaboteadores = CrearCabinaSaboteador(mapa, m);
            CrearBarredora(mapa, m);
            var zonasMortales = CrearRio(mapa, m);
            CrearEmpujadores(mapa, m);
            CrearAlmacen(mapa, m);
            CrearPatio(mapa, m);
            var casaRoja = CrearCasa(mapa, m, 0, -XCasa, "CASA ROJA", m.rojo, m.paredRoja, ColorRojo, 24f);
            var casaAzul = CrearCasa(mapa, m, 1, XCasa, "CASA AZUL", m.azul, m.paredAzul, ColorAzul, -24f);

            CrearDecorado(mapa);
            Cartel(mapa, "MUDANZA FRÁGIL", new Vector3(0f, 9f, 70f), 3f, Color.white);
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
            ronda.zonaSaboteadores = zonaSaboteadores;
            ronda.equipos = new[]
            {
                new RondaMudanza.Equipo { nombre = "Rojo", color = ColorRojo, casa = casaRoja, aparicionObjeto = PuntoObjeto(rondaGo.transform, "ObjetoRojo", -5f), rutaBots = RutaBots(rondaGo.transform, "RutaRojo", -1f) },
                new RondaMudanza.Equipo { nombre = "Azul", color = ColorAzul, casa = casaAzul, aparicionObjeto = PuntoObjeto(rondaGo.transform, "ObjetoAzul", 5f), rutaBots = RutaBots(rondaGo.transform, "RutaAzul", 1f) },
            };

            Ambientes.Aplicar(Ambientes.Tema.Barrio);
            EditorSceneManager.SaveScene(escena, RutaEscena);
            E.AnadirAlBuild(RutaEscena);
            AssetDatabase.SaveAssets();

            // Abrir la escena tal como ha quedado guardada, para que al darle a Play las referencias
            // a los prefabs sean las del disco y no las que había en memoria al generarla.
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            var rondaGuardada = Object.FindAnyObjectByType<RondaMudanza>();
            if (rondaGuardada != null) Selection.activeGameObject = rondaGuardada.gameObject;
        }

        /// <summary>
        /// Ruta de los bots para un equipo (lado = -1 rojo, +1 azul). Siempre avanza en +Z:
        /// salida → la barredora por fuera de su brazo (en un lado barre hacia atrás) → su plataforma basculante (por el centro, donde menos se inclina) →
        /// calle de empujadores (cerca del centro, donde casi no llegan) → almacén y patio (por su
        /// carril, entre las pilas de cajas) → la puerta por el lado en el que las hojas van hacia
        /// dentro → alfombra.
        /// </summary>
        static Transform[] RutaBots(Transform padre, string nombre, float lado)
        {
            Vector2[] puntos =
            {
                new Vector2(7.8f, -25f), new Vector2(7.8f, -17.6f), new Vector2(6f, -14f), new Vector2(6f, -12f), new Vector2(6f, -6.8f),
                new Vector2(2.5f, -3f), new Vector2(2.5f, 6f), new Vector2(2.5f, 15f),
                new Vector2(3.5f, 19f), new Vector2(3.5f, 31f), new Vector2(3.5f, 45f),
                new Vector2(XCasa + 1.8f, 50f), new Vector2(XCasa + 1.8f, 57f), new Vector2(XCasa, ZAlfombra),
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
                // Raíz con física; el dibujo es la pieza del kit.
                var cono = new GameObject("Cono");
                cono.transform.SetParent(salida, false);
                cono.transform.position = p;
                var capsula = cono.AddComponent<CapsuleCollider>();
                capsula.radius = 0.2f;
                capsula.height = 0.7f;
                capsula.center = new Vector3(0f, 0.35f, 0f);
                cono.AddComponent<Rigidbody>().mass = 0.5f;
                KitObjetos.Poner("Cono", cono.transform, Vector3.zero, Random.Range(0f, 360f), local: true);
            }

            // Cajas de cartón de la mudanza, apiladas junto a los camiones
            foreach (float lado in new[] { -1f, 1f })
            {
                float x = 8.3f * lado;
                Caja(salida, new Vector3(x, 0f, -35.2f), 8f);
                Caja(salida, new Vector3(x + 0.1f * lado, 0f, -34.3f), -12f);
                Caja(salida, new Vector3(x, 0.7f, -34.8f), 30f);
            }
        }

        static void Caja(Transform padre, Vector3 posicion, float giro)
        {
            var caja = KitObjetos.Poner("CajaCarton", padre, posicion, giro);
            var col = caja.AddComponent<BoxCollider>();
            col.size = new Vector3(0.72f, 0.7f, 0.72f);
            col.center = new Vector3(0f, 0.35f, 0f);
        }

        static void CrearCamion(Transform padre, float x, Material colorEquipo, Mats m)
        {
            var camion = Grupo(padre, colorEquipo == m.rojo ? "CamionRojo" : "CamionAzul");
            // Colisiones invisibles con las mismas medidas de siempre (caja de carga y cabina)…
            Colision(camion, "ColisionCaja", new Vector3(x, 1.6f, -34.5f), new Vector3(3.4f, 3f, 5f));
            Colision(camion, "ColisionCabina", new Vector3(x, 1.2f, -38f), new Vector3(3.2f, 2.2f, 1.8f));
            // …y el camión de Blender, con la franja y la cabina del color del equipo.
            KitObjetos.Poner("Camion", camion, new Vector3(x, 0f, -34.5f), 0f, colorEquipo);
        }

        static void Colision(Transform padre, string nombre, Vector3 centro, Vector3 tamano)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = centro;
            go.AddComponent<BoxCollider>().size = tamano;
        }

        // --- Decorado: farolas, buzones, islas con árboles y edificios de fondo (solo para la vista) ---
        static void CrearDecorado(Transform mapa)
        {
            var decorado = Grupo(mapa, "Decorado");
            var cesped = E.CrearMaterial("Cesped", new Color(0.45f, 0.75f, 0.38f), 0.1f);
            var tierra = E.CrearMaterial("IslaTierra", new Color(0.62f, 0.5f, 0.42f), 0.1f);

            // Farolas en los bordes, con el brazo hacia el recorrido (lejos de los empujadores).
            foreach (float z in new[] { -36f, -28.5f, -6f, 18f, 32.5f })
            foreach (float lado in new[] { -1f, 1f })
            {
                var farola = KitObjetos.Poner("Farola", decorado, new Vector3(9.5f * lado, 0f, z), lado < 0 ? 90f : -90f);
                var col = farola.AddComponent<CapsuleCollider>();
                col.radius = 0.15f;
                col.height = 3.6f;
                col.center = new Vector3(0f, 1.8f, 0f);
            }

            // Un buzón junto a la puerta de cada casa.
            foreach (float cx in new[] { -XCasa, XCasa })
            {
                var buzon = KitObjetos.Poner("Buzon", decorado, new Vector3(cx + 4.7f * Mathf.Sign(cx), 0f, FrenteCasa - 1.3f), 180f);
                var col = buzon.AddComponent<BoxCollider>();
                col.size = new Vector3(0.45f, 1.6f, 0.6f);
                col.center = new Vector3(0f, 0.8f, 0f);
            }

            // Islas flotantes con árboles, arbustos y algún banco, a los lados del recorrido.
            float[] zs = { -34f, -19f, -3f, 12f, 27f, 41f, 56f };
            for (int i = 0; i < zs.Length; i++)
            foreach (float lado in new[] { -1f, 1f })
            {
                float z = zs[i] + (lado < 0 ? 0f : 4f);
                var centro = new Vector3((z > 44f ? 20.5f : 16.5f) * lado, 0f, z); // junto a las casas el suelo es más ancho
                E.Bloque("Isla", decorado, centro + new Vector3(0f, -2.6f, 0f), new Vector3(6f, 4f, 6f), tierra);
                E.Bloque("Cesped", decorado, centro + new Vector3(0f, -0.65f, 0f), new Vector3(6.2f, 0.3f, 6.2f), cesped);
                KitObjetos.Poner("Arbol", decorado, centro + new Vector3(0.8f * lado, -0.5f, -0.6f), i * 47f, escala: 1f + 0.15f * (i % 3));
                KitObjetos.Poner("Arbusto", decorado, centro + new Vector3(-1.4f * lado, -0.5f, 1.4f), i * 31f);
                if (i % 2 == 0)
                    KitObjetos.Poner("Banco", decorado, centro + new Vector3(-1.2f * lado, -0.5f, -1.6f), lado < 0 ? 90f : -90f);
            }

            // Edificios de fondo sobre islas grandes, lejos, entre la niebla.
            string[] edificios = { "EdificioA", "EdificioB", "EdificioC" };
            (Vector3 pos, float giro)[] fondo =
            {
                (new Vector3(-34f, 0f, -28f), 0f), (new Vector3(-36f, 0f, -2f), 90f), (new Vector3(-33f, 0f, 22f), 180f),
                (new Vector3(34f, 0f, -24f), 270f), (new Vector3(37f, 0f, 2f), 0f), (new Vector3(33f, 0f, 26f), 90f),
                (new Vector3(-35f, 0f, 48f), 270f), (new Vector3(36f, 0f, 52f), 180f),
                (new Vector3(-16f, 0f, 84f), 0f), (new Vector3(2f, 0f, 88f), 90f), (new Vector3(19f, 0f, 83f), 180f),
            };
            for (int i = 0; i < fondo.Length; i++)
            {
                var (pos, giro) = fondo[i];
                E.Bloque("IslaGrande", decorado, pos + new Vector3(0f, -5f, 0f), new Vector3(11f, 6f, 11f), tierra);
                E.Bloque("CespedGrande", decorado, pos + new Vector3(0f, -1.85f, 0f), new Vector3(11.2f, 0.3f, 11.2f), cesped);
                KitObjetos.Poner(edificios[i % edificios.Length], decorado, pos + new Vector3(0f, -1.7f, 0f), giro, escala: 1f + 0.12f * (i % 2));
            }
        }

        // --- Zona 2: barredora gigante ---
        static void CrearBarredora(Transform mapa, Mats m)
        {
            var barredora = new GameObject("Barredora");
            barredora.transform.SetParent(mapa, false);
            barredora.transform.position = new Vector3(0f, 0f, -20f);
            barredora.AddComponent<Rigidbody>().isKinematic = true;
            var giro = barredora.AddComponent<BarraGiratoria>();
            giro.velocidadGiro = 75f;
            E.Pieza(PrimitiveType.Cylinder, "Eje", barredora.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.8f, 0.6f, 0.8f), m.peligro, conCollider: true);
            var brazo = E.Bloque("Brazo", barredora.transform, new Vector3(0f, 0.55f, 0f), new Vector3(14f, 0.4f, 0.4f), m.basicos.obstaculo, local: true);

            giro.nombreTrampa = "Barredora";
            giro.enfriamiento = 8f;
            giro.radioEfecto = 8f;
            giro.alturaMarcador = 3.5f;
            giro.renderersAviso = new[] { brazo.GetComponent<Renderer>() };
        }

        // --- Plataforma de saboteadores, entre los camiones ---
        static Collider CrearCabinaSaboteador(Transform mapa, Mats m)
        {
            var cabina = Grupo(mapa, "CabinaSaboteador");
            Decorado(PrimitiveType.Cube, "Plataforma", cabina, new Vector3(0f, 0.03f, -34.5f), new Vector3(3.2f, 0.06f, 3.2f), m.sabotaje);
            Decorado(PrimitiveType.Cube, "Poste", cabina, new Vector3(0f, 1.3f, -36.2f), new Vector3(0.15f, 2.6f, 0.15f), m.rueda);
            Decorado(PrimitiveType.Cube, "Cartel", cabina, new Vector3(0f, 2.9f, -36.2f), new Vector3(3.4f, 0.8f, 0.12f), m.sabotaje);
            Cartel(cabina, "SABOTEADOR", new Vector3(0f, 2.9f, -36.3f), 0.5f, Color.white);

            var zona = new GameObject("ZonaSaboteadores");
            zona.transform.SetParent(cabina, false);
            zona.transform.position = new Vector3(0f, 1.5f, -34.5f);
            var caja = zona.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.size = new Vector3(3.2f, 3f, 3.2f);
            return caja;
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

                var basculante = plataforma.AddComponent<PlataformaBasculante>();
                basculante.nombreTrampa = x < 0 ? "Plataforma izquierda" : "Plataforma derecha";
                basculante.enfriamiento = 7f;
                basculante.radioEfecto = 5f;
                basculante.alturaMarcador = 2.5f;
                basculante.renderersAviso = new[] { plataforma.GetComponent<Renderer>() };
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

            foreach (float z in new[] { -4f, 16.5f })
            {
                var baden = E.Pieza(PrimitiveType.Cylinder, "Baden", calle, new Vector3(0f, 0f, z), new Vector3(0.7f, 10f, 0.7f), m.peligro, conCollider: true);
                baden.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }

            // (z, lado del que sale, desfase)
            (float z, float lado, float desfase)[] empujadores = { (0f, -1f, 0f), (4.5f, 1f, 1f), (9f, -1f, 2f), (13.5f, 1f, 3f) };
            int numero = 0;
            foreach (var e in empujadores)
            {
                numero++;
                var go = E.Bloque(e.lado < 0 ? "EmpujadorIzq" : "EmpujadorDer", calle,
                    new Vector3(9f * e.lado, 0.7f, e.z), new Vector3(2f, 1.4f, 2.5f), m.peligro);
                go.AddComponent<Rigidbody>().isKinematic = true;
                var empujador = go.AddComponent<Empujador>();
                empujador.recorrido = new Vector3(-5f * e.lado, 0f, 0f);
                empujador.periodo = 4f;
                empujador.desfase = e.desfase;
                empujador.cuerpoVisible = go.GetComponent<Renderer>();
                empujador.nombreTrampa = $"Empujador {numero}";
                empujador.enfriamiento = 4f;
                empujador.radioEfecto = 4.5f;
                empujador.alturaMarcador = 2.5f;
                // Carcasa detrás, para que se vea de dónde sale.
                E.Bloque("Carcasa", calle, new Vector3(10.4f * e.lado, 0.9f, e.z), new Vector3(0.8f, 1.8f, 3f), m.rueda);
            }
        }

        // --- Zona 5: almacén con cintas que van hacia atrás y pilas de cajas en medio ---
        static void CrearAlmacen(Transform mapa, Mats m)
        {
            var almacen = Grupo(mapa, "Almacen");
            foreach (float lado in new[] { -1f, 1f })
            {
                Cinta(almacen, m, 4.5f * lado, 7f, 19f, 31f);
                PilaCajas(almacen, new Vector3(6.8f * lado, 0f, 23f), 15f * lado);
                PilaCajas(almacen, new Vector3(8.9f * lado, 0f, 29.5f), -10f * lado);
            }
            PilaCajas(almacen, new Vector3(0f, 0f, 27.5f), 30f);
            Cartel(almacen, "ALMACÉN", new Vector3(0f, 3.2f, 31.5f), 0.9f, Color.white);
        }

        /// <summary>Cinta transportadora a ras de suelo: lleva hacia atrás (-Z) todo lo que la pisa.</summary>
        static void Cinta(Transform padre, Mats m, float x, float ancho, float z0, float z1)
        {
            float largo = z1 - z0;
            var go = new GameObject("Cinta");
            go.transform.SetParent(padre, false);
            go.transform.position = new Vector3(x, 0f, (z0 + z1) * 0.5f);
            Decorado(PrimitiveType.Cube, "Banda", go.transform, new Vector3(0f, 0.012f, 0f), new Vector3(ancho, 0.02f, largo), m.rueda, local: true);
            foreach (float lado in new[] { -1f, 1f })
                Decorado(PrimitiveType.Cube, "Borde", go.transform, new Vector3(lado * (ancho * 0.5f + 0.1f), 0.03f, 0f), new Vector3(0.2f, 0.06f, largo), m.peligro, local: true);

            // Rayas y flechas que se mueven con la cinta (se ve hacia dónde te lleva).
            var marcas = new List<Transform>();
            for (float z = -largo * 0.5f + 0.5f; z < largo * 0.5f; z += 1f)
                marcas.Add(Decorado(PrimitiveType.Cube, "Raya", go.transform, new Vector3(0f, 0.026f, z), new Vector3(ancho - 0.5f, 0.01f, 0.12f), m.linea, local: true).transform);
            for (int i = 0; i < 3; i++)
                marcas.Add(KitObjetos.Poner("Flecha", go.transform, new Vector3(0f, 0.03f, -largo * 0.5f + largo * (i + 0.5f) / 3f), 180f,
                                            m.peligro, 1.4f, true, KitObjetos.KitCostura).transform);

            var cinta = go.AddComponent<CintaTransportadora>();
            cinta.direccion = Vector3.back;
            cinta.velocidad = 1.8f;
            cinta.tamano = new Vector2(ancho, largo);
            cinta.marcas = marcas.ToArray();
        }

        /// <summary>Pila fija de cajas de cartón (una sola colisión, para no llenar la cinta de piezas).</summary>
        static void PilaCajas(Transform padre, Vector3 posicion, float giro)
        {
            var pila = new GameObject("PilaCajas");
            pila.transform.SetParent(padre, false);
            pila.transform.SetPositionAndRotation(posicion, Quaternion.Euler(0f, giro, 0f));
            var col = pila.AddComponent<BoxCollider>();
            col.size = new Vector3(1.5f, 2.1f, 1.5f);
            col.center = new Vector3(0f, 1.05f, 0f);
            (Vector3 p, float g)[] cajas =
            {
                (new Vector3(-0.38f, 0f, -0.38f), 4f), (new Vector3(0.38f, 0f, -0.36f), -6f),
                (new Vector3(-0.36f, 0f, 0.38f), 10f), (new Vector3(0.38f, 0f, 0.4f), -3f),
                (new Vector3(-0.2f, 0.7f, 0f), 20f), (new Vector3(0.25f, 0.7f, 0.1f), -8f), (new Vector3(0.05f, 1.4f, 0.05f), -15f),
            };
            foreach (var c in cajas) KitObjetos.Poner("CajaCarton", pila.transform, c.p, c.g, local: true);
        }

        // --- Zona 6: patio de los ventiladores: ráfagas de lado y huecos en la valla ---
        static void CrearPatio(Transform mapa, Mats m)
        {
            var patio = Grupo(mapa, "PatioVentiladores");
            const float z0 = 34f, z1 = 46f;
            // (z, lado en el que está): sopla hacia el otro lado, donde la valla tiene un hueco.
            (float z, float lado, float desfase)[] ventiladores = { (37f, -1f, 0f), (43f, 1f, 0.5f) };
            foreach (var v in ventiladores)
            {
                float giro = v.lado < 0 ? 90f : -90f; // mira (y sopla) hacia dentro
                E.Bloque("Peana", patio, new Vector3(10.9f * v.lado, -1.5f, v.z), new Vector3(1.8f, 3f, 2.6f), m.rueda);
                var ventilador = KitObjetos.Poner("Ventilador", patio, new Vector3(10.9f * v.lado, 0f, v.z), giro, m.peligro, 1.2f, false, KitObjetos.KitGirasol);
                var col = ventilador.AddComponent<BoxCollider>();
                col.size = new Vector3(1.4f, 3.4f, 1f);
                col.center = new Vector3(0f, 1.7f, 0f);
                var aspas = KitObjetos.Poner("AspasVentilador", ventilador.transform, new Vector3(0f, 2.8f, 0.2f), 0f, m.peligro, 1f, true, KitObjetos.KitGirasol);

                var viento = new GameObject($"Viento z{v.z:0}").AddComponent<VientoLateral>();
                viento.transform.SetParent(patio, false);
                viento.transform.SetPositionAndRotation(new Vector3(10f * v.lado, 0f, v.z), Quaternion.LookRotation(new Vector3(-v.lado, 0f, 0f)));
                viento.tamanoZona = new Vector3(5.5f, 4f, 21f);
                viento.periodo = 5f;
                viento.duracion = 1.6f;
                viento.desfase = v.desfase;
                viento.fuerza = 15f;
                viento.fuerzaObjetos = 9f;
                viento.aspas = aspas.transform;

                // Flechas en el suelo: hacia dónde sopla.
                foreach (float x in new[] { -4f, 4f })
                    KitObjetos.Poner("Flecha", patio, new Vector3(x, 0.02f, v.z), giro, m.linea, 1.6f, false, KitObjetos.KitCostura);

                // Valla baja: entera del lado del ventilador; con un hueco enfrente (por ahí se sale volando).
                Valla(patio, m, v.lado, z0, z1);
                Valla(patio, m, -v.lado, z0, v.z - 1.7f);
                Valla(patio, m, -v.lado, v.z + 1.7f, z1);
                Decorado(PrimitiveType.Cube, "AvisoHueco", patio, new Vector3(9.7f * -v.lado, 0.015f, v.z), new Vector3(0.6f, 0.03f, 3.4f), m.peligro);
            }
        }

        static void Valla(Transform padre, Mats m, float lado, float z0, float z1)
        {
            if (z1 - z0 < 0.5f) return;
            E.Bloque("Valla", padre, new Vector3(9.85f * lado, 0.35f, (z0 + z1) * 0.5f), new Vector3(0.3f, 0.7f, z1 - z0), m.madera);
        }

        // --- Zona 7: casa de cada equipo con puerta giratoria ---
        static ZonaEntrega CrearCasa(Transform mapa, Mats m, int equipo, float cx, string titulo,
                                     Material colorEquipo, Material pared, Color color, float giroPuerta)
        {
            var casa = Grupo(mapa, equipo == 0 ? "CasaRoja" : "CasaAzul");
            const float frente = FrenteCasa, fondo = FrenteCasa + 9f, alto = 3.6f, medio = 6f, hueco = 3.4f;
            float centroZ = (frente + fondo) * 0.5f;
            float anchoFrente = medio - hueco;

            E.Bloque("ParedFondo", casa, new Vector3(cx, alto / 2f, fondo), new Vector3(medio * 2f, alto, 0.3f), pared);
            E.Bloque("ParedIzq", casa, new Vector3(cx - medio, alto / 2f, centroZ), new Vector3(0.3f, alto, fondo - frente), pared);
            E.Bloque("ParedDer", casa, new Vector3(cx + medio, alto / 2f, centroZ), new Vector3(0.3f, alto, fondo - frente), pared);
            E.Bloque("FrenteIzq", casa, new Vector3(cx - hueco - anchoFrente / 2f, alto / 2f, frente), new Vector3(anchoFrente, alto, 0.3f), pared);
            E.Bloque("FrenteDer", casa, new Vector3(cx + hueco + anchoFrente / 2f, alto / 2f, frente), new Vector3(anchoFrente, alto, 0.3f), pared);
            E.Bloque("Dintel", casa, new Vector3(cx, alto - 0.3f, frente), new Vector3(hueco * 2f, 0.6f, 0.3f), pared);
            E.Bloque("Letrero", casa, new Vector3(cx, alto + 0.9f, frente), new Vector3(7f, 1.2f, 0.25f), colorEquipo);
            Cartel(casa, titulo, new Vector3(cx, alto + 0.9f, frente - 0.2f), 0.8f, Color.white);

            // Puerta giratoria: tres hojas de cristal grandes y lentas, con un rulo blando en la punta.
            // Sus golpes hacen poco daño (Acolchado): lo difícil es meterse a tiempo en un hueco.
            var puerta = new GameObject("PuertaGiratoria");
            puerta.transform.SetParent(casa, false);
            puerta.transform.position = new Vector3(cx, 0f, frente);
            puerta.AddComponent<Rigidbody>().isKinematic = true;
            var giro = puerta.AddComponent<BarraGiratoria>();
            giro.velocidadGiro = giroPuerta;
            giro.multiplicadorTurbo = -3f;
            giro.nombreTrampa = equipo == 0 ? "Puerta giratoria roja" : "Puerta giratoria azul";
            giro.enfriamiento = 7f;
            giro.radioEfecto = 5f;
            giro.alturaMarcador = alto + 2.3f;
            puerta.AddComponent<Acolchado>().factorDano = 0.35f;
            const float radioColumna = 0.3f, largoHoja = 2.9f;
            E.Pieza(PrimitiveType.Cylinder, "Columna", puerta.transform, new Vector3(0f, 1.35f, 0f), new Vector3(radioColumna * 2f, 1.35f, radioColumna * 2f), m.rueda, conCollider: true);
            var hojas = new Renderer[3];
            for (int i = 0; i < 3; i++)
            {
                var rot = Quaternion.Euler(0f, 120f * i, 0f);
                var hoja = E.Pieza(PrimitiveType.Cube, "Hoja", puerta.transform, rot * new Vector3(radioColumna + largoHoja / 2f, 1.35f, 0f), new Vector3(largoHoja, 2.6f, 0.12f), m.cristal, conCollider: true);
                hoja.transform.localRotation = rot;
                hojas[i] = hoja.GetComponent<Renderer>();
                E.Pieza(PrimitiveType.Cylinder, "Rulo", puerta.transform, rot * new Vector3(radioColumna + largoHoja, 1.35f, 0f), new Vector3(0.36f, 1.3f, 0.36f), colorEquipo, conCollider: true);
            }
            giro.renderersAviso = hojas;

            // Zona de entrega dentro de la casa
            Decorado(PrimitiveType.Cube, "Alfombra", casa, new Vector3(cx, 0.02f, ZAlfombra), new Vector3(9f, 0.04f, 5f), colorEquipo);
            var zona = ZonaTrigger<ZonaEntrega>("ZonaEntrega", casa, new Vector3(cx, 1f, ZAlfombra), new Vector3(10f, 2f, 5.2f));
            zona.equipo = equipo;
            zona.confeti = Particulas("Confeti", casa, new Vector3(cx, 0.3f, ZAlfombra),
                color, new Color(1f, 0.92f, 0.3f), 140, 5f, 10f, m.particula);
            return zona;
        }

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------

        internal static Transform Grupo(Transform padre, string nombre)
        {
            var t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        /// <summary>Pieza solo visual (sin collider).</summary>
        internal static GameObject Decorado(PrimitiveType tipo, string nombre, Transform padre, Vector3 posicion, Vector3 escala,
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
            MallasRedondeadas.Redondear(go);
            return go;
        }

        internal static T ZonaTrigger<T>(string nombre, Transform padre, Vector3 posicion, Vector3 tamano) where T : Component
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = posicion;
            var caja = go.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.size = tamano;
            return go.AddComponent<T>();
        }

        internal static void Cartel(Transform padre, string texto, Vector3 posicion, float altura, Color color)
        {
            var go = new GameObject($"Cartel {texto}");
            go.transform.SetParent(padre, false);
            go.transform.position = posicion;
            var tm = go.AddComponent<TextMesh>();
            // La fuente del show (Lilita One); si no está, la de Unity.
            var fuente = Resources.Load<Font>("Fuentes/LilitaOne") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        internal static ParticleSystem Particulas(string nombre, Transform padre, Vector3 posicion, Color colorA, Color colorB,
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
            RecursosEfectos.AplicarA(ps); // confeti, espuma, gotas, sal o polen: su textura de Blender
            return ps;
        }
    }
}
