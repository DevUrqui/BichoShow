using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// Las rondas y el show avisan aquí de lo que cuenta para el pase (lo llaman en el ordenador
    /// que lleva la partida). Solo avanza el jugador 1 de cada ordenador, el dueño de la cuenta:
    /// si es de aquí, se apunta; si juega desde fuera (online), se le manda a su ordenador.
    /// </summary>
    public static class EventosPase
    {
        public static void Notificar(ControladorBicho bicho, string evento, int cantidad = 1)
        {
            if (bicho == null || bicho.EsBot || cantidad <= 0) return;
            if (bicho.TryGetComponent(out EntradaRemota remota))
            {
                if (remota.Local == 0) RedShow.EnviarPase(remota.Cliente, evento, cantidad);
                return;
            }
            if (bicho.TryGetComponent(out PlayerInput entrada) && entrada.playerIndex == 0)
                ProgresoPase.Evento(evento, cantidad);
        }

        /// <summary>El bicho del jugador con ese número (0 a 3), o null.</summary>
        public static ControladorBicho BichoDe(int indice)
        {
            foreach (var b in ControladorBicho.Todos)
                if (b != null && b.IndiceJugador == indice) return b;
            return null;
        }
    }
}
