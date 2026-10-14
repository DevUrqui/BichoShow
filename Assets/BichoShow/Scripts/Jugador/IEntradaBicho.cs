using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Fuente de controles alternativa al mando: si un componente del bicho implementa esto
    /// (por ejemplo un bot), ControladorBicho lo lee en vez del PlayerInput.
    /// </summary>
    public interface IEntradaBicho
    {
        /// <summary>Stick de movimiento, igual que el del mando (relativo a la cámara).</summary>
        Vector2 Mover { get; }
        /// <summary>Botón de agarrar mantenido.</summary>
        bool Agarrar { get; }
        /// <summary>Devuelve true una vez por cada pulsación de saltar.</summary>
        bool ConsumirSaltar();
        /// <summary>Devuelve true una vez por cada pulsación de lanzarse.</summary>
        bool ConsumirLanzarse();
    }
}
