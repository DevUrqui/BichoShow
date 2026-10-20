using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Costura a la fuga", zona 4: puente con una cremallera gigante. El suelo son tramos de
    /// dos mitades. El cursor (el tirador dorado) va del principio al final abriendo los tramos
    /// que deja atrás: las mitades se doblan hacia abajo como dientes que se separan. Se queda
    /// un momento al final y vuelve cerrándolos; al principio espera un poco con todo cerrado.
    ///
    /// Se puede cruzar siempre que vayas por delante del cursor: hay que salir justo cuando
    /// cierra. Antes de abrirse, cada tramo tiembla y hace clic.
    /// </summary>
    public class Cremallera : MonoBehaviour, IEsperaBot
    {
        [Tooltip("Bisagras de cada tramo, por orden: [2k] la mitad izquierda, [2k+1] la derecha. Con Rigidbody cinemático.")]
        public Rigidbody[] bisagras;
        [Tooltip("El tirador: se mueve por el eje Z local, de 0 a 'largo'.")]
        public Transform cursor;
        [Tooltip("Largo del puente (m), en el eje Z local.")]
        public float largo = 10f;
        [Tooltip("Velocidad del cursor (m/s). Un bicho corre a 6.")]
        public float velocidad = 3f;
        [Tooltip("Segundos con todo cerrado al principio.")]
        public float pausaCerrada = 2f;
        [Tooltip("Segundos con todo abierto al final.")]
        public float pausaAbierta = 1f;
        [Tooltip("Retraso del ciclo (s), para que dos cremalleras no vayan a la vez.")]
        public float retraso;
        public float anguloAbierta = 80f;
        public float tiempoAbrir = 0.3f;
        [Tooltip("El tramo tiembla cuando el cursor está a esta distancia de su final (m).")]
        public float distanciaAviso = 1.6f;

        float[] apertura;
        bool[] abiertoAntes, avisandoAntes;
        Quaternion[] giroInicial;
        Vector3 posicionCursor;

        int Tramos => bisagras != null ? bisagras.Length / 2 : 0;
        float LargoTramo => largo / Mathf.Max(1, Tramos);
        float Ida => largo / Mathf.Max(0.1f, velocidad);
        float Periodo => pausaCerrada + Ida + pausaAbierta + Ida;

        void Awake()
        {
            int n = Tramos;
            apertura = new float[n];
            abiertoAntes = new bool[n];
            avisandoAntes = new bool[n];
            giroInicial = new Quaternion[bisagras.Length];
            for (int i = 0; i < bisagras.Length; i++)
            {
                if (bisagras[i] == null) continue;
                bisagras[i].isKinematic = true;
                bisagras[i].interpolation = RigidbodyInterpolation.Interpolate;
                giroInicial[i] = bisagras[i].transform.localRotation;
            }
            if (cursor != null) posicionCursor = cursor.localPosition;
        }

        /// <summary>fase: 0 cerrada esperando, 1 abriendo (ida), 2 abierta, 3 cerrando (vuelta). s: dónde está el cursor.</summary>
        void Estado(float t, out int fase, out float s, out float quedaFase)
        {
            float f = Mathf.Repeat(t - retraso, Periodo);
            if (f < pausaCerrada) { fase = 0; s = 0f; quedaFase = pausaCerrada - f; return; }
            f -= pausaCerrada;
            if (f < Ida) { fase = 1; s = f * velocidad; quedaFase = Ida - f; return; }
            f -= Ida;
            if (f < pausaAbierta) { fase = 2; s = largo; quedaFase = pausaAbierta - f; return; }
            f -= pausaAbierta;
            fase = 3;
            s = largo - f * velocidad;
            quedaFase = Ida - f;
        }

        bool Abierto(int k, int fase, float s)
        {
            float inicio = k * LargoTramo, fin = inicio + LargoTramo;
            switch (fase)
            {
                case 0: return false;
                case 1: return s >= fin;                         // el cursor ya lo ha dejado atrás
                case 2: return true;
                default: return s > inicio + LargoTramo * 0.5f;  // aún no ha vuelto a cerrarlo
            }
        }

        void FixedUpdate()
        {
            if (apertura == null) return;
            Estado(Time.time, out int fase, out float s, out _);
            if (cursor != null) cursor.localPosition = new Vector3(posicionCursor.x, posicionCursor.y, s);

            float paso = Time.fixedDeltaTime / Mathf.Max(0.05f, tiempoAbrir);
            for (int k = 0; k < Tramos; k++)
            {
                bool abierto = Abierto(k, fase, s);
                float fin = (k + 1) * LargoTramo;
                bool avisando = fase == 1 && !abierto && s > fin - distanciaAviso;
                Vector3 centro = transform.TransformPoint(new Vector3(0f, 0f, fin - LargoTramo * 0.5f));

                if (avisando && !avisandoAntes[k]) Sonidos.Reproducir(Sonido.Click, centro, 0.5f, 1.2f);
                if (abierto && !abiertoAntes[k]) Sonidos.Reproducir(Sonido.Mecanismo, centro, 0.45f);
                avisandoAntes[k] = avisando;
                abiertoAntes[k] = abierto;

                apertura[k] = Mathf.MoveTowards(apertura[k], abierto ? 1f : 0f, paso);
                float temblor = avisando ? Mathf.Sin(Time.time * 50f + k) * 3f : 0f;
                float angulo = Mathf.SmoothStep(0f, 1f, apertura[k]) * anguloAbierta + temblor;
                Girar(2 * k, -angulo);     // la izquierda baja por su lado de dentro
                Girar(2 * k + 1, angulo);  // y la derecha, por el suyo
            }
        }

        void Girar(int i, float angulo)
        {
            var b = bisagras[i];
            if (b == null) return;
            var local = giroInicial[i] * Quaternion.Euler(0f, 0f, angulo);
            var padre = b.transform.parent;
            b.MoveRotation(padre != null ? padre.rotation * local : local);
        }

        /// <summary>
        /// Para los bots: ¿salgo ya? Solo con todo cerrado (esperando al principio o el cursor
        /// a punto de llegar): corriendo más que el cursor, siempre se va por delante.
        /// </summary>
        public bool PuedoPasar(ControladorBicho bicho)
        {
            Estado(Time.time, out int fase, out float s, out float queda);
            return (fase == 0 && queda > 0.15f) || (fase == 3 && s < LargoTramo * 0.5f);
        }
    }
}
