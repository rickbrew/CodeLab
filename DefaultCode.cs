﻿namespace PdnCodeLab
{
    internal static class DefaultCode
    {
        internal static string Default => ProjectType.Default switch
        {
            ProjectType.ClassicEffect => ClassicEffect,
            ProjectType.GpuEffect => string.Empty,
            ProjectType.BitmapEffect => BitmapEffect,
            _ => string.Empty,
        };

        internal const string ClassicEffect = ""
            + "// Name:\r\n"
            + "// Submenu:\r\n"
            + "// Author:\r\n"
            + "// Title:\r\n"
            + "// Version:\r\n"
            + "// Desc:\r\n"
            + "// Keywords:\r\n"
            + "// URL:\r\n"
            + "// Help:\r\n"
            + "#region UICode\r\n"
            + "IntSliderControl Amount1 = 0; // [0,100] Slider 1 Description\r\n"
            + "IntSliderControl Amount2 = 0; // [0,100] Slider 2 Description\r\n"
            + "IntSliderControl Amount3 = 0; // [0,100] Slider 3 Description\r\n"
            + "#endregion\r\n"
            + "\r\n"
            + "void Render(Surface dst, Surface src, Rectangle rect)\r\n"
            + "{\r\n"
            + "    // Delete any of these lines you don't need\r\n"
            + "    Rectangle selection = EnvironmentParameters.SelectionBounds;\r\n"
            + "    int centerX = ((selection.Right - selection.Left) / 2) + selection.Left;\r\n"
            + "    int centerY = ((selection.Bottom - selection.Top) / 2) + selection.Top;\r\n"
            + "    ColorBgra primaryColor = EnvironmentParameters.PrimaryColor;\r\n"
            + "    ColorBgra secondaryColor = EnvironmentParameters.SecondaryColor;\r\n"
            + "    int brushWidth = (int)EnvironmentParameters.BrushWidth;\r\n"
            + "\r\n"
            + "    ColorBgra currentPixel;\r\n"
            + "    for (int y = rect.Top; y < rect.Bottom; y++)\r\n"
            + "    {\r\n"
            + "        if (IsCancelRequested) return;\r\n"
            + "        for (int x = rect.Left; x < rect.Right; x++)\r\n"
            + "        {\r\n"
            + "            currentPixel = src[x,y];\r\n"
            + "            // TODO: Add pixel processing code here\r\n"
            + "            // Access RGBA values this way, for example:\r\n"
            + "            // currentPixel.R = primaryColor.R;\r\n"
            + "            // currentPixel.G = primaryColor.G;\r\n"
            + "            // currentPixel.B = primaryColor.B;\r\n"
            + "            // currentPixel.A = primaryColor.A;\r\n"
            + "            dst[x,y] = currentPixel;\r\n"
            + "        }\r\n"
            + "    }\r\n"
            + "}\r\n";

        internal const string BitmapEffect = ""
            + "// Name:\r\n"
            + "// Submenu:\r\n"
            + "// Author:\r\n"
            + "// Title:\r\n"
            + "// Version:\r\n"
            + "// Desc:\r\n"
            + "// Keywords:\r\n"
            + "// URL:\r\n"
            + "// Help:\r\n"
            + "#region UICode\r\n"
            + "IntSliderControl Amount1 = 0; // [0,100] Slider 1 Description\r\n"
            + "IntSliderControl Amount2 = 0; // [0,100] Slider 2 Description\r\n"
            + "IntSliderControl Amount3 = 0; // [0,100] Slider 3 Description\r\n"
            + "#endregion\r\n"
            + "\r\n"
            + "protected override void OnRender(IBitmapEffectOutput output)\r\n"
            + "{\r\n"
            + "    ColorBgra32 primaryColor = Environment.PrimaryColor;\r\n"
            + "    ColorBgra32 secondaryColor = Environment.SecondaryColor;\r\n"
            + "\r\n"
            + "    using IBitmapLock<ColorBgra32> outputLock = output.LockBgra32();\r\n"
            + "    RegionPtr<ColorBgra32> outputRegion = outputLock.AsRegionPtr();\r\n"
            + "\r\n"
            + "    using IBitmap<ColorBgra32> sourceTile = Environment\r\n"
            + "        .GetSourceBitmapBgra32()\r\n"
            + "        .ToBitmap();\r\n"
            + "\r\n"
            + "    if (this.IsCancelRequested)\r\n"
            + "    {\r\n"
            + "        return;\r\n"
            + "    }\r\n"
            + "\r\n"
            + "    using IBitmapLock<ColorBgra32> sourceTileLock = sourceTile.Lock(BitmapLockOptions.Read);\r\n"
            + "    var sourceRegion = sourceTileLock.AsRegionPtr();\r\n"
            + "\r\n"
            + "    for (int outputDY = 0; outputDY < outputRegion.Height; ++outputDY)\r\n"
            + "    {\r\n"
            + "        if (this.IsCancelRequested)\r\n"
            + "        {\r\n"
            + "            return;\r\n"
            + "        }\r\n"
            + "\r\n"
            + "        for (int outputDX = 0; outputDX < outputRegion.Width; ++outputDX)\r\n"
            + "        {\r\n"
            + "            ColorBgra32 outputBgra32 = sourceRegion[outputDX, outputDY];\r\n"
            + "            //outputBgra32.R = primaryColor.R;\r\n"
            + "            //outputBgra32.G = primaryColor.G;\r\n"
            + "            //outputBgra32.B = primaryColor.B;\r\n"
            + "            //outputBgra32.A = primaryColor.A;\r\n"
            + "\r\n"
            + "            outputRegion[outputDX, outputDY] = outputBgra32;\r\n"
            + "        }\r\n"
            + "    }\r\n"
            + "}\r\n";
    }
}
