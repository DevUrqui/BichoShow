using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Da vida al modelo del personaje sin animaciones hechas a mano: todo procedural y
    /// guiado por la física del bicho.
    ///  - Las botas caminan según la velocidad (zancada y altura del paso) y cuelgan en el aire.
    ///  - Las manos se balancean al revés que los pies; al agarrar, las dos manoplas se estiran.
    ///  - Brazos y piernas son tubos "de fideo" entre el cuerpo y las manos / botas.
    ///  - El torso rebota al andar; parpadea de vez en cuando; al caer, agita brazos y piernas.
    /// Las referencias las pone la herramienta BichoShow > Personaje.
    /// </summary>
    [RequireComponent(typeof(ControladorBicho))]
    public class AspectoBicho : MonoBehaviour
    {
        [Header("Piezas (las coloca la herramienta)")]
        public Transform torso;
        public Transform manoIzq, manoDer;
        public Transform pieIzq, pieDer;
        [Tooltip("Grupos de cada ojo (blanco, pupila y brillo) para parpadear.")]
        public Transform[] ojos;
        public MeshFilter brazoIzq, brazoDer, piernaIzq, piernaDer;

        [Header("Anclajes (lado derecho; el izquierdo es su espejo)")]
        public Vector3 hombro = new Vector3(0.27f, 1.05f, 0f);
        public Vector3 cadera = new Vector3(0.09f, 0.74f, 0f);
        [Tooltip("Altura del tobillo sobre el origen de la bota.")]
        public float alturaTobillo = 0.22f;
        public float radioBrazo = 0.036f;
        public float radioPierna = 0.038f;

        [Header("Andar")]
        [Tooltip("Pasos por metro recorrido (en radianes de ciclo).")]
        public float ritmoPaso = 2.4f;
        public float zancada = 0.17f;
        public float alturaPaso = 0.11f;
        public float balanceoBrazos = 0.16f;
        public float rebote = 0.035f;
        [Tooltip("Suavizado de manos y pies (más alto = más rápido).")]
        public float suavizado = 18f;

        ControladorBicho bicho;
        Rigidbody rb;
        TuboProcedural tuboBrazoIzq, tuboBrazoDer, tuboPiernaIzq, tuboPiernaDer;
        Vector3 manoIzqReposo, manoDerReposo, pieIzqReposo, pieDerReposo, torsoReposo;
        Vector3[] escalaOjos;
        float fase;
        float siguienteParpadeo, finParpadeo;

        void Awake()
        {
            bicho = GetComponent<ControladorBicho>();
            rb = GetComponent<Rigidbody>();

            if (manoIzq != null) manoIzqReposo = manoIzq.localPosition;
            if (manoDer != null) manoDerReposo = manoDer.localPosition;
            if (pieIzq != null) pieIzqReposo = pieIzq.localPosition;
            if (pieDer != null) pieDerReposo = pieDer.localPosition;
            if (torso != null) torsoReposo = torso.localPosition;

            if (ojos != null)
            {
                escalaOjos = new Vector3[ojos.Length];
                for (int i = 0; i < ojos.Length; i++)
                    if (ojos[i] != null) escalaOjos[i] = ojos[i].localScale;
            }

            tuboBrazoIzq = Preparar(brazoIzq, "BrazoIzq");
            tuboBrazoDer = Preparar(brazoDer, "BrazoDer");
            tuboPiernaIzq = Preparar(piernaIzq, "PiernaIzq");
            tuboPiernaDer = Preparar(piernaDer, "PiernaDer");

            siguienteParpadeo = Time.time + Random.Range(1f, 4f);
            fase = Random.value * Mathf.PI * 2f;
        }

        static TuboProcedural Preparar(MeshFilter filtro, string nombre)
        {
            if (filtro == null) return null;
            var tubo = new TuboProcedural(nombre);
            filtro.sharedMesh = tubo.Malla;
            return tubo;
        }

        void LateUpdate()
        {
            if (bicho == null || bicho.Retirado) return;
            float dt = Time.deltaTime;
            float k = 1f - Mathf.Exp(-suavizado * dt);

            Vector3 velocidadLocal = transform.InverseTransformDirection(rb != null ? rb.linearVelocity : Vector3.zero);
            float rapidez = new Vector2(velocidadLocal.x, velocidadLocal.z).magnitude;
            float amplitud = Mathf.Clamp01(rapidez / 4f);
            fase += rapidez * ritmoPaso * dt;

            bool enSuelo = bicho.EnSuelo;
            bool tumbado = bicho.Tumbado;

            // --- Torso: rebota un poco con cada paso ---
            Vector3 botTorso = Vector3.zero;
            if (torso != null)
            {
                botTorso = Vector3.up * Mathf.Abs(Mathf.Sin(fase)) * rebote * amplitud;
                torso.localPosition = Vector3.Lerp(torso.localPosition, torsoReposo + botTorso, k);
            }

            // --- Pies ---
            MoverPie(pieIzq, pieIzqReposo, fase, enSuelo, tumbado, amplitud, -1f, k);
            MoverPie(pieDer, pieDerReposo, fase + Mathf.PI, enSuelo, tumbado, amplitud, 1f, k);

            // --- Manos ---
            if (bicho.ManoEstirada)
            {
                // Las dos manoplas al punto de agarre, una a cada lado.
                Vector3 punto = transform.InverseTransformPoint(bicho.PuntoMano);
                Vector3 lado = Vector3.right * 0.14f;
                if (manoIzq != null) manoIzq.localPosition = Vector3.Lerp(manoIzq.localPosition, punto - lado, k);
                if (manoDer != null) manoDer.localPosition = Vector3.Lerp(manoDer.localPosition, punto + lado, k);
            }
            else
            {
                MoverMano(manoIzq, manoIzqReposo, fase + Mathf.PI, enSuelo, tumbado, amplitud, -1f, k);
                MoverMano(manoDer, manoDerReposo, fase, enSuelo, tumbado, amplitud, 1f, k);
            }

            // --- Fideos ---
            ActualizarBrazo(tuboBrazoIzq, -1f, manoIzq, botTorso);
            ActualizarBrazo(tuboBrazoDer, 1f, manoDer, botTorso);
            ActualizarPierna(tuboPiernaIzq, -1f, pieIzq, botTorso);
            ActualizarPierna(tuboPiernaDer, 1f, pieDer, botTorso);

            Parpadear();
        }

        void MoverPie(Transform pie, Vector3 reposo, float fasePie, bool enSuelo, bool tumbado, float amplitud, float lado, float k)
        {
            if (pie == null) return;
            Vector3 objetivo = reposo;
            if (tumbado)
            {
                // Pataleo
                objetivo += new Vector3(0f, 0.08f + Mathf.Max(0f, Mathf.Sin(Time.time * 16f + lado)) * 0.1f, Mathf.Sin(Time.time * 13f + lado * 2f) * 0.12f);
            }
            else if (!enSuelo)
            {
                // En el aire: piernas colgando, un poco hacia atrás
                objetivo += new Vector3(0f, 0.1f, -0.08f);
            }
            else
            {
                objetivo.z += Mathf.Sin(fasePie) * zancada * amplitud;
                objetivo.y += Mathf.Max(0f, Mathf.Cos(fasePie)) * alturaPaso * amplitud;
            }
            pie.localPosition = Vector3.Lerp(pie.localPosition, objetivo, k);
        }

        void MoverMano(Transform mano, Vector3 reposo, float faseMano, bool enSuelo, bool tumbado, float amplitud, float lado, float k)
        {
            if (mano == null) return;
            Vector3 objetivo = reposo;
            if (tumbado)
            {
                // Agita los brazos hacia arriba
                objetivo = new Vector3(reposo.x * 1.25f, reposo.y + 0.55f + Mathf.Sin(Time.time * 15f + lado) * 0.12f, reposo.z + Mathf.Cos(Time.time * 11f + lado) * 0.1f);
            }
            else if (!enSuelo)
            {
                // Saltando: brazos arriba
                objetivo = new Vector3(reposo.x * 1.15f, reposo.y + 0.35f, reposo.z);
            }
            else
            {
                objetivo.z += Mathf.Sin(faseMano) * balanceoBrazos * amplitud;
                objetivo.y += Mathf.Abs(Mathf.Sin(faseMano)) * 0.03f * amplitud + Mathf.Sin(Time.time * 2f + lado) * 0.008f;
            }
            mano.localPosition = Vector3.Lerp(mano.localPosition, objetivo, k);
        }

        void ActualizarBrazo(TuboProcedural tubo, float lado, Transform mano, Vector3 botTorso)
        {
            if (tubo == null || mano == null) return;
            Vector3 inicio = new Vector3(hombro.x * lado, hombro.y, hombro.z) + botTorso;
            Vector3 fin = mano.localPosition;
            // Codo hacia fuera y un poco caído: forma de fideo blando.
            Vector3 c1 = inicio + new Vector3(0.2f * lado, 0.03f, 0f);
            Vector3 c2 = fin + new Vector3(0f, 0.14f, 0f);
            tubo.Actualizar(inicio, c1, c2, fin, radioBrazo, radioBrazo * 0.9f);
        }

        void ActualizarPierna(TuboProcedural tubo, float lado, Transform pie, Vector3 botTorso)
        {
            if (tubo == null || pie == null) return;
            Vector3 inicio = new Vector3(cadera.x * lado, cadera.y, cadera.z) + botTorso;
            Vector3 fin = pie.localPosition + Vector3.up * alturaTobillo;
            Vector3 c1 = inicio + new Vector3(0.05f * lado, -0.15f, 0f);
            Vector3 c2 = fin + new Vector3(0f, 0.12f, 0f);
            tubo.Actualizar(inicio, c1, c2, fin, radioPierna, radioPierna);
        }

        void Parpadear()
        {
            if (ojos == null || escalaOjos == null) return;
            if (Time.time >= siguienteParpadeo)
            {
                finParpadeo = Time.time + 0.12f;
                siguienteParpadeo = Time.time + Random.Range(2.5f, 5.5f);
            }
            bool cerrado = Time.time < finParpadeo || bicho.Tumbado;
            for (int i = 0; i < ojos.Length; i++)
            {
                if (ojos[i] == null) continue;
                Vector3 e = escalaOjos[i];
                ojos[i].localScale = cerrado ? new Vector3(e.x, e.y * 0.12f, e.z) : e;
            }
        }
    }
}
