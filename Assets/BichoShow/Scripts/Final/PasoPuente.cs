using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Paso 2: puente levadizo sobre un hueco. Uno pisa la placa de este lado mientras el otro
    /// cruza; el que cruza pisa la placa del otro lado para que pase su compañero.
    /// Bots: rol 0 cruza primero, rol 1 sujeta. Se adaptan a lo que haga el compañero humano.
    /// </summary>
    public class PasoPuente : PasoCooperativo
    {
        public PuenteLevadizo puente;
        public PlacaPresion placaCerca, placaLejos;
        [Tooltip("Centro del puente a lo ancho (X).")]
        public float xCentro;
        public float zInicioHueco, zFinHueco;
        [Tooltip("A dónde ir cuando han cruzado los dos.")]
        public Vector3 salida;

        bool Cruzado(ControladorBicho b) => b.transform.position.z > zFinHueco + 0.3f;

        public override Vector3 ObjetivoBot(ControladorBicho bot, int rol, ControladorBicho companero, out float intensidad)
        {
            intensidad = 1f;
            Vector3 antesDelPuente = new Vector3(xCentro, 0f, zInicioHueco - 1.2f);
            Vector3 trasElPuente = new Vector3(xCentro, 0f, zFinHueco + 1.5f);
            Vector3 lejos = placaLejos != null ? placaLejos.transform.position : trasElPuente;
            Vector3 cerca = placaCerca != null ? placaCerca.transform.position : antesDelPuente;

            bool yoCruce = Cruzado(bot);
            bool elCruzo = companero == null || Cruzado(companero);

            // Solo en el equipo: el puente está siempre bajado.
            if (companero == null) return yoCruce ? salida : trasElPuente;

            if (yoCruce)
                return elCruzo ? salida : lejos; // espera pisando la placa lejana

            if (rol == 0 || elCruzo)
            {
                // Me toca cruzar: si el puente está bajado, adelante; si no, esperar en la orilla.
                if (puente != null && puente.Bajado) return trasElPuente;
                intensidad = Plano(bot.transform.position - antesDelPuente).magnitude < 0.6f ? 0f : 1f;
                return antesDelPuente;
            }

            // Rol 1 con el compañero aún sin cruzar: sujetar el puente desde la placa cercana.
            return cerca;
        }
    }
}
