using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Bot para "Pintabichos": va a por las baldosas que no son de su equipo (mejor las del
    /// rival), corre a por los botes de pintura si los tiene cerca, placa a los rivales que se
    /// cruza y salta la barredora del centro.
    /// </summary>
    public class BotPintura : MonoBehaviour, IEntradaBicho
    {
        [Range(0f, 1f)] public float habilidad = 0.55f;

        public Vector2 Mover { get; private set; }
        public bool Agarrar => false;

        RondaPintura ronda;
        ControladorBicho bicho;
        Camera camara;
        bool saltarPendiente, lanzarsePendiente;
        float siguientePensar, siguienteSalto, semilla, finObjetivo;
        Vector3 objetivo, direccion;

        public bool ConsumirSaltar() { bool s = saltarPendiente; saltarPendiente = false; return s; }
        public bool ConsumirLanzarse() { bool s = lanzarsePendiente; lanzarsePendiente = false; return s; }

        public void Configurar(RondaPintura rondaPintura, float nivel)
        {
            ronda = rondaPintura;
            habilidad = nivel;
        }

        void Start()
        {
            bicho = GetComponent<ControladorBicho>();
            camara = Camera.main;
            if (ronda == null) ronda = RondaPintura.Instancia;
            semilla = Random.value * 100f;
            objetivo = transform.position;
        }

        void Update()
        {
            if (ronda == null || bicho == null || ronda.EstadoActual != RondaBase.Estado.Jugando || bicho.Tumbado)
            {
                Mover = Vector2.zero;
                return;
            }
            if (Time.time >= siguientePensar)
            {
                Pensar();
                siguientePensar = Time.time + Mathf.Lerp(0.5f, 0.15f, habilidad) * Random.Range(0.8f, 1.2f);
            }
            SaltarBarredora();
            if (Camera.main != null) camara = Camera.main;
            Mover = AStick(direccion);
        }

        void Pensar()
        {
            Vector3 yo = transform.position;
            int equipo = RondaBase.EquipoDe(bicho);

            // 1) Bote de pintura cerca: a por él.
            Vector3? bote = null;
            float mejorBote = Mathf.Lerp(4f, 8f, habilidad);
            foreach (var b in ronda.Botes())
            {
                float d = Plano(b - yo).magnitude;
                if (d < mejorBote) { mejorBote = d; bote = b; }
            }

            if (bote.HasValue)
            {
                objetivo = bote.Value;
            }
            else
            {
                // 2) Seguir hacia la baldosa elegida mientras no sea ya nuestra; si no, elegir otra.
                bool llegado = Plano(objetivo - yo).magnitude < 0.7f || ronda.DuenaEn(objetivo) == equipo || Time.time > finObjetivo;
                if (llegado) ElegirBaldosa(yo, equipo);
            }

            Vector3 deseo = Plano(objetivo - yo);
            deseo = deseo.sqrMagnitude > 0.01f ? deseo.normalized : Vector3.zero;

            // 3) Placar a un rival que pase cerca.
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == bicho || b.Retirado || RondaBase.EquipoDe(b) == equipo) continue;
                float d = Plano(b.transform.position - yo).magnitude;
                if (d < 2.4f && d > 1f && Random.value < Mathf.Lerp(0.03f, 0.12f, habilidad))
                {
                    deseo = Plano(b.transform.position - yo).normalized;
                    lanzarsePendiente = true;
                    break;
                }
            }

            float ruido = (Mathf.PerlinNoise(Time.time * 1.3f, semilla) - 0.5f) * Mathf.Lerp(45f, 8f, habilidad);
            direccion = Quaternion.Euler(0f, ruido, 0f) * deseo;
        }

        /// <summary>Mira unas cuantas baldosas al azar y se queda con la más jugosa y cercana.</summary>
        void ElegirBaldosa(Vector3 yo, int equipo)
        {
            float mejor = float.MaxValue;
            Vector3 elegida = ronda.Centro;
            int muestras = Mathf.RoundToInt(Mathf.Lerp(6f, 18f, habilidad));
            for (int k = 0; k < muestras; k++)
            {
                int x = Random.Range(0, ronda.columnas), z = Random.Range(0, ronda.filas);
                int duena = ronda.Duena(x, z);
                if (duena == equipo) continue;
                Vector3 c = ronda.CentroBaldosa(x, z);
                float puntos = Plano(c - yo).magnitude - (duena >= 0 ? 2.5f : 0f) + Random.Range(0f, 2f);
                if (puntos < mejor) { mejor = puntos; elegida = c; }
            }
            objetivo = elegida;
            finObjetivo = Time.time + 4f;
        }

        void SaltarBarredora()
        {
            if (Time.time < siguienteSalto || !bicho.EnSuelo) return;
            if (Plano(transform.position - ronda.Centro).magnitude < 4.2f)
            {
                saltarPendiente = Random.value < Mathf.Lerp(0.3f, 0.8f, habilidad);
                siguienteSalto = Time.time + Random.Range(0.6f, 1.2f);
            }
        }

        Vector2 AStick(Vector3 direccionMundo)
        {
            Vector3 adelante = Vector3.forward, derecha = Vector3.right;
            if (camara != null)
            {
                adelante = Plano(camara.transform.forward).normalized;
                derecha = Plano(camara.transform.right).normalized;
            }
            return new Vector2(Vector3.Dot(direccionMundo, derecha), Vector3.Dot(direccionMundo, adelante));
        }

        static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
