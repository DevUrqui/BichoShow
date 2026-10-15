using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Paso 4: el ascensor solo sube con la pareja encima y lleva a la meta, en lo alto.
    /// Superado cuando toda la pareja está en la zona de meta.
    /// </summary>
    public class PasoAscensor : PasoCooperativo
    {
        public Ascensor ascensor;
        [Tooltip("Zona de la meta (cuenta solo a los de este equipo).")]
        public PlacaPresion meta;

        public override bool Completado(List<ControladorBicho> miembros) =>
            miembros.Count > 0 && meta != null && meta.Contados >= miembros.Count;

        public override Vector3 ObjetivoBot(ControladorBicho bot, int rol, ControladorBicho companero, out float intensidad)
        {
            intensidad = 1f;
            Vector3 centroMeta = meta != null ? meta.transform.position : bot.transform.position;
            if (ascensor == null) return centroMeta;

            // Ya arriba (en el ascensor subido o en la repisa): a la meta.
            if (bot.transform.position.y > ascensor.AlturaArriba - 0.8f) return centroMeta;

            // Abajo: subirse al ascensor, cada uno en su lado, y esperar quieto.
            Vector3 lateral = Vector3.right * (rol == 0 ? -0.8f : 0.8f);
            Vector3 sitio = Plano(ascensor.transform.position) + lateral;
            if (Plano(bot.transform.position - sitio).magnitude < 0.5f) intensidad = 0f;
            return sitio;
        }
    }
}
