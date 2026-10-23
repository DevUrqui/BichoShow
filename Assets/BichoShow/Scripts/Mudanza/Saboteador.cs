using UnityEngine;
using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// Jugador que no corre: activa las trampas del mapa para fastidiar a los equipos.
    ///  - Humano: con el stick salta de trampa en trampa (hacia donde apunta) y con A o el
    ///    gatillo la activa.
    ///  - Máquina (sin PlayerInput): activa la trampa lista que tenga más víctimas cerca.
    /// Un rombo de su color flota sobre la trampa elegida: los demás ven lo que va a hacer.
    ///
    /// Villano (Villano = true): en las pruebas con un "malo" (el robot del Cortacésped), no hay
    /// trampas: la prueba lee su mando (StickCrudo, StickMundo, PulsoAccion) y le suma los puntos.
    /// </summary>
    public class Saboteador : MonoBehaviour
    {
        public int Puntos { get; private set; }
        public string Etiqueta { get; set; } = "Saboteador";
        public Color ColorSaboteador { get; private set; } = new Color(0.75f, 0.4f, 1f);
        /// <summary>La máquina elige (también si es un jugador de fuera al que no le llega la conexión).</summary>
        public bool EsMaquina => entrada == null && (remota == null || !remota.Activo);
        /// <summary>Número del jugador que sabotea (-1 si es la máquina).</summary>
        public int IndiceJugador { get; private set; } = -1;
        public TrampaSaboteable Seleccionada => seleccionada;
        /// <summary>Hace de villano de la prueba (no usa trampas).</summary>
        public bool Villano { get; set; }
        /// <summary>Lo que puede hacer ahora el villano (lo pone la prueba; sale en su panel).</summary>
        public string AyudaVillano { get; set; } = "";

        /// <summary>Su stick tal cual (cero si es la máquina).</summary>
        public Vector2 StickCrudo => EsMaquina ? Vector2.zero : remota != null ? remota.MoverPropio : accionMover != null ? accionMover.ReadValue<Vector2>() : Vector2.zero;
        /// <summary>Hacia dónde apunta su stick en el suelo (cero si está suelto).</summary>
        public Vector3 StickMundo
        {
            get
            {
                Vector2 s = StickCrudo;
                return s.magnitude < 0.35f ? Vector3.zero : StickAMundo(s);
            }
        }
        /// <summary>¿Ha pulsado A (o agarrar) en este frame?</summary>
        public bool PulsoAccion => !EsMaquina &&
            ((accionSaltar != null && accionSaltar.WasPressedThisFrame()) ||
             (accionAgarrar != null && accionAgarrar.WasPressedThisFrame()) ||
             (remota != null && (remota.PulsadoEsteFrame(EntradaRemota.Boton.Saltar) || remota.PulsadoEsteFrame(EntradaRemota.Boton.Agarrar))));
        /// <summary>¿Ha pulsado X (lanzarse) en este frame?</summary>
        public bool PulsoCambiar => !EsMaquina &&
            ((accionLanzarse != null && accionLanzarse.WasPressedThisFrame()) ||
             (remota != null && remota.PulsadoEsteFrame(EntradaRemota.Boton.Lanzarse)));

        RondaBase ronda;
        PlayerInput entrada;
        EntradaRemota remota; // jugador de una partida online que sabotea desde su casa
        InputAction accionMover, accionSaltar, accionAgarrar, accionLanzarse;
        float agresividad = 0.5f;
        int filaHud;

        TrampaSaboteable seleccionada;
        bool stickCentrado = true;

        // Máquina
        TrampaSaboteable pendiente;
        float activarEn;
        float siguientePensar;

        // Aspecto
        Transform marcador, rayo;
        Material materialMarcador;
        float ultimoPulso = -10f, ultimoFallo = -10f;
        string mensaje;
        float finMensaje;
        GUIStyle estilo;

        static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");

        public void Configurar(RondaBase rondaActual, PlayerInput mando, Color color, string etiqueta,
                               float agresividadMaquina, int fila, int indiceJugador, EntradaRemota mandoRemoto = null)
        {
            ronda = rondaActual;
            entrada = mando;
            remota = mandoRemoto;
            IndiceJugador = indiceJugador;
            ColorSaboteador = color;
            Etiqueta = etiqueta;
            agresividad = agresividadMaquina;
            filaHud = fila;

            if (entrada != null && entrada.actions != null)
            {
                accionMover = entrada.actions.FindAction("Mover");
                accionSaltar = entrada.actions.FindAction("Saltar");
                accionAgarrar = entrada.actions.FindAction("Agarrar");
                accionLanzarse = entrada.actions.FindAction("Lanzarse");
            }

            CrearMarcador();
            seleccionada = TrampaInicial();
            siguientePensar = Time.time + 2f; // la máquina da un respiro al empezar
        }

        public void SumarPuntos(int puntos, string motivo)
        {
            if (puntos <= 0) return;
            Puntos += puntos;
            mensaje = $"+{puntos} {motivo}";
            finMensaje = Time.time + 2f;
        }

        void OnDestroy()
        {
            if (marcador != null) Destroy(marcador.gameObject);
            if (rayo != null) Destroy(rayo.gameObject);
        }

        void OnDisable()
        {
            if (marcador != null) marcador.gameObject.SetActive(false);
            if (rayo != null) rayo.gameObject.SetActive(false);
        }

        bool Jugando => ronda != null && ronda.EstadoActual == RondaBase.Estado.Jugando;

        void Update()
        {
            bool visible = Jugando && seleccionada != null;
            if (marcador != null) marcador.gameObject.SetActive(visible);
            if (rayo != null) rayo.gameObject.SetActive(visible);
            if (!Jugando || Villano) return;

            if (seleccionada == null || !seleccionada.isActiveAndEnabled) seleccionada = TrampaInicial();
            if (seleccionada == null) return;

            if (EsMaquina) PensarComoMaquina();
            else LeerMando();

            ActualizarMarcador();
        }

        // ---------------------------------------------------------------------
        // Humano
        // ---------------------------------------------------------------------

        void LeerMando()
        {
            Vector2 stick = StickCrudo;
            if (stick.magnitude < 0.3f)
            {
                stickCentrado = true;
            }
            else if (stickCentrado && stick.magnitude > 0.6f)
            {
                stickCentrado = false;
                SaltarHacia(StickAMundo(stick));
            }

            if (PulsoAccion) Activar(seleccionada);
        }

        /// <summary>Elige la trampa más cercana en esa dirección (en el suelo).</summary>
        void SaltarHacia(Vector3 direccion)
        {
            if (seleccionada == null) return;
            Vector3 desde = seleccionada.CentroEfecto;
            TrampaSaboteable mejor = null;
            float mejorPuntuacion = float.MaxValue;

            foreach (var t in TrampaSaboteable.Todas)
            {
                if (t == seleccionada) continue;
                Vector3 v = Plano(t.CentroEfecto - desde);
                float distancia = v.magnitude;
                if (distancia < 0.1f) continue;
                float alineacion = Vector3.Dot(v / distancia, direccion);
                if (alineacion < 0.4f) continue;
                // Cerca y bien alineada es mejor.
                float puntuacion = distancia * (2f - alineacion);
                if (puntuacion < mejorPuntuacion) { mejorPuntuacion = puntuacion; mejor = t; }
            }

            if (mejor != null) seleccionada = mejor;
        }

        static Vector3 StickAMundo(Vector2 stick)
        {
            Vector3 adelante = Vector3.forward, derecha = Vector3.right;
            var cam = Camera.main;
            if (cam != null)
            {
                adelante = Plano(cam.transform.forward).normalized;
                derecha = Plano(cam.transform.right).normalized;
            }
            return (adelante * stick.y + derecha * stick.x).normalized;
        }

        void Activar(TrampaSaboteable trampa)
        {
            if (trampa != null && trampa.IntentarActivar(this)) ultimoPulso = Time.time;
            else ultimoFallo = Time.time;
        }

        // ---------------------------------------------------------------------
        // Máquina
        // ---------------------------------------------------------------------

        void PensarComoMaquina()
        {
            // Ya ha elegido: deja ver el rombo un momento y dispara.
            if (pendiente != null)
            {
                if (Time.time >= activarEn)
                {
                    Activar(pendiente);
                    pendiente = null;
                    // Descanso entre trampas: con menos, cada una se nota más (las estadísticas
                    // mostraban una trampa cada 5-6 s y se comían la partida).
                    siguientePensar = Time.time + Mathf.Lerp(11f, 6f, agresividad);
                }
                return;
            }

            if (Time.time < siguientePensar) return;
            siguientePensar = Time.time + 0.4f;

            TrampaSaboteable mejor = null;
            float mejorPuntuacion = 0f;
            foreach (var t in TrampaSaboteable.Todas)
            {
                if (!t.Lista) continue;
                float puntuacion = 0f;
                Vector3 centro = t.CentroEfecto;

                foreach (var objetivo in ronda.ObjetivosSabotaje())
                    if (Plano(objetivo - centro).magnitude < t.radioEfecto * 0.7f)
                        puntuacion += 3f;
                foreach (var b in ControladorBicho.Todos)
                    if (Plano(b.transform.position - centro).magnitude < t.radioEfecto * 0.6f)
                        puntuacion += 1f;

                if (puntuacion > mejorPuntuacion) { mejorPuntuacion = puntuacion; mejor = t; }
            }

            if (mejor != null && mejorPuntuacion >= 2f && Random.value < Mathf.Lerp(0.2f, 0.75f, agresividad))
            {
                seleccionada = mejor;
                pendiente = mejor;
                activarEn = Time.time + Mathf.Lerp(1.2f, 0.5f, agresividad);
            }
        }

        // ---------------------------------------------------------------------
        // Rombo sobre la trampa elegida
        // ---------------------------------------------------------------------

        TrampaSaboteable TrampaInicial()
        {
            TrampaSaboteable mejor = null;
            float mejorDistancia = float.MaxValue;
            Vector3 referencia = Vector3.zero;
            if (ControladorBicho.Todos.Count > 0)
            {
                foreach (var b in ControladorBicho.Todos) referencia += b.transform.position;
                referencia /= ControladorBicho.Todos.Count;
            }
            foreach (var t in TrampaSaboteable.Todas)
            {
                float d = (t.CentroEfecto - referencia).sqrMagnitude;
                if (d < mejorDistancia) { mejorDistancia = d; mejor = t; }
            }
            return mejor;
        }

        void CrearMarcador()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            materialMarcador = new Material(shader);

            marcador = Pieza(PrimitiveType.Cube, "Marcador saboteador");
            rayo = Pieza(PrimitiveType.Cylinder, "Rayo saboteador");
        }

        Transform Pieza(PrimitiveType tipo, string nombre)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = $"{nombre} ({Etiqueta})";
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = materialMarcador;
            go.SetActive(false);
            return go.transform;
        }

        void ActualizarMarcador()
        {
            if (marcador == null) return;

            bool lista = seleccionada.Lista;
            Color color = lista ? ColorSaboteador : Color.Lerp(ColorSaboteador, Color.gray, 0.7f);
            if (Time.time - ultimoFallo < 0.25f) color = Color.Lerp(color, Color.black, 0.5f);
            if (materialMarcador.HasProperty(IdBaseColor)) materialMarcador.SetColor(IdBaseColor, color);
            else materialMarcador.color = color;

            float pulso = Mathf.Clamp01(1f - (Time.time - ultimoPulso) / 0.4f);
            float tam = (lista ? 0.9f : 0.6f) * (1f + pulso * 0.8f);
            Vector3 punto = seleccionada.PuntoMarcador + Vector3.up * Mathf.Sin(Time.time * 4f) * 0.15f;

            marcador.position = punto;
            marcador.rotation = Quaternion.Euler(45f, Time.time * 120f, 45f);
            marcador.localScale = Vector3.one * tam;

            // Rayo fino del rombo hasta la trampa, para que se vea bien a cuál apunta.
            Vector3 suelo = seleccionada.CentroEfecto + Vector3.up * 0.2f;
            float alto = Mathf.Max(0.1f, punto.y - suelo.y);
            rayo.position = (punto + suelo) * 0.5f;
            rayo.rotation = Quaternion.identity;
            rayo.localScale = new Vector3(0.08f, alto * 0.5f, 0.08f);
        }

        // ---------------------------------------------------------------------
        // Panel en pantalla
        // ---------------------------------------------------------------------

        void OnGUI()
        {
            if (ronda == null || ronda.EstadoActual == RondaBase.Estado.Esperando) return;
            if (ShowBicho.Instancia != null && ShowBicho.Instancia.TapaLaPantalla) return;

            if (estilo == null)
                estilo = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            estilo.fontSize = Mathf.Max(12, Screen.height / 36);

            float h = Screen.height, w = Screen.width;
            float alto = h * 0.05f;
            float y = h * 0.93f - filaHud * (alto * 1.1f);

            string trampa = "";
            if (Villano)
                trampa = ronda.EstadoActual == RondaBase.Estado.Jugando && !EsMaquina ? AyudaVillano : "";
            else if (seleccionada != null && ronda.EstadoActual == RondaBase.Estado.Jugando)
                trampa = seleccionada.Lista ? $"{seleccionada.Nombre}: ¡LISTA!" : $"{seleccionada.Nombre}: {seleccionada.TiempoParaLista:0.0}s";
            string ayuda = Villano || EsMaquina || ronda.EstadoActual != RondaBase.Estado.Jugando ? "" : "   ·   Stick: elegir   ·   A: ¡activar!";
            string texto = $"{Etiqueta.ToUpper()}  {Puntos} pts   ·   {trampa}{ayuda}";
            if (Time.time < finMensaje) texto += $"   {mensaje}";

            var r = new Rect(w * 0.1f, y, w * 0.8f, alto);
            EstiloShow.Rectangulo(r, new Color(0f, 0f, 0f, 0.45f));
            estilo.normal.textColor = ColorSaboteador;
            EstiloShow.Etiqueta(r, texto, estilo);
        }

        static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
