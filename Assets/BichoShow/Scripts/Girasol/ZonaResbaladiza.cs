using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "La Cumbre del Girasol", sección 3: el rocío y el néctar del tallo. Entre dos alturas y dentro
    /// de la pista (entre dos radios alrededor del tallo) casi no hay agarre: se patina en las curvas.
    /// </summary>
    public class ZonaResbaladiza : MonoBehaviour
    {
        public float alturaDesde = 27f;
        public float alturaHasta = 40f;
        public float radioDesde = 9f;
        public float radioHasta = 18f;
        [Range(0f, 1f)] public float agarre = 0.3f;

        void FixedUpdate()
        {
            Vector3 eje = transform.position;
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado || !b.EnSuelo) continue;
                Vector3 p = b.transform.position;
                if (p.y < alturaDesde || p.y > alturaHasta) continue;
                float r = new Vector2(p.x - eje.x, p.z - eje.z).magnitude;
                if (r < radioDesde || r > radioHasta) continue;
                b.Resbalar(agarre, 0.2f);
            }
        }
    }
}
