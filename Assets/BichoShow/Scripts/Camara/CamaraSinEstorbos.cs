using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BichoShow
{
    /// <summary>
    /// Va en la cámara: el decorado que se queda entre la cámara y algún bicho (gradas,
    /// banderolas, pétalos…) se vuelve invisible mientras estorba; sigue dando sombra, así que
    /// no "desaparece" del todo. Solo mira las piezas que cuelgan de "raices" y no toca las
    /// muy grandes (suelo, cúpula, tallo), que nunca se ponen delante.
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
        public float tiempoVolver = 0.6f;

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
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado) continue;
                Vector3 p = CamaraGrupo.PuntoDe(b);
                puntos.Add(p);
                puntos.Add(p + Vector3.up * 0.7f);
            }

            foreach (var pieza in piezas)
            {
                if (pieza.render == null) continue;
                bool estorba = Estorba(pieza.render.bounds, ojo);
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

        bool Estorba(Bounds caja, Vector3 ojo)
        {
            if (caja.size.magnitude > tamanoMaximo) return false;
            caja.Expand(0.6f);
            foreach (var p in puntos)
            {
                Vector3 d = p - ojo;
                float largo = d.magnitude;
                if (largo < 0.1f) continue;
                if (caja.IntersectRay(new Ray(ojo, d / largo), out float distancia) && distancia < largo - holgura) return true;
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
