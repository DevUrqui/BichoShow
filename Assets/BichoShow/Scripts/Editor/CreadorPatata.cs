using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;
using M = BichoShow.Herramientas.CreadorMudanza;
using A = BichoShow.Herramientas.CreadorArena;

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
            var bicho = A.Preparar(RutaEscena, "la Patata caliente", out var obstaculo);
            if (bicho == null) return;
            CrearEscena(bicho, obstaculo);
            Debug.Log("Bicho Show: escena de la Patata caliente creada. Para que el show la use, rehaz también la escena del menú.");
        }

        static void CrearEscena(GameObject prefabBicho, Material obstaculo)
        {
            var suelo = E.CrearMaterial("SueloPatata", new Color(0.88f, 0.84f, 0.97f), 0.3f);
            var anilloRosa = E.CrearMaterial("AnilloPatata", new Color(1f, 0.55f, 0.75f), 0.35f);
            var anilloOro = E.CrearMaterial("AnilloPatataOro", new Color(1f, 0.82f, 0.35f), 0.5f);
            var base_ = E.CrearMaterial("BasePatata", new Color(0.35f, 0.2f, 0.55f), 0.3f);

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            A.Camara();
            var mapa = new GameObject("Mapa").transform;

            // --- Plató redondo: disco, dianas pintadas y pie que baja al vacío ---
            A.Disco(mapa, "Suelo", Vector3.zero, 0f, Radio, 1f, suelo);
            (float radio, Material mat)[] anillos = { (9.6f, anilloRosa), (8.6f, suelo), (5.6f, anilloOro), (4.8f, suelo), (2.2f, anilloRosa) };
            float y = 0.004f;
            foreach (var (radio, mat) in anillos)
            {
                M.Decorado(PrimitiveType.Cylinder, "Diana", mapa, new Vector3(0f, y, 0f), new Vector3(radio * 2f, 0.004f, radio * 2f), mat);
                y += 0.002f;
            }
            M.Decorado(PrimitiveType.Cylinder, "Borde", mapa, new Vector3(0f, -0.55f, 0f), new Vector3(Radio * 2f + 0.5f, 0.35f, Radio * 2f + 0.5f), anilloOro);
            M.Decorado(PrimitiveType.Cylinder, "Pie", mapa, new Vector3(0f, -6.5f, 0f), new Vector3(7f, 6f, 7f), base_);
            A.Vacio(mapa);

            // --- Trampas: barredora en el centro, guantes en el borde y setas rebotadoras ---
            var trampas = M.Grupo(mapa, "Trampas");
            A.Barredora(trampas, Vector3.zero, 45f, "Barredora", obstaculo);
            A.Guante(trampas, "Guante izquierdo", new Vector3(-(Radio - 0.6f), 0f, 2.5f), Vector3.right, 0f);
            A.Guante(trampas, "Guante derecho", new Vector3(Radio - 0.6f, 0f, -2.5f), Vector3.left, 2.5f);
            foreach (float angulo in new[] { 30f, 150f, 270f })
            {
                float a = angulo * Mathf.Deg2Rad;
                A.Seta(mapa, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 6.8f, angulo);
            }

            var zonaSaboteadores = A.CabinaSaboteador(mapa, new Vector3(0f, 0f, -9.4f));
            A.Publico(mapa, 17f, new[] { -100f, -60f, -20f, 20f, 60f, 100f }, new[] { -120f, -40f, 40f, 120f }, "PATATA CALIENTE");
            var gestor = A.Jugadores(prefabBicho, A.EnCirculo(4, 6f), Vector3.zero);

            // --- La bomba (apagada hasta que empieza la ronda) ---
            var bombaGo = new GameObject("Bomba");
            bombaGo.transform.SetParent(mapa, false);
            var modeloBomba = A.Kit("Bomba", bombaGo.transform, Vector3.zero, 0f, 1.1f, local: true);
            var bomba = bombaGo.AddComponent<Bomba>();
            bomba.modelo = modeloBomba.transform;
            bombaGo.SetActive(false);

            // --- Ronda ---
            var rondaGo = new GameObject("RondaPatata");
            var ronda = rondaGo.AddComponent<RondaPatata>();
            ronda.gestor = gestor;
            ronda.zonaSaboteadores = zonaSaboteadores;
            ronda.saboteadorMaquina = true;
            ronda.agresividadMaquina = 0.35f;
            ronda.bomba = bomba;
            ronda.centroArena = rondaGo.transform;
            ronda.radioArena = Radio - 0.5f;

            A.Guardar(escena, RutaEscena, rondaGo);
        }
    }
}
