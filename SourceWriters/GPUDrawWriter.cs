﻿using PaintDotNet;
using PaintDotNet.Effects.Gpu;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PdnCodeLab
{
    internal static class GPUDrawWriter
    {
        private const ProjectType projectType = ProjectType.GpuDrawEffect;

        private const string UsingStatements = ""
            + "using System;\r\n"
            + "using System.IO;\r\n"
            + "using System.Linq;\r\n"
            + "using System.Diagnostics;\r\n"
            + "using System.Threading;\r\n"
            + "using System.Reflection;\r\n"
            + "using System.Windows.Forms;\r\n"
            + "using System.IO.Compression;\r\n"
            + "using System.Collections.Generic;\r\n"
            + "using System.Text;\r\n"
            + "using System.Text.RegularExpressions;\r\n"
            + "using System.Runtime.InteropServices;\r\n"
            + "using System.Runtime.Versioning;\r\n"
            + "using Registry = Microsoft.Win32.Registry;\r\n"
            + "using RegistryKey = Microsoft.Win32.RegistryKey;\r\n"
            + "using PaintDotNet;\r\n"
            + "using PaintDotNet.AppModel;\r\n"
            + "using PaintDotNet.Direct2D1;\r\n"
            + "using PaintDotNet.Direct2D1.Effects;\r\n"
            + "using PaintDotNet.IndirectUI;\r\n"
            + "using PaintDotNet.DirectWrite;\r\n"
            + "using PaintDotNet.PropertySystem;\r\n"
            + "using PaintDotNet.Effects;\r\n"
            + "using PaintDotNet.Effects.Gpu;\r\n"
            + "using PaintDotNet.Clipboard;\r\n"
            + "using PaintDotNet.Imaging;\r\n"
            + "using PaintDotNet.Collections;\r\n"
            + "using PaintDotNet.Rendering;\r\n"
            + "using ColorWheelControl = PaintDotNet.Imaging.ColorBgra32;\r\n"
            + "using AngleControl = System.Double;\r\n"
            + "using PanSliderControl = PaintDotNet.Rendering.Vector2Double;\r\n"
            + "using FolderControl = System.String;\r\n"
            + "using FilenameControl = System.String;\r\n"
            + "using ReseedButtonControl = System.Byte;\r\n"
            + "using RollControl = PaintDotNet.Rendering.Vector3Double;\r\n"
            + "using IntSliderControl = System.Int32;\r\n"
            + "using CheckboxControl = System.Boolean;\r\n"
            + "using TextboxControl = System.String;\r\n"
            + "using DoubleSliderControl = System.Double;\r\n"
            + "using ListBoxControl = System.Byte;\r\n"
            + "using RadioButtonControl = System.Byte;\r\n"
            + "using MultiLineTextboxControl = System.String;\r\n"
            + "using LabelComment = System.String;\r\n"
            + "using FontFamily = System.String;\r\n"
            + "using FontStyle = PaintDotNet.DirectWrite.FontStyle;\r\n"
            + "using LayerControl = System.Int32;\r\n"
            + "\r\n";

        private const string prepend_code = "\r\n"
            + "namespace PdnCodeLab\r\n"
            + "{\r\n"
            + "public class UserScript : GpuDrawingEffect\r\n"
            + "{\r\n"
            + "    public UserScript()\r\n"
            + "        : base(\"UserScript\", string.Empty, GpuDrawingEffectOptions.Create() with { IsConfigurable = false })\r\n";

        private const string EmptyCode = "\r\n"
            + "#region User Entered Code\r\n"
            + "protected override unsafe void OnDraw(PaintDotNet.Direct2D1.IDeviceContext deviceContext) { deviceContext.DrawImage(Environment.SourceImage); }\r\n"
            + "#endregion\r\n";

        private static string ConstructorPart(UIElement[] UserControls, string FileName, string subMenuName, string menuName, string iconPath)
        {
            menuName = menuName.Trim().Replace('"', '\'');
            if (menuName.Length == 0)
            {
                menuName = FileName;
            }

            string className = Regex.Replace(FileName, @"[^\w]", "") + "EffectPlugin";

            string icon = File.Exists(iconPath)
                ? "new System.Drawing.Bitmap(typeof(" + className + "), \"" + Path.GetFileName(iconPath) + "\")"
                : "null";

            string configurable = (UserControls.Length != 0).ToString().ToLowerInvariant();

            string EffectPart = "";
            EffectPart += "    public class " + className + " : PropertyBasedGpuDrawingEffect\r\n";
            EffectPart += "    {\r\n";
            EffectPart += "        public static string StaticName => \"" + menuName + "\";\r\n";
            EffectPart += "        public static System.Drawing.Image StaticIcon => " + icon + ";\r\n";
            EffectPart += "        public static string SubmenuName => " + CommonWriter.SubmenuPart(subMenuName) + ";\r\n";
            EffectPart += "\r\n";
            EffectPart += "        public " + className + "()\r\n";
            EffectPart += "            : base(StaticName, StaticIcon, SubmenuName, GpuDrawingEffectOptions.Create() with { IsConfigurable = " + configurable + " })\r\n";
            EffectPart += "        {\r\n";

            if (UserControls.Any(u => u.ElementType == ElementType.ReseedButton))
            {
                // if we have a random number generator control, include the following line...
                EffectPart += "            instanceSeed = unchecked((int)DateTime.Now.Ticks);\r\n";
            }

            EffectPart += "        }\r\n";
            EffectPart += "\r\n";
            return EffectPart;
        }

        private static string InitializeRenderInfoPart(ScriptRenderingFlags renderingFlags, ScriptRenderingSchedule renderingSchedule)
        {
            string renderInfo =
                "        protected override void OnInitializeRenderInfo(IGpuDrawingEffectRenderInfo renderInfo)\r\n" +
                "        {\r\n";

            if (renderingFlags != ScriptRenderingFlags.None)
            {
                List<string> flags= new List<string>();
                if (renderingFlags.HasFlag(ScriptRenderingFlags.SingleThreaded))
                {
                    flags.Add("GpuEffectRenderingFlags.SingleThreaded");
                }
                if (renderingFlags.HasFlag(ScriptRenderingFlags.NoSelectionClipping))
                {
                    flags.Add("GpuEffectRenderingFlags.DisableSelectionClipping");
                }
                if (renderingFlags.HasFlag(ScriptRenderingFlags.AliasedSelection))
                {
                    flags.Add("GpuEffectRenderingFlags.ForceAliasedSelectionQuality");
                }

                foreach(string flag in flags)
                {
                    renderInfo += "            renderInfo.Flags |= " + flag + ";\r\n";
                }

            }

            if (renderingFlags.HasFlag(ScriptRenderingFlags.StraightAlpha))
            {
                renderInfo += "            renderInfo.InputAlphaMode = GpuEffectAlphaMode.Straight;\r\n";
                renderInfo += "            renderInfo.OutputAlphaMode = GpuEffectAlphaMode.Straight;\r\n";
            }
            else
            {
                renderInfo += "            renderInfo.InputAlphaMode = GpuEffectAlphaMode.Premultiplied;\r\n";
                renderInfo += "            renderInfo.OutputAlphaMode = GpuEffectAlphaMode.Premultiplied;\r\n";
            }

            if (renderingFlags.HasFlag(ScriptRenderingFlags.WorkingSpaceColorContext))
            {
                renderInfo += "            renderInfo.ColorContext = GpuEffectColorContext.WorkingSpace;\r\n";
            }
            else
            {
                renderInfo += "            renderInfo.ColorContext = GpuEffectColorContext.WorkingSpaceLinear;\r\n";
            }

            renderInfo += "\r\n" +
                "            base.OnInitializeRenderInfo(renderInfo);\r\n" +
                "        }\r\n" +
                "\r\n";

            return renderInfo;
        }

        private static string SetTokenPart(UIElement[] UserControls)
        {
            string setToken =
                "        protected override void OnSetToken(PropertyBasedEffectConfigToken newToken)\r\n" +
                "        {\r\n";

            if (UserControls.Length > 0)
            {
                setToken += CommonWriter.TokenValuesPart(UserControls, "newToken");
            }

            setToken +=
                "            base.OnSetToken(newToken);\r\n" +
                "        }\r\n" +
                "\r\n";

            return setToken;
        }

        internal static string FullSourceCode(string SourceCode, string FileName, bool isAdjustment, string subMenuName, string menuName, string iconPath, string SupportURL, ScriptRenderingFlags renderingFlags, ScriptRenderingSchedule renderingSchedule, string Author, int MajorVersion, int MinorVersion, string Description, string KeyWords, string WindowTitleStr, HelpType HelpType, string HelpText)
        {
            UIElement[] UserControls = UIElement.ProcessUIControls(SourceCode, projectType);

            return
                GPUDrawWriter.UsingStatements +
                CommonWriter.AssemblyInfoPart(FileName, menuName, Author, MajorVersion, MinorVersion, Description, KeyWords) +
                CommonWriter.NamespacePart(FileName) +
                CommonWriter.SupportInfoPart(FileName, menuName, SupportURL) +
                CommonWriter.CategoryPart(isAdjustment) +
                ConstructorPart(UserControls, FileName, subMenuName, menuName, iconPath) +
                CommonWriter.HelpPart(HelpType, HelpText) +
                CommonWriter.PropertyPart(UserControls, FileName, WindowTitleStr, HelpType, HelpText, projectType) +
                GPUDrawWriter.InitializeRenderInfoPart(renderingFlags, renderingSchedule) +
                GPUDrawWriter.SetTokenPart(UserControls) +
                CommonWriter.UserEnteredPart(SourceCode) +
                CommonWriter.EndPart();
        }

        internal static string FullPreview(string scriptText)
        {
            const string FileName = "PreviewEffect";
            UIElement[] UserControls = UIElement.ProcessUIControls(scriptText, projectType);

            return
                GPUDrawWriter.UsingStatements +
                CommonWriter.NamespacePart("UserScript") +
                GPUDrawWriter.ConstructorPart(UserControls, FileName, string.Empty, "FULL UI PREVIEW - Temporarily renders to canvas", string.Empty) +
                CommonWriter.PropertyPart(UserControls, FileName, string.Empty, HelpType.None, string.Empty, projectType) +
                GPUDrawWriter.SetTokenPart(UserControls) +
                CommonWriter.UserEnteredPart(scriptText) +
                CommonWriter.EndPart();
        }

        internal static string UiPreview(string uiCode)
        {
            const string FileName = "UiPreviewEffect";
            uiCode = "#region UICode\r\n" + uiCode + "\r\n#endregion\r\n";

            UIElement[] UserControls = UIElement.ProcessUIControls(uiCode, projectType);

            return
                GPUDrawWriter.UsingStatements +
                CommonWriter.NamespacePart(FileName) +
                GPUDrawWriter.ConstructorPart(UserControls, FileName, string.Empty, "UI PREVIEW - Does NOT Render to canvas", string.Empty) +
                CommonWriter.PropertyPart(UserControls, FileName, string.Empty, HelpType.None, string.Empty, projectType) +
                uiCode +
                GPUDrawWriter.EmptyCode +
                CommonWriter.EndPart();
        }

        internal static string Run(string scriptText, bool debug)
        {
            return
                GPUDrawWriter.UsingStatements +
                GPUDrawWriter.prepend_code +
                CommonWriter.ConstructorBodyPart(debug) +
                CommonWriter.UserEnteredPart(scriptText) +
                CommonWriter.EndPart();
        }
    }
}
