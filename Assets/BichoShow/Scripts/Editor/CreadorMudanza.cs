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
        public const int Version = 4;

        // Las casas: centro de cada una (en X, a cada lado), su fachada y la alfombra de entrega.
        const float XCasa = 6.2f, FrenteCasa = 54f, ZAlfombra = 60.2f;

        static readonly Color ColorRojo = new Color(1f, 0.38f, 0.35f);
        static readonly Color ColorAzul = new Color(0.3f, 0.6f, 1f);

        class Mats
        {
            public E.Materiales basicos;
            public Material agua, madera, camion, rueda, sabotaje, cono, peligro, linea, cristal, marcador,
                tele, pantalla, jarron, tallo, flor, sofa, cojin, particula,
                rojo, azul, paredRoja, paredAzul,
                tierra, pavimento, asfalto, acera, bordillo, hormigon, baldosaA, baldosaB, cesped, camino, parquet;
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
                // Suelos: cada zona con el suyo (la tierra es lo que se ve por los lados de las losas).
                tierra = E.CrearMaterial("IslaTierra", new Color(0.62f, 0.5f, 0.42f), 0.1f),
                pavimento = E.CrearMaterial("MudanzaPavimento", new Color(0.84f, 0.8f, 0.74f), 0.1f),
                asfalto = E.CrearMaterial("MudanzaAsfalto", new Color(0.4f, 0.42f, 0.5f), 0.15f),
                acera = E.CrearMaterial("MudanzaAcera", new Color(0.9f, 0.85f, 0.76f), 0.1f),
                bordillo = E.CrearMaterial("MudanzaBordillo", new Color(0.97f, 0.96f, 0.93f), 0.1f),
                hormigon = E.CrearMaterial("MudanzaHormigon", new Color(0.72f, 0.74f, 0.77f), 0.1f),
                baldosaA = E.CrearMaterial("MudanzaBaldosaA", new Color(0.96f, 0.9f, 0.8f), 0.2f),
                baldosaB = E.CrearMaterial("MudanzaBaldosaB", new Color(0.9f, 0.66f, 0.54f), 0.2f),
                cesped = E.CrearMaterial("Cesped", new Color(0.45f, 0.75f, 0.38f), 0.1f),
                camino = E.CrearMaterial("MudanzaCamino", new Color(0.88f, 0.8f, 0.66f), 0.1f),
                parquet = E.CrearMaterial("MudanzaParquet", new Color(0.8f, 0.6f, 0.42f), 0.25f),
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

            return GuardarObjeto(raiz, modelo, m, "la tele", puntos: 100, fragilidad: 1f, alturaMarcador: 2f, portadores: 2);
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

            return GuardarObjeto(raiz, modelo, m, "el sofá", puntos: 150, fragilidad: 0.6f, alturaMarcador: 1.9f, portadores: 2);
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
                                          int puntos, float fragilidad, float alturaMarcador, int portadores = 1)
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
            fragil.portadoresNecesarios = portadores; // la tele y el sofá pesan: entre dos

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
            E.Bloque("SueloSalida", mapa, new Vector3(0f, -1.5f, -27f), new Vector3(20f, 3f, 22f), m.tierra);
            E.Bloque("SueloPrincipal", mapa, new Vector3(0f, -1.5f, 19f), new Vector3(20f, 3f, 54f), m.tierra);
            E.Bloque("SueloCasas", mapa, new Vector3(0f, -1.5f, 56f), new Vector3(28f, 3f, 20f), m.tierra);
            CrearSuelos(mapa, m);

            CrearSalida(mapa, m);
            var zonaSaboteadores = CrearCabinaSaboteador(mapa, m);
            CrearBarredora(mapa, m);
            var zonasMortales = CrearRio(mapa, m);
            CrearEmpujadores(mapa, m);
            var placasCinta = CrearAlmacen(mapa, m);
            CrearPatio(mapa, m);
            var casaRoja = CrearCasa(mapa, m, 0, -XCasa, "CASA ROJA", m.rojo, m.paredRoja, ColorRojo, 24f, out var frenoRojo);
            var casaAzul = CrearCasa(mapa, m, 1, XCasa, "CASA AZUL", m.azul, m.paredAzul, ColorAzul, -24f, out var frenoAzul);
            CrearPlato(mapa, m);

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
                new RondaMudanza.Equipo { nombre = "Rojo", color = ColorRojo, casa = casaRoja, aparicionObjeto = PuntoObjeto(rondaGo.transform, "ObjetoRojo", -5f), rutaBots = RutaBots(rondaGo.transform, "RutaRojo", -1f), ayudas = Ayudas(placasCinta[0], frenoRojo) },
                new RondaMudanza.Equipo { nombre = "Azul", color = ColorAzul, casa = casaAzul, aparicionObjeto = PuntoObjeto(rondaGo.transform, "ObjetoAzul", 5f), rutaBots = RutaBots(rondaGo.transform, "RutaAzul", 1f), ayudas = Ayudas(placasCinta[1], frenoAzul) },
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

        /// <summary>Las placas de un equipo y el tramo en el que sirven (los bots las pisan ahí).</summary>
        static RondaMudanza.Ayuda[] Ayudas(PlacaPresion cinta, PlacaPresion puerta) => new[]
        {
            new RondaMudanza.Ayuda { placa = cinta, desdeZ = 17f, hastaZ = 31f },
            new RondaMudanza.Ayuda { placa = puerta, desdeZ = 45f, hastaZ = FrenteCasa + 3f },
        };

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
            float[] zs = { -36f, -16.5f, 0.5f, 17.5f, 34.5f, 57f }; // entre las islas de las gradas
            for (int i = 0; i < zs.Length; i++)
            foreach (float lado in new[] { -1f, 1f })
            {
                float z = zs[i];
                var centro = new Vector3((z > 44f ? 20.5f : 16.5f) * lado, 0f, z); // junto a las casas el suelo es más ancho
                E.Bloque("Isla", decorado, centro + new Vector3(0f, -2.6f, 0f), new Vector3(6f, 4f, 6f), tierra);
                E.Bloque("Cesped", decorado, centro + new Vector3(0f, -0.65f, 0f), new Vector3(6.2f, 0.3f, 6.2f), cesped);
                KitObjetos.Poner("Arbol", decorado, centro + new Vector3(0.8f * lado, -0.5f, -0.6f), i * 47f, escala: 1f + 0.15f * (i % 3));
                KitObjetos.Poner("Arbusto", decorado, centro + new Vector3(-1.4f * lado, -0.5f, 1.4f), i * 31f);
                KitObjetos.Poner("TorreFocos", decorado, centro + new Vector3(-1.6f * lado, -0.5f, 1.4f), -lado * 90f, kit: KitObjetos.KitPlato);
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

        /// <summary>
        /// El plató, como en las demás pruebas: gradas con público en islas a los dos lados, la
        /// pantalla gigante detrás de las casas, cámaras de tele y cañones de confeti en el jardín.
        /// </summary>
        static void CrearPlato(Transform mapa, Mats m)
        {
            var plato = Grupo(mapa, "Plato");
            var gradas = Grupo(plato, "Gradas");
            gradas.gameObject.AddComponent<PublicoAnimado>();

            Color[] coloresPublico =
            {
                new Color(1f, 0.45f, 0.45f), new Color(0.4f, 0.65f, 1f), new Color(1f, 0.8f, 0.3f),
                new Color(0.5f, 0.9f, 0.5f), new Color(0.85f, 0.55f, 1f), new Color(1f, 0.6f, 0.85f),
            };
            var materialesPublico = new Material[coloresPublico.Length];
            for (int i = 0; i < coloresPublico.Length; i++)
                materialesPublico[i] = E.CrearMaterial($"Publico{i}", coloresPublico[i], 0.3f);

            var azar = new System.Random(11);
            foreach (float lado in new[] { -1f, 1f })
            foreach (float z in new[] { -25f, -8f, 9f, 26f, 43f })
            {
                var isla = new Vector3(18.2f * lado, 0f, z);
                E.Bloque("IslaGrada", plato, isla + new Vector3(0f, -2.6f, 0f), new Vector3(6.6f, 4f, 10f), m.tierra);
                E.Bloque("CespedGrada", plato, isla + new Vector3(0f, -0.65f, 0f), new Vector3(6.8f, 0.3f, 10.2f), m.cesped);
                var grada = KitObjetos.Poner("Grada", gradas, new Vector3(17.6f * lado, -0.5f, z), -lado * 90f, kit: KitObjetos.KitPlato);

                // Público sentado en las cuatro filas (asientos en local: y = 0.5·(i+1)+0.1, z = 1.35 − 1.1·i).
                for (int fila = 0; fila < 4; fila++)
                for (int s = 0; s < 5; s++)
                {
                    if (azar.NextDouble() < 0.15) continue; // algún hueco libre
                    float px = -3.2f + 1.6f * s + (float)(azar.NextDouble() - 0.5) * 0.5f;
                    string pieza = azar.NextDouble() < 0.3 ? "PublicoBrazos" : "Publico";
                    KitObjetos.Poner(pieza, grada.transform, new Vector3(px, 0.5f * (fila + 1) + 0.1f, 1.35f - 1.1f * fila),
                        (float)(azar.NextDouble() - 0.5) * 30f, materialesPublico[azar.Next(materialesPublico.Length)],
                        1f + (float)(azar.NextDouble() - 0.5) * 0.2f, true, KitObjetos.KitPlato);
                }
            }

            // Pantalla gigante detrás de las casas (el cartel de "MUDANZA FRÁGIL" va delante).
            E.Bloque("IslaPantalla", plato, new Vector3(0f, -3f, 76.5f), new Vector3(20f, 6f, 5f), m.tierra);
            E.Bloque("CespedPantalla", plato, new Vector3(0f, -0.15f, 76.5f), new Vector3(20.2f, 0.3f, 5.2f), m.cesped);
            KitObjetos.Poner("Pantalla", plato, new Vector3(0f, 6f, 76.5f), 180f, kit: KitObjetos.KitPlato);

            // En el jardín: una cámara de tele enfocando cada puerta y cañones de confeti a los lados.
            foreach (float lado in new[] { -1f, 1f })
            {
                var pos = new Vector3(12.8f * lado, 0f, 47.2f);
                KitObjetos.Poner("CamaraTV", plato, pos, KitObjetos.GiroMirando(pos, new Vector3(XCasa * lado, 0f, FrenteCasa)), kit: KitObjetos.KitPlato);
                KitObjetos.Poner("CanonConfeti", plato, new Vector3(13.3f * lado, 0f, FrenteCasa - 0.8f), 180f - lado * 25f, kit: KitObjetos.KitPlato);
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
            // Colisiones de siempre (invisibles) y encima el brazo a rayas y el eje con baliza, de Blender.
            Ocultar(E.Pieza(PrimitiveType.Cylinder, "Eje", barredora.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.8f, 0.6f, 0.8f), m.peligro, conCollider: true));
            Ocultar(E.Bloque("Brazo", barredora.transform, new Vector3(0f, 0.55f, 0f), new Vector3(14f, 0.4f, 0.4f), m.basicos.obstaculo, local: true));
            KitObjetos.Poner("BarredoraEje", barredora.transform, Vector3.zero, local: true);
            var brazo = KitObjetos.Poner("BarredoraBrazo", barredora.transform, Vector3.zero, local: true);

            giro.nombreTrampa = "Barredora";
            giro.enfriamiento = 8f;
            giro.radioEfecto = 8f;
            giro.alturaMarcador = 3.5f;
            giro.renderersAviso = brazo.GetComponentsInChildren<Renderer>();
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
            Ocultar(E.Bloque("PuenteCentral", rio, new Vector3(0f, -0.2f, -12f), new Vector3(1.8f, 0.4f, 8.1f), m.madera));
            KitObjetos.Poner("PuenteTablas", rio, new Vector3(0f, -0.4f, -12f));

            // Plataformas basculantes a los lados: anchas, pero se inclinan según dónde piséis.
            // (Balsas de tablas con los bordes de aviso; la raíz no tiene escala para que el dibujo no se deforme.)
            foreach (float x in new[] { -6f, 6f })
            {
                var plataforma = new GameObject(x < 0 ? "BasculanteIzq" : "BasculanteDer");
                plataforma.transform.SetParent(rio, false);
                plataforma.transform.position = new Vector3(x, -0.2f, -12f);
                plataforma.AddComponent<BoxCollider>().size = new Vector3(5f, 0.4f, 7.9f);
                var balsa = KitObjetos.Poner("BalsaTablas", plataforma.transform, Vector3.zero, local: true);
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
                basculante.renderersAviso = balsa.GetComponentsInChildren<Renderer>();
            }

            // Orillas: juncos pegados a cada orilla, rocas y nenúfares en el agua (fuera de los caminos).
            const float superficie = -2.85f;
            foreach (float lado in new[] { -1f, 1f })
            {
                foreach (float z in new[] { -15.5f, -8.5f })
                {
                    KitObjetos.Poner("Juncos", rio, new Vector3(2.2f * lado, superficie, z + (z < -12f ? 0.1f : -0.1f)), 40f * lado);
                    KitObjetos.Poner("Juncos", rio, new Vector3(11.6f * lado, superficie, z), -25f * lado, escala: 1.2f);
                }
                KitObjetos.Poner("RocasRio", rio, new Vector3(11.4f * lado, superficie - 0.25f, -12.3f), 70f * lado);
                KitObjetos.Poner("RocasRio", rio, new Vector3(2.2f * lado, superficie - 0.3f, -11.2f), 150f * lado, escala: 0.75f);
                KitObjetos.Poner("Nenufar", rio, new Vector3(10.3f * lado, superficie, -13.6f), 30f * lado);
                KitObjetos.Poner("Nenufar", rio, new Vector3(2.4f * lado, superficie, -13.4f), 200f * lado, escala: 0.8f);
                KitObjetos.Poner("Nenufar", rio, new Vector3(12f * lado, superficie, -9.8f), 110f * lado, escala: 0.9f);
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
                // Raíz sin escala con la colisión de siempre; el dibujo (de Blender) mira hacia la calle.
                float haciaCalle = e.lado < 0 ? 90f : -90f;
                var go = new GameObject(e.lado < 0 ? "EmpujadorIzq" : "EmpujadorDer");
                go.transform.SetParent(calle, false);
                go.transform.position = new Vector3(9f * e.lado, 0.7f, e.z);
                go.AddComponent<BoxCollider>().size = new Vector3(2f, 1.4f, 2.5f);
                var cuerpo = KitObjetos.Poner("Empujador", go.transform, new Vector3(0f, -0.7f, 0f), haciaCalle, local: true);
                go.AddComponent<Rigidbody>().isKinematic = true;
                var empujador = go.AddComponent<Empujador>();
                empujador.recorrido = new Vector3(-5f * e.lado, 0f, 0f);
                empujador.periodo = 4f;
                empujador.desfase = e.desfase;
                empujador.cuerpoVisible = cuerpo.GetComponentInChildren<Renderer>();
                empujador.nombreTrampa = $"Empujador {numero}";
                empujador.enfriamiento = 4f;
                empujador.radioEfecto = 4.5f;
                empujador.alturaMarcador = 2.5f;
                // Carcasa detrás, para que se vea de dónde sale.
                Ocultar(E.Bloque("Carcasa", calle, new Vector3(10.4f * e.lado, 0.9f, e.z), new Vector3(0.8f, 1.8f, 3f), m.rueda));
                KitObjetos.Poner("CarcasaEmpujador", calle, new Vector3(10.4f * e.lado, 0f, e.z), haciaCalle);
            }
        }

        // --- Zona 5: almacén con cintas que van hacia atrás y pilas de cajas en medio ---
        static PlacaPresion[] CrearAlmacen(Transform mapa, Mats m)
        {
            var almacen = Grupo(mapa, "Almacen");
            var placas = new PlacaPresion[2];
            foreach (float lado in new[] { -1f, 1f })
            {
                // Cada equipo tiene su cinta y, al lado, la placa que la para mientras alguien la pisa.
                int equipo = lado < 0 ? 0 : 1;
                var cinta = Cinta(almacen, m, 4.5f * lado, 7f, 19f, 31f);
                placas[equipo] = PlacaAyuda(almacen, equipo == 0 ? "PlacaCintaRoja" : "PlacaCintaAzul", new Vector3(9f * lado, 0f, 20.6f),
                                            equipo, m, "PARA\nLA CINTA");
                cinta.interruptor = placas[equipo];
                PilaCajas(almacen, new Vector3(6.8f * lado, 0f, 23f), 15f * lado);
                PilaCajas(almacen, new Vector3(8.9f * lado, 0f, 29.5f), -10f * lado);
            }
            PilaCajas(almacen, new Vector3(0f, 0f, 27.5f), 30f);
            Cartel(almacen, "ALMACÉN", new Vector3(0f, 3.2f, 31.5f), 0.9f, Color.white);
            return placas;
        }

        /// <summary>Placa de ayuda de un equipo, con su letrero: la pisa uno mientras el otro lleva el objeto.</summary>
        static PlacaPresion PlacaAyuda(Transform padre, string nombre, Vector3 posicion, int equipo, Mats m, string letrero)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = posicion;
            Decorado(PrimitiveType.Cube, "Borde", go.transform, new Vector3(0f, 0.025f, 0f), new Vector3(2.1f, 0.05f, 2.1f), m.peligro, local: true);
            var disco = E.Pieza(PrimitiveType.Cube, "Disco", go.transform, new Vector3(0f, 0.07f, 0f), new Vector3(1.7f, 0.12f, 1.7f), equipo == 0 ? m.rojo : m.azul);
            var placa = go.AddComponent<PlacaPresion>();
            placa.equipo = equipo;
            placa.tamano = new Vector3(1.9f, 1.6f, 1.9f);
            placa.visual = disco.GetComponent<Renderer>();
            Color color = equipo == 0 ? ColorRojo : ColorAzul;
            placa.colorLibre = color;
            placa.colorPulsada = new Color(0.45f, 1f, 0.5f);
            // Letrero en un panel oscuro sobre un poste, para que se lea desde lejos.
            Decorado(PrimitiveType.Cube, "Poste", go.transform, new Vector3(0f, 0.9f, 1.05f), new Vector3(0.1f, 1.8f, 0.1f), m.rueda, local: true);
            Decorado(PrimitiveType.Cube, "Panel", go.transform, new Vector3(0f, 1.95f, 1.05f), new Vector3(2.3f, 1.05f, 0.1f), m.rueda, local: true);
            Cartel(go.transform, letrero, posicion + new Vector3(0f, 1.95f, 0.98f), 0.42f, Color.white);
            return placa;
        }

        /// <summary>Cinta transportadora a ras de suelo: lleva hacia atrás (-Z) todo lo que la pisa.</summary>
        static CintaTransportadora Cinta(Transform padre, Mats m, float x, float ancho, float z0, float z1)
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

            foreach (float extremo in new[] { -1f, 1f })
                KitObjetos.Poner("RodilloCinta", go.transform, new Vector3(0f, 0f, extremo * (largo * 0.5f + 0.1f)), local: true);

            var cinta = go.AddComponent<CintaTransportadora>();
            cinta.direccion = Vector3.back;
            cinta.velocidad = 1.8f;
            cinta.tamano = new Vector2(ancho, largo);
            cinta.marcas = marcas.ToArray();
            return cinta;
        }

        /// <summary>Pila fija de cajas de cartón (una sola colisión, para no llenar la cinta de piezas).</summary>
        static void PilaCajas(Transform padre, Vector3 posicion, float giro)
        {
            var pila = new GameObject("PilaCajas");
            pila.transform.SetParent(padre, false);
            pila.transform.SetPositionAndRotation(posicion, Quaternion.Euler(0f, giro, 0f));
            var col = pila.AddComponent<BoxCollider>();
            col.size = new Vector3(1.6f, 2.25f, 1.6f);
            col.center = new Vector3(0f, 1.12f, 0f);
            KitObjetos.Poner("Palet", pila.transform, Vector3.zero, local: true);
            (Vector3 p, float g)[] cajas =
            {
                (new Vector3(-0.38f, 0f, -0.38f), 4f), (new Vector3(0.38f, 0f, -0.36f), -6f),
                (new Vector3(-0.36f, 0f, 0.38f), 10f), (new Vector3(0.38f, 0f, 0.4f), -3f),
                (new Vector3(-0.2f, 0.7f, 0f), 20f), (new Vector3(0.25f, 0.7f, 0.1f), -8f), (new Vector3(0.05f, 1.4f, 0.05f), -15f),
            };
            foreach (var c in cajas) KitObjetos.Poner("CajaCarton", pila.transform, c.p + Vector3.up * 0.14f, c.g, local: true);
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
            Ocultar(E.Bloque("Valla", padre, new Vector3(9.85f * lado, 0.35f, (z0 + z1) * 0.5f), new Vector3(0.3f, 0.7f, z1 - z0), m.madera));
            // Tramos de 2 m de la valla de Blender, repartidos a lo largo.
            int tramos = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / 2f));
            for (int i = 0; i < tramos; i++)
                KitObjetos.Poner("Valla", padre, new Vector3(9.85f * lado, 0f, z0 + (z1 - z0) * (i + 0.5f) / tramos), 90f, escala: 1f);
        }

        // --- Zona 7: casa de cada equipo con puerta giratoria ---
        static ZonaEntrega CrearCasa(Transform mapa, Mats m, int equipo, float cx, string titulo,
                                     Material colorEquipo, Material pared, Color color, float giroPuerta, out PlacaPresion freno)
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

            // Remate de color encima de las paredes (desde arriba se ve el contorno de la casa).
            Decorado(PrimitiveType.Cube, "Remate", casa, new Vector3(cx, alto + 0.06f, fondo), new Vector3(medio * 2f + 0.45f, 0.14f, 0.45f), colorEquipo);
            foreach (float s in new[] { -1f, 1f })
            {
                Decorado(PrimitiveType.Cube, "Remate", casa, new Vector3(cx + medio * s, alto + 0.06f, centroZ), new Vector3(0.45f, 0.14f, fondo - frente + 0.45f), colorEquipo);
                Decorado(PrimitiveType.Cube, "Remate", casa, new Vector3(cx + (hueco + anchoFrente / 2f) * s, alto + 0.06f, frente), new Vector3(anchoFrente + 0.2f, 0.14f, 0.45f), colorEquipo);
                // Ventanas con contraventanas y geranios en la fachada.
                KitObjetos.Poner("VentanaCasa", casa, new Vector3(cx + (hueco + anchoFrente / 2f) * s, 0f, frente - 0.16f), 180f, colorEquipo);
            }
            float fuera = Mathf.Sign(cx);
            foreach (float z in new[] { frente + 2.6f, frente + 6.4f })
                KitObjetos.Poner("VentanaCasa", casa, new Vector3(cx + (medio + 0.16f) * fuera, 0f, z), 90f * fuera, colorEquipo);
            KitObjetos.Poner("Toldo", casa, new Vector3(cx, alto + 0.25f, frente - 0.16f), 180f, colorEquipo);

            // Maceta junto a la puerta, por el lado de dentro (al otro lado está el buzón).
            var maceta = KitObjetos.Poner("Maceta", casa, new Vector3(cx - 3.9f * fuera, 0f, frente - 0.9f), 0f);
            var colMaceta = maceta.AddComponent<CapsuleCollider>();
            colMaceta.radius = 0.42f;
            colMaceta.height = 1.4f;
            colMaceta.center = new Vector3(0f, 0.7f, 0f);
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
                Ocultar(hoja);
                // La hoja de Blender: cristal con marco y barras para empujar.
                var dibujo = KitObjetos.Poner("HojaPuerta", puerta.transform, rot * new Vector3(radioColumna + largoHoja / 2f, 0.05f, 0f), 120f * i, local: true);
                hojas[i] = dibujo.GetComponentInChildren<Renderer>();
                E.Pieza(PrimitiveType.Cylinder, "Rulo", puerta.transform, rot * new Vector3(radioColumna + largoHoja, 1.35f, 0f), new Vector3(0.36f, 1.3f, 0.36f), colorEquipo, conCollider: true);
            }
            giro.renderersAviso = hojas;

            // Placa en el jardín, por fuera del camino: mientras alguien del equipo la pisa, la puerta va muy despacio.
            freno = PlacaAyuda(casa, "PlacaFreno", new Vector3(cx + 4.7f * fuera, 0f, frente - 3.8f), equipo, m, "FRENA\nLA PUERTA");
            giro.freno = freno;
            giro.factorFreno = 0.25f;

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

        /// <summary>Deja la pieza solo como colisión: su dibujo lo pone una pieza del kit.</summary>
        static void Ocultar(GameObject go)
        {
            if (go != null && go.TryGetComponent(out Renderer r)) r.enabled = false;
        }

        /// <summary>Losa fina de suelo encima de la tierra; arriba queda en y = arriba.</summary>
        static void Losa(Transform padre, string nombre, float x0, float x1, float z0, float z1, Material material, float arriba = 0.01f, float giro = 0f)
        {
            var go = Decorado(PrimitiveType.Cube, nombre, padre, new Vector3((x0 + x1) * 0.5f, arriba - 0.02f, (z0 + z1) * 0.5f),
                              new Vector3(x1 - x0, 0.04f, z1 - z0), material);
            if (go != null && giro != 0f) go.transform.rotation = Quaternion.Euler(0f, giro, 0f);
        }

        /// <summary>
        /// Lo que se pisa en cada zona: patio de carga con plazas pintadas, calle de asfalto con aceras
        /// y pasos de cebra, hormigón con cinta de peligro en el almacén, baldosas en el patio y césped
        /// con caminos (y parquet dentro de las casas).
        /// </summary>
        static void CrearSuelos(Transform mapa, Mats m)
        {
            var suelos = Grupo(mapa, "Suelos");

            // Salida: pavimento, plaza pintada de cada camión y zona rayada donde sale cada objeto.
            Losa(suelos, "Pavimento", -10f, 10f, -38f, -16f, m.pavimento);
            foreach (float lado in new[] { -1f, 1f })
            {
                float x = 5f * lado;
                foreach (float s in new[] { -1f, 1f })
                    Losa(suelos, "Plaza", x + 2.2f * s - 0.075f, x + 2.2f * s + 0.075f, -38f, -31.6f, m.linea, 0.02f);
                Losa(suelos, "Plaza", x - 2.275f, x + 2.275f, -31.75f, -31.6f, m.linea, 0.02f);
                Losa(suelos, "ZonaCarga", x - 1.6f, x + 1.6f, -31.2f, -29.2f, m.peligro, 0.015f);
                for (int i = 0; i < 6; i++)
                {
                    float cx = x - 1.35f + i * 0.54f;
                    Losa(suelos, "RayaCarga", cx - 0.1f, cx + 0.1f, -31.1f, -29.3f, m.rueda, 0.02f, 35f);
                }
            }

            // Calle: aceras de lado a lado, asfalto en medio, bordillos y pasos de cebra.
            const float c0 = -8f, c1 = 17.6f;
            Losa(suelos, "Acera", -10f, 10f, c0, c1, m.acera);
            Losa(suelos, "Asfalto", -7f, 7f, c0, c1, m.asfalto, 0.015f);
            foreach (float s in new[] { -1f, 1f })
                Losa(suelos, "Bordillo", 7f * s - 0.15f, 7f * s + 0.15f, c0, c1, m.bordillo, 0.02f);
            for (float z = -2.5f; z <= 12.5f; z += 2.5f)
                Losa(suelos, "Linea", -0.12f, 0.12f, z - 0.6f, z + 0.6f, m.linea, 0.02f);
            foreach (float zPaso in new[] { -6.6f, 14.6f })
                for (float x = -6.25f; x <= 6.3f; x += 1.25f)
                    Losa(suelos, "PasoCebra", x - 0.35f, x + 0.35f, zPaso, zPaso + 1.6f, m.linea, 0.02f);
            foreach (var p in new[] { new Vector3(-4.6f, 0.012f, 2.6f), new Vector3(5f, 0.012f, 7.4f), new Vector3(-5f, 0.012f, 11.6f) })
                KitObjetos.Poner("Alcantarilla", suelos, p, 20f);

            // Almacén: hormigón con una franja de cinta de peligro a la entrada y a la salida.
            const float a0 = 17.6f, a1 = 32.6f;
            Losa(suelos, "Hormigon", -10f, 10f, a0, a1, m.hormigon);
            foreach (float zFranja in new[] { a0 + 0.35f, a1 - 0.35f })
                for (int i = 0; i < 20; i++)
                    Losa(suelos, "Franja", -10f + i, -9f + i, zFranja - 0.25f, zFranja + 0.25f, i % 2 == 0 ? m.peligro : m.rueda, 0.02f);

            // Patio: baldosas a cuadros.
            const float p0 = 32.6f, p1 = 46f;
            Losa(suelos, "Baldosas", -10f, 10f, p0, p1, m.baldosaA);
            int filas = 6;
            float altoFila = (p1 - p0) / filas;
            for (int fila = 0; fila < filas; fila++)
                for (int col = 0; col < 10; col++)
                    if ((fila + col) % 2 == 1)
                        Losa(suelos, "Baldosa", -10f + col * 2f + 0.04f, -8f + col * 2f - 0.04f, p0 + fila * altoFila + 0.04f, p0 + (fila + 1) * altoFila - 0.04f, m.baldosaB, 0.015f);

            // Jardín de las casas: césped, una plazoleta de entrada, un camino a cada puerta y parquet dentro.
            Losa(suelos, "Cesped", -14f, 14f, 46f, 66f, m.cesped);
            Losa(suelos, "Plazoleta", -10f, 10f, 46f, 48.5f, m.camino, 0.015f);
            foreach (float cx in new[] { -XCasa, XCasa })
            {
                Losa(suelos, "Camino", cx - 3.6f, cx + 3.6f, 48.5f, FrenteCasa, m.camino, 0.015f);
                Losa(suelos, "Parquet", cx - 5.85f, cx + 5.85f, FrenteCasa, FrenteCasa + 9f, m.parquet, 0.015f);
                for (float z = FrenteCasa + 0.6f; z < FrenteCasa + 9f; z += 1.2f)
                    Losa(suelos, "Tablon", cx - 5.85f, cx + 5.85f, z - 0.02f, z + 0.02f, m.madera, 0.018f);
            }
        }

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
