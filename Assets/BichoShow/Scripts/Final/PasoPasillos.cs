using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Paso de la final: los pasillos cruzados. Dos pasillos paralelos separados por una pared;
    /// las compuertas de cada pasillo las abren las placas del otro. Hay que avanzar a saltos de
    /// rana: uno pisa su placa para abrirle la compuerta al otro, que llega a la suya y le abre
    /// la siguiente al primero… Superado cuando los dos salen por el final (zFin).
    /// Bots: cada uno va por un pasillo (el que no lleve su compañero); si su siguiente compuerta
    /// está abierta, avanza hasta la siguiente placa; si no, espera quieto en la placa que pisa.
    /// </summary>
    public class PasoPasillos : PasoCooperativo
    {
        [Tooltip("Centro X de cada pasillo (A y B).")]
        public float xA, xB;
        [Tooltip("Z donde empiezan los pasillos.")]
        public float zEntrada;
        [Tooltip("Placas de cada pasillo, en orden (las de A abren compuertas de B y al revés).")]
        public PlacaPresion[] placasA, placasB;
        [Tooltip("Compuertas de cada pasillo, en orden.")]
        public Compuerta[] compuertasA, compuertasB;
        [Tooltip("A dónde ir al salir de los pasillos.")]
        public Vector3 salida;

        bool EnPasillo(ControladorBicho b, float x) =>
            b.transform.position.z > zEntrada - 0.5f && Mathf.Abs(b.transform.position.x - x) < 1.4f;

        /// <summary>El pasillo de este bot: el que no lleva su compañero (o, si no hay pista, según su rol).</summary>
        bool UsaA(ControladorBicho bot, int rol, ControladorBicho companero)
        {
            if (EnPasillo(bot, xA)) return true;
            if (EnPasillo(bot, xB)) return false;
            if (companero != null)
            {
                if (EnPasillo(companero, xA)) return false;
                if (EnPasillo(companero, xB)) return true;
            }
            return rol == 0;
        }

        // Esperando, no pide ayuda dando saltitos: en el aire deja de pisar la placa y se cierra la compuerta.

        public override string Estado(ControladorBicho bot)
        {
            var ronda = RondaFinal.Instancia;
            if (ronda == null) return pista;
            bool a = UsaA(bot, ronda.Rol(bot), ronda.Companero(bot));
            var (compuerta, _) = Siguiente(bot, a);
            return compuerta != null && !compuerta.Abierta && EnPlaca(bot, a) ? "pisando su placa, esperando su compuerta" : pista;
        }

        /// <summary>La siguiente compuerta del pasillo (delante del bot) y la placa que hay tras ella.</summary>
        (Compuerta compuerta, PlacaPresion placaTras) Siguiente(ControladorBicho bot, bool a)
        {
            var compuertas = a ? compuertasA : compuertasB;
            var placas = a ? placasA : placasB;
            float z = bot.transform.position.z;
            Compuerta siguiente = null;
            if (compuertas != null)
                foreach (var c in compuertas)
                    if (c != null && c.transform.position.z > z + 0.2f) { siguiente = c; break; }
            PlacaPresion placaTras = null;
            if (placas != null && siguiente != null)
                foreach (var p in placas)
                    if (p != null && p.transform.position.z > siguiente.transform.position.z) { placaTras = p; break; }
            return (siguiente, placaTras);
        }

        bool EnPlaca(ControladorBicho bot, bool a)
        {
            var placas = a ? placasA : placasB;
            if (placas == null) return false;
            foreach (var p in placas)
                if (p != null && Plano(bot.transform.position - p.transform.position).magnitude < 0.7f) return true;
            return false;
        }

        public override Vector3 ObjetivoBot(ControladorBicho bot, int rol, ControladorBicho companero, out float intensidad)
        {
            intensidad = 1f;
            if (bot.transform.position.z > zFin) return salida;

            bool a = UsaA(bot, rol, companero);
            float x = a ? xA : xB;
            var placas = a ? placasA : placasB;

            // Aún fuera de los pasillos: a la entrada del mío.
            if (!EnPasillo(bot, x)) return new Vector3(x, 0f, zEntrada + 0.6f);

            var (compuerta, placaTras) = Siguiente(bot, a);
            if (compuerta == null)
            {
                // Ya no me quedan compuertas. Si mi compañero sigue dentro, no suelto mi última
                // placa (es la que le abre la suya); si no, a la salida.
                bool dentro = companero != null && companero.transform.position.z < zFin && companero.transform.position.z > zEntrada - 1f;
                PlacaPresion ultima = null;
                if (placas != null)
                    foreach (var p in placas)
                        if (p != null && p.transform.position.z < bot.transform.position.z + 0.8f) ultima = p;
                // Solo si esa placa está pasada mi última compuerta (si no, volvería atrás y se liaría).
                float ultimaCompuerta = float.MinValue;
                var mias = a ? compuertasA : compuertasB;
                if (mias != null) foreach (var c in mias) if (c != null) ultimaCompuerta = Mathf.Max(ultimaCompuerta, c.transform.position.z);
                if (!dentro || ultima == null || ultima.transform.position.z < ultimaCompuerta) return new Vector3(x, 0f, zFin + 1.5f);
                if (Plano(bot.transform.position - ultima.transform.position).magnitude < 0.4f) intensidad = 0f;
                return ultima.transform.position;
            }

            if (compuerta.Abierta)
                return placaTras != null ? placaTras.transform.position : new Vector3(x, 0f, zFin + 1.5f);

            // Cerrada: esperar en la placa que tengo antes de ella (así le abro al otro la suya).
            PlacaPresion mia = null;
            if (placas != null)
                foreach (var p in placas)
                    if (p != null && p.transform.position.z < compuerta.transform.position.z) mia = p;
            Vector3 espera = mia != null ? mia.transform.position : new Vector3(x, 0f, compuerta.transform.position.z - 1.2f);
            if (Plano(bot.transform.position - espera).magnitude < 0.4f) intensidad = 0f;
            return espera;
        }
    }
}
