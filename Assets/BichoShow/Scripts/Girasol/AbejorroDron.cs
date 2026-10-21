using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "La Cumbre del Girasol": abejorro con cámara de tele. Revolotea por lo alto del invernadero
    /// y, cuando alguien va muy destacado, se le planta delante para grabarlo (RondaCumbre le
    /// dice a quién). Las alas aletean solas.
    /// </summary>
    public class AbejorroDron : MonoBehaviour
    {
        [Tooltip("Dónde revolotea cuando no graba a nadie.")]
        public Vector3 reposo = new Vector3(0f, 30f, 0f);
        public Vector3 ejeTallo = Vector3.zero;
        public Transform alaIzquierda, alaDerecha;
        public float suavizado = 0.8f;

        ControladorBicho objetivo;
        Vector3 velocidad;
        Vector3 rumboObjetivo = Vector3.forward;

        public void Grabar(ControladorBicho bicho) => objetivo = bicho;

        void Update()
        {
            Vector3 destino;
            Vector3 mirar;
            if (objetivo != null && !objetivo.Retirado)
            {
                // Por delante del bicho (hacia donde corre) y un poco por fuera y por encima, mirándolo:
                // así sale en el plano a su lado, sin taparlo desde la cámara.
                Vector3 p = objetivo.transform.position;
                Vector3 fuera = new Vector3(p.x - ejeTallo.x, 0f, p.z - ejeTallo.z).normalized;
                Vector3 v = objetivo.Velocidad;
                v.y = 0f;
                if (v.sqrMagnitude > 1f) rumboObjetivo = Vector3.Slerp(rumboObjetivo, v.normalized, 1f - Mathf.Exp(-3f * Time.deltaTime));
                destino = p + rumboObjetivo * 4.5f + fuera * 1.2f + Vector3.up * 1.8f;
                mirar = p + Vector3.up * 0.8f;
            }
            else
            {
                float a = Time.time * 0.25f;
                destino = reposo + new Vector3(Mathf.Cos(a), 0.3f * Mathf.Sin(a * 3f), Mathf.Sin(a)) * 14f;
                mirar = ejeTallo + Vector3.up * reposo.y * 0.8f;
            }
            destino += Vector3.up * Mathf.Sin(Time.time * 3f) * 0.15f; // revoloteo
            transform.position = Vector3.SmoothDamp(transform.position, destino, ref velocidad, suavizado);
            Vector3 hacia = mirar - transform.position;
            if (hacia.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(hacia), 1f - Mathf.Exp(-4f * Time.deltaTime));

            float aleteo = Mathf.Sin(Time.time * 60f) * 35f;
            if (alaIzquierda != null) alaIzquierda.localRotation = Quaternion.Euler(0f, 0f, aleteo);
            if (alaDerecha != null) alaDerecha.localRotation = Quaternion.Euler(0f, 0f, -aleteo);
        }
    }
}
