using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;
using M = BichoShow.Herramientas.CreadorMudanza;
using A = BichoShow.Herramientas.CreadorArena;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Menú "BichoShow > Crear escena: Pintabichos".
    /// Una pista cuadrada de baldosas blancas flotando en el plató, con una barredora en el
    /// centro, guantes en dos bordes y público. Rojos salen por la izquierda, azules por la derecha.
    /// </summary>
    public static class CreadorPintura
    {
        const string RutaEscena = E.CarpetaEscenas + "/Pintura.unity";
        const int Lado = 14;
        const float Paso = 1.4f;

        static readonly Color Rojo = new Color(1f, 0.38f, 0.35f);
        static readonly Color Azul = new Color(0.3f, 0.6f, 1f);

        [MenuItem("BichoShow/Crear escena: Pintabichos")]
        public static void Crear()
        {
            var bicho = A.Preparar(RutaEscena, "Pintabichos", out var obstaculo);
            if (bicho == null) return;
            CrearEscena(bicho, obstaculo);
            Debug.Log("Bicho Show: escena de Pintabichos creada. Para que el show la use, rehaz también la escena del menú.");
        }

        static void CrearEscena(GameObject prefabBicho, Material obstaculo)
        {
            var base_ = E.CrearMaterial("BasePintura", new Color(0.3f, 0.26f, 0.38f), 0.3f);
            var neutra = E.CrearMaterial("BaldosaNeutra", new Color(0.93f, 0.92f, 0.96f), 0.45f);
            var roja = E.CrearMaterial("BaldosaRoja", Color.Lerp(Rojo, Color.white, 0.1f), 0.55f);
            var azul = E.CrearMaterial("BaldosaAzul", Color.Lerp(Azul, Color.white, 0.1f), 0.55f);

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            A.Camara(15f, 30f);
            var mapa = new GameObject("Mapa").transform;

            // --- Base (se pisa esta) y baldosas (solo se ven) ---
            float lado = Lado * Paso;
            E.Bloque("Base", mapa, new Vector3(0f, -0.55f, 0f), new Vector3(lado + 0.4f, 1f, lado + 0.4f), base_);
            var grupoBaldosas = M.Grupo(mapa, "Baldosas");
            var origen = new Vector3(-(Lado - 1) * Paso * 0.5f, 0f, -(Lado - 1) * Paso * 0.5f);
            var baldosas = new List<Renderer>();
            for (int z = 0; z < Lado; z++)
                for (int x = 0; x < Lado; x++)
                {
                    var b = M.Decorado(PrimitiveType.Cube, $"Baldosa {x},{z}", grupoBaldosas, origen + new Vector3(x * Paso, -0.025f, z * Paso),
                                       new Vector3(Paso - 0.08f, 0.06f, Paso - 0.08f), neutra);
                    b.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    baldosas.Add(b.GetComponent<Renderer>());
                }
            A.Vacio(mapa);

            // --- Trampas: barredora en el centro y guantes en los bordes de delante y detrás ---
            var trampas = M.Grupo(mapa, "Trampas");
            A.Barredora(trampas, Vector3.zero, 40f, "Barredora", obstaculo);
            A.Guante(trampas, "Guante de atrás", new Vector3(-3f, 0f, lado * 0.5f - 0.6f), Vector3.back, 0f, 5f);
            A.Guante(trampas, "Guante de delante", new Vector3(3f, 0f, -lado * 0.5f + 0.6f), Vector3.forward, 2.5f, 5f);

            var zonaSaboteadores = A.CabinaSaboteador(mapa, new Vector3(-7f, 0f, -8.6f));
            A.Publico(mapa, 18f, new[] { -100f, -60f, -20f, 20f, 60f, 100f }, new[] { -120f, -40f, 40f, 120f }, "PINTABICHOS");

            // Jugadores: 1 y 3 (rojos) a la izquierda; 2 y 4 (azules) a la derecha.
            var apariciones = new[] { new Vector3(-7f, 0f, 2f), new Vector3(7f, 0f, 2f), new Vector3(-7f, 0f, -2f), new Vector3(7f, 0f, -2f) };
            var gestor = A.Jugadores(prefabBicho, apariciones, Vector3.zero);

            // --- Plantilla del bote de pintura (apagada) ---
            var bote = new GameObject("BotePintura");
            bote.transform.SetParent(mapa, false);
            bote.transform.position = new Vector3(0f, -50f, 0f);
            A.Kit("BotePintura", bote.transform, Vector3.zero, 0f, 1.4f, local: true);
            bote.SetActive(false);

            // --- Ronda ---
            var rondaGo = new GameObject("RondaPintura");
            var ronda = rondaGo.AddComponent<RondaPintura>();
            ronda.gestor = gestor;
            ronda.zonaSaboteadores = zonaSaboteadores;
            ronda.saboteadorMaquina = true;
            ronda.agresividadMaquina = 0.35f;
            ronda.baldosas = baldosas.ToArray();
            ronda.columnas = Lado;
            ronda.filas = Lado;
            ronda.origen = origen;
            ronda.paso = Paso;
            ronda.neutra = neutra;
            ronda.roja = roja;
            ronda.azul = azul;
            ronda.plantillaBote = bote;
            ronda.colores = new[] { Rojo, Azul };

            A.Guardar(escena, RutaEscena, rondaGo);
        }
    }
}
