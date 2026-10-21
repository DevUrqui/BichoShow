using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BichoShow
{
    /// <summary>
    /// Va en la cámara: el decorado que se queda entre la cámara y algún bicho (gradas,
    /// banderolas, pétalos…) se vuelve invisible mientras estorba; sigue dando sombra, así que
    /// no "desaparece" del todo. Solo mira las piezas que cuelgan de "raices" y no toca las
    /// muy grandes (suelo, cúpula, tallo), que nunca se ponen delante. Mira a los bichos de las
    /// personas (si no hay ninguna, a todos). Para que no parpadee: la prueba es con la caja de
    /// cada pieza en su propio giro (no la caja del mundo, que en una hoja inclinada es enorme),
    /// para seguir oculta basta con estar cerca de estorbar y tarda un rato en volver.
    /// También funciona en los invitados del online (cada uno lo calcula con su cámara).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CamaraSinEstorbos : MonoBehaviour
    {
        [Tooltip("Grupos de decorado que se pueden ocultar si tapan a un bicho.")]
        public Transform[] raices;
        [Tooltip("Las piezas más grandes que esto (diagonal de su caja, m) no se ocultan nunca.")]
        public float tamanoMaximo = 40f;
        [Tooltip("Lo que se acerca a menos de esto del bicho no cuenta como estorbo (m): está a su lado, no delante.")]
        public float holgura = 1.2f;
        [Tooltip("Cada cuánto se comprueba (s).")]
        public float cadencia = 0.1f;
        [Tooltip("Cuánto tarda en volver a verse una pieza que ya no estorba (s).")]
        public float tiempoVolver = 1.2f;
        [Tooltip("Margen (m) alrededor de la pieza para empezar a ocultarla.")]
        public float margenOcultar = 0.2f;
        [Tooltip("Margen (m), más grande, para seguir oculta: así no parpadea en el límite.")]
        public float margenSeguir = 1f;

        class Pieza
        {
            public Renderer render;
            public ShadowCastingMode sombra;
            public bool oculta;
            public float hasta;
        }

        readonly List<Pieza> piezas = new List<Pieza>();
        readonly List<Vector3> puntos = new List<Vector3>();
        float siguiente;

        void Start() => Recoger();

        /// <summary>Vuelve a buscar las piezas (por si el decorado ha cambiado).</summary>
        public void Recoger()
        {
            Restaurar();
            piezas.Clear();
            if (raices == null) return;
            foreach (var raiz in raices)
            {
                if (raiz == null) continue;
                foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
                    if (!(r is ParticleSystemRenderer))
                        piezas.Add(new Pieza { render = r, sombra = r.shadowCastingMode });
            }
        }

        void LateUpdate()
        {
            if (Time.time < siguiente) return;
            siguiente = Time.time + cadencia;

            Vector3 ojo = transform.position;
            puntos.Clear();
            bool hayPersonas = false;
            foreach (var b in ControladorBicho.Todos)
                if (b != null && !b.Retirado && !b.EsBot) { hayPersonas = true; break; }
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado || (hayPersonas && b.EsBot)) continue;
                Vector3 p = CamaraGrupo.PuntoDe(b);
                puntos.Add(p);
                puntos.Add(p + Vector3.up * 0.7f);
            }

            foreach (var pieza in piezas)
            {
                if (pieza.render == null) continue;
                bool estorba = Estorba(pieza.render, ojo, pieza.oculta ? margenSeguir : margenOcultar);
                if (estorba)
                {
                    pieza.hasta = Time.time + tiempoVolver;
                    if (!pieza.oculta)
                    {
                        pieza.oculta = true;
                        pieza.render.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    }
                }
                else if (pieza.oculta && Time.time >= pieza.hasta)
                {
                    pieza.oculta = false;
                    pieza.render.shadowCastingMode = pieza.sombra;
                }
            }
        }

        /// <summary>¿Corta la pieza (su caja, en su propio giro) alguna línea de la cámara a un bicho?</summary>
        bool Estorba(Renderer render, Vector3 ojo, float margen)
        {
            if (render.bounds.size.magnitude > tamanoMaximo) return false;
            Matrix4x4 aLocal = render.worldToLocalMatrix;
            Vector3 escala = render.transform.lossyScale;
            float escalaMedia = Mathf.Max(0.001f, (Mathf.Abs(escala.x) + Mathf.Abs(escala.y) + Mathf.Abs(escala.z)) / 3f);
            Bounds caja = render.localBounds;
            caja.Expand(2f * margen / escalaMedia);
            Vector3 desde = aLocal.MultiplyPoint3x4(ojo);
            foreach (var p in puntos)
            {
                float largo = Vector3.Distance(ojo, p);
                if (largo < 0.1f) continue;
                Vector3 d = aLocal.MultiplyPoint3x4(p) - desde;
                float largoLocal = d.magnitude;
                if (largoLocal < 0.0001f) continue;
                if (caja.IntersectRay(new Ray(desde, d / largoLocal), out float t) && t / largoLocal * largo < largo - holgura) return true;
            }
            return false;
        }

        void OnDisable() => Restaurar();

        void Restaurar()
        {
            foreach (var pieza in piezas)
                if (pieza.render != null && pieza.oculta)
                {
                    pieza.oculta = false;
                    pieza.render.shadowCastingMode = pieza.sombra;
                }
        }
    }
}
