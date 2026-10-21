using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "La Cumbre del Girasol", sección 2: el cañón de polen. Suelta bolas gigantes que bajan
    /// rodando por la pista siguiendo el camino de la espiral, por cualquier carril (no hay sitio
    /// seguro). Hay cuatro clases: normal, en zigzag (cambia de carril), saltarina (bota: se puede
    /// pasar por debajo cuando sube) y gigante (más lenta, tapa media pista). A veces salen dos
    /// a la vez. Quien se cruza con una rueda sin control cuesta abajo un par de segundos.
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
        [Tooltip("Ancho de la pista en cada punto (las bolas van por todo el ancho, sin tocar los bordes).")]
        public float[] ancho;
        public float radioBola = 1.2f;
        public float velocidad = 7f;
        [Tooltip("Segundos entre bola y bola (la mitad durante la fiebre del polen).")]
        public float cadencia = 3.5f;
        [Header("Clases de bola (probabilidades)")]
        [Range(0f, 1f)] public float zigzag = 0.3f;
        [Range(0f, 1f)] public float saltarina = 0.2f;
        [Range(0f, 1f)] public float gigante = 0.12f;
        [Tooltip("Probabilidad de que salgan dos bolas a la vez, por carriles distintos.")]
        [Range(0f, 1f)] public float pareja = 0.25f;
        public float escalaGigante = 1.6f;
        public float alturaBote = 2.6f;
        public float periodoBote = 1.2f;
        public float empujon = 8f;
        public float tropiezo = 2f;

        class Viaje
        {
            public float recorrido;   // metros desde el cañón
            public float carril;      // -1..1 de borde a borde
            public bool activa;
            public Clase clase;
            public float fase;
        }

        enum Clase { Normal, Zigzag, Saltarina, Gigante }

        Viaje[] viajes;
        Vector3[] escalaOriginal;
        float[] distancias;
        float siguiente;
        float finFiebre = -1f;
        bool activo;
        readonly Dictionary<ControladorBicho, float> ultimoGolpe = new Dictionary<ControladorBicho, float>();

        public bool EnFiebre => Time.time < finFiebre;

        void Awake()
        {
            viajes = new Viaje[bolas != null ? bolas.Length : 0];
            escalaOriginal = new Vector3[viajes.Length];
            for (int i = 0; i < viajes.Length; i++)
            {
                viajes[i] = new Viaje();
                if (bolas[i] != null)
                {
                    escalaOriginal[i] = bolas[i].transform.localScale;
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
                float carril = Soltar(float.NaN);
                if (Random.value < pareja) Soltar(carril);
                siguiente = Time.time + cadencia * (EnFiebre ? 0.5f : 1f) * Random.Range(0.8f, 1.2f);
            }
            float total = distancias[distancias.Length - 1];
            for (int i = 0; i < viajes.Length; i++)
            {
                var v = viajes[i];
                if (!v.activa) continue;
                v.recorrido += velocidad * (v.clase == Clase.Gigante ? 0.75f : 1f) * Time.fixedDeltaTime;
                if (v.recorrido >= total)
                {
                    Efectos.Polvo(bolas[i].position, 1f);
                    Apagar(i);
                    continue;
                }
                Muestra(v.recorrido, out Vector3 centro, out Vector3 lado, out float w);
                float radio = Radio(v);
                float carril = v.carril;
                if (v.clase == Clase.Zigzag) carril = Mathf.Sin((Time.time + v.fase) * Mathf.PI * 2f / 2.6f) * 0.85f;
                float alto = 0f;
                if (v.clase == Clase.Saltarina) alto = Mathf.Abs(Mathf.Sin((Time.time + v.fase) * Mathf.PI / periodoBote)) * alturaBote;
                float medio = Mathf.Max(0f, w * 0.5f - radio - 0.3f);
                Vector3 p = centro + lado * (carril * medio) + Vector3.up * (radio + alto);
                Vector3 paso = p - bolas[i].position;
                bolas[i].MovePosition(p);
                // Que ruede: gira sobre el eje perpendicular al avance.
                if (paso.sqrMagnitude > 0.0001f)
                {
                    Vector3 eje = Vector3.Cross(Vector3.up, paso.normalized);
                    bolas[i].MoveRotation(Quaternion.AngleAxis(paso.magnitude / radio * Mathf.Rad2Deg, eje) * bolas[i].rotation);
                }
            }
        }

        float Radio(Viaje v) => radioBola * (v.clase == Clase.Gigante ? escalaGigante : 1f);

        /// <summary>Suelta una bola (si queda alguna libre). Con "lejosDe", va por el otro lado de la pista. Devuelve su carril.</summary>
        float Soltar(float lejosDe)
        {
            for (int i = 0; i < viajes.Length; i++)
            {
                var v = viajes[i];
                if (v.activa) continue;
                v.activa = true;
                v.recorrido = 0f;
                v.fase = Random.Range(0f, 3f);
                float r = Random.value;
                v.clase = r < gigante ? Clase.Gigante : r < gigante + saltarina ? Clase.Saltarina : r < gigante + saltarina + zigzag ? Clase.Zigzag : Clase.Normal;
                if (!float.IsNaN(lejosDe) && v.clase == Clase.Gigante) v.clase = Clase.Normal; // dos gigantes taparían toda la pista
                v.carril = float.IsNaN(lejosDe) ? Random.Range(-1f, 1f) : -Mathf.Sign(lejosDe == 0f ? 1f : lejosDe) * Random.Range(0.5f, 1f);
                bolas[i].transform.localScale = escalaOriginal[i] * (v.clase == Clase.Gigante ? escalaGigante : 1f);
                Muestra(0f, out Vector3 centro, out _, out _);
                bolas[i].transform.position = centro + Vector3.up * Radio(v);
                bolas[i].gameObject.SetActive(true);
                Sonidos.Reproducir(Sonido.Bote, centro, 0.8f, v.clase == Clase.Gigante ? 0.5f : 0.7f);
                return v.carril;
            }
            return 0f;
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
