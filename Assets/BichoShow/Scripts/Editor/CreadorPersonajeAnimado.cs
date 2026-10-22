using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Menús del personaje con esqueleto y animaciones hecho en Blender
    /// (Arte/Blender/crear_personaje.py → Modelos/&lt;Nombre&gt;.fbx, uno por bicho):
    ///  · "Personaje: preparar los personajes": para cada bicho (Bichete, Zumbi, Concha,
    ///    Cornelio, Chispa) ajusta la importación (rig genérico, animaciones en bucle con nombres
    ///    limpios), crea su Animator Controller (Animaciones/&lt;Nombre&gt;.controller: Locomoción
    ///    Quieto → Andar → Correr, Aire Saltar → Caer, Tumbado y Celebrar), le pone materiales de
    ///    URP con su paleta y guarda el prefab Resources/Personajes/&lt;Nombre&gt; (con ModeloBicho),
    ///    que es lo que PersonajeBicho pone en el bicho durante la partida.
    ///  · "Personaje: aplicar Bichete (el concursante)": lo anterior y, además, pone a Bichete en
    ///    el prefab del bicho con AnimadorBicho, Vestuario y PersonajeBicho. No toca la física ni
    ///    los controles.
    ///  · "… Triangulín animado": el concursante de antes (crear_triangulin_animado.py).
    /// </summary>
    public static class CreadorPersonajeAnimado
    {
        const string CarpetaAnimaciones = E.Raiz + "/Animaciones";
        const string CarpetaPersonajes = E.Raiz + "/Resources/Personajes";
        static readonly string[] Animaciones = { "Quieto", "Andar", "Correr", "Saltar", "Caer", "Tumbado", "Celebrar" };

        // El personaje que se está preparando
        static string RutaModelo, RutaControlador, ScriptBlender;

        /// <summary>
        /// ¿Están los prefabs de todos los bichos, al día con su modelo (no hay un .fbx más nuevo)
        /// y el bicho sabe cambiar de uno a otro?
        /// </summary>
        public static bool Preparados()
        {
            var bicho = AssetDatabase.LoadAssetAtPath<GameObject>(E.RutaPrefabBicho);
            if (bicho == null || bicho.GetComponent<PersonajeBicho>() == null) return false;
            foreach (var n in PersonajeBicho.Especies)
            {
                string prefab = $"{CarpetaPersonajes}/{n}.prefab", modelo = $"{E.Raiz}/Modelos/{n}.fbx";
                if (!System.IO.File.Exists(prefab)) return false;
                if (System.IO.File.Exists(modelo) && System.IO.File.GetLastWriteTimeUtc(modelo) > System.IO.File.GetLastWriteTimeUtc(prefab)) return false;
            }
            return true;
        }

        [MenuItem("BichoShow/Personaje: preparar los personajes")]
        public static void PrepararPersonajes() => PrepararTodos();

        [MenuItem("BichoShow/Personaje: aplicar Bichete (el concursante)")]
        public static void AplicarBichete()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(E.RutaPrefabBicho) == null)
            {
                EditorUtility.DisplayDialog("Bicho Show", "No encuentro el prefab del bicho. Crea antes alguna escena desde el menú BichoShow.", "Vale");
                return;
            }
            CreadorVestuario.Preparar();
            PrepararTodos();
            var bichete = AssetDatabase.LoadAssetAtPath<ModeloBicho>($"{CarpetaPersonajes}/{PersonajeBicho.Especies[0]}.prefab");
            if (bichete == null) return;
            PonerEnBicho(bichete, true);
        }

        [MenuItem("BichoShow/Personaje: aplicar Triangulín animado (con esqueleto)")]
        public static void AplicarTriangulin()
        {
            RutaModelo = E.Raiz + "/Modelos/TriangulinAnimado.fbx";
            RutaControlador = CarpetaAnimaciones + "/Triangulin.controller";
            ScriptBlender = "Arte/Blender/crear_triangulin_animado.py";
            E.CrearCarpeta(E.Raiz, "Materiales");
            var modelo = Montar(new Dictionary<string, Material>
            {
                { "Piel", E.CrearMaterial("Piel", new Color(0.88f, 0.30f, 0.24f), 0.25f) },
                { "Ojo", E.CrearMaterial("OjoTriangulin", new Color(0.97f, 0.94f, 0.88f), 0.65f) },
                { "Pupila", E.CrearMaterial("PupilaTriangulin", new Color(0.06f, 0.04f, 0.04f), 0.85f) },
                { "Brillo", E.CrearMaterial("Brillo", Color.white, 0.9f) },
                { "Boca", E.CrearMaterial("BocaTriangulin", new Color(0.32f, 0.08f, 0.06f), 0.4f) },
                { "Bota", E.CrearMaterial("Bota", new Color(0.06f, 0.42f, 0.42f), 0.4f) },
                { "Suela", E.CrearMaterial("Suela", new Color(0.04f, 0.28f, 0.28f), 0.2f) },
            }, true);
            if (modelo == null) return;
            try { PonerEnBicho(modelo, false); }
            finally { Object.DestroyImmediate(modelo.gameObject); }
        }

        // ------------------------------------------------------------------
        // Los bichos y sus colores
        // ------------------------------------------------------------------

        /// <summary>
        /// Paleta de cada bicho. La piel y el caparazón se tiñen con el color del jugador en la
        /// partida (estos colores se ven en el editor y si no hay color); el resto es de cada uno.
        /// </summary>
        static Dictionary<string, Material> Paleta(string bicho)
        {
            // Bichete (mariquita), y lo que no cambie en los demás
            var c = new Dictionary<string, (Color color, float brillo)>
            {
                { "Piel", (new Color(1f, 0.42f, 0.36f), 0.35f) },
                { "Caparazon", (new Color(0.82f, 0.18f, 0.2f), 0.8f) },
                { "Barriga", (new Color(1f, 0.93f, 0.78f), 0.3f) },
                { "Mancha", (new Color(0.16f, 0.08f, 0.14f), 0.7f) },
                { "Ojo", (new Color(0.98f, 0.97f, 0.95f), 0.7f) },
                { "Pupila", (new Color(0.06f, 0.04f, 0.06f), 0.85f) },
                { "Brillo", (Color.white, 0.9f) },
                { "Boca", (new Color(0.28f, 0.06f, 0.1f), 0.4f) },
                { "Lengua", (new Color(1f, 0.45f, 0.55f), 0.5f) },
                { "Rubor", (new Color(1f, 0.55f, 0.6f), 0.2f) },
                { "Bota", (new Color(0.2f, 0.75f, 0.75f), 0.45f) },
                { "Suela", (new Color(0.95f, 0.95f, 0.92f), 0.3f) },
                { "Luz", (new Color(1f, 0.95f, 0.45f), 0.6f) },
                { "Ala", (new Color(0.82f, 0.94f, 1f, 0.55f), 0.9f) },
                // Estos dos no se tiñen con el color del jugador: son de cada bicho.
                { "Concha", (new Color(0.93f, 0.72f, 0.45f), 0.55f) },
                { "Pelo", (new Color(1f, 0.95f, 0.8f), 0.1f) },
            };
            switch (bicho)
            {
                case "Zumbi": // abeja: amarilla con rayas casi negras y zapatillas naranjas
                    c["Piel"] = (new Color(1f, 0.78f, 0.2f), 0.4f);
                    c["Barriga"] = (new Color(1f, 0.93f, 0.68f), 0.3f);
                    c["Mancha"] = (new Color(0.14f, 0.09f, 0.06f), 0.6f);
                    c["Bota"] = (new Color(0.98f, 0.48f, 0.2f), 0.45f);
                    break;
                case "Concha": // caracol: verde lechuga, concha de galleta y zapatillas rosas
                    c["Piel"] = (new Color(0.62f, 0.85f, 0.5f), 0.55f);
                    c["Caparazon"] = (new Color(0.85f, 0.55f, 0.35f), 0.7f);
                    c["Barriga"] = (new Color(0.95f, 0.97f, 0.8f), 0.4f);
                    c["Mancha"] = (new Color(0.5f, 0.3f, 0.18f), 0.4f); // la espiral de la concha
                    c["Bota"] = (new Color(0.9f, 0.42f, 0.62f), 0.45f);
                    break;
                case "Cornelio": // escarabajo: azul brillante, cuerno oscuro, zapatillas amarillas
                    c["Piel"] = (new Color(0.38f, 0.56f, 0.95f), 0.45f);
                    c["Caparazon"] = (new Color(0.22f, 0.3f, 0.75f), 0.9f);
                    c["Mancha"] = (new Color(0.12f, 0.1f, 0.2f), 0.8f);
                    c["Bota"] = (new Color(0.98f, 0.78f, 0.22f), 0.45f);
                    break;
                case "Chispa": // luciérnaga: lila con capucha oscura, farolillo y zapatillas verdes
                    c["Piel"] = (new Color(0.62f, 0.52f, 0.92f), 0.4f);
                    c["Mancha"] = (new Color(0.24f, 0.18f, 0.34f), 0.6f);
                    c["Bota"] = (new Color(0.32f, 0.8f, 0.5f), 0.45f);
                    break;
            }

            var materiales = new Dictionary<string, Material>();
            foreach (var par in c)
            {
                // Los de Bichete se llaman como siempre (PielBichete, ZapatillaBichete…).
                string nombre = par.Key == "Brillo" ? "Brillo"
                              : bicho == "Bichete" && par.Key == "Bota" ? "ZapatillaBichete"
                              : par.Key + bicho;
                var m = E.CrearMaterial(nombre, par.Value.color, par.Value.brillo);
                if (par.Key == "Luz") Emisivo(m, par.Value.color * 2.5f);
                if (par.Key == "Ala") Transparente(m);
                materiales[par.Key] = m;
            }
            return materiales;
        }

        static void Emisivo(Material m, Color emision)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", emision);
            EditorUtility.SetDirty(m);
        }

        static void Transparente(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
        }

        /// <summary>Hace el prefab de cada bicho (Resources/Personajes). Devuelve cuántos.</summary>
        static int PrepararTodos()
        {
            E.CrearCarpeta(E.Raiz, "Materiales");
            E.CrearCarpeta(E.Raiz, "Resources");
            E.CrearCarpeta(E.Raiz + "/Resources", "Personajes");
            int hechos = 0;
            foreach (var bicho in PersonajeBicho.Especies)
            {
                RutaModelo = $"{E.Raiz}/Modelos/{bicho}.fbx";
                RutaControlador = $"{CarpetaAnimaciones}/{bicho}.controller";
                ScriptBlender = "Arte/Blender/crear_personaje.py";
                var modelo = Montar(Paleta(bicho), false);
                if (modelo == null) continue;
                try
                {
                    // Talla de la ropa: lo ancho que es cada uno respecto a Bichete (ver FORMAS en crear_personaje.py).
                    modelo.tallaRopa = bicho switch { "Zumbi" => 1.12f, "Concha" => 0.85f, "Cornelio" => 1.18f, "Chispa" => 0.88f, _ => 1f };
                    if (bicho == "Chispa") LuzFarol(modelo);
                    PrefabUtility.SaveAsPrefabAsset(modelo.gameObject, $"{CarpetaPersonajes}/{bicho}.prefab");
                    hechos++;
                }
                finally { Object.DestroyImmediate(modelo.gameObject); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Bicho Show: {hechos} de {PersonajeBicho.Especies.Length} personajes preparados en {CarpetaPersonajes}.");
            return hechos;
        }

        /// <summary>El farolillo de Chispa da luz de verdad (va con la cadera, como el farol).</summary>
        static void LuzFarol(ModeloBicho modelo)
        {
            var cadera = CreadorPersonaje.Buscar(modelo.transform, "Cadera");
            var go = new GameObject("LuzFarol");
            go.transform.SetParent(modelo.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.65f, -1.05f); // detrás del farol (que sobresale del culete)
            if (cadera != null) go.transform.SetParent(cadera, true);
            var luz = go.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.95f, 0.45f);
            luz.range = 5f;
            luz.intensity = 2f;
            luz.shadows = LightShadows.None;
        }

        /// <summary>
        /// Monta el personaje del FBX actual (RutaModelo): un objeto "Modelo" con ModeloBicho y,
        /// dentro, el modelo de pie mirando a +Z con su Animator, materiales y huesos apuntados.
        /// Queda en la escena abierta: quien lo pide lo guarda o lo copia y luego lo borra.
        /// </summary>
        static ModeloBicho Montar(Dictionary<string, Material> materiales, bool avisar)
        {
            if (!PrepararImportacion(avisar)) return null;
            var modeloAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelo);
            var clips = BuscarClips();
            foreach (var n in Animaciones)
                if (!clips.ContainsKey(n))
                {
                    string texto = $"A {System.IO.Path.GetFileName(RutaModelo)} le falta la animación \"{n}\". Vuelve a generarlo con {ScriptBlender}.";
                    if (avisar) EditorUtility.DisplayDialog("Bicho Show", texto, "Vale");
                    else Debug.LogWarning("Bicho Show: " + texto);
                    return null;
                }
            var controlador = CrearControlador(clips);

            var raiz = new GameObject("Modelo");
            var datos = raiz.AddComponent<ModeloBicho>();
            var modelo = (GameObject)Object.Instantiate(modeloAsset, raiz.transform);
            modelo.name = modeloAsset.name;
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.identity;
            CreadorPersonaje.Orientar(modelo.transform, raiz.transform);

            if (!modelo.TryGetComponent(out Animator animator)) animator = modelo.AddComponent<Animator>();
            animator.runtimeAnimatorController = controlador;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            datos.animador = animator;

            // Materiales de URP por nombre (los del FBX solo sirven para saber cuál es cuál).
            var piel = new List<Renderer>();
            foreach (var r in modelo.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var actuales = r.sharedMaterials;
                for (int i = 0; i < actuales.Length; i++)
                {
                    string n = actuales[i] != null ? actuales[i].name.Split('.')[0].Trim() : "";
                    actuales[i] = materiales.TryGetValue(n, out var m) ? m : materiales["Piel"];
                }
                r.sharedMaterials = actuales;
                r.updateWhenOffscreen = true; // la caja de la malla sigue al esqueleto (porrazos, saltos)
                if (actuales.Length > 0 && actuales[0] == materiales["Piel"]) piel.Add(r);
            }
            datos.piel = piel.ToArray();

            Transform B(string n) => CreadorPersonaje.Buscar(modelo.transform, n);
            datos.ojoIzq = B("OjoIzq");
            datos.ojoDer = B("OjoDer");
            datos.brazoIzq = B("BrazoIzq");
            datos.brazoDer = B("BrazoDer");
            datos.antebrazoIzq = B("AntebrazoIzq");
            datos.antebrazoDer = B("AntebrazoDer");
            datos.manoIzq = B("ManoIzq");
            datos.manoDer = B("ManoDer");
            datos.antenaIzq = B("AntenaIzq1");
            datos.antenaDer = B("AntenaDer1");
            datos.puntaAntenaIzq = B("AntenaIzq2");
            datos.puntaAntenaDer = B("AntenaDer2");
            datos.alaIzq = B("AlaIzq");
            datos.alaDer = B("AlaDer");
            datos.anclaGorro = B("AnclaGorro");
            datos.anclaCara = B("AnclaCara");
            datos.anclaPecho = B("AnclaPecho");
            return datos;
        }

        /// <summary>
        /// Pone una copia del personaje en el prefab del bicho (Aspecto/Modelo) y lo conecta todo.
        /// Con conEspecies, el bicho lleva PersonajeBicho y puede cambiar de bicho en la partida.
        /// </summary>
        static void PonerEnBicho(ModeloBicho personaje, bool conEspecies)
        {
            var raiz = PrefabUtility.LoadPrefabContents(E.RutaPrefabBicho);
            try
            {
                // Fuera el aspecto anterior (cápsulas, Triangulín de fideos…) y su animador por código.
                foreach (var nombre in new[] { "Cuerpo", "OrejaIzq", "OrejaDer", "OjoIzq", "OjoDer", "Aspecto" })
                {
                    var viejo = raiz.transform.Find(nombre);
                    if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
                }
                if (raiz.TryGetComponent(out AspectoBicho viejoAnimador)) Object.DestroyImmediate(viejoAnimador);

                var aspecto = new GameObject("Aspecto").transform;
                aspecto.SetParent(raiz.transform, false);
                var modelo = Object.Instantiate(personaje, aspecto); // copia suelta, sin enlace al prefab
                modelo.name = "Modelo";
                modelo.transform.localPosition = Vector3.zero;
                modelo.transform.localRotation = Quaternion.identity;
                modelo.transform.localScale = Vector3.one;

                // Física: cápsula a la medida (1.5 m de alto).
                var capsula = raiz.GetComponent<CapsuleCollider>();
                if (capsula != null)
                {
                    capsula.radius = 0.45f;
                    capsula.height = 1.4f;
                    capsula.center = new Vector3(0f, 0.7f, 0f);
                }

                var control = raiz.GetComponent<ControladorBicho>();
                control.renderersColor = modelo.piel;
                control.manoSimple = false;

                if (!raiz.TryGetComponent(out AnimadorBicho anim)) anim = raiz.AddComponent<AnimadorBicho>();
                anim.raizVisual = aspecto;
                anim.animador = modelo.animador;
                anim.ojoIzq = modelo.ojoIzq;
                anim.ojoDer = modelo.ojoDer;
                anim.brazoIzq = modelo.brazoIzq;
                anim.brazoDer = modelo.brazoDer;
                anim.antebrazoIzq = modelo.antebrazoIzq;
                anim.antebrazoDer = modelo.antebrazoDer;
                anim.manoIzq = modelo.manoIzq;
                anim.manoDer = modelo.manoDer;
                anim.antenaIzq = modelo.antenaIzq;
                anim.antenaDer = modelo.antenaDer;
                anim.puntaAntenaIzq = modelo.puntaAntenaIzq;
                anim.puntaAntenaDer = modelo.puntaAntenaDer;
                anim.alaIzq = modelo.alaIzq;
                anim.alaDer = modelo.alaDer;

                // Vestuario (si el modelo tiene anclajes para gorros y complementos)
                if (modelo.anclaGorro != null)
                {
                    if (!raiz.TryGetComponent(out Vestuario vestuario)) vestuario = raiz.AddComponent<Vestuario>();
                    vestuario.anclaGorro = modelo.anclaGorro;
                    vestuario.anclaCara = modelo.anclaCara;
                    vestuario.anclaPecho = modelo.anclaPecho;
                    vestuario.referencia = aspecto;
                    vestuario.talla = modelo.tallaRopa;
                }

                if (conEspecies) { if (!raiz.TryGetComponent(out PersonajeBicho _)) raiz.AddComponent<PersonajeBicho>(); }
                else if (raiz.TryGetComponent(out PersonajeBicho sobra)) Object.DestroyImmediate(sobra);

                PrefabUtility.SaveAsPrefabAsset(raiz, E.RutaPrefabBicho);
                Debug.Log($"Bicho Show: {personaje.transform.GetChild(0).name} aplicado al prefab del bicho. Dale a Play en cualquier escena.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        /// <summary>Rig genérico con avatar propio, ejes de Unity, animaciones en bucle y nombres limpios.</summary>
        static bool PrepararImportacion(bool avisar = true)
        {
            var importador = AssetImporter.GetAtPath(RutaModelo) as ModelImporter;
            if (importador == null)
            {
                if (avisar) EditorUtility.DisplayDialog("Bicho Show", $"No encuentro el modelo en:\n{RutaModelo}\n\nGenéralo con {ScriptBlender}.", "Vale");
                else Debug.LogWarning($"Bicho Show: no encuentro {RutaModelo}. Genéralo con {ScriptBlender}.");
                return false;
            }

            importador.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importador.importCameras = false;
            importador.importLights = false;
            importador.bakeAxisConversion = true;
            importador.animationType = ModelImporterAnimationType.Generic;
            importador.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importador.importAnimation = true;
            importador.animationCompression = ModelImporterAnimationCompression.Optimal;

            var tomas = importador.defaultClipAnimations;
            foreach (var toma in tomas)
            {
                string nombre = toma.name;
                int barra = nombre.LastIndexOf('|');
                if (barra >= 0) nombre = nombre.Substring(barra + 1);
                toma.name = nombre;
                toma.loopTime = true;
            }
            importador.clipAnimations = tomas;
            importador.SaveAndReimport();
            return true;
        }

        static Dictionary<string, AnimationClip> BuscarClips()
        {
            var clips = new Dictionary<string, AnimationClip>();
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(RutaModelo))
                if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                    clips[c.name] = c;
            return clips;
        }

        /// <summary>
        /// Animator Controller:
        ///   Locomocion (Velocidad: 0 Quieto, 2.2 Andar, 6 Correr; su ritmo con "Ritmo")
        ///   Aire (VelY: -4 Caer, 1.5 Saltar) · Tumbado · Celebrar
        /// Desde cualquier estado: Tumbado manda; luego Celebrar. Sin tiempos de salida.
        /// </summary>
        static AnimatorController CrearControlador(Dictionary<string, AnimationClip> clips)
        {
            E.CrearCarpeta(E.Raiz, "Animaciones");
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador) != null)
                AssetDatabase.DeleteAsset(RutaControlador);
            var c = AnimatorController.CreateAnimatorControllerAtPath(RutaControlador);
            c.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
            c.AddParameter("VelY", AnimatorControllerParameterType.Float);
            c.AddParameter("EnSuelo", AnimatorControllerParameterType.Bool);
            c.AddParameter("Tumbado", AnimatorControllerParameterType.Bool);
            c.AddParameter("Celebrando", AnimatorControllerParameterType.Bool);
            c.AddParameter("Ritmo", AnimatorControllerParameterType.Float);
            var parametros = c.parameters; // (copias: hay que volver a asignarlas)
            foreach (var p in parametros)
            {
                if (p.name == "Ritmo") p.defaultFloat = 1f;
                if (p.name == "EnSuelo") p.defaultBool = true;
            }
            c.parameters = parametros;

            var sm = c.layers[0].stateMachine;

            var locomocion = c.CreateBlendTreeInController("Locomocion", out BlendTree arbol, 0);
            arbol.blendType = BlendTreeType.Simple1D;
            arbol.blendParameter = "Velocidad";
            arbol.useAutomaticThresholds = false;
            arbol.AddChild(clips["Quieto"], 0f);
            arbol.AddChild(clips["Andar"], 2.2f);
            arbol.AddChild(clips["Correr"], 6f);
            locomocion.speedParameterActive = true;
            locomocion.speedParameter = "Ritmo";

            var aire = c.CreateBlendTreeInController("Aire", out BlendTree arbolAire, 0);
            arbolAire.blendType = BlendTreeType.Simple1D;
            arbolAire.blendParameter = "VelY";
            arbolAire.useAutomaticThresholds = false;
            arbolAire.AddChild(clips["Caer"], -4f);
            arbolAire.AddChild(clips["Saltar"], 1.5f);

            var tumbado = sm.AddState("Tumbado");
            tumbado.motion = clips["Tumbado"];
            var celebrar = sm.AddState("Celebrar");
            celebrar.motion = clips["Celebrar"];
            sm.defaultState = locomocion;

            AnimatorStateTransition T(AnimatorStateTransition t, float duracion)
            {
                t.hasExitTime = false;
                t.duration = duracion;
                t.canTransitionToSelf = false;
                return t;
            }

            T(sm.AddAnyStateTransition(tumbado), 0.08f).AddCondition(AnimatorConditionMode.If, 0, "Tumbado");
            var t1 = T(tumbado.AddTransition(locomocion), 0.2f);
            t1.AddCondition(AnimatorConditionMode.IfNot, 0, "Tumbado");
            t1.AddCondition(AnimatorConditionMode.If, 0, "EnSuelo");
            var t2 = T(tumbado.AddTransition(aire), 0.2f);
            t2.AddCondition(AnimatorConditionMode.IfNot, 0, "Tumbado");
            t2.AddCondition(AnimatorConditionMode.IfNot, 0, "EnSuelo");

            T(locomocion.AddTransition(aire), 0.12f).AddCondition(AnimatorConditionMode.IfNot, 0, "EnSuelo");
            T(aire.AddTransition(locomocion), 0.08f).AddCondition(AnimatorConditionMode.If, 0, "EnSuelo");

            T(sm.AddAnyStateTransition(celebrar), 0.2f).AddCondition(AnimatorConditionMode.If, 0, "Celebrando");
            T(celebrar.AddTransition(locomocion), 0.2f).AddCondition(AnimatorConditionMode.IfNot, 0, "Celebrando");

            AssetDatabase.SaveAssets();
            return c;
        }
    }
}
