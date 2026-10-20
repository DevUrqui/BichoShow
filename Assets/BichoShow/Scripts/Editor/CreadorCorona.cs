using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;
using M = BichoShow.Herramientas.CreadorMudanza;
using A = BichoShow.Herramientas.CreadorArena;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Menú "BichoShow > Crear escena: La Corona".
    /// Una tarta gigante de dos pisos flotando en el plató: el piso de abajo con velas gigantes
    /// (para esconderse), dos rampas y un escalón para subir al de arriba, donde gira una
    /// barredora y aparece la corona. Setas rebotadoras, guantes en el borde y público.
    /// </summary>
    public static class CreadorCorona
    {
        const string RutaEscena = E.CarpetaEscenas + "/Corona.unity";
        const float Radio = 11f;
        const float RadioArriba = 4.5f;
        const float AlturaArriba = 1.2f;

        [MenuItem("BichoShow/Crear escena: La Corona")]
        public static void Crear()
        {
            var bicho = A.Preparar(RutaEscena, "La Corona", out var obstaculo);
            if (bicho == null) return;
            CrearEscena(bicho, obstaculo);
            Debug.Log("Bicho Show: escena de La Corona creada. Para que el show la use, rehaz también la escena del menú.");
        }

        static void CrearEscena(GameObject prefabBicho, Material obstaculo)
        {
            var bizcocho = E.CrearMaterial("Bizcocho", new Color(0.96f, 0.76f, 0.5f), 0.2f);
            var glaseado = E.CrearMaterial("Glaseado", new Color(1f, 0.72f, 0.84f), 0.55f);
            var nata = E.CrearMaterial("Nata", new Color(1f, 0.97f, 0.93f), 0.45f);
            var chocolate = E.CrearMaterial("Chocolate", new Color(0.45f, 0.26f, 0.18f), 0.5f);
            var plato = E.CrearMaterial("PlatoTarta", new Color(0.92f, 0.92f, 0.98f), 0.8f);

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            A.Camara();
            var mapa = new GameObject("Mapa").transform;

            // --- La tarta: piso de abajo, piso de arriba, glaseado, nata y el plato ---
            A.Disco(mapa, "PisoAbajo", Vector3.zero, 0f, Radio, 1.6f, bizcocho);
            M.Decorado(PrimitiveType.Cylinder, "GlaseadoAbajo", mapa, new Vector3(0f, 0.005f, 0f), new Vector3(Radio * 2f - 0.3f, 0.005f, Radio * 2f - 0.3f), glaseado);
            M.Decorado(PrimitiveType.Cylinder, "Plato", mapa, new Vector3(0f, -1.7f, 0f), new Vector3(Radio * 2f + 3f, 0.1f, Radio * 2f + 3f), plato);
            A.Disco(mapa, "PisoArriba", Vector3.zero, AlturaArriba, RadioArriba, AlturaArriba + 0.2f, chocolate);
            M.Decorado(PrimitiveType.Cylinder, "GlaseadoArriba", mapa, new Vector3(0f, AlturaArriba + 0.005f, 0f), new Vector3(RadioArriba * 2f - 0.2f, 0.005f, RadioArriba * 2f - 0.2f), glaseado);

            // Bolitas de nata alrededor de cada piso (decorado)
            for (int i = 0; i < 28; i++)
            {
                float a = i * Mathf.PI * 2f / 28f;
                M.Decorado(PrimitiveType.Sphere, "Nata", mapa, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (Radio - 0.15f) + Vector3.up * 0.05f, Vector3.one * 0.55f, nata);
            }
            for (int i = 0; i < 14; i++)
            {
                float a = i * Mathf.PI * 2f / 14f;
                M.Decorado(PrimitiveType.Sphere, "Nata", mapa, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (RadioArriba - 0.1f) + Vector3.up * (AlturaArriba + 0.05f), Vector3.one * 0.45f, nata);
            }

            // --- Rampas para subir al piso de arriba (delante y detrás) ---
            float largo = Radio * 0.33f;
            float angulo = Mathf.Atan2(AlturaArriba, largo) * Mathf.Rad2Deg;
            foreach (float lado in new[] { -1f, 1f })
            {
                float r = RadioArriba + largo * 0.5f - 0.1f;
                var rampa = E.Bloque("Rampa", mapa, new Vector3(0f, AlturaArriba * 0.5f - 0.1f, r * lado),
                                     new Vector3(2.2f, 0.3f, Mathf.Sqrt(largo * largo + AlturaArriba * AlturaArriba) + 0.3f), nata);
                rampa.transform.rotation = Quaternion.Euler(lado < 0f ? -angulo : angulo, 0f, 0f);
            }

            // --- Velas gigantes en el piso de abajo (obstáculos para esconderse) ---
            foreach (float grados in new[] { 30f, 90f, 150f, 210f, 270f, 330f })
            {
                float a = grados * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 8.6f;
                var vela = A.Kit("Vela", mapa, pos, grados);
                var capsula = vela.AddComponent<CapsuleCollider>();
                capsula.radius = 0.4f;
                capsula.height = 2.6f;
                capsula.center = new Vector3(0f, 1.3f, 0f);
            }
            A.Vacio(mapa);

            // --- Trampas: barredora en lo alto, guantes en el borde y setas para subir de un bote ---
            var trampas = M.Grupo(mapa, "Trampas");
            A.Barredora(trampas, new Vector3(0f, AlturaArriba, 0f), 40f, "Barredora", obstaculo);
            A.Guante(trampas, "Guante izquierdo", new Vector3(-(Radio - 0.6f), 0f, 0f), Vector3.right, 0f);
            A.Guante(trampas, "Guante derecho", new Vector3(Radio - 0.6f, 0f, 0f), Vector3.left, 2.5f);
            A.Seta(mapa, new Vector3(-6.3f, 0f, 2.5f), 0f);
            A.Seta(mapa, new Vector3(6.3f, 0f, -2.5f), 180f);

            var zonaSaboteadores = A.CabinaSaboteador(mapa, new Vector3(-4.5f, 0f, -9.2f));
            A.Publico(mapa, 18f, new[] { -100f, -60f, -20f, 20f, 60f, 100f }, new[] { -120f, -40f, 40f, 120f }, "LA CORONA");
            var gestor = A.Jugadores(prefabBicho, A.EnCirculo(4, 7.5f), Vector3.zero);

            // --- La corona ---
            var coronaGo = new GameObject("Corona");
            coronaGo.transform.SetParent(mapa, false);
            coronaGo.transform.position = new Vector3(0f, AlturaArriba + 1.5f, 2.2f);
            A.Kit("Corona", coronaGo.transform, new Vector3(0f, -0.3f, 0f), 0f, 1.25f, local: true);
            var esfera = coronaGo.AddComponent<SphereCollider>();
            esfera.radius = 0.45f;
            var rb = coronaGo.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.angularDamping = 1f;
            var corona = coronaGo.AddComponent<Corona>();
            var aparicion = new GameObject("AparicionCorona").transform;
            aparicion.SetParent(mapa, false);
            aparicion.position = coronaGo.transform.position;

            // --- Ronda ---
            var rondaGo = new GameObject("RondaCorona");
            var ronda = rondaGo.AddComponent<RondaCorona>();
            ronda.gestor = gestor;
            ronda.zonaSaboteadores = zonaSaboteadores;
            ronda.saboteadorMaquina = true;
            ronda.agresividadMaquina = 0.35f;
            ronda.corona = corona;
            ronda.aparicionCorona = aparicion;
            ronda.centroArena = rondaGo.transform;
            ronda.radioArena = Radio - 0.5f;
            ronda.alturaPisoAlto = AlturaArriba;
            ronda.radioPisoAlto = RadioArriba;

            A.Guardar(escena, RutaEscena, rondaGo);
        }
    }
}
