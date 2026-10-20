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
    /// Menú "BichoShow > Crear escena: Costura a la fuga". Versión de prueba (bloques y colores
    /// planos) de la carrera sobre la mesa de costura, en cinco zonas a lo largo del eje Z:
    ///  1. Salida: la Tabla de Corte (z 0-23): arco de salida, un hueco pequeño y un escalón.
    ///  2. Los Carretes Viajeros (z 23-44): tres franjas con un carrete rodando de lado a lado.
    ///  3. El Puente Acordeón (z 44-79): paneles que se inclinan en ola, o el camino largo del lado.
    ///  4. Las Cremalleras Gemelas (z 79-108): ancha y lenta (izquierda) o estrecha y rápida (derecha).
    ///  5. La Subida del Dedal (z 108-147): tres alfileteros en terrazas (el del medio se hunde) y la meta.
    ///
    /// Medidas pensadas para el bicho: corre a 6 m/s, salta ~2,1 m de alto y ~6,5 m de largo
    /// corriendo. Saltos: huecos de 1,5-2,5 m y escalones de hasta 1,2 m.
    /// </summary>
    public static class CreadorCostura
    {
        const string RutaEscena = E.CarpetaEscenas + "/Costura.unity";
        /// <summary>Altura de la mesa (lo de abajo, decorado). La carrera va por encima, sobre patas.</summary>
        const float AlturaMesa = -9f;

        static Transform mapa, suelos, obstaculos, decorado, rutaBots;
        static readonly List<PuntoRuta> ruta = new List<PuntoRuta>();
        static Material turquesa, turquesaClaro, mostaza, coral, azul, madera, mesa, crema, dorado, rejilla, dedal;
        static Material fondoCoral, fondoAzul, fondoMostaza, fondoTurquesa, fondoPared, fondoGris;

        [MenuItem("BichoShow/Crear escena: Costura a la fuga")]
        public static void Crear()
        {
            var bicho = A.Preparar(RutaEscena, "Costura a la fuga", out _);
            if (bicho == null) return;
            CrearEscena(bicho);
            Debug.Log("Bicho Show: escena de Costura a la fuga creada (versión de prueba con bloques). Ya entra en el show.");
        }

        static void CrearMateriales()
        {
            turquesa = E.CrearMaterial("CosturaTurquesa", new Color(0.13f, 0.6f, 0.6f), 0.25f);
            turquesaClaro = E.CrearMaterial("CosturaTurquesaClaro", new Color(0.36f, 0.76f, 0.72f), 0.25f);
            mostaza = E.CrearMaterial("CosturaMostaza", new Color(0.93f, 0.68f, 0.2f), 0.25f);
            coral = E.CrearMaterial("CosturaCoral", new Color(0.95f, 0.45f, 0.38f), 0.25f);
            azul = E.CrearMaterial("CosturaAzul", new Color(0.14f, 0.2f, 0.42f), 0.3f);
            madera = E.CrearMaterial("CosturaMadera", new Color(0.88f, 0.72f, 0.52f), 0.3f);
            mesa = E.CrearMaterial("CosturaMesa", new Color(0.55f, 0.38f, 0.24f), 0.35f);
            crema = E.CrearMaterial("CosturaCrema", new Color(0.98f, 0.94f, 0.84f), 0.2f);
            dorado = E.CrearMaterial("CosturaDorado", new Color(1f, 0.78f, 0.3f), 0.7f);
            rejilla = E.CrearMaterial("CosturaRejilla", new Color(0.08f, 0.44f, 0.44f), 0.2f);
            dedal = E.CrearMaterial("CosturaDedal", new Color(0.8f, 0.8f, 0.84f), 0.85f);
            // El fondo, apagado: que no compita con el recorrido.
            fondoCoral = E.CrearMaterial("CosturaFondoCoral", new Color(0.7f, 0.48f, 0.44f), 0.2f);
            fondoAzul = E.CrearMaterial("CosturaFondoAzul", new Color(0.42f, 0.48f, 0.58f), 0.2f);
            fondoMostaza = E.CrearMaterial("CosturaFondoMostaza", new Color(0.74f, 0.62f, 0.4f), 0.2f);
            fondoTurquesa = E.CrearMaterial("CosturaFondoTurquesa", new Color(0.4f, 0.58f, 0.56f), 0.2f);
            fondoPared = E.CrearMaterial("CosturaFondoPared", new Color(0.82f, 0.74f, 0.62f), 0.1f);
            fondoGris = E.CrearMaterial("CosturaFondoGris", new Color(0.6f, 0.6f, 0.62f), 0.6f);
        }

        static void CrearEscena(GameObject prefabBicho)
        {
            CrearMateriales();
            ruta.Clear();
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camara = A.Camara(14f, 34f);
            camara.centroSinJugadores = new Vector3(0f, 0f, 10f);
            camara.ColocarAlInstante();

            mapa = new GameObject("Mapa").transform;
            suelos = M.Grupo(mapa, "Suelos");
            obstaculos = M.Grupo(mapa, "Obstaculos");
            decorado = M.Grupo(mapa, "Decorado");
            rutaBots = M.Grupo(mapa, "RutaBots");
            var controles = new List<PuntoControlCarrera>();

            // =================================================================
            // 1. SALIDA: la Tabla de Corte
            // =================================================================
            Losa("TablaCorte", -9f, 9f, 0f, 14f, 0f, turquesa);
            Cuadricula(-9f, 9f, 0f, 14f, 0f);
            Arco("SALIDA", 4f, 0f, 18f);
            Flecha(new Vector3(0f, 0f, 10.5f), 0f, crema);
            // Primer saltito (1,5 m) a un escalón (+0,6 m): para aprender a saltar.
            Losa("Escalon", -6f, 6f, 15.5f, 23f, 0.6f, turquesaClaro);
            Pespunte(-6f, 6f, 15.5f, 23f, 0.6f);
            Flecha(new Vector3(0f, 0.6f, 20.5f), 0f, azul);
            controles.Add(PuntoControl(1, 18f, -6f, 6f, 0.6f));

            // =================================================================
            // 2. LOS CARRETES VIAJEROS
            // =================================================================
            Losa("SueloCarretes", -6f, 6f, 23f, 44f, 0.6f, mostaza);
            float[] franjas = { 26f, 32.5f, 39f };                 // centro de cada franja (4 m de fondo)
            float[] periodos = { 5.5f, 4.5f, 3.4f };               // de más lento a más rápido
            float[] desfases = { 0f, 0.35f, 0.7f };
            Material[] hilos = { coral, azul, turquesa };
            var carretes = new CarreteViajero[3];
            for (int i = 0; i < 3; i++)
            {
                // La guía del carrete en el suelo (su recorrido) y los topes de los lados.
                Pegatina($"Guia carrete {i + 1}", -6f, 6f, franjas[i] - 1.7f, franjas[i] + 1.7f, 0.6f, azul);
                Pegatina($"Guia carrete {i + 1} centro", -5.6f, 5.6f, franjas[i] - 0.08f, franjas[i] + 0.08f, 0.615f, crema);
                carretes[i] = Carrete($"Carrete {i + 1}", franjas[i], periodos[i], desfases[i], hilos[i]);
            }
            // Zonas seguras entre franjas, con un pespunte que las marca.
            Pespunte(-6f, 6f, 28f, 30.5f, 0.6f);
            Pespunte(-6f, 6f, 34.5f, 37f, 0.6f);

            // Descanso y bifurcación
            Losa("Descanso1", -10f, 13f, 44f, 51f, 0.6f, turquesa);
            Pespunte(-10f, 13f, 44f, 51f, 0.6f);
            controles.Add(PuntoControl(2, 46.5f, -10f, 13f, 0.6f));
            Flecha(new Vector3(-3f, 0.6f, 49.3f), 0f, crema);
            Flecha(new Vector3(7.5f, 0.6f, 48.8f), 25f, crema);

            // =================================================================
            // 3. EL PUENTE ACORDEÓN (y el camino largo del lado)
            // =================================================================
            // Seis paneles seguidos (juntas de 15 cm). Cada uno se inclina 0,525 s después del
            // anterior: lo que tarda un bicho en cruzar uno. Quien entra cuando el primero se
            // pone plano, cruza todo en "ola verde"; si no, hay que saltar los inclinados.
            var paneles = new PanelAcordeon[6];
            for (int i = 0; i < 6; i++)
            {
                float z0 = 51.15f + 3.15f * i;
                paneles[i] = Panel(i, -3f, z0, z0 + 3f, 0.6f, i % 2 == 0 ? 1f : -1f, 0.525f * i, i % 2 == 0 ? coral : azul);
            }
            paneles[0].ventanaBot = 1.0f;
            Bisagras(-3f, 51.15f, 6, 3.15f, 0.6f);

            // Camino del lado: más largo (sale a x=13,5 y vuelve), plano y con barandilla de hilo.
            var lado = new[]
            {
                (6.5f, 9.5f, 51f, 58.5f), (6.5f, 15f, 58.5f, 61.5f), (12f, 15f, 61.5f, 68.5f),
                (6.5f, 15f, 68.5f, 71.5f), (6.5f, 9.5f, 71.5f, 73f),
            };
            for (int i = 0; i < lado.Length; i++)
                Losa($"Camino lateral {i + 1}", lado[i].Item1, lado[i].Item2, lado[i].Item3, lado[i].Item4, 0.6f, mostaza);
            Barandillas(0.6f,
                (6.5f, 51f, 6.5f, 58.5f), (9.5f, 51f, 9.5f, 58.5f),
                (6.5f, 58.5f, 6.5f, 61.5f), (6.5f, 61.5f, 12f, 61.5f), (9.5f, 58.5f, 15f, 58.5f), (15f, 58.5f, 15f, 71.5f),
                (12f, 61.5f, 12f, 68.5f), (6.5f, 68.5f, 12f, 68.5f), (9.5f, 71.5f, 15f, 71.5f),
                (6.5f, 68.5f, 6.5f, 73f), (9.5f, 71.5f, 9.5f, 73f));
            Flecha(new Vector3(8f, 0.6f, 55f), 0f, azul);
            Flecha(new Vector3(11f, 0.6f, 60f), 90f, azul);
            Flecha(new Vector3(13.5f, 0.6f, 65f), 0f, azul);
            Flecha(new Vector3(11f, 0.6f, 70f), -90f, azul);

            // Descanso 2: se juntan las dos rutas.
            Losa("Descanso2", -10f, 16f, 73f, 79f, 0.6f, turquesa);
            Pespunte(-10f, 16f, 73f, 79f, 0.6f);
            controles.Add(PuntoControl(3, 75.5f, -10f, 16f, 0.6f));
            Flecha(new Vector3(-4.5f, 0.6f, 77.5f), 0f, crema);
            Flecha(new Vector3(4.5f, 0.6f, 77.5f), 0f, crema);

            // =================================================================
            // 4. LAS CREMALLERAS GEMELAS
            // =================================================================
            // Izquierda: ancha (4,4 m) y lenta, en dos mitades con un descansillo entre medias.
            var izq1 = Cremallera("Cremallera ancha 1", -4.5f, 4.4f, 79f, 5, 2f, 0.6f, 2.5f, 2.5f, 1f, 0f, coral);
            Losa("Descansillo", -6.7f, -2.3f, 89f, 91.5f, 0.6f, mostaza);
            var izq2 = Cremallera("Cremallera ancha 2", -4.5f, 4.4f, 91.5f, 5, 2f, 0.6f, 2.5f, 2.5f, 1f, 4.5f, coral);
            // Derecha: estrecha (2,4 m) y de una tirada, con el cursor más rápido.
            var der = Cremallera("Cremallera estrecha", 4.5f, 2.4f, 79f, 9, 2.5f, 0.6f, 4.6f, 1.4f, 0.8f, 0f, azul);

            Losa("Destino", -9f, 9f, 101.5f, 108f, 0.6f, turquesa);
            Pespunte(-9f, 9f, 101.5f, 108f, 0.6f);
            controles.Add(PuntoControl(4, 104.5f, -9f, 9f, 0.6f));
            Flecha(new Vector3(0f, 0.6f, 106.5f), 0f, crema);

            // =================================================================
            // 5. LA SUBIDA DEL DEDAL
            // =================================================================
            Rampa("Rampa alfiletero", 0f, 6f, 108f, 0.6f, 111f, 1.6f, crema);
            Cojin("Alfiletero 1", -6f, 6f, 111f, 118f, 1.6f, mostaza, false);
            controles.Add(PuntoControl(5, 113.5f, -6f, 6f, 1.6f));
            Flecha(new Vector3(0f, 1.6f, 116.3f), 0f, azul);
            // Hueco de 2 m y +1 m al segundo: este se hunde si te quedas encima.
            var blando = Cojin("Alfiletero 2 (blando)", -6f, 6f, 120f, 127f, 2.6f, coral, true);
            // Al lado del blando, un escalón fijo y una rampa al tercero (por si está hundido).
            Losa("Escalon lateral", 6f, 8.5f, 123f, 126f, 2.6f, madera);
            Rampa("Rampa lateral", 7.25f, 2.5f, 126f, 2.6f, 129.5f, 3.8f, crema);
            Cojin("Alfiletero 3", -6f, 8.5f, 129.5f, 136f, 3.8f, turquesaClaro, false);
            // La cinta métrica hace de puente hasta la meta.
            Losa("Cinta metrica", -3f, 3f, 136f, 138f, 3.8f, mostaza, 0.25f, false);
            for (int i = 0; i < 9; i++)
                Pegatina("Marca cinta", -3f, -2.2f + (i % 2) * 0.3f, 136.1f + i * 0.22f, 136.14f + i * 0.22f, 3.81f, azul);
            Losa("Meta", -9f, 9f, 138f, 147f, 3.8f, turquesa);
            Cuadricula(-9f, 9f, 138f, 147f, 3.8f);
            Arco("META", 142f, 3.8f, 18f);
            var meta = M.ZonaTrigger<MetaCarrera>("Meta", mapa, new Vector3(0f, 3.8f + 1.5f, 144.5f), new Vector3(18f, 3f, 5f));
            Dedal(new Vector3(12f, 3.8f, 144f));

            // =================================================================
            // Ruta de los bots
            // =================================================================
            var w0 = Ruta("Salida", 0f, 0f, 9f, 3f);
            var w1 = Ruta("Salto escalon", 0f, 0f, 13.6f, 3f, saltar: true);
            var c0 = Ruta("Espera carrete 1", 0f, 0.6f, 23.4f, 2.5f, esperar: carretes[0]);
            var c1 = Ruta("Espera carrete 2", 0f, 0.6f, 29.9f, 2.5f, esperar: carretes[1]);
            var c2 = Ruta("Espera carrete 3", 0f, 0.6f, 36.4f, 2.5f, esperar: carretes[2]);
            var c3 = Ruta("Fin carretes", 0f, 0.6f, 43f, 2.5f);
            var f = Ruta("Bifurcacion acordeon", 0f, 0.6f, 47.5f, 2f);
            var a0 = Ruta("Entrada acordeon", -3f, 0.6f, 50.6f, 0.8f, esperar: paneles[0]);
            var a1 = Ruta("Salida acordeon", -3f, 0.6f, 70.6f, 0.8f);
            var b0 = Ruta("Lateral 1", 8f, 0.6f, 51.5f, 0.6f);
            var b1 = Ruta("Lateral 2", 8f, 0.6f, 60f, 0.6f);
            var b2 = Ruta("Lateral 3", 13.5f, 0.6f, 60f, 0.6f);
            var b3 = Ruta("Lateral 4", 13.5f, 0.6f, 70f, 0.6f);
            var b4 = Ruta("Lateral 5", 8f, 0.6f, 70f, 0.6f);
            var b5 = Ruta("Lateral 6", 8f, 0.6f, 74f, 0.6f);
            var g = Ruta("Bifurcacion cremalleras", 0f, 0.6f, 76.3f, 2f);
            var rc0 = Ruta("Entrada cremallera estrecha", 4.5f, 0.6f, 78.6f, 0.35f, esperar: der);
            var rc1 = Ruta("Salida cremallera estrecha", 4.5f, 0.6f, 102.8f, 0.35f);
            var lc0 = Ruta("Entrada cremallera ancha", -4.5f, 0.6f, 78.6f, 1.2f, esperar: izq1);
            var lc1 = Ruta("Descansillo cremalleras", -4.5f, 0.6f, 90.3f, 1.2f, esperar: izq2);
            var lc2 = Ruta("Salida cremallera ancha", -4.5f, 0.6f, 102.8f, 1.2f);
            var d = Ruta("Destino", 0f, 0.6f, 104.5f, 2f);
            var e1 = Ruta("Pie de la rampa", 0f, 0.6f, 107.6f, 1.5f);
            var e2 = Ruta("Alfiletero 1", 0f, 1.6f, 112f, 2f);
            var e3 = Ruta("Salto al blando", 0f, 1.6f, 117.6f, 2f, saltar: true);
            var e4 = Ruta("Salto al lateral", 5.3f, 2.6f, 124f, 0.3f, saltar: true);
            var e5 = Ruta("Alfiletero 3", 7.25f, 3.8f, 131f, 0.5f);
            var e6 = Ruta("Cinta metrica", 0f, 3.8f, 137f, 1.2f);
            var e7 = Ruta("Meta", 0f, 3.8f, 144.5f, 2.5f);
            Unir(w0, w1); Unir(w1, c0); Unir(c0, c1); Unir(c1, c2); Unir(c2, c3); Unir(c3, f);
            Unir(f, a0, b0);                                      // [difícil, fácil]
            Unir(a0, a1); Unir(a1, g);
            Unir(b0, b1); Unir(b1, b2); Unir(b2, b3); Unir(b3, b4); Unir(b4, b5); Unir(b5, g);
            Unir(g, rc0, lc0);                                    // estrecha (difícil) o ancha (fácil)
            Unir(rc0, rc1); Unir(rc1, d);
            Unir(lc0, lc1); Unir(lc1, lc2); Unir(lc2, d);
            Unir(d, e1); Unir(e1, e2); Unir(e2, e3); Unir(e3, e4); Unir(e4, e5); Unir(e5, e6); Unir(e6, e7);

            // =================================================================
            // Caída, decorado, jugadores y ronda
            // =================================================================
            var vacio = M.ZonaTrigger<ZonaMortal>("ZonaCaida", mapa, new Vector3(2f, -5f, 75f), new Vector3(140f, 3f, 260f));
            vacio.motivoObjeto = "se ha caído de la mesa";
            Fondo();

            var apariciones = new[] { new Vector3(-4.5f, 0f, 2.5f), new Vector3(-1.5f, 0f, 2.5f), new Vector3(1.5f, 0f, 2.5f), new Vector3(4.5f, 0f, 2.5f) };
            var gestor = A.Jugadores(prefabBicho, apariciones, new Vector3(0f, 0f, 30f));

            var rondaGo = new GameObject("RondaCostura");
            var ronda = rondaGo.AddComponent<RondaCostura>();
            ronda.gestor = gestor;
            ronda.saboteadorMaquina = false;
            ronda.puntosControl = controles.ToArray();
            ronda.meta = meta;
            ronda.ruta = ruta.ToArray();
            ronda.inicioRuta = w0;
            ronda.zSalida = 2.5f;
            ronda.zMeta = 142f;

            Ambientes.Aplicar(Ambientes.Tema.Taller);
            EditorSceneManager.SaveScene(escena, RutaEscena);
            E.AnadirAlBuild(RutaEscena);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rondaGo;
        }

        // ---------------------------------------------------------------------
        // Suelos
        // ---------------------------------------------------------------------

        /// <summary>Losa transitable (con colisión) entre x0-x1 y z0-z1, con la cara de arriba a "arriba". Con patas hasta la mesa.</summary>
        static GameObject Losa(string nombre, float x0, float x1, float z0, float z1, float arriba, Material material,
                               float grosor = 0.6f, bool patas = true)
        {
            var go = E.Bloque(nombre, suelos, new Vector3((x0 + x1) * 0.5f, arriba - grosor * 0.5f, (z0 + z1) * 0.5f),
                              new Vector3(x1 - x0, grosor, z1 - z0), material);
            if (patas) Patas(x0, x1, z0, z1, arriba - grosor);
            return go;
        }

        /// <summary>Patas de madera de una losa hasta la mesa (decorado).</summary>
        static void Patas(float x0, float x1, float z0, float z1, float abajo)
        {
            float alto = abajo - AlturaMesa;
            if (alto <= 0.1f) return;
            float m = Mathf.Min(0.8f, (x1 - x0) * 0.25f), n = Mathf.Min(0.8f, (z1 - z0) * 0.25f);
            foreach (var x in new[] { x0 + m, x1 - m })
                foreach (var z in new[] { z0 + n, z1 - n })
                    M.Decorado(PrimitiveType.Cylinder, "Pata", decorado, new Vector3(x, abajo - alto * 0.5f, z), new Vector3(0.6f, alto * 0.5f, 0.6f), madera);
        }

        /// <summary>Algo pintado en el suelo (sin colisión): guías, marcas.</summary>
        static void Pegatina(string nombre, float x0, float x1, float z0, float z1, float arriba, Material material)
        {
            M.Decorado(PrimitiveType.Cube, nombre, decorado, new Vector3((x0 + x1) * 0.5f, arriba + 0.012f, (z0 + z1) * 0.5f),
                       new Vector3(x1 - x0, 0.02f, z1 - z0), material);
        }

        /// <summary>Pespunte de hilo crema alrededor de una losa (marca lo que se pisa).</summary>
        static void Pespunte(float x0, float x1, float z0, float z1, float arriba)
        {
            const float m = 0.35f, g = 0.1f;
            Pegatina("Pespunte", x0 + m, x1 - m, z0 + m, z0 + m + g, arriba, crema);
            Pegatina("Pespunte", x0 + m, x1 - m, z1 - m - g, z1 - m, arriba, crema);
            Pegatina("Pespunte", x0 + m, x0 + m + g, z0 + m, z1 - m, arriba, crema);
            Pegatina("Pespunte", x1 - m - g, x1 - m, z0 + m, z1 - m, arriba, crema);
        }

        /// <summary>Cuadrícula de la tabla de corte (cada 2 m).</summary>
        static void Cuadricula(float x0, float x1, float z0, float z1, float arriba)
        {
            for (float x = x0 + 2f; x < x1 - 0.5f; x += 2f) Pegatina("Rejilla", x - 0.04f, x + 0.04f, z0 + 0.3f, z1 - 0.3f, arriba, rejilla);
            for (float z = z0 + 2f; z < z1 - 0.5f; z += 2f) Pegatina("Rejilla", x0 + 0.3f, x1 - 0.3f, z - 0.04f, z + 0.04f, arriba, rejilla);
            Pespunte(x0, x1, z0, z1, arriba);
        }

        /// <summary>Rampa recta de (z0, y0) a (z1, y1), centrada en x.</summary>
        static void Rampa(string nombre, float x, float ancho, float z0, float y0, float z1, float y1, Material material)
        {
            float dz = z1 - z0, dy = y1 - y0;
            float largo = Mathf.Sqrt(dz * dz + dy * dy);
            float angulo = Mathf.Atan2(dy, dz) * Mathf.Rad2Deg;
            var rampa = E.Bloque(nombre, suelos, new Vector3(x, (y0 + y1) * 0.5f - 0.15f, (z0 + z1) * 0.5f), new Vector3(ancho, 0.3f, largo + 0.3f), material);
            rampa.transform.rotation = Quaternion.Euler(-angulo, 0f, 0f);
        }

        // ---------------------------------------------------------------------
        // Obstáculos
        // ---------------------------------------------------------------------

        /// <summary>Carrete de hilo tumbado (eje a lo largo de la carrera) que rueda de lado a lado.</summary>
        static CarreteViajero Carrete(string nombre, float z, float periodo, float desfase, Material hilo)
        {
            const float radio = 1.25f;
            var go = new GameObject(nombre);
            go.transform.SetParent(obstaculos, false);
            go.transform.position = new Vector3(0f, 0.6f + radio + 0.05f, z);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // el eje Y del cilindro, a lo largo de Z
            go.AddComponent<Rigidbody>().isKinematic = true;
            var capsula = go.AddComponent<CapsuleCollider>();
            capsula.direction = 1;
            capsula.radius = radio + 0.1f;
            capsula.height = 3.4f;

            E.Pieza(PrimitiveType.Cylinder, "Hilo", go.transform, Vector3.zero, new Vector3(radio * 2f, 1.45f, radio * 2f), hilo);
            foreach (float y in new[] { -1.55f, 1.55f })
                E.Pieza(PrimitiveType.Cylinder, "Tapa", go.transform, new Vector3(0f, y, 0f), new Vector3(radio * 2f + 0.7f, 0.14f, radio * 2f + 0.7f), madera);
            // Una raya a lo largo: así se ve que rueda.
            E.Pieza(PrimitiveType.Cube, "Raya", go.transform, new Vector3(radio - 0.02f, 0f, 0f), new Vector3(0.08f, 2.9f, 0.35f), crema);
            E.Pieza(PrimitiveType.Cube, "Raya", go.transform, new Vector3(-radio + 0.02f, 0f, 0f), new Vector3(0.08f, 2.9f, 0.35f), crema);

            var carrete = go.AddComponent<CarreteViajero>();
            carrete.recorrido = 6f - radio - 0.35f;
            carrete.periodo = periodo;
            carrete.desfase = desfase;
            carrete.radio = radio;
            return carrete;
        }

        /// <summary>Panel del acordeón (4 m de ancho), con su colisión y la tela acolchada encima.</summary>
        static PanelAcordeon Panel(int i, float x, float z0, float z1, float arriba, float sentido, float retraso, Material tela)
        {
            var go = new GameObject($"Panel acordeon {i + 1}");
            go.transform.SetParent(obstaculos, false);
            go.transform.position = new Vector3(x, arriba - 0.2f, (z0 + z1) * 0.5f);
            go.AddComponent<Rigidbody>().isKinematic = true;
            var caja = go.AddComponent<BoxCollider>();
            caja.size = new Vector3(4f, 0.4f, z1 - z0);
            E.Pieza(PrimitiveType.Cube, "Tabla", go.transform, Vector3.zero, new Vector3(4f, 0.4f, z1 - z0), madera);
            E.Pieza(PrimitiveType.Cube, "Tela", go.transform, new Vector3(0f, 0.05f, 0f), new Vector3(3.8f, 0.36f, z1 - z0 - 0.2f), tela);
            // Pespunte en zigzag: dos líneas que marcan hacia dónde se inclinará.
            E.Pieza(PrimitiveType.Cube, "Pespunte", go.transform, new Vector3(sentido * 1.5f, 0.24f, 0f), new Vector3(0.1f, 0.02f, z1 - z0 - 0.5f), crema);
            E.Pieza(PrimitiveType.Cube, "Pespunte", go.transform, new Vector3(-sentido * 1.5f, 0.24f, 0f), new Vector3(0.1f, 0.02f, z1 - z0 - 0.5f), crema);
            var panel = go.AddComponent<PanelAcordeon>();
            panel.sentido = sentido;
            panel.retraso = retraso;
            return panel;
        }

        /// <summary>Bisagras (decorado) en las juntas del acordeón, a los dos lados.</summary>
        static void Bisagras(float x, float z0, int paneles, float paso, float arriba)
        {
            for (int i = 1; i < paneles; i++)
            {
                float z = z0 + paso * i - 0.075f;
                foreach (float lado in new[] { -2.15f, 2.15f })
                {
                    var b = M.Decorado(PrimitiveType.Cylinder, "Bisagra", decorado, new Vector3(x + lado, arriba - 0.25f, z), new Vector3(0.25f, 0.2f, 0.25f), dorado);
                    if (b != null) b.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                }
            }
        }

        /// <summary>
        /// Puente con cremallera: "tramos" tramos de dos mitades (cada una con su bisagra en el
        /// borde de fuera), los dientes en el borde de dentro y el tirador dorado por fuera.
        /// </summary>
        static Cremallera Cremallera(string nombre, float x, float ancho, float z0, int tramos, float largoTramo, float arriba,
                                     float velocidad, float pausaCerrada, float pausaAbierta, float retraso, Material tela)
        {
            var raiz = new GameObject(nombre);
            raiz.transform.SetParent(obstaculos, false);
            raiz.transform.position = new Vector3(x, arriba, z0);
            float largo = tramos * largoTramo;
            var bisagras = new List<Rigidbody>();
            for (int k = 0; k < tramos; k++)
            {
                float zc = (k + 0.5f) * largoTramo;
                foreach (float lado in new[] { -1f, 1f })
                {
                    var pivote = new GameObject($"Tramo {k + 1} {(lado < 0f ? "izquierdo" : "derecho")}");
                    pivote.transform.SetParent(raiz.transform, false);
                    pivote.transform.localPosition = new Vector3(lado * ancho * 0.5f, 0f, zc);
                    var cuerpo = pivote.AddComponent<Rigidbody>();
                    cuerpo.isKinematic = true;
                    bisagras.Add(cuerpo);
                    float mitad = ancho * 0.5f - 0.03f;
                    E.Pieza(PrimitiveType.Cube, "Suelo", pivote.transform, new Vector3(-lado * mitad * 0.5f, -0.2f, 0f),
                            new Vector3(mitad, 0.4f, largoTramo - 0.06f), tela, conCollider: true);
                    // Dientes de la cremallera en el borde de dentro (decorado).
                    for (float dz = -largoTramo * 0.5f + 0.25f; dz < largoTramo * 0.5f - 0.1f; dz += 0.5f)
                        E.Pieza(PrimitiveType.Cube, "Diente", pivote.transform, new Vector3(-lado * (mitad - 0.12f), 0.02f, dz + (lado < 0f ? 0f : 0.25f)),
                                new Vector3(0.28f, 0.1f, 0.22f), dorado);
                }
            }
            // Las cintas de los lados (decorado).
            foreach (float lado in new[] { -1f, 1f })
                E.Pieza(PrimitiveType.Cube, "Cinta", raiz.transform, new Vector3(lado * (ancho * 0.5f + 0.2f), -0.25f, largo * 0.5f),
                        new Vector3(0.4f, 0.3f, largo), azul);
            // El tirador, por el lado de fuera.
            var cursor = new GameObject("Tirador").transform;
            cursor.SetParent(raiz.transform, false);
            float fuera = x < 0f ? -1f : 1f;
            cursor.localPosition = new Vector3(fuera * (ancho * 0.5f + 0.45f), 0.25f, 0f);
            E.Pieza(PrimitiveType.Cube, "Cuerpo", cursor, Vector3.zero, new Vector3(0.6f, 0.55f, 1.1f), dorado);
            E.Pieza(PrimitiveType.Cube, "Lengueta", cursor, new Vector3(fuera * 0.55f, 0.1f, -0.2f), new Vector3(0.7f, 0.15f, 0.55f), dorado);
            // Patas debajo de todo el puente.
            Patas(x - ancho * 0.5f, x + ancho * 0.5f, z0, z0 + largo, arriba - 0.4f);

            var cremallera = raiz.AddComponent<Cremallera>();
            cremallera.bisagras = bisagras.ToArray();
            cremallera.cursor = cursor;
            cremallera.largo = largo;
            cremallera.velocidad = velocidad;
            cremallera.pausaCerrada = pausaCerrada;
            cremallera.pausaAbierta = pausaAbierta;
            cremallera.retraso = retraso;
            return cremallera;
        }

        /// <summary>Alfiletero acolchado (1,2 m de grueso). Si es blando, se hunde con alguien encima.</summary>
        static CojinBlando Cojin(string nombre, float x0, float x1, float z0, float z1, float arriba, Material tela, bool blando)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(blando ? obstaculos : suelos, false);
            go.transform.position = new Vector3((x0 + x1) * 0.5f, arriba - 0.6f, (z0 + z1) * 0.5f);
            float w = x1 - x0, d = z1 - z0;
            E.Pieza(PrimitiveType.Cube, "Cojin", go.transform, Vector3.zero, new Vector3(w, 1.2f, d), tela, conCollider: true);
            // Botones y pespuntes encima (se mueven con el cojín).
            foreach (float bx in new[] { -0.3f, 0.3f })
                foreach (float bz in new[] { -0.3f, 0.3f })
                    E.Pieza(PrimitiveType.Sphere, "Boton", go.transform, new Vector3(bx * w, 0.6f, bz * d), new Vector3(0.6f, 0.15f, 0.6f), dorado);
            E.Pieza(PrimitiveType.Cube, "Pespunte", go.transform, new Vector3(0f, 0.61f, 0f), new Vector3(w - 0.8f, 0.02f, 0.1f), crema);
            E.Pieza(PrimitiveType.Cube, "Pespunte", go.transform, new Vector3(0f, 0.61f, 0f), new Vector3(0.1f, 0.02f, d - 0.8f), crema);
            // Base de madera (fija) y patas.
            M.Decorado(PrimitiveType.Cube, "Base", decorado, new Vector3((x0 + x1) * 0.5f, arriba - 1.6f, (z0 + z1) * 0.5f), new Vector3(w - 0.4f, 0.8f, d - 0.4f), madera);
            Patas(x0, x1, z0, z1, arriba - 2f);
            if (!blando) return null;

            go.AddComponent<Rigidbody>().isKinematic = true;
            var cojin = go.AddComponent<CojinBlando>();
            cojin.tamanoArriba = new Vector2(w - 0.4f, d - 0.4f);
            cojin.alturaArriba = 0.6f;
            cojin.hundimiento = 0.8f;
            cojin.velocidadBajar = 0.8f;   // da tiempo a saltar enseguida
            cojin.velocidadSubir = 0.3f;
            return cojin;
        }

        // ---------------------------------------------------------------------
        // Guías: arcos, flechas, banderines, barandillas, puntos de control
        // ---------------------------------------------------------------------

        static void Arco(string texto, float z, float arriba, float ancho)
        {
            var arco = M.Grupo(decorado, $"Arco {texto}");
            foreach (float lado in new[] { -1f, 1f })
            {
                E.Pieza(PrimitiveType.Cylinder, "Poste", arco, new Vector3(lado * (ancho * 0.5f), arriba + 2.2f, z), new Vector3(0.9f, 2.2f, 0.9f), coral, conCollider: true);
                E.Pieza(PrimitiveType.Sphere, "Boton", arco, new Vector3(lado * (ancho * 0.5f), arriba + 4.6f, z), new Vector3(1.3f, 0.5f, 1.3f), dorado);
            }
            E.Pieza(PrimitiveType.Cube, "Cinta", arco, new Vector3(0f, arriba + 4.7f, z), new Vector3(ancho - 1f, 1.3f, 0.5f), crema);
            E.Pieza(PrimitiveType.Cube, "Borde", arco, new Vector3(0f, arriba + 4.05f, z - 0.05f), new Vector3(ancho - 1f, 0.12f, 0.55f), coral);
            E.Pieza(PrimitiveType.Cube, "Borde", arco, new Vector3(0f, arriba + 5.35f, z - 0.05f), new Vector3(ancho - 1f, 0.12f, 0.55f), coral);
            M.Cartel(arco, texto, new Vector3(0f, arriba + 4.7f, z - 0.3f), 0.95f, new Color(0.14f, 0.2f, 0.42f));
        }

        /// <summary>Flecha cosida en el suelo: un ">" que apunta hacia "giro" (0 = adelante).</summary>
        static void Flecha(Vector3 posicion, float giro, Material material)
        {
            var flecha = M.Grupo(decorado, "Flecha");
            flecha.position = posicion + Vector3.up * 0.015f;
            flecha.rotation = Quaternion.Euler(0f, giro, 0f);
            foreach (float lado in new[] { -1f, 1f })
            {
                var brazo = M.Decorado(PrimitiveType.Cube, "Brazo", flecha, new Vector3(lado * 0.42f, 0f, -0.1f), new Vector3(0.28f, 0.03f, 1.3f), material, local: true);
                if (brazo != null) brazo.transform.localRotation = Quaternion.Euler(0f, -lado * 45f, 0f);
            }
        }

        static void Banderin(Vector3 pie, Material tela)
        {
            var b = M.Grupo(decorado, "Banderin");
            b.position = pie;
            M.Decorado(PrimitiveType.Cylinder, "Palo", b, new Vector3(0f, 0.9f, 0f), new Vector3(0.12f, 0.9f, 0.12f), madera, local: true);
            M.Decorado(PrimitiveType.Cube, "Tela", b, new Vector3(0.38f, 1.55f, 0f), new Vector3(0.7f, 0.45f, 0.05f), tela, local: true);
            M.Decorado(PrimitiveType.Sphere, "Punta", b, new Vector3(0f, 1.85f, 0f), Vector3.one * 0.2f, dorado, local: true);
        }

        /// <summary>Barandillas de hilo con colisión (invisible): el camino fácil no tira a nadie.</summary>
        static void Barandillas(float arriba, params (float x0, float z0, float x1, float z1)[] tramos)
        {
            var grupo = M.Grupo(suelos, "Barandillas");
            foreach (var (x0, z0, x1, z1) in tramos)
            {
                var a = new Vector3(x0, arriba, z0);
                var b = new Vector3(x1, arriba, z1);
                float largo = Vector3.Distance(a, b);
                if (largo < 0.1f) continue;
                Quaternion giro = Quaternion.LookRotation(b - a);
                var pared = new GameObject("Barandilla");
                pared.transform.SetParent(grupo, false);
                pared.transform.SetPositionAndRotation((a + b) * 0.5f + Vector3.up * 0.5f, giro);
                pared.AddComponent<BoxCollider>().size = new Vector3(0.25f, 1f, largo);
                var hilo = M.Decorado(PrimitiveType.Cylinder, "Hilo", grupo, (a + b) * 0.5f + Vector3.up * 0.65f, new Vector3(0.07f, largo * 0.5f, 0.07f), coral);
                if (hilo != null) hilo.transform.rotation = giro * Quaternion.Euler(90f, 0f, 0f);
                int postes = Mathf.Max(1, Mathf.RoundToInt(largo / 2f));
                for (int i = 0; i <= postes; i++)
                    M.Decorado(PrimitiveType.Cylinder, "Poste", grupo, Vector3.Lerp(a, b, i / (float)postes) + Vector3.up * 0.35f, new Vector3(0.14f, 0.35f, 0.14f), madera);
            }
        }

        /// <summary>Punto de control: cruza el ancho de la losa en z, con banderines a los lados.</summary>
        static PuntoControlCarrera PuntoControl(int indice, float z, float x0, float x1, float arriba)
        {
            var punto = M.ZonaTrigger<PuntoControlCarrera>($"PuntoControl {indice}", mapa, new Vector3((x0 + x1) * 0.5f, arriba + 1.5f, z), new Vector3(x1 - x0, 3f, 1.5f));
            punto.indice = indice;
            var reaparicion = new GameObject("Reaparicion").transform;
            reaparicion.SetParent(punto.transform, false);
            reaparicion.position = new Vector3(Mathf.Clamp(0f, x0 + 3f, x1 - 3f), arriba + 0.2f, z);
            punto.reaparicion = reaparicion;
            Banderin(new Vector3(x0 + 0.4f, arriba, z), coral);
            Banderin(new Vector3(x1 - 0.4f, arriba, z), coral);
            Pegatina("Linea de control", x0 + 0.6f, x1 - 0.6f, z - 0.06f, z + 0.06f, arriba, crema);
            return punto;
        }

        // ---------------------------------------------------------------------
        // Ruta de los bots
        // ---------------------------------------------------------------------

        static PuntoRuta Ruta(string nombre, float x, float arriba, float z, float anchura, bool saltar = false, MonoBehaviour esperar = null)
        {
            var go = new GameObject($"Ruta {ruta.Count + 1}: {nombre}");
            go.transform.SetParent(rutaBots, false);
            go.transform.position = new Vector3(x, arriba, z);
            var p = go.AddComponent<PuntoRuta>();
            p.anchura = anchura;
            p.saltar = saltar;
            p.esperarA = esperar;
            ruta.Add(p);
            return p;
        }

        static void Unir(PuntoRuta desde, params PuntoRuta[] siguientes) => desde.siguientes = siguientes;

        // ---------------------------------------------------------------------
        // Decorado: el dedal, la mesa y el taller (apagados, para no distraer)
        // ---------------------------------------------------------------------

        static void Dedal(Vector3 base_)
        {
            var g = M.Grupo(decorado, "Dedal");
            M.Decorado(PrimitiveType.Cylinder, "Dedal", g, base_ + new Vector3(0f, 2.4f, 0f), new Vector3(4.4f, 2.4f, 4.4f), dedal);
            M.Decorado(PrimitiveType.Sphere, "Cupula", g, base_ + new Vector3(0f, 4.8f, 0f), new Vector3(4.4f, 1.8f, 4.4f), dedal);
            for (int fila = 0; fila < 4; fila++)
                for (int i = 0; i < 10; i++)
                {
                    float a = (i + fila * 0.5f) * Mathf.PI * 2f / 10f;
                    M.Decorado(PrimitiveType.Sphere, "Hoyuelo", g, base_ + new Vector3(Mathf.Cos(a) * 2.18f, 1f + fila * 0.9f, Mathf.Sin(a) * 2.18f), Vector3.one * 0.3f, fondoGris);
                }
            M.Decorado(PrimitiveType.Cylinder, "Columna", g, new Vector3(base_.x, (base_.y + AlturaMesa) * 0.5f, base_.z),
                       new Vector3(2.5f, (base_.y - AlturaMesa) * 0.5f, 2.5f), madera);
        }

        static void Fondo()
        {
            var f = M.Grupo(decorado, "Taller");
            // La mesa de costura, abajo.
            M.Decorado(PrimitiveType.Cube, "Mesa", f, new Vector3(0f, AlturaMesa - 0.5f, 75f), new Vector3(130f, 1f, 250f), mesa);
            // Carretes gigantes de pie.
            CarreteFondo(f, new Vector3(-26f, AlturaMesa, 25f), 4f, 8f, fondoCoral);
            CarreteFondo(f, new Vector3(28f, AlturaMesa, 62f), 3.5f, 7f, fondoAzul);
            CarreteFondo(f, new Vector3(-28f, AlturaMesa, 108f), 4.5f, 9f, fondoMostaza);
            CarreteFondo(f, new Vector3(30f, AlturaMesa, 135f), 3f, 6f, fondoTurquesa);
            CarreteFondo(f, new Vector3(-20f, AlturaMesa, 165f), 3.5f, 7.5f, fondoAzul);
            // Botones tumbados.
            BotonFondo(f, new Vector3(22f, AlturaMesa, 18f), 5f, fondoMostaza);
            BotonFondo(f, new Vector3(-21f, AlturaMesa, 72f), 4f, fondoTurquesa);
            BotonFondo(f, new Vector3(25f, AlturaMesa, 102f), 4.5f, fondoCoral);
            // Tijeras abiertas.
            foreach (float giro in new[] { 15f, -15f })
            {
                var hoja = M.Decorado(PrimitiveType.Cube, "Tijera", f, new Vector3(-24f, AlturaMesa + 0.4f, 145f), new Vector3(1.4f, 0.4f, 18f), fondoGris);
                if (hoja != null) hoja.transform.rotation = Quaternion.Euler(0f, giro, 0f);
            }
            // Cinta métrica larga sobre la mesa.
            M.Decorado(PrimitiveType.Cube, "Cinta metrica", f, new Vector3(-15f, AlturaMesa + 0.05f, 70f), new Vector3(2.5f, 0.1f, 170f), fondoMostaza);
            // Pared del taller al fondo y una estantería.
            M.Decorado(PrimitiveType.Cube, "Pared", f, new Vector3(0f, AlturaMesa + 18f, 205f), new Vector3(160f, 40f, 2f), fondoPared);
            for (int i = 0; i < 3; i++)
                M.Decorado(PrimitiveType.Cube, "Balda", f, new Vector3(0f, AlturaMesa + 8f + i * 7f, 202f), new Vector3(70f, 0.6f, 4f), mesa);
        }

        static void CarreteFondo(Transform padre, Vector3 pie, float radio, float alto, Material hilo)
        {
            M.Decorado(PrimitiveType.Cylinder, "Carrete", padre, pie + Vector3.up * alto * 0.5f, new Vector3(radio * 2f, alto * 0.5f, radio * 2f), hilo);
            M.Decorado(PrimitiveType.Cylinder, "Tapa", padre, pie + Vector3.up * 0.3f, new Vector3(radio * 2.5f, 0.3f, radio * 2.5f), madera);
            M.Decorado(PrimitiveType.Cylinder, "Tapa", padre, pie + Vector3.up * (alto - 0.3f), new Vector3(radio * 2.5f, 0.3f, radio * 2.5f), madera);
        }

        static void BotonFondo(Transform padre, Vector3 pie, float radio, Material material)
        {
            M.Decorado(PrimitiveType.Cylinder, "Boton", padre, pie + Vector3.up * 0.4f, new Vector3(radio * 2f, 0.4f, radio * 2f), material);
            foreach (var o in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
                M.Decorado(PrimitiveType.Cylinder, "Agujero", padre, pie + new Vector3(o.x * radio * 0.22f, 0.82f, o.y * radio * 0.22f), new Vector3(radio * 0.25f, 0.03f, radio * 0.25f), mesa);
        }
    }
}
