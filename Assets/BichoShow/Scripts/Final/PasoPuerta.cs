using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Paso 3: una puerta que solo se abre pisando a la vez dos placas muy separadas. Abierta
    /// unos segundos: hay que correr. Bots: rol 0 a la placa A, rol 1 a la B.
    /// </summary>
    public class PasoPuerta : PasoCooperativo
    {
        public PuertaDoble puerta;
        public PlacaPresion placaA, placaB;
        [Tooltip("A dónde correr cuando la puerta se abre (pasada la puerta).")]
        public Vector3 salida;

        public override Vector3 ObjetivoBot(ControladorBicho bot, int rol, ControladorBicho companero, out float intensidad)
        {
            intensidad = 1f;
            if (puerta != null && (puerta.Abierta || puerta.SegundosAbierta > 0.5f)) return salida;

            var placa = companero == null || rol == 0 ? placaA : placaB;
            Vector3 destino = placa != null ? placa.transform.position : salida;
            if (Plano(bot.transform.position - destino).magnitude < 0.4f) intensidad = 0f; // ya encima: quieto
            return destino;
        }
    }
}
