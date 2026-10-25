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
    /// Cada carril, de la salida (z = -46) a la meta (z = 108):
    ///   1. Pasillo estrecho tapado por un bloque que hay que empujar entre dos hasta un foso.
    ///   2. Puente levadizo sobre un hueco: uno pisa una placa mientras el otro cruza.
    ///   3. Puerta que solo se abre pisando a la vez dos placas separadas.
    ///   4. Pasillos cruzados: las compuertas de cada pasillo las abren las placas del otro.
    ///   5. Camino de baldosas: cada tramo está solo mientras el compañero pisa su placa.
    ///   6. Balancín: uno cruza y el otro hace de contrapeso; al llegar, se pisa el freno.
    ///   7. Ascensor que solo sube con los dos encima, hasta la meta en lo alto.
    /// Trampas (para el saboteador, o solas si no hay): dos barredoras y tres empujadores por carril.
    /// </summary>
    public static class CreadorFinal
    {
        public const string RutaEscena = E.CarpetaEscenas + "/Final.unity";
        /// <summary>Súbela al cambiar el mapa: las escenas viejas avisan al darle a Play (EscenasAnticuadas).</summary>
        public const int Version = 2;

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

            M.Cartel(mapa, "JUNTOS O NADA", new Vector3(0f, 9f, 109.8f), 3.5f, Color.white);
            CrearPlato(mapa);
            var vacio = M.ZonaTrigger<ZonaMortal>("ZonaVacio", mapa, new Vector3(0f, -12f, 30f), new Vector3(120f, 4f, 230f));
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
            ronda.confeti = M.Particulas("Confeti", rondaGo.transform, new Vector3(0f, 4.3f, 104f),
                ColorRojo, new Color(1f, 0.92f, 0.3f), 150, 5f, 10f, m.particula);

            new GameObject(EscenasAnticuadas.Marca + Version);

            Ambientes.Aplicar(Ambientes.Tema.Plato);
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
            E.Bloque("FondoFoso", p1, new Vector3(lx, -3.2f, -23.0f), new Vector3(3f, 1.6f, 3.2f), m.oscuro); // 3.2 m: holgura para que el bloque (2.4) vuelque y caiga sin encallarse
            E.Bloque("PasilloB", p1, new Vector3(lx, -1f, -20.7f), new Vector3(3f, 2f, 1.4f), m.suelo);
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
            paso1.zFin = -20.5f;
            paso1.bloque = rbBloque;
            paso1.adelante = Vector3.forward;
            paso1.salida = new Vector3(lx, 0f, -18.5f);
            paso1.centroFoso = new Vector3(lx, 0f, -23.0f);

            // --- 2. El puente levadizo ---
            var p2 = M.Grupo(carril, "2 Puente");
            E.Bloque("Orilla", p2, new Vector3(lx, -1f, -15f), new Vector3(10f, 2f, 10f), m.suelo);
            E.Bloque("OtraOrilla", p2, new Vector3(lx, -1f, 7f), new Vector3(10f, 2f, 18f), m.suelo);

            Barredora(p2, $"Barredora ({campo})", new Vector3(lx, 0f, -14.5f), equipo == 0 ? 55f : -55f, m);

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

            // --- 4. Los pasillos cruzados (las compuertas de un pasillo las abren las placas del otro) ---
            var p4 = M.Grupo(carril, "4 Pasillos");
            E.Bloque("SueloPasillos", p4, new Vector3(lx, -1f, 29f), new Vector3(10f, 2f, 26f), m.suelo);
            const float z0 = 17f, largoPasillos = 20f;
            float xA = lx - 1.6f, xB = lx + 1.6f;
            E.Bloque("ParedMedio", p4, new Vector3(lx, 1.5f, z0 + largoPasillos * 0.5f), new Vector3(0.4f, 3f, largoPasillos), m.pared);
            foreach (float lado in new[] { -1f, 1f })
                E.Bloque("ParedFuera", p4, new Vector3(lx + lado * 4f, 1.5f, z0 + largoPasillos * 0.5f), new Vector3(2f, 3f, largoPasillos), m.pared);
            // B: placa, compuerta, placa, compuerta… ; A: compuerta, placa, compuerta, placa…
            var placasB = new List<PlacaPresion>();
            var placasA = new List<PlacaPresion>();
            for (int i = 0; i < 3; i++)
            {
                placasB.Add(Placa(p4, $"PlacaB{i + 1}", new Vector3(xB, 0f, z0 + 1.5f + 6f * i), equipo, m));
                placasA.Add(Placa(p4, $"PlacaA{i + 1}", new Vector3(xA, 0f, z0 + 6f + 6f * i), equipo, m));
            }
            var compuertasA = new List<Compuerta>();
            var compuertasB = new List<Compuerta>();
            for (int i = 0; i < 3; i++)
            {
                compuertasA.Add(CompuertaPasillo(p4, $"CompuertaA{i + 1}", new Vector3(xA, 0f, z0 + 3f + 6f * i), placasB[i], equipo, colorEquipo));
                compuertasB.Add(CompuertaPasillo(p4, $"CompuertaB{i + 1}", new Vector3(xB, 0f, z0 + 4.5f + 6f * i), placasA[i], equipo, colorEquipo));
            }
            M.Cartel(p4, "TU PAREJA TE ABRE", new Vector3(lx, 4f, z0 - 0.4f), 0.6f, Color.white);

            var paso4 = p4.gameObject.AddComponent<PasoPasillos>();
            paso4.equipo = equipo;
            paso4.pista = "Cada uno por un pasillo: ¡tu placa abre su compuerta!";
            paso4.zFin = z0 + largoPasillos + 0.2f;
            paso4.zEntrada = z0;
            paso4.xA = xA;
            paso4.xB = xB;
            paso4.placasA = placasA.ToArray();
            paso4.placasB = placasB.ToArray();
            paso4.compuertasA = compuertasA.ToArray();
            paso4.compuertasB = compuertasB.ToArray();
            paso4.salida = new Vector3(lx, 0f, 39.5f);

            // --- 5. El camino de baldosas (z 42-56: cada tramo, mientras se pisa su placa) ---
            var p5 = M.Grupo(carril, "5 Baldosas");
            const float zHueco5 = 42f, zFin5 = 56f;
            E.Bloque("OrillaLejana", p5, new Vector3(lx, -1f, 61f), new Vector3(10f, 2f, 10f), m.suelo);
            float xCamino = lx - fuera * 1.8f;
            var baldosa = E.CrearMaterial("Baldosa", new Color(0.4f, 0.95f, 1f), 0.6f);
            var tramos = new GameObject[3];
            var placasTramo = new PlacaPresion[3];
            float largoTramo = (zFin5 - zHueco5) / tramos.Length;
            for (int i = 0; i < tramos.Length; i++)
            {
                var tramo = new GameObject($"Tramo {i + 1}");
                tramo.transform.SetParent(p5, false);
                float zc = zHueco5 + largoTramo * (i + 0.5f);
                // Dos baldosas por tramo, con una raya entre medias (se nota que son losas).
                E.Bloque("Baldosa", tramo.transform, new Vector3(xCamino, -0.16f, zc - largoTramo * 0.25f), new Vector3(2.4f, 0.32f, largoTramo * 0.5f - 0.06f), baldosa);
                E.Bloque("Baldosa", tramo.transform, new Vector3(xCamino, -0.16f, zc + largoTramo * 0.25f), new Vector3(2.4f, 0.32f, largoTramo * 0.5f - 0.06f), baldosa);
                tramos[i] = tramo;
                placasTramo[i] = Placa(p5, $"PlacaTramo{i + 1}", new Vector3(lx + fuera * (0.6f + 1.8f * i), 0f, 39.8f), equipo, m);
                M.Cartel(p5, (i + 1).ToString(), new Vector3(lx + fuera * (0.6f + 1.8f * i), 1.4f, 40.9f), 0.5f, Color.white);
            }
            var placaTodo = Placa(p5, "PlacaLejana", new Vector3(lx + fuera * 2.4f, 0f, 57.6f), equipo, m);
            var camino = p5.gameObject.AddComponent<CaminoBaldosas>();
            camino.equipo = equipo;
            camino.tramos = tramos;
            camino.placas = placasTramo;
            camino.placaLejos = placaTodo;
            M.Cartel(p5, "PISA EL TRAMO DE TU PAREJA", new Vector3(lx, 3.2f, 41.4f), 0.5f, Color.white);
            Empujador(p5, $"Empujador ({campo}) 3", lx, fuera, 38.6f, 0.8f, m);

            var paso5 = p5.gameObject.AddComponent<PasoBaldosas>();
            paso5.equipo = equipo;
            paso5.pista = "Uno pisa las placas 1-2-3, el otro cruza por las baldosas";
            paso5.zFin = zFin5 + 0.4f;
            paso5.camino = camino;
            paso5.xCamino = xCamino;
            paso5.zInicioHueco = zHueco5;
            paso5.zFinHueco = zFin5;
            paso5.salida = new Vector3(xCamino, 0f, 57.4f);

            // --- 6. El balancín (z 66-82): uno cruza, el otro hace de contrapeso ---
            var p6 = M.Grupo(carril, "6 Balancin");
            const float zHueco6 = 66f, zFin6 = 82f, zEje = 74f;
            Barredora(p6, $"Barredora ({campo}) 2", new Vector3(lx, 0f, 61.6f), equipo == 0 ? -60f : 60f, m);
            E.Bloque("OrillaMeta", p6, new Vector3(lx, -1f, 90f), new Vector3(10f, 2f, 16f), m.suelo);
            var eje = new GameObject("Balancin");
            eje.transform.SetParent(p6, false);
            eje.transform.position = new Vector3(lx, 0f, zEje);
            eje.AddComponent<Rigidbody>().isKinematic = true;
            E.Bloque("Tabla", eje.transform, new Vector3(0f, -0.18f, 0f), new Vector3(2.6f, 0.4f, 20f), m.madera, local: true);
            E.Bloque("Raya", eje.transform, new Vector3(0f, 0.03f, 0f), new Vector3(2.62f, 0.04f, 0.3f), colorEquipo, local: true);
            E.Pieza(PrimitiveType.Cylinder, "Eje", eje.transform, new Vector3(0f, -0.55f, 0f), new Vector3(0.6f, 1.4f, 0.6f), m.peligro).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var balancin = eje.AddComponent<Balancin>();
            balancin.equipo = equipo;
            balancin.freno = Placa(p6, "Freno", new Vector3(lx + fuera * 2.6f, 0f, zFin6 + 1.5f), equipo, m);
            M.Cartel(p6, "FRENO", new Vector3(lx + fuera * 2.6f, 1.6f, zFin6 + 2.6f), 0.5f, Color.white);
            M.Cartel(p6, "UNO CRUZA, OTRO HACE DE CONTRAPESO", new Vector3(lx, 3.2f, zHueco6 - 1.4f), 0.45f, Color.white);

            var paso6 = p6.gameObject.AddComponent<PasoBalancin>();
            paso6.equipo = equipo;
            paso6.pista = "¡Contrapeso! Uno cruza, el otro al revés; al llegar, pisa el freno";
            paso6.zFin = zFin6 + 0.4f;
            paso6.balancin = balancin;
            paso6.xTabla = lx;
            paso6.zInicioHueco = zHueco6;
            paso6.zFinHueco = zFin6;
            paso6.salida = new Vector3(lx, 0f, 86f);

            // --- 7. El ascensor y la meta ---
            var p7 = M.Grupo(carril, "7 Ascensor");
            E.Bloque("Repisa", p7, new Vector3(lx, 1f, 103f), new Vector3(10f, 6f, 10f), m.pared);
            var ascensorGo = E.Bloque("Ascensor", p7, new Vector3(lx, 0.15f, 95.8f), new Vector3(4f, 0.3f, 4f), m.ascensor);
            ascensorGo.AddComponent<Rigidbody>().isKinematic = true;
            var ascensor = ascensorGo.AddComponent<Ascensor>();
            ascensor.equipo = equipo;
            ascensor.subida = 3.7f;
            M.Cartel(p7, "SUBID LOS DOS", new Vector3(lx, 2.8f, 97.85f), 0.7f, Color.white);
            Empujador(p7, $"Empujador ({campo}) 2", lx, fuera, 92f, 1.6f, m);

            // Meta en lo alto: arco hinchable del equipo, trofeo, cañones de confeti y altavoz
            M.Decorado(PrimitiveType.Cube, "Banderin", p7, new Vector3(lx, 4.02f, 100f), new Vector3(10f, 0.04f, 0.5f), colorEquipo);
            KitObjetos.Poner("ArcoMeta", p7, new Vector3(lx, 4f, 100f), 0f, colorEquipo, kit: KitObjetos.KitPlato);
            Kit("Trofeo", p7, new Vector3(lx, 4f, 106f), 180f, 1.2f);
            Kit("CanonConfeti", p7, new Vector3(lx - fuera * 3.8f, 4f, 107f), 180f - fuera * 20f);
            Kit("CanonConfeti", p7, new Vector3(lx + fuera * 2.4f, 4f, 107.3f), 180f + fuera * 20f);
            Kit("Altavoz", p7, new Vector3(lx + fuera * 4.1f, 4f, 107.3f), 180f);
            M.Cartel(p7, "META", new Vector3(lx, 8f, 107.5f), 1.4f, Color.white);
            var meta = new GameObject("Meta").AddComponent<PlacaPresion>();
            meta.transform.SetParent(p7, false);
            meta.transform.position = new Vector3(lx, 4f, 104f);
            meta.tamano = new Vector3(9f, 3f, 7f);
            meta.equipo = equipo;

            var paso7 = p7.gameObject.AddComponent<PasoAscensor>();
            paso7.equipo = equipo;
            paso7.pista = "¡Subid los dos al ascensor!";
            paso7.ascensor = ascensor;
            paso7.meta = meta;

            // --- Puntos de control: salida y el inicio de cada prueba ---
            var controles = new[]
            {
                Control(carril, "Control0", new Vector3(lx, 0.2f, -42f)),
                Control(carril, "Control1", new Vector3(lx, 0.2f, -19.2f)),
                Control(carril, "Control2", new Vector3(lx - fuera * 1.5f, 0.2f, 1.5f)),
                Control(carril, "Control3", new Vector3(lx, 0.2f, 14.5f)),
                Control(carril, "Control4", new Vector3(lx, 0.2f, 38.8f)),
                Control(carril, "Control5", new Vector3(lx - fuera * 1.8f, 0.2f, 57.2f)),
                Control(carril, "Control6", new Vector3(lx, 0.2f, 86f)),
            };

            return (new PasoCooperativo[] { paso1, paso2, paso3, paso4, paso5, paso6, paso7 }, controles);
        }

        /// <summary>Barredora giratoria del kit (la colisión es el cilindro y el brazo de siempre).</summary>
        static void Barredora(Transform padre, string nombre, Vector3 posicion, float velocidad, Mats m)
        {
            var barredora = new GameObject(nombre);
            barredora.transform.SetParent(padre, false);
            barredora.transform.position = posicion;
            barredora.AddComponent<Rigidbody>().isKinematic = true;
            var giro = barredora.AddComponent<BarraGiratoria>();
            giro.velocidadGiro = velocidad;
            var eje = E.Pieza(PrimitiveType.Cylinder, "Eje", barredora.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.7f, 0.6f, 0.7f), m.peligro, conCollider: true);
            var brazo = E.Bloque("Brazo", barredora.transform, new Vector3(0f, 0.55f, 0f), new Vector3(7f, 0.4f, 0.4f), m.obstaculo, local: true);
            Object.DestroyImmediate(eje.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(brazo.GetComponent<MeshRenderer>());
            var modelo = Kit("Barredora", barredora.transform, Vector3.zero, local: true);
            giro.nombreTrampa = nombre;
            giro.enfriamiento = 8f;
            giro.radioEfecto = 5f;
            giro.alturaMarcador = 3f;
            giro.renderersAviso = modelo.GetComponentsInChildren<Renderer>();
        }

        /// <summary>Compuerta de los pasillos: se hunde mientras se pisa su placa (en el otro pasillo).</summary>
        static Compuerta CompuertaPasillo(Transform padre, string nombre, Vector3 posicion, PlacaPresion placa, int equipo, Material color)
        {
            var go = E.Bloque(nombre, padre, posicion + Vector3.up * 1.5f, new Vector3(2.8f, 3f, 0.4f), color);
            go.AddComponent<Rigidbody>().isKinematic = true;
            var c = go.AddComponent<Compuerta>();
            c.placa = placa;
            c.equipo = equipo;
            return c;
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
            // Un guante de boxeo con pistón: sale desde el borde exterior y empuja hacia el vacío del centro.
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = new Vector3(lx + fuera * 4f, 0.7f, z);
            Vector3 haciaDentro = new Vector3(-fuera, 0f, 0f);
            var caja = go.AddComponent<BoxCollider>();
            caja.size = new Vector3(1.8f, 1.4f, 1.5f);
            caja.center = haciaDentro * 0.18f;
            var guante = Kit("Guante", go.transform, new Vector3(0f, -0.7f, 0f), -fuera * 90f, local: true);
            go.AddComponent<Rigidbody>().isKinematic = true;
            var empujador = go.AddComponent<Empujador>();
            empujador.recorrido = haciaDentro * 4.5f;
            empujador.periodo = 4.5f;
            empujador.desfase = desfase;
            empujador.cuerpoVisible = guante.GetComponentInChildren<Renderer>();
            empujador.nombreTrampa = nombre;
            empujador.enfriamiento = 4f;
            empujador.radioEfecto = 4.5f;
            empujador.alturaMarcador = 2.5f;
        }

        // ------------------------------------------------------------------
        // Plató de tele con el kit (Arte/Blender/crear_kit_plato.py)
        // ------------------------------------------------------------------

        static GameObject Kit(string pieza, Transform padre, Vector3 posicion, float giro = 0f, float escala = 1f, bool local = false) =>
            KitObjetos.Poner(pieza, padre, posicion, giro, escala: escala, local: local, kit: KitObjetos.KitPlato);

        /// <summary>
        /// Gradas con público a los dos lados de los carriles, torres de focos entre ellas,
        /// la pantalla gigante detrás de la meta y una cámara de tele en cada salida.
        /// </summary>
        static void CrearPlato(Transform mapa)
        {
            var plato = M.Grupo(mapa, "Plato");
            var gradas = M.Grupo(plato, "Gradas");
            gradas.gameObject.AddComponent<PublicoAnimado>();

            Color[] coloresPublico =
            {
                new Color(1f, 0.45f, 0.45f), new Color(0.4f, 0.65f, 1f), new Color(1f, 0.8f, 0.3f),
                new Color(0.5f, 0.9f, 0.5f), new Color(0.85f, 0.55f, 1f), new Color(1f, 0.6f, 0.85f),
            };
            var materialesPublico = new Material[coloresPublico.Length];
            for (int i = 0; i < coloresPublico.Length; i++)
                materialesPublico[i] = E.CrearMaterial($"Publico{i}", coloresPublico[i], 0.3f);

            var azar = new System.Random(7);
            foreach (float lado in new[] { -1f, 1f })
            {
                float x = 16.5f * lado;
                float giroGrada = -lado * 90f; // mirando hacia los carriles
                for (int k = 0; k < 16; k++)
                {
                    float z = -38f + 9.5f * k;
                    var grada = Kit("Grada", gradas, new Vector3(x, 0f, z), giroGrada);

                    // Público sentado en las cuatro filas (asientos en local: y = 0.5·(i+1)+0.1, z = 1.35 − 1.1·i).
                    for (int fila = 0; fila < 4; fila++)
                    {
                        for (int s = 0; s < 5; s++)
                        {
                            if (azar.NextDouble() < 0.15) continue; // algún hueco libre
                            float px = -3.2f + 1.6f * s + (float)(azar.NextDouble() - 0.5) * 0.5f;
                            string pieza = azar.NextDouble() < 0.3 ? "PublicoBrazos" : "Publico";
                            var color = materialesPublico[azar.Next(materialesPublico.Length)];
                            KitObjetos.Poner(pieza, grada.transform, new Vector3(px, 0.5f * (fila + 1) + 0.1f, 1.35f - 1.1f * fila),
                                (float)(azar.NextDouble() - 0.5) * 30f, color, 1f + (float)(azar.NextDouble() - 0.5) * 0.2f,
                                local: true, kit: KitObjetos.KitPlato);
                        }
                    }
                }

                // Torres de focos en uno de cada dos huecos entre gradas.
                for (int k = 0; k < 15; k += 2)
                    Kit("TorreFocos", plato, new Vector3(x, 0f, -33.25f + 9.5f * k), giroGrada);
            }

            // Pantalla gigante detrás de las metas (el cartel de "JUNTOS O NADA" va delante).
            Kit("Pantalla", plato, new Vector3(0f, 4f, 111f), 180f);

            // Una cámara de tele en la esquina de dentro de cada salida, enfocando su carril.
            foreach (float lx in new[] { -7f, 7f })
            {
                var pos = new Vector3(lx * 0.4f, 0f, -45f);
                Kit("CamaraTV", plato, pos, KitObjetos.GiroMirando(pos, new Vector3(lx, 0f, -30f)));
            }
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
