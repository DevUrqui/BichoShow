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
    /// Menú "BichoShow > Crear escena: Costura a la fuga". La carrera sobre la mesa de costura,
    /// a lo largo del eje Z (unos 335 m). Tras la salida hay cruces: en cada uno se elige entre
    /// dos caminos con pruebas distintas, que se juntan en un descanso con punto de control:
    ///  1. Salida: la Tabla de Corte (z 0-23) y el escalón ancho.
    ///  2. Botones giratorios (derecha) o Carretes viajeros (izquierda) → Descanso 1 (z 46-53).
    ///  3. Puente acordeón (izquierda) o Cinta métrica rodante con agujas de tejer (derecha) → Descanso 2 (z 72-78).
    ///  4. Cremallera estrecha y rápida (derecha) o ancha y lenta (izquierda) → Destino (z 100-107).
    ///  5. Escalera de carretes (derecha) o Alfileteros (izquierda) → Descanso 5 (z 137-146).
    ///  6. Botones elásticos (izquierda) o Pasarela de los dedales boxeadores (derecha) → Descanso 6.
    ///  7. El campo de alfileres (salen por oleadas y lanzan por los aires) → Descanso 7.
    ///  8. Carretes rodantes (izquierda) o Botones-tecla (derecha) → Descanso 8.
    ///  9. Las tijeras gigantes (abiertas, por el centro; cerradas, por los lados) → Meta junto al dedal (z 325-334).
    ///
    /// Medidas pensadas para el bicho: corre a 6 m/s, salta ~2,1 m de alto y ~6,5 m de largo
    /// corriendo. Saltos: huecos de 1,5-2,5 m y escalones de hasta 1,2 m.
    /// </summary>
    public static class CreadorCostura
    {
        public const string RutaEscena = E.CarpetaEscenas + "/Costura.unity";
        /// <summary>Sube al cambiar el recorrido: al darle a Play se avisa si la escena es de antes (EscenasAnticuadas).</summary>
        public const int Version = 2;
        /// <summary>Altura de la segunda mitad (desde el Descanso 5 hasta la meta).</summary>
        const float Y2 = 3.8f;
        /// <summary>Altura de la mesa (lo de abajo, decorado). La carrera va por encima, sobre patas.</summary>
        const float AlturaMesa = -9f;

        static Transform mapa, suelos, obstaculos, decorado, rutaBots;
        static readonly List<PuntoRuta> ruta = new List<PuntoRuta>();
        static Material turquesa, turquesaClaro, mostaza, mostazaClaro, coral, azul, madera, mesa, crema, dorado, rejilla, dedal, negro;
        static Material fondoCoral, fondoAzul, fondoMostaza, fondoTurquesa, fondoPared, fondoGris;
        // Telas con dibujo (TelasCostura): para las losas, los parches, la madera y la pared.
        static Material telaVichy, telaVichyClaro, telaLunares, tablaCorte, maderaVeta, mesaVeta, papelPintado;
        static Material parcheLunares, parcheVichy, parcheRayas;

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
            mostazaClaro = E.CrearMaterial("CosturaMostazaClaro", new Color(0.97f, 0.8f, 0.42f), 0.25f);
            negro = E.CrearMaterial("CosturaNegro", new Color(0.12f, 0.1f, 0.1f), 0.2f);

            telaVichy = TelasCostura.Tela("CosturaTelaVichy", new Color(0.16f, 0.68f, 0.67f), TelasCostura.Vichy, 2f);
            telaVichyClaro = TelasCostura.Tela("CosturaTelaVichyClaro", new Color(0.42f, 0.84f, 0.79f), TelasCostura.Vichy, 2f);
            telaLunares = TelasCostura.Tela("CosturaTelaLunares", new Color(1f, 0.78f, 0.27f), TelasCostura.Lunares, 1.6f);
            tablaCorte = TelasCostura.Tela("CosturaTablaCorteTela", new Color(0.15f, 0.64f, 0.63f), TelasCostura.TablaCorte, 2f);
            maderaVeta = TelasCostura.Tela("CosturaMaderaVeta", new Color(0.95f, 0.78f, 0.57f), TelasCostura.Madera, 3f, 0.3f);
            mesaVeta = TelasCostura.Tela("CosturaMesaVeta", new Color(0.62f, 0.43f, 0.28f), TelasCostura.Madera, 9f, 0.35f);
            papelPintado = TelasCostura.Tela("CosturaPapelPintado", new Color(0.9f, 0.8f, 0.68f), TelasCostura.PapelPintado, 6f, 0.1f);
            parcheLunares = TelasCostura.Tela("CosturaParcheLunares", new Color(0.45f, 0.86f, 0.82f), TelasCostura.Lunares, 1.2f);
            parcheVichy = TelasCostura.Tela("CosturaParcheVichy", new Color(1f, 0.86f, 0.48f), TelasCostura.Vichy, 1.2f);
            parcheRayas = TelasCostura.Tela("CosturaParcheRayas", new Color(1f, 0.5f, 0.42f), TelasCostura.Rayas, 1.2f);
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
            camara.adelanto = 9f; // en una carrera hay que ver el obstáculo que viene
            camara.ColocarAlInstante();

            mapa = new GameObject("Mapa").transform;
            suelos = M.Grupo(mapa, "Suelos");
            obstaculos = M.Grupo(mapa, "Obstaculos");
            decorado = M.Grupo(mapa, "Decorado");
            rutaBots = M.Grupo(mapa, "RutaBots");
            var controles = new List<PuntoControlCarrera>();

            // =================================================================
            // 1. SALIDA: la Tabla de Corte. Tras un saltito, el escalón ancho donde se elige camino.
            // =================================================================
            Losa("TablaCorte", -9f, 9f, 0f, 14f, 0f, tablaCorte);
            Cuadricula(-9f, 9f, 0f, 14f, 0f);
            Regla(-9f, 0f, 14f, 0f);
            Barandillas(0f, (-8.85f, 0.15f, -8.85f, 13.85f), (8.85f, 0.15f, 8.85f, 13.85f), (-8.85f, 0.15f, 8.85f, 0.15f));
            Guirnalda(new Vector3(-8.3f, 0f, 0.8f), new Vector3(-8.3f, 0f, 13.2f));
            Guirnalda(new Vector3(8.3f, 0f, 0.8f), new Vector3(8.3f, 0f, 13.2f));
            Arco("SALIDA", 4f, 0f, 18f);
            Flecha(new Vector3(0f, 0f, 10.5f), 0f, crema);
            Losa("Escalon", -12f, 12f, 15.5f, 23f, 0.6f, telaVichyClaro);
            Pespunte(-12f, 12f, 15.5f, 23f, 0.6f);
            Parches(-12f, 12f, 15.5f, 23f, 0.6f, parcheLunares);
            Flecha(new Vector3(-7f, 0.6f, 21f), -12f, azul);     // a los carretes
            Flecha(new Vector3(7.5f, 0.6f, 21f), 12f, azul);     // a los botones
            controles.Add(PuntoControl(1, 18f, -12f, 12f, 0.6f));

            // =================================================================
            // 2a. LOS CARRETES VIAJEROS (izquierda): esperar el hueco y cruzar
            // =================================================================
            Losa("SueloCarretes", -12f, -2f, 23f, 46f, 0.6f, telaLunares);
            float[] franjas = { 27f, 34f, 41f };                   // centro de cada franja (4 m de fondo)
            float[] periodos = { 4.4f, 3.6f, 2.9f };               // de más lento a más rápido
            float[] desfases = { 0f, 0.35f, 0.7f };
            Material[] hilos = { coral, azul, turquesa };
            var carretes = new CarreteViajero[3];
            for (int i = 0; i < 3; i++)
            {
                Pegatina($"Guia carrete {i + 1}", -12f, -2f, franjas[i] - 1.7f, franjas[i] + 1.7f, 0.6f, azul);
                Pegatina($"Guia carrete {i + 1} centro", -11.6f, -2.4f, franjas[i] - 0.08f, franjas[i] + 0.08f, 0.615f, crema);
                carretes[i] = Carrete($"Carrete {i + 1}", -7f, 5f, franjas[i], periodos[i], desfases[i], hilos[i]);
            }
            Pespunte(-12f, -2f, 29f, 32f, 0.6f);
            Pespunte(-12f, -2f, 36f, 39f, 0.6f);

            // =================================================================
            // 2b. LOS BOTONES GIRATORIOS (derecha): de botón en botón, saltando, mientras giran
            // =================================================================
            var botones = new[] { new Vector2(6.5f, 26.8f), new Vector2(9f, 32.6f), new Vector2(6.5f, 38.4f), new Vector2(9f, 44f) };
            for (int i = 0; i < botones.Length; i++)
                BotonGiratorio($"Boton giratorio {i + 1}", botones[i], 0.6f, 2f, i % 2 == 0 ? 40f : -48f, i % 2 == 0 ? coral : mostaza);

            // Descanso 1 (hacia la derecha): se juntan los dos caminos.
            Losa("Descanso1", -12f, 14f, 46f, 53f, 0.6f, telaVichy);
            Pespunte(-12f, 14f, 46f, 53f, 0.6f);
            Parches(-12f, 14f, 46f, 53f, 0.6f, parcheLunares);
            Guirnalda(new Vector3(-11.4f, 0.6f, 46.6f), new Vector3(-11.4f, 0.6f, 52.4f));
            Guirnalda(new Vector3(13.4f, 0.6f, 46.6f), new Vector3(13.4f, 0.6f, 52.4f));
            controles.Add(PuntoControl(2, 48.5f, -12f, 14f, 0.6f));
            Flecha(new Vector3(-6f, 0.6f, 51.3f), 0f, crema);
            Flecha(new Vector3(8f, 0.6f, 51.3f), 0f, crema);

            // =================================================================
            // 3a. EL PUENTE ACORDEÓN (izquierda)
            // =================================================================
            // Seis paneles seguidos; cada uno se inclina 0,525 s después del anterior (lo que tarda
            // un bicho en cruzarlo): quien entra cuando el primero se pone plano cruza en "ola verde".
            var paneles = new PanelAcordeon[6];
            for (int i = 0; i < 6; i++)
            {
                float z0 = 53.15f + 3.15f * i;
                paneles[i] = Panel(i, -6f, z0, z0 + 3f, 0.6f, i % 2 == 0 ? 1f : -1f, 0.525f * i, i % 2 == 0 ? coral : azul);
            }
            paneles[0].ventanaBot = 1.0f;
            Bisagras(-6f, 53.15f, 6, 3.15f, 0.6f);

            // =================================================================
            // 3b. LA CINTA MÉTRICA RODANTE (derecha): va hacia atrás y la barren agujas de tejer
            // =================================================================
            CintaRodante(8f, 6f, 53f, 72f, 0.6f);
            Aguja("Aguja de tejer 1", new Vector3(8f, 0.6f, 59f), 95f, coral);
            Aguja("Aguja de tejer 2", new Vector3(8f, 0.6f, 66f), -115f, azul);

            // Descanso 2 (hacia la izquierda)
            Losa("Descanso2", -14f, 12f, 72f, 78f, 0.6f, telaVichy);
            Pespunte(-14f, 12f, 72f, 78f, 0.6f);
            Parches(-14f, 12f, 72f, 78f, 0.6f, parcheLunares);
            Guirnalda(new Vector3(-13.4f, 0.6f, 72.6f), new Vector3(-13.4f, 0.6f, 77.4f));
            Guirnalda(new Vector3(11.4f, 0.6f, 72.6f), new Vector3(11.4f, 0.6f, 77.4f));
            controles.Add(PuntoControl(3, 74.5f, -14f, 12f, 0.6f));
            Flecha(new Vector3(-8f, 0.6f, 76.6f), 0f, crema);
            Flecha(new Vector3(4f, 0.6f, 76.6f), 0f, crema);

            // =================================================================
            // 4. LAS CREMALLERAS GEMELAS
            // =================================================================
            // Izquierda: ancha (4,4 m) y lenta, en dos mitades con un descansillo entre medias.
            var izq1 = Cremallera("Cremallera ancha 1", -8f, 4.4f, 78f, 5, 2f, 0.6f, 2.5f, 2.5f, 1f, 0f, coral);
            Losa("Descansillo", -10.2f, -5.8f, 88f, 90.5f, 0.6f, telaLunares);
            Pespunte(-10.2f, -5.8f, 88f, 90.5f, 0.6f);
            var izq2 = Cremallera("Cremallera ancha 2", -8f, 4.4f, 90.5f, 5, 2f, 0.6f, 2.5f, 2.5f, 1f, 4.5f, coral);
            // Derecha: estrecha (2,4 m) y de una tirada, con el cursor más rápido.
            var der = Cremallera("Cremallera estrecha", 4f, 2.4f, 78f, 9, 2.5f, 0.6f, 4.6f, 1.4f, 0.8f, 0f, azul);

            Losa("Destino", -12f, 12f, 100.5f, 107f, 0.6f, telaVichy);
            Pespunte(-12f, 12f, 100.5f, 107f, 0.6f);
            Parches(-12f, 12f, 100.5f, 107f, 0.6f, parcheLunares);
            Guirnalda(new Vector3(-11.4f, 0.6f, 101.1f), new Vector3(-11.4f, 0.6f, 106.4f));
            Guirnalda(new Vector3(11.4f, 0.6f, 101.1f), new Vector3(11.4f, 0.6f, 106.4f));
            controles.Add(PuntoControl(4, 103.5f, -12f, 12f, 0.6f));
            Flecha(new Vector3(-4f, 0.6f, 105.6f), 0f, crema);
            Flecha(new Vector3(9f, 0.6f, 105.6f), 0f, crema);

            // =================================================================
            // 5a. LOS ALFILETEROS (izquierda): el del medio se hunde si te quedas encima
            // =================================================================
            Rampa("Rampa alfiletero", -4f, 6f, 107f, 0.6f, 110f, 1.6f, crema);
            Cojin("Alfiletero 1", -10f, 2f, 110f, 117f, 1.6f, mostaza, false);
            controles.Add(PuntoControl(5, 112.5f, -10f, 2f, 1.6f));
            Flecha(new Vector3(-4f, 1.6f, 115.3f), 0f, azul);
            Cojin("Alfiletero 2 (blando)", -10f, 2f, 119f, 126f, 2.6f, coral, true);
            Losa("Escalon lateral", 2f, 4.5f, 122f, 125f, 2.6f, maderaVeta);
            Rampa("Rampa lateral", 3.25f, 2.5f, 125f, 2.6f, 128.5f, 3.8f, crema);
            Cojin("Alfiletero 3", -10f, 4.5f, 128.5f, 135f, 3.8f, turquesaClaro, false);
            SoloColision(Losa("Cinta metrica", -7f, -1f, 135f, 137f, 3.8f, mostaza, 0.25f, false));
            Kit("PuenteCinta", decorado, new Vector3(-4f, 3.8f, 136f));

            // =================================================================
            // 5b. LA ESCALERA DE CARRETES (derecha): de pie, cada uno más alto; dos giran
            // =================================================================
            var pilares = new[] { (9f, 110.5f, 1.2f, 0f), (11f, 115.8f, 1.9f, 40f), (9f, 121.1f, 2.6f, 0f), (11f, 126.4f, 3.2f, -45f), (9f, 131.7f, 3.8f, 0f) };
            Material[] hilosPilar = { turquesa, coral, mostaza, azul, coral };
            for (int i = 0; i < pilares.Length; i++)
                CarretePilar($"Carrete escalera {i + 1}", pilares[i].Item1, pilares[i].Item2, pilares[i].Item3, pilares[i].Item4, hilosPilar[i]);
            controles.Add(PuntoControl(5, 121.1f, 7.3f, 10.7f, 2.6f, 0.5f));
            SoloColision(Losa("Cinta metrica 2", 7.5f, 10.5f, 133.5f, 137f, 3.8f, mostaza, 0.25f, false));
            var cintaDerecha = Kit("PuenteCinta", decorado, new Vector3(9f, 3.8f, 135.25f), 90f);
            cintaDerecha.transform.localScale = Vector3.Scale(cintaDerecha.transform.localScale, new Vector3(0.58f, 1f, 1.5f));

            // =================================================================
            // DESCANSO 5 (donde antes estaba la meta): la carrera sigue
            // =================================================================
            Descanso("Descanso5", -12f, 12f, 137f, 146f, Y2, 6, controles, tablaCorte);
            Flecha(new Vector3(-6.5f, Y2, 144.6f), -8f, azul);   // a los botones elásticos
            Flecha(new Vector3(7f, Y2, 144.6f), 8f, azul);       // a los dedales

            // =================================================================
            // 6a. LOS BOTONES ELÁSTICOS (izquierda): botas en un botón y te lanza al siguiente
            // =================================================================
            Losa("Antes de los botones", -10f, -3f, 146f, 150f, Y2, telaLunares);
            var elasticos = new[]
            {
                BotonElastico("Boton elastico 1", new Vector3(-6.5f, Y2, 154f), 0f, coral),
                BotonElastico("Boton elastico 2", new Vector3(-6.5f, Y2, 168f), 0.33f, mostaza),
                BotonElastico("Boton elastico 3", new Vector3(-6.5f, Y2, 182f), 0.66f, turquesa),
            };

            // =================================================================
            // 6b. LA PASARELA DE LOS DEDALES (derecha): estrecha, y los dedales empujan al vacío
            // =================================================================
            Losa("Pasarela dedales", 5.5f, 8.5f, 146f, 190f, Y2, maderaVeta);
            Pespunte(5.5f, 8.5f, 146f, 190f, Y2);
            float[] zDedales = { 151f, 157f, 163f, 169f, 175f, 181f, 186f };
            for (int i = 0; i < zDedales.Length; i++)
                DedalBoxeador($"Dedal boxeador {i + 1}", 7f, Y2, zDedales[i], i % 2 == 0 ? 1f : -1f, i * 0.13f, i % 2 == 0 ? coral : azul);

            // =================================================================
            // DESCANSO 6
            // =================================================================
            Descanso("Descanso6", -12f, 12f, 190f, 197f, Y2, 7, controles, telaVichy);
            Flecha(new Vector3(0f, Y2, 195.6f), 0f, crema);

            // =================================================================
            // 7. EL CAMPO DE ALFILERES: salen del acerico por oleadas y te lanzan por los aires
            // =================================================================
            var campo = CampoDeAlfileres(-8f, 8f, 197f, 237f, Y2);

            // =================================================================
            // DESCANSO 7
            // =================================================================
            Descanso("Descanso7", -12f, 12f, 237f, 244f, Y2, 8, controles, telaVichy);
            Flecha(new Vector3(-6f, Y2, 242.6f), 0f, azul);      // a los carretes rodantes
            Flecha(new Vector3(7f, Y2, 242.6f), 0f, azul);       // a los botones-tecla

            // =================================================================
            // 8a. LOS CARRETES RODANTES (izquierda): correr por encima sin que el giro te tire
            // =================================================================
            var rodantes = new List<CarreteRodante>();
            float zCarrete = 245f;
            Material[] hilosRodantes = { coral, turquesa, mostaza, azul };
            for (int i = 0; i < 4; i++)
            {
                rodantes.Add(CarreteRodando($"Carrete rodante {i + 1}", -6f, zCarrete, zCarrete + 8f, Y2 + 0.6f,
                                            2f + 0.4f * i, (i * 0.27f) % 1f, hilosRodantes[i]));
                zCarrete += 9.6f;
            }

            // =================================================================
            // 8b. LOS BOTONES-TECLA (derecha): suben y bajan como teclas de piano, a destiempo
            // =================================================================
            var teclas = new List<PetaloPiano>();
            for (int i = 0; i < 7; i++)
                teclas.Add(BotonTecla($"Boton tecla {i + 1}", new Vector2(i % 2 == 0 ? 6.4f : 8.2f, 247.5f + 5f * i), Y2, 1.6f,
                                      (i * 0.17f) % 1f, i % 2 == 0 ? mostaza : coral));

            // =================================================================
            // DESCANSO 8
            // =================================================================
            Descanso("Descanso8", -12f, 12f, 284f, 291f, Y2, 9, controles, telaVichy);
            Flecha(new Vector3(0f, Y2, 289.6f), 0f, crema);

            // =================================================================
            // 9. LAS TIJERAS GIGANTES: abiertas, por el centro; cerradas, por los lados
            // =================================================================
            Losa("Puente de las tijeras", -5f, 5f, 291f, 325f, Y2, telaVichyClaro);
            Pespunte(-5f, 5f, 291f, 325f, Y2);
            var tijeras = new[]
            {
                Tijeras("Tijeras 1", 296f, Y2, 2.2f, 1.0f, 0f, coral),
                Tijeras("Tijeras 2", 303.5f, Y2, 1.7f, 1.2f, 0.35f, azul),
                Tijeras("Tijeras 3", 311f, Y2, 1.5f, 1.0f, 0.7f, mostaza),
                Tijeras("Tijeras 4", 318.5f, Y2, 1.2f, 1.4f, 0.15f, coral),
            };

            // =================================================================
            // META
            // =================================================================
            Losa("Meta", -12f, 12f, 325f, 334f, Y2, tablaCorte);
            Cuadricula(-12f, 12f, 325f, 334f, Y2);
            Regla(-12f, 325f, 334f, Y2);
            Barandillas(Y2, (-11.85f, 325.2f, -11.85f, 333.85f), (11.85f, 325.2f, 11.85f, 333.85f), (-11.85f, 333.85f, 11.85f, 333.85f));
            Guirnalda(new Vector3(-11.3f, Y2, 325.8f), new Vector3(-11.3f, Y2, 333.3f));
            Guirnalda(new Vector3(11.3f, Y2, 325.8f), new Vector3(11.3f, Y2, 333.3f));
            Arco("META", 329f, Y2, 18f);
            var meta = M.ZonaTrigger<MetaCarrera>("Meta", mapa, new Vector3(0f, Y2 + 1.5f, 331.5f), new Vector3(24f, 3f, 5f));
            Dedal(new Vector3(15.5f, Y2, 331f));

            // =================================================================
            // Ruta de los bots ([difícil, fácil] en cada cruce)
            // =================================================================
            var w0 = Ruta("Salida", 0f, 0f, 9f, 3f);
            var w1 = Ruta("Salto escalon", 0f, 0f, 13.6f, 3f, saltar: true);
            var f1 = Ruta("Cruce 1", 0f, 0.6f, 20.5f, 3f);
            var s0 = Ruta("Salto boton 1", 6.5f, 0.6f, 22.6f, 0.6f, saltar: true);
            var s1 = Ruta("Salto boton 2", 7.14f, 0.6f, 28.27f, 0.3f, saltar: true);
            var s2 = Ruta("Salto boton 3", 8.36f, 0.6f, 34.07f, 0.3f, saltar: true);
            var s3 = Ruta("Salto boton 4", 7.14f, 0.6f, 39.87f, 0.3f, saltar: true);
            var c0 = Ruta("Espera carrete 1", -7f, 0.6f, 24.3f, 2.5f, esperar: carretes[0]);
            var c1 = Ruta("Espera carrete 2", -7f, 0.6f, 31.3f, 2.5f, esperar: carretes[1]);
            var c2 = Ruta("Espera carrete 3", -7f, 0.6f, 38.3f, 2.5f, esperar: carretes[2]);
            var c3 = Ruta("Fin carretes", -7f, 0.6f, 45f, 2.5f);
            var f2 = Ruta("Cruce 2", 1f, 0.6f, 49.5f, 2f);
            var a0 = Ruta("Entrada acordeon", -6f, 0.6f, 52.6f, 0.8f, esperar: paneles[0]);
            var a1 = Ruta("Salida acordeon", -6f, 0.6f, 72.6f, 0.8f);
            var k0 = Ruta("Entrada cinta", 8f, 0.6f, 53.6f, 1.5f);
            var k1 = Ruta("Salto aguja 1", 8f, 0.6f, 57.4f, 1.2f, saltar: true);
            var k2 = Ruta("Salto aguja 2", 8f, 0.6f, 64.4f, 1.2f, saltar: true);
            var k3 = Ruta("Salida cinta", 8f, 0.6f, 72.8f, 1.5f);
            var f3 = Ruta("Cruce 3", -1f, 0.6f, 75.2f, 2f);
            var rc0 = Ruta("Entrada cremallera estrecha", 4f, 0.6f, 77.6f, 0.35f, esperar: der);
            var rc1 = Ruta("Salida cremallera estrecha", 4f, 0.6f, 101.6f, 0.35f);
            var lc0 = Ruta("Entrada cremallera ancha", -8f, 0.6f, 77.6f, 1.2f, esperar: izq1);
            var lc1 = Ruta("Descansillo cremalleras", -8f, 0.6f, 89.3f, 1.2f, esperar: izq2);
            var lc2 = Ruta("Salida cremallera ancha", -8f, 0.6f, 101.6f, 1.2f);
            var f4 = Ruta("Cruce 4", 0f, 0.6f, 103.6f, 2f);
            var p0 = Ruta("Salto carrete 1", 9f, 0.6f, 106.4f, 0.4f, saltar: true);
            var p1 = Ruta("Salto carrete 2", 9.49f, 1.2f, 111.82f, 0.2f, saltar: true);
            var p2 = Ruta("Salto carrete 3", 10.51f, 1.9f, 117.12f, 0.2f, saltar: true);
            var p3 = Ruta("Salto carrete 4", 9.49f, 2.6f, 122.42f, 0.2f, saltar: true);
            var p4 = Ruta("Salto carrete 5", 10.51f, 3.2f, 127.72f, 0.2f, saltar: true);
            var p5 = Ruta("Fin escalera", 9f, 3.8f, 133.6f, 0.4f);
            var e1 = Ruta("Pie de la rampa", -4f, 0.6f, 106.6f, 1.5f);
            var e2 = Ruta("Alfiletero 1", -4f, 1.6f, 111f, 2f);
            var e3 = Ruta("Salto al blando", -4f, 1.6f, 116.6f, 2f, saltar: true);
            var e4 = Ruta("Salto al lateral", 1.3f, 2.6f, 123f, 0.3f, saltar: true);
            var e5 = Ruta("Alfiletero 3", 3.25f, 3.8f, 130f, 0.5f);
            var e6 = Ruta("Cinta metrica", -4f, 3.8f, 136f, 1.2f);
            var d5 = Ruta("Descanso 5", 0f, Y2, 142f, 3f);
            // Botones elásticos (difícil) o dedales (fácil)
            var be0 = Ruta("Antes de los botones", -6.5f, Y2, 149.4f, 0.6f, saltar: true);
            var be1 = Ruta("Boton elastico 1", -6.5f, Y2 + 0.8f, 154f, 0.4f);
            var be2 = Ruta("Boton elastico 2", -6.5f, Y2 + 0.8f, 168f, 0.4f);
            var be3 = Ruta("Boton elastico 3", -6.5f, Y2 + 0.8f, 182f, 0.4f);
            var dd0 = Ruta("Entrada dedales", 7f, Y2, 147f, 0.3f);
            var dd1 = Ruta("Salida dedales", 7f, Y2, 189.5f, 0.3f);
            var d6 = Ruta("Descanso 6", 0f, Y2, 193.5f, 3f);
            var al0 = Ruta("Alfileres 1", 0f, Y2, 205f, 4f);
            var al1 = Ruta("Alfileres 2", 0f, Y2, 220f, 4f);
            var al2 = Ruta("Alfileres 3", 0f, Y2, 235f, 4f);
            var d7 = Ruta("Descanso 7", 0f, Y2, 240.5f, 3f);
            // Carretes rodantes (difícil) o botones-tecla (fácil)
            var rr = new List<PuntoRuta> { Ruta("Al primer carrete", -6f, Y2, 243.6f, 0.4f, saltar: true) };
            for (int i = 0; i < rodantes.Count; i++)
            {
                float zFin = 245f + 9.6f * i + 8f;
                rr.Add(Ruta($"Carrete rodante {i + 1}", -6f, Y2 + 0.6f, zFin - 0.6f, 0.2f, saltar: true));
            }
            var tt = new List<PuntoRuta> { Ruta("A la primera tecla", 7f, Y2, 245.2f, 0.4f, saltar: true) };
            for (int i = 0; i < teclas.Count; i++)
                tt.Add(Ruta($"Boton tecla {i + 1}", i % 2 == 0 ? 6.4f : 8.2f, Y2, 247.5f + 5f * i, 0.3f, saltar: true));
            var d8 = Ruta("Descanso 8", 0f, Y2, 287.5f, 3f);
            var tj = new List<PuntoRuta>();
            for (int i = 0; i < tijeras.Length; i++)
                tj.Add(Ruta($"Antes de las tijeras {i + 1}", 0f, Y2, tijeras[i].transform.position.z - 2.6f, 0.8f, esperar: tijeras[i]));
            var tjFin = Ruta("Fin de las tijeras", 0f, Y2, 324f, 1.5f);
            var em = Ruta("Meta", 0f, Y2, 331.5f, 3f);

            Unir(w0, w1); Unir(w1, f1);
            Unir(f1, s0, c0);                                     // botones (difícil) o carretes (fácil)
            Unir(s0, s1); Unir(s1, s2); Unir(s2, s3); Unir(s3, f2);
            Unir(c0, c1); Unir(c1, c2); Unir(c2, c3); Unir(c3, f2);
            Unir(f2, a0, k0);                                     // acordeón o cinta rodante
            Unir(a0, a1); Unir(a1, f3);
            Unir(k0, k1); Unir(k1, k2); Unir(k2, k3); Unir(k3, f3);
            Unir(f3, rc0, lc0);                                   // cremallera estrecha o ancha
            Unir(rc0, rc1); Unir(rc1, f4);
            Unir(lc0, lc1); Unir(lc1, lc2); Unir(lc2, f4);
            Unir(f4, p0, e1);                                     // escalera de carretes o alfileteros
            Unir(p0, p1); Unir(p1, p2); Unir(p2, p3); Unir(p3, p4); Unir(p4, p5);
            Unir(e1, e2); Unir(e2, e3); Unir(e3, e4); Unir(e4, e5); Unir(e5, e6);
            Unir(p5, d5); Unir(e6, d5);
            Unir(d5, be0, dd0);                                   // botones elásticos o dedales
            Unir(be0, be1); Unir(be1, be2); Unir(be2, be3); Unir(be3, d6);
            Unir(dd0, dd1); Unir(dd1, d6);
            Unir(d6, al0); Unir(al0, al1); Unir(al1, al2); Unir(al2, d7);
            Unir(d7, rr[0], tt[0]);                               // carretes rodantes o teclas
            for (int i = 0; i < rr.Count - 1; i++) Unir(rr[i], rr[i + 1]);
            Unir(rr[rr.Count - 1], d8);
            for (int i = 0; i < tt.Count - 1; i++) Unir(tt[i], tt[i + 1]);
            Unir(tt[tt.Count - 1], d8);
            Unir(d8, tj[0]);
            for (int i = 0; i < tj.Count - 1; i++) Unir(tj[i], tj[i + 1]);
            Unir(tj[tj.Count - 1], tjFin); Unir(tjFin, em);


            // =================================================================
            // Caída, decorado, jugadores y ronda
            // =================================================================
            var vacio = M.ZonaTrigger<ZonaMortal>("ZonaCaida", mapa, new Vector3(2f, -5f, 165f), new Vector3(140f, 3f, 440f));
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
            ronda.zMeta = 329f;
            ronda.duracionMinima = 240f;
            new GameObject(EscenasAnticuadas.Marca + Version);

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
            if (patas)
            {
                Patas(x0, x1, z0, z1, arriba - grosor - 0.3f);
                Vestir(x0, x1, z0, z1, arriba, grosor);
            }
            return go;
        }

        /// <summary>
        /// Lo que hace que una losa parezca de costura y no un bloque: un marco de madera debajo
        /// (la tela va "tapizada" encima), un ribete acolchado por el borde y botones en el frente.
        /// </summary>
        static void Vestir(float x0, float x1, float z0, float z1, float arriba, float grosor)
        {
            float w = x1 - x0, d = z1 - z0, abajo = arriba - grosor;
            M.Decorado(PrimitiveType.Cube, "Marco", decorado, new Vector3((x0 + x1) * 0.5f, abajo - 0.15f, (z0 + z1) * 0.5f),
                       new Vector3(w + 0.35f, 0.3f, d + 0.35f), maderaVeta);

            // Ribete: un cordón por los cuatro bordes de arriba.
            const float r = 0.11f, dentro = 0.06f;
            foreach (float z in new[] { z0 + dentro, z1 - dentro })
            {
                var c = M.Decorado(PrimitiveType.Cylinder, "Ribete", decorado, new Vector3((x0 + x1) * 0.5f, arriba, z), new Vector3(r * 2f, w * 0.5f, r * 2f), crema);
                if (c != null) c.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }
            foreach (float x in new[] { x0 + dentro, x1 - dentro })
            {
                var c = M.Decorado(PrimitiveType.Cylinder, "Ribete", decorado, new Vector3(x, arriba, (z0 + z1) * 0.5f), new Vector3(r * 2f, d * 0.5f, r * 2f), crema);
                if (c != null) c.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }

            // Botones cosidos en el frente (la cara que ve la cámara).
            int botones = Mathf.Max(2, Mathf.RoundToInt(w / 6f) + 1);
            for (int i = 0; i < botones; i++)
            {
                float x = Mathf.Lerp(x0 + 0.7f, x1 - 0.7f, botones == 1 ? 0.5f : i / (float)(botones - 1));
                var boton = Kit("Boton", decorado, new Vector3(x, arriba - grosor * 0.5f, z0 - 0.02f), 0f, i % 2 == 0 ? dorado : coral, 0.38f);
                boton.transform.rotation = Quaternion.Euler(-90f, 0f, 0f) * boton.transform.rotation;
            }
        }

        /// <summary>
        /// Parches de tela cosidos encima de un suelo: en damero, del tono "parche" (y alguno de
        /// coral para que no sea monótono), cada uno con su pespunte.
        /// </summary>
        static void Parches(float x0, float x1, float z0, float z1, float arriba, Material parche)
        {
            int nx = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / 4f)), nz = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / 3.5f));
            float cx = (x1 - x0) / nx, cz = (z1 - z0) / nz;
            int semilla = Mathf.Abs(Mathf.RoundToInt(x0 * 7f + z0 * 13f));
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    if ((i + j) % 2 == 1) continue;
                    bool acento = (i * 3 + j * 5 + semilla) % 7 == 0;
                    float a0 = x0 + i * cx + 0.45f, a1 = x0 + (i + 1) * cx - 0.45f;
                    float b0 = z0 + j * cz + 0.45f, b1 = z0 + (j + 1) * cz - 0.45f;
                    if (a1 - a0 < 0.8f || b1 - b0 < 0.8f) continue;
                    Pegatina("Parche", a0, a1, b0, b1, arriba, acento ? parcheRayas : parche, -0.004f);
                    const float g = 0.06f, m = 0.15f;
                    Pegatina("Pespunte parche", a0 + m, a1 - m, b0 + m, b0 + m + g, arriba, crema);
                    Pegatina("Pespunte parche", a0 + m, a1 - m, b1 - m - g, b1 - m, arriba, crema);
                    Pegatina("Pespunte parche", a0 + m, a0 + m + g, b0 + m, b1 - m, arriba, crema);
                    Pegatina("Pespunte parche", a1 - m - g, a1 - m, b0 + m, b1 - m, arriba, crema);
                }
        }

        /// <summary>Regla en el borde izquierdo de la tabla de corte: marcas cada medio metro.</summary>
        static void Regla(float x0, float z0, float z1, float arriba)
        {
            Pegatina("Regla", x0 + 0.5f, x0 + 1.5f, z0 + 0.5f, z1 - 0.5f, arriba, mostazaClaro, 0.004f);
            for (float z = z0 + 1f; z < z1 - 0.6f; z += 0.5f)
            {
                bool larga = Mathf.Abs(z - Mathf.Round(z)) < 0.01f;
                Pegatina("Marca regla", x0 + 0.5f, x0 + (larga ? 1.1f : 0.85f), z - 0.025f, z + 0.025f, arriba, negro, 0.008f);
            }
        }

        /// <summary>Patas de madera de una losa hasta la mesa (decorado).</summary>
        static void Patas(float x0, float x1, float z0, float z1, float abajo)
        {
            float alto = abajo - AlturaMesa;
            if (alto <= 0.1f) return;
            float m = Mathf.Min(0.8f, (x1 - x0) * 0.25f), n = Mathf.Min(0.8f, (z1 - z0) * 0.25f);
            foreach (var x in new[] { x0 + m, x1 - m })
                foreach (var z in new[] { z0 + n, z1 - n })
                    M.Decorado(PrimitiveType.Cylinder, "Pata", decorado, new Vector3(x, abajo - alto * 0.5f, z), new Vector3(0.6f, alto * 0.5f, 0.6f), maderaVeta);
        }

        /// <summary>Algo pintado en el suelo (sin colisión): guías, marcas.</summary>
        static void Pegatina(string nombre, float x0, float x1, float z0, float z1, float arriba, Material material, float elevar = 0f)
        {
            M.Decorado(PrimitiveType.Cube, nombre, decorado, new Vector3((x0 + x1) * 0.5f, arriba + 0.012f + elevar, (z0 + z1) * 0.5f),
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
        static CarreteViajero Carrete(string nombre, float x, float mitadAncho, float z, float periodo, float desfase, Material hilo)
        {
            const float radio = 1.25f;
            var go = new GameObject(nombre);
            go.transform.SetParent(obstaculos, false);
            go.transform.position = new Vector3(x, 0.6f + radio + 0.05f, z);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // el eje Y del cilindro, a lo largo de Z
            go.AddComponent<Rigidbody>().isKinematic = true;
            var capsula = go.AddComponent<CapsuleCollider>();
            capsula.direction = 1;
            capsula.radius = radio + 0.1f;
            capsula.height = 3.4f;

            // El modelo, en coordenadas del mundo (eje a lo largo de Z) para no heredar el giro del collider.
            Kit("Carrete", go.transform, go.transform.position, 0f, hilo);

            var carrete = go.AddComponent<CarreteViajero>();
            carrete.recorrido = mitadAncho - radio - 0.35f;
            carrete.periodo = periodo;
            carrete.desfase = desfase;
            carrete.radio = radio;
            return carrete;
        }

        /// <summary>Botón gigante que gira (y arrastra a quien va encima), sobre una columna de madera.</summary>
        static PlataformaGiratoria BotonGiratorio(string nombre, Vector2 centro, float arriba, float radio, float velocidad, Material color)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(obstaculos, false);
            go.transform.position = new Vector3(centro.x, arriba, centro.y);
            go.AddComponent<Rigidbody>().isKinematic = true;
            SoloColision(A.Disco(go.transform, "Cara", new Vector3(centro.x, 0f, centro.y), arriba, radio, 0.44f, color));
            Kit("Boton", go.transform, new Vector3(centro.x, arriba - radio * 0.22f, centro.y), 0f, color, radio);
            float alto = arriba - 0.5f - AlturaMesa;
            M.Decorado(PrimitiveType.Cylinder, "Columna", decorado, new Vector3(centro.x, AlturaMesa + alto * 0.5f, centro.y), new Vector3(0.8f, alto * 0.5f, 0.8f), maderaVeta);
            var giro = go.AddComponent<PlataformaGiratoria>();
            giro.velocidad = velocidad;
            giro.radio = radio;
            giro.alturaArriba = 0f;
            return giro;
        }

        /// <summary>Carrete de pie (la cara de arriba, Ø3,8, a la altura "arriba") sobre una columna. Si velocidad ≠ 0, gira.</summary>
        static void CarretePilar(string nombre, float x, float z, float arriba, float velocidad, Material hilo)
        {
            const float radioCara = 1.9f, altoCarrete = 3.2f;
            var go = new GameObject(nombre);
            go.transform.SetParent(obstaculos, false);
            go.transform.position = new Vector3(x, arriba, z);
            SoloColision(A.Disco(go.transform, "Cara", new Vector3(x, 0f, z), arriba, radioCara, 0.5f, hilo));
            Kit("CarreteDePie", go.transform, new Vector3(x, arriba - altoCarrete, z), 0f, hilo, altoCarrete / 2f);
            float alto = arriba - altoCarrete - AlturaMesa;
            M.Decorado(PrimitiveType.Cylinder, "Columna", decorado, new Vector3(x, AlturaMesa + alto * 0.5f, z), new Vector3(1.4f, alto * 0.5f, 1.4f), maderaVeta);
            if (Mathf.Abs(velocidad) < 0.01f) return;
            go.AddComponent<Rigidbody>().isKinematic = true;
            var giro = go.AddComponent<PlataformaGiratoria>();
            giro.velocidad = velocidad;
            giro.radio = radioCara;
            giro.alturaArriba = 0f;
        }

        /// <summary>Cinta métrica rodante: lleva hacia atrás a quien la pisa; sus marcas se mueven con ella.</summary>
        static CintaTransportadora CintaRodante(float x, float ancho, float z0, float z1, float arriba)
        {
            Losa("Cinta rodante", x - ancho * 0.5f, x + ancho * 0.5f, z0, z1, arriba, mostazaClaro);
            var go = new GameObject("Cinta rodante (motor)");
            go.transform.SetParent(obstaculos, false);
            go.transform.position = new Vector3(x, arriba, (z0 + z1) * 0.5f);
            float largo = z1 - z0;
            var marcas = new List<Transform>();
            for (float z = -largo * 0.5f + 0.25f; z < largo * 0.5f; z += 0.5f)
            {
                bool metro = Mathf.Abs(Mathf.Repeat(z + largo * 0.5f, 1f) - 0.25f) < 0.01f;
                foreach (float lado in new[] { -1f, 1f })
                {
                    float largoMarca = metro ? 1f : 0.5f;
                    var m = M.Decorado(PrimitiveType.Cube, "Marca", go.transform, new Vector3(lado * (ancho * 0.5f - 0.25f - largoMarca * 0.5f), 0.014f, z),
                                       new Vector3(largoMarca, 0.02f, 0.06f), negro, local: true);
                    if (m != null) marcas.Add(m.transform);
                }
            }
            // Flechas hacia atrás: hacia donde te lleva.
            for (int i = 0; i < 4; i++)
            {
                var flecha = Kit("Flecha", go.transform, new Vector3(0f, 0.02f, -largo * 0.5f + largo * (i + 0.5f) / 4f), 180f, coral, 1.1f, local: true);
                marcas.Add(flecha.transform);
            }
            var cinta = go.AddComponent<CintaTransportadora>();
            cinta.direccion = Vector3.back;
            cinta.velocidad = 2.8f;
            cinta.tamano = new Vector2(ancho, largo);
            cinta.marcas = marcas.ToArray();
            return cinta;
        }

        /// <summary>Aguja de tejer que gira a ras de la cinta: hay que saltarla (la puede acelerar un saboteador).</summary>
        static BarraGiratoria Aguja(string nombre, Vector3 centro, float velocidad, Material bola)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(obstaculos, false);
            go.transform.position = centro;
            go.AddComponent<Rigidbody>().isKinematic = true;
            var caja = go.AddComponent<BoxCollider>();
            caja.size = new Vector3(6f, 0.4f, 0.4f);
            caja.center = new Vector3(0f, 0.5f, 0f);
            var palo = E.Pieza(PrimitiveType.Cylinder, "Aguja", go.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.32f, 2.9f, 0.32f), dedal);
            palo.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var cabeza = E.Pieza(PrimitiveType.Sphere, "Cabeza", go.transform, new Vector3(2.95f, 0.5f, 0f), Vector3.one * 0.7f, bola);
            E.Pieza(PrimitiveType.Sphere, "Punta", go.transform, new Vector3(-2.95f, 0.5f, 0f), new Vector3(0.35f, 0.2f, 0.2f), dedal);
            E.Pieza(PrimitiveType.Cylinder, "Eje", go.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.25f, 0.5f), dorado);
            var barra = go.AddComponent<BarraGiratoria>();
            barra.velocidadGiro = velocidad;
            barra.nombreTrampa = nombre;
            barra.enfriamiento = 8f;
            barra.radioEfecto = 3.5f;
            barra.alturaMarcador = 2.5f;
            barra.renderersAviso = new Renderer[] { palo.GetComponent<Renderer>(), cabeza.GetComponent<Renderer>() };
            return barra;
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
            Kit("PanelAcordeon", go.transform, go.transform.position, 0f, tela);
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
                    SoloColision(E.Pieza(PrimitiveType.Cube, "Suelo", pivote.transform, new Vector3(-lado * mitad * 0.5f, -0.2f, 0f),
                                         new Vector3(mitad, 0.4f, largoTramo - 0.06f), tela, conCollider: true));
                    // La mitad de cremallera del kit (tela, bisagra y dientes): la izquierda tal cual, la derecha girada.
                    Kit(ancho > 3f ? "MitadCremalleraAncha" : "MitadCremalleraEstrecha", pivote.transform, Vector3.zero, lado < 0f ? 0f : 180f, tela, local: true);
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
            Kit("Tirador", cursor, Vector3.zero, fuera > 0f ? 0f : 180f, local: true);
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
            SoloColision(E.Pieza(PrimitiveType.Cube, "Cojin", go.transform, Vector3.zero, new Vector3(w, 1.2f, d), tela, conCollider: true));
            // El alfiletero acolchado del kit (se mueve con el cojín).
            Kit(w > 13f ? "Alfiletero14x6" : "Alfiletero12x7", go.transform, Vector3.zero, 0f, tela, local: true);
            // Base de madera (fija) y patas.
            M.Decorado(PrimitiveType.Cube, "Base", decorado, new Vector3((x0 + x1) * 0.5f, arriba - 1.6f, (z0 + z1) * 0.5f), new Vector3(w - 0.4f, 0.8f, d - 0.4f), maderaVeta);
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
            Kit("ArcoTela", arco, new Vector3(0f, arriba, z), 0f, coral);   // 18 m de ancho, postes en x = ±9
            foreach (float lado in new[] { -1f, 1f })
            {
                var poste = new GameObject("Poste (colision)");
                poste.transform.SetParent(arco, false);
                poste.transform.position = new Vector3(lado * (ancho * 0.5f), arriba + 2.3f, z);
                var capsula = poste.AddComponent<CapsuleCollider>();
                capsula.radius = 0.65f;
                capsula.height = 4.6f;
            }
            M.Cartel(arco, texto, new Vector3(0f, arriba + 4.7f, z - 0.3f), 0.95f, new Color(0.14f, 0.2f, 0.42f));
        }

        /// <summary>Flecha cosida en el suelo: un ">" que apunta hacia "giro" (0 = adelante).</summary>
        static void Flecha(Vector3 posicion, float giro, Material material)
        {
            Kit("Flecha", decorado, posicion + Vector3.up * 0.01f, giro, material, 1.3f);
        }

        static void Banderin(Vector3 pie, Material tela)
        {
            Kit("Banderin", decorado, pie, pie.x < 0f ? 180f : 0f, tela);
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
                var hilo = M.Decorado(PrimitiveType.Cylinder, "Hilo", grupo, (a + b) * 0.5f + Vector3.up * 0.62f, new Vector3(0.07f, largo * 0.5f, 0.07f), coral);
                if (hilo != null) hilo.transform.rotation = giro * Quaternion.Euler(90f, 0f, 0f);
                int postes = Mathf.Max(1, Mathf.RoundToInt(largo / 2f));
                for (int i = 0; i <= postes; i++)
                    Kit("PosteBoton", grupo, Vector3.Lerp(a, b, i / (float)postes), 0f, i % 2 == 0 ? coral : mostaza, 0.7f);
            }
        }

        /// <summary>Punto de control: cruza el ancho de la losa en z, con banderines a los lados.</summary>
        static PuntoControlCarrera PuntoControl(int indice, float z, float x0, float x1, float arriba, float separacion = 1.6f)
        {
            var punto = M.ZonaTrigger<PuntoControlCarrera>($"PuntoControl {indice}", mapa, new Vector3((x0 + x1) * 0.5f, arriba + 1.5f, z), new Vector3(x1 - x0, 3f, 1.5f));
            punto.indice = indice;
            punto.separacion = separacion;
            var reaparicion = new GameObject("Reaparicion").transform;
            reaparicion.SetParent(punto.transform, false);
            float xReaparicion = x1 - x0 >= 6f ? Mathf.Clamp(0f, x0 + 3f, x1 - 3f) : (x0 + x1) * 0.5f;
            reaparicion.position = new Vector3(xReaparicion, arriba + 0.2f, z);
            punto.reaparicion = reaparicion;
            Banderin(new Vector3(x0 + 0.4f, arriba, z), coral);
            Banderin(new Vector3(x1 - 0.4f, arriba, z), coral);
            Pegatina("Linea de control", x0 + 0.6f, x1 - 0.6f, z - 0.06f, z + 0.06f, arriba, crema);
            return punto;
        }

        // ---------------------------------------------------------------------
        // La segunda mitad: descansos, botones elásticos, dedales, alfileres, carretes rodantes,
        // botones-tecla y tijeras
        // ---------------------------------------------------------------------

        /// <summary>Descanso entre tramos: losa ancha con parches, guirnaldas y punto de control.</summary>
        static void Descanso(string nombre, float x0, float x1, float z0, float z1, float arriba, int indice,
                             List<PuntoControlCarrera> controles, Material tela)
        {
            Losa(nombre, x0, x1, z0, z1, arriba, tela);
            Pespunte(x0, x1, z0, z1, arriba);
            Parches(x0, x1, z0, z1, arriba, parcheLunares);
            Guirnalda(new Vector3(x0 + 0.6f, arriba, z0 + 0.6f), new Vector3(x0 + 0.6f, arriba, z1 - 0.6f));
            Guirnalda(new Vector3(x1 - 0.6f, arriba, z0 + 0.6f), new Vector3(x1 - 0.6f, arriba, z1 - 0.6f));
            controles.Add(PuntoControl(indice, (z0 + z1) * 0.5f, x0, x1, arriba));
        }

        /// <summary>Botón gigante elástico: al botar en él te lanza ~14 m hacia delante. Va y viene un poco de lado.</summary>
        static ColchonElastico BotonElastico(string nombre, Vector3 pie, float desfase, Material color)
        {
            const float lado = 4.8f, radio = 2.4f;
            var go = new GameObject(nombre);
            go.transform.SetParent(obstaculos, false);
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
            go.AddComponent<Rigidbody>().isKinematic = true;
            var visual = Kit("Boton", go.transform, new Vector3(0f, 0.8f - radio * 0.22f, 0f), 0f, color, radio, local: true);
            var c = go.AddComponent<ColchonElastico>();
            c.visual = visual.transform;
            c.vaiven = new Vector3(1.6f, 0f, 0f);
            c.periodoVaiven = 3.4f;
            c.desfaseVaiven = desfase;
            return c;
        }

        /// <summary>Dedal boxeador: sale de lado sobre la pasarela y empuja hacia el vacío ("lado": de dónde sale, +1 derecha).</summary>
        static void DedalBoxeador(string nombre, float xPasarela, float arriba, float z, float lado, float desfase, Material color)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(obstaculos, false);
            go.transform.position = new Vector3(xPasarela + lado * 2.6f, arriba + 0.75f, z);
            Vector3 haciaDentro = new Vector3(-lado, 0f, 0f);
            var caja = go.AddComponent<BoxCollider>();
            caja.size = new Vector3(1.6f, 1.4f, 1.4f);
            go.AddComponent<Rigidbody>().isKinematic = true;
            var cuerpo = E.Pieza(PrimitiveType.Cylinder, "Dedal", go.transform, Vector3.zero, new Vector3(1.3f, 0.8f, 1.3f), dedal);
            cuerpo.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var punta = E.Pieza(PrimitiveType.Sphere, "Punta", go.transform, haciaDentro * 0.8f, new Vector3(0.9f, 1.25f, 1.25f), color);
            E.Pieza(PrimitiveType.Cylinder, "Piston", go.transform, -haciaDentro * 1.2f, new Vector3(0.4f, 0.6f, 0.4f), dorado)
             .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            // El soporte, fijo, al lado de la pasarela.
            E.Bloque("Soporte dedal", suelos, new Vector3(xPasarela + lado * 3.8f, arriba - 0.3f, z), new Vector3(1.6f, 0.6f, 1.6f), maderaVeta);
            var empujador = go.AddComponent<Empujador>();
            empujador.recorrido = haciaDentro * 3.2f;
            empujador.periodo = 3.4f;
            empujador.desfase = desfase;
            empujador.cuerpoVisible = punta.GetComponent<Renderer>();
            empujador.nombreTrampa = nombre;
            empujador.enfriamiento = 4f;
            empujador.radioEfecto = 3.5f;
            empujador.alturaMarcador = 2.5f;
        }

        /// <summary>Botón-tecla: sube y baja a su ritmo (sin columna: flota sobre un hilo).</summary>
        static PetaloPiano BotonTecla(string nombre, Vector2 centro, float arriba, float radio, float desfase, Material color)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(obstaculos, false);
            go.transform.position = new Vector3(centro.x, arriba - 0.2f, centro.y);
            go.AddComponent<Rigidbody>().isKinematic = true;
            SoloColision(A.Disco(go.transform, "Cara", new Vector3(centro.x, 0f, centro.y), arriba - 0.2f, radio, 0.44f, color));
            Kit("Boton", go.transform, new Vector3(centro.x, arriba - 0.2f - radio * 0.22f, centro.y), 0f, color, radio);
            E.Pieza(PrimitiveType.Cylinder, "Hilo", go.transform, new Vector3(0f, -6f, 0f), new Vector3(0.08f, 6f, 0.08f), crema);
            var p = go.AddComponent<PetaloPiano>();
            p.amplitud = 0.9f;
            p.periodo = 2.8f;
            p.desfase = desfase;
            return p;
        }

        /// <summary>El acerico gigante con los alfileres que salen por oleadas.</summary>
        static CampoAlfileres CampoDeAlfileres(float x0, float x1, float z0, float z1, float arriba)
        {
            var acerico = E.CrearMaterial("CosturaAcerico", new Color(0.92f, 0.36f, 0.34f), 0.15f);
            Losa("Acerico", x0, x1, z0, z1, arriba, acerico);
            Pespunte(x0, x1, z0, z1, arriba);
            var go = new GameObject("Campo de alfileres");
            go.transform.SetParent(obstaculos, false);
            go.transform.position = new Vector3((x0 + x1) * 0.5f, arriba, (z0 + z1) * 0.5f);
            Material[] cabezas = { mostaza, turquesa, azul, crema };
            var alfileres = new List<Rigidbody>();
            var fases = new List<float>();
            int fila = 0;
            for (float z = z0 + 3f; z < z1 - 1.5f; z += 2.5f, fila++)
            {
                int col = 0;
                for (float x = x0 + 1f; x < x1 - 0.5f; x += 2f, col++)
                {
                    float xx = x + (fila % 2 == 0 ? 0f : 1f);
                    if (xx > x1 - 0.8f) continue;
                    var pin = new GameObject($"Alfiler {fila}-{col}");
                    pin.transform.SetParent(go.transform, false);
                    pin.transform.position = new Vector3(xx, arriba - 1.9f, z);
                    var rb = pin.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    var capsula = pin.AddComponent<CapsuleCollider>();
                    capsula.radius = 0.3f;
                    capsula.height = 1.8f;
                    capsula.center = new Vector3(0f, -0.6f, 0f);
                    E.Pieza(PrimitiveType.Cylinder, "Aguja", pin.transform, new Vector3(0f, -0.75f, 0f), new Vector3(0.16f, 0.75f, 0.16f), dedal);
                    E.Pieza(PrimitiveType.Sphere, "Cabeza", pin.transform, Vector3.zero, Vector3.one * 0.6f, cabezas[(fila + col) % cabezas.Length]);
                    foreach (var c in pin.GetComponentsInChildren<Collider>()) if (c != capsula) Object.DestroyImmediate(c);
                    // El agujero del alfiler en el acerico.
                    Pegatina("Agujero", xx - 0.3f, xx + 0.3f, z - 0.3f, z + 0.3f, arriba, negro);
                    alfileres.Add(rb);
                    // Oleadas: en damero, y cada fila un poco después que la anterior.
                    fases.Add(Mathf.Repeat(fila * 0.085f + ((fila + col) % 2) * 0.5f + col * 0.02f, 1f));
                }
            }
            var campo = go.AddComponent<CampoAlfileres>();
            campo.alfileres = alfileres.ToArray();
            campo.fases = fases.ToArray();
            campo.alturaFuera = 1.9f + 1.5f;      // la cabeza, 1,5 m por encima del acerico
            campo.alturaAsomado = 1.9f - 0.25f;   // asoma la cabeza
            campo.alturaDentro = 0f;              // escondido
            return campo;
        }

        /// <summary>Carrete tumbado que rueda (eje a lo largo de la carrera): se corre por encima. "arriba" = lo alto del carrete.</summary>
        static CarreteRodante CarreteRodando(string nombre, float x, float z0, float z1, float arriba, float arrastre, float desfase, Material hilo)
        {
            const float radio = 1.8f;
            float largo = z1 - z0;
            var go = new GameObject(nombre);
            go.transform.SetParent(obstaculos, false);
            go.transform.position = new Vector3(x, arriba - radio, (z0 + z1) * 0.5f);
            go.AddComponent<Rigidbody>().isKinematic = true;
            var giro = new GameObject("Giro").transform;
            giro.SetParent(go.transform, false);
            var tambor = E.Pieza(PrimitiveType.Cylinder, "Hilo", giro, Vector3.zero, new Vector3(radio * 2f, (largo - 0.8f) * 0.5f, radio * 2f), hilo);
            tambor.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            foreach (float k in new[] { -1f, 1f })
            {
                var tapa = E.Pieza(PrimitiveType.Cylinder, "Tapa", giro, new Vector3(0f, 0f, k * (largo * 0.5f - 0.2f)), new Vector3(radio * 2.15f, 0.2f, radio * 2.15f), maderaVeta);
                tapa.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            // La colisión: un cilindro de verdad (una cápsula tendría las puntas redondas), que no gira.
            var colision = new GameObject("Colision");
            colision.transform.SetParent(go.transform, false);
            colision.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            colision.transform.localScale = new Vector3(radio * 2f, largo * 0.5f, radio * 2f);
            var mallaCilindro = colision.AddComponent<MeshCollider>();
            mallaCilindro.sharedMesh = tambor.GetComponent<MeshFilter>().sharedMesh;
            mallaCilindro.convex = true;
            // Una etiqueta a lo largo: así se ve hacia dónde gira.
            E.Pieza(PrimitiveType.Cube, "Etiqueta", giro, new Vector3(0f, radio, 0f), new Vector3(0.7f, 0.06f, largo - 1.2f), crema);
            foreach (var c in giro.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            // Soportes de madera en los extremos (fijos).
            foreach (float k in new[] { -1f, 1f })
                M.Decorado(PrimitiveType.Cube, "Soporte", decorado, new Vector3(x, (arriba - radio + AlturaMesa) * 0.5f, (z0 + z1) * 0.5f + k * (largo * 0.5f - 0.2f)),
                           new Vector3(0.8f, arriba - radio - AlturaMesa, 0.4f), maderaVeta);
            var carrete = go.AddComponent<CarreteRodante>();
            carrete.visual = giro;
            carrete.radio = radio;
            carrete.largo = largo;
            carrete.arrastre = arrastre;
            carrete.periodoCambio = 3.6f;
            carrete.desfase = desfase;
            return carrete;
        }

        /// <summary>Tijeras gigantes de pie sobre el puente, con el eje a 6,2 m de alto en el centro.</summary>
        static TijerasGigantes Tijeras(string nombre, float z, float arriba, float pausaAbierta, float pausaCerrada, float desfase, Material mango)
        {
            const float altoEje = 6.2f, largoHoja = 7.6f;
            var go = new GameObject(nombre);
            go.transform.SetParent(obstaculos, false);
            go.transform.position = new Vector3(0f, arriba + altoEje, z);
            var hojas = new Rigidbody[2];
            for (int k = 0; k < 2; k++)
            {
                var pivote = new GameObject(k == 0 ? "Hoja izquierda" : "Hoja derecha");
                pivote.transform.SetParent(go.transform, false);
                pivote.transform.localPosition = new Vector3(0f, 0f, k == 0 ? -0.5f : 0.5f);
                var rb = pivote.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                pivote.AddComponent<HojaTijera>();
                var caja = pivote.AddComponent<BoxCollider>();
                caja.size = new Vector3(0.7f, largoHoja, 1f);
                caja.center = new Vector3(0f, -largoHoja * 0.5f, 0f);
                E.Pieza(PrimitiveType.Cube, "Hoja", pivote.transform, new Vector3(0f, -largoHoja * 0.5f, 0f), new Vector3(0.6f, largoHoja, 0.9f), dedal);
                E.Pieza(PrimitiveType.Cube, "Filo", pivote.transform, new Vector3(k == 0 ? 0.32f : -0.32f, -largoHoja * 0.5f, 0f), new Vector3(0.06f, largoHoja - 0.3f, 0.92f), crema);
                // El mango (arriba del eje): un aro de color.
                var mangoGo = E.Pieza(PrimitiveType.Cylinder, "Mango", pivote.transform, new Vector3(0f, 1.3f, 0f), new Vector3(1.6f, 0.12f, 1.6f), mango);
                mangoGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                hojas[k] = rb;
            }
            var tornillo = E.Pieza(PrimitiveType.Cylinder, "Tornillo", go.transform, Vector3.zero, new Vector3(0.6f, 0.4f, 0.6f), dorado);
            tornillo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            // Un poste a cada lado sujeta el eje por arriba (decorado).
            foreach (float lado in new[] { -1f, 1f })
                M.Decorado(PrimitiveType.Cylinder, "Poste tijeras", decorado, new Vector3(lado * 6f, arriba + altoEje * 0.5f - 2f, z), new Vector3(0.35f, altoEje * 0.5f + 2f, 0.35f), maderaVeta);
            M.Decorado(PrimitiveType.Cylinder, "Travesano", decorado, new Vector3(0f, arriba + altoEje + 0.2f, z), new Vector3(0.3f, 6f, 0.3f), maderaVeta)
             .transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            var t = go.AddComponent<TijerasGigantes>();
            t.hojaIzq = hojas[0];
            t.hojaDer = hojas[1];
            t.anguloAbierto = 36f;
            t.pausaAbierta = pausaAbierta;
            t.pausaCerrada = pausaCerrada;
            t.desfase = desfase;
            t.tiempoCerrar = 0.32f;
            return t;
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
        // Piezas del kit de Blender (Arte/Blender/crear_kit_costura.py)
        // ---------------------------------------------------------------------

        static GameObject Kit(string pieza, Transform padre, Vector3 posicion, float giro = 0f, Material equipo = null,
                              float escala = 1f, bool local = false) =>
            KitObjetos.Poner(pieza, padre, posicion, giro, equipo, escala, local, KitObjetos.KitCostura);

        /// <summary>Deja solo la colisión de una pieza hecha con primitivas (lo que se ve es el modelo del kit).</summary>
        static GameObject SoloColision(GameObject go)
        {
            if (go != null && go.TryGetComponent(out MeshRenderer r)) Object.DestroyImmediate(r);
            return go;
        }

        // ---------------------------------------------------------------------
        // Decorado: el dedal, la mesa y el taller (apagados, para no distraer)
        // ---------------------------------------------------------------------

        static void Dedal(Vector3 base_)
        {
            var g = M.Grupo(decorado, "Dedal");
            Kit("Dedal", g, base_);
            M.Decorado(PrimitiveType.Cylinder, "Columna", g, new Vector3(base_.x, (base_.y + AlturaMesa) * 0.5f, base_.z),
                       new Vector3(2.5f, (base_.y - AlturaMesa) * 0.5f, 2.5f), madera);
        }

        static void Fondo()
        {
            var f = M.Grupo(decorado, "Taller");
            // La mesa de costura, abajo, con su veta.
            M.Decorado(PrimitiveType.Cube, "Mesa", f, new Vector3(0f, AlturaMesa - 0.5f, 165f), new Vector3(130f, 1f, 440f), mesaVeta);

            // El público: bichos en gradas encima de carretes gigantes, a los lados del recorrido.
            Grada(f, new Vector3(-20f, 0f, 8f), new Vector3(0f, 0f, 10f), fondoCoral);
            Grada(f, new Vector3(26f, 0f, 46f), new Vector3(8f, 0f, 46f), fondoAzul);
            Grada(f, new Vector3(-21f, 0f, 96f), new Vector3(-4f, 0f, 96f), fondoMostaza);
            Grada(f, new Vector3(-21f, 3.8f, 141f), new Vector3(0f, 3.8f, 141f), fondoTurquesa);
            Grada(f, new Vector3(22f, 3.8f, 217f), new Vector3(0f, 3.8f, 217f), fondoCoral);
            Grada(f, new Vector3(-22f, 3.8f, 266f), new Vector3(-6f, 3.8f, 266f), fondoAzul);
            Grada(f, new Vector3(20f, 3.8f, 308f), new Vector3(0f, 3.8f, 308f), fondoMostaza);
            Grada(f, new Vector3(-21f, 3.8f, 329f), new Vector3(0f, 3.8f, 329f), fondoTurquesa);
            // Focos del plató (esto sigue siendo un concurso de la tele).
            foreach (var (pie, mirar) in new[]
                     {
                         (new Vector3(-15f, AlturaMesa, -6f), new Vector3(0f, 0f, 10f)), (new Vector3(15f, AlturaMesa, -6f), new Vector3(0f, 0f, 10f)),
                         (new Vector3(-17f, AlturaMesa, 343f), new Vector3(0f, 0f, 328f)), (new Vector3(17f, AlturaMesa, 343f), new Vector3(0f, 0f, 328f)),
                         (new Vector3(-24f, AlturaMesa, 210f), new Vector3(0f, 0f, 217f)), (new Vector3(24f, AlturaMesa, 290f), new Vector3(0f, 0f, 300f)),
                     })
                KitObjetos.Poner("TorreFocos", f, pie, KitObjetos.GiroMirando(pie, mirar), null, 2.4f, false, KitObjetos.KitPlato);

            // El taller: máquina de coser, acerico, rollos de tela, flexo, macetas, botones…
            Kit("MaquinaCoser", f, new Vector3(40f, AlturaMesa, 118f), -90f, null, 3.4f);
            Kit("Acerico", f, new Vector3(-34f, AlturaMesa, 52f), 20f, null, 4.5f);
            Kit("RolloTela", f, new Vector3(31f, AlturaMesa, 16f), 15f, fondoTurquesa, 3f);
            Kit("RolloTela", f, new Vector3(33f, AlturaMesa, 22f), 5f, fondoCoral, 3f);
            Kit("RolloTela", f, new Vector3(32f, AlturaMesa + 4.6f, 19f), 10f, fondoMostaza, 3f);
            Kit("Lampara", f, new Vector3(-37f, AlturaMesa, 84f), 0f, null, 5f);
            Kit("Maceta", f, new Vector3(42f, AlturaMesa, 2f), 0f, null, 4f);
            Kit("Maceta", f, new Vector3(-40f, AlturaMesa, 170f), 40f, null, 5f);
            CarreteFondo(f, new Vector3(30f, AlturaMesa, 82f), 3.5f, 7f, fondoCoral);
            CarreteFondo(f, new Vector3(-30f, AlturaMesa, 128f), 4.5f, 9f, fondoAzul);
            CarreteFondo(f, new Vector3(28f, AlturaMesa, 160f), 3f, 6f, fondoMostaza);
            BotonFondo(f, new Vector3(20f, AlturaMesa, 4f), 5f, fondoMostaza);
            BotonFondo(f, new Vector3(-24f, AlturaMesa, 70f), 4f, fondoTurquesa);
            BotonFondo(f, new Vector3(24f, AlturaMesa, 102f), 4.5f, fondoCoral);
            Kit("Tijeras", f, new Vector3(-26f, AlturaMesa, 150f), 30f, fondoCoral, 3f);
            Kit("CintaRollo", f, new Vector3(-15f, AlturaMesa, -14f), -20f, null, 3f);
            M.Decorado(PrimitiveType.Cube, "Cinta metrica", f, new Vector3(-15f, AlturaMesa + 0.05f, 70f), new Vector3(2.5f, 0.1f, 170f), fondoMostaza);
            // La segunda mitad: otra máquina de coser, más rollos, carretes y botones.
            Kit("MaquinaCoser", f, new Vector3(-42f, AlturaMesa, 300f), 90f, null, 3.4f);
            Kit("Acerico", f, new Vector3(36f, AlturaMesa, 240f), -30f, null, 4f);
            Kit("RolloTela", f, new Vector3(-33f, AlturaMesa, 205f), 40f, fondoAzul, 3f);
            Kit("RolloTela", f, new Vector3(35f, AlturaMesa, 330f), -10f, fondoTurquesa, 3f);
            CarreteFondo(f, new Vector3(30f, AlturaMesa, 270f), 3.5f, 8f, fondoTurquesa);
            CarreteFondo(f, new Vector3(-30f, AlturaMesa, 360f), 4f, 7f, fondoCoral);
            BotonFondo(f, new Vector3(-26f, AlturaMesa, 250f), 4f, fondoMostaza);
            Kit("Tijeras", f, new Vector3(28f, AlturaMesa, 352f), -40f, fondoAzul, 3f);

            // Pared del taller con papel pintado y una estantería con rollos de tela.
            M.Decorado(PrimitiveType.Cube, "Pared", f, new Vector3(0f, AlturaMesa + 18f, 395f), new Vector3(160f, 40f, 2f), papelPintado);
            Material[] rollos = { fondoCoral, fondoTurquesa, fondoMostaza, fondoAzul };
            for (int i = 0; i < 3; i++)
            {
                float y = AlturaMesa + 8f + i * 7f;
                M.Decorado(PrimitiveType.Cube, "Balda", f, new Vector3(0f, y, 392f), new Vector3(70f, 0.6f, 4f), mesaVeta);
                for (int k = 0; k < 6; k++)
                    Kit("RolloTela", f, new Vector3(-28f + k * 11f + (i % 2) * 4f, y + 0.3f, 392f), 0f, rollos[(k + i) % rollos.Length], 2.2f);
            }
        }

        /// <summary>Grada del plató con público de bichos, encima de un carrete gigante que llega hasta ella.</summary>
        static void Grada(Transform padre, Vector3 sitio, Vector3 mirar, Material hilo)
        {
            float alto = sitio.y - AlturaMesa;
            Kit("CarreteDePie", padre, new Vector3(sitio.x, AlturaMesa, sitio.z), 0f, hilo, alto / 2f);

            var grupo = M.Grupo(padre, "Gradas");
            grupo.gameObject.AddComponent<PublicoAnimado>();
            var grada = KitObjetos.Poner("Grada", grupo, sitio, KitObjetos.GiroMirando(sitio, mirar), null, 1f, false, KitObjetos.KitPlato);

            Color[] colores =
            {
                new Color(1f, 0.45f, 0.45f), new Color(0.4f, 0.65f, 1f), new Color(1f, 0.8f, 0.3f),
                new Color(0.5f, 0.9f, 0.5f), new Color(0.85f, 0.55f, 1f), new Color(1f, 0.6f, 0.85f),
            };
            var materiales = new Material[colores.Length];
            for (int i = 0; i < colores.Length; i++) materiales[i] = E.CrearMaterial($"Publico{i}", colores[i], 0.3f);
            var azar = new System.Random(Mathf.RoundToInt(sitio.x * 31f + sitio.z * 17f));
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

        /// <summary>Guirnalda de banderitas entre dos palos (con su caída).</summary>
        static void Guirnalda(Vector3 a, Vector3 b, float caida = 0.5f)
        {
            var g = M.Grupo(decorado, "Guirnalda");
            Kit("Banderin", g, a, 0f, coral);
            Kit("Banderin", g, b, 0f, coral);
            Vector3 alto = Vector3.up * 1.82f;
            a += alto;
            b += alto;
            Vector3 dir = b - a;
            float giro = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - 90f; // la tela en el plano del hilo
            Material[] colores = { coral, mostaza, turquesa, azul };
            int n = Mathf.Max(3, Mathf.RoundToInt(dir.magnitude / 0.8f));
            Vector3 anterior = a;
            for (int i = 1; i <= n; i++)
            {
                float k = i / (float)n;
                Vector3 p = Vector3.Lerp(a, b, k) + Vector3.down * caida * 4f * k * (1f - k);
                var hilo = M.Decorado(PrimitiveType.Cylinder, "Hilo", g, (anterior + p) * 0.5f, new Vector3(0.035f, Vector3.Distance(anterior, p) * 0.5f, 0.035f), crema);
                if (hilo != null) hilo.transform.rotation = Quaternion.FromToRotation(Vector3.up, p - anterior);
                if (i < n) Kit("Banderita", g, p, giro, colores[i % colores.Length], 0.9f);
                anterior = p;
            }
        }

        static void CarreteFondo(Transform padre, Vector3 pie, float radio, float alto, Material hilo)
        {
            Kit("CarreteDePie", padre, pie, radio * 40f, hilo, alto / 2f);
        }

        static void BotonFondo(Transform padre, Vector3 pie, float radio, Material material)
        {
            Kit("Boton", padre, pie, radio * 25f, material, radio);
        }
    }
}
