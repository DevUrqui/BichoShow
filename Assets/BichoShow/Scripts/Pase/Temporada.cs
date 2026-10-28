using System;
using System.Collections.Generic;

namespace BichoShow
{
    /// <summary>
    /// La temporada del pase: fechas, niveles, lo que da cada nivel y los retos diarios.
    /// Para la siguiente temporada se cambia aquí (Id nuevo, fechas, premios); lo desbloqueado en
    /// las anteriores se queda (ProgresoPase guarda cada temporada con su Id).
    /// </summary>
    public static class Temporada
    {
        public const string Id = "t1";
        public const string Nombre = "TEMPORADA 1 · ¡GRAN ESTRENO!";
        /// <summary>Primer día (a las 00:00, hora del ordenador). Cámbialo antes de publicar.</summary>
        public static readonly DateTime Inicio = new DateTime(2026, 10, 26);
        public const int Dias = 20;
        public const int Niveles = 30;
        public const int XpPorNivel = 600;

        public static DateTime Fin => Inicio.AddDays(Dias);
        public static bool EnCurso => DateTime.Now >= Inicio && DateTime.Now < Fin;
        public static bool SinEmpezar => DateTime.Now < Inicio;
        public static TimeSpan Queda => (SinEmpezar ? Inicio : Fin) - DateTime.Now;

        // ------------------------------------------------------------------
        // Experiencia
        // ------------------------------------------------------------------

        /// <summary>Lo que da cada cosa (los retos, además, dan XpReto al completarse).</summary>
        public static int XpDe(string evento, int cantidad) => evento switch
        {
            Eventos.Ronda => 40 * cantidad,
            Eventos.Estrellas => 15 * cantidad,
            Eventos.Victoria => 30 * cantidad,
            Eventos.Show => 150 * cantidad,
            Eventos.GanaShow => 100 * cantidad,
            _ => 0,
        };
        public const int XpReto = 200;

        /// <summary>Lo que puede pasar en una partida y cuenta para el pase.</summary>
        public static class Eventos
        {
            public const string Ronda = "ronda";             // jugar una prueba entera
            public const string Estrellas = "estrellas";     // estrellas ganadas (cantidad = cuántas)
            public const string Victoria = "victoria";       // ganar una prueba
            public const string Final = "final";             // jugar una gran final
            public const string Show = "show";               // llegar al final de un show
            public const string GanaShow = "ganaShow";       // ganar el show
            public const string Entrega = "entrega";         // tu equipo entrega un objeto en la Mudanza
            public const string Meta = "meta";               // llegar a la meta en una carrera
            public const string Sabotaje = "sabotaje";       // 25 puntos o más saboteando en una ronda
        }

        // ------------------------------------------------------------------
        // Premios: uno por nivel
        // ------------------------------------------------------------------

        public enum Tipo { Gorro, Cara, Cuerpo, Titulo }

        public readonly struct Premio
        {
            public readonly int nivel;
            public readonly Tipo tipo;
            /// <summary>La prenda (como en Vestuario) o el texto del título.</summary>
            public readonly string id;
            public readonly string nombre;
            public Premio(int nivel, Tipo tipo, string id, string nombre) { this.nivel = nivel; this.tipo = tipo; this.id = id; this.nombre = nombre; }
            public bool EsPrenda => tipo != Tipo.Titulo;
        }

        static Premio T(int nivel, string titulo) => new Premio(nivel, Tipo.Titulo, titulo, titulo);

        public static readonly Premio[] Premios =
        {
            T(1, "Novato del plató"),
            new Premio(2, Tipo.Cara, "Gafas3D", "Gafas 3D"),
            T(3, "Mudancero"),
            new Premio(4, Tipo.Gorro, "CascoObra", "Casco de obra"),
            T(5, "Manos de mantequilla"),
            new Premio(6, Tipo.Cuerpo, "Flotador", "Flotador de pato"),
            T(7, "Pies ligeros"),
            new Premio(8, Tipo.Cara, "BigoteRizado", "Bigote rizado"),
            T(9, "Amigo de los bots"),
            new Premio(10, Tipo.Gorro, "Auriculares", "Cascos de DJ"),
            T(11, "Cazador de estrellas"),
            new Premio(12, Tipo.Cuerpo, "Guitarra", "Guitarra"),
            T(13, "Rompejarrones"),
            T(14, "Saboteador de guardia"),
            new Premio(15, Tipo.Cara, "Antifaz", "Antifaz"),
            T(16, "As del placaje"),
            T(17, "Rey del sofá"),
            new Premio(18, Tipo.Gorro, "GorroCohete", "Gorro cohete"),
            T(19, "Bicho con estilo"),
            T(20, "Superviviente"),
            new Premio(21, Tipo.Cuerpo, "Banda", "Banda de campeón"),
            T(22, "Cerebro del equipo"),
            T(23, "Terror de las cintas"),
            new Premio(24, Tipo.Cara, "GafasEstrella", "Gafas de estrella"),
            T(25, "Leyenda del barrio"),
            T(26, "Invencible (casi)"),
            new Premio(27, Tipo.Gorro, "Tiara", "Tiara de estrella"),
            T(28, "Estrella invitada"),
            T(29, "Fenómeno de la tele"),
            new Premio(30, Tipo.Cuerpo, "Alas", "Alas de mariposa"),
        };

        public static Premio? PremioDe(int nivel)
        {
            foreach (var p in Premios) if (p.nivel == nivel) return p;
            return null;
        }

        /// <summary>Nivel del pase con el que se desbloquea algo; 0 = no es del pase (siempre libre).</summary>
        public static int NivelDe(Tipo tipo, string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            foreach (var p in Premios) if (p.tipo == tipo && p.id == id) return p.nivel;
            return 0;
        }

        // ------------------------------------------------------------------
        // Retos diarios: tres al día, los mismos para todos ese día
        // ------------------------------------------------------------------

        public readonly struct Reto
        {
            public readonly string texto, evento;
            public readonly int objetivo;
            public Reto(string texto, string evento, int objetivo) { this.texto = texto; this.evento = evento; this.objetivo = objetivo; }
        }

        public static readonly Reto[] RetosPosibles =
        {
            new Reto("Juega 2 shows enteros", Eventos.Show, 2),
            new Reto("Consigue 8 estrellas", Eventos.Estrellas, 8),
            new Reto("Gana 2 pruebas", Eventos.Victoria, 2),
            new Reto("Entrega 3 objetos en Mudanza frágil", Eventos.Entrega, 3),
            new Reto("Llega a la meta en 2 carreras", Eventos.Meta, 2),
            new Reto("Consigue 25 puntos saboteando en una prueba", Eventos.Sabotaje, 1),
            new Reto("Juega una gran final", Eventos.Final, 1),
            new Reto("Juega 5 pruebas", Eventos.Ronda, 5),
            new Reto("Gana un show", Eventos.GanaShow, 1),
        };

        /// <summary>Los tres retos de un día (siempre los mismos para esa fecha).</summary>
        public static int[] RetosDelDia(DateTime dia)
        {
            var azar = new Random(dia.Year * 10000 + dia.Month * 100 + dia.Day);
            var indices = new List<int>();
            for (int i = 0; i < RetosPosibles.Length; i++) indices.Add(i);
            var elegidos = new int[3];
            for (int k = 0; k < 3; k++)
            {
                int j = azar.Next(indices.Count);
                elegidos[k] = indices[j];
                indices.RemoveAt(j);
            }
            return elegidos;
        }
    }
}
