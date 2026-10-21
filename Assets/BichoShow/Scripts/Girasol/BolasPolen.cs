using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "La Cumbre del Girasol", sección 2: el cañón de polen. Suelta bolas gigantes que bajan
    /// rodando por el centro de la pista (las franjas amarillas y naranjas), siguiendo el camino
    /// de la espiral. Quien se cruza con una rueda sin control cuesta abajo un par de segundos.
    /// Las bolas son siempre las mismas (se reciclan): así se ven igual en una partida online.
    /// </summary>
    public class BolasPolen : MonoBehaviour
    {
        [Tooltip("Las bolas (con Rigidbody cinemático y colisión). Se reciclan.")]
        public Rigidbody[] bolas;
        [Tooltip("Centro de la pista, de arriba (donde salen) hacia abajo (donde se pierden).")]
        public Vector3[] camino;
        [Tooltip("Hacia fuera de la espiral en cada punto del camino (para el carril).")]
        public Vector3[] fuera;
        [Tooltip("Ancho de la pista en cada punto: las bolas van por el tercio del centro.")]
        public float[] ancho;
        public float radioBola = 1.2f;
        public float velocidad = 7f;
        [Tooltip("Segundos entre bola y bola (la mitad durante la fiebre del polen).")]
        public float cadencia = 3.5f;
        public float empujon = 8f;
        public float tropiezo = 2f;

        class Viaje
        {
            public float recorrido;   // metros desde el cañón
            public float carril;      // -1..1 dentro del tercio del centro
            public bool activa;
        }

        Viaje[] viajes;
        float[] distancias;
        float siguiente;
        float finFiebre = -1f;
        bool activo;
        readonly Dictionary<ControladorBicho, float> ultimoGolpe = new Dictionary<ControladorBicho, float>();

        public bool EnFiebre => Time.time < finFiebre;

        void Awake()
        {
            viajes = new Viaje[bolas != null ? bolas.Length : 0];
            for (int i = 0; i < viajes.Length; i++)
            {
                viajes[i] = new Viaje();
                if (bolas[i] != null)
                {
                    bolas[i].isKinematic = true;
                    bolas[i].interpolation = RigidbodyInterpolation.Interpolate;
                    bolas[i].gameObject.SetActive(false);
                    var golpe = bolas[i].gameObject.AddComponent<GolpeBola>();
                    golpe.dueno = this;
                }
            }
            distancias = new float[camino != null ? camino.Length : 0];
            for (int i = 1; i < distancias.Length; i++) distancias[i] = distancias[i - 1] + Vector3.Distance(camino[i - 1], camino[i]);
        }

        public void Empezar(float retraso)
        {
            activo = true;
            siguiente = Time.time + retraso;
        }

        public void Parar()
        {
            activo = false;
            for (int i = 0; i < viajes.Length; i++) Apagar(i);
        }

        /// <summary>Fiebre del polen: el doble de bolas un rato.</summary>
        public void Fiebre(float segundos) => finFiebre = Time.time + segundos;

        void FixedUpdate()
        {
            if (distancias.Length < 2) return;
            if (activo && Time.time >= siguiente)
            {
                Soltar();
                siguiente = Time.time + cadencia * (EnFiebre ? 0.5f : 1f) * Random.Range(0.8f, 1.2f);
            }
            float total = distancias[distancias.Length - 1];
            for (int i = 0; i < viajes.Length; i++)
            {
                var v = viajes[i];
                if (!v.activa) continue;
                v.recorrido += velocidad * Time.fixedDeltaTime;
                if (v.recorrido >= total)
                {
                    Efectos.Polvo(bolas[i].position, 1f);
                    Apagar(i);
                    continue;
                }
                Muestra(v.recorrido, out Vector3 centro, out Vector3 lado, out float w);
                Vector3 p = centro + lado * (v.carril * w / 6f) + Vector3.up * radioBola;
                Vector3 paso = p - bolas[i].position;
                bolas[i].MovePosition(p);
                // Que ruede: gira sobre el eje perpendicular al avance.
                if (paso.sqrMagnitude > 0.0001f)
                {
                    Vector3 eje = Vector3.Cross(Vector3.up, paso.normalized);
                    bolas[i].MoveRotation(Quaternion.AngleAxis(paso.magnitude / radioBola * Mathf.Rad2Deg, eje) * bolas[i].rotation);
                }
            }
        }

        void Soltar()
        {
            for (int i = 0; i < viajes.Length; i++)
            {
                if (viajes[i].activa) continue;
                viajes[i].activa = true;
                viajes[i].recorrido = 0f;
                viajes[i].carril = Random.Range(-1f, 1f);
                Muestra(0f, out Vector3 centro, out _, out _);
                bolas[i].transform.position = centro + Vector3.up * radioBola;
                bolas[i].gameObject.SetActive(true);
                Sonidos.Reproducir(Sonido.Bote, centro, 0.8f, 0.7f);
                return;
            }
        }

        void Apagar(int i)
        {
            viajes[i].activa = false;
            if (bolas[i] != null) bolas[i].gameObject.SetActive(false);
        }

        void Muestra(float d, out Vector3 centro, out Vector3 lado, out float w)
        {
            int k = 1;
            while (k < distancias.Length - 1 && distancias[k] < d) k++;
            float t = Mathf.InverseLerp(distancias[k - 1], distancias[k], d);
            centro = Vector3.Lerp(camino[k - 1], camino[k], t);
            lado = Vector3.Lerp(fuera[k - 1], fuera[k], t).normalized;
            w = Mathf.Lerp(ancho[k - 1], ancho[k], t);
        }

        /// <summary>Atropello: el bicho sale rodando cuesta abajo sin control.</summary>
        internal void Golpear(Rigidbody bola, Collision choque)
        {
            var cuerpo = choque.rigidbody;
            var bicho = cuerpo != null ? cuerpo.GetComponent<ControladorBicho>() : null;
            if (bicho == null || bicho.Retirado) return;
            if (ultimoGolpe.TryGetValue(bicho, out float antes) && Time.time - antes < 1f) return;
            ultimoGolpe[bicho] = Time.time;

            Vector3 abajo = bicho.transform.position - bola.position;
            abajo.y = 0f;
            int i = System.Array.IndexOf(bolas, bola);
            if (i >= 0 && viajes[i].activa)
            {
                Muestra(viajes[i].recorrido + 2f, out Vector3 delante, out _, out _);
                Muestra(viajes[i].recorrido, out Vector3 aqui, out _, out _);
                abajo = (delante - aqui) + abajo.normalized * 0.5f;
                abajo.y = 0f;
            }
            Vector3 v = abajo.normalized * empujon;
            v.y = 3f;
            bicho.Cuerpo.linearVelocity = v;
            bicho.Tropezar(tropiezo);
            Sonidos.Reproducir(Sonido.Golpe, bicho.transform.position, bicho.EsBot ? 0.6f : 1f);
            Efectos.Impacto(bicho.Cuerpo.worldCenterOfMass, new Color(1f, 0.85f, 0.3f), 0.8f);
            if (!bicho.EsBot) Efectos.Sacudir(0.25f);
        }
    }
}
