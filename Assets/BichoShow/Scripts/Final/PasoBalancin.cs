using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Paso de la final: el balancín. Uno cruza mientras el otro hace de contrapeso (en el lado
    /// contrario de la tabla, a la misma distancia del eje). Al llegar, el primero pisa el freno
    /// (la tabla se queda recta) y pasa el segundo.
    /// Bots: el que cruza avanza, pero se para si la tabla se le inclina hacia delante; el
    /// contrapeso se coloca en el espejo del que cruza.
    /// </summary>
    public class PasoBalancin : PasoCooperativo
    {
        public Balancin balancin;
        [Tooltip("X del balancín.")]
        public float xTabla;
        [Tooltip("Z del principio y del final del hueco.")]
        public float zInicioHueco, zFinHueco;
        [Tooltip("A dónde ir cuando han cruzado los dos.")]
        public Vector3 salida;

        bool Cruzado(ControladorBicho b) => b.transform.position.z > zFinHueco + 0.4f;
        bool EnTabla(ControladorBicho b) => b.transform.position.z > zInicioHueco - 0.3f && !Cruzado(b);
        float ZEje => balancin != null ? balancin.transform.position.z : (zInicioHueco + zFinHueco) * 0.5f;

        // El que espera está pisando el freno: sin saltitos (en el aire se soltaría).

        public override Vector3 ObjetivoBot(ControladorBicho bot, int rol, ControladorBicho companero, out float intensidad)
        {
            intensidad = 1f;
            Vector3 orilla = new Vector3(xTabla, 0f, zInicioHueco - 1f);
            Vector3 otraOrilla = new Vector3(xTabla, 0f, zFinHueco + 1.6f);

            if (Cruzado(bot))
            {
                if (companero == null || Cruzado(companero)) return salida;
                // Frenar la tabla para el compañero.
                Vector3 freno = balancin != null && balancin.freno != null ? balancin.freno.transform.position : otraOrilla;
                if (Plano(bot.transform.position - freno).magnitude < 0.4f) intensidad = 0f;
                return freno;
            }

            bool frenado = balancin == null || balancin.Frenado;
            float angulo = balancin != null ? balancin.Angulo : 0f;

            // Antes de subir a la tabla, ponerse en línea con ella (es estrecha: en diagonal se cae).
            bool fueraDeLinea = !EnTabla(bot) && Mathf.Abs(bot.transform.position.x - xTabla) > 0.5f;

            // Con la tabla frenada (o sin compañero): cruzar sin más.
            if (companero == null || Cruzado(companero))
                return frenado && !fueraDeLinea ? otraOrilla : orilla;

            bool cruzo = rol == 0 ? !(EnTabla(companero) && companero.transform.position.z > bot.transform.position.z + 1f)
                                  : (EnTabla(bot) && bot.transform.position.z > companero.transform.position.z + 1f);
            if (cruzo)
            {
                if (fueraDeLinea) return orilla;
                // Avanzar; si la tabla se inclina hacia delante, esperar a que el otro equilibre.
                intensidad = angulo > 5f ? 0f : 0.7f;
                return otraOrilla;
            }

            // Contrapeso: en el espejo del que cruza (sin pasarse del principio de la tabla).
            float eje = ZEje;
            float espejo = eje - (companero.transform.position.z - eje);
            espejo = Mathf.Clamp(espejo, zInicioHueco + 0.6f, eje);
            if (!EnTabla(companero)) espejo = zInicioHueco - 1f; // el otro aún no ha subido: esperar en la orilla
            Vector3 destino = new Vector3(xTabla, 0f, espejo);
            if (Plano(bot.transform.position - destino).magnitude < 0.5f) intensidad = 0f;
            return destino;
        }
    }
}
