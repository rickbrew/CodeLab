/////////////////////////////////////////////////////////////////////////////////
// CodeLab for Paint.NET
// Copyright ©2006 Rick Brewster, Tom Jackson. All Rights Reserved.
// Portions Copyright ©2007-2023 BoltBait. All Rights Reserved.
// Portions Copyright ©2016-2023 Jason Wendt. All Rights Reserved.
// Portions Copyright ©Microsoft Corporation. All Rights Reserved.
//
// THE CODELAB DEVELOPERS MAKE NO WARRANTY OF ANY KIND REGARDING THE CODE. THEY
// SPECIFICALLY DISCLAIM ANY WARRANTY OF FITNESS FOR ANY PARTICULAR PURPOSE OR
// ANY OTHER WARRANTY.  THE CODELAB DEVELOPERS DISCLAIM ALL LIABILITY RELATING
// TO THE USE OF THIS CODE.  NO LICENSE, EXPRESS OR IMPLIED, BY ESTOPPEL OR
// OTHERWISE, TO ANY INTELLECTUAL PROPERTY RIGHTS IS GRANTED HEREIN.
//
// Latest distribution: https://www.BoltBait.com/pdn/codelab
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using PaintDotNet.Direct2D1;
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using IDeviceContext = PaintDotNet.Direct2D1.IDeviceContext;
using WpfGeometry = System.Windows.Media.Geometry;

[assembly: AssemblyTitle("CodeLab plugin for Paint.NET")]
[assembly: AssemblyDescription("C# Code Editor for Paint.NET Plugin Development")]
[assembly: AssemblyConfiguration("C#|development|plugin|build|builder|code|coding|script|scripting")]
[assembly: AssemblyCompany("BoltBait")]
[assembly: AssemblyProduct("CodeLab")]
[assembly: AssemblyCopyright("Copyright ©2023 BoltBait")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: SupportedOSPlatform("Windows")]
[assembly: AssemblyVersion(PdnCodeLab.CodeLab.Version + ".*")]
// The ScintillaNET text editor requires the assembly to have a Guid.
[assembly: Guid("b908a26a-45e2-4d24-9681-e6f2020e68a8")]

namespace PdnCodeLab
{
    public class CodeLabSupportInfo : IPluginSupportInfo
    {
        public string Author => base.GetType().Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
        public string Copyright => base.GetType().Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
        public string DisplayName => base.GetType().Assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
        public Version Version => base.GetType().Assembly.GetName().Version;
        public Uri WebsiteUri => new Uri("https://www.boltbait.com/pdn/CodeLab/");
    }

    [PluginSupportInfo<CodeLabSupportInfo>]
    public class CodeLab : BitmapEffect<CodeLabConfigToken>
    {
        internal const string Version = "6.12";

        // Includes the Build and Revision fields that are generated by the compiler
        internal static string VersionFull => typeof(CodeLab).Assembly.GetName().Version.ToString();

        public CodeLab()
            : base("CodeLab", UIUtil.GetImage("CodeLab"), "Advanced", BitmapEffectOptions.Create() with { IsConfigurable = true })
        {
            renderPreset = Settings.RenderPreset;
        }

        private readonly RenderPreset renderPreset;

#if !FASTDEBUG
        protected override IEffectConfigForm OnCreateConfigForm()
        {
            return new CodeLabConfigDialog(renderPreset);
        }
#endif

        private IEffect userEffect;
        private bool fetchDebugMsg;
        private ProjectType projectType;
        private string shapeCode;
        private IBitmapEffectRenderer renderer;
        private IBitmapSource<ColorBgra32> sourceBitmap;

        // These are the rendering options that the user specified in settings. CodeLab will always
        // specify disabled selection clipping, and default antialiasing quality. We do have
        // to pass the schedule along to Paint.NET though.
        // TODO: The effect script should be able to directly specify what it wants, rather than
        //       having these in CodeLab's global settings. If that happens, this information will
        //       be available (somewhere) in the Token, and we can update these two fields and let
        //       OnRender() do its thing.
        private BitmapEffectRenderingFlags renderingFlags;
        private BitmapEffectRenderingSchedule renderingSchedule;

        protected override void OnInitializeRenderInfo(IBitmapEffectRenderInfo renderInfo)
        {
            this.sourceBitmap = this.Environment.GetSourceBitmapBgra32();

            switch (renderPreset)
            {
                case RenderPreset.Regular:
                    // No need to set anything; use the defaults
                    this.renderingFlags = BitmapEffectRenderingFlags.None;
                    this.renderingSchedule = BitmapEffectRenderingSchedule.SquareTiles;
                    break;
                case RenderPreset.LegacyROI:
                    this.renderingFlags = BitmapEffectRenderingFlags.None;
                    this.renderingSchedule = BitmapEffectRenderingSchedule.HorizontalStrips;
                    break;
                case RenderPreset.AliasedSelection:
                    this.renderingFlags = BitmapEffectRenderingFlags.ForceAliasedSelectionQuality;
                    this.renderingSchedule = BitmapEffectRenderingSchedule.SquareTiles;
                    break;
                case RenderPreset.SingleRenderCall:
                    this.renderingFlags = BitmapEffectRenderingFlags.None;
                    this.renderingSchedule = BitmapEffectRenderingSchedule.None;
                    break;
                case RenderPreset.NoSelectionClip:
                    this.renderingFlags = BitmapEffectRenderingFlags.DisableSelectionClipping;
                    this.renderingSchedule = BitmapEffectRenderingSchedule.SquareTiles;
                    break;
                case RenderPreset.UserDefined:
                    this.renderingFlags = Settings.RenderingFlags;
                    this.renderingSchedule = Settings.RenderingSchedule;
                    break;
            }

            renderInfo.Flags = BitmapEffectRenderingFlags.DisableSelectionClipping;
            renderInfo.Schedule = this.renderingSchedule;

            base.OnInitializeRenderInfo(renderInfo);
        }

        protected override void OnSetToken(CodeLabConfigToken newToken)
        {
            projectType = newToken.ProjectType;
            fetchDebugMsg = true;
            shapeCode = (projectType == ProjectType.Shape) ? newToken.UserCode : null;

            if (projectType.IsEffect() && newToken.UserScriptObject != null)
            {
                if (userEffect != newToken.UserScriptObject)
                {
                    userEffect = newToken.UserScriptObject;

                    // TODO: may need this logic elsewhere, toehead/boltbait will need to figure that out
                    using IEffectEnvironment environment = this.renderingFlags.HasFlag(BitmapEffectRenderingFlags.ForceAliasedSelectionQuality)
                        ? this.Environment.CreateRef()
                        : this.Environment.CloneWithAliasedSelection();
                    using (IEffect effect = userEffect.EffectInfo.CreateInstance(this.Services, environment))
                    {
                        this.renderer = effect.CreateRenderer<IBitmapEffectRenderer>();
                    }
                    BitmapEffectInitializeInfo initializeInfo = new BitmapEffectInitializeInfo();
                    this.renderer.Initialize(initializeInfo);
                }

                this.renderer.SetToken(newToken.PreviewToken);
            }

            base.OnSetToken(newToken);
        }

        protected override void OnRender(IBitmapEffectOutput output)
        {
            using IBitmapLock<ColorBgra32> outputLock = output.LockBgra32();

            if (projectType == ProjectType.Shape)
            {
                WpfGeometry wpfGeometry = ShapeBuilder.GeometryFromRawString(shapeCode);
                if (wpfGeometry == null)
                {
                    this.sourceBitmap.CopyPixels(outputLock, output.Bounds.Location);
                    return;
                }

                IDirect2DFactory d2dFactory = this.Services.GetService<IDirect2DFactory>();
                using IGeometry d2dGeometry = d2dFactory.CreateGeometryFromWpfGeometry(wpfGeometry);

                RectFloat geoBounds = d2dGeometry.GetWidenedBounds(this.Environment.BrushSize);
                RectInt32 selBounds = this.Environment.Selection.RenderBounds;

                float scale = (selBounds.Width - geoBounds.Width) < (selBounds.Height - geoBounds.Height)
                    ? (selBounds.Width - 10) / geoBounds.Width
                    : (selBounds.Height - 10) / geoBounds.Height;

                float selCenterX = (selBounds.Right - selBounds.Left) / 2f + selBounds.Left;
                float selCenterY = (selBounds.Bottom - selBounds.Top) / 2f + selBounds.Top;

                Matrix3x2Float matrix = Matrix3x2Float.Translation(
                    (selBounds.Width - geoBounds.Width) / 2f - geoBounds.Left + selBounds.Left,
                    (selBounds.Height - geoBounds.Height) / 2f - geoBounds.Top + selBounds.Top);

                matrix.ScaleAt(scale, scale, selCenterX, selCenterY);

                using ITransformedGeometry transformedGeometry = d2dFactory.CreateTransformedGeometry(d2dGeometry, matrix);

                using IBitmap<ColorBgra32> outputBitmap = outputLock.CreateSharedBitmap();
                using IBitmap<ColorPbgra32> outputBitmapP = outputBitmap.CreatePremultipliedAdapter(PremultipliedAdapterOptions.UnPremultiplyOnDispose | PremultipliedAdapterOptions.PremultiplyOnCreate);
                using IDeviceContext outputDC = d2dFactory.CreateBitmapDeviceContext(outputBitmapP);
                using ISolidColorBrush strokeBrush = outputDC.CreateSolidColorBrush(this.Environment.PrimaryColor.GetSrgb());
                using ISolidColorBrush solidBrush = outputDC.CreateSolidColorBrush(this.Environment.SecondaryColor.GetSrgb());

                using (outputDC.UseBeginDraw())
                {
                    outputDC.Clear();

                    ICommandList commandList = outputDC.CreateCommandList();
                    using (commandList.UseBeginDraw(outputDC))
                    {
                        using IBitmapImage srcImage = outputDC.CreateImageFromBitmap(this.Environment.GetSourceBitmapBgra32());
                        outputDC.DrawImage(srcImage);

                        using (outputDC.UseTranslateTransform(0.5f, 0.5f))
                        {
                            outputDC.FillGeometry(transformedGeometry, solidBrush);
                            outputDC.DrawGeometry(transformedGeometry, strokeBrush, this.Environment.BrushSize);
                        }
                    }

                    using (outputDC.UseTranslateTransform(-output.Bounds.Location))
                    {
                        outputDC.DrawImage(commandList);
                    }
                }
            }
            else if (projectType.IsEffect() && userEffect != null)
            {
                try
                {
                    if (this.renderer == null || this.renderer.IsDisposed)
                    {
                        this.sourceBitmap.CopyPixels(outputLock, output.Bounds.Location);
                    }
                    else
                    {
                        bool disableSelectionClipping = this.renderingFlags.HasFlag(BitmapEffectRenderingFlags.DisableSelectionClipping);
                        bool forceAliasedRendering = this.renderingFlags.HasFlag(BitmapEffectRenderingFlags.ForceAliasedSelectionQuality);

                        // Classic effects do not support DisableSelectionClipping
                        bool isClassicEffect = projectType == ProjectType.ClassicEffect;

                        if (disableSelectionClipping && !isClassicEffect)
                        {
                            // Render directly to the output buffer
                            this.renderer.Render(outputLock, output.Bounds.Location);
                        }
                        else
                        {
                            // Copy the source to the output
                            RegionPtr<ColorBgra32> outputRegion = outputLock.AsRegionPtr();
                            this.sourceBitmap.CopyPixels(outputRegion, output.Bounds.Location);

                            // Render effect to a temporary buffer (bitmaps are pooled/recycled, so this allocation is not expensive)
                            using IBitmap<ColorBgra32> renderBitmap = this.Environment.ImagingFactory.CreateBitmap<ColorBgra32>(output.Bounds.Size);
                            using IBitmapLock<ColorBgra32> renderLock = renderBitmap.Lock(BitmapLockOptions.ReadWrite);
                            RegionPtr<ColorBgra32> renderRegion = renderLock.AsRegionPtr();
                            this.renderer.Render(renderRegion, output.Bounds.Location);

                            if (forceAliasedRendering)
                            {
                                // For each rect in the selection, clipped to the output, copy from renderBitmap to output
                                foreach (RectInt32 outputRect in this.Environment.Selection.GetRenderScansClipped(output.Bounds))
                                {
                                    RectInt32 localRect = RectInt32.Offset(outputRect, -output.Bounds.Location);
                                    renderRegion.Slice(localRect)
                                        .CopyTo(outputRegion.Slice(localRect));
                                }
                            }
                            else
                            {
                                // Get the selection mask bitmap that overlaps with the output bounds
                                using IBitmapLock<ColorAlpha8> maskLock = this.Environment.Selection.MaskBitmap.Lock(output.Bounds);
                                RegionPtr<ColorAlpha8> maskRegion = maskLock.AsRegionPtr();
                                
                                // Combine the source layer and effect output. The effect's output is blended "on top" using overwrite
                                // blending mode, with the selection mask modulating each pixel (255 is 100% effect pixel, 0 is 100%
                                // source layer pixel, 127 or 128 is about ~50% of each).
                                PixelKernels.Overwrite(outputRegion, renderRegion, maskRegion);
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    this.Token.LastExceptions.Add(exc);
                    this.sourceBitmap.CopyPixels(outputLock, output.Bounds.Location);
                    this.Token.UserScriptObject = null;
                    userEffect?.Dispose();
                    userEffect = null;
                }

                if (fetchDebugMsg)
                {
                    fetchDebugMsg = false;
                    try
                    {
                        string debugOutput = userEffect?.GetType()
                            .GetProperty("__DebugMsgs", typeof(string))?
                            .GetValue(userEffect)?
                            .ToString();

                        if (!debugOutput.IsNullOrEmpty())
                        {
                            this.Token.Output.Add(debugOutput);
                        }
                    }
                    catch
                    {
                        // just fail silently
                    }
                }
            }
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                userEffect?.Dispose();
                userEffect = null;
            }

            base.OnDispose(disposing);
        }
    }
}
