using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// La cuenta del jugador 1 de esta copia del juego (Unity Gaming Services), para el pase de
    /// temporada, las salas online y el ranking mundial.
    ///  - Al abrir el juego entra con la sesión guardada; si no hay ninguna, como invitado.
    ///  - "Iniciar sesión" abre el navegador con la cuenta de Unity (Google, Apple o email). El
    ///    invitado se vincula a esa cuenta y conserva su progreso; si la cuenta ya tenía un
    ///    jugador (de otro ordenador o de antes de reinstalar), se pasa a ese jugador.
    /// Solo la primera copia abierta en el ordenador usa la sesión guardada; las demás (para
    /// probar el online en el mismo PC) entran cada vez como un invitado nuevo.
    /// </summary>
    public static class Cuenta
    {
        public enum Estado { Apagada, Conectando, Invitado, ConCuenta, SinConexion }

        public static Estado EstadoActual { get; private set; } = Estado.Apagada;
        /// <summary>Lo que se le cuenta al jugador (en la pantalla del pase).</summary>
        public static string Aviso { get; private set; } = "";
        /// <summary>Esta copia es la primera abierta en el ordenador (la que guarda la sesión).</summary>
        public static bool Principal { get; private set; }
        /// <summary>Esperando a que el jugador entre en el navegador.</summary>
        public static bool EsperandoNavegador { get; private set; }
        public static string IdJugador => EstadoActual == Estado.Invitado || EstadoActual == Estado.ConCuenta
            ? AuthenticationService.Instance.PlayerId : null;
        /// <summary>El correo con el que entró (solo para enseñarlo), si lo dio la cuenta.</summary>
        public static string Correo => Principal ? PlayerPrefs.GetString(ClaveCorreo, "") : "";

        /// <summary>Ha cambiado el estado o el jugador (para recargar el progreso).</summary>
        public static event Action AlCambiar;

        const string ClaveCorreo = "BichoShow.Cuenta.Correo";
        static Mutex candado;
        static Task iniciando;
        static TaskCompletionSource<bool> navegador;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimpiarEstaticos()
        {
            EstadoActual = Estado.Apagada;
            Aviso = "";
            AlCambiar = null;
            iniciando = null;
            navegador = null;
            EsperandoNavegador = false;
        }

        /// <summary>Se conecta solo al abrir el juego (el pase necesita saber quién eres).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AlArrancar() => _ = IniciarSinErrores();

        static async Task IniciarSinErrores()
        {
            try { await Iniciar(); } catch { /* ya lo explica el aviso */ }
        }

        /// <summary>Unity Services y la sesión (la guardada o una de invitado). Se puede llamar muchas veces.</summary>
        public static Task Iniciar() => iniciando ??= IniciarInterno();

        static async Task IniciarInterno()
        {
            Cambiar(Estado.Conectando, "Conectando…");
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    // La primera copia del ordenador usa siempre el mismo perfil (y con él, la sesión
                    // guardada); las demás, uno nuevo cada vez, para ser jugadores distintos.
                    Principal = CogerCandado();
                    string perfil = Principal ? "principal" : "j" + Guid.NewGuid().ToString("N").Substring(0, 20);
                    await UnityServices.InitializeAsync(new InitializationOptions().SetProfile(perfil));
                }
                while (UnityServices.State == ServicesInitializationState.Initializing) await Task.Yield();
                var auth = AuthenticationService.Instance;
                if (!auth.IsSignedIn) await auth.SignInAnonymouslyAsync(); // con sesión guardada, vuelve a ese jugador
                await ComprobarCuenta();
            }
            catch (Exception e)
            {
                iniciando = null; // otro intento más tarde
                Cambiar(Estado.SinConexion, "Sin conexión: lo que ganes se guarda en este ordenador y se sube al volver.");
                Debug.LogWarning($"Bicho Show: no se pudo entrar en Unity Services ({e.Message}).");
                throw;
            }
        }

        static async Task ComprobarCuenta()
        {
            var auth = AuthenticationService.Instance;
            PlayerInfo info = null;
            try { info = await auth.GetPlayerInfoAsync(); } catch { info = auth.PlayerInfo; }
            bool conCuenta = info != null && !string.IsNullOrEmpty(info.GetUnityId());
            Cambiar(conCuenta ? Estado.ConCuenta : Estado.Invitado,
                    conCuenta ? "" : "Juegas como invitado: inicia sesión para no perder el progreso si reinstalas el juego.");
        }

        /// <summary>Abre el navegador para entrar con la cuenta de Unity (Google, Apple o email).</summary>
        public static async void IniciarSesion()
        {
            if (EstadoActual == Estado.Conectando || EstadoActual == Estado.ConCuenta) return;
            if (RedShow.EnLinea) { Aviso = "Sal de la sala online para iniciar sesión."; AlCambiar?.Invoke(); return; }
            try
            {
                await Iniciar();
                var cuentas = PlayerAccountService.Instance;
                Cambiar(Estado.Conectando, "Sigue en el navegador para entrar con tu cuenta (Google, Apple o email)…");
                if (!cuentas.IsSignedIn)
                {
                    navegador = new TaskCompletionSource<bool>();
                    void Bien() => navegador?.TrySetResult(true);
                    void Mal(RequestFailedException e) => navegador?.TrySetException(e);
                    cuentas.SignedIn += Bien;
                    cuentas.SignInFailed += Mal;
                    EsperandoNavegador = true;
                    try
                    {
                        await cuentas.StartSignInAsync();
                        await navegador.Task;
                    }
                    finally
                    {
                        cuentas.SignedIn -= Bien;
                        cuentas.SignInFailed -= Mal;
                        EsperandoNavegador = false;
                        navegador = null;
                    }
                }

                string token = cuentas.AccessToken;
                var auth = AuthenticationService.Instance;
                try
                {
                    await auth.LinkWithUnityAsync(token); // el invitado se queda con su progreso
                }
                catch (AuthenticationException e) when (e.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
                {
                    // Esa cuenta ya tenía su jugador: pasar a él (el progreso del invitado se junta luego).
                    auth.SignOut(true);
                    await auth.SignInWithUnityAsync(token);
                }

                string correo = cuentas.IdTokenClaims != null ? cuentas.IdTokenClaims.Email : "";
                if (Principal) { PlayerPrefs.SetString(ClaveCorreo, correo ?? ""); PlayerPrefs.Save(); }
                await ComprobarCuenta();
            }
            catch (OperationCanceledException)
            {
                await VolverAlEstado("Inicio de sesión cancelado.");
            }
            catch (PlayerAccountsException e) when (e.ErrorCode == PlayerAccountsErrorCodes.MissingClientId)
            {
                await VolverAlEstado("Faltan por activar las cuentas de Unity en el proyecto (mira Docs/Pase de temporada.md).");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Bicho Show: no se pudo iniciar sesión ({e.Message}).");
                await VolverAlEstado("No se pudo iniciar sesión. Prueba otra vez.");
            }
        }

        /// <summary>Deja de esperar al navegador (el jugador se ha arrepentido).</summary>
        public static void CancelarInicio() => navegador?.TrySetCanceled();

        /// <summary>Sale de la cuenta y vuelve a jugar como invitado nuevo.</summary>
        public static async void CerrarSesion()
        {
            if (EstadoActual != Estado.ConCuenta) return;
            if (RedShow.EnLinea) { Aviso = "Sal de la sala online para cerrar la sesión."; AlCambiar?.Invoke(); return; }
            try
            {
                AuthenticationService.Instance.SignOut(true);
                try { PlayerAccountService.Instance.SignOut(); } catch { /* no estaba */ }
                if (Principal) { PlayerPrefs.DeleteKey(ClaveCorreo); PlayerPrefs.Save(); }
                Cambiar(Estado.Conectando, "Cerrando sesión…");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                await ComprobarCuenta();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Bicho Show: al cerrar sesión ({e.Message}).");
                Cambiar(Estado.SinConexion, "Sin conexión.");
            }
        }

        static async Task VolverAlEstado(string aviso)
        {
            try { await ComprobarCuenta(); } catch { Cambiar(Estado.SinConexion, ""); }
            Aviso = aviso;
            AlCambiar?.Invoke();
        }

        static void Cambiar(Estado estado, string aviso)
        {
            EstadoActual = estado;
            Aviso = aviso;
            AlCambiar?.Invoke();
        }

        /// <summary>¿Somos la primera copia del juego abierta en este ordenador?</summary>
        static bool CogerCandado()
        {
            try
            {
                candado ??= new Mutex(false, "Local\\BichoShow.Cuenta");
                try { return candado.WaitOne(0); }
                catch (AbandonedMutexException) { return true; } // la copia anterior se cerró de golpe
            }
            catch
            {
                return true; // sin candados (otra plataforma): una sola copia
            }
        }
    }
}
