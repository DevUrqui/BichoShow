using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;
using M = BichoShow.Herramientas.CreadorMudanza;
using A = BichoShow.Herramientas.CreadorArena;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Menú "BichoShow > Crear escena: Lluvia de muebles".
    /// Un salón gigante flotando en el plató (parqué con alfombra) del que no hay que caerse,
    /// con público alrededor. Los muebles que caen son plantillas apagadas en la escena (con su
    /// caja de colisión a medida) que RondaLluvia va copiando.
    /// </summary>
    public static class CreadorLluvia
    {
        const string RutaEscena = E.CarpetaEscenas + "/Lluvia.unity";
        const float Medio = 9.5f;

        [MenuItem("BichoShow/Crear escena: Lluvia de muebles")]
        public static void Crear()
        {
            var bicho = A.Preparar(RutaEscena, "la Lluvia de muebles", out _);
            if (bicho == null) return;
            CrearEscena(bicho);
            Debug.Log("Bicho Show: escena de la Lluvia de muebles creada. Para que el show la use, rehaz también la escena del menú.");
        }

        static void CrearEscena(GameObject prefabBicho)
        {
            var parque = E.CrearMaterial("Parque", new Color(0.82f, 0.6f, 0.4f), 0.35f);
            var parqueOscuro = E.CrearMaterial("ParqueOscuro", new Color(0.7f, 0.48f, 0.3f), 0.35f);
            var alfombra = E.CrearMaterial("Alfombra", new Color(0.85f, 0.3f, 0.4f), 0.1f);
            var alfombraBorde = E.CrearMaterial("AlfombraBorde", new Color(1f, 0.82f, 0.35f), 0.1f);
            var rodapie = E.CrearMaterial("Rodapie", new Color(0.95f, 0.93f, 0.9f), 0.4f);
            var sombraMat = E.CrearMaterial("SombraMueble", new Color(0.12f, 0.06f, 0.16f), 0f);

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            A.Camara();
            var mapa = new GameObject("Mapa").transform;

            // --- El salón: suelo de parqué a tablas, alfombra y rodapié ---
            E.Bloque("Suelo", mapa, new Vector3(0f, -0.5f, 0f), new Vector3(Medio * 2f, 1f, Medio * 2f), parque);
            for (int i = 0; i < 10; i++)
                if (i % 2 == 1)
                    M.Decorado(PrimitiveType.Cube, "Tabla", mapa, new Vector3(-Medio + (i + 0.5f) * Medio * 0.2f, 0.004f, 0f),
                               new Vector3(Medio * 0.2f - 0.02f, 0.005f, Medio * 2f - 0.05f), parqueOscuro);
            M.Decorado(PrimitiveType.Cube, "AlfombraBorde", mapa, new Vector3(0f, 0.01f, 0f), new Vector3(10.6f, 0.01f, 7.6f), alfombraBorde);
            M.Decorado(PrimitiveType.Cube, "Alfombra", mapa, new Vector3(0f, 0.014f, 0f), new Vector3(10f, 0.01f, 7f), alfombra);
            foreach (var (pos, tam) in new[]
            {
                (new Vector3(0f, -0.95f, Medio + 0.1f), new Vector3(Medio * 2f + 0.4f, 0.3f, 0.2f)),
                (new Vector3(0f, -0.95f, -Medio - 0.1f), new Vector3(Medio * 2f + 0.4f, 0.3f, 0.2f)),
                (new Vector3(Medio + 0.1f, -0.95f, 0f), new Vector3(0.2f, 0.3f, Medio * 2f + 0.4f)),
                (new Vector3(-Medio - 0.1f, -0.95f, 0f), new Vector3(0.2f, 0.3f, Medio * 2f + 0.4f)),
            })
                M.Decorado(PrimitiveType.Cube, "Rodapie", mapa, pos, tam, rodapie);
            A.Vacio(mapa);

            // Dos guantes en los bordes: para el saboteador (empujar a la gente bajo las sombras).
            var trampas = M.Grupo(mapa, "Trampas");
            A.Guante(trampas, "Guante izquierdo", new Vector3(-(Medio - 0.6f), 0f, 3f), Vector3.right, 0f, 5f);
            A.Guante(trampas, "Guante derecho", new Vector3(Medio - 0.6f, 0f, -3f), Vector3.left, 2.5f, 5f);

            var zonaSaboteadores = A.CabinaSaboteador(mapa, new Vector3(-7f, 0f, -8f));
            A.Publico(mapa, 18f, new[] { -100f, -60f, -20f, 20f, 60f, 100f }, new[] { -120f, -40f, 40f, 120f }, "LLUVIA DE MUEBLES");
            var gestor = A.Jugadores(prefabBicho, A.EnCirculo(4, 4.5f), Vector3.zero);

            // --- Plantillas (apagadas): los muebles que caen y la sombra que avisa ---
            var plantillas = M.Grupo(mapa, "Plantillas");
            (string pieza, string kit, string nombre, float escala)[] tipos =
            {
                ("Sofa", KitObjetos.KitBarrio, "un sofá", 1.1f),
                ("Tele", KitObjetos.KitBarrio, "una tele", 1.2f),
                ("Jarron", KitObjetos.KitBarrio, "un jarrón", 1.3f),
                ("CajaCarton", KitObjetos.KitBarrio, "una caja", 1.6f),
                ("Piano", KitObjetos.KitPlato, "un piano", 1f),
                ("Yunque", KitObjetos.KitPlato, "un yunque", 1.2f),
                ("Altavoz", KitObjetos.KitPlato, "un altavoz", 1f),
            };
            var muebles = new GameObject[tipos.Length];
            for (int i = 0; i < tipos.Length; i++)
                muebles[i] = Plantilla(plantillas, tipos[i].pieza, tipos[i].kit, tipos[i].nombre, tipos[i].escala);

            var sombra = M.Decorado(PrimitiveType.Cylinder, "Sombra", plantillas, Vector3.zero, new Vector3(1f, 0.01f, 1f), sombraMat);
            sombra.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sombra.SetActive(false);

            // --- Ronda ---
            var rondaGo = new GameObject("RondaLluvia");
            var ronda = rondaGo.AddComponent<RondaLluvia>();
            ronda.gestor = gestor;
            ronda.zonaSaboteadores = zonaSaboteadores;
            ronda.saboteadorMaquina = false; // aquí ya llueve bastante
            ronda.muebles = muebles;
            ronda.sombra = sombra;
            ronda.centroArena = rondaGo.transform;
            ronda.medioTamano = new Vector2(Medio, Medio);

            A.Guardar(escena, RutaEscena, rondaGo);
        }

        /// <summary>Un mueble listo para caer: cuerpo pesado, caja de colisión a su medida y apagado.</summary>
        static GameObject Plantilla(Transform padre, string pieza, string kit, string nombre, float escala)
        {
            var raiz = new GameObject(nombre);
            raiz.transform.SetParent(padre, false);
            raiz.transform.position = new Vector3(0f, -50f, 0f);
            var modelo = KitObjetos.Poner(pieza, raiz.transform, Vector3.zero, 0f, escala: escala, local: true, kit: kit);

            // Caja a la medida del modelo (en local de la raíz).
            var renderers = modelo.GetComponentsInChildren<Renderer>();
            var limites = new Bounds(raiz.transform.position + Vector3.up * 0.5f, Vector3.one * 0.5f);
            if (renderers.Length > 0)
            {
                limites = renderers[0].bounds;
                foreach (var r in renderers) limites.Encapsulate(r.bounds);
            }
            var caja = raiz.AddComponent<BoxCollider>();
            caja.center = limites.center - raiz.transform.position;
            caja.size = limites.size;

            var rb = raiz.AddComponent<Rigidbody>();
            rb.mass = 40f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            raiz.AddComponent<MuebleCayendo>();
            raiz.SetActive(false);
            return raiz;
        }
    }
}
