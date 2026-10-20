using System.IO;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// En una partida online, graba lo que dibuja la interfaz del anfitrión (todo pasa por
    /// EstiloShow: textos, paneles, barras, estrellas…) para que los invitados vean exactamente
    /// la misma pantalla (marcadores, relojes, carteles, la tabla de estrellas) sin tener que
    /// copiar el estado de cada prueba. Se graba la orden de dibujo, no la imagen: ocupa poco.
    ///
    /// Los invitados la reproducen escalada a su pantalla.
    /// </summary>
    public static class GrabadorPantalla
    {
        const byte OrdenTextura = 1, OrdenTexto = 2, OrdenMatriz = 3, OrdenColor = 4, OrdenEtiqueta = 5;

        /// <summary>Lo activa RedShow en el anfitrión mientras hay show y alguien mirando.</summary>
        public static bool Grabando;

        static readonly MemoryStream memoria = new MemoryStream(8192);
        static readonly BinaryWriter escritor = new BinaryWriter(memoria);
        static Matrix4x4 matriz = Matrix4x4.identity;
        static Color colorGui = Color.white;

        /// <summary>Lo grabado desde el último EmpezarFotograma.</summary>
        public static byte[] Buffer => memoria.GetBuffer();
        public static int Longitud => (int)memoria.Length;

        /// <summary>Borra lo grabado (se llama una vez por fotograma, antes de que se dibuje).</summary>
        public static void EmpezarFotograma()
        {
            memoria.SetLength(0);
            matriz = Matrix4x4.identity;
            colorGui = Color.white;
        }

        static bool Activo => Grabando && Event.current != null && Event.current.type == EventType.Repaint;

        // ------------------------------------------------------------------
        // Grabar (lo llama EstiloShow)
        // ------------------------------------------------------------------

        internal static void Textura(Rect r, int textura, ScaleMode modo, Color color, float borde, float radio)
        {
            if (!Activo) return;
            Estado();
            escritor.Write(OrdenTextura);
            EscribirRect(r);
            escritor.Write((byte)textura);
            escritor.Write((byte)modo);
            EscribirColor(color);
            escritor.Write(Mathf.FloatToHalf(borde));
            escritor.Write(Mathf.FloatToHalf(radio));
        }

        internal static void Texto(Rect r, string texto, int tamano, Color color, TextAnchor alineacion,
                                   EstiloShow.Fuente fuente, bool ajustar, float grosor, Color? contorno)
        {
            if (!Activo) return;
            Estado();
            escritor.Write(OrdenTexto);
            EscribirRect(r);
            escritor.Write(texto);
            escritor.Write((short)tamano);
            EscribirColor(color);
            escritor.Write((byte)alineacion);
            byte flags = (byte)((fuente == EstiloShow.Fuente.Texto ? 1 : 0) | (ajustar ? 2 : 0) | (contorno.HasValue ? 4 : 0));
            escritor.Write(flags);
            escritor.Write(Mathf.FloatToHalf(grosor));
            if (contorno.HasValue) EscribirColor(contorno.Value);
        }

        internal static void Etiqueta(Rect r, string texto, GUIStyle estilo, int fuente)
        {
            if (!Activo) return;
            Estado();
            escritor.Write(OrdenEtiqueta);
            EscribirRect(r);
            escritor.Write(texto ?? "");
            escritor.Write((short)estilo.fontSize);
            escritor.Write((byte)estilo.alignment);
            escritor.Write((byte)estilo.fontStyle);
            escritor.Write((byte)((estilo.wordWrap ? 1 : 0) | (fuente << 1)));
            EscribirColor(estilo.normal.textColor);
        }

        /// <summary>Giros (letras que rebotan, flechas) y transparencia general: se graban solo cuando cambian.</summary>
        static void Estado()
        {
            if (GUI.matrix != matriz)
            {
                matriz = GUI.matrix;
                escritor.Write(OrdenMatriz);
                escritor.Write(matriz.m00); escritor.Write(matriz.m01); escritor.Write(matriz.m03);
                escritor.Write(matriz.m10); escritor.Write(matriz.m11); escritor.Write(matriz.m13);
            }
            if (GUI.color != colorGui)
            {
                colorGui = GUI.color;
                escritor.Write(OrdenColor);
                EscribirColor(colorGui);
            }
        }

        static void EscribirRect(Rect r)
        {
            escritor.Write(Corto(r.x));
            escritor.Write(Corto(r.y));
            escritor.Write(Corto(r.width));
            escritor.Write(Corto(r.height));
        }

        static short Corto(float v) => (short)Mathf.Clamp(Mathf.RoundToInt(v), short.MinValue, short.MaxValue);

        static void EscribirColor(Color c)
        {
            Color32 c32 = c;
            escritor.Write(c32.r); escritor.Write(c32.g); escritor.Write(c32.b); escritor.Write(c32.a);
        }

        // ------------------------------------------------------------------
        // Reproducir (en los invitados, dentro de OnGUI)
        // ------------------------------------------------------------------

        static GUIStyle estiloEtiqueta;

        /// <summary>Dibuja un fotograma grabado. Escala = pantalla de aquí / pantalla del anfitrión.</summary>
        public static void Reproducir(byte[] datos, int longitud, float escalaX, float escalaY)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint || datos == null || longitud <= 0) return;
            var escala = Matrix4x4.Scale(new Vector3(escalaX, escalaY, 1f));
            var matrizAntes = GUI.matrix;
            var colorAntes = GUI.color;
            GUI.matrix = escala;
            GUI.color = Color.white;
            bool grabandoAntes = Grabando;
            Grabando = false;

            using (var lector = new BinaryReader(new MemoryStream(datos, 0, longitud, false)))
            {
                try
                {
                    while (lector.BaseStream.Position < longitud)
                    {
                        byte orden = lector.ReadByte();
                        switch (orden)
                        {
                            case OrdenMatriz:
                            {
                                var m = Matrix4x4.identity;
                                m.m00 = lector.ReadSingle(); m.m01 = lector.ReadSingle(); m.m03 = lector.ReadSingle();
                                m.m10 = lector.ReadSingle(); m.m11 = lector.ReadSingle(); m.m13 = lector.ReadSingle();
                                GUI.matrix = escala * m;
                                break;
                            }
                            case OrdenColor:
                                GUI.color = LeerColor(lector);
                                break;
                            case OrdenTextura:
                            {
                                var r = LeerRect(lector);
                                int textura = lector.ReadByte();
                                var modo = (ScaleMode)lector.ReadByte();
                                var color = LeerColor(lector);
                                float borde = Mathf.HalfToFloat(lector.ReadUInt16());
                                float radio = Mathf.HalfToFloat(lector.ReadUInt16());
                                var tex = EstiloShow.TexturaPorId(textura);
                                if (tex != null) GUI.DrawTexture(r, tex, modo, true, 0f, color, borde, radio);
                                break;
                            }
                            case OrdenTexto:
                            {
                                var r = LeerRect(lector);
                                string texto = lector.ReadString();
                                int tamano = lector.ReadInt16();
                                var color = LeerColor(lector);
                                var alineacion = (TextAnchor)lector.ReadByte();
                                byte flags = lector.ReadByte();
                                float grosor = Mathf.HalfToFloat(lector.ReadUInt16());
                                Color? contorno = (flags & 4) != 0 ? LeerColor(lector) : (Color?)null;
                                EstiloShow.Texto(r, texto, tamano, color, alineacion,
                                                 (flags & 1) != 0 ? EstiloShow.Fuente.Texto : EstiloShow.Fuente.Titulo,
                                                 (flags & 2) != 0, grosor, contorno);
                                break;
                            }
                            case OrdenEtiqueta:
                            {
                                var r = LeerRect(lector);
                                string texto = lector.ReadString();
                                if (estiloEtiqueta == null) estiloEtiqueta = new GUIStyle(GUI.skin.label);
                                estiloEtiqueta.fontSize = lector.ReadInt16();
                                estiloEtiqueta.alignment = (TextAnchor)lector.ReadByte();
                                estiloEtiqueta.fontStyle = (FontStyle)lector.ReadByte();
                                byte flags = lector.ReadByte();
                                estiloEtiqueta.wordWrap = (flags & 1) != 0;
                                estiloEtiqueta.font = EstiloShow.FuentePorId(flags >> 1);
                                estiloEtiqueta.normal.textColor = LeerColor(lector);
                                estiloEtiqueta.Draw(r, texto, false, false, false, false);
                                break;
                            }
                            default:
                                return; // fotograma de otra versión del juego: mejor no seguir
                        }
                    }
                }
                catch (EndOfStreamException) { /* fotograma cortado: se dibuja lo que haya */ }
                finally
                {
                    GUI.matrix = matrizAntes;
                    GUI.color = colorAntes;
                    Grabando = grabandoAntes;
                }
            }
        }

        static Rect LeerRect(BinaryReader l) => new Rect(l.ReadInt16(), l.ReadInt16(), l.ReadInt16(), l.ReadInt16());

        static Color LeerColor(BinaryReader l) => new Color32(l.ReadByte(), l.ReadByte(), l.ReadByte(), l.ReadByte());
    }
}
