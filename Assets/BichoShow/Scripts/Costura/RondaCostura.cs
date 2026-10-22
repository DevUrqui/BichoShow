namespace BichoShow
{
    /// <summary>
    /// Prueba "Costura a la fuga" (todos contra todos): carrera de obstáculos sobre una mesa de
    /// costura, de la Tabla de Corte a la meta junto al dedal. Toda la carrera (puntos de control,
    /// meta, puntos por llegada) está en RondaCarrera.
    /// </summary>
    public class RondaCostura : RondaCarrera
    {
        public override string Titulo => "COSTURA A LA FUGA";
        public override string IdRanking => "costura";
        protected override string Descripcion => "¡Carrera por la mesa de costura! Esquiva los carretes, cruza el acordeón y las cremalleras y sube al dedal.";
        protected override string ConsejoInicio => "¡A la meta! Si te caes, vuelves a la última bandera que cruzaste";
    }
}
