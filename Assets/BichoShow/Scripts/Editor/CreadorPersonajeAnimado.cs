using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Menús "BichoShow > Personaje: aplicar Bichete" (el concursante de ahora) y
    /// "… Triangulín animado". Ponen en el prefab del bicho un modelo con esqueleto y
    /// animaciones hecho en Blender (Arte/Blender/crear_bichete.py → Modelos/Bichete.fbx, o
    /// crear_triangulin_animado.py → Modelos/TriangulinAnimado.fbx):
    ///  1. Ajusta la importación: rig genérico, animaciones en bucle con nombres limpios.
    ///  2. Crea el Animator Controller (Animaciones/&lt;Personaje&gt;.controller):
    ///     Locomoción (Quieto → Andar → Correr según la velocidad), Aire (Saltar → Caer según
    ///     la velocidad vertical), Tumbado y Celebrar.
    ///  3. Cambia el aspecto del prefab, pone materiales de URP (la Piel primero: se tiñe con el
    ///     color del jugador) y el componente AnimadorBicho. No toca la física ni los controles.
    /// </summary>
    public static class CreadorPersonajeAnimado
    {
        const string CarpetaAnimaciones = E.Raiz + "/Animaciones";
        static readonly string[] Animaciones = { "Quieto", "Andar", "Correr", "Saltar", "Caer", "Tumbado", "Celebrar" };

        // El personaje que se está aplicando (los pone cada menú)
        static string RutaModelo, RutaControlador, ScriptBlender;

        [MenuItem("BichoShow/Personaje: aplicar Bichete (el concursante)")]
        public static void AplicarBichete()
        {
            RutaModelo = E.Raiz + "/Modelos/Bichete.fbx";
            RutaControlador = CarpetaAnimaciones + "/Bichete.controller";
            ScriptBlender = "Arte/Blender/crear_bichete.py";
            E.CrearCarpeta(E.Raiz, "Materiales");
            // Los que empiezan por "Piel" y "Caparazon" se tiñen con el color del jugador (ControladorBicho).
            Aplicar(new Dictionary<string, Material>
            {
                { "Piel", E.CrearMaterial("PielBichete", new Color(1f, 0.42f, 0.36f), 0.35f) },
                { "Caparazon", E.CrearMaterial("CaparazonBichete", new Color(0.82f, 0.18f, 0.2f), 0.8f) },
                { "Barriga", E.CrearMaterial("BarrigaBichete", new Color(1f, 0.93f, 0.78f), 0.3f) },
                { "Mancha", E.CrearMaterial("ManchaBichete", new Color(0.16f, 0.08f, 0.14f), 0.7f) },
                { "Ojo", E.CrearMaterial("OjoBichete", new Color(0.98f, 0.97f, 0.95f), 0.7f) },
                { "Pupila", E.CrearMaterial("PupilaBichete", new Color(0.06f, 0.04f, 0.06f), 0.85f) },
                { "Brillo", E.CrearMaterial("Brillo", Color.white, 0.9f) },
                { "Boca", E.CrearMaterial("BocaBichete", new Color(0.28f, 0.06f, 0.1f), 0.4f) },
                { "Lengua", E.CrearMaterial("LenguaBichete", new Color(1f, 0.45f, 0.55f), 0.5f) },
                { "Rubor", E.CrearMaterial("RuborBichete", new Color(1f, 0.55f, 0.6f), 0.2f) },
                { "Bota", E.CrearMaterial("ZapatillaBichete", new Color(0.2f, 0.75f, 0.75f), 0.45f) },
                { "Suela", E.CrearMaterial("SuelaBichete", new Color(0.95f, 0.95f, 0.92f), 0.3f) },
            });
        }

        [MenuItem("BichoShow/Personaje: aplicar Triangulín animado (con esqueleto)")]
        public static void AplicarTriangulin()
        {
            RutaModelo = E.Raiz + "/Modelos/TriangulinAnimado.fbx";
            RutaControlador = CarpetaAnimaciones + "/Triangulin.controller";
            ScriptBlender = "Arte/Blender/crear_triangulin_animado.py";
            E.CrearCarpeta(E.Raiz, "Materiales");
            Aplicar(new Dictionary<string, Material>
            {
                { "Piel", E.CrearMaterial("Piel", new Color(0.88f, 0.30f, 0.24f), 0.25f) },
                { "Ojo", E.CrearMaterial("OjoTriangulin", new Color(0.97f, 0.94f, 0.88f), 0.65f) },
                { "Pupila", E.CrearMaterial("PupilaTriangulin", new Color(0.06f, 0.04f, 0.04f), 0.85f) },
                { "Brillo", E.CrearMaterial("Brillo", Color.white, 0.9f) },
                { "Boca", E.CrearMaterial("BocaTriangulin", new Color(0.32f, 0.08f, 0.06f), 0.4f) },
                { "Bota", E.CrearMaterial("Bota", new Color(0.06f, 0.42f, 0.42f), 0.4f) },
                { "Suela", E.CrearMaterial("Suela", new Color(0.04f, 0.28f, 0.28f), 0.2f) },
            });
        }

        static void Aplicar(Dictionary<string, Material> materiales)
        {
            if (!PrepararImportacion()) return;
            var modeloAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelo);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(E.RutaPrefabBicho) == null)
            {
                EditorUtility.DisplayDialog("Bicho Show", "No encuentro el prefab del bicho. Crea antes alguna escena desde el menú BichoShow.", "Vale");
                return;
            }

            var clips = BuscarClips();
            foreach (var n in Animaciones)
                if (!clips.ContainsKey(n))
                {
                    EditorUtility.DisplayDialog("Bicho Show", $"Al modelo le falta la animación \"{n}\". Vuelve a generarlo con el script de Blender.", "Vale");
                    return;
                }
            var controlador = CrearControlador(clips);

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
                var modelo = (GameObject)Object.Instantiate(modeloAsset, aspecto);
                modelo.name = "Modelo";
                modelo.transform.localPosition = Vector3.zero;
                modelo.transform.localRotation = Quaternion.identity;
                CreadorPersonaje.Orientar(modelo.transform, raiz.transform);

                if (!modelo.TryGetComponent(out Animator animator)) animator = modelo.AddComponent<Animator>();
                animator.runtimeAnimatorController = controlador;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

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

                // Física: cápsula a la medida (1.5 m de alto).
                var capsula = raiz.GetComponent<CapsuleCollider>();
                if (capsula != null)
                {
                    capsula.radius = 0.45f;
                    capsula.height = 1.4f;
                    capsula.center = new Vector3(0f, 0.7f, 0f);
                }

                var control = raiz.GetComponent<ControladorBicho>();
                control.renderersColor = piel.ToArray();
                control.manoSimple = false;

                if (!raiz.TryGetComponent(out AnimadorBicho anim)) anim = raiz.AddComponent<AnimadorBicho>();
                anim.raizVisual = aspecto;
                anim.animador = animator;
                Transform B(string n) => CreadorPersonaje.Buscar(modelo.transform, n);
                anim.ojoIzq = B("OjoIzq");
                anim.ojoDer = B("OjoDer");
                anim.brazoIzq = B("BrazoIzq");
                anim.brazoDer = B("BrazoDer");
                anim.antebrazoIzq = B("AntebrazoIzq");
                anim.antebrazoDer = B("AntebrazoDer");
                anim.manoIzq = B("ManoIzq");
                anim.manoDer = B("ManoDer");
                anim.antenaIzq = B("AntenaIzq1");
                anim.antenaDer = B("AntenaDer1");
                anim.puntaAntenaIzq = B("AntenaIzq2");
                anim.puntaAntenaDer = B("AntenaDer2");

                PrefabUtility.SaveAsPrefabAsset(raiz, E.RutaPrefabBicho);
                Debug.Log("Bicho Show: Triangulín animado aplicado al prefab del bicho. Dale a Play en cualquier escena.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        /// <summary>Rig genérico con avatar propio, ejes de Unity, animaciones en bucle y nombres limpios.</summary>
        static bool PrepararImportacion()
        {
            var importador = AssetImporter.GetAtPath(RutaModelo) as ModelImporter;
            if (importador == null)
            {
                EditorUtility.DisplayDialog("Bicho Show",
                    $"No encuentro el modelo en:\n{RutaModelo}\n\nGenéralo con {ScriptBlender}.", "Vale");
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
