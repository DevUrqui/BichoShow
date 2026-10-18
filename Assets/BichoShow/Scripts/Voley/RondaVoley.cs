using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Prueba "Voley atados": 2 contra 2, cada pareja unida por una goma elástica.
    /// El equipo 0 (rojo) juega en el campo de la izquierda (x negativa) y el 1 (azul) en el
    /// de la derecha. Punto cuando la pelota toca el suelo; partido a tiempo, y si hay empate,
    /// punto de oro. A partir de unos puntos salen pelotas especiales.
    /// </summary>
    public class RondaVoley : RondaBase
    {
        [Header("Equipos")]
        public string[] nombres = { "Rojo", "Azul" };
        public Color[] colores = { new Color(1f, 0.38f, 0.35f), new Color(0.3f, 0.6f, 1f) };

        [Header("Pista")]
        [Tooltip("Centro de cada campo (0 = izquierda, 1 = derecha).")]
        public Transform[] centrosCampo = new Transform[2];
        [Tooltip("Tamaño de cada campo: X = fondo (desde la red), Y = ancho (a lo largo de la red).")]
        public Vector2 tamanoCampo = new Vector2(10f, 12f);
        [Tooltip("Pared invisible sobre la red: frena a los bichos pero no a la pelota.")]
        public Collider barreraRed;
        [Tooltip("Altura del borde de arriba de la red (m). Los golpes se calculan para pasar por encima.")]
        public float alturaRed = 2.4f;
        public float alturaSaque = 5f;
        [Tooltip("Espera entre un punto y el siguiente saque (s).")]
        public float esperaSaque = 1.8f;
        [Tooltip("Un bicho por debajo de esta altura se ha caído de la plataforma y reaparece (aunque la goma lo tenga colgando).")]
        public float alturaRescate = -1.2f;

        [Header("Goma elástica")]
        [Tooltip("Largo de la goma sin estirar (m).")]
        public float largoGoma = 3f;
        public float fuerzaGoma = 60f;
        public float amortiguacionGoma = 3f;

        [Header("Pelotas especiales")]
        public bool pelotasEspeciales = true;
        [Tooltip("Puntos jugados antes de que empiecen a salir pelotas especiales.")]
        public int puntosAntesDeEspeciales = 2;
        [Range(0f, 1f)] public float probabilidadEspecial = 0.5f;

        [Header("Sabotaje")]
        [Tooltip("Puntos para el saboteador si su trampa provoca el punto.")]
        public int puntosPuntoProvocado = 25;

        [Header("Aspecto")]
        public Material materialPelota;
        public Material materialSombra;
        public Material materialCharco;
        public ParticleSystem confeti;

        public PelotaVoley Pelota { get; private set; }

        readonly int[] puntos = new int[2];
        readonly List<GomaElastica> gomas = new List<GomaElastica>();
        int puntosJugados;
        int equipoSaque;
        float siguienteSaque = -1f;
        bool puntoDeOro;

        public override string Titulo => "VOLEY ATADOS";
        protected override string Descripcion => "Que la pelota caiga en el campo rival… atados a tu pareja con una goma";
        protected override string Controles => "Stick mover · A saltar · X golpear (en el aire: ¡remate!) · Tocarla con el cuerpo la levanta";
        public override string NombreEquipo(int equipo) => nombres[equipo];
        public override Color ColorEquipo(int equipo) => colores[equipo];
        public override int PuntosEquipo(int equipo) => puntos[equipo];

        public override IEnumerable<Vector3> ObjetivosSabotaje()
        {
            if (Pelota != null) yield return Pelota.transform.position;
        }

        // ---------------------------------------------------------------------
        // Pista
        // ---------------------------------------------------------------------

        public Vector3 CentroCampo(int equipo) =>
            centrosCampo != null && equipo >= 0 && equipo < centrosCampo.Length && centrosCampo[equipo] != null
                ? centrosCampo[equipo].position
                : new Vector3(equipo == 0 ? -5f : 5f, 0f, 0f);

        /// <summary>Dirección (en el suelo) desde el campo de este equipo hacia el rival.</summary>
        public Vector3 HaciaRival(int equipo)
        {
            Vector3 d = CentroCampo(1 - equipo) - CentroCampo(equipo);
            d.y = 0f;
            return d.sqrMagnitude > 0.01f ? d.normalized : Vector3.right;
        }

        /// <summary>A qué equipo pertenece este punto del suelo (por el lado de la red).</summary>
        public int CampoDe(Vector3 punto)
        {
            Vector3 red = (CentroCampo(0) + CentroCampo(1)) * 0.5f;
            return Vector3.Dot(punto - red, HaciaRival(0)) < 0f ? 0 : 1;
        }

        /// <summary>
        /// Dónde cae un golpe del equipo dado: siempre en el campo rival. Apuntar hacia delante
        /// manda la pelota al fondo; hacia atrás, cerca de la red; a los lados, a los lados.
        /// </summary>
        public Vector3 PuntoDeGolpe(int equipo, Vector3 direccion, bool remate)
        {
            Vector3 adelante = HaciaRival(equipo);
            Vector3 lateral = Vector3.Cross(Vector3.up, adelante);
            direccion.y = 0f;
            if (direccion.sqrMagnitude < 0.01f) direccion = adelante;
            direccion.Normalize();

            float fondo = Vector3.Dot(direccion, adelante);          // -1 cerca de la red, 1 al fondo
            float lado = Vector3.Dot(direccion, lateral);
            float profundidad = Mathf.Lerp(0.25f, 0.85f, (fondo + 1f) * 0.5f) * tamanoCampo.x;
            if (remate) profundidad *= 0.8f;

            Vector3 red = (CentroCampo(0) + CentroCampo(1)) * 0.5f;
            Vector3 objetivo = red + adelante * profundidad + lateral * lado * tamanoCampo.y * 0.38f;
            objetivo += Random.insideUnitSphere * 0.8f;
            objetivo.y = 0.3f;
            return objetivo;
        }

        // ---------------------------------------------------------------------
        // Ciclo de la ronda
        // ---------------------------------------------------------------------

        protected override void AlPrepararRonda()
        {
            puntos[0] = puntos[1] = 0;
            puntosJugados = 0;
            puntoDeOro = false;
            siguienteSaque = -1f;
            QuitarPelota();
            QuitarGomas();
        }

        protected override void CrearBot(int indice)
        {
            var bot = gestor.CrearBot<BotVoley>(indice);
            if (bot != null) bot.Configurar(this, habilidadBots);
        }

        protected override void AlEmpezarRonda()
        {
            CrearGomas();
            equipoSaque = Random.value < 0.5f ? 0 : 1;
            siguienteSaque = Time.time + 0.5f;
            Mensaje("¡A JUGAR!", Color.white, 1.5f);
        }

        protected override void ActualizarJugando()
        {
            if (Pelota == null && siguienteSaque >= 0f && Time.time >= siguienteSaque) Sacar();
        }

        protected override void Update()
        {
            base.Update();
            RescatarCaidos();
        }

        /// <summary>
        /// Con la goma, un bicho que se cae por el borde puede quedarse colgando sin llegar al
        /// agua (y su pareja, arrastrada hacia el borde sin poder volver). Se le devuelve a su sitio.
        /// </summary>
        void RescatarCaidos()
        {
            var bichos = ControladorBicho.Todos;
            for (int i = bichos.Count - 1; i >= 0; i--)
                if (bichos[i].transform.position.y < alturaRescate) bichos[i].Reaparecer();
        }

        protected override bool PuedeTerminar()
        {
            if (puntos[0] != puntos[1]) return true;
            if (!puntoDeOro)
            {
                puntoDeOro = true;
                Mensaje("¡EMPATE! PUNTO DE ORO: quien marque, gana", Color.yellow, 3f);
            }
            return false;
        }

        protected override string TextoReloj => puntoDeOro ? "PUNTO DE ORO" : base.TextoReloj;

        protected override void AlTerminarRonda()
        {
            siguienteSaque = -1f;
            QuitarPelota();
            QuitarGomas(); // libres para pasearse por la pista
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            QuitarPelota();
            QuitarGomas();
        }

        // ---------------------------------------------------------------------
        // Pelota y puntos
        // ---------------------------------------------------------------------

        void Sacar()
        {
            siguienteSaque = -1f;

            var tipo = PelotaVoley.Tipo.Normal;
            if (pelotasEspeciales && puntosJugados >= puntosAntesDeEspeciales && Random.value < probabilidadEspecial)
                tipo = (PelotaVoley.Tipo)Random.Range(1, 4);

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Pelota";
            go.transform.position = CentroCampo(equipoSaque) + Vector3.up * alturaSaque;
            go.transform.localScale = Vector3.one * 0.7f;
            go.AddComponent<Rigidbody>();
            Pelota = go.AddComponent<PelotaVoley>();
            Pelota.Configurar(this, tipo, materialPelota != null ? new Material(materialPelota) : null, materialSombra, materialCharco);

            if (barreraRed != null) Physics.IgnoreCollision(go.GetComponent<Collider>(), barreraRed);

            string especial = PelotaVoley.NombreTipo(tipo);
            Mensaje($"Saca el equipo {nombres[equipoSaque]}  {especial}", colores[equipoSaque], 2f);
        }

        /// <summary>La pelota ha tocado un suelo de la pista.</summary>
        public void PelotaEnSuelo(PelotaVoley pelota, ZonaPista zona)
        {
            if (EstadoActual != Estado.Jugando || pelota != Pelota || pelota.PuntoResuelto) return;

            int ganador;
            string como;
            if (zona.equipo >= 0)
            {
                ganador = 1 - zona.equipo;
                como = "¡en su campo!";
            }
            else
            {
                // Fuera: pierde quien la tocó último (si nadie, el del campo por donde salió).
                int culpable = pelota.UltimoEquipo >= 0 ? pelota.UltimoEquipo : CampoDe(pelota.transform.position);
                ganador = 1 - culpable;
                como = "¡fuera!";
            }
            DarPunto(pelota, ganador, como);
        }

        /// <summary>La pelota se ha caído de la plataforma.</summary>
        public void PelotaPerdida(PelotaVoley pelota)
        {
            if (EstadoActual != Estado.Jugando || pelota != Pelota || pelota.PuntoResuelto) return;
            int culpable = pelota.UltimoEquipo >= 0 ? pelota.UltimoEquipo : CampoDe(pelota.transform.position);
            DarPunto(pelota, 1 - culpable, "¡al agua!");
        }

        void DarPunto(PelotaVoley pelota, int ganador, string como)
        {
            pelota.PuntoResuelto = true;
            puntos[ganador]++;
            puntosJugados++;
            var culpable = Responsable(pelota.transform.position);
            string autoria = "";
            if (culpable != null)
            {
                culpable.SumarPuntos(puntosPuntoProvocado, "¡punto provocado!");
                autoria = $"  (obra de {culpable.Etiqueta})";
            }
            Mensaje($"¡Punto para el equipo {nombres[ganador]}! {como}{autoria}", colores[ganador], 2.5f);
            Sonidos.Reproducir(Sonido.Silbato, pelota.transform.position);
            Estadisticas.Evento("punto", null, $"{nombres[ganador]}: {como}{autoria}");
            Sonidos.Reproducir(Sonido.Aplausos, pelota.transform.position, 0.8f);
            Efectos.Sacudir(0.15f);

            if (confeti != null)
            {
                confeti.transform.position = CentroCampo(ganador) + Vector3.up * 0.3f;
                var main = confeti.main;
                main.startColor = new ParticleSystem.MinMaxGradient(colores[ganador], new Color(1f, 0.92f, 0.3f));
                confeti.Play();
            }

            // La pelota se queda un momento rebotando y desaparece. Saca quien perdió el punto.
            Destroy(pelota.gameObject, 0.9f);
            Pelota = null;
            equipoSaque = 1 - ganador;

            if (puntoDeOro) TerminarRonda();
            else siguienteSaque = Time.time + esperaSaque;
        }

        void QuitarPelota()
        {
            if (Pelota != null) Destroy(Pelota.gameObject);
            Pelota = null;
        }

        // ---------------------------------------------------------------------
        // Gomas: jugadores 1-3 y 2-4
        // ---------------------------------------------------------------------

        void CrearGomas()
        {
            QuitarGomas();
            for (int equipo = 0; equipo < 2; equipo++)
            {
                ControladorBicho a = null, b = null;
                foreach (var bicho in ControladorBicho.Todos)
                {
                    if (bicho.IndiceJugador == equipo) a = bicho;
                    else if (bicho.IndiceJugador == equipo + 2) b = bicho;
                }
                if (a != null && b != null)
                    gomas.Add(GomaElastica.Crear(a, b, colores[equipo], largoGoma, fuerzaGoma, amortiguacionGoma));
            }
        }

        void QuitarGomas()
        {
            foreach (var g in gomas)
                if (g != null) Destroy(g.gameObject);
            gomas.Clear();
        }
    }
}
