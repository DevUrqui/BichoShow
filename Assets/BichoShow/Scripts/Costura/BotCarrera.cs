using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Bot de carrera (Costura a la fuga, La Cumbre del Girasol): sigue los PuntoRuta de la escena. En cada punto
    /// salta si hay hueco y espera si el obstáculo lo pide (carrete en su carril, panel a punto
    /// de inclinarse, cremallera abierta). Los hábiles cogen más la ruta difícil y reaccionan
    /// antes; los torpes dudan, se despistan y prefieren el camino largo.
    /// </summary>
    public class BotCarrera : MonoBehaviour, IEntradaBicho
    {
        [Range(0f, 1f)] public float habilidad = 0.55f;

        public Vector2 Mover { get; private set; }
        /// <summary>Con la mano estirada cuando va a por algo que hay que agarrar (la corona de la cumbre).</summary>
        public bool Agarrar => objetivo != null && objetivo.agarrar;

        RondaRuta ronda;
        ControladorBicho bicho;
        Camera camara;
        PuntoRuta objetivo;
        bool saltarPendiente, lanzarsePendiente;
        float carril, semilla;
        float listoDesde = -1f, reaccion;
        float mejorDistancia = float.MaxValue, ultimoAvance;
        int atascos;

        public bool ConsumirSaltar() { bool s = saltarPendiente; saltarPendiente = false; return s; }
        public bool ConsumirLanzarse() { bool s = lanzarsePendiente; lanzarsePendiente = false; return s; }

        public void Configurar(RondaRuta rondaCarrera, float nivel)
        {
            ronda = rondaCarrera;
            habilidad = nivel;
        }

        void Start()
        {
            bicho = GetComponent<ControladorBicho>();
            camara = Camera.main;
            if (ronda == null) ronda = FindAnyObjectByType<RondaRuta>();
            carril = Random.Range(-1f, 1f);
            semilla = Random.value * 100f;
            if (bicho != null) bicho.AlReaparecer += AlReaparecer;
        }

        void OnDestroy()
        {
            if (bicho != null) bicho.AlReaparecer -= AlReaparecer;
        }

        void AlReaparecer(ControladorBicho _)
        {
            objetivo = null; // al volver a jugar, busca el punto del camino más cercano
            mejorDistancia = float.MaxValue;
            ultimoAvance = Time.time;
        }

        void Update()
        {
            if (ronda == null || bicho == null || bicho.Tumbado || ronda.HaLlegado(bicho) ||
                ronda.EstadoActual != RondaBase.Estado.Jugando)
            {
                Mover = Vector2.zero;
                return;
            }
            if (Camera.main != null) camara = Camera.main;
            Vector3 yo = transform.position;

            if (objetivo == null)
            {
                objetivo = ronda.PuntoRutaCercano(yo);
                mejorDistancia = float.MaxValue;
                ultimoAvance = Time.time;
                if (objetivo == null) { Mover = AStick(Vector3.forward); return; }
            }

            Vector3 destino = objetivo.transform.position + objetivo.transform.right * (carril * objetivo.anchura);
            Vector3 hacia = Plano(destino - yo);
            float distancia = hacia.magnitude;
            bool llegado = distancia < 0.7f && yo.y > objetivo.transform.position.y - 1.2f;

            if (llegado)
            {
                // ¿Hay que esperar a que el obstáculo deje pasar?
                var espera = objetivo.Espera;
                if (espera != null)
                {
                    if (!espera.PuedoPasar(bicho)) { listoDesde = -1f; Quieto(hacia); return; }
                    if (listoDesde < 0f)
                    {
                        listoDesde = Time.time;
                        // Los torpes tardan en darse cuenta de que ya pueden ir.
                        reaccion = Mathf.Lerp(0.6f, 0.05f, habilidad) * Random.Range(0.5f, 1.5f);
                    }
                    if (Time.time - listoDesde < reaccion) { Quieto(hacia); return; }
                }
                if (objetivo.saltar && bicho.EnSuelo) saltarPendiente = true;
                objetivo = Elegir(objetivo.siguientes);
                listoDesde = -1f;
                mejorDistancia = float.MaxValue;
                ultimoAvance = Time.time;
                atascos = 0;
                if (objetivo == null) { Mover = AStick(Vector3.forward); return; }
                destino = objetivo.transform.position + objetivo.transform.right * (carril * objetivo.anchura);
                hacia = Plano(destino - yo);
                distancia = hacia.magnitude;
            }

            // Ya ha dejado atrás su punto (una hoja trampolín lo ha lanzado) o está en otro piso
            // (se ha caído a la vuelta de abajo): buscar otro.
            if (ronda.AvanceDe(objetivo.transform.position) < ronda.AvanceDe(yo) - 2.5f || Mathf.Abs(objetivo.transform.position.y - yo.y) > 6f)
            {
                objetivo = null;
                Mover = Vector2.zero;
                return;
            }

            // Atascado (empujado contra algo, escalón…): saltar; si sigue, buscar otro punto.
            if (distancia < mejorDistancia - 0.3f)
            {
                mejorDistancia = distancia;
                ultimoAvance = Time.time;
            }
            else if (Time.time - ultimoAvance > 2.5f)
            {
                saltarPendiente = true;
                ultimoAvance = Time.time;
                if (++atascos >= 3) { objetivo = null; atascos = 0; }
            }

            // Un poco de torpeza: el rumbo baila más cuanto menos hábil.
            float ruido = (Mathf.PerlinNoise(Time.time * 1.1f, semilla) - 0.5f) * Mathf.Lerp(40f, 6f, habilidad);
            Vector3 direccion = Quaternion.Euler(0f, ruido, 0f) * hacia.normalized;

            // De vez en cuando, un placaje al rival que tiene delante.
            if (Random.value < Time.deltaTime * Mathf.Lerp(0.05f, 0.25f, habilidad) && RivalDelante(direccion))
                lanzarsePendiente = true;

            Mover = AStick(direccion);
        }

        /// <summary>Esperando: quieto, pero mirando hacia donde va (así se ve que espera).</summary>
        void Quieto(Vector3 hacia)
        {
            Mover = hacia.sqrMagnitude > 0.04f ? AStick(hacia.normalized * 0.2f) : Vector2.zero;
        }

        PuntoRuta Elegir(PuntoRuta[] opciones)
        {
            if (opciones == null || opciones.Length == 0) return null;
            if (opciones.Length == 1 || opciones[1] == null) return opciones[0];
            // [0] difícil y rápida, [1] fácil y larga.
            return Random.value < Mathf.Lerp(0.2f, 0.85f, habilidad) ? opciones[0] : opciones[1];
        }

        bool RivalDelante(Vector3 direccion)
        {
            Vector3 yo = transform.position;
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == bicho || b.Retirado) continue;
                Vector3 d = Plano(b.transform.position - yo);
                if (d.magnitude < 2.6f && d.magnitude > 1f && Vector3.Dot(d.normalized, direccion) > 0.8f &&
                    Mathf.Abs(b.transform.position.y - yo.y) < 0.6f)
                    return true;
            }
            return false;
        }

        Vector2 AStick(Vector3 direccionMundo)
        {
            Vector3 adelante = Vector3.forward, derecha = Vector3.right;
            if (camara != null)
            {
                adelante = Plano(camara.transform.forward).normalized;
                derecha = Plano(camara.transform.right).normalized;
            }
            return Vector2.ClampMagnitude(new Vector2(Vector3.Dot(direccionMundo, derecha), Vector3.Dot(direccionMundo, adelante)), 1f);
        }

        static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
