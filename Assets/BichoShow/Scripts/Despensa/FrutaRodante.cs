using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Despensa Deslizante", zona 3: lanzador de fruta en lo alto de la rampa de mantequilla.
    /// Suelta piezas (tomates cherry por el centro, arándanos por los bordes) que bajan rodando
    /// en línea recta; los arándanos, además, van dando botes de lado. A quien pillan lo derriban
    /// y lo mandan mantequilla abajo. Si una pieza llega a un muro de refugio, revienta contra él:
    /// detrás de los muros se puede esperar a ver pasar la fruta. Las piezas se reciclan.
    /// </summary>
    public class FrutaRodante : MonoBehaviour
    {
        [Tooltip("Las piezas (Rigidbody cinemático con colisión). Se reciclan.")]
        public Rigidbody[] piezas;
        [Tooltip("Arriba (donde salen) y abajo (donde revientan), por el centro de la rampa.")]
        public Vector3 arriba, abajo;
        [Tooltip("Hacia la derecha de la rampa (en el mundo).")]
        public Vector3 lado = Vector3.right;
        [Tooltip("Carriles: distancia al centro (m), desde y hasta (con signo al azar).")]
        public Vector2 carril = new Vector2(0f, 3f);
        public float radio = 1f;
        public float velocidad = 8f;
        public float cadencia = 1.8f;
        [Tooltip("Botes de lado (m de amplitud; 0 = recta). Los arándanos.")]
        public float zigzag;
        public Color colorZumo = new Color(0.9f, 0.15f, 0.1f);
        public float empujon = 9f;
        public float tumbado = 1.5f;
        [Tooltip("Muros de refugio: la fruta revienta al llegar a ellos. Su escala X es el ancho.")]
        public Transform[] refugios;

        /// <summary>Último golpe de fruta a cada bicho (para la pegatina en la cámara).</summary>
        public static readonly Dictionary<ControladorBicho, float> UltimoGolpe = new Dictionary<ControladorBicho, float>();

        class Viaje { public bool activa; public float recorrido, carril, fase; }

        Viaje[] viajes;
        float siguiente;
        bool activo;

        void Awake()
        {
            viajes = new Viaje[piezas != null ? piezas.Length : 0];
            for (int i = 0; i < viajes.Length; i++)
            {
                viajes[i] = new Viaje();
                if (piezas[i] == null) continue;
                piezas[i].isKinematic = true;
                piezas[i].interpolation = RigidbodyInterpolation.Interpolate;
                piezas[i].gameObject.SetActive(false);
                piezas[i].gameObject.AddComponent<GolpeFruta>().dueno = this;
            }
        }

        public void Empezar(float retraso) { activo = true; siguiente = Time.time + retraso; }

        public void Parar()
        {
            activo = false;
            for (int i = 0; i < viajes.Length; i++) Apagar(i, false);
        }

        float Largo => Vector3.Distance(arriba, abajo);

        Vector3 Posicion(Viaje v)
        {
            Vector3 dir = (abajo - arriba).normalized;
            float x = v.carril + (zigzag > 0f ? Mathf.Sin((Time.time + v.fase) * 5f) * zigzag : 0f);
            // La normal de la rampa: perpendicular a la bajada y al lado.
            Vector3 normal = Vector3.Cross(dir, lado).normalized;
            if (normal.y < 0f) normal = -normal;
            return arriba + dir * v.recorrido + lado.normalized * x + normal * radio;
        }

        void FixedUpdate()
        {
            if (activo && Time.time >= siguiente)
            {
                Soltar();
                siguiente = Time.time + cadencia * Random.Range(0.7f, 1.3f);
            }
            float largo = Largo;
            for (int i = 0; i < viajes.Length; i++)
            {
                var v = viajes[i];
                if (!v.activa) continue;
                v.recorrido += velocidad * Time.fixedDeltaTime;
                Vector3 p = Posicion(v);
                if (v.recorrido >= largo || ChocaConRefugio(p)) { Apagar(i, true); continue; }
                Vector3 paso = p - piezas[i].position;
                piezas[i].MovePosition(p);
                if (paso.sqrMagnitude > 0.0001f)
                {
                    Vector3 eje = Vector3.Cross(Vector3.up, paso.normalized);
                    piezas[i].MoveRotation(Quaternion.AngleAxis(paso.magnitude / radio * Mathf.Rad2Deg, eje) * piezas[i].rotation);
                }
            }
        }

        bool ChocaConRefugio(Vector3 p)
        {
            if (refugios == null) return false;
            foreach (var r in refugios)
            {
                if (r == null) continue;
                Vector3 local = r.InverseTransformPoint(p);
                // Caja del muro (cubo de 1 escalado) más el radio de la fruta.
                Vector3 e = r.lossyScale;
                if (Mathf.Abs(local.x) * e.x < e.x * 0.5f + radio && Mathf.Abs(local.z) * e.z < e.z * 0.5f + radio && local.y * e.y < e.y * 0.5f + radio)
                    return true;
            }
            return false;
        }

        void Soltar()
        {
            for (int i = 0; i < viajes.Length; i++)
            {
                var v = viajes[i];
                if (v.activa) continue;
                v.activa = true;
                v.recorrido = 0f;
                v.fase = Random.Range(0f, 6f);
                v.carril = Random.Range(carril.x, carril.y) * (Random.value < 0.5f ? -1f : 1f);
                piezas[i].transform.position = Posicion(v);
                piezas[i].gameObject.SetActive(true);
                Sonidos.Reproducir(Sonido.Bote, arriba, 0.6f, zigzag > 0f ? 1.4f : 0.9f);
                return;
            }
        }

        void Apagar(int i, bool reventar)
        {
            viajes[i].activa = false;
            if (piezas[i] == null) return;
            if (reventar && piezas[i].gameObject.activeSelf)
            {
                Efectos.Impacto(piezas[i].position, colorZumo, 0.9f);
                Sonidos.Reproducir(Sonido.Rotura, piezas[i].position, 0.5f, 1.3f);
            }
            piezas[i].gameObject.SetActive(false);
        }

        /// <summary>Atropello: el bicho cae y patina mantequilla abajo.</summary>
        internal void Golpear(Rigidbody pieza, Collision choque)
        {
            var cuerpo = choque.rigidbody;
            var bicho = cuerpo != null ? cuerpo.GetComponent<ControladorBicho>() : null;
            if (bicho == null || bicho.Retirado) return;
            if (UltimoGolpe.TryGetValue(bicho, out float antes) && Time.time - antes < 0.8f) return;
            UltimoGolpe[bicho] = Time.time;

            Vector3 bajada = (abajo - arriba).normalized;
            Vector3 v = bajada * empujon;
            v.y = Mathf.Max(v.y, 0f) + 3f;
            cuerpo.linearVelocity = v;
            bicho.Tropezar(tumbado);
            bicho.Resbalar(0.05f, tumbado + 0.5f);
            Sonidos.Reproducir(Sonido.Golpe, bicho.transform.position, bicho.EsBot ? 0.6f : 1f);
            Efectos.Impacto(bicho.Cuerpo.worldCenterOfMass, colorZumo, 0.8f);
            if (!bicho.EsBot) Efectos.Sacudir(0.25f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Limpiar() => UltimoGolpe.Clear();
    }
}
