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
    /// Piezas comunes de las pruebas "de plató" (Patata caliente, La Corona, Lluvia de muebles,
    /// Pintabichos): cámara, jugadores, plataforma de saboteador, público alrededor, vacío,
    /// discos con colisión, barredora, guantes de boxeo y setas rebotadoras.
    /// </summary>
    static class CreadorArena
    {
        internal static readonly Color[] ColoresJugadores =
        {
            new Color(1f, 0.38f, 0.35f), new Color(0.3f, 0.6f, 1f), new Color(1f, 0.68f, 0.2f), new Color(0.45f, 0.9f, 0.95f),
        };

        /// <summary>Comprueba los controles, pregunta si rehacer y guarda lo abierto. Devuelve el prefab del bicho (o null si se cancela).</summary>
        internal static GameObject Preparar(string rutaEscena, string nombrePrueba, out Material obstaculo)
        {
            obstaculo = null;
            var controles = AssetDatabase.LoadAssetAtPath<InputActionAsset>(E.RutaControles);
            if (controles == null)
            {
                EditorUtility.DisplayDialog("Bicho Show", $"No encuentro los controles en:\n{E.RutaControles}", "Vale");
                return null;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(rutaEscena) != null &&
                !EditorUtility.DisplayDialog("Bicho Show", $"La escena de {nombrePrueba} ya existe. ¿Rehacerla?", "Rehacer", "Cancelar"))
                return null;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;

            E.CrearCarpeta(E.Raiz, "Materiales");
            E.CrearCarpeta(E.Raiz, "Prefabs");
            E.CrearCarpeta(E.Raiz, "Escenas");
            var basicos = E.CrearMateriales();
            obstaculo = basicos.obstaculo;
            return AssetDatabase.LoadAssetAtPath<GameObject>(E.RutaPrefabBicho) ?? E.CrearPrefabBicho(controles, basicos);
        }

        internal static void Guardar(UnityEngine.SceneManagement.Scene escena, string rutaEscena, GameObject seleccion)
        {
            Ambientes.Aplicar(Ambientes.Tema.Plato);
            EditorSceneManager.SaveScene(escena, rutaEscena);
            E.AnadirAlBuild(rutaEscena);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = seleccion;
        }

        internal static GameObject Kit(string pieza, Transform padre, Vector3 posicion, float giro = 0f, float escala = 1f, bool local = false) =>
            KitObjetos.Poner(pieza, padre, posicion, giro, escala: escala, local: local, kit: KitObjetos.KitPlato);

        internal static CamaraGrupo Camara(float distanciaMinima = 14f, float distanciaMaxima = 30f)
        {
            var camGo = new GameObject("Camara");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 45f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();
            var camara = camGo.AddComponent<CamaraGrupo>();
            camara.direccion = new Vector3(0f, 1.3f, -1f);
            camara.distanciaMinima = distanciaMinima;
            camara.distanciaMaxima = distanciaMaxima;
            camara.centroSinJugadores = Vector3.zero;
            camara.ColocarAlInstante();
            return camara;
        }

        /// <summary>Gestor de jugadores con sus puntos de aparición, mirando hacia "mirar".</summary>
        internal static GestorJugadores Jugadores(GameObject prefabBicho, Vector3[] apariciones, Vector3 mirar)
        {
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
            gestor.colores = (Color[])ColoresJugadores.Clone();
            var puntos = new List<Transform>();
            for (int i = 0; i < apariciones.Length; i++)
            {
                var p = new GameObject($"Aparicion{i + 1}").transform;
                p.SetParent(jugadoresGo.transform, false);
                p.position = apariciones[i] + Vector3.up * 0.2f;
                Vector3 hacia = mirar - apariciones[i];
                hacia.y = 0f;
                p.rotation = hacia.sqrMagnitude > 0.01f ? Quaternion.LookRotation(hacia) : Quaternion.identity;
                puntos.Add(p);
            }
            gestor.puntosAparicion = puntos.ToArray();
            return gestor;
        }

        /// <summary>Puntos en círculo (para apariciones).</summary>
        internal static Vector3[] EnCirculo(int cuantos, float radio, float anguloInicial = 45f)
        {
            var puntos = new Vector3[cuantos];
            for (int i = 0; i < cuantos; i++)
            {
                float a = (anguloInicial + 360f * i / cuantos) * Mathf.Deg2Rad;
                puntos[i] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radio;
            }
            return puntos;
        }

        /// <summary>Plataforma morada: quien esté encima al pulsar START sabotea (jugando la prueba suelta).</summary>
        internal static Collider CabinaSaboteador(Transform mapa, Vector3 posicion)
        {
            var sabotaje = E.CrearMaterial("Sabotaje", new Color(0.62f, 0.3f, 0.9f), 0.5f);
            var cabina = M.Grupo(mapa, "CabinaSaboteador");
            M.Decorado(PrimitiveType.Cube, "Plataforma", cabina, posicion + Vector3.up * 0.03f, new Vector3(2.4f, 0.06f, 1.6f), sabotaje);
            M.Cartel(cabina, "SABOTEADOR", posicion + new Vector3(0f, 2f, -0.5f), 0.45f, Color.white);
            var zona = new GameObject("ZonaSaboteadores");
            zona.transform.SetParent(cabina, false);
            zona.transform.position = posicion + Vector3.up * 1.5f;
            var trigger = zona.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.4f, 3f, 1.6f);
            return trigger;
        }

        internal static void Vacio(Transform mapa)
        {
            var vacio = M.ZonaTrigger<ZonaMortal>("ZonaVacio", mapa, new Vector3(0f, -12f, 0f), new Vector3(140f, 4f, 140f));
            vacio.motivoObjeto = "se ha caído al vacío";
        }

        /// <summary>Disco (cilindro) con colisión de malla y la cara de arriba a la altura "arriba".</summary>
        internal static GameObject Disco(Transform padre, string nombre, Vector3 centro, float arriba, float radio, float alto, Material material)
        {
            var disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disco.name = nombre;
            disco.transform.SetParent(padre, false);
            disco.transform.position = new Vector3(centro.x, arriba - alto * 0.5f, centro.z);
            disco.transform.localScale = new Vector3(radio * 2f, alto * 0.5f, radio * 2f);
            disco.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(disco.GetComponent<Collider>());
            disco.AddComponent<MeshCollider>().sharedMesh = disco.GetComponent<MeshFilter>().sharedMesh;
            return disco;
        }

        /// <summary>Barredora giratoria (colisión de cilindro y brazo; se ve el modelo de espuma).</summary>
        internal static BarraGiratoria Barredora(Transform padre, Vector3 posicion, float velocidad, string nombre, Material obstaculo)
        {
            var peligro = E.CrearMaterial("Peligro", new Color(1f, 0.82f, 0.15f), 0.3f);
            var barredora = new GameObject(nombre);
            barredora.transform.SetParent(padre, false);
            barredora.transform.position = posicion;
            barredora.AddComponent<Rigidbody>().isKinematic = true;
            var giro = barredora.AddComponent<BarraGiratoria>();
            giro.velocidadGiro = velocidad;
            var eje = E.Pieza(PrimitiveType.Cylinder, "Eje", barredora.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.7f, 0.6f, 0.7f), peligro, conCollider: true);
            var brazo = E.Bloque("Brazo", barredora.transform, new Vector3(0f, 0.55f, 0f), new Vector3(7f, 0.4f, 0.4f), obstaculo, local: true);
            Object.DestroyImmediate(eje.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(brazo.GetComponent<MeshRenderer>());
            var modelo = Kit("Barredora", barredora.transform, Vector3.zero, local: true);
            giro.nombreTrampa = nombre;
            giro.enfriamiento = 8f;
            giro.radioEfecto = 5f;
            giro.alturaMarcador = 3f;
            giro.renderersAviso = modelo.GetComponentsInChildren<Renderer>();
            return giro;
        }

        /// <summary>Guante de boxeo con pistón que sale hacia "haciaDentro" cada cierto tiempo.</summary>
        internal static Empujador Guante(Transform padre, string nombre, Vector3 posicion, Vector3 haciaDentro, float desfase, float recorrido = 4.5f)
        {
            haciaDentro.y = 0f;
            haciaDentro.Normalize();
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = posicion + Vector3.up * 0.7f;
            var caja = go.AddComponent<BoxCollider>();
            caja.size = new Vector3(Mathf.Lerp(1.5f, 1.8f, Mathf.Abs(haciaDentro.x)), 1.4f, Mathf.Lerp(1.5f, 1.8f, Mathf.Abs(haciaDentro.z)));
            caja.center = haciaDentro * 0.18f;
            var guante = Kit("Guante", go.transform, new Vector3(0f, -0.7f, 0f), Mathf.Atan2(haciaDentro.x, haciaDentro.z) * Mathf.Rad2Deg, local: true);
            go.AddComponent<Rigidbody>().isKinematic = true;
            var empujador = go.AddComponent<Empujador>();
            empujador.recorrido = haciaDentro * recorrido;
            empujador.periodo = 5f;
            empujador.desfase = desfase;
            empujador.cuerpoVisible = guante.GetComponentInChildren<Renderer>();
            empujador.nombreTrampa = nombre;
            empujador.enfriamiento = 4f;
            empujador.radioEfecto = 4.5f;
            empujador.alturaMarcador = 2.5f;
            return empujador;
        }

        /// <summary>Seta rebotadora: quien la toca sale disparado hacia arriba.</summary>
        internal static Rebotador Seta(Transform padre, Vector3 posicion, float giro = 0f)
        {
            var seta = new GameObject("Seta rebotadora");
            seta.transform.SetParent(padre, false);
            seta.transform.position = posicion;
            var modelo = Kit("Seta", seta.transform, Vector3.zero, giro, 1f, local: true);
            var sombrero = seta.AddComponent<BoxCollider>();
            sombrero.size = new Vector3(1.8f, 0.55f, 1.8f);
            sombrero.center = new Vector3(0f, 0.9f, 0f);
            var tallo = seta.AddComponent<CapsuleCollider>();
            tallo.radius = 0.35f;
            tallo.height = 0.9f;
            tallo.center = new Vector3(0f, 0.45f, 0f);
            var rebotador = seta.AddComponent<Rebotador>();
            rebotador.visual = modelo.transform;
            return rebotador;
        }

        /// <summary>
        /// Gradas con público alrededor del centro (en los ángulos dados; 0 = detrás, al fondo de
        /// la cámara), torres de focos y la pantalla gigante al fondo con el nombre de la prueba.
        /// </summary>
        internal static void Publico(Transform mapa, float radio, float[] angulosGradas, float[] angulosFocos, string titulo)
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

            var azar = new System.Random(titulo.GetHashCode());
            foreach (float angulo in angulosGradas)
            {
                float a = angulo * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radio;
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
            foreach (float angulo in angulosFocos)
            {
                float a = angulo * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (radio + 3.5f);
                Kit("TorreFocos", plato, pos, KitObjetos.GiroMirando(pos, Vector3.zero));
            }
            Kit("Pantalla", plato, new Vector3(0f, 4.5f, radio + 4.5f), 180f);
            M.Cartel(mapa, titulo, new Vector3(0f, 9f, radio + 2.5f), 3.2f, Color.white);
        }
    }
}
