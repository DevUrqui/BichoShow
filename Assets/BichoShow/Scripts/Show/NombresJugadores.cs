using System.Text;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// El nombre que cada jugador se pone en el vestuario (se guarda en este ordenador). Sale en
    /// los avisos ("¡Ivan llega 1º!"), en el ranking de tiempos y, online, lo ven los demás.
    /// Quien no se ha puesto nombre es J1, J2…; los bots, Bot 1, Bot 2…
    /// </summary>
    public static class NombresJugadores
    {
        public const int Largo = 12;

        static string Clave(int jugador) => $"BichoShow.Nombre{jugador}";

        public static string PorDefecto(int jugador) => $"J{jugador + 1}";

        /// <summary>El nombre que se puso ese jugador de este ordenador ("" si ninguno).</summary>
        public static string Guardado(int jugador) => Limpiar(PlayerPrefs.GetString(Clave(jugador), ""));

        public static void Guardar(int jugador, string nombre)
        {
            PlayerPrefs.SetString(Clave(jugador), Limpiar(nombre));
            PlayerPrefs.Save();
        }

        /// <summary>Nombre de un jugador de este ordenador (o J1, J2… si no se ha puesto ninguno).</summary>
        public static string DeLocal(int jugador)
        {
            string n = Guardado(jugador);
            return n.Length > 0 ? n : PorDefecto(jugador);
        }

        /// <summary>Nombre de cualquier bicho: bot, jugador de fuera (online) o de este ordenador.</summary>
        public static string De(ControladorBicho b)
        {
            if (b == null) return "?";
            if (b.EsBot) return $"Bot {b.IndiceJugador + 1}";
            if (b.TryGetComponent(out EntradaRemota remota))
                return string.IsNullOrEmpty(remota.Nombre) ? PorDefecto(b.IndiceJugador) : remota.Nombre;
            return DeLocal(b.IndiceJugador);
        }

        /// <summary>Letras, números, espacios y poco más.</summary>
        public static bool Valido(char c) => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_' || c == '.';

        /// <summary>Sin caracteres raros, sin espacios de sobra y como mucho <see cref="Largo"/> letras.</summary>
        public static string Limpiar(string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return "";
            var sb = new StringBuilder(Largo);
            foreach (char c in nombre)
            {
                if (!Valido(c)) continue;
                if (c == ' ' && (sb.Length == 0 || sb[sb.Length - 1] == ' ')) continue;
                sb.Append(c);
                if (sb.Length == Largo) break;
            }
            return sb.ToString().TrimEnd();
        }
    }
}
