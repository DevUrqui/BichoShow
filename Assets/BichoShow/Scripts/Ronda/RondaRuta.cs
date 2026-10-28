using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Base de las pruebas de carrera (Costura a la fuga, La Cumbre del Girasol): un camino de
    /// PuntoRuta que siguen los bots (BotCarrera) y una forma de saber quién ya ha llegado.
    /// </summary>
    public abstract class RondaRuta : RondaBase
    {
        [Header("Ruta de los bots")]
        [Tooltip("Todos los puntos del camino de los bots.")]
        public PuntoRuta[] ruta;
        [Tooltip("Por dónde empiezan los bots.")]
        public PuntoRuta inicioRuta;

        /// <summary>Id de esta carrera en el ranking de tiempos (RankingTiempos.Carreras); null = sin ranking.</summary>
        public virtual string IdRanking => null;

        /// <summary>
        /// Al cruzar la meta: el tiempo desde la salida va al ranking (los bots no cuentan). Si es un
        /// jugador de fuera (online), se le manda a su ordenador, que lo apunta con su nombre.
        /// </summary>
        protected void ApuntarTiempo(ControladorBicho b)
        {
            if (b != null) EventosPase.Notificar(b, Temporada.Eventos.Meta);
            if (IdRanking == null || b == null || b.EsBot) return;
            float segundos = Time.time - InicioJugando;
            string nombre = NombreJugador(b);
            string bicho = b.TryGetComponent(out PersonajeBicho p) ? PersonajeBicho.Especies[p.Especie] : PersonajeBicho.Especies[0];
            Color color = gestor != null ? gestor.ColorDe(b.IndiceJugador) : Color.white;
            if (b.TryGetComponent(out EntradaRemota remota))
            {
                RedShow.EnviarTiempo(remota.Cliente, remota.Local, IdRanking, segundos, bicho);
                Mensaje($"{nombre}: {RankingTiempos.Formato(segundos)}", color, 2.5f);
                return;
            }
            int puesto = RankingTiempos.Apuntar(IdRanking, nombre, segundos, bicho);
            if (puesto == 1) Mensaje($"¡NUEVO RÉCORD! {nombre}: {RankingTiempos.Formato(segundos)}", EstiloShow.Oro, 3.5f);
            else if (puesto > 0) Mensaje($"{nombre}: {RankingTiempos.Formato(segundos)}  ·  {puesto}º del ranking", color, 3f);
            else Mensaje($"{nombre}: {RankingTiempos.Formato(segundos)}", color, 2.5f);
        }

        /// <summary>Este bicho ya ha terminado la carrera (los bots dejan de correr).</summary>
        public abstract bool HaLlegado(ControladorBicho b);

        /// <summary>Cuánto ha avanzado un punto en la carrera (más = más cerca de la meta).</summary>
        protected virtual float Avance(Vector3 punto) => punto.z;

        public float AvanceDe(Vector3 punto) => Avance(punto);

        /// <summary>El punto del camino más cercano (prefiere los de delante): para seguir tras caerse o salir volando.</summary>
        public PuntoRuta PuntoRutaCercano(Vector3 posicion)
        {
            PuntoRuta mejor = inicioRuta;
            float mejorCoste = float.MaxValue;
            if (ruta == null) return mejor;
            float avanceAqui = Avance(posicion);
            foreach (var p in ruta)
            {
                if (p == null) continue;
                Vector3 d = p.transform.position - posicion;
                float coste = d.magnitude + (Avance(p.transform.position) < avanceAqui - 1f ? 8f : 0f) + Mathf.Max(0f, d.y - 1f) * 4f;
                if (coste < mejorCoste) { mejorCoste = coste; mejor = p; }
            }
            return mejor;
        }
    }
}
