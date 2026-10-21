using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Prueba "Despensa Deslizante" (todos contra todos): carrera por la encimera de la cocina.
    /// Mantel con rodillos y saleros, fregadero con esponjas y tapones, rampa de mantequilla con
    /// fruta rodando y bandejas con colchones elásticos hasta la meta. Tres puntos de control.
    /// Lo propio de esta carrera (el resto está en RondaCarrera):
    ///  - Quien llega sube al podio (1º, 2º, 3º) y se queda celebrando mientras los demás sufren.
    ///  - Cuando llega el primero suena el temporizador del horno: tic-tac cada vez más rápido.
    ///  - Los últimos segundos, el público golpea las gradas y los saleros tiemblan el doble.
    ///  - Si un tomate te tira fuera de la encimera, sales volando contra la grúa de cámara y
    ///    queda una pegatina de bicho espachurrado en la pantalla un momento.
    /// </summary>
    public class RondaDespensa : RondaCarrera
    {
        [Header("Despensa")]
        public FrutaRodante[] frutas;
        [Tooltip("Sitios del podio: 1º, 2º, 3º y el del 4º (al lado).")]
        public Transform[] podio;
        [Tooltip("Los últimos segundos en los que el público golpea las gradas.")]
        public float finalConPrisa = 20f;

        public override string Titulo => "DESPENSA DESLIZANTE";
        protected override string Descripcion => "¡Carrera por la encimera! Salta los rodillos, cruza el fregadero por las esponjas, sube la rampa de mantequilla y bota hasta la meta.";
        protected override string ConsejoInicio => "En la mantequilla, ¡no saltes! Si te caes, vuelves al último arco que cruzaste";

        bool hornoEnMarcha, prisa;
        float siguienteTic;
        readonly List<ControladorBicho> vigilados = new List<ControladorBicho>();
        (Color color, float hasta, float giro) pegatina = (Color.white, -1f, 0f);

        protected override void AlPrepararRonda()
        {
            base.AlPrepararRonda();
            hornoEnMarcha = prisa = false;
            SaleroGigante.Prisa = false;
            CamaraGrupo.SinEncuadrar.Clear();
            if (frutas != null) foreach (var f in frutas) if (f != null) f.Parar();
        }

        protected override void AlEmpezarRonda()
        {
            base.AlEmpezarRonda();
            if (frutas != null)
                for (int i = 0; i < frutas.Length; i++) if (frutas[i] != null) frutas[i].Empezar(2f + i * 0.7f);
            Vigilar(true);
        }

        protected override void AlTerminarRonda()
        {
            base.AlTerminarRonda();
            if (frutas != null) foreach (var f in frutas) if (f != null) f.Parar();
            SaleroGigante.Prisa = false;
            Vigilar(false);
            // Todos los que han llegado, al podio para la foto final.
            foreach (var b in corredores)
                if (b != null && HaLlegado(b)) AlPodio(b, llegada[b.IndiceJugador]);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Vigilar(false);
            CamaraGrupo.SinEncuadrar.Clear();
            SaleroGigante.Prisa = false;
        }

        // ---------------------------------------------------------------------
        // Podio
        // ---------------------------------------------------------------------

        /// <summary>En vez de dejar el circuito, sube al podio y se queda celebrando.</summary>
        protected override void TrasCelebrar(ControladorBicho b, int puesto) => AlPodio(b, puesto);

        void AlPodio(ControladorBicho b, int puesto)
        {
            if (podio == null || podio.Length == 0) { b.Retirar(true); return; }
            var sitio = podio[Mathf.Clamp(puesto - 1, 0, podio.Length - 1)];
            if (sitio == null) return;
            if (b.Retirado) b.Retirar(false);
            b.Colocar(sitio.position, sitio.rotation);
            b.FijarPuntoReaparicion(sitio.position, sitio.rotation);
            CamaraGrupo.SinEncuadrar.Add(b);
            if (b.TryGetComponent(out AnimadorBicho animador)) animador.Celebrar(30f);
        }

        // ---------------------------------------------------------------------
        // Temporizador del horno y público
        // ---------------------------------------------------------------------

        protected override void AlLlegarPrimero(ControladorBicho b)
        {
            if (TiempoRestante > tiempoTrasPrimero) TiempoRestante = tiempoTrasPrimero;
            hornoEnMarcha = true;
            siguienteTic = Time.time;
            Mensaje($"¡Suena el temporizador del horno! Quedan {Mathf.CeilToInt(TiempoRestante)} segundos", EstiloShow.Oro, 3f);
            Sonidos.Reproducir(Sonido.Alarma, null, 0.6f);
        }

        protected override void ActualizarCarrera()
        {
            // Tic-tac del horno, cada vez más rápido.
            if (hornoEnMarcha && Time.time >= siguienteTic)
            {
                float k = Mathf.Clamp01(TiempoRestante / Mathf.Max(1f, tiempoTrasPrimero));
                siguienteTic = Time.time + Mathf.Lerp(0.15f, 1f, k);
                Sonidos.Reproducir(Sonido.Click, null, 0.7f, Mathf.Lerp(1.6f, 1.1f, k));
            }

            // Los últimos segundos: el público golpea las gradas y los saleros tiemblan más.
            if (!prisa && TiempoRestante <= finalConPrisa)
            {
                prisa = true;
                SaleroGigante.Prisa = true;
                Mensaje("¡El público golpea las gradas! ¡Los saleros tiemblan!", EstiloShow.Rosa, 3f);
                Sonidos.Reproducir(Sonido.Aplausos);
            }
            if (prisa) PublicoAnimado.FiestaTodos(0.5f);
        }

        protected override void AlCruzarControl(ControladorBicho b, PuntoControlCarrera punto)
        {
            if (b.EsBot) return;
            string zona = punto.indice switch { 1 => "¡ZONA 2: el fregadero!", 2 => "¡ZONA 3: la rampa de mantequilla!", 3 => "¡ZONA 4: las bandejas de postre!", _ => null };
            if (zona != null && control[b.IndiceJugador] == punto.indice) Mensaje(zona, EstiloShow.Cian, 1.8f);
        }

        // ---------------------------------------------------------------------
        // Pegatina en la cámara
        // ---------------------------------------------------------------------

        void Vigilar(bool si)
        {
            foreach (var b in vigilados) if (b != null) b.AlReaparecer -= AlReaparecer;
            vigilados.Clear();
            if (!si) return;
            foreach (var b in ControladorBicho.Todos)
            {
                b.AlReaparecer += AlReaparecer;
                vigilados.Add(b);
            }
        }

        /// <summary>Reaparece tras caerse: si hace nada que le dio una fruta, ha salido volando contra la cámara.</summary>
        void AlReaparecer(ControladorBicho b)
        {
            if (EstadoActual != Estado.Jugando) return;
            if (!FrutaRodante.UltimoGolpe.TryGetValue(b, out float cuando) || Time.time - cuando > 4f) return;
            FrutaRodante.UltimoGolpe.Remove(b);
            pegatina = (gestor != null ? gestor.ColorDe(b.IndiceJugador) : Color.white, Time.time + 1.2f, Random.Range(-18f, 18f));
            Sonidos.Reproducir(Sonido.Golpe, null, 1f, 0.7f);
            Sonidos.Reproducir(Sonido.Ooh, null, 0.8f);
            Mensaje($"¡{NombreJugador(b)} se ha estampado contra la cámara!", pegatina.color, 2f);
        }

        protected override void OnGUI()
        {
            base.OnGUI();
            if (Time.time >= pegatina.hasta) return;
            float vida = Mathf.Clamp01((pegatina.hasta - Time.time) / 1.2f);
            float alfa = Mathf.Clamp01(vida * 3f);
            float tam = EstiloShow.Px(380f) * (1f + (1f - vida) * 0.08f);
            var centro = new Vector2(Screen.width * 0.5f, Screen.height * 0.45f);
            var matriz = GUI.matrix;
            GUIUtility.RotateAroundPivot(pegatina.giro, centro);
            // Manchurrón: un círculo grande y salpicaduras alrededor.
            Color c = pegatina.color.WithAlpha(0.85f * alfa);
            EstiloShow.Panel(new Rect(centro.x - tam * 0.5f, centro.y - tam * 0.42f, tam, tam * 0.84f), c, tam * 0.42f, 0f, false);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.9f + 0.3f;
                float r = tam * (0.5f + 0.08f * (i % 3));
                float g = tam * (0.08f + 0.04f * (i % 2));
                var p = centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                EstiloShow.Panel(new Rect(p.x - g, p.y - g, g * 2f, g * 2f), c, g, 0f, false);
            }
            // La cara del bicho espachurrado y el "¡PLAF!".
            float ojo = tam * 0.07f;
            Color blanco = Color.white.WithAlpha(alfa);
            EstiloShow.Panel(new Rect(centro.x - tam * 0.16f - ojo, centro.y - tam * 0.12f - ojo, ojo * 2f, ojo * 2f), blanco, ojo, 0f, false);
            EstiloShow.Panel(new Rect(centro.x + tam * 0.16f - ojo, centro.y - tam * 0.12f - ojo, ojo * 2f, ojo * 2f), blanco, ojo, 0f, false);
            EstiloShow.Texto(new Rect(centro.x - tam, centro.y + tam * 0.02f, tam * 2f, tam * 0.3f), "¡PLAF!", Mathf.RoundToInt(EstiloShow.Px(70f)), blanco);
            GUI.matrix = matriz;
        }
    }
}
