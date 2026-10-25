using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;
using M = BichoShow.Herramientas.CreadorMudanza;
using A = BichoShow.Herramientas.CreadorArena;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Menú "BichoShow > Crear escena: El Cortacésped Mecha". Arena circular en un jardín de
    /// noche convertido en plató:
    ///  - En el centro, el robot cortacésped (CortacespedMecha) sobre su isla, con la pantalla LED
    ///    de la cara, la turbina arriba y la boca que escupe tuercas.
    ///  - Alrededor de la isla, el foso de chatarra (r 4,2-7): quien cae, vuelve y pierde el punto.
    ///  - El césped donde se juega: un anillo de colores (r 7-22) sin barandillas.
    ///  - Fuera: gradas con público, focos, dientes de león y hierba gigante, y el letrero de neón.
    /// Las porciones del césped son cinemáticas (el robot las tira en el "corte de césped"); los
    /// aros del láser (bajos y altos) y las cuchillas del barrido están apagados hasta que atacan.
    /// Los efectos del viento y del láser usan mallas y texturas hechas en Blender
    /// (Arte/Blender/crear_efectos_cortacesped.py → Arte/Efectos y Texturas/Efectos).
    /// </summary>
    public static class CreadorCortacesped
    {
        public const string RutaEscena = E.CarpetaEscenas + "/Cortacesped.unity";
        /// <summary>Sube al cambiar el mapa: al darle a Play se avisa si la escena es de antes (EscenasAnticuadas).</summary>
        public const int Version = 7;
        const string CarpetaMallas = E.Raiz + "/Modelos/Cortacesped";
        const float RadioRobot = 4.2f, RadioFoso = 7f, RadioPista = 22f, FondoFoso = -5f, Grosor = 0.8f;

        static Transform mapa, decorado;
        static readonly List<Rigidbody> porciones = new List<Rigidbody>();
        static readonly Dictionary<string, Material> materiales = new Dictionary<string, Material>();

        [MenuItem("BichoShow/Crear escena: El Cortacésped Mecha")]
        public static void Crear()
        {
            var bicho = A.Preparar(RutaEscena, "El Cortacésped Mecha", out _);
            if (bicho == null) return;
            E.CrearCarpeta(E.Raiz, "Modelos");
            E.CrearCarpeta(E.Raiz + "/Modelos", "Cortacesped");
            CrearEscena(bicho);
            Debug.Log("Bicho Show: escena de El Cortacésped Mecha creada. Ya entra en el show.");
        }

        static Material Mat(string nombre, Color color, float brillo = 0.3f)
        {
            if (!materiales.TryGetValue(nombre, out var m) || m == null) materiales[nombre] = m = E.CrearMaterial("Cortacesped" + nombre, color, brillo);
            return m;
        }

        static Material Neon(string nombre, Color color, float intensidad = 2.5f)
        {
            var m = Mat(nombre, color, 0.6f);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", color * intensidad);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void CrearEscena(GameObject prefabBicho)
        {
            materiales.Clear();
            porciones.Clear();
            mallasBlender = null; // por si se han vuelto a generar en Blender
            mallasBlender = null; // por si se han vuelto a generar en Blender
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camara = A.Camara(22f, 46f);
            camara.centroSinJugadores = Vector3.zero;
            camara.ColocarAlInstante();
            mapa = new GameObject("Mapa").transform;
            var suelos = M.Grupo(mapa, "Suelos");
            decorado = M.Grupo(mapa, "Decorado");

            // =================================================================
            // El césped: un anillo de colores (12 porciones) del foso al borde
            // =================================================================
            Color[] colores =
            {
                new Color(0.45f, 0.82f, 0.38f), new Color(1f, 0.82f, 0.32f), new Color(0.38f, 0.62f, 0.98f),
                new Color(0.98f, 0.52f, 0.7f), new Color(1f, 0.6f, 0.3f), new Color(0.7f, 0.56f, 0.96f),
            };
            for (int i = 0; i < 12; i++)
            {
                var m = Mat($"Cesped{i % 6}", colores[i % 6], 0.25f);
                var porcion = Pieza($"Cesped {i + 1}", suelos, Anillo($"Porcion{i}", RadioFoso, RadioPista, i * 30f, (i + 1) * 30f, 0f, -Grosor), m, true);
                var rb = porcion.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                porciones.Add(rb); // el robot las tira en el corte de césped
                // Su trozo del borde de neón exterior (se mueve con ella).
                Pieza("Borde exterior", porcion.transform, Anillo($"BordeExterior{i}", RadioPista - 0.35f, RadioPista, i * 30f, (i + 1) * 30f, 0.03f, 0.01f), Neon("NeonCian", new Color(0.3f, 0.9f, 1f)), false);
            }
            // Las rayas del cortacésped (más claras) y los bordes de neón.
            Pieza("Borde del foso", decorado, Anillo("BordeFoso", RadioFoso, RadioFoso + 0.35f, 0f, 360f, 0.03f, 0.01f), Neon("NeonRosa", new Color(1f, 0.3f, 0.75f)), false);
            var raya = Mat("Raya", new Color(1f, 1f, 1f), 0.2f);
            for (int i = 0; i < 12; i++)
                Pieza("Raya", decorado, Anillo($"Raya{i}", RadioFoso + 0.5f, RadioPista - 0.5f, i * 30f - 0.4f, i * 30f + 0.4f, 0.02f, 0.01f), raya, false);

            // =================================================================
            // La isla del robot y el foso de chatarra
            // =================================================================
            var metal = Mat("Metal", new Color(0.5f, 0.52f, 0.58f), 0.6f);
            A.Disco(suelos, "Isla", Vector3.zero, 0f, RadioRobot, -FondoFoso + 1f, metal);
            A.Disco(suelos, "Fondo del foso", Vector3.zero, FondoFoso, RadioFoso + 0.5f, 1f, Mat("FondoFoso", new Color(0.18f, 0.16f, 0.2f), 0.2f));
            Pieza("Pared del foso", decorado, Tubo("ParedFoso", RadioFoso, FondoFoso, -Grosor), Mat("ParedFoso", new Color(0.32f, 0.26f, 0.22f), 0.1f), false);
            Chatarra();
            var vacio = M.ZonaTrigger<ZonaMortal>("ZonaCaida", mapa, new Vector3(0f, -3.6f, 0f), new Vector3(160f, 1.5f, 160f));
            vacio.motivoObjeto = "se ha caído";

            // =================================================================
            // El robot
            // =================================================================
            var robot = Robot();

            // =================================================================
            // El jardín de noche y el plató
            // =================================================================
            A.Disco(decorado, "Jardin", Vector3.zero, -9f, 90f, 1f, Mat("Jardin", new Color(0.12f, 0.3f, 0.16f), 0.1f));
            A.Publico(mapa, 31f, new[] { -140f, -100f, -60f, -20f, 20f, 60f, 100f, 140f }, new[] { -135f, -45f, 45f, 135f }, "EL CORTACÉSPED MECHA");
            Jardin();
            Letrero();
            Focos();

            var zonaSaboteadores = A.CabinaSaboteador(mapa, new Vector3(16f, 0f, -12f));
            var gestor = A.Jugadores(prefabBicho, A.EnCirculo(4, 14f, 225f), Vector3.zero);

            var rondaGo = new GameObject("RondaCortacesped");
            var ronda = rondaGo.AddComponent<RondaCortacesped>();
            ronda.gestor = gestor;
            ronda.robot = robot;
            ronda.zonaSaboteadores = zonaSaboteadores;
            ronda.saboteadorMaquina = false;
            new GameObject(EscenasAnticuadas.Marca + Version);

            Ambientes.Aplicar(Ambientes.Tema.JardinNoche);
            EditorSceneManager.SaveScene(escena, RutaEscena);
            E.AnadirAlBuild(RutaEscena);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rondaGo;
        }

        // ---------------------------------------------------------------------
        // El robot cortacésped
        // ---------------------------------------------------------------------

        static CortacespedMecha Robot()
        {
            var verde = Mat("RobotVerde", new Color(0.36f, 0.72f, 0.4f), 0.45f);
            var rojo = Mat("RobotRojo", new Color(0.9f, 0.26f, 0.24f), 0.45f);
            var goma = Mat("Goma", new Color(0.1f, 0.1f, 0.12f), 0.15f);
            var gris = Mat("Gris", new Color(0.62f, 0.64f, 0.68f), 0.7f);
            var amarillo = Neon("Faro", new Color(1f, 0.92f, 0.55f), 2f);

            var raiz = new GameObject("Cortacesped Mecha");
            raiz.transform.SetParent(mapa, false);
            var mecha = raiz.AddComponent<CortacespedMecha>();
            mecha.radioRobot = RadioRobot;
            mecha.radioFoso = RadioFoso;
            mecha.radioPista = RadioPista;

            // Colisión fija (el robot es redondo por fuera: no hace falta que gire): parachoques y cuerpo.
            var choque = A.Disco(raiz.transform, "Choque (colision)", Vector3.zero, 6.5f, RadioRobot - 0.2f, 6.5f, rojo);
            Object.DestroyImmediate(choque.GetComponent<MeshRenderer>());
            A.Disco(raiz.transform, "Parachoques acolchado", Vector3.zero, 1.5f, RadioRobot - 0.1f, 1.1f, rojo);
            var aroBase = Pieza("Emisor laser", raiz.transform, Anillo("EmisorLaser", RadioRobot - 0.05f, RadioRobot + 0.1f, 0f, 360f, 0.5f, 0.2f), Neon("Laser", new Color(1f, 0.25f, 0.75f), 3f), false);

            // Lo que gira.
            var cuerpo = new GameObject("Cuerpo").transform;
            cuerpo.SetParent(raiz.transform, false);
            mecha.cuerpo = cuerpo;
            Bloque("Chasis", cuerpo, new Vector3(0f, 3f, 0f), new Vector3(5.4f, 2.6f, 5.2f), verde);
            Bloque("Capota", cuerpo, new Vector3(0f, 4.6f, 0.4f), new Vector3(4.6f, 0.8f, 4.2f), rojo);
            foreach (float lado in new[] { -1f, 1f })
            {
                Bloque("Oruga", cuerpo, new Vector3(lado * 3.1f, 1.9f, 0f), new Vector3(1.3f, 2.2f, 5.6f), goma);
                for (int k = -1; k <= 1; k++)
                {
                    var rueda = E.Pieza(PrimitiveType.Cylinder, "Rueda", cuerpo, new Vector3(lado * 3.8f, 1.9f, k * 1.8f), new Vector3(1.4f, 0.12f, 1.4f), gris);
                    rueda.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
                E.Pieza(PrimitiveType.Sphere, "Faro", cuerpo, new Vector3(lado * 1.7f, 2.2f, -2.7f), new Vector3(0.6f, 0.6f, 0.3f), amarillo);
            }

            // La pantalla LED de la cara (una por expresión; solo se ve la que toca).
            Bloque("Marco pantalla", cuerpo, new Vector3(0f, 3.5f, -2.55f), new Vector3(4.4f, 2.8f, 0.5f), goma);
            string[] nombres = { "Normal", "Enfadado", "Risa", "Mareado", "Laser", "Furia", "Corte" }; // en el orden de CortacespedMecha.Cara
            var caras = new GameObject[nombres.Length];
            for (int i = 0; i < nombres.Length; i++)
            {
                var cara = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cara.name = $"Cara {nombres[i]}";
                Object.DestroyImmediate(cara.GetComponent<Collider>());
                cara.transform.SetParent(cuerpo, false);
                cara.transform.localPosition = new Vector3(0f, 3.5f, -2.83f);
                cara.transform.localScale = new Vector3(3.9f, 2.3f, 1f);
                cara.GetComponent<Renderer>().sharedMaterial = PantallaLed(nombres[i]);
                cara.SetActive(i == 0);
                caras[i] = cara;
            }
            mecha.caras = caras;

            // La boca (rejilla) de donde salen las tuercas.
            Bloque("Rejilla", cuerpo, new Vector3(0f, 1.6f, -2.75f), new Vector3(2.4f, 0.9f, 0.4f), goma);
            var boca = new GameObject("Boca").transform;
            boca.SetParent(cuerpo, false);
            boca.localPosition = new Vector3(0f, 2f, -4.6f); // por fuera de la colisión del robot
            boca.localRotation = Quaternion.Euler(0f, 180f, 0f); // mira hacia delante (-Z)
            mecha.boca = boca;

            // La turbina de arriba (gira deprisa al aspirar) y la antena.
            var carcasa = E.Pieza(PrimitiveType.Cylinder, "Carcasa turbina", cuerpo, new Vector3(0f, 5.3f, 1.2f), new Vector3(2.8f, 0.35f, 2.8f), gris);
            var turbina = new GameObject("Turbina").transform;
            turbina.SetParent(cuerpo, false);
            turbina.localPosition = new Vector3(0f, 5.75f, 1.2f);
            for (int k = 0; k < 4; k++)
            {
                var aspa = E.Pieza(PrimitiveType.Cube, "Aspa", turbina, Vector3.zero, new Vector3(2.4f, 0.08f, 0.45f), rojo);
                aspa.transform.localRotation = Quaternion.Euler(12f, k * 45f, 0f);
            }
            mecha.turbina = turbina;
            E.Pieza(PrimitiveType.Cylinder, "Antena", cuerpo, new Vector3(1.6f, 5.8f, 1.6f), new Vector3(0.12f, 0.9f, 0.12f), gris);
            E.Pieza(PrimitiveType.Sphere, "Luz antena", cuerpo, new Vector3(1.6f, 6.8f, 1.6f), Vector3.one * 0.4f, Neon("LuzAntena", new Color(1f, 0.3f, 0.3f), 3f));
            foreach (var c in cuerpo.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

            // Los aros del láser (crecen al lanzarse): una pared fina de luz, apagados.
            var aros = new Transform[3];
            var mallaAro = Tubo("AroLaser", 1f, 0.05f, 0.65f, 24f);
            var brillo = TexturaEfecto("Brillo", false);
            for (int i = 0; i < aros.Length; i++)
            {
                var aro = Pieza($"Aro laser {i + 1}", raiz.transform, mallaAro, MaterialEfecto("AroLaser", brillo, new Color(2.2f, 0.5f, 1.6f, 1f), true), false);
                aro.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                aro.SetActive(false);
                aros[i] = aro.transform;
            }
            mecha.aros = aros;
            // Los altos (azules): te dan si saltas.
            var arosAltos = new Transform[3];
            var mallaAlta = Tubo("AroLaserAlto", 1f, 1.25f, 1.85f, 24f);
            for (int i = 0; i < arosAltos.Length; i++)
            {
                var aro = Pieza($"Aro laser alto {i + 1}", raiz.transform, mallaAlta, MaterialEfecto("AroLaserAlto", brillo, new Color(0.5f, 1.6f, 2.2f, 1f), true), false);
                aro.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                aro.SetActive(false);
                arosAltos[i] = aro.transform;
            }
            mecha.arosAltos = arosAltos;

            // Las cuchillas del barrido: hojas largas a ras de suelo (pivote en el centro, hacia +X).
            var hojaRoja = Mat("Cuchilla", new Color(0.85f, 0.2f, 0.2f), 0.6f);
            var hojaBlanca = Mat("CuchillaBlanca", new Color(0.95f, 0.95f, 0.95f), 0.6f);
            var filo = Neon("Filo", new Color(1f, 0.55f, 0.25f), 2.5f);
            var cuchillas = new Transform[2];
            float largo = RadioPista + 0.6f - (RadioRobot - 0.2f);
            for (int i = 0; i < cuchillas.Length; i++)
            {
                var pivote = new GameObject($"Cuchilla {i + 1}").transform;
                pivote.SetParent(raiz.transform, false);
                float centro = RadioRobot - 0.2f + largo * 0.5f;
                E.Pieza(PrimitiveType.Cube, "Hoja", pivote, new Vector3(centro, 0.45f, 0f), new Vector3(largo, 0.45f, 0.35f), hojaRoja);
                for (float x = RadioRobot + 1f; x < RadioPista; x += 2f)
                    E.Pieza(PrimitiveType.Cube, "Franja", pivote, new Vector3(x, 0.45f, 0f), new Vector3(0.7f, 0.47f, 0.37f), hojaBlanca);
                E.Pieza(PrimitiveType.Cube, "Filo", pivote, new Vector3(centro, 0.45f, -0.2f), new Vector3(largo, 0.12f, 0.06f), filo);
                foreach (var c in pivote.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                pivote.gameObject.SetActive(false);
                cuchillas[i] = pivote;
            }
            mecha.cuchillas = cuchillas;

            // El viento de la aspiración: rachas en el aire y flechas en el suelo (apagadas).
            var viento = M.Grupo(raiz.transform, "Viento");
            var mallaRacha = MallaBlender("RachaViento");
            var materialRacha = MaterialEfecto("Racha", TexturaEfecto("Racha", false), new Color(1.3f, 1.8f, 2.2f, 1f), true);
            var rafagas = new Transform[30];
            for (int i = 0; i < rafagas.Length; i++)
            {
                var r = Pieza("Racha", viento, mallaRacha, materialRacha, false);
                r.transform.localScale = Vector3.one * 1.9f; // a la distancia de juego, las finas no se veían
                r.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.SetActive(false);
                rafagas[i] = r.transform;
            }
            mecha.rafagas = rafagas;

            var mallaRecorte = MallaBlender("Recorte");
            var materialRecorte = MaterialEfecto("Recorte", TexturaEfecto("Hoja", false), new Color(0.5f, 0.9f, 0.35f, 1f), false);
            var recortes = new Transform[20];
            for (int i = 0; i < recortes.Length; i++)
            {
                var h = Pieza("Recorte de cesped", viento, mallaRecorte, materialRecorte, false);
                h.transform.localScale = Vector3.one * 1.6f;
                h.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                h.SetActive(false);
                recortes[i] = h.transform;
            }
            mecha.recortes = recortes;

            var remolino = Pieza("Remolino", raiz.transform, MallaBlender("Remolino"),
                                 MaterialEfecto("Remolino", TexturaEfecto("Remolino", true), new Color(0.8f, 1.4f, 1.9f, 0.85f), true), false);
            remolino.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            remolino.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mecha.remolino = remolino.GetComponent<Renderer>();
            var embudo = Pieza("Ciclon", raiz.transform, MallaBlender("Embudo"),
                               MaterialEfecto("Embudo", TexturaEfecto("Embudo", true), new Color(1f, 1.5f, 1.9f, 0.85f), true), false);
            embudo.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mecha.embudo = embudo.GetComponent<Renderer>();
            var flechaMat = Neon("FlechaViento", new Color(0.45f, 0.9f, 1f), 1.2f);
            var flechas = new Transform[12];
            for (int i = 0; i < flechas.Length; i++)
            {
                var flecha = new GameObject("Flecha viento").transform;
                flecha.SetParent(viento, false);
                flecha.localScale = Vector3.one * 1.6f;
                foreach (float lado in new[] { -1f, 1f })
                {
                    var brazo = E.Pieza(PrimitiveType.Cube, "Brazo", flecha, new Vector3(lado * 0.42f, 0f, -0.42f), new Vector3(0.28f, 0.04f, 1.3f), flechaMat);
                    brazo.transform.localRotation = Quaternion.Euler(0f, -lado * 40f, 0f);
                    brazo.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                flecha.gameObject.SetActive(false);
                flechas[i] = flecha;
            }
            mecha.flechasViento = flechas;

            // Anillos en el suelo con flechas en V: se cierran hacia el robot al aspirar (cian) y se
            // abren al soplar (blanco cálido). Radio 1 en la malla: el robot los escala.
            var texSuccion = TexturaEfecto("Succion", true);
            mecha.anillosSuccion = Anillos("Anillo succion", viento, MallaBlender("AnilloSuccion"),
                                           MaterialEfecto("AnilloSuccion", texSuccion, new Color(0.6f, 1.25f, 1.75f, 0.6f), true));
            mecha.anillosSoplido = Anillos("Anillo soplido", viento, MallaBlender("AnilloSoplido"),
                                           MaterialEfecto("AnilloSoplido", texSuccion, new Color(1.7f, 1.45f, 0.85f, 0.6f), true));

            // Partículas: líneas de velocidad en espiral y polvo arrastrado por el césped.
            mecha.particulasAspira = ParticulasViento("Particulas aspira", viento, true);
            mecha.particulasSopla = ParticulasViento("Particulas sopla", viento, false);
            mecha.porciones = porciones.ToArray();

            mecha.proyectiles = Proyectiles(mecha);
            return mecha;
        }

        /// <summary>Cuatro anillos planos (radio 1 en la malla) a ras de suelo, apagados.</summary>
        static Transform[] Anillos(string nombre, Transform padre, Mesh malla, Material material)
        {
            var lista = new Transform[4];
            for (int i = 0; i < lista.Length; i++)
            {
                var a = Pieza(nombre, padre, malla, material, false);
                a.transform.localPosition = new Vector3(0f, 0.07f + i * 0.004f, 0f);
                a.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                a.SetActive(false);
                lista[i] = a.transform;
            }
            return lista;
        }

        /// <summary>
        /// Viento de partículas (apagado; lo enciende el robot): líneas de velocidad que van en
        /// espiral hacia el robot (o salen de él) y polvo arrastrado por el césped. Se mueven solas
        /// (sin script), así que también se ven en las partidas online.
        /// </summary>
        static GameObject ParticulasViento(string nombre, Transform padre, bool haciaDentro)
        {
            var raiz = new GameObject(nombre);
            raiz.transform.SetParent(padre, false);
            Sistema(raiz.transform, "Lineas", haciaDentro, false);
            Sistema(raiz.transform, "Polvo", haciaDentro, true);
            raiz.SetActive(false);
            return raiz;
        }

        static void Sistema(Transform padre, string nombre, bool haciaDentro, bool polvo)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = new Vector3(0f, polvo ? 0.4f : 1.4f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            float velocidad = polvo ? 8f : 15f;
            float recorrido = RadioPista + 1f - (RadioRobot + 0.8f);
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.duration = 2f;
            main.startSpeed = 0f;
            main.startLifetime = recorrido / velocidad;
            main.startSize = polvo ? new ParticleSystem.MinMaxCurve(1.3f, 2.6f) : new ParticleSystem.MinMaxCurve(0.2f, 0.36f);
            main.startColor = polvo ? new Color(0.85f, 0.92f, 1f, 0.5f)
                            : haciaDentro ? new Color(0.65f, 0.95f, 1f, 1f) : new Color(1f, 0.92f, 0.7f, 1f);
            main.startRotation = polvo ? new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f) : new ParticleSystem.MinMaxCurve(0f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = polvo ? 120 : 360;

            var emision = ps.emission;
            emision.rateOverTime = polvo ? 32f : 120f;

            var forma = ps.shape;
            forma.shapeType = ParticleSystemShapeType.Circle;
            forma.radius = haciaDentro ? RadioPista + 1f : RadioRobot + 0.8f;
            forma.radiusThickness = 0f;
            forma.rotation = new Vector3(90f, 0f, 0f);
            forma.randomPositionAmount = polvo ? 0.4f : 1.2f;

            // Hacia el centro (o hacia fuera) y girando: una espiral.
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(0f);
            vel.z = new ParticleSystem.MinMaxCurve(0f);
            vel.radial = new ParticleSystem.MinMaxCurve(haciaDentro ? -velocidad : velocidad);
            vel.orbitalY = new ParticleSystem.MinMaxCurve(haciaDentro ? 0.55f : -0.55f);

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var degradado = new Gradient();
            degradado.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                              new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            color.color = new ParticleSystem.MinMaxGradient(degradado);

            var render = go.GetComponent<ParticleSystemRenderer>();
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.sharedMaterial = RecursosEfectos.Material(polvo ? "Nube" : "Chispa");
            if (polvo)
            {
                render.renderMode = ParticleSystemRenderMode.Billboard;
                var hojas = ps.textureSheetAnimation;
                hojas.enabled = true;
                hojas.numTilesX = 2;
                hojas.numTilesY = 2;
                hojas.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
                hojas.startFrame = new ParticleSystem.MinMaxCurve(0f, 3.999f);
                var giro = ps.rotationOverLifetime;
                giro.enabled = true;
                giro.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            }
            else
            {
                render.renderMode = ParticleSystemRenderMode.Stretch;
                render.velocityScale = 0.1f;
                render.lengthScale = 2.5f;
            }
        }

        /// <summary>Textura de la pantalla LED (píxeles gordos) con la cara que toca, en un material que brilla.</summary>
        static Material PantallaLed(string cara)
        {
            const int w = 48, h = 28;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = $"Led{cara}" };
            var fondo = new Color(0.05f, 0.08f, 0.12f);
            Color tinta = cara switch
            {
                "Enfadado" => new Color(1f, 0.35f, 0.3f),
                "Risa" => new Color(0.45f, 1f, 0.55f),
                "Mareado" => new Color(1f, 0.85f, 0.35f),
                "Laser" => new Color(1f, 0.35f, 0.85f),
                "Furia" => new Color(1f, 0.2f, 0.15f),
                "Corte" => new Color(1f, 0.6f, 0.2f),
                _ => new Color(0.55f, 0.95f, 1f),
            };
            var p = new Color[w * h];
            for (int i = 0; i < p.Length; i++) p[i] = (i / w) % 2 == 0 ? fondo : fondo * 0.8f; // líneas de pantalla
            void Pon(int x, int y) { if (x >= 0 && x < w && y >= 0 && y < h) p[y * w + x] = tinta; }
            void Circulo(int cx, int cy, int r, bool lleno = true)
            {
                for (int y = -r; y <= r; y++)
                    for (int x = -r; x <= r; x++)
                    {
                        int d = x * x + y * y;
                        if (lleno ? d <= r * r : d <= r * r && d >= (r - 1) * (r - 1)) Pon(cx + x, cy + y);
                    }
            }
            void Linea(int x0, int y0, int x1, int y1)
            {
                int n = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
                for (int i = 0; i <= n; i++) Pon(Mathf.RoundToInt(Mathf.Lerp(x0, x1, i / (float)n)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, i / (float)n)));
            }
            int ojoI = 14, ojoD = 33, ojoY = 15;
            switch (cara)
            {
                case "Enfadado":
                    Circulo(ojoI, ojoY - 1, 4); Circulo(ojoD, ojoY - 1, 4);
                    for (int k = 0; k < 2; k++) { Linea(8, 23 - k, 19, 19 - k); Linea(28, 19 - k, 39, 23 - k); } // cejas en V
                    Linea(17, 5, 30, 5);
                    break;
                case "Risa":
                    for (int k = 0; k < 2; k++) { Linea(10, ojoY - 2 + k, 14, ojoY + 2 + k); Linea(14, ojoY + 2 + k, 18, ojoY - 2 + k); Linea(29, ojoY - 2 + k, 33, ojoY + 2 + k); Linea(33, ojoY + 2 + k, 37, ojoY - 2 + k); }
                    for (int x = 15; x <= 32; x++) for (int y = 3; y <= 7; y++) if ((x - 23.5f) * (x - 23.5f) / 80f + (y - 7f) * (y - 7f) / 18f <= 1f) Pon(x, y);
                    break;
                case "Mareado":
                    foreach (int cx in new[] { ojoI, ojoD })
                    {
                        Circulo(cx, ojoY, 5, false); Circulo(cx, ojoY, 3, false); Pon(cx, ojoY);
                    }
                    for (int x = 16; x <= 31; x++) Pon(x, 5 + ((x / 2) % 2));
                    break;
                case "Laser":
                    Circulo(ojoI, ojoY, 4); Circulo(ojoD, ojoY, 4);
                    for (int k = 0; k < 2; k++) { Linea(8, 22 - k, 19, 20 - k); Linea(28, 20 - k, 39, 22 - k); }
                    for (int x = 4; x <= 43; x += 2) Pon(x, 4); // la raya del láser
                    Circulo(24, 4, 2);
                    break;
                case "Furia":
                    // Ojos en raya, cejas muy bajas, boca de dientes y humo saliendo.
                    for (int k = 0; k < 3; k++) { Linea(9, ojoY - 1 + k, 19, ojoY - 3 + k); Linea(28, ojoY - 3 + k, 38, ojoY - 1 + k); }
                    for (int x = 13; x <= 34; x++) Pon(x, 6 + ((x / 2) % 2 == 0 ? 0 : -2));
                    Linea(13, 7, 34, 7);
                    Circulo(4, 24, 2, false); Circulo(43, 24, 2, false); Circulo(6, 21, 1, false); Circulo(41, 21, 1, false);
                    break;
                case "Corte":
                    // Ojos mirando abajo (al césped) y una sierra por boca.
                    Circulo(ojoI, ojoY, 4, false); Circulo(ojoD, ojoY, 4, false);
                    Circulo(ojoI, ojoY - 2, 2); Circulo(ojoD, ojoY - 2, 2);
                    for (int x = 12; x <= 35; x++) Pon(x, 5 + Mathf.Abs((x % 4) - 2));
                    break;
                default:
                    Circulo(ojoI, ojoY, 4); Circulo(ojoD, ojoY, 4);
                    Pon(ojoI + 1, ojoY + 2); Pon(ojoD + 1, ojoY + 2);
                    Linea(18, 6, 21, 4); Linea(21, 4, 26, 4); Linea(26, 4, 29, 6);
                    break;
            }
            tex.SetPixels(p);
            tex.Apply();
            string rutaTex = $"{CarpetaMallas}/Led{cara}.asset";
            AssetDatabase.DeleteAsset(rutaTex);
            AssetDatabase.CreateAsset(tex, rutaTex);

            var m = Mat($"Led{cara}", Color.white, 0.2f);
            m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetTexture("_EmissionMap", tex);
            m.SetColor("_EmissionColor", Color.white * 1.8f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Plantillas (apagadas) de lo que escupe: tuercas y pilas de goma que rebotan.</summary>
        static GameObject[] Proyectiles(CortacespedMecha mecha)
        {
            var plantillas = M.Grupo(mapa, "Plantillas");
            var rebote = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>($"{CarpetaMallas}/Goma.physicMaterial");
            if (rebote == null)
            {
                rebote = new PhysicsMaterial("Goma") { bounciness = 0.6f, bounceCombine = PhysicsMaterialCombine.Maximum, dynamicFriction = 0.4f, staticFriction = 0.4f };
                AssetDatabase.CreateAsset(rebote, $"{CarpetaMallas}/Goma.physicMaterial");
            }

            GameObject Preparar(GameObject go, float masa)
            {
                go.transform.SetParent(plantillas, false);
                go.transform.position = new Vector3(0f, -60f, 0f);
                foreach (var c in go.GetComponentsInChildren<Collider>()) c.sharedMaterial = rebote;
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = masa;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                go.AddComponent<ProyectilChatarra>();
                go.SetActive(false);
                return go;
            }

            // Tuerca de goma (hexagonal).
            var tuerca = Pieza("una tuerca", plantillas, Hexagono("Tuerca", 0.7f, 0.5f), Mat("TuercaGoma", new Color(0.62f, 0.68f, 0.8f), 0.4f), false);
            var malla = tuerca.AddComponent<MeshCollider>();
            malla.sharedMesh = tuerca.GetComponent<MeshFilter>().sharedMesh;
            malla.convex = true;
            Pieza("Agujero", tuerca.transform, Hexagono("AgujeroTuerca", 0.28f, 0.52f), Mat("Agujero", new Color(0.15f, 0.15f, 0.18f), 0.2f), false)
                .transform.localPosition = Vector3.zero;

            // Pila gigante (cilindro con su polo).
            GameObject Pila(string nombre, Color cuerpoColor, float escala)
            {
                var pila = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pila.name = nombre;
                pila.transform.localScale = new Vector3(0.75f, 0.85f, 0.75f) * escala;
                pila.GetComponent<Renderer>().sharedMaterial = Mat($"Pila{nombre.Length}", cuerpoColor, 0.5f);
                var franja = E.Pieza(PrimitiveType.Cylinder, "Franja", pila.transform, new Vector3(0f, 0.55f, 0f), new Vector3(1.02f, 0.45f, 1.02f), Mat("PilaNegra", new Color(0.12f, 0.12f, 0.14f), 0.3f));
                E.Pieza(PrimitiveType.Cylinder, "Polo", pila.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.35f, 0.08f, 0.35f), Mat("PilaPolo", new Color(0.85f, 0.8f, 0.7f), 0.8f));
                return pila;
            }

            // --- La chatarra nueva (mallas de Blender: crear_chatarra_cortacesped.py) ---
            // Muelle: bota mucho.
            var saltarin = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>($"{CarpetaMallas}/Muelle.physicMaterial");
            if (saltarin == null)
            {
                saltarin = new PhysicsMaterial("Muelle") { bounciness = 0.95f, bounceCombine = PhysicsMaterialCombine.Maximum, dynamicFriction = 0.2f, staticFriction = 0.2f };
                AssetDatabase.CreateAsset(saltarin, $"{CarpetaMallas}/Muelle.physicMaterial");
            }
            var muelle = Pieza("un muelle", plantillas, MallaBlender("Muelle"), Mat("Muelle", new Color(0.25f, 0.85f, 0.75f), 0.75f), false);
            var capsula = muelle.AddComponent<CapsuleCollider>();
            capsula.height = 1.15f;
            capsula.radius = 0.46f;
            mecha.plantillaMuelle = Preparar(muelle, 2f);
            capsula.sharedMaterial = saltarin;

            // Lata de aceite: al caer, charco resbaladizo.
            var lata = new GameObject("una lata de aceite");
            Pieza("Lata", lata.transform, MallaBlender("Lata"), Mat("Lata", new Color(0.85f, 0.15f, 0.12f), 0.6f), false);
            Pieza("Etiqueta", lata.transform, MallaBlender("LataEtiqueta"), Mat("LataEtiqueta", new Color(0.97f, 0.88f, 0.55f), 0.4f), false);
            var cuerpoLata = lata.AddComponent<CapsuleCollider>();
            cuerpoLata.height = 1.04f;
            cuerpoLata.radius = 0.36f;
            cuerpoLata.center = new Vector3(0f, 0.52f, 0f);
            // (A.Disco coloca en coordenadas del mundo: las plantillas están en y = -60.)
            var charco = new GameObject("Charco de aceite");
            charco.transform.SetParent(plantillas, false);
            charco.transform.position = new Vector3(0f, -60f, 0f);
            A.Disco(charco.transform, "Mancha", new Vector3(0f, -60f, 0f), -60f + 0.02f, 1f, 0.02f, Mat("Aceite", new Color(0.07f, 0.06f, 0.09f), 0.97f));
            A.Disco(charco.transform, "Brillo", new Vector3(0.3f, -60f, 0.25f), -60f + 0.03f, 0.35f, 0.01f, Neon("AceiteBrillo", new Color(0.55f, 0.4f, 0.9f), 0.6f));
            foreach (var c in charco.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            charco.transform.localScale = new Vector3(2.2f, 1f, 2.2f);
            charco.AddComponent<CharcoAceite>().radio = 2.2f;
            charco.SetActive(false);
            mecha.plantillaLata = Preparar(lata, 3f);
            lata.AddComponent<LataAceite>().charco = charco;

            // Neumático: rueda hacia fuera (sin físicas).
            var rueda = new GameObject("un neumatico");
            rueda.transform.SetParent(plantillas, false);
            rueda.transform.position = new Vector3(0f, -60f, 0f);
            var dibujo = new GameObject("Dibujo").transform;
            dibujo.SetParent(rueda.transform, false);
            dibujo.localScale = Vector3.one * 0.8f;
            Pieza("Goma", dibujo, MallaBlender("Neumatico"), Mat("Neumatico", new Color(0.09f, 0.09f, 0.11f), 0.25f), false);
            Pieza("Llanta", dibujo, MallaBlender("Llanta"), Mat("Llanta", new Color(1f, 0.75f, 0.15f), 0.55f), false);
            rueda.AddComponent<RuedaRodante>().radio = 0.8f;
            rueda.SetActive(false);
            mecha.plantillaRueda = rueda;

            // Perno gigante: diana roja en el suelo y el perno que baja.
            var perno = new GameObject("un perno gigante");
            perno.transform.SetParent(plantillas, false);
            perno.transform.position = new Vector3(0f, -60f, 0f);
            var cuerpoPerno = new GameObject("Cuerpo").transform;
            cuerpoPerno.SetParent(perno.transform, false);
            Pieza("Rosca", cuerpoPerno, MallaBlender("PernoRosca"), Mat("PernoRosca", new Color(0.62f, 0.66f, 0.74f), 0.7f), false);
            Pieza("Cabeza", cuerpoPerno, MallaBlender("PernoCabeza"), Mat("PernoCabeza", new Color(1f, 0.45f, 0.15f), 0.55f), false);
            var diana = new GameObject("Diana").transform;
            diana.SetParent(perno.transform, false);
            var rojoDiana = MaterialEfecto("Diana", TexturaEfecto("Brillo", false), new Color(2.2f, 0.35f, 0.3f, 0.9f), true);
            var aroDiana = Pieza("Aro", diana, Anillo("DianaAro", 2.0f, 2.35f, 0f, 360f, 0.04f, 0.02f), Neon("DianaNeon", new Color(1f, 0.2f, 0.15f), 2.5f), false);
            aroDiana.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            var centroDiana = Pieza("Centro", diana, Anillo("DianaCentro", 0.55f, 0.85f, 0f, 360f, 0.04f, 0.02f), Neon("DianaNeon", new Color(1f, 0.2f, 0.15f), 2.5f), false);
            centroDiana.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            var halo = A.Disco(diana, "Halo", new Vector3(0f, -60f, 0f), -60f + 0.015f, 2.3f, 0.01f, rojoDiana);
            foreach (var c in halo.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (var rr in perno.GetComponentsInChildren<Renderer>()) rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach (var rr in cuerpoPerno.GetComponentsInChildren<Renderer>()) rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            var pg = perno.AddComponent<PernoGigante>();
            pg.cuerpo = cuerpoPerno;
            pg.diana = diana;
            perno.SetActive(false);
            mecha.plantillaPerno = perno;

            return new[]
            {
                Preparar(tuerca, 3f),
                Preparar(Pila("una pila", new Color(1f, 0.62f, 0.2f), 1f), 4f),
                Preparar(Pila("una pila gorda", new Color(0.95f, 0.8f, 0.25f), 1.4f), 6f),
            };
        }

        // ---------------------------------------------------------------------
        // Decorado
        // ---------------------------------------------------------------------

        /// <summary>Chatarra en el fondo del foso: pilas, tuercas y muelles tirados.</summary>
        static void Chatarra()
        {
            var g = M.Grupo(decorado, "Chatarra");
            var azar = new System.Random(11);
            var tuercaMalla = Hexagono("TuercaChatarra", 0.6f, 0.4f);
            for (int i = 0; i < 28; i++)
            {
                float a = (float)azar.NextDouble() * Mathf.PI * 2f;
                float r = Mathf.Lerp(RadioRobot + 0.6f, RadioFoso - 0.4f, (float)azar.NextDouble());
                var pos = new Vector3(Mathf.Cos(a) * r, FondoFoso + 0.3f, Mathf.Sin(a) * r);
                GameObject go;
                switch (i % 3)
                {
                    case 0:
                        go = Pieza("Tuerca", g, tuercaMalla, Mat("TuercaVieja", new Color(0.55f, 0.5f, 0.45f), 0.6f), false);
                        break;
                    case 1:
                        go = E.Pieza(PrimitiveType.Cylinder, "Pila", g, Vector3.zero, new Vector3(0.6f, 0.7f, 0.6f), Mat("PilaVieja", new Color(0.8f, 0.45f, 0.2f), 0.4f));
                        break;
                    default:
                        go = E.Pieza(PrimitiveType.Cube, "Tornillo", g, Vector3.zero, new Vector3(0.25f, 1.4f, 0.25f), Mat("TornilloViejo", new Color(0.7f, 0.72f, 0.75f), 0.7f));
                        break;
                }
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler((float)azar.NextDouble() * 360f, (float)azar.NextDouble() * 360f, (float)azar.NextDouble() * 360f);
            }
            // Una luz roja que sale del foso: da miedo caerse.
            var luz = new GameObject("Luz del foso").AddComponent<Light>();
            luz.transform.SetParent(g, false);
            luz.transform.position = new Vector3(0f, FondoFoso + 1f, -RadioFoso + 1f);
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.35f, 0.25f);
            luz.range = 9f;
            luz.intensity = 2f;
        }

        /// <summary>Hierba gigante y dientes de león alrededor (el jardín, de noche).</summary>
        static void Jardin()
        {
            var g = M.Grupo(decorado, "Jardin");
            var azar = new System.Random(5);
            var hierbas = new[] { Mat("Hierba1", new Color(0.2f, 0.55f, 0.25f), 0.2f), Mat("Hierba2", new Color(0.3f, 0.66f, 0.28f), 0.2f), Mat("Hierba3", new Color(0.16f, 0.45f, 0.3f), 0.2f) };
            for (int i = 0; i < 70; i++)
            {
                float a = (float)azar.NextDouble() * Mathf.PI * 2f;
                float r = 38f + (float)azar.NextDouble() * 22f;
                float alto = 8f + (float)azar.NextDouble() * 12f;
                var brizna = E.Pieza(PrimitiveType.Cube, "Brizna", g, new Vector3(Mathf.Cos(a) * r, -9f + alto * 0.5f, Mathf.Sin(a) * r),
                                     new Vector3(0.9f, alto, 0.25f), hierbas[i % hierbas.Length]);
                brizna.transform.rotation = Quaternion.Euler((float)(azar.NextDouble() - 0.5) * 25f, (float)azar.NextDouble() * 360f, (float)(azar.NextDouble() - 0.5) * 25f);
            }
            var tallo = Mat("Tallo", new Color(0.35f, 0.6f, 0.3f), 0.2f);
            var pelusa = Neon("Pelusa", new Color(0.95f, 0.95f, 0.9f), 0.4f);
            foreach (var (x, z, alto) in new[] { (-30f, 34f, 22f), (34f, 30f, 26f), (-42f, -6f, 18f), (44f, -2f, 20f), (8f, 44f, 28f) })
            {
                E.Pieza(PrimitiveType.Cylinder, "Tallo", g, new Vector3(x, -9f + alto * 0.5f, z), new Vector3(0.5f, alto * 0.5f, 0.5f), tallo);
                E.Pieza(PrimitiveType.Sphere, "Diente de leon", g, new Vector3(x, -9f + alto + 2.5f, z), Vector3.one * 6f, pelusa);
            }
        }

        /// <summary>Letrero de neón "BichoShow" detrás del robot, sobre una estructura de focos.</summary>
        static void Letrero()
        {
            var g = M.Grupo(decorado, "Letrero");
            var estructura = Mat("Estructura", new Color(0.55f, 0.57f, 0.62f), 0.7f);
            foreach (float x in new[] { -6f, 6f })
                E.Pieza(PrimitiveType.Cylinder, "Poste", g, new Vector3(x, 4f, 11f), new Vector3(0.4f, 8f, 0.4f), estructura);
            E.Pieza(PrimitiveType.Cube, "Panel", g, new Vector3(0f, 12f, 11.2f), new Vector3(11f, 3.2f, 0.4f), Mat("PanelLetrero", new Color(0.12f, 0.06f, 0.2f), 0.4f));
            E.Pieza(PrimitiveType.Cube, "Borde", g, new Vector3(0f, 12f, 11.4f), new Vector3(11.6f, 3.8f, 0.2f), Neon("NeonRosa", new Color(1f, 0.3f, 0.75f)));
            M.Cartel(g, "BichoShow", new Vector3(0f, 12f, 10.9f), 2f, new Color(0.45f, 0.95f, 1f));
            // Banderines entre los postes de los focos (colores del show).
            var colores = new[] { Neon("Banderin1", new Color(1f, 0.4f, 0.4f), 0.6f), Neon("Banderin2", new Color(1f, 0.85f, 0.3f), 0.6f), Neon("Banderin3", new Color(0.4f, 0.7f, 1f), 0.6f) };
            for (int i = 0; i < 40; i++)
            {
                float a = i / 40f * Mathf.PI * 2f;
                var b = E.Pieza(PrimitiveType.Cube, "Banderin", g, new Vector3(Mathf.Cos(a) * 26f, 7.5f + Mathf.Sin(i * 1.7f) * 0.3f, Mathf.Sin(a) * 26f), new Vector3(0.8f, 0.9f, 0.05f), colores[i % 3]);
                b.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 45f);
            }
        }

        /// <summary>Focos del plató: luces de verdad que iluminan el césped de noche.</summary>
        static void Focos()
        {
            var g = M.Grupo(decorado, "Focos");
            foreach (float angulo in new[] { -135f, -45f, 45f, 135f })
            {
                float a = angulo * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Sin(a) * 30f, 20f, Mathf.Cos(a) * 30f);
                var luz = new GameObject("Foco").AddComponent<Light>();
                luz.transform.SetParent(g, false);
                luz.transform.position = pos;
                luz.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, 0f) - pos);
                luz.type = LightType.Spot;
                luz.spotAngle = 55f;
                luz.range = 70f;
                luz.intensity = 6f;
                luz.color = new Color(1f, 0.93f, 0.85f);
                luz.shadows = angulo < 0f ? LightShadows.Soft : LightShadows.None;
            }
        }

        // ---------------------------------------------------------------------
        // Mallas hechas a mano (se guardan en Modelos/Cortacesped)
        // ---------------------------------------------------------------------

        static GameObject Pieza(string nombre, Transform padre, Mesh malla, Material material, bool conColision)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (conColision) go.AddComponent<MeshCollider>().sharedMesh = malla;
            return go;
        }

        static void Bloque(string nombre, Transform padre, Vector3 posicion, Vector3 escala, Material material)
        {
            E.Bloque(nombre, padre, posicion, escala, material, local: true);
        }

        static Mesh Guardar(string nombre, Mesh malla)
        {
            malla.name = nombre;
            malla.RecalculateBounds();
            string ruta = $"{CarpetaMallas}/{nombre}.asset";
            AssetDatabase.DeleteAsset(ruta);
            AssetDatabase.CreateAsset(malla, ruta);
            return malla;
        }

        /// <summary>Porción de anillo (de r0 a r1, de a0 a a1 grados) con la cara de arriba en "arriba" y la de abajo en "abajo".</summary>
        static Mesh Anillo(string nombre, float r0, float r1, float a0, float a1, float arriba, float abajo)
        {
            int n = Mathf.Max(2, Mathf.CeilToInt((a1 - a0) / 4f));
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            void Cara(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
            {
                int b = v.Count;
                v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3);
                uv.Add(new Vector2(p0.x, p0.z)); uv.Add(new Vector2(p1.x, p1.z)); uv.Add(new Vector2(p2.x, p2.z)); uv.Add(new Vector2(p3.x, p3.z));
                t.Add(b); t.Add(b + 1); t.Add(b + 2); t.Add(b); t.Add(b + 2); t.Add(b + 3);
            }
            Vector3 P(float r, float a, float y) => new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * r, y, Mathf.Sin(a * Mathf.Deg2Rad) * r);
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)n), b = Mathf.Lerp(a0, a1, (i + 1) / (float)n);
                Cara(P(r0, a, arriba), P(r0, b, arriba), P(r1, b, arriba), P(r1, a, arriba));      // arriba
                Cara(P(r1, a, abajo), P(r1, b, abajo), P(r0, b, abajo), P(r0, a, abajo));          // abajo
                Cara(P(r1, a, arriba), P(r1, b, arriba), P(r1, b, abajo), P(r1, a, abajo));        // fuera
                Cara(P(r0, a, abajo), P(r0, b, abajo), P(r0, b, arriba), P(r0, a, arriba));        // dentro
            }
            if (a1 - a0 < 359f)
            {
                Cara(P(r0, a0, abajo), P(r0, a0, arriba), P(r1, a0, arriba), P(r1, a0, abajo));
                Cara(P(r1, a1, abajo), P(r1, a1, arriba), P(r0, a1, arriba), P(r0, a1, abajo));
            }
            var m = new Mesh();
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            return Guardar(nombre, m);
        }

        /// <summary>Pared de tubo (sin grosor) de radio r, vista por dentro y por fuera.</summary>
        static Mesh Tubo(string nombre, float r, float y0, float y1, float repeticiones = 1f)
        {
            const int n = 72;
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            for (int i = 0; i <= n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                v.Add(new Vector3(Mathf.Cos(a) * r, y0, Mathf.Sin(a) * r));
                v.Add(new Vector3(Mathf.Cos(a) * r, y1, Mathf.Sin(a) * r));
                uv.Add(new Vector2(i / (float)n * repeticiones, 0f));
                uv.Add(new Vector2(i / (float)n * repeticiones, 1f));
            }
            int doble = v.Count;
            v.AddRange(new List<Vector3>(v)); // la otra cara
            uv.AddRange(new List<Vector2>(uv));
            for (int i = 0; i < n; i++)
            {
                int a = i * 2;
                t.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2 });
                t.AddRange(new[] { doble + a, doble + a + 3, doble + a + 1, doble + a, doble + a + 2, doble + a + 3 });
            }
            var m = new Mesh();
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            return Guardar(nombre, m);
        }

        // ---------------------------------------------------------------------
        // Lo hecho en Blender: mallas (JSON en coordenadas de Unity) y texturas de efectos
        // ---------------------------------------------------------------------

#pragma warning disable 0649 // los rellena JsonUtility
        [System.Serializable]
        class MallasJson { public MallaJson[] mallas; }

        [System.Serializable]
        class MallaJson
        {
            public string nombre;
            public float[] v, uv, n;
            public int[] t;
        }
#pragma warning restore 0649

        static Dictionary<string, MallaJson> mallasBlender;

        /// <summary>Una malla de Arte/Efectos/mallas_cortacesped.json (la hace crear_efectos_cortacesped.py).</summary>
        static Mesh MallaBlender(string nombre)
        {
            if (mallasBlender == null)
            {
                mallasBlender = new Dictionary<string, MallaJson>();
                string ruta = Path.Combine(Application.dataPath, "..", "Arte", "Efectos", "mallas_cortacesped.json");
                if (File.Exists(ruta))
                    foreach (var m in JsonUtility.FromJson<MallasJson>(File.ReadAllText(ruta)).mallas) mallasBlender[m.nombre] = m;
                else
                    Debug.LogWarning($"Bicho Show: no encuentro {ruta}. Genéralo con Arte/Blender/crear_efectos_cortacesped.py.");
            }
            var malla = new Mesh();
            if (mallasBlender.TryGetValue(nombre, out var datos))
            {
                var v = new Vector3[datos.v.Length / 3];
                for (int i = 0; i < v.Length; i++) v[i] = new Vector3(datos.v[i * 3], datos.v[i * 3 + 1], datos.v[i * 3 + 2]);
                var uv = new Vector2[datos.uv.Length / 2];
                for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(datos.uv[i * 2], datos.uv[i * 2 + 1]);
                malla.indexFormat = v.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                malla.vertices = v;
                malla.uv = uv;
                malla.triangles = datos.t;
                if (datos.n != null && datos.n.Length == datos.v.Length)
                {
                    var n = new Vector3[v.Length];
                    for (int i = 0; i < n.Length; i++) n[i] = new Vector3(datos.n[i * 3], datos.n[i * 3 + 1], datos.n[i * 3 + 2]);
                    malla.normals = n;
                }
                else malla.RecalculateNormals();
            }
            return Guardar("Efecto" + nombre, malla);
        }

        /// <summary>Textura de Texturas/Efectos (blanca, con transparencia), con sus ajustes de importación.</summary>
        static Texture2D TexturaEfecto(string nombre, bool repetir)
        {
            string ruta = $"{E.Raiz}/Texturas/Efectos/{nombre}.png";
            AssetDatabase.ImportAsset(ruta);
            if (AssetImporter.GetAtPath(ruta) is TextureImporter imp)
            {
                imp.textureType = TextureImporterType.Default;
                imp.alphaSource = TextureImporterAlphaSource.FromInput;
                imp.alphaIsTransparency = true;
                imp.wrapMode = repetir ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                imp.mipmapEnabled = true;
                imp.sRGBTexture = true;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        /// <summary>Material de efecto: sin luces (URP Unlit), transparente, de las dos caras; aditivo si brilla.</summary>
        static Material MaterialEfecto(string nombre, Texture2D textura, Color color, bool aditivo)
        {
            string ruta = $"{E.Raiz}/Materiales/CortacespedEfecto{nombre}.mat";
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, ruta);
            }
            else m.shader = shader;
            m.SetTexture("_BaseMap", textura);
            m.mainTexture = textura;
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", aditivo ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(aditivo ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Prisma hexagonal (tuerca) tumbado, de radio r y grosor g, centrado.</summary>
        static Mesh Hexagono(string nombre, float r, float g)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            void Cara(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int k = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                t.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
            }
            Vector3 P(int i, float y) => new Vector3(Mathf.Cos(i * Mathf.PI / 3f) * r, y, Mathf.Sin(i * Mathf.PI / 3f) * r);
            for (int i = 0; i < 6; i++)
            {
                Cara(P(i, g * 0.5f), P(i + 1, g * 0.5f), P(i + 1, -g * 0.5f), P(i, -g * 0.5f));
                int k = v.Count;
                v.Add(Vector3.up * g * 0.5f); v.Add(P(i + 1, g * 0.5f)); v.Add(P(i, g * 0.5f));
                t.AddRange(new[] { k, k + 1, k + 2 });
                k = v.Count;
                v.Add(Vector3.down * g * 0.5f); v.Add(P(i, -g * 0.5f)); v.Add(P(i + 1, -g * 0.5f));
                t.AddRange(new[] { k, k + 1, k + 2 });
            }
            var m = new Mesh();
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            return Guardar(nombre, m);
        }
    }
}
