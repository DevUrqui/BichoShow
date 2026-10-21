using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Despensa Deslizante", zona 2: grifo cromado gigante en el borde del fregadero. Su caño
    /// largo gira y, cada pocos segundos, abre un chorro a presión que cae en vertical y barre el
    /// fregadero de un lado a otro: a quien pilla lo empuja de lado (al agua, si está parado
    /// dudando al borde de una esponja). Antes de abrir, gotea y suena.
    /// </summary>
    public class ChorroGrifo : MonoBehaviour
    {
        [Tooltip("El caño, que gira sobre el eje vertical del grifo (este objeto).")]
        public Transform cano;
        [Tooltip("La columna de agua (se enciende durante el barrido). Cuelga de la punta del caño.")]
        public GameObject columna;
        [Tooltip("Gotas antes de abrir.")]
        public ParticleSystem gotas;
        [Tooltip("Largo del caño (m): la punta, desde donde cae el chorro.")]
        public float largo = 16f;
        [Tooltip("Giro (grados) del caño en un extremo y en el otro del barrido.")]
        public float anguloA = -50f;
        public float anguloB = 50f;
        public float periodo = 6f;
        public float duracionBarrido = 2.2f;
        public float aviso = 0.9f;
        [Tooltip("Radio de la columna de agua (m) y altura hasta la que empuja (desde la base del grifo).")]
        public float radioChorro = 1.1f;
        public float alturaChorro = 4f;
        public float empujon = 10f;
        [Tooltip("Retraso del primer barrido (s), para que dos grifos no vayan a la par.")]
        public float desfase;

        float inicioCiclo;
        bool haciaB = true, abierto;
        float anguloAnterior;

        void Start()
        {
            inicioCiclo = Time.time + desfase;
            Girar(anguloA);
            if (columna != null) columna.SetActive(false);
        }

        void Girar(float grados)
        {
            anguloAnterior = grados;
            if (cano != null) cano.localRotation = Quaternion.Euler(0f, grados, 0f);
        }

        Vector3 Punta => cano != null ? cano.position + cano.forward * largo : transform.position;

        void Update()
        {
            float c = Time.time - inicioCiclo;
            if (c < 0f) return;
            float espera = periodo - duracionBarrido;
            float desde = haciaB ? anguloA : anguloB, hasta = haciaB ? anguloB : anguloA;
            if (c < espera)
            {
                if (abierto) Abrir(false);
                if (c > espera - aviso && gotas != null && !gotas.isPlaying)
                {
                    gotas.Play();
                    Sonidos.Reproducir(Sonido.Click, transform.position, 0.6f, 0.7f);
                }
                return;
            }
            if (c < periodo)
            {
                if (!abierto) Abrir(true);
                float k = Mathf.SmoothStep(0f, 1f, (c - espera) / duracionBarrido);
                Girar(Mathf.Lerp(desde, hasta, k));
                return;
            }
            // Fin del barrido: se queda en ese lado y el siguiente va al revés.
            Girar(hasta);
            haciaB = !haciaB;
            inicioCiclo = Time.time;
            Abrir(false);
        }

        void Abrir(bool abrir)
        {
            abierto = abrir;
            if (columna != null) columna.SetActive(abrir);
            if (abrir) Sonidos.Reproducir(Sonido.Ola, transform.position, 1f, 1.2f);
        }

        void FixedUpdate()
        {
            if (!abierto || cano == null) return;
            Vector3 punta = Punta;
            // Hacia dónde barre ahora la punta (tangente del giro).
            Vector3 barre = Vector3.Cross(Vector3.up, cano.forward) * (haciaB == (anguloB > anguloA) ? 1f : -1f);
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado) continue;
                Vector3 p = b.transform.position;
                if (p.y > transform.position.y + alturaChorro || p.y < transform.position.y - 4f) continue;
                if (new Vector2(p.x - punta.x, p.z - punta.z).sqrMagnitude > radioChorro * radioChorro) continue;
                var cuerpo = b.Cuerpo;
                cuerpo.linearVelocity = barre * empujon + Vector3.up * 2f;
                b.Tropezar(0.6f);
                Sonidos.Reproducir(Sonido.Ola, p, b.EsBot ? 0.5f : 0.9f, 1.5f);
            }
        }
    }
}
