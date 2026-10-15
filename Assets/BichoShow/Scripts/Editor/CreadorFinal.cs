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
    /// Menú "BichoShow > Crear escena: Gran final (Juntos o nada)".
    /// Dos carriles en espejo (rojo a la izquierda, azul a la derecha) con un vacío entre medias.
    /// Cada carril, de la salida (z = -46) a la meta (z = 34):
    ///   1. Pasillo estrecho tapado por un bloque que hay que empujar entre dos hasta un foso.
    ///   2. Puente levadizo sobre un hueco: uno pisa una placa mientras el otro cruza.
    ///   3. Puerta que solo se abre pisando a la vez dos placas separadas.
    ///   4. Ascensor que solo sube con los dos encima, hasta la meta en lo alto.
    /// Trampas para el saboteador: una barredora y dos empujadores por carril.
    /// </summary>
    public static class CreadorFinal
    {
        const string RutaEscena = E.CarpetaEscenas + "/Final.unity";

        static readonly Color ColorRojo = new Color(1f, 0.38f, 0.35f);
        static readonly Color ColorAzul = new Color(0.3f, 0.6f, 1f);

        class Mats
        {
            public Material suelo, pared, bloque, placa, madera, puerta, ascensor, oro, peligro, oscuro, rojo, azul, sabotaje, particula, obstaculo;
        }

        [MenuItem("BichoShow/Crear escena: Gran final (Juntos o nada)")]
        public static void Crear()
        {
            var controles = AssetDatabase.LoadAssetAtPath<InputActionAsset>(E.RutaControles);
            if (controles == null)
            {
                EditorUtility.DisplayDialog("Bicho Show", $"No encuentro los controles en:\n{E.RutaControles}", "Vale");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) != null &&
                !EditorUtility.DisplayDialog("Bicho Show", "La escena de la final ya existe. ¿Rehacerla?", "Rehacer", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            E.CrearCarpeta(E.Raiz, "Materiales");
            E.CrearCarpeta(E.Raiz, "Prefabs");
            E.CrearCarpeta(E.Raiz, "Escenas");
            var basicos = E.CrearMateriales();
            var bicho = AssetDatabase.LoadAssetAtPath<GameObject>(E.RutaPrefabBicho) ?? E.CrearPrefabBicho(controles, basicos);

            var m = new Mats
            {
                suelo = E.CrearMaterial("SueloFinal", new Color(0.72f, 0.74f, 0.82f), 0.2f),
                pared = E.CrearMaterial("ParedFinal", new Color(0.5f, 0.45f, 0.62f), 0.2f),
                bloque = E.CrearMaterial("BloqueFinal", new Color(0.85f, 0.6f, 0.35f), 0.15f),
                placa = E.CrearMaterial("Placa", new Color(0.55f, 0.55f, 0.6f), 0.5f),
                madera = E.CrearMaterial("Madera", new Color(0.62f, 0.43f, 0.26f), 0.15f),
                puerta = E.CrearMaterial("PuertaFinal", new Color(0.9f, 0.3f, 0.45f), 0.4f),
                ascensor = E.CrearMaterial("AscensorFinal", new Color(0.35f, 0.85f, 0.75f), 0.5f),
                oro = E.CrearMaterial("Oro", new Color(1f, 0.82f, 0.25f), 0.9f),
                peligro = E.CrearMaterial("Peligro", new Color(1f, 0.82f, 0.15f), 0.3f),
                oscuro = E.CrearMaterial("Rueda", new Color(0.12f, 0.12f, 0.14f), 0.2f),
                rojo = E.CrearMaterial("EquipoRojo", ColorRojo, 0.3f),
                azul = E.CrearMaterial("EquipoAzul", ColorAzul, 0.3f),
                sabotaje = E.CrearMaterial("Sabotaje", new Color(0.62f, 0.3f, 0.9f), 0.5f),
                obstaculo = basicos.obstaculo,
            };
            m.particula = AssetDatabase.LoadAssetAtPath<Material>($"{E.CarpetaMateriales}/Particula.mat");
            if (m.particula == null)
            {
                m.particula = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
                AssetDatabase.CreateAsset(m.particula, $"{E.CarpetaMateriales}/Particula.mat");
            }

            CrearEscena(bicho, m);
            Debug.Log("Bicho Show: escena de la final creada. Para que el show la use, rehaz también la escena del menú.");
        }

        static void CrearEscena(GameObject prefabBicho, Mats m)
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.75f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.6f, 0.58f, 0.68f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.3f, 0.38f);

            var luz = new GameObject("Luz").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.3f;
            luz.shadows = LightShadows.Soft;
            luz.color = new Color(1f, 0.96f, 0.9f);
            luz.transform.rotation = Quaternion.Euler(55f, -20f, 0f);

            var camGo = new GameObject("Camara");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.32f, 0.22f, 0.5f);
            cam.fieldOfView = 45f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();
            var camara = camGo.AddComponent<CamaraGrupo>();
            camara.direccion = new Vector3(0f, 1.3f, -1f);
            camara.distanciaMinima = 13f;
            camara.distanciaMaxima = 40f;
            camara.centroSinJugadores = new Vector3(0f, 0f, -38f);
            camara.ColocarAlInstante();

            var mapa = new GameObject("Mapa").transform;
            var carrilRojo = CrearCarril(mapa, 0, -7f, -1f, m);
            var carrilAzul = CrearCarril(mapa, 1, 7f, 1f, m);

            M.Cartel(mapa, "JUNTOS O NADA", new Vector3(0f, 12f, 38f), 3.5f, Color.white);
            var vacio = M.ZonaTrigger<ZonaMortal>("ZonaVacio", mapa, new Vector3(0f, -12f, -5f), new Vector3(120f, 4f, 160f));
            vacio.motivoObjeto = "se ha caído al vacío";

            // Plataforma de saboteador (jugando la final suelta), en la esquina de la salida roja.
            var cabina = M.Grupo(mapa, "CabinaSaboteador");
            M.Decorado(PrimitiveType.Cube, "Plataforma", cabina, new Vector3(-10.5f, 0.03f, -45f), new Vector3(2.4f, 0.06f, 1.6f), m.sabotaje);
            M.Cartel(cabina, "SABOTEADOR", new Vector3(-10.5f, 2f, -45.5f), 0.45f, Color.white);
            var zona = new GameObject("ZonaSaboteadores");
            zona.transform.SetParent(cabina, false);
            zona.transform.position = new Vector3(-10.5f, 1.5f, -45f);
            var trigger = zona.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.4f, 3f, 1.6f);

            // --- Jugadores: 1 y 3 en el carril rojo, 2 y 4 en el azul ---
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
            gestor.mostrarAyuda = false;
            gestor.colores = new[] { ColorRojo, ColorAzul, new Color(1f, 0.68f, 0.2f), new Color(0.45f, 0.9f, 0.95f) };
            float[] xs = { -8f, 6f, -6f, 8f };
            var puntos = new List<Transform>();
            for (int i = 0; i < xs.Length; i++)
            {
                var p = new GameObject($"Aparicion{i + 1}").transform;
                p.SetParent(jugadoresGo.transform, false);
                p.position = new Vector3(xs[i], 0.2f, -42f);
                puntos.Add(p);
            }
            gestor.puntosAparicion = puntos.ToArray();

            // --- Ronda ---
            var rondaGo = new GameObject("RondaFinal");
            var ronda = rondaGo.AddComponent<RondaFinal>();
            ronda.gestor = gestor;
            ronda.zonaSaboteadores = trigger;
            ronda.saboteadorMaquina = true;
            ronda.agresividadMaquina = 0.4f;
            ronda.equipos = new[]
            {
                new RondaFinal.Equipo { nombre = "Rojo", color = ColorRojo, puntosControl = carrilRojo.controles, pasos = carrilRojo.pasos },
                new RondaFinal.Equipo { nombre = "Azul", color = ColorAzul, puntosControl = carrilAzul.controles, pasos = carrilAzul.pasos },
            };
            ronda.confeti = M.Particulas("Confeti", rondaGo.transform, new Vector3(0f, 4.3f, 30f),
                ColorRojo, new Color(1f, 0.92f, 0.3f), 150, 5f, 10f, m.particula);

            EditorSceneManager.SaveScene(escena, RutaEscena);
            E.AnadirAlBuild(RutaEscena);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rondaGo;
        }

        // ------------------------------------------------------------------
        // Un carril (lx = centro en X; fuera = -1 si el borde exterior está a la izquierda)
        // ------------------------------------------------------------------

        static (PasoCooperativo[] pasos, Transform[] controles) CrearCarril(Transform mapa, int equipo, float lx, float fuera, Mats m)
        {
            string campo = equipo == 0 ? "rojo" : "azul";
            Material colorEquipo = equipo == 0 ? m.rojo : m.azul;
            var carril = M.Grupo(mapa, $"Carril {campo}");

            // --- Salida ---
            E.Bloque("Salida", carril, new Vector3(lx, -1f, -38.5f), new Vector3(10f, 2f, 15f), m.suelo);
            M.Decorado(PrimitiveType.Cube, "LineaSalida", carril, new Vector3(lx, 0.01f, -39.5f), new Vector3(10f, 0.02f, 0.4f), colorEquipo);
            M.Cartel(carril, $"CARRIL {campo.ToUpper()}", new Vector3(lx, 3.5f, -46.5f), 0.9f, Color.white);

            // --- 1. El bloque: pasillo estrecho con foso ---
            var p1 = M.Grupo(carril, "1 Bloque");
            E.Bloque("PasilloA", p1, new Vector3(lx, -1f, -27.8f), new Vector3(3f, 2f, 6.4f), m.suelo);
            E.Bloque("FondoFoso", p1, new Vector3(lx, -3.2f, -23.3f), new Vector3(3f, 1.6f, 2.6f), m.oscuro);
            E.Bloque("PasilloB", p1, new Vector3(lx, -1f, -21f), new Vector3(3f, 2f, 2f), m.suelo);
            foreach (float lado in new[] { -1f, 1f })
                E.Bloque("ParedPasillo", p1, new Vector3(lx + 1.7f * lado, 0.3f, -25.5f), new Vector3(0.4f, 5.4f, 11f), m.pared);
            var bloque = E.Bloque("Bloque", p1, new Vector3(lx, 1.2f, -28f), new Vector3(2.8f, 2.4f, 2.4f), m.bloque);
            var rbBloque = bloque.AddComponent<Rigidbody>();
            rbBloque.mass = 45f;
            rbBloque.angularDamping = 0.5f;
            rbBloque.interpolation = RigidbodyInterpolation.Interpolate;
            bloque.AddComponent<NoAgarrable>();
            M.Cartel(p1, "EMPUJAD JUNTOS", new Vector3(lx, 3.6f, -31.3f), 0.7f, Color.white);

            var paso1 = p1.gameObject.AddComponent<PasoBloque>();
            paso1.equipo = equipo;
            paso1.pista = "¡Empujad el bloque juntos!";
            paso1.zFin = -21.5f;
            paso1.bloque = rbBloque;
            paso1.adelante = Vector3.forward;
            paso1.salida = new Vector3(lx, 0f, -18.5f);

            // --- 2. El puente levadizo ---
            var p2 = M.Grupo(carril, "2 Puente");
            E.Bloque("Orilla", p2, new Vector3(lx, -1f, -15f), new Vector3(10f, 2f, 10f), m.suelo);
            E.Bloque("OtraOrilla", p2, new Vector3(lx, -1f, 7f), new Vector3(10f, 2f, 18f), m.suelo);

            var barredora = new GameObject($"Barredora ({campo})");
            barredora.transform.SetParent(p2, false);
            barredora.transform.position = new Vector3(lx, 0f, -14.5f);
            barredora.AddComponent<Rigidbody>().isKinematic = true;
            var giro = barredora.AddComponent<BarraGiratoria>();
            giro.velocidadGiro = equipo == 0 ? 55f : -55f;
            E.Pieza(PrimitiveType.Cylinder, "Eje", barredora.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.7f, 0.6f, 0.7f), m.peligro, conCollider: true);
            var brazo = E.Bloque("Brazo", barredora.transform, new Vector3(0f, 0.55f, 0f), new Vector3(7f, 0.4f, 0.4f), m.obstaculo, local: true);
            giro.nombreTrampa = $"Barredora ({campo})";
            giro.enfriamiento = 8f;
            giro.radioEfecto = 5f;
            giro.alturaMarcador = 3f;
            giro.renderersAviso = new[] { brazo.GetComponent<Renderer>() };

            var pivote = new GameObject("PuenteLevadizo");
            pivote.transform.SetParent(p2, false);
            pivote.transform.position = new Vector3(lx, 0f, -10f);
            pivote.AddComponent<Rigidbody>().isKinematic = true;
            E.Bloque("Tablero", pivote.transform, new Vector3(0f, -0.2f, 4f), new Vector3(3f, 0.4f, 8f), m.madera, local: true);
            var puente = pivote.AddComponent<PuenteLevadizo>();
            puente.equipo = equipo;

            var placaCerca = Placa(p2, "PlacaCerca", new Vector3(lx + fuera * 3f, 0f, -11.5f), equipo, m);
            var placaLejos = Placa(p2, "PlacaLejos", new Vector3(lx + fuera * 3f, 0f, 0f), equipo, m);
            puente.placas = new[] { placaCerca, placaLejos };
            M.Cartel(p2, "UNO PISA, OTRO CRUZA", new Vector3(lx, 3f, -10.6f), 0.55f, Color.white);

            var paso2 = p2.gameObject.AddComponent<PasoPuente>();
            paso2.equipo = equipo;
            paso2.pista = "Uno pisa la placa y el otro cruza";
            paso2.zFin = -1.2f;
            paso2.puente = puente;
            paso2.placaCerca = placaCerca;
            paso2.placaLejos = placaLejos;
            paso2.xCentro = lx;
            paso2.zInicioHueco = -10f;
            paso2.zFinHueco = -2f;
            paso2.salida = new Vector3(lx, 0f, 4f);

            // --- 3. La puerta doble ---
            var p3 = M.Grupo(carril, "3 Puerta");
            var puertaGo = E.Bloque("Puerta", p3, new Vector3(lx, 1.6f, 12f), new Vector3(10f, 3.2f, 0.6f), m.puerta);
            puertaGo.AddComponent<Rigidbody>().isKinematic = true;
            var puerta = puertaGo.AddComponent<PuertaDoble>();
            puerta.equipo = equipo;
            puerta.placaA = Placa(p3, "PlacaA", new Vector3(lx - 3.5f, 0f, 7f), equipo, m);
            puerta.placaB = Placa(p3, "PlacaB", new Vector3(lx + 3.5f, 0f, 7f), equipo, m);
            M.Cartel(p3, "PISAD A LA VEZ", new Vector3(lx, 2.2f, 11.6f), 0.7f, Color.white);
            Empujador(p3, $"Empujador ({campo}) 1", lx, fuera, 4.5f, 0f, m);

            var paso3 = p3.gameObject.AddComponent<PasoPuerta>();
            paso3.equipo = equipo;
            paso3.pista = "¡Pisad las dos placas a la vez!";
            paso3.zFin = 13f;
            paso3.puerta = puerta;
            paso3.placaA = puerta.placaA;
            paso3.placaB = puerta.placaB;
            paso3.salida = new Vector3(lx, 0f, 15.5f);

            // --- 4. El ascensor y la meta ---
            var p4 = M.Grupo(carril, "4 Ascensor");
            E.Bloque("Explanada", p4, new Vector3(lx, -1f, 20f), new Vector3(10f, 2f, 8f), m.suelo);
            E.Bloque("Repisa", p4, new Vector3(lx, 1f, 29f), new Vector3(10f, 6f, 10f), m.pared);
            var ascensorGo = E.Bloque("Ascensor", p4, new Vector3(lx, 0.15f, 21.8f), new Vector3(4f, 0.3f, 4f), m.ascensor);
            ascensorGo.AddComponent<Rigidbody>().isKinematic = true;
            var ascensor = ascensorGo.AddComponent<Ascensor>();
            ascensor.equipo = equipo;
            ascensor.subida = 3.7f;
            M.Cartel(p4, "SUBID LOS DOS", new Vector3(lx, 2.8f, 23.85f), 0.7f, Color.white);
            Empujador(p4, $"Empujador ({campo}) 2", lx, fuera, 18f, 1.6f, m);

            // Meta en lo alto, con podio y trofeo
            M.Decorado(PrimitiveType.Cylinder, "Podio", p4, new Vector3(lx, 4.15f, 30.5f), new Vector3(3f, 0.15f, 3f), m.oro);
            M.Decorado(PrimitiveType.Cylinder, "Copa", p4, new Vector3(lx, 4.9f, 32f), new Vector3(0.4f, 0.6f, 0.4f), m.oro);
            M.Decorado(PrimitiveType.Sphere, "CopaArriba", p4, new Vector3(lx, 5.7f, 32f), Vector3.one * 0.9f, m.oro);
            M.Decorado(PrimitiveType.Cube, "Banderin", p4, new Vector3(lx, 4.02f, 26f), new Vector3(10f, 0.04f, 0.5f), colorEquipo);
            M.Cartel(p4, "META", new Vector3(lx, 8f, 33.5f), 1.4f, Color.white);
            var meta = new GameObject("Meta").AddComponent<PlacaPresion>();
            meta.transform.SetParent(p4, false);
            meta.transform.position = new Vector3(lx, 4f, 30f);
            meta.tamano = new Vector3(9f, 3f, 7f);
            meta.equipo = equipo;

            var paso4 = p4.gameObject.AddComponent<PasoAscensor>();
            paso4.equipo = equipo;
            paso4.pista = "¡Subid los dos al ascensor!";
            paso4.ascensor = ascensor;
            paso4.meta = meta;

            // --- Puntos de control: salida y el inicio de cada prueba ---
            var controles = new[]
            {
                Control(carril, "Control0", new Vector3(lx, 0.2f, -42f)),
                Control(carril, "Control1", new Vector3(lx, 0.2f, -19.2f)),
                Control(carril, "Control2", new Vector3(lx - fuera * 1.5f, 0.2f, 1.5f)),
                Control(carril, "Control3", new Vector3(lx, 0.2f, 14.5f)),
            };

            return (new PasoCooperativo[] { paso1, paso2, paso3, paso4 }, controles);
        }

        static PlacaPresion Placa(Transform padre, string nombre, Vector3 posicion, int equipo, Mats m)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = posicion;
            var visual = E.Pieza(PrimitiveType.Cube, "Disco", go.transform, new Vector3(0f, 0.06f, 0f), new Vector3(1.6f, 0.12f, 1.6f), m.placa);
            var placa = go.AddComponent<PlacaPresion>();
            placa.equipo = equipo;
            placa.visual = visual.GetComponent<Renderer>();
            return placa;
        }

        static void Empujador(Transform padre, string nombre, float lx, float fuera, float z, float desfase, Mats m)
        {
            // Sale desde el borde exterior y empuja hacia el vacío del centro.
            var go = E.Bloque(nombre, padre, new Vector3(lx + fuera * 4f, 0.7f, z), new Vector3(2f, 1.4f, 2.5f), m.peligro);
            go.AddComponent<Rigidbody>().isKinematic = true;
            var empujador = go.AddComponent<Empujador>();
            empujador.recorrido = new Vector3(-fuera * 4.5f, 0f, 0f);
            empujador.periodo = 4.5f;
            empujador.desfase = desfase;
            empujador.cuerpoVisible = go.GetComponent<Renderer>();
            empujador.nombreTrampa = nombre;
            empujador.enfriamiento = 4f;
            empujador.radioEfecto = 4.5f;
            empujador.alturaMarcador = 2.5f;
        }

        static Transform Control(Transform padre, string nombre, Vector3 posicion)
        {
            var t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            t.position = posicion;
            return t;
        }
    }
}
