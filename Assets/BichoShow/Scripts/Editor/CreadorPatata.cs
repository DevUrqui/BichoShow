using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;
using M = BichoShow.Herramientas.CreadorMudanza;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Menú "BichoShow > Crear escena: Patata caliente".
    /// Un plató redondo que flota en el estudio, con dianas de colores en el suelo, una barredora
    /// girando en el centro (hay que saltarla), tres setas rebotadoras, dos guantes de boxeo en el
    /// borde y gradas con público alrededor. La bomba la reparte RondaPatata.
    /// </summary>
    public static class CreadorPatata
    {
        const string RutaEscena = E.CarpetaEscenas + "/Patata.unity";
        const float Radio = 11f;

        [MenuItem("BichoShow/Crear escena: Patata caliente")]
        public static void Crear()
        {
            var controles = AssetDatabase.LoadAssetAtPath<InputActionAsset>(E.RutaControles);
            if (controles == null)
            {
                EditorUtility.DisplayDialog("Bicho Show", $"No encuentro los controles en:\n{E.RutaControles}", "Vale");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) != null &&
                !EditorUtility.DisplayDialog("Bicho Show", "La escena de la Patata caliente ya existe. ¿Rehacerla?", "Rehacer", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            E.CrearCarpeta(E.Raiz, "Materiales");
            E.CrearCarpeta(E.Raiz, "Prefabs");
            E.CrearCarpeta(E.Raiz, "Escenas");
            var basicos = E.CrearMateriales();
            var bicho = AssetDatabase.LoadAssetAtPath<GameObject>(E.RutaPrefabBicho) ?? E.CrearPrefabBicho(controles, basicos);

            CrearEscena(bicho, basicos.obstaculo);
            Debug.Log("Bicho Show: escena de la Patata caliente creada. Para que el show la use, rehaz también la escena del menú.");
        }

        static GameObject Kit(string pieza, Transform padre, Vector3 posicion, float giro = 0f, float escala = 1f, bool local = false) =>
            KitObjetos.Poner(pieza, padre, posicion, giro, escala: escala, local: local, kit: KitObjetos.KitPlato);

        static void CrearEscena(GameObject prefabBicho, Material obstaculo)
        {
            var suelo = E.CrearMaterial("SueloPatata", new Color(0.88f, 0.84f, 0.97f), 0.3f);
            var anilloRosa = E.CrearMaterial("AnilloPatata", new Color(1f, 0.55f, 0.75f), 0.35f);
            var anilloOro = E.CrearMaterial("AnilloPatataOro", new Color(1f, 0.82f, 0.35f), 0.5f);
            var base_ = E.CrearMaterial("BasePatata", new Color(0.35f, 0.2f, 0.55f), 0.3f);
            var sabotaje = E.CrearMaterial("Sabotaje", new Color(0.62f, 0.3f, 0.9f), 0.5f);
            var peligro = E.CrearMaterial("Peligro", new Color(1f, 0.82f, 0.15f), 0.3f);

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Cámara ---
            var camGo = new GameObject("Camara");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 45f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();
            var camara = camGo.AddComponent<CamaraGrupo>();
            camara.direccion = new Vector3(0f, 1.3f, -1f);
            camara.distanciaMinima = 14f;
            camara.distanciaMaxima = 30f;
            camara.centroSinJugadores = Vector3.zero;
            camara.ColocarAlInstante();

            var mapa = new GameObject("Mapa").transform;

            // --- Plató redondo: disco con colisión de malla, dianas pintadas y pie que baja al vacío ---
            var disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disco.name = "Suelo";
            disco.transform.SetParent(mapa, false);
            disco.transform.position = new Vector3(0f, -0.5f, 0f);
            disco.transform.localScale = new Vector3(Radio * 2f, 0.5f, Radio * 2f);
            disco.GetComponent<Renderer>().sharedMaterial = suelo;
            Object.DestroyImmediate(disco.GetComponent<Collider>());
            disco.AddComponent<MeshCollider>().sharedMesh = disco.GetComponent<MeshFilter>().sharedMesh;

            (float radio, Material mat)[] anillos = { (9.6f, anilloRosa), (8.6f, suelo), (5.6f, anilloOro), (4.8f, suelo), (2.2f, anilloRosa) };
            float y = 0.004f;
            foreach (var (radio, mat) in anillos)
            {
                M.Decorado(PrimitiveType.Cylinder, "Diana", mapa, new Vector3(0f, y, 0f), new Vector3(radio * 2f, 0.004f, radio * 2f), mat);
                y += 0.002f;
            }
            M.Decorado(PrimitiveType.Cylinder, "Borde", mapa, new Vector3(0f, -0.55f, 0f), new Vector3(Radio * 2f + 0.5f, 0.35f, Radio * 2f + 0.5f), anilloOro);
            M.Decorado(PrimitiveType.Cylinder, "Pie", mapa, new Vector3(0f, -6.5f, 0f), new Vector3(7f, 6f, 7f), base_);

            M.Cartel(mapa, "PATATA CALIENTE", new Vector3(0f, 9f, 19.5f), 3.2f, Color.white);
            var vacio = M.ZonaTrigger<ZonaMortal>("ZonaVacio", mapa, new Vector3(0f, -12f, 0f), new Vector3(120f, 4f, 120f));
            vacio.motivoObjeto = "se ha caído al vacío";

            // --- Trampas: barredora en el centro y guantes en el borde ---
            var trampas = M.Grupo(mapa, "Trampas");
            var barredora = new GameObject("Barredora");
            barredora.transform.SetParent(trampas, false);
            barredora.AddComponent<Rigidbody>().isKinematic = true;
            var giro = barredora.AddComponent<BarraGiratoria>();
            giro.velocidadGiro = 45f;
            var eje = E.Pieza(PrimitiveType.Cylinder, "Eje", barredora.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.7f, 0.6f, 0.7f), peligro, conCollider: true);
            var brazo = E.Bloque("Brazo", barredora.transform, new Vector3(0f, 0.55f, 0f), new Vector3(7f, 0.4f, 0.4f), obstaculo, local: true);
            Object.DestroyImmediate(eje.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(brazo.GetComponent<MeshRenderer>());
            var modeloBarredora = Kit("Barredora", barredora.transform, Vector3.zero, local: true);
            giro.nombreTrampa = "Barredora";
            giro.enfriamiento = 8f;
            giro.radioEfecto = 5f;
            giro.alturaMarcador = 3f;
            giro.renderersAviso = modeloBarredora.GetComponentsInChildren<Renderer>();

            for (int i = 0; i < 2; i++)
            {
                float lado = i == 0 ? -1f : 1f;
                var pos = new Vector3(lado * (Radio - 0.6f), 0.7f, i == 0 ? 2.5f : -2.5f);
                Vector3 haciaDentro = new Vector3(-lado, 0f, 0f);
                var go = new GameObject($"Guante {(i == 0 ? "izquierdo" : "derecho")}");
                go.transform.SetParent(trampas, false);
                go.transform.position = pos;
                var caja = go.AddComponent<BoxCollider>();
                caja.size = new Vector3(1.8f, 1.4f, 1.5f);
                caja.center = haciaDentro * 0.18f;
                var guante = Kit("Guante", go.transform, new Vector3(0f, -0.7f, 0f), lado > 0 ? -90f : 90f, local: true);
                go.AddComponent<Rigidbody>().isKinematic = true;
                var empujador = go.AddComponent<Empujador>();
                empujador.recorrido = haciaDentro * 4.5f;
                empujador.periodo = 5f;
                empujador.desfase = i * 2.5f;
                empujador.cuerpoVisible = guante.GetComponentInChildren<Renderer>();
                empujador.nombreTrampa = go.name;
                empujador.enfriamiento = 4f;
                empujador.radioEfecto = 4.5f;
                empujador.alturaMarcador = 2.5f;
            }

            // --- Setas rebotadoras ---
            foreach (float angulo in new[] { 30f, 150f, 270f })
            {
                float a = angulo * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 6.8f;
                var seta = new GameObject("Seta rebotadora");
                seta.transform.SetParent(mapa, false);
                seta.transform.position = pos;
                var modelo = Kit("Seta", seta.transform, Vector3.zero, angulo, 1f, local: true);
                var sombrero = seta.AddComponent<BoxCollider>();
                sombrero.size = new Vector3(1.8f, 0.55f, 1.8f);
                sombrero.center = new Vector3(0f, 0.9f, 0f);
                var tallo = seta.AddComponent<CapsuleCollider>();
                tallo.radius = 0.35f;
                tallo.height = 0.9f;
                tallo.center = new Vector3(0f, 0.45f, 0f);
                var rebotador = seta.AddComponent<Rebotador>();
                rebotador.visual = modelo.transform;
            }

            // --- Plataforma de saboteador (jugando la prueba suelta), en el borde de delante ---
            var cabina = M.Grupo(mapa, "CabinaSaboteador");
            M.Decorado(PrimitiveType.Cube, "Plataforma", cabina, new Vector3(0f, 0.03f, -9.4f), new Vector3(2.4f, 0.06f, 1.6f), sabotaje);
            M.Cartel(cabina, "SABOTEADOR", new Vector3(0f, 2f, -9.9f), 0.45f, Color.white);
            var zona = new GameObject("ZonaSaboteadores");
            zona.transform.SetParent(cabina, false);
            zona.transform.position = new Vector3(0f, 1.5f, -9.4f);
            var trigger = zona.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.4f, 3f, 1.6f);

            CrearPublico(mapa);

            // --- Jugadores: en cruz alrededor del centro, mirando hacia dentro ---
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
            gestor.colores = new[] { new Color(1f, 0.38f, 0.35f), new Color(0.3f, 0.6f, 1f), new Color(1f, 0.68f, 0.2f), new Color(0.45f, 0.9f, 0.95f) };
            var puntos = new List<Transform>();
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + 90f * i) * Mathf.Deg2Rad;
                var p = new GameObject($"Aparicion{i + 1}").transform;
                p.SetParent(jugadoresGo.transform, false);
                p.position = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 6f + Vector3.up * 0.2f;
                p.rotation = Quaternion.LookRotation(-new Vector3(p.position.x, 0f, p.position.z));
                puntos.Add(p);
            }
            gestor.puntosAparicion = puntos.ToArray();

            // --- La bomba (apagada hasta que empieza la ronda) ---
            var bombaGo = new GameObject("Bomba");
            bombaGo.transform.SetParent(mapa, false);
            var modeloBomba = Kit("Bomba", bombaGo.transform, Vector3.zero, 0f, 1.1f, local: true);
            var bomba = bombaGo.AddComponent<Bomba>();
            bomba.modelo = modeloBomba.transform;
            bombaGo.SetActive(false);

            // --- Ronda ---
            var rondaGo = new GameObject("RondaPatata");
            var ronda = rondaGo.AddComponent<RondaPatata>();
            ronda.gestor = gestor;
            ronda.zonaSaboteadores = trigger;
            ronda.saboteadorMaquina = true;
            ronda.agresividadMaquina = 0.35f;
            ronda.bomba = bomba;
            ronda.centroArena = rondaGo.transform;
            ronda.radioArena = Radio - 0.5f;

            Ambientes.Aplicar(Ambientes.Tema.Plato);
            EditorSceneManager.SaveScene(escena, RutaEscena);
            E.AnadirAlBuild(RutaEscena);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rondaGo;
        }

        /// <summary>Gradas con público alrededor (menos por delante, donde está la cámara), focos y pantalla.</summary>
        static void CrearPublico(Transform mapa)
        {
            var plato = M.Grupo(mapa, "Plato");
            var gradas = M.Grupo(plato, "Gradas");
            gradas.gameObject.AddComponent<PublicoAnimado>();

            Color[] colores =
            {
                new Color(1f, 0.45f, 0.45f), new Color(0.4f, 0.65f, 1f), new Color(1f, 0.8f, 0.3f),
                new Color(0.5f, 0.9f, 0.5f), new Color(0.85f, 0.55f, 1f), new Color(1f, 0.6f, 0.85f),
            };
            var materiales = new Material[colores.Length];
            for (int i = 0; i < colores.Length; i++) materiales[i] = E.CrearMaterial($"Publico{i}", colores[i], 0.3f);

            var azar = new System.Random(11);
            foreach (float angulo in new[] { -100f, -60f, -20f, 20f, 60f, 100f })
            {
                float a = angulo * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 17f;
                var grada = Kit("Grada", gradas, pos, KitObjetos.GiroMirando(pos, Vector3.zero));
                for (int fila = 0; fila < 4; fila++)
                    for (int s = 0; s < 5; s++)
                    {
                        if (azar.NextDouble() < 0.15) continue;
                        float px = -3.2f + 1.6f * s + (float)(azar.NextDouble() - 0.5) * 0.5f;
                        KitObjetos.Poner(azar.NextDouble() < 0.3 ? "PublicoBrazos" : "Publico", grada.transform,
                            new Vector3(px, 0.5f * (fila + 1) + 0.1f, 1.35f - 1.1f * fila), (float)(azar.NextDouble() - 0.5) * 30f,
                            materiales[azar.Next(materiales.Length)], 1f + (float)(azar.NextDouble() - 0.5) * 0.2f, true, KitObjetos.KitPlato);
                    }
            }
            foreach (float angulo in new[] { -120f, -40f, 40f, 120f })
            {
                float a = angulo * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 20.5f;
                Kit("TorreFocos", plato, pos, KitObjetos.GiroMirando(pos, Vector3.zero));
            }
            Kit("Pantalla", plato, new Vector3(0f, 4.5f, 21.5f), 180f);
        }
    }
}
