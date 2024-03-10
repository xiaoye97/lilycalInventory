using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

#if LIL_VRCSDK3A
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Control = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using ControlType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType;
using Parameter = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter;
using ValueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType;
#endif

#if LIL_NDMF
using nadena.dev.ndmf;
#endif

#if LIL_MODULAR_AVATAR
using nadena.dev.modular_avatar.core;
#endif

namespace jp.lilxyzw.lilycalinventory
{
    using runtime;

    #if LIL_VRCSDK3A
    internal static class VRChatHelper
    {
        private static Texture2D m_IconNext;
        private static Texture2D iconNext
        {
            get
            {
                if(!m_IconNext) m_IconNext = ObjHelper.LoadAssetByGUID<Texture2D>(ConstantValues.GUID_ICON_NEXT);
                return m_IconNext;
            }
        }

        internal static AnimatorController TryGetFXAnimatorController(this VRCAvatarDescriptor descriptor, BuildContext ctx)
        {
            AnimatorController CreateFXController()
            {
                var controller = new AnimatorController{name = "FX"};
                AssetDatabase.AddObjectToAsset(controller, ctx.AssetContainer);
                return controller;
            }
            if(!descriptor.customizeAnimationLayers)
            {
                descriptor.customizeAnimationLayers = true;
            }
            for(int i = 0; i < descriptor.baseAnimationLayers.Length; i++)
            {
                if(descriptor.baseAnimationLayers[i].type != VRCAvatarDescriptor.AnimLayerType.FX) continue;
                var layer = descriptor.baseAnimationLayers[i];
                if(layer.isDefault)
                {
                    layer.isDefault = false;
                    layer.animatorController = null;
                }
                if(layer.animatorController) return (AnimatorController)layer.animatorController;
                var controllerI = CreateFXController();
                layer.animatorController = controllerI;
                descriptor.baseAnimationLayers[i] = layer;
                return controllerI;
            }
            Array.Resize(ref descriptor.baseAnimationLayers, descriptor.baseAnimationLayers.Length+1);
            var newcontroller = CreateFXController();
            var newlayer = new VRCAvatarDescriptor.CustomAnimLayer
            {
                animatorController = newcontroller,
                isDefault = false,
                type = VRCAvatarDescriptor.AnimLayerType.FX
            };
            descriptor.baseAnimationLayers[descriptor.baseAnimationLayers.Length - 1] = newlayer;
            return newcontroller;
        }

        internal static void MergeParameters(this VRCAvatarDescriptor descriptor, VRCExpressionsMenu menu, VRCExpressionParameters parameters, BuildContext ctx)
        {
            if(!descriptor.customExpressions)
            {
                descriptor.customExpressions = true;
                descriptor.expressionsMenu = null;
                descriptor.expressionParameters = null;
            }
            if(descriptor.expressionsMenu) descriptor.expressionsMenu.Merge(menu);
            else descriptor.expressionsMenu = menu;
            descriptor.expressionsMenu.ResolveOver(ctx);

            if(descriptor.expressionParameters) descriptor.expressionParameters.Merge(parameters);
            else descriptor.expressionParameters = parameters;
        }

        private static void ResolveOver(this VRCExpressionsMenu menu, BuildContext ctx)
        {
            if(menu.controls.Count > VRCExpressionsMenu.MAX_CONTROLS)
            {
                int last = VRCExpressionsMenu.MAX_CONTROLS - 1;
                int count = menu.controls.Count - last;
                var menuNext = CreateMenu(ctx, $"{menu.name}_Next");
                menuNext.controls = menu.controls.GetRange(last, count);
                menu.controls.RemoveRange(last, count);
                menu.AddMenu(menuNext, iconNext, "Next");
            }

            foreach(var child in menu.controls)
                if(child.subMenu) child.subMenu.ResolveOver(ctx);
        }

        internal static VRCExpressionsMenu CreateMenu(BuildContext ctx, string name = ConstantValues.TOOL_NAME)
        {
            var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menu.name = name;
            AssetDatabase.AddObjectToAsset(menu, ctx.AssetContainer);
            return menu;
        }

        internal static VRCExpressionParameters CreateParameters(BuildContext ctx, string name = ConstantValues.TOOL_NAME + "Parameters")
        {
            var parameter = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            parameter.name = name;
            parameter.parameters = new VRCExpressionParameters.Parameter[0];
            AssetDatabase.AddObjectToAsset(parameter, ctx.AssetContainer);
            return parameter;
        }

        internal static void Merge(this VRCExpressionsMenu menu, VRCExpressionsMenu menu2)
        {
            menu.controls.AddRange(menu2.controls);
        }

        internal static void Merge(this VRCExpressionParameters parameters, VRCExpressionParameters parameters2)
        {
            if(parameters.CalcTotalCost() + parameters2.CalcTotalCost() >= VRCExpressionParameters.MAX_PARAMETER_COST)
                ErrorHelper.Report("dialog.error.parameterover");

            var size = parameters.parameters.Length;
            Array.Resize(ref parameters.parameters, parameters.parameters.Length + parameters2.parameters.Length);
            for(int i = 0; i < parameters2.parameters.Length; i++)
            {
                parameters.parameters[size+i] = parameters2.parameters[i];
            }
        }

        internal static void AddMenu(this VRCExpressionsMenu menu, VRCExpressionsMenu menu2, Texture2D icon = null, string name = null)
        {
            menu.controls.Add(
                new Control
                {
                    icon = icon,
                    name = name ?? menu2.name,
                    subMenu = menu2,
                    type = ControlType.SubMenu
                }
            );
        }

        internal static void AddMenu(this VRCExpressionsMenu menu, VRCExpressionsMenu menu2, MenuBaseComponent component)
        {
            menu.AddMenu(menu2, component.icon);
        }

        internal static void AddParameter(this VRCExpressionParameters parameters, string name, bool isLocalOnly, bool isSave, float defaultValue, VRCExpressionParameters.ValueType type)
        {
            if(!parameters.parameters.Any(p => p.name == name))
            {
                Array.Resize(ref parameters.parameters, parameters.parameters.Length+1);
                parameters.parameters[parameters.parameters.Length-1] = new VRCExpressionParameters.Parameter
                {
                    defaultValue = defaultValue,
                    name = name,
                    networkSynced = !isLocalOnly,
                    saved = isSave,
                    valueType = type
                };
            }
        }

        internal static void AddParameterToggle(this VRCExpressionParameters parameters, string name, bool isLocalOnly, bool isSave, float defaultValue = 0)
        {
            parameters.AddParameter(name, isLocalOnly, isSave, defaultValue, VRCExpressionParameters.ValueType.Bool);
        }

        internal static void AddParameterInt(this VRCExpressionParameters parameters, string name, bool isLocalOnly, bool isSave, float defaultValue = 0)
        {
            parameters.AddParameter(name, isLocalOnly, isSave, defaultValue, VRCExpressionParameters.ValueType.Int);
        }

        internal static void AddParameterFloat(this VRCExpressionParameters parameters, string name, bool isLocalOnly, bool isSave, float defaultValue = 0)
        {
            parameters.AddParameter(name, isLocalOnly, isSave, defaultValue, VRCExpressionParameters.ValueType.Float);
        }

        internal static Control CreateControl(string name, Texture2D icon, ControlType type, string parameterName, float value = 1)
        {
            var control =  new Control
            {
                icon = icon,
                name = name,
                type = type,
                value = value
            };
            if(type == ControlType.RadialPuppet) control.subParameters = new[]{new Parameter{name = parameterName}};
            else control.parameter = new Parameter{name = parameterName};
            return control;
        }

        internal static void CreateAndAdd(this List<Control> controls, string name, Texture2D icon, ControlType type, string parameterName, float value = 1)
        {
            controls.Add(CreateControl(name, icon, type, parameterName, value));
        }
    }
    #endif

    internal static class ParameterViewer
    {
        #if LIL_VRCSDK3A
        private static int costBool = VRCExpressionParameters.TypeCost(ValueType.Bool);
        private static int costInt = VRCExpressionParameters.TypeCost(ValueType.Int);
        private static int costFloat = VRCExpressionParameters.TypeCost(ValueType.Float);
        private static int costMax = VRCExpressionParameters.MAX_PARAMETER_COST;

        private static bool isInitialized = false;
        private static bool isExpandedAvatar = false;
        private static bool isExpandedMA = false;
        private static bool isExpandedLI = false;
        private static IEnumerable<Object> autoDressers;
        private static IEnumerable<Object> props;
        private static IEnumerable<Object> itemTogglers;
        private static IEnumerable<Object> costumeChangers;
        private static IEnumerable<Object> smoothChangers;
        private static IEnumerable<Object> maParams;
        private static int costByAvatar;
        private static int costByLI;
        private static int costByMA;
        private static GameObject avatarRoot;
        private static VRCExpressionParameters parameters;

        internal static void Draw(MenuBaseComponent component)
        {
            Update(component);
            if(!avatarRoot) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            int costSum = costByAvatar + costByMA + costByLI;
            EditorGUILayout.LabelField($"{Localization.S("inspector.ParameterViewer.memoryUsed")}: {costSum} / {costMax} ({Localization.S("inspector.ParameterViewer.memoryRemaining")}: {costMax - costSum})");
            var position = EditorGUILayout.GetControlRect(GUILayout.Height(8));
            var rect = position;
            EditorGUI.DrawRect(rect, new Color(0.5f,0.5f,0.5f,0.5f));

            if(costByAvatar != 0)
            {
                rect.width = position.width * ((float)costByAvatar / costMax);
                EditorGUI.DrawRect(rect, new Color(0.203f, 0.764f, 0.450f));
                rect.x = rect.xMax;

                EditorGUI.indentLevel++;
                if(isExpandedAvatar = EditorGUILayout.Foldout(isExpandedAvatar, $"{Localization.S("inspector.ParameterViewer.memoryAvatar")}: {costByAvatar} / {costMax}"))
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(parameters, typeof(Object), true);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUI.indentLevel--;
            }

            #if LIL_MODULAR_AVATAR
            if(costByMA != 0)
            {
                rect.width = position.width * ((float)costByMA / costMax);
                EditorGUI.DrawRect(rect, new Color(0.000f, 0.627f, 0.913f));
                rect.x = rect.xMax;

                EditorGUI.indentLevel++;
                if(isExpandedMA = EditorGUILayout.Foldout(isExpandedMA, $"Modular Avatar: {costByMA} / {costMax}"))
                {
                    EditorGUI.BeginDisabledGroup(true);
                    foreach(var c in maParams) EditorGUILayout.ObjectField(c, typeof(Object), true);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUI.indentLevel--;
            }
            #endif

            if(costByLI != 0)
            {
                rect.width = position.width * ((float)costByLI / costMax);
                EditorGUI.DrawRect(rect, new Color(0.572f, 0.549f, 0.858f));
                rect.x = rect.xMax;

                EditorGUI.indentLevel++;
                if(isExpandedLI = EditorGUILayout.Foldout(isExpandedLI, $"lilycalInventory: {costByLI} / {costMax}"))
                {
                    EditorGUI.BeginDisabledGroup(true);
                    foreach(var c in autoDressers) EditorGUILayout.ObjectField(c, typeof(Object), true);
                    foreach(var c in props) EditorGUILayout.ObjectField(c, typeof(Object), true);
                    foreach(var c in itemTogglers) EditorGUILayout.ObjectField(c, typeof(Object), true);
                    foreach(var c in costumeChangers) EditorGUILayout.ObjectField(c, typeof(Object), true);
                    foreach(var c in smoothChangers) EditorGUILayout.ObjectField(c, typeof(Object), true);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private static void Update(MenuBaseComponent component)
        {
            if(isInitialized) return;
            isInitialized = true;
            var root = component.gameObject.GetAvatarRoot();
            if(!root) return;
            avatarRoot = root.gameObject;

            // By Avatar
            costByAvatar = 0;
            parameters = null;
            var descriptor = avatarRoot.GetComponent<VRCAvatarDescriptor>();
            if(descriptor && descriptor.expressionParameters)
            {
                costByAvatar = descriptor.expressionParameters.CalcTotalCost();
                parameters = descriptor.expressionParameters;
            }

            // By Modular Avatar
            #if LIL_MODULAR_AVATAR
            int CalcCost(ParameterConfig config)
            {
                if(config.internalParameter || config.isPrefix || config.localOnly || config.syncType == ParameterSyncType.NotSynced) return 0;
                switch(config.syncType)
                {
                    case ParameterSyncType.Bool: return costBool;
                    case ParameterSyncType.Int: return costInt;
                    case ParameterSyncType.Float: return costFloat;
                }
                return 0;
            }

            maParams = avatarRoot.GetComponentsInChildren<ModularAvatarParameters>().Where(c => !c.IsEditorOnly());
            costByMA = maParams.SelectMany(c => ((ModularAvatarParameters)c).parameters).Sum(p => CalcCost(p));
            #endif

            // By lilycalInventory
            var components = avatarRoot.GetActiveComponentsInChildren<MenuBaseComponent>(true).Where(c => !(c is MenuFolder) && !(c is AutoDresserSettings) && !c.IsEditorOnly());
            autoDressers = components.Where(c => c is AutoDresser);
            props = components.Where(c => c is Prop);
            itemTogglers = components.Where(c => c is ItemToggler);
            costumeChangers = components.Where(c => c is CostumeChanger);
            smoothChangers = components.Where(c => c is SmoothChanger);

            costByLI = costBool * (props.Count() + itemTogglers.Count())
                + costInt * costumeChangers.Count()
                + costFloat * smoothChangers.Count();
            if(autoDressers.Count() > 0) costByLI += costInt;
        }

        internal static void Reset()
        {
            isInitialized = false;
        }
        #else
        internal static void Draw(MenuBaseComponent component){}
        internal static void Update(MenuBaseComponent component){}
        internal static void Reset(){}
        #endif
    }
}
