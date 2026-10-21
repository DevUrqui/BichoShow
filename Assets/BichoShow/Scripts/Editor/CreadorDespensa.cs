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
    /// Menú "BichoShow > Crear escena: Despensa Deslizante". Carrera por la encimera de una cocina
    /// (a lo largo de +Z, unos 350 m), con cuatro arcos de control:
    ///  · Zona 1 (z 0-95): salida acolchada con bajada y mantel de cuadros con cuatro rodillos de
    ///    amasar que ruedan hacia la salida y vuelven; el último, por encima de un tramo de masa
    ///    pegajosa que frena. Saleros en los bordes que echan sal (resbala).
    ///  · Zona 2 (z 95-157): el fregadero de agua jabonosa rosa (caer = volver al arco). Por el
    ///    centro, tres esponjas que se hunden con el peso y se empapan; por los lados, tapones que
    ///    giran. Tres grifos gigantes barren el fregadero con su chorro.
    ///  · Zona 3 (z 157-237): rampa de mantequilla de 70 m (resbala; si saltas, patinas abajo), con
    ///    tomates y arándanos rodando, muros de refugio y una lámpara de calor que derrite franjas.
    ///  · Zona 4 (z 237-288): tres filas de colchones elásticos que van y vienen, sobre una cinta
    ///    que te devuelve.
    ///  · Zona 5 (z 288-358): la tostadora gigante: tres ranuras demasiado anchas para saltar, que
    ///    se cruzan sobre las tostadas cuando saltan. Después, la meta y el podio.
    /// Todo va sobre la encimera; caer a ella (o al agua, o a una ranura) devuelve al último arco.
    /// </summary>
    public static class CreadorDespensa
    {
        public const string RutaEscena = E.CarpetaEscenas + "/Despensa.unity";
        /// <summary>Súbela al cambiar el generador (EscenasAnticuadas avisa al darle a Play).</summary>
        public const int Version = 3;

        const float Encimera = -4f;            // la encimera de la cocina, debajo de todo
        const float ZRampa0 = 157f, ZRampa1 = 225f, AltoRampa = 16f;
        static readonly float LargoRampa = Mathf.Sqrt((ZRampa1 - ZRampa0) * (ZRampa1 - ZRampa0) + AltoRampa * AltoRampa);
        /// <summary>Las tres filas de colchones (z del centro).</summary>
        static readonly float[] FilasColchones = { 242f, 256f, 270f };
        /// <summary>Las ranuras de la tostadora (desde, hasta).</summary>
        static readonly Vector2[] Ranuras = { new Vector2(293f, 300f), new Vector2(304f, 311f), new Vector2(315f, 322f) };
        const float ZTostadora0 = 288f, ZTostadora1 = 326f;
        const float ZMeta = 336f;

        static Transform mapa, suelos, obstaculos, decorado, rutaBots;
        static readonly List<PuntoRuta> ruta = new List<PuntoRuta>();
        static Material azul, azulOscuro, mantel, metal, aguaRosa, mantequilla, derretida, madera, pared, armario, pomo;
        static Material ventana, cinta, marcaCinta, sal, agua, rosa, amarillo, cian, naranja, rojo, blanco, particula;
        static Material masa, tostadoraMat, ranura, resistencia, tostada, corteza;

        [MenuItem("BichoShow/Crear escena: Despensa Deslizante")]
        public static void Crear()
        {
            var bicho = A.Preparar(RutaEscena, "Despensa Deslizante", out _);
            if (bicho == null) return;
            CrearEscena(bicho);
            Debug.Log("Bicho Show: escena de Despensa Deslizante creada (entra sola en el sorteo del show).");
        }

        static void CrearMateriales()
        {
            azul = E.CrearMaterial("DespensaAzul", new Color(0.36f, 0.55f, 0.95f), 0.35f);
            azulOscuro = E.CrearMaterial("DespensaAzulOscuro", new Color(0.2f, 0.3f, 0.68f), 0.3f);
            mantel = TelasCostura.Tela("DespensaMantel", new Color(0.92f, 0.18f, 0.2f), TelasCostura.Vichy, 2f, 0.2f);
            metal = E.CrearMaterial("DespensaMetal", new Color(0.76f, 0.79f, 0.84f), 0.85f);
            aguaRosa = E.CrearMaterial("DespensaAguaRosa", new Color(1f, 0.55f, 0.82f), 0.95f);
            mantequilla = E.CrearMaterial("DespensaMantequilla", new Color(1f, 0.87f, 0.36f), 0.8f);
            derretida = E.CrearMaterial("DespensaDerretida", new Color(1f, 0.62f, 0.15f), 0.97f);
            madera = TelasCostura.Tela("DespensaEncimera", new Color(0.88f, 0.66f, 0.44f), TelasCostura.Madera, 4f, 0.3f);
            pared = TelasCostura.Tela("DespensaPared", new Color(0.76f, 0.92f, 0.88f), TelasCostura.PapelPintado, 3f, 0.2f);
            armario = E.CrearMaterial("DespensaArmario", new Color(0.96f, 0.88f, 0.74f), 0.3f);
            pomo = E.CrearMaterial("DespensaPomo", new Color(0.7f, 0.5f, 0.3f), 0.5f);
            ventana = E.CrearMaterial("DespensaVentana", new Color(0.72f, 0.9f, 1f), 0.9f);
            cinta = E.CrearMaterial("DespensaCinta", new Color(0.22f, 0.22f, 0.27f), 0.3f);
            marcaCinta = E.CrearMaterial("DespensaMarcaCinta", new Color(1f, 0.8f, 0.2f), 0.3f);
            sal = E.CrearMaterial("DespensaSal", new Color(1f, 1f, 1f), 0.6f);
            agua = E.CrearMaterial("DespensaChorro", new Color(0.72f, 0.92f, 1f), 0.95f);
            rosa = E.CrearMaterial("DespensaRosa", new Color(1f, 0.42f, 0.66f), 0.35f);
            amarillo = E.CrearMaterial("DespensaAmarillo", new Color(1f, 0.8f, 0.22f), 0.35f);
            cian = E.CrearMaterial("DespensaCian", new Color(0.36f, 0.86f, 0.96f), 0.35f);
            naranja = E.CrearMaterial("DespensaNaranja", new Color(1f, 0.55f, 0.2f), 0.35f);
            rojo = E.CrearMaterial("DespensaRojo", new Color(0.95f, 0.25f, 0.22f), 0.4f);
            blanco = E.CrearMaterial("DespensaBlanco", new Color(0.97f, 0.96f, 0.93f), 0.3f);
            masa = E.CrearMaterial("DespensaMasa", new Color(0.98f, 0.88f, 0.7f), 0.25f);
            tostadoraMat = E.CrearMaterial("DespensaTostadora", new Color(0.93f, 0.9f, 0.84f), 0.75f);
            ranura = E.CrearMaterial("DespensaRanura", new Color(0.12f, 0.1f, 0.1f), 0.2f);
            resistencia = E.CrearMaterial("DespensaResistencia", new Color(1f, 0.45f, 0.1f), 0.9f);
            tostada = E.CrearMaterial("DespensaTostada", new Color(0.92f, 0.7f, 0.38f), 0.25f);
            corteza = E.CrearMaterial("DespensaCorteza", new Color(0.62f, 0.38f, 0.16f), 0.25f);
            particula = AssetDatabase.LoadAssetAtPath<Material>($"{E.CarpetaMateriales}/Particula.mat");
            if (particula == null)
            {
                particula = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
                AssetDatabase.CreateAsset(particula, $"{E.CarpetaMateriales}/Particula.mat");
            }
        }

        static void CrearEscena(GameObject prefabBicho)
        {
            CrearMateriales();
            ruta.Clear();
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camara = A.Camara(15f, 36f);
            camara.centroSinJugadores = new Vector3(0f, 1.5f, 8f);
            camara.adelanto = 9f;          // en una carrera hay que ver lo que viene
            camara.anticipacion = 0.5f;    // los colchones lanzan rápido
            camara.ColocarAlInstante();

            mapa = new GameObject("Mapa").transform;
            new GameObject(EscenasAnticuadas.Marca + Version);
            suelos = M.Grupo(mapa, "Suelos");
            obstaculos = M.Grupo(mapa, "Obstaculos");
            decorado = M.Grupo(mapa, "Decorado");
            rutaBots = M.Grupo(mapa, "RutaBots");
            camara.gameObject.AddComponent<CamaraSinEstorbos>().raices = new[] { decorado };

            var controles = new List<PuntoControlCarrera>();
            var rodillos = Zona1();
            controles.Add(PuntoControl(1, 91.5f, -12f, 12f, 0f, "ZONA 2", cian));
            var (esponjas, tapones) = Zona2();
            controles.Add(PuntoControl(2, 151f, -12f, 12f, 0f, "ZONA 3", amarillo));
            var frutas = Zona3();
            controles.Add(PuntoControl(3, 229f, -9f, 9f, AltoRampa, "ZONA 4", naranja));
            var colchones = Zona4();
            controles.Add(PuntoControl(4, 282f, -9f, 9f, AltoRampa, "ZONA 5", rosa));
            var tostadas = Zona5();
            var (meta, podio) = Meta();
            Plato();

            // Caer a la encimera (fuera de la carrera) devuelve al último arco cruzado.
            var caida = M.ZonaTrigger<ZonaMortal>("Encimera (caida)", mapa, new Vector3(0f, Encimera + 0.9f, 175f), new Vector3(140f, 1.8f, 420f));
            caida.motivoObjeto = "se ha caído de la encimera";

            var apariciones = new[] { new Vector3(-4.5f, 1.6f, 4f), new Vector3(-1.5f, 1.6f, 5f), new Vector3(1.5f, 1.6f, 5f), new Vector3(4.5f, 1.6f, 4f) };
            var gestor = A.Jugadores(prefabBicho, apariciones, new Vector3(0f, 1.6f, 20f));

            var inicio = RutaBots(rodillos, esponjas, tapones, colchones, tostadas);

            var rondaGo = new GameObject("RondaDespensa");
            var ronda = rondaGo.AddComponent<RondaDespensa>();
            ronda.gestor = gestor;
            ronda.saboteadorMaquina = false;
            ronda.puntosControl = controles.ToArray();
            ronda.meta = meta;
            ronda.zSalida = 0f;
            ronda.zMeta = ZMeta;
            ronda.duracionMinima = 180f;
            ronda.tiempoTrasPrimero = 25f;
            ronda.frutas = frutas;
            ronda.podio = podio;
            ronda.ruta = ruta.ToArray();
            ronda.inicioRuta = inicio;

            Ambientes.Aplicar(Ambientes.Tema.Cocina);
            EditorSceneManager.SaveScene(escena, RutaEscena);
            E.AnadirAlBuild(RutaEscena);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rondaGo;
        }

        // =====================================================================
        // Zona 1: el mantel de los rodillos (z 0-95)
        // =====================================================================

        static RodilloAmasar[] Zona1()
        {
            // Salida: plataforma azul acolchada y una bajadita para coger velocidad.
            Losa("Salida", -9f, 9f, 0f, 10f, 1.5f, azul);
            Rampa("Bajada", 0f, 18f, 10f, 1.5f, 20.3f, 0f, azul);
            Arco("BICHO SHOW", 3f, 1.5f, 1f, rosa, true);
            // El mantel de cuadros.
            Losa("Mantel", -11f, 11f, 20f, 88f, 0f, mantel);

            var g = M.Grupo(obstaculos, "Zona 1");
            // Cuatro rodillos, cada uno en su tramo, desfasados. El último, encima de la masa.
            var rodillos = new[]
            {
                Rodillo(g, "Rodillo 1", 21.5f, 33f, 0f),
                Rodillo(g, "Rodillo 2", 35f, 46.5f, 1.4f),
                Rodillo(g, "Rodillo 3", 48.5f, 60f, 2.8f),
                Rodillo(g, "Rodillo 4 (sobre la masa)", 74f, 86.5f, 0.7f),
            };
            // Saleros en los bordes: por donde se bordean los rodillos.
            int k = 0;
            foreach (float z in new[] { 27f, 40.5f, 54f, 67f })
                foreach (float x in new[] { -9.9f, 9.9f })
                    Salero(g, new Vector3(x, 0f, z), (k++ * 0.37f) % 1f);
            // La masa de pan pegajosa (z 76,5-85,5), de lado a lado.
            Masa(g, 81f, 9f);
            // Raíles de los rodillos (decorado) a los lados del mantel.
            foreach (float x in new[] { -10.95f, 10.95f })
                M.Decorado(PrimitiveType.Cube, "Rail", decorado, new Vector3(x, 0.08f, 54f), new Vector3(0.12f, 0.16f, 66f), metal);
            return rodillos;
        }

        static RodilloAmasar Rodillo(Transform padre, string nombre, float zDesde, float zHasta, float desfase)
        {
            const float radio = 0.9f;
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = new Vector3(0f, radio, zHasta);
            go.AddComponent<Rigidbody>().isKinematic = true;
            var cuerpo = go.AddComponent<CapsuleCollider>();
            cuerpo.direction = 0;
            cuerpo.radius = radio;
            cuerpo.height = 11f + radio * 2f;
            foreach (float s in new[] { -1f, 1f })
            {
                var mango = go.AddComponent<CapsuleCollider>();
                mango.direction = 0;
                mango.radius = 0.3f;
                mango.height = 2.9f;
                mango.center = new Vector3(s * 6.75f, 0f, 0f);
                var pomo_ = go.AddComponent<SphereCollider>();
                pomo_.radius = 0.38f;
                pomo_.center = new Vector3(s * 7.85f, 0f, 0f);
            }
            var giro = new GameObject("Rueda").transform;
            giro.SetParent(go.transform, false);
            Kit("Rodillo", giro, Vector3.zero, 0f, null, 1f, local: true);
            var r = go.AddComponent<RodilloAmasar>();
            r.zDesde = zDesde;
            r.zHasta = zHasta;
            r.desfase = desfase;
            r.radio = radio;
            r.visual = giro;
            r.velocidadIda = 6f;
            r.velocidadVuelta = 4.5f;
            r.pausa = 0.5f;
            return r;
        }

        static void Salero(Transform padre, Vector3 pie, float desfase)
        {
            var go = new GameObject($"Salero {pie.x:0} {pie.z:0}");
            go.transform.SetParent(padre, false);
            go.transform.position = pie;
            go.AddComponent<Rigidbody>().isKinematic = true;
            var capsula = go.AddComponent<CapsuleCollider>();
            capsula.radius = 1.05f;
            capsula.height = 3.4f;
            capsula.center = new Vector3(0f, 1.7f, 0f);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            Kit("Salero", visual, Vector3.zero, 0f, null, 1f, local: true);
            var circulo = M.Decorado(PrimitiveType.Cylinder, "Sal en el suelo", go.transform, new Vector3(0f, 0.03f, 0f), new Vector3(9f, 0.02f, 9f), sal, local: true);
            var granos = M.Particulas("Granos de sal", go.transform, pie + Vector3.up * 3.3f, Color.white, new Color(0.9f, 0.95f, 1f), 60, 3f, 6f, particula);
            var main = granos.main;
            main.playOnAwake = false;
            main.gravityModifier = 1f;
            var s = go.AddComponent<SaleroGigante>();
            s.desfase = desfase;
            s.periodo = 3f;
            s.periodoPrisa = 1.5f;
            s.radioSal = 4.5f;
            s.agarreSal = 0.08f;
            s.visual = visual;
            s.circuloSal = circulo;
            s.granos = granos;
        }

        /// <summary>Masa de pan cruda extendida de lado a lado del mantel: frena a quien la pisa.</summary>
        static void Masa(Transform padre, float z, float largo)
        {
            var go = new GameObject("Masa pegajosa");
            go.transform.SetParent(padre, false);
            go.transform.position = new Vector3(0f, 0f, z);
            var m = go.AddComponent<MasaPegajosa>();
            m.tamano = new Vector2(22f, largo);
            M.Decorado(PrimitiveType.Cube, "Masa", go.transform, new Vector3(0f, 0.06f, 0f), new Vector3(21.6f, 0.12f, largo), masa, local: true);
            // Bultos y burbujas de la masa.
            var azar = new System.Random(11);
            for (int i = 0; i < 26; i++)
            {
                float x = (float)(azar.NextDouble() * 2 - 1) * 10f, dz = (float)(azar.NextDouble() * 2 - 1) * (largo * 0.5f - 0.6f);
                float r = 0.4f + (float)azar.NextDouble() * 0.7f;
                M.Decorado(PrimitiveType.Sphere, "Bulto", go.transform, new Vector3(x, 0.08f, dz), new Vector3(r * 2f, r * 0.45f, r * 2f), masa, local: true);
            }
        }

        // =====================================================================
        // Zona 2: el fregadero espumoso (z 88-157)
        // =====================================================================

        static (EsponjaFregadero[] esponjas, PlataformaGiratoria[] tapones) Zona2()
        {
            const float z0 = 95f, z1 = 147f, interior = 10.5f;
            // La entrada y la salida del fregadero (acolchadas).
            Losa("Antes del fregadero", -12f, 12f, 88f, z0, 0f, azul);
            Losa("Despues del fregadero", -12f, 12f, z1, ZRampa0, 0f, azul);
            // El fregadero: paredes de metal altas a los lados (no se puede ir por el borde), fondo y agua.
            foreach (float s in new[] { -1f, 1f })
                E.Bloque("Pared del fregadero", suelos, new Vector3(s * (interior + 1f), 0f, (z0 + z1) * 0.5f), new Vector3(2f, 6f, z1 - z0), metal);
            M.Decorado(PrimitiveType.Cube, "Fondo del fregadero", decorado, new Vector3(0f, -3.2f, (z0 + z1) * 0.5f), new Vector3(interior * 2f, 0.4f, z1 - z0), metal);
            M.Decorado(PrimitiveType.Cube, "Agua jabonosa", decorado, new Vector3(0f, -1.05f, (z0 + z1) * 0.5f), new Vector3(interior * 2f, 0.1f, z1 - z0), aguaRosa);
            var agua_ = M.ZonaTrigger<ZonaMortal>("Agua (caida)", mapa, new Vector3(0f, -2f, (z0 + z1) * 0.5f), new Vector3(interior * 2f, 2.2f, z1 - z0));
            agua_.motivoObjeto = "se ha caído al agua del fregadero";
            // Espuma flotando.
            var azar = new System.Random(5);
            for (int i = 0; i < 34; i++)
            {
                float x = (float)(azar.NextDouble() * 2 - 1) * 9.5f;
                float z = z0 + 1f + (float)azar.NextDouble() * (z1 - z0 - 2f);
                if (Mathf.Abs(x) < 3.5f) x = Mathf.Sign(x) * (3.5f + (float)azar.NextDouble() * 1.2f);
                Kit("Espuma", decorado, new Vector3(x, -1.05f, z), (float)azar.NextDouble() * 360f, null, 0.8f + (float)azar.NextDouble() * 0.8f);
            }

            var g = M.Grupo(obstaculos, "Zona 2");
            // Centro: tres esponjas (rápido, pero se hunden y se empapan).
            var esponjas = new[] { Esponja(g, "Esponja 1", 104.5f), Esponja(g, "Esponja 2", 121.5f), Esponja(g, "Esponja 3", 138.5f) };
            // Lados: tapones que giran (más lentos, pero no se hunden).
            var tapones = new List<PlataformaGiratoria>();
            int k = 0;
            foreach (float x in new[] { -6.8f, 6.8f })
                for (float z = 98f; z < 145.5f; z += 4.6f, k++)
                    tapones.Add(Tapon(g, new Vector3(x, -0.4f, z), k % 2 == 0 ? rojo : azul, k % 2 == 0 ? 70f : -70f));
            // Tres grifos, en las dos paredes y a destiempo: ninguna ruta se libra del chorro.
            Grifo(g, new Vector3(11.5f, 3f, 118f), -163f, -107f, 0f);
            Grifo(g, new Vector3(-11.5f, 3f, 113f), 16f, 73f, 1.5f);
            Grifo(g, new Vector3(11.5f, 3f, 145f), -163f, -107f, 3f);
            return (esponjas, tapones.ToArray());
        }

        const float LargoEsponja = 13f;

        static EsponjaFregadero Esponja(Transform padre, string nombre, float z)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = new Vector3(0f, -0.3f, z);
            go.AddComponent<Rigidbody>().isKinematic = true;
            var caja = go.AddComponent<BoxCollider>();
            caja.size = new Vector3(6f, 1.2f, LargoEsponja);
            caja.center = new Vector3(0f, -0.6f, 0f);
            var visual = Kit("Esponja", go.transform, Vector3.zero, 0f, null, 1f, local: true);
            visual.transform.localScale = Vector3.Scale(visual.transform.localScale, new Vector3(1f, 1f, LargoEsponja / 11f));
            var espuma = M.Particulas("Espuma", go.transform, go.transform.position + Vector3.up * 0.2f, new Color(1f, 0.85f, 0.95f), Color.white, 50, 2f, 5f, particula);
            var main = espuma.main;
            main.playOnAwake = false;
            var e = go.AddComponent<EsponjaFregadero>();
            e.tamanoArriba = new Vector2(6f, LargoEsponja);
            e.alturaArriba = 0f;
            e.hundimientoPorBicho = 0.3f;
            e.limite = 2;
            e.tiempoEmpapar = 2.2f;
            e.visual = visual.transform;
            e.espuma = espuma;
            return e;
        }

        static PlataformaGiratoria Tapon(Transform padre, Vector3 arriba, Material color, float velocidad)
        {
            var go = new GameObject($"Tapon {arriba.x:0} {arriba.z:0}");
            go.transform.SetParent(padre, false);
            go.transform.position = arriba;
            go.AddComponent<Rigidbody>().isKinematic = true;
            const float radio = 1.1f;
            SoloColision(A.Disco(go.transform, "Disco", arriba, arriba.y, radio, 0.5f, metal));
            Kit("Tapon", go.transform, Vector3.zero, 0f, color, radio / 1.4f, local: true);
            var p = go.AddComponent<PlataformaGiratoria>();
            p.velocidad = velocidad;
            p.radio = radio;
            p.alturaArriba = 0f;
            p.balanceo = 0.15f;
            return p;
        }

        /// <summary>Grifo gigante en una pared: su caño de 16 m barre el fregadero con el chorro.</summary>
        static void Grifo(Transform padre, Vector3 base_, float anguloA, float anguloB, float desfase)
        {
            const float largo = 16f, alturaCano = 4.7f;
            var go = new GameObject("Grifo");
            go.transform.SetParent(padre, false);
            go.transform.position = base_;
            Kit("GrifoBase", go.transform, Vector3.zero, 0f, null, 1f, local: true);
            var cano = new GameObject("Cano").transform;
            cano.SetParent(go.transform, false);
            cano.localPosition = new Vector3(0f, alturaCano, 0f);
            Kit("GrifoCano", cano, Vector3.zero, 0f, null, 1f, local: true);
            // La columna de agua: de la boquilla al agua del fregadero.
            float arriba = base_.y + alturaCano - 0.55f, abajo = -1.05f;
            var columna = M.Decorado(PrimitiveType.Cylinder, "Chorro", cano, new Vector3(0f, (arriba + abajo) * 0.5f - (base_.y + alturaCano), largo),
                                     new Vector3(1.5f, (arriba - abajo) * 0.5f, 1.5f), agua, local: true);
            var gotas = M.Particulas("Gotas", cano, cano.TransformPoint(0f, -0.6f, largo), new Color(0.7f, 0.9f, 1f), Color.white, 25, 1f, 3f, particula);
            var main = gotas.main;
            main.playOnAwake = false;
            main.gravityModifier = 1.5f;
            gotas.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var chorro = go.AddComponent<ChorroGrifo>();
            chorro.cano = cano;
            chorro.columna = columna;
            chorro.gotas = gotas;
            chorro.largo = largo;
            chorro.anguloA = anguloA;
            chorro.anguloB = anguloB;
            chorro.alturaChorro = 4f;
            chorro.periodo = 4.5f;
            chorro.desfase = desfase;
            cano.localRotation = Quaternion.Euler(0f, anguloA, 0f);
        }

        // =====================================================================
        // Zona 3: la rampa de mantequilla (z 157-237)
        // =====================================================================

        static FrutaRodante[] Zona3()
        {
            float largo = LargoRampa;
            float angulo = Mathf.Atan2(AltoRampa, ZRampa1 - ZRampa0) * Mathf.Rad2Deg;
            Quaternion giro = Quaternion.Euler(-angulo, 0f, 0f);
            Rampa("Rampa de mantequilla", 0f, 14f, ZRampa0, 0f, ZRampa1, AltoRampa, mantequilla);

            var g = M.Grupo(obstaculos, "Zona 3");
            var rampaGo = new GameObject("Mantequilla");
            rampaGo.transform.SetParent(g, false);
            rampaGo.transform.SetPositionAndRotation(new Vector3(0f, 0f, ZRampa0), giro);
            var rampa = rampaGo.AddComponent<RampaMantequilla>();
            rampa.tamano = new Vector2(14f, largo);
            rampa.agarre = 0.5f;
            rampa.resbalonSalto = 8f;
            rampa.agarreDerretida = 0.05f;
            rampa.largoFranja = 10f;
            rampa.duracionFranja = 7f;
            rampa.descanso = 2f;
            var franja = M.Decorado(PrimitiveType.Cube, "Franja derretida", rampaGo.transform, new Vector3(0f, 0.03f, 0f), new Vector3(14f, 0.04f, rampa.largoFranja), derretida, local: true);
            rampa.franja = franja.transform;

            // La lámpara de calor, en un raíl por encima de la rampa.
            var lampara = new GameObject("Lampara de calor").transform;
            lampara.SetParent(g, false);
            lampara.position = rampaGo.transform.TransformPoint(0f, 5.5f, 0f);
            Kit("LamparaCalor", lampara, Vector3.zero, 0f, null, 1.4f, local: true);
            var bombilla = Kit("BombillaCalor", lampara, new Vector3(0f, -0.1f, 0f), 0f, null, 1.4f, local: true);
            var luz = new GameObject("Luz").AddComponent<Light>();
            luz.transform.SetParent(lampara, false);
            luz.transform.localPosition = new Vector3(0f, -1f, 0f);
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.6f, 0.25f);
            luz.range = 16f;
            luz.intensity = 4f;
            rampa.lampara = lampara;
            rampa.alturaLampara = 5.5f;
            rampa.bombilla = bombilla.GetComponentInChildren<Renderer>();
            rampa.luzLampara = luz;
            Vector3 railA = rampaGo.transform.TransformPoint(0f, 8.3f, 0f), railB = rampaGo.transform.TransformPoint(0f, 8.3f, largo);
            var rail = M.Decorado(PrimitiveType.Cube, "Rail de la lampara", decorado, (railA + railB) * 0.5f, new Vector3(0.4f, 0.4f, largo + 2f), metal);
            rail.transform.rotation = giro;
            foreach (var p in new[] { railA, railB })
                M.Decorado(PrimitiveType.Cylinder, "Poste del rail", decorado, new Vector3(9f, (p.y + Encimera) * 0.5f, p.z), new Vector3(0.5f, (p.y - Encimera) * 0.5f, 0.5f), metal);
            foreach (var p in new[] { railA, railB })
                M.Decorado(PrimitiveType.Cube, "Brazo del rail", decorado, new Vector3(4.5f, p.y, p.z), new Vector3(9f, 0.35f, 0.35f), metal);

            // Muros de refugio: en la base y por la rampa (la fruta revienta contra ellos).
            var refugios = new List<Transform>();
            refugios.Add(Refugio(g, new Vector3(-4f, 0f, ZRampa0 - 2.5f), Quaternion.identity));
            refugios.Add(Refugio(g, new Vector3(4f, 0f, ZRampa0 - 2.5f), Quaternion.identity));
            foreach (var (x, z) in new[] { (-4f, 10f), (3.5f, 24f), (-1.5f, 38f), (4f, 52f), (-3f, 62f) })
                refugios.Add(Refugio(g, rampaGo.transform.TransformPoint(x, 0f, z), giro));

            Vector3 arriba = new Vector3(0f, AltoRampa, ZRampa1 - 0.5f), abajo = new Vector3(0f, 0f, ZRampa0 + 0.5f);
            var tomates = Fruta(g, "Tomates", "Tomate", 12, 1f, arriba, abajo, new Vector2(0f, 3.2f), 10f, 1.1f, 0f, new Color(0.9f, 0.15f, 0.1f), refugios);
            var arandanos = Fruta(g, "Arandanos", "Arandano", 12, 0.7f, arriba, abajo, new Vector2(4.2f, 6.2f), 12f, 0.9f, 1f, new Color(0.35f, 0.3f, 0.8f), refugios);
            return new[] { tomates, arandanos };
        }

        static Transform Refugio(Transform padre, Vector3 pie, Quaternion giro)
        {
            var go = new GameObject("Muro de refugio");
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(pie, giro);
            var caja = go.AddComponent<BoxCollider>();
            caja.size = new Vector3(3f, 1.6f, 1.2f);
            caja.center = new Vector3(0f, 0.8f, 0f);
            Kit("MuroRefugio", go.transform, Vector3.zero, 0f, null, 1f, local: true);
            // La caja con la que choca la fruta (un cubo de 1 escalado al tamaño del muro).
            var choque = new GameObject("Caja para la fruta").transform;
            choque.SetParent(go.transform, false);
            choque.localPosition = new Vector3(0f, 0.8f, 0f);
            choque.localScale = new Vector3(3f, 1.6f, 1.2f);
            return choque;
        }

        static FrutaRodante Fruta(Transform padre, string nombre, string pieza, int cuantas, float radio, Vector3 arriba, Vector3 abajo,
                                  Vector2 carril, float velocidad, float cadencia, float zigzag, Color zumo, List<Transform> refugios)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            var piezas = new List<Rigidbody>();
            for (int i = 0; i < cuantas; i++)
            {
                var p = new GameObject($"{pieza} {i + 1}");
                p.transform.SetParent(go.transform, false);
                p.transform.position = arriba + Vector3.up * radio;
                var rb = p.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                p.AddComponent<SphereCollider>().radius = radio * 0.95f;
                Kit(pieza, p.transform, Vector3.zero, 0f, null, 1f, local: true);
                piezas.Add(rb);
            }
            var f = go.AddComponent<FrutaRodante>();
            f.piezas = piezas.ToArray();
            f.arriba = arriba;
            f.abajo = abajo;
            f.lado = Vector3.right;
            f.carril = carril;
            f.radio = radio;
            f.velocidad = velocidad;
            f.cadencia = cadencia;
            f.zigzag = zigzag;
            f.colorZumo = zumo;
            f.refugios = refugios.ToArray();
            return f;
        }

        // =====================================================================
        // Zona 4: las bandejas de postre (z 225-288)
        // =====================================================================

        static ColchonElastico[] Zona4()
        {
            float y = AltoRampa;
            Losa("Arriba de la rampa", -9f, 9f, ZRampa1, 237f, y, azul);
            Losa("Despues de los colchones", -9f, 9f, 278f, ZTostadora0, y, azul);

            var g = M.Grupo(obstaculos, "Zona 4");
            // Seis colchones en tres filas, con huecos entre ellos; van y vienen de lado, a destiempo.
            var colchones = new List<ColchonElastico>();
            int k = 0;
            foreach (float z in FilasColchones)
                foreach (float x in new[] { -3.5f, 3.5f })
                    colchones.Add(Colchon(g, new Vector3(x, y, z), (k++ * 0.17f + (x > 0f ? 0.5f : 0f)) % 1f));

            // Debajo, la cinta que te devuelve: te lleva hacia atrás y te tira al cubo (al último arco).
            CintaDeAbajo(239f, 276f, y - 9f);
            return colchones.ToArray();
        }

        static ColchonElastico Colchon(Transform padre, Vector3 pie, float desfase)
        {
            const float lado = 4.8f;
            var go = new GameObject($"Colchon {pie.x:0} {pie.z:0}");
            go.transform.SetParent(padre, false);
            go.transform.position = pie;
            var salto = go.AddComponent<BoxCollider>();
            salto.isTrigger = true;
            salto.size = new Vector3(lado, 1.2f, lado);
            salto.center = new Vector3(0f, 1.1f, 0f);
            var superficie = new GameObject("Superficie");
            superficie.transform.SetParent(go.transform, false);
            var caja = superficie.AddComponent<BoxCollider>();
            caja.size = new Vector3(lado, 0.8f, lado);
            caja.center = new Vector3(0f, 0.4f, 0f);
            var visual = Kit("Colchon", go.transform, Vector3.zero, 0f, null, lado / 6f, local: true);
            var c = go.AddComponent<ColchonElastico>();
            c.visual = visual.transform;
            c.vaiven = new Vector3(2.4f, 0f, 0f);
            c.periodoVaiven = 3.2f;
            c.desfaseVaiven = desfase;
            return c;
        }

        static void CintaDeAbajo(float z0, float z1, float arriba)
        {
            E.Bloque("Cinta (suelo)", suelos, new Vector3(0f, arriba - 0.3f, (z0 + z1) * 0.5f), new Vector3(18f, 0.6f, z1 - z0), cinta);
            var go = new GameObject("Cinta de abajo (motor)");
            go.transform.SetParent(obstaculos, false);
            go.transform.position = new Vector3(0f, arriba, (z0 + z1) * 0.5f);
            float largo = z1 - z0;
            var marcas = new List<Transform>();
            for (float z = -largo * 0.5f + 1f; z < largo * 0.5f; z += 3f)
            {
                var m = M.Decorado(PrimitiveType.Cube, "Marca", go.transform, new Vector3(0f, 0.02f, z), new Vector3(14f, 0.03f, 0.5f), marcaCinta, local: true);
                if (m != null) marcas.Add(m.transform);
            }
            var c = go.AddComponent<CintaTransportadora>();
            c.direccion = Vector3.back;
            c.velocidad = 4f;
            c.tamano = new Vector2(18f, largo);
            c.marcas = marcas.ToArray();
            // Paredes bajas a los lados (que nadie se escape de la cinta).
            foreach (float s in new[] { -1f, 1f })
                E.Bloque("Borde de la cinta", suelos, new Vector3(s * 9.3f, arriba + 0.5f, (z0 + z1) * 0.5f), new Vector3(0.6f, 1f, z1 - z0), azulOscuro);
            // Al final de la cinta, un cubo: quien cae ahí vuelve al último arco.
            var cubo = M.ZonaTrigger<ZonaMortal>("Cubo de la cinta", mapa, new Vector3(0f, arriba - 1.5f, z0 - 2f), new Vector3(18f, 3f, 3f));
            cubo.motivoObjeto = "se ha caído al cubo de la cinta";
            M.Decorado(PrimitiveType.Cylinder, "Cubo", decorado, new Vector3(0f, arriba - 2.5f, z0 - 2f), new Vector3(10f, 2f, 4f), azulOscuro);
        }

        // =====================================================================
        // Zona 5: la tostadora gigante (z 288-326)
        // =====================================================================

        /// <summary>Por encima de la tostadora: tres ranuras con dos tostadas cada una (por ranura, a contratiempo).</summary>
        static TostadaSaltarina[][] Zona5()
        {
            float y = AltoRampa;
            var g = M.Grupo(obstaculos, "Zona 5");
            // Lo de arriba de la tostadora, entre ranura y ranura.
            float desde = ZTostadora0;
            for (int i = 0; i <= Ranuras.Length; i++)
            {
                float hasta = i < Ranuras.Length ? Ranuras[i].x : ZTostadora1;
                E.Bloque("Tostadora (arriba)", suelos, new Vector3(0f, y - 0.25f, (desde + hasta) * 0.5f), new Vector3(18f, 0.5f, hasta - desde), tostadoraMat);
                if (i < Ranuras.Length) desde = Ranuras[i].y;
            }
            // El cuerpo de la tostadora: los costados, el fondo de las ranuras con su resistencia y la palanca.
            float centro = (ZTostadora0 + ZTostadora1) * 0.5f, largo = ZTostadora1 - ZTostadora0;
            foreach (float s in new[] { -1f, 1f })
                M.Decorado(PrimitiveType.Cube, "Costado de la tostadora", decorado, new Vector3(s * 9.4f, (y + Encimera) * 0.5f, centro), new Vector3(0.8f, y - Encimera, largo), tostadoraMat);
            M.Decorado(PrimitiveType.Cube, "Frente de la tostadora", decorado, new Vector3(0f, (y - 0.5f + Encimera) * 0.5f, ZTostadora0 + 0.4f), new Vector3(18f, y - 0.5f - Encimera, 0.8f), tostadoraMat);
            M.Decorado(PrimitiveType.Cube, "Palanca", decorado, new Vector3(10.6f, y - 3f, centro), new Vector3(1.6f, 0.8f, 3f), ranura);
            M.Decorado(PrimitiveType.Sphere, "Pomo de la palanca", decorado, new Vector3(11.6f, y - 3f, centro), new Vector3(1.2f, 1.2f, 1.2f), rojo);

            var todas = new TostadaSaltarina[Ranuras.Length][];
            for (int i = 0; i < Ranuras.Length; i++)
            {
                var r = Ranuras[i];
                float zc = (r.x + r.y) * 0.5f, ancho = r.y - r.x;
                M.Decorado(PrimitiveType.Cube, "Fondo de la ranura", decorado, new Vector3(0f, y - 4.5f, zc), new Vector3(18f, 0.4f, ancho), ranura);
                M.Decorado(PrimitiveType.Cube, "Resistencia", decorado, new Vector3(0f, y - 4.2f, zc), new Vector3(17f, 0.15f, 0.3f), resistencia);
                var quemadura = M.ZonaTrigger<ZonaMortal>($"Ranura {i + 1} (caida)", mapa, new Vector3(0f, y - 3.2f, zc), new Vector3(18f, 1.5f, ancho));
                quemadura.motivoObjeto = "se ha caído dentro de la tostadora";
                todas[i] = new[]
                {
                    Tostada(g, new Vector3(-4.45f, y, zc), ancho - 0.5f, (i * 0.23f) % 1f),
                    Tostada(g, new Vector3(4.45f, y, zc), ancho - 0.5f, (i * 0.23f + 0.5f) % 1f),
                };
            }
            return todas;
        }

        static TostadaSaltarina Tostada(Transform padre, Vector3 arriba, float largo, float desfase)
        {
            const float ancho = 8.5f, grueso = 0.8f;
            var go = new GameObject($"Tostada {arriba.x:0} {arriba.z:0}");
            go.transform.SetParent(padre, false);
            go.transform.position = arriba;
            go.AddComponent<Rigidbody>().isKinematic = true;
            var caja = go.AddComponent<BoxCollider>();
            caja.size = new Vector3(ancho, grueso, largo);
            caja.center = new Vector3(0f, -grueso * 0.5f, 0f);
            // Miga y corteza.
            M.Decorado(PrimitiveType.Cube, "Miga", go.transform, new Vector3(0f, -grueso * 0.5f, 0f), new Vector3(ancho - 0.5f, grueso, largo - 0.5f), tostada, local: true);
            foreach (float s in new[] { -1f, 1f })
            {
                M.Decorado(PrimitiveType.Cube, "Corteza", go.transform, new Vector3(s * (ancho * 0.5f - 0.25f), -grueso * 0.5f, 0f), new Vector3(0.5f, grueso + 0.04f, largo), corteza, local: true);
                M.Decorado(PrimitiveType.Cube, "Corteza", go.transform, new Vector3(0f, -grueso * 0.5f, s * (largo * 0.5f - 0.25f)), new Vector3(ancho, grueso + 0.04f, 0.5f), corteza, local: true);
            }
            var t = go.AddComponent<TostadaSaltarina>();
            t.tamano = new Vector2(ancho, largo);
            t.desfase = desfase;
            return t;
        }

        // =====================================================================
        // La meta y el podio (z 326-358)
        // =====================================================================

        static (MetaCarrera meta, Transform[] podio) Meta()
        {
            float y = AltoRampa;
            Losa("Bandeja de la meta", -9f, 9f, ZTostadora1, 358f, y, azul);
            var g = M.Grupo(obstaculos, "Meta");
            // Saleros a los lados antes de la meta (al vacío si te descuidas).
            Salero(g, new Vector3(-8f, y, 331f), 0.2f);
            Salero(g, new Vector3(8f, y, 331f), 0.7f);

            // La meta: línea de cuadros bajo el marcador, y el podio detrás.
            Kit("LineaMeta", decorado, new Vector3(0f, y, ZMeta), 180f, null, 1f);
            Kit("PortalMeta", decorado, new Vector3(0f, y, ZMeta), 180f, rosa, 1f);
            M.Cartel(decorado, "META", new Vector3(0f, y + 9.75f, ZMeta - 0.7f), 1.6f, Color.white);
            foreach (float s in new[] { -1f, 1f })
            {
                var poste = new GameObject("Poste de la meta (colision)");
                poste.transform.SetParent(suelos, false);
                poste.transform.position = new Vector3(s * 9.3f, y + 4.2f, ZMeta);
                poste.AddComponent<BoxCollider>().size = new Vector3(1f, 8.4f, 1f);
            }
            var meta = M.ZonaTrigger<MetaCarrera>("Meta", mapa, new Vector3(0f, y + 2f, ZMeta + 0.5f), new Vector3(18f, 4f, 1.5f));

            var podioGo = new GameObject("Podio").transform;
            podioGo.SetParent(suelos, false);
            podioGo.SetPositionAndRotation(new Vector3(0f, y, 349f), Quaternion.Euler(0f, 180f, 0f));
            Kit("Podio", podioGo, Vector3.zero, 0f, null, 1f, local: true);
            foreach (var (x, alto) in new[] { (0f, 1.5f), (2.3f, 1f), (-2.3f, 0.7f) })
            {
                var escalon = podioGo.gameObject.AddComponent<BoxCollider>();
                escalon.size = new Vector3(2.2f, alto, 2.2f);
                escalon.center = new Vector3(x, alto * 0.5f, 0f);
            }
            var sitios = new List<Transform>();
            foreach (var (nombre, local) in new[] { ("1º", new Vector3(0f, 1.6f, 0f)), ("2º", new Vector3(2.3f, 1.1f, 0f)), ("3º", new Vector3(-2.3f, 0.8f, 0f)), ("4º", new Vector3(-5.2f, 0.1f, 0.5f)) })
            {
                var sitio = new GameObject($"Sitio {nombre}").transform;
                sitio.SetParent(podioGo, false);
                sitio.localPosition = local;
                sitio.localRotation = Quaternion.identity; // el podio mira hacia la carrera (-Z)
                sitios.Add(sitio);
                if (nombre != "4º") M.Cartel(decorado, nombre, podioGo.TransformPoint(local.x, local.y * 0.5f, 1.2f), 0.7f, EstiloShow.MoradoOscuro);
            }
            Guirnalda(new Vector3(-8.5f, y, 344f), new Vector3(8.5f, y, 344f));
            return (meta, sitios.ToArray());
        }

        // =====================================================================
        // Plató y cocina
        // =====================================================================

        static void Plato()
        {
            var cocina = M.Grupo(decorado, "Cocina");
            // La encimera de madera, la pared con azulejos, los armarios y la ventana.
            E.Bloque("Encimera", suelos, new Vector3(0f, Encimera - 0.5f, 175f), new Vector3(90f, 1f, 420f), madera);
            M.Decorado(PrimitiveType.Cube, "Pared", cocina, new Vector3(38f, Encimera + 22f, 175f), new Vector3(1f, 52f, 420f), pared);
            for (float z = -10f; z < 380f; z += 14f)
            {
                if (Mathf.Abs(z - 124f) < 12f || Mathf.Abs(z - 264f) < 12f) continue; // ahí van las ventanas
                M.Decorado(PrimitiveType.Cube, "Armario", cocina, new Vector3(34.5f, Encimera + 30f, z), new Vector3(6f, 10f, 13.4f), armario);
                M.Decorado(PrimitiveType.Sphere, "Pomo", cocina, new Vector3(31.4f, Encimera + 26.5f, z + 5f), new Vector3(0.6f, 0.6f, 0.6f), pomo);
            }
            foreach (float zv in new[] { 124f, 264f })
            {
                M.Decorado(PrimitiveType.Cube, "Ventana", cocina, new Vector3(37.4f, Encimera + 26f, zv), new Vector3(0.3f, 14f, 20f), ventana);
                M.Decorado(PrimitiveType.Cube, "Marco", cocina, new Vector3(37.2f, Encimera + 26f, zv), new Vector3(0.4f, 0.8f, 21f), blanco);
                M.Decorado(PrimitiveType.Cube, "Marco", cocina, new Vector3(37.2f, Encimera + 26f, zv), new Vector3(0.4f, 15f, 0.8f), blanco);
            }
            Kit("Tostadora", cocina, new Vector3(27f, Encimera, 128f), -90f, null, 2.2f);
            Kit("Tarro", cocina, new Vector3(26f, Encimera, 52f), -90f, amarillo, 1.8f);
            Kit("Tarro", cocina, new Vector3(28f, Encimera, 200f), -90f, cian, 2.2f);
            Kit("Tarro", cocina, new Vector3(26f, Encimera, 330f), -90f, rosa, 2f);

            // Gradas de cubiertos con público, a los lados.
            Grada(new Vector3(-20f, 4f, 40f));
            Grada(new Vector3(20f, 4f, 120f));
            Grada(new Vector3(-20f, 8f, 190f));
            Grada(new Vector3(20f, 14f, 255f));
            Grada(new Vector3(-18f, 18f, 340f));
            Grada(new Vector3(18f, 18f, 340f));

            // Grúas de cámara en el borde de la encimera, siguiendo al pelotón.
            GruaCamara(new Vector3(-17f, Encimera, 90f));
            GruaCamara(new Vector3(-17f, Encimera, 230f));

            // Guirnaldas de banderitas y carteles del show.
            Guirnalda(new Vector3(-10.5f, 0f, 21f), new Vector3(-10.5f, 0f, 73f), 0.8f);
            Guirnalda(new Vector3(10.5f, 0f, 21f), new Vector3(10.5f, 0f, 73f), 0.8f);
            Guirnalda(new Vector3(-8.5f, AltoRampa, 226f), new Vector3(8.5f, AltoRampa, 226f));
            foreach (var (pos, texto) in new[] { (new Vector3(-13f, 0f, 86f), "ZONA 2"), (new Vector3(-13.5f, 0f, 152f), "ZONA 3"),
                                                  (new Vector3(-10.5f, AltoRampa, 232f), "ZONA 4"), (new Vector3(-10.5f, AltoRampa, 285f), "ZONA 5") })
                Pancarta(pos, texto);
        }

        static void Grada(Vector3 sitio)
        {
            var g = M.Grupo(decorado, "Grada");
            M.Decorado(PrimitiveType.Cube, "Plataforma", g, sitio + Vector3.down * 0.3f, new Vector3(10f, 0.6f, 7f), azul);
            M.Decorado(PrimitiveType.Cylinder, "Pata", g, new Vector3(sitio.x, (sitio.y + Encimera) * 0.5f, sitio.z), new Vector3(1.4f, (sitio.y - Encimera) * 0.5f, 1.4f), azulOscuro);
            var grupo = M.Grupo(g, "Gradas");
            grupo.gameObject.AddComponent<PublicoAnimado>();
            var mirar = new Vector3(0f, sitio.y, sitio.z);
            var grada = KitObjetos.Poner("Grada", grupo, sitio, KitObjetos.GiroMirando(sitio, mirar), null, 1f, false, KitObjetos.KitPlato);
            Color[] colores =
            {
                new Color(0.92f, 0.2f, 0.18f), new Color(0.45f, 0.85f, 0.35f), new Color(0.3f, 0.3f, 0.36f),
                new Color(1f, 0.8f, 0.3f), new Color(0.4f, 0.65f, 1f), new Color(0.6f, 0.95f, 0.45f),
            };
            var materiales = new Material[colores.Length];
            for (int i = 0; i < colores.Length; i++) materiales[i] = E.CrearMaterial($"PublicoCocina{i}", colores[i], 0.35f);
            var azar = new System.Random(Mathf.RoundToInt(sitio.x * 31f + sitio.z * 17f));
            for (int fila = 0; fila < 4; fila++)
                for (int s = 0; s < 5; s++)
                {
                    if (azar.NextDouble() < 0.12) continue;
                    float px = -3.2f + 1.6f * s + (float)(azar.NextDouble() - 0.5) * 0.5f;
                    KitObjetos.Poner(azar.NextDouble() < 0.35 ? "PublicoBrazos" : "Publico", grada.transform,
                        new Vector3(px, 0.5f * (fila + 1) + 0.1f, 1.35f - 1.1f * fila), (float)(azar.NextDouble() - 0.5) * 30f,
                        materiales[azar.Next(materiales.Length)], 1f + (float)(azar.NextDouble() - 0.5) * 0.2f, true, KitObjetos.KitPlato);
                }
        }

        static void GruaCamara(Vector3 pie)
        {
            const float escala = 1.6f;
            var go = new GameObject("Grua de camara");
            go.transform.SetParent(decorado, false);
            go.transform.SetPositionAndRotation(pie, Quaternion.Euler(0f, KitObjetos.GiroMirando(pie, new Vector3(0f, 0f, pie.z)), 0f));
            Kit("GruaBase", go.transform, Vector3.zero, 0f, null, escala, local: true);
            var brazo = new GameObject("Brazo").transform;
            brazo.SetParent(go.transform, false);
            brazo.localPosition = new Vector3(0f, 4.55f * escala, 0f);
            Kit("GruaBrazo", brazo, Vector3.zero, 0f, null, escala, local: true);
            go.AddComponent<GruaCamara>().brazo = brazo;
        }

        static void Pancarta(Vector3 pie, string texto)
        {
            var g = M.Grupo(decorado, $"Pancarta {texto}");
            M.Decorado(PrimitiveType.Cylinder, "Palo", g, pie + Vector3.up * 1.5f, new Vector3(0.15f, 1.5f, 0.15f), metal);
            M.Decorado(PrimitiveType.Cube, "Cartel", g, pie + Vector3.up * 3.2f, new Vector3(3.2f, 1.3f, 0.2f), azulOscuro);
            M.Cartel(g, texto, pie + new Vector3(0f, 3.2f, -0.15f), 0.6f, Color.white);
        }

        /// <summary>Guirnalda de banderitas (las del kit de costura) entre dos palos, con su caída.</summary>
        static void Guirnalda(Vector3 a, Vector3 b, float caida = 0.5f)
        {
            var g = M.Grupo(decorado, "Guirnalda");
            KitObjetos.Poner("Banderin", g, a, 0f, rosa, 1.4f, false, KitObjetos.KitCostura);
            KitObjetos.Poner("Banderin", g, b, 0f, rosa, 1.4f, false, KitObjetos.KitCostura);
            Vector3 alto = Vector3.up * 1.82f * 1.4f;
            a += alto;
            b += alto;
            Vector3 dir = b - a;
            float giro = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - 90f;
            Material[] colores = { rosa, amarillo, cian, naranja };
            int n = Mathf.Max(3, Mathf.RoundToInt(dir.magnitude / 1f));
            Vector3 anterior = a;
            for (int i = 1; i <= n; i++)
            {
                float k = i / (float)n;
                Vector3 p = Vector3.Lerp(a, b, k) + Vector3.down * caida * 4f * k * (1f - k);
                var hilo = M.Decorado(PrimitiveType.Cylinder, "Hilo", g, (anterior + p) * 0.5f, new Vector3(0.04f, Vector3.Distance(anterior, p) * 0.5f, 0.04f), blanco);
                if (hilo != null) hilo.transform.rotation = Quaternion.FromToRotation(Vector3.up, p - anterior);
                if (i < n) KitObjetos.Poner("Banderita", g, p, giro, colores[i % colores.Length], 1.2f, false, KitObjetos.KitCostura);
                anterior = p;
            }
        }

        // =====================================================================
        // Suelos, arcos y puntos de control
        // =====================================================================

        /// <summary>Losa de la carrera sobre su pedestal acolchado (hasta la encimera).</summary>
        static GameObject Losa(string nombre, float x0, float x1, float z0, float z1, float arriba, Material material)
        {
            const float grosor = 0.5f;
            var go = E.Bloque(nombre, suelos, new Vector3((x0 + x1) * 0.5f, arriba - grosor * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, grosor, z1 - z0), material);
            float alto = arriba - grosor - Encimera;
            if (alto > 0.2f)
                M.Decorado(PrimitiveType.Cube, $"{nombre} (pedestal)", decorado, new Vector3((x0 + x1) * 0.5f, Encimera + alto * 0.5f, (z0 + z1) * 0.5f),
                           new Vector3(x1 - x0 - 0.6f, alto, z1 - z0 - 0.6f), azulOscuro);
            return go;
        }

        static void Rampa(string nombre, float x, float ancho, float z0, float y0, float z1, float y1, Material material)
        {
            float dz = z1 - z0, dy = y1 - y0;
            float largo = Mathf.Sqrt(dz * dz + dy * dy);
            float angulo = Mathf.Atan2(dy, dz) * Mathf.Rad2Deg;
            var rampa = E.Bloque(nombre, suelos, new Vector3(x, (y0 + y1) * 0.5f - 0.25f, (z0 + z1) * 0.5f), new Vector3(ancho, 0.5f, largo + 0.3f), material);
            rampa.transform.rotation = Quaternion.Euler(-angulo, 0f, 0f);
            // Pedestal debajo (lo que se ve desde los lados).
            float bajo = Mathf.Min(y0, y1);
            M.Decorado(PrimitiveType.Cube, $"{nombre} (pedestal)", decorado, new Vector3(x, (bajo - 0.5f + Encimera) * 0.5f, (z0 + z1) * 0.5f),
                       new Vector3(ancho - 0.6f, bajo - 0.5f - Encimera, dz - 0.6f), azulOscuro);
        }

        /// <summary>Arco luminoso (cartel arriba). Con "salida", lleva el nombre del show; si no, es un punto de control.</summary>
        static void Arco(string texto, float z, float arriba, float escala, Material color, bool salida = false)
        {
            var g = M.Grupo(decorado, $"Arco {texto}");
            Kit("ArcoControl", g, new Vector3(0f, arriba, z), 180f, color, escala);
            M.Cartel(g, texto, new Vector3(0f, arriba + 7.3f * escala, z - 0.45f * escala), salida ? 1.1f : 0.9f, salida ? EstiloShow.Morado : EstiloShow.MoradoOscuro);
            foreach (float s in new[] { -1f, 1f })
            {
                var poste = new GameObject("Poste (colision)");
                poste.transform.SetParent(suelos, false);
                poste.transform.position = new Vector3(s * 9.2f * escala, arriba + 3.25f * escala, z);
                poste.AddComponent<BoxCollider>().size = new Vector3(0.8f, 6.5f, 0.8f) * escala;
            }
        }

        static PuntoControlCarrera PuntoControl(int indice, float z, float x0, float x1, float arriba, string texto, Material color)
        {
            Arco(texto, z, arriba, (x1 - x0) / 18.4f, color);
            var punto = M.ZonaTrigger<PuntoControlCarrera>($"PuntoControl {indice}", mapa, new Vector3((x0 + x1) * 0.5f, arriba + 1.5f, z), new Vector3(x1 - x0, 3f, 1.5f));
            punto.indice = indice;
            punto.separacion = 1.8f;
            var reaparicion = new GameObject("Reaparicion").transform;
            reaparicion.SetParent(punto.transform, false);
            reaparicion.position = new Vector3(0f, arriba + 0.2f, z + 1.5f);
            punto.reaparicion = reaparicion;
            return punto;
        }

        // =====================================================================
        // Ruta de los bots
        // =====================================================================

        static PuntoRuta RutaBots(RodilloAmasar[] rodillos, EsponjaFregadero[] esponjas, PlataformaGiratoria[] tapones,
                                  ColchonElastico[] colchones, TostadaSaltarina[][] tostadas)
        {
            // Zona 1: bajar y saltar los rodillos por el mango (es lo más bajo), esperando a que no vengan de frente.
            var salida = Ruta("Salida", new Vector3(0f, 1.5f, 6f), 2f);
            var bajada = Ruta("Bajada", new Vector3(0f, 0f, 19f), 2f);
            Unir(salida, bajada);
            PuntoRuta previo = bajada;
            for (int i = 0; i < rodillos.Length; i++)
            {
                var r = rodillos[i];
                var antes = Ruta($"Antes del rodillo {i + 1}", new Vector3(6.6f, 0f, r.zDesde - 1.5f), 0.4f, esperar: r);
                var salto = PuntoEn(r.transform, $"Mango del rodillo {i + 1} (delante)", new Vector3(6.6f, -0.9f, -2.3f), saltar: true);
                var tras = PuntoEn(r.transform, $"Mango del rodillo {i + 1} (detras)", new Vector3(6.6f, -0.9f, 2.6f));
                Unir(previo, antes); Unir(antes, salto); Unir(salto, tras);
                previo = tras;
            }
            var finMantel = Ruta("Fin del mantel", new Vector3(0f, 0f, 89f), 2f);
            Unir(previo, finMantel);

            // Zona 2: por las esponjas (rápido) o por los tapones de la derecha (seguro).
            var borde = Ruta("Borde del fregadero", new Vector3(0f, 0f, 93.5f), 1.5f);
            Unir(finMantel, borde);
            var salidaFregadero = Ruta("Salida del fregadero", new Vector3(0f, 0f, 150f), 1.5f);
            var e0 = Ruta("Saltar a la esponja 1", new Vector3(0f, 0f, 94.6f), 0.5f, saltar: true, esperar: esponjas[0]);
            PuntoRuta anterior = e0;
            for (int i = 0; i < esponjas.Length; i++)
            {
                float fin = esponjas[i].transform.position.z + LargoEsponja * 0.5f - 0.6f;
                var p = Ruta($"Final de la esponja {i + 1}", new Vector3(0f, -0.3f, fin), 0.8f, saltar: true,
                             esperar: i + 1 < esponjas.Length ? esponjas[i + 1] : null);
                Unir(anterior, p);
                anterior = p;
            }
            Unir(anterior, salidaFregadero);
            var t0 = Ruta("Saltar a los tapones", new Vector3(6.8f, 0f, 94.6f), 0.3f, saltar: true);
            anterior = t0;
            foreach (var t in tapones)
            {
                if (t.transform.position.x < 0f) continue;
                var p = Ruta($"Tapon z{t.transform.position.z:0}", t.transform.position, 0.2f, saltar: true);
                Unir(anterior, p);
                anterior = p;
            }
            Unir(anterior, salidaFregadero);
            Unir(borde, e0, t0);

            // Zona 3: subir la mantequilla corriendo, sin saltar, esquivando los muros.
            var pie = Ruta("Pie de la rampa", new Vector3(0f, 0f, ZRampa0 - 1f), 1f);
            Unir(salidaFregadero, pie);
            anterior = pie;
            foreach (var (x, d) in new[] { (0f, 5f), (0f, 17f), (0f, 24f), (1f, 31f), (1.5f, 38f), (0f, 45f), (0f, 52f), (1f, 62f), (0f, 68f) })
            {
                float z = ZRampa0 + d * (ZRampa1 - ZRampa0) / LargoRampa;
                var p = Ruta($"Rampa {d:0}", new Vector3(x, (z - ZRampa0) / (ZRampa1 - ZRampa0) * AltoRampa, z), 1f);
                Unir(anterior, p);
                anterior = p;
            }

            // Zona 4: saltar al colchón y dejarse lanzar de fila en fila (los de la derecha).
            float y = AltoRampa;
            var arriba = Ruta("Arriba de la rampa", new Vector3(3.5f, y, 231f), 1f);
            var borde4 = Ruta("Borde de la bandeja", new Vector3(3.5f, y, 236.2f), 0.4f, saltar: true);
            Unir(anterior, arriba); Unir(arriba, borde4);
            anterior = borde4;
            for (int i = 1; i < colchones.Length; i += 2)
            {
                var p = PuntoEn(colchones[i].transform, $"Colchon {i / 2 + 1}", new Vector3(0f, 0.8f, 0f));
                Unir(anterior, p);
                anterior = p;
            }

            // Zona 5: esperar a que salte la tostada de la derecha, cruzar por encima y seguir.
            var tras4 = Ruta("Antes de la tostadora", new Vector3(4.45f, y, 285f), 1f);
            Unir(anterior, tras4);
            anterior = tras4;
            for (int i = 0; i < Ranuras.Length; i++)
            {
                var antes = Ruta($"Antes de la ranura {i + 1}", new Vector3(4.45f, y, Ranuras[i].x - 0.8f), 0.3f, saltar: true, esperar: tostadas[i][1]);
                var encima = Ruta($"Tostada {i + 1}", new Vector3(4.45f, y, (Ranuras[i].x + Ranuras[i].y) * 0.5f), 0.3f, saltar: true);
                var despues = Ruta($"Despues de la ranura {i + 1}", new Vector3(4.45f, y, Ranuras[i].y + 1f), 0.6f);
                Unir(anterior, antes); Unir(antes, encima); Unir(encima, despues);
                anterior = despues;
            }
            var meta = Ruta("Meta", new Vector3(0f, y, ZMeta + 3f), 2f);
            Unir(anterior, meta);
            return salida;
        }

        static PuntoRuta Ruta(string nombre, Vector3 posicion, float anchura, bool saltar = false, MonoBehaviour esperar = null)
        {
            var go = new GameObject($"Ruta {ruta.Count + 1}: {nombre}");
            go.transform.SetParent(rutaBots, false);
            go.transform.position = posicion;
            var p = go.AddComponent<PuntoRuta>();
            p.anchura = anchura;
            p.saltar = saltar;
            p.esperarA = esperar;
            ruta.Add(p);
            return p;
        }

        /// <summary>Punto de ruta pegado a algo que se mueve (va con él).</summary>
        static PuntoRuta PuntoEn(Transform padre, string nombre, Vector3 local, bool saltar = false)
        {
            var go = new GameObject($"Ruta {ruta.Count + 1}: {nombre}");
            go.transform.SetParent(padre, false);
            go.transform.localPosition = local;
            var p = go.AddComponent<PuntoRuta>();
            p.anchura = 0.2f;
            p.saltar = saltar;
            ruta.Add(p);
            return p;
        }

        static void Unir(PuntoRuta desde, params PuntoRuta[] siguientes) => desde.siguientes = siguientes;

        // =====================================================================
        // Utilidades
        // =====================================================================

        static GameObject Kit(string pieza, Transform padre, Vector3 posicion, float giro = 0f, Material equipo = null,
                              float escala = 1f, bool local = false) =>
            KitObjetos.Poner(pieza, padre, posicion, giro, equipo, escala, local, KitObjetos.KitDespensa);

        static GameObject SoloColision(GameObject go)
        {
            if (go != null && go.TryGetComponent(out MeshRenderer r)) Object.DestroyImmediate(r);
            return go;
        }
    }
}
