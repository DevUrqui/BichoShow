using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Paso de la final: el camino de baldosas. Uno se queda en la orilla saltando de placa en
    /// placa para encender el tramo por el que va su compañero; el que ha cruzado pisa la placa
    /// del otro lado (enciende todo) para que pase el primero.
    /// Bots: el que va delante cruza tramo a tramo (espera al final del tramo si el siguiente no
    /// está); el otro pisa la placa del tramo que toca según por dónde va el que cruza.
    /// </summary>
    public class PasoBaldosas : PasoCooperativo
    {
        public CaminoBaldosas camino;
        [Tooltip("X del camino de baldosas.")]
        public float xCamino;
        [Tooltip("Z donde empieza y acaba el hueco.")]
        public float zInicioHueco, zFinHueco;
        [Tooltip("A dónde ir cuando han cruzado los dos.")]
        public Vector3 salida;

        int Tramos => camino != null && camino.tramos != null ? camino.tramos.Length : 0;
        float LargoTramo => (zFinHueco - zInicioHueco) / Mathf.Max(1, Tramos);
        bool Cruzado(ControladorBicho b) => b.transform.position.z > zFinHueco + 0.3f;
        bool EnHueco(ControladorBicho b) => b.transform.position.z > zInicioHueco - 0.2f && !Cruzado(b);

        /// <summary>En qué tramo está (‑1 = en la orilla de salida).</summary>
        int TramoDe(ControladorBicho b)
        {
            float z = b.transform.position.z;
            if (z < zInicioHueco) return -1;
            return Mathf.Clamp(Mathf.FloorToInt((z - zInicioHueco) / LargoTramo), 0, Tramos - 1);
        }

        // El que espera está pisando la placa lejana: sin saltitos (en el aire se apagaría el camino).

        public override Vector3 ObjetivoBot(ControladorBicho bot, int rol, ControladorBicho companero, out float intensidad)
        {
            intensidad = 1f;
            Vector3 orilla = new Vector3(xCamino, 0f, zInicioHueco - 0.9f);
            Vector3 otraOrilla = new Vector3(xCamino, 0f, zFinHueco + 1.4f);

            if (Cruzado(bot))
            {
                if (companero == null || Cruzado(companero)) return salida;
                // Encender todo el camino para el compañero.
                Vector3 lejos = camino != null && camino.placaLejos != null ? camino.placaLejos.transform.position : otraOrilla;
                if (Plano(bot.transform.position - lejos).magnitude < 0.4f) intensidad = 0f;
                return lejos;
            }

            // ¿Me toca cruzar? Si soy el rol 0, si ya estoy en el hueco o si mi compañero ya ha cruzado.
            bool cruzo = companero == null || Cruzado(companero) || EnHueco(bot) || (rol == 0 && !EnHueco(companero));
            if (cruzo)
            {
                int t = TramoDe(bot);
                // Avanzar mientras el tramo siguiente esté encendido.
                int hasta = t;
                while (hasta + 1 < Tramos && camino != null && camino.Visible(hasta + 1)) hasta++;
                if (hasta >= Tramos - 1 && (camino == null || camino.Visible(Tramos - 1))) return otraOrilla;
                if (hasta < 0) { intensidad = Plano(bot.transform.position - orilla).magnitude < 0.5f ? 0f : 1f; return orilla; }
                Vector3 finTramo = new Vector3(xCamino, 0f, zInicioHueco + LargoTramo * (hasta + 1) - 0.7f);
                if (Plano(bot.transform.position - finTramo).magnitude < 0.4f) intensidad = 0f;
                return finTramo;
            }

            // Me quedo en la orilla: la placa del tramo por el que va mi compañero (o el siguiente,
            // si ya está llegando al final del suyo).
            int tc = TramoDe(companero);
            int placa = Mathf.Max(0, tc);
            if (tc >= 0)
            {
                float dentro = (companero.transform.position.z - zInicioHueco) - LargoTramo * tc;
                if (dentro > LargoTramo * 0.55f) placa = tc + 1;
            }
            else if (companero.transform.position.z > zInicioHueco - 1.6f) placa = 0;
            placa = Mathf.Clamp(placa, 0, (camino != null && camino.placas != null ? camino.placas.Length : 1) - 1);
            Vector3 destino = camino != null && camino.placas != null && camino.placas.Length > placa && camino.placas[placa] != null
                ? camino.placas[placa].transform.position : orilla;
            if (Plano(bot.transform.position - destino).magnitude < 0.35f) intensidad = 0f;
            return destino;
        }
    }
}
