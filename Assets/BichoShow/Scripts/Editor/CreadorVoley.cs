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
    /// Menú "BichoShow > Crear escena: Voley atados".
    /// Una pista de playa flotando en el mar: campo rojo a la izquierda, azul a la derecha,
    /// red en medio (con una pared invisible encima para que los bichos no la salten) y
    /// arena alrededor que cuenta como "fuera".
    /// </summary>
    public static class CreadorVoley
    {
        const string RutaEscena = E.CarpetaEscenas + "/Voley.unity";

        static readonly Color ColorRojo = new Color(1f, 0.38f, 0.35f);
        static readonly Color ColorAzul = new Color(0.3f, 0.6f, 1f);
        static readonly Color ColorArena = new Color(0.96f, 0.86f, 0.62f);

        [MenuItem("BichoShow/Crear escena: Voley atados")]
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
                    "La escena del Voley ya existe. ¿Rehacerla? Se perderán los cambios hechos en ella (el prefab del bicho no se toca).",
                    "Rehacer", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            E.CrearCarpeta(E.Raiz, "Materiales");
            E.CrearCarpeta(E.Raiz, "Prefabs");
            E.CrearCarpeta(E.Raiz, "Escenas");

            var basicos = E.CrearMateriales();
            var bicho = AssetDatabase.LoadAssetAtPath<GameObject>(E.RutaPrefabBicho)
                        ?? E.CrearPrefabBicho(controles, basicos);

            CrearEscena(bicho);
            Debug.Log("Bicho Show: escena del Voley creada. Uníos con un botón y pulsad START (o ENTER) para empezar.");
        }

        static void CrearEscena(GameObject prefabBicho)
        {
            var arena = E.CrearMaterial("Arena", ColorArena, 0.05f);
            var campoRojo = E.CrearMaterial("CampoRojo", Color.Lerp(ColorArena, ColorRojo, 0.22f), 0.05f);
            var campoAzul = E.CrearMaterial("CampoAzul", Color.Lerp(ColorArena, ColorAzul, 0.22f), 0.05f);
            var linea = E.CrearMaterial("Linea", Color.white, 0.1f);
            var red = E.CrearMaterial("Red", new Color(0.92f, 0.92f, 0.95f), 0.2f);
            var poste = E.CrearMaterial("Poste", new Color(0.35f, 0.35f, 0.4f), 0.4f);
            var agua = E.CrearMaterial("Agua", new Color(0.25f, 0.55f, 0.95f), 0.9f);
            var tronco = E.CrearMaterial("Tronco", new Color(0.55f, 0.38f, 0.22f), 0.1f);
            var hojas = E.CrearMaterial("Hojas", new Color(0.3f, 0.75f, 0.35f), 0.2f);
            var pelota = E.CrearMaterial("Pelota", Color.white, 0.5f);
            var sombra = E.CrearMaterial("SombraPelota", new Color(0.12f, 0.1f, 0.12f), 0f);
            var charco = E.CrearMaterial("Aceite", new Color(0.08f, 0.06f, 0.05f), 0.98f);
            var rojo = E.CrearMaterial("EquipoRojo", ColorRojo, 0.3f);
            var azul = E.CrearMaterial("EquipoAzul", ColorAzul, 0.3f);
            var particula = AssetDatabase.LoadAssetAtPath<Material>($"{E.CarpetaMateriales}/Particula.mat");
            if (particula == null)
            {
                particula = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
                AssetDatabase.CreateAsset(particula, $"{E.CarpetaMateriales}/Particula.mat");
            }

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Luz, cámara y ambiente de playa ---
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.8f, 0.85f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.7f, 0.68f, 0.65f);
            RenderSettings.ambientGroundColor = new Color(0.4f, 0.38f, 0.35f);

            var luz = new GameObject("Sol").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.4f;
            luz.shadows = LightShadows.Soft;
            luz.color = new Color(1f, 0.95f, 0.85f);
            luz.transform.rotation = Quaternion.Euler(60f, -25f, 0f);

            var camGo = new GameObject("Camara");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.98f);
            cam.fieldOfView = 45f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();
            var camara = camGo.AddComponent<CamaraGrupo>();
            camara.direccion = new Vector3(0f, 1.35f, -1f);
            camara.distanciaMinima = 17f;
            camara.distanciaMaxima = 32f;
            camara.inclinacionExtraMaxima = 10f;
            camara.centroSinJugadores = Vector3.zero;
            camara.ColocarAlInstante();

            var mapa = new GameObject("Pista").transform;

            // --- Suelos: dos campos y la arena de fuera alrededor ---
            Suelo("CampoRojo", mapa, new Vector3(-5f, -1f, 0f), new Vector3(10f, 2f, 12f), campoRojo, 0);
            Suelo("CampoAzul", mapa, new Vector3(5f, -1f, 0f), new Vector3(10f, 2f, 12f), campoAzul, 1);
            Suelo("FueraAtras", mapa, new Vector3(0f, -1f, -8.5f), new Vector3(30f, 2f, 5f), arena, -1);
            Suelo("FueraDelante", mapa, new Vector3(0f, -1f, 8.5f), new Vector3(30f, 2f, 5f), arena, -1);
            Suelo("FueraIzq", mapa, new Vector3(-12.5f, -1f, 0f), new Vector3(5f, 2f, 12f), arena, -1);
            Suelo("FueraDer", mapa, new Vector3(12.5f, -1f, 0f), new Vector3(5f, 2f, 12f), arena, -1);

            // Líneas del campo
            M.Decorado(PrimitiveType.Cube, "LineaFondoA", mapa, new Vector3(0f, 0.01f, -6f), new Vector3(20.15f, 0.02f, 0.15f), linea);
            M.Decorado(PrimitiveType.Cube, "LineaFondoB", mapa, new Vector3(0f, 0.01f, 6f), new Vector3(20.15f, 0.02f, 0.15f), linea);
            M.Decorado(PrimitiveType.Cube, "LineaIzq", mapa, new Vector3(-10f, 0.01f, 0f), new Vector3(0.15f, 0.02f, 12f), linea);
            M.Decorado(PrimitiveType.Cube, "LineaDer", mapa, new Vector3(10f, 0.01f, 0f), new Vector3(0.15f, 0.02f, 12f), linea);

            // --- Red: la pelota choca con la red; los bichos, además, con una pared invisible encima ---
            var grupoRed = M.Grupo(mapa, "Red");
            foreach (float z in new[] { -6.4f, 6.4f })
                E.Pieza(PrimitiveType.Cylinder, "Poste", grupoRed, new Vector3(0f, 1.3f, z), new Vector3(0.25f, 1.3f, 0.25f), poste, conCollider: true);
            var malla = E.Bloque("Malla", grupoRed, new Vector3(0f, 1.3f, 0f), new Vector3(0.06f, 2f, 12.8f), red);
            malla.GetComponent<BoxCollider>().size = new Vector3(2.5f, 1.15f, 1f); // más gruesa para la física (0.15 m)
            M.Decorado(PrimitiveType.Cube, "Cinta", grupoRed, new Vector3(0f, 2.3f, 0f), new Vector3(0.1f, 0.12f, 12.8f), linea);

            var barrera = new GameObject("BarreraInvisible");
            barrera.transform.SetParent(grupoRed, false);
            barrera.transform.position = new Vector3(0f, 7f, 0f);
            var cajaBarrera = barrera.AddComponent<BoxCollider>();
            cajaBarrera.size = new Vector3(0.3f, 14f, 22f);

            // --- Decorado: mar, palmeras y cartel ---
            M.Decorado(PrimitiveType.Cube, "Mar", mapa, new Vector3(0f, -3f, 0f), new Vector3(220f, 0.2f, 220f), agua);
            foreach (var p in new[] { new Vector3(-14f, 0f, -10f), new Vector3(14f, 0f, -10f), new Vector3(-14f, 0f, 10f), new Vector3(14f, 0f, 10f) })
                Palmera(mapa, p, tronco, hojas);
            M.Cartel(mapa, "VOLEY ATADOS", new Vector3(0f, 7f, 14f), 2.5f, Color.white);

            var vacio = M.ZonaTrigger<ZonaMortal>("ZonaAgua", mapa, new Vector3(0f, -8f, 0f), new Vector3(220f, 4f, 220f));
            vacio.motivoObjeto = "se ha caído al agua";

            // --- Trampas de playa (las activa el saboteador) ---
            var zonaSaboteadores = CrearTrampasYCabina(mapa, agua);

            // --- Jugadores: 1 y 3 en el campo rojo, 2 y 4 en el azul ---
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
            (Vector3 pos, float giro)[] apariciones =
            {
                (new Vector3(-5f, 0.2f, -2.5f), 90f), (new Vector3(5f, 0.2f, -2.5f), -90f),
                (new Vector3(-5f, 0.2f, 2.5f), 90f), (new Vector3(5f, 0.2f, 2.5f), -90f),
            };
            var puntos = new List<Transform>();
            for (int i = 0; i < apariciones.Length; i++)
            {
                var p = new GameObject($"Aparicion{i + 1}").transform;
                p.SetParent(jugadoresGo.transform, false);
                p.position = apariciones[i].pos;
                p.rotation = Quaternion.Euler(0f, apariciones[i].giro, 0f);
                puntos.Add(p);
            }
            gestor.puntosAparicion = puntos.ToArray();

            // --- Ronda ---
            var rondaGo = new GameObject("RondaVoley");
            var ronda = rondaGo.AddComponent<RondaVoley>();
            ronda.gestor = gestor;
            ronda.nombres = new[] { "Rojo", "Azul" };
            ronda.colores = new[] { ColorRojo, ColorAzul };
            ronda.centrosCampo = new[] { Punto(rondaGo.transform, "CentroRojo", new Vector3(-5f, 0f, 0f)), Punto(rondaGo.transform, "CentroAzul", new Vector3(5f, 0f, 0f)) };
            ronda.tamanoCampo = new Vector2(10f, 12f);
            ronda.barreraRed = cajaBarrera;
            ronda.zonaSaboteadores = zonaSaboteadores;
            ronda.saboteadorMaquina = true;
            ronda.agresividadMaquina = 0.35f;
            ronda.materialPelota = pelota;
            ronda.materialSombra = sombra;
            ronda.materialCharco = charco;
            ronda.confeti = M.Particulas("Confeti", rondaGo.transform, new Vector3(0f, 0.3f, 0f),
                ColorRojo, new Color(1f, 0.92f, 0.3f), 120, 5f, 10f, particula);

            EditorSceneManager.SaveScene(escena, RutaEscena);
            E.AnadirAlBuild(RutaEscena);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rondaGo;
        }

        /// <summary>
        /// En cada campo: un cangrejo que lo cruza, un ventilador que sopla a lo ancho y una
        /// ola que lo encharca. Y la plataforma morada para elegir saboteador jugando suelto.
        /// </summary>
        static Collider CrearTrampasYCabina(Transform mapa, Material agua)
        {
            var cangrejoMat = E.CrearMaterial("Cangrejo", new Color(0.95f, 0.32f, 0.2f), 0.45f);
            var ojo = E.CrearMaterial("Ojo", Color.white, 0.8f);
            var pupila = E.CrearMaterial("Pupila", new Color(0.08f, 0.08f, 0.1f), 0.9f);
            var metal = E.CrearMaterial("Ventilador", new Color(0.55f, 0.58f, 0.62f), 0.7f);
            var sabotaje = E.CrearMaterial("Sabotaje", new Color(0.62f, 0.3f, 0.9f), 0.5f);

            var trampas = M.Grupo(mapa, "TrampasPlaya");
            for (int equipo = 0; equipo < 2; equipo++)
            {
                float cx = equipo == 0 ? -5f : 5f;
                string campo = equipo == 0 ? "campo rojo" : "campo azul";

                // Cangrejo: cruza el campo de fondo a fondo y se entierra en la arena de fuera.
                var cangrejoGo = new GameObject($"Cangrejo ({campo})");
                cangrejoGo.transform.SetParent(trampas, false);
                cangrejoGo.transform.position = new Vector3(cx, -1.6f, -8.5f);
                cangrejoGo.AddComponent<Rigidbody>().isKinematic = true;
                var caja = cangrejoGo.AddComponent<BoxCollider>();
                caja.size = new Vector3(1.8f, 0.8f, 1.2f);
                caja.center = new Vector3(0f, 0.4f, 0f);
                var cuerpo = new GameObject("Cuerpo").transform;
                cuerpo.SetParent(cangrejoGo.transform, false);
                var caparazon = E.Pieza(PrimitiveType.Sphere, "Caparazon", cuerpo, new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.7f, 1.1f), cangrejoMat);
                foreach (float lado in new[] { -1f, 1f })
                {
                    E.Pieza(PrimitiveType.Cube, "Pinza", cuerpo, new Vector3(0.95f * lado, 0.5f, 0.55f), new Vector3(0.5f, 0.35f, 0.35f), cangrejoMat);
                    E.Pieza(PrimitiveType.Cylinder, "Tallo", cuerpo, new Vector3(0.25f * lado, 0.85f, 0.35f), new Vector3(0.06f, 0.15f, 0.06f), cangrejoMat);
                    var o = E.Pieza(PrimitiveType.Sphere, "Ojo", cuerpo, new Vector3(0.25f * lado, 1.05f, 0.35f), Vector3.one * 0.22f, ojo);
                    E.Pieza(PrimitiveType.Sphere, "Pupila", o.transform, new Vector3(0f, 0f, 0.38f), Vector3.one * 0.5f, pupila);
                }
                var cangrejo = cangrejoGo.AddComponent<Cangrejo>();
                cangrejo.desde = new Vector3(cx, 0f, -8.5f);
                cangrejo.hasta = new Vector3(cx, 0f, 8.5f);
                cangrejo.cuerpo = cuerpo;
                cangrejo.nombreTrampa = $"Cangrejo ({campo})";
                cangrejo.enfriamiento = 9f;
                cangrejo.radioEfecto = 9f;
                cangrejo.alturaMarcador = 2.5f;
                cangrejo.renderersAviso = new[] { caparazon.GetComponent<Renderer>() };

                // Ventilador: en la arena de atrás, sopla a lo ancho del campo.
                var base_ = E.Bloque($"Ventilador ({campo})", trampas, new Vector3(cx, 0.8f, -10f), new Vector3(1.2f, 1.6f, 0.6f), metal);
                var aspas = new GameObject("Aspas").transform;
                aspas.SetParent(trampas, false);
                aspas.position = new Vector3(cx, 1.9f, -9.6f);
                M.Decorado(PrimitiveType.Cube, "Aspa", aspas, new Vector3(cx, 1.9f, -9.6f), new Vector3(2.4f, 0.28f, 0.05f), metal);
                var aspa2 = M.Decorado(PrimitiveType.Cube, "Aspa", aspas, new Vector3(cx, 1.9f, -9.6f), new Vector3(2.4f, 0.28f, 0.05f), metal);
                aspa2.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                var viento = base_.AddComponent<RafagaViento>();
                viento.direccion = Vector3.forward;
                viento.centroZona = new Vector3(cx, 3f, 0f);
                viento.tamanoZona = new Vector3(10f, 6f, 12f);
                viento.aspas = aspas;
                viento.nombreTrampa = $"Viento ({campo})";
                viento.enfriamiento = 8f;
                viento.radioEfecto = 7f;
                viento.alturaMarcador = 0f;
                viento.renderersAviso = new[] { base_.GetComponent<Renderer>() };

                // Ola: entra desde el mar (el lado de fuera) y encharca el campo.
                var olaGo = new GameObject($"Ola ({campo})");
                olaGo.transform.SetParent(trampas, false);
                olaGo.transform.position = new Vector3(cx, 0f, 0f);
                var lamina = M.Decorado(PrimitiveType.Cube, "Agua", olaGo.transform, new Vector3(cx, -0.2f, 0f), new Vector3(10f, 0.08f, 12f), agua);
                lamina.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                lamina.SetActive(false);
                var ola = olaGo.AddComponent<Ola>();
                ola.centroZona = new Vector3(cx, 0f, 0f);
                ola.tamano = new Vector2(10f, 12f);
                ola.direccion = equipo == 0 ? Vector3.right : Vector3.left;
                ola.agua = lamina.transform;
                ola.nombreTrampa = $"Ola ({campo})";
                ola.enfriamiento = 10f;
                ola.radioEfecto = 7f;
                ola.alturaMarcador = 3f;
            }

            // Plataforma de saboteador (para jugar el voley suelto): en una esquina de la arena.
            var cabina = M.Grupo(mapa, "CabinaSaboteador");
            M.Decorado(PrimitiveType.Cube, "Plataforma", cabina, new Vector3(-12.5f, 0.03f, -8.5f), new Vector3(3f, 0.06f, 3f), sabotaje);
            M.Cartel(cabina, "SABOTEADOR", new Vector3(-12.5f, 2.2f, -8.5f), 0.5f, Color.white);
            var zona = new GameObject("ZonaSaboteadores");
            zona.transform.SetParent(cabina, false);
            zona.transform.position = new Vector3(-12.5f, 1.5f, -8.5f);
            var trigger = zona.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3f, 3f, 3f);
            return trigger;
        }

        static void Suelo(string nombre, Transform padre, Vector3 posicion, Vector3 escala, Material material, int equipo)
        {
            var suelo = E.Bloque(nombre, padre, posicion, escala, material);
            suelo.AddComponent<ZonaPista>().equipo = equipo;
        }

        static Transform Punto(Transform padre, string nombre, Vector3 posicion)
        {
            var p = new GameObject(nombre).transform;
            p.SetParent(padre, false);
            p.position = posicion;
            return p;
        }

        static void Palmera(Transform padre, Vector3 pie, Material tronco, Material hojas)
        {
            var palmera = M.Grupo(padre, "Palmera");
            for (int i = 0; i < 5; i++)
            {
                var trozo = M.Decorado(PrimitiveType.Cylinder, "Tronco", palmera,
                    pie + new Vector3(i * 0.12f, 0.5f + i * 0.95f, 0f), new Vector3(0.45f - i * 0.04f, 0.5f, 0.45f - i * 0.04f), tronco);
                trozo.transform.rotation = Quaternion.Euler(0f, 0f, -6f);
            }
            Vector3 copa = pie + new Vector3(0.6f, 5f, 0f);
            for (int i = 0; i < 6; i++)
            {
                var hoja = M.Decorado(PrimitiveType.Cube, "Hoja", palmera, copa, new Vector3(2.6f, 0.08f, 0.6f), hojas);
                hoja.transform.rotation = Quaternion.Euler(0f, i * 60f, -20f);
                hoja.transform.position = copa + hoja.transform.right * 1.1f;
            }
        }
    }
}
