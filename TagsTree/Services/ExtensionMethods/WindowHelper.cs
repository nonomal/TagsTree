﻿using Microsoft.UI.Xaml;
using PInvoke;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace TagsTree.Services.ExtensionMethods;

public static class WindowHelper
{
    public static MainWindow Window { get; private set; } = null!;
    public static nint HWnd { get; private set; } = 0;

    public static MainWindow Initialize()
    {
        Window = new();

        if (Window.Content is FrameworkElement frameworkElement)
            frameworkElement.RequestedTheme = App.AppConfiguration.Theme switch
            {
                1 => ElementTheme.Light,
                2 => ElementTheme.Dark,
                _ => frameworkElement.RequestedTheme
            };

        // 等效于 HWnd = PInvoke.User32.GetActiveWindow();
        HWnd = WindowNative.GetWindowHandle(Window);
        // EnableMica(HWnd, darkThemeEnabled);
        return Window;
    }

    public static MainWindow SetWindowSize(this MainWindow mainWindow, int width, int height)
    {
        // Win32 uses pixels and WinUI 3 uses effective pixels, so you should apply the DPI scale factor
        var dpi = User32.GetDpiForWindow(HWnd);
        var scalingFactor = (float)dpi / 96;
        width = (int)(width * scalingFactor);
        height = (int)(height * scalingFactor);
        _ = User32.SetWindowPos(HWnd, User32.SpecialWindowHandles.HWND_TOP, 0, 0, width, height, User32.SetWindowPosFlags.SWP_NOMOVE);
        return mainWindow;
    }

    public static T InitializeWithWindow<T>(this T obj)
    {
        // When running on win32, FileOpenPicker needs to know the top-level hWnd via IInitializeWithWindow::Initialize.
        if (Microsoft.UI.Xaml.Window.Current is null)
            //HWnd 或者 User32.GetActiveWindow()
            obj.As<Imports.IInitializeWithWindow>()?.Initialize(HWnd);
        return obj;
    }

    private static void EnableMica(nint hWnd, bool darkThemeEnabled)
    {
        var trueValue = 1;
        var falseValue = 0;
        _ = darkThemeEnabled
            ? Imports.DwmSetWindowAttribute(hWnd, Imports.DwmWindowAttribute.DwmwaUseImmersiveDarkMode, ref trueValue, Marshal.SizeOf<int>())
            : Imports.DwmSetWindowAttribute(hWnd, Imports.DwmWindowAttribute.DwmwaUseImmersiveDarkMode, ref falseValue, Marshal.SizeOf<int>());
        _ = Imports.DwmSetWindowAttribute(hWnd, Imports.DwmWindowAttribute.DwmwaMicaEffect, ref trueValue, Marshal.SizeOf<int>());
    }
}
