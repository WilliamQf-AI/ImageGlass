﻿/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2024 DUONG DIEU PHAP
Project homepage: https://imageglass.org

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Diagnostics.Debug;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ImageGlass.Base.WinApi;

public class WindowApi
{
    [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint iUsage, int ppvBits, IntPtr hSection, uint dwOffset);


    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref bool attrValue, int attrSize);



    /// <summary>
    /// <para>Issue #360: IG periodically searching for dismounted device.</para>
    /// <para>
    /// Controls whether the system will handle the specified types of serious errors
    /// or whether the process will handle them.
    /// </para>
    /// <para>
    /// Best practice is that all applications call the process-wide SetErrorMode
    /// function with a parameter of <c>SEM_FAILCRITICALERRORS</c> at startup. This is to
    /// prevent error mode dialogs from hanging the application.
    /// </para>
    /// Ref: <see href="https://docs.microsoft.com/en-us/windows/win32/api/errhandlingapi/nf-errhandlingapi-seterrormode"/>
    /// </summary>
    public static void SetAppErrorMode()
    {
        _ = PInvoke.SetErrorMode(THREAD_ERROR_MODE.SEM_FAILCRITICALERRORS);
    }


    /// <summary>
    /// Sets window state.
    /// </summary>
    public static void ShowAppWindow(IntPtr wndHandle, SHOW_WINDOW_CMD cmd)
    {
        _ = PInvoke.ShowWindow(new HWND(wndHandle), (Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD)cmd);
    }


    /// <summary>
    /// Simulates left mouse click on a window
    /// </summary>
    /// <param name="wndHandle"></param>
    /// <param name="clientPoint"></param>
    public static void ClickOnWindow(IntPtr wndHandle, Point clientPoint)
    {
        var cliPoint = new Point()
        {
            X = clientPoint.X,
            Y = clientPoint.Y,
        };
        var oldPos = Cursor.Position;

        // set cursor on coords, and press mouse
        Cursor.Position = new Point(clientPoint.X, clientPoint.Y);

        // left button down data
        var inputMouseDown = new INPUT
        {
            type = 0,
            Anonymous = new INPUT._Anonymous_e__Union()
            {
                mi = new MOUSEINPUT()
                {
                    dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTDOWN,
                },
            },
        };

        // left button up data
        var inputMouseUp = new INPUT
        {
            type = 0,
            Anonymous = new INPUT._Anonymous_e__Union()
            {
                mi = new MOUSEINPUT()
                {
                    dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTUP,
                },
            },
        };

        var inputs = new INPUT[] { inputMouseDown, inputMouseUp };
        PInvoke.SendInput(inputs.AsSpan(), Marshal.SizeOf(typeof(INPUT)));

        // return mouse 
        Cursor.Position = oldPos;
    }


    /// <summary>
    /// Sets dark mode for window title bar.
    /// </summary>
    public static void SetImmersiveDarkMode(IntPtr wndHandle, bool enabled)
    {
        // ~< 20H1
        if (!BHelper.IsOSBuildOrGreater(18985)) return;

        unsafe
        {
            _ = PInvoke.DwmSetWindowAttribute(new HWND(wndHandle),
                DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                &enabled, sizeof(uint));
        }
    }


    /// <summary>
    /// Sets round corner option for Windows 11.
    /// </summary>
    /// <param name="wndHandle">Window's handle.</param>
    /// <param name="enable"><c>true</c> to make the corner round, <c>false</c> to use square.</param>
    /// <returns>Returns <c>true</c> if succeeded, else <c>false</c>.</returns>
    public static bool SetRoundCorner(IntPtr wndHandle, WindowCorner style = WindowCorner.Round)
    {
        if (!BHelper.IsOS(WindowsOS.Win11OrLater)) return false;

        HRESULT result;
        unsafe
        {
            result = PInvoke.DwmSetWindowAttribute(new HWND(wndHandle),
                DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
                &style, sizeof(uint));
        }

        return result.Succeeded;
    }



    /// <summary>
    /// Sets background and text color for window title bar.
    /// </summary>
    /// <param name="wndHandle"></param>
    /// <param name="bgColor">
    /// Specifying <c>null</c> will reset the caption background color back to using the system's default value.
    /// </param>
    /// <param name="textColor">
    /// Specifying <c>null</c> will reset the caption text color back to using the system's default value.
    /// </param>
    public static void SetTitleBar(IntPtr wndHandle, Color? bgColor, Color? textColor)
    {
        // ~< 20H1
        if (!BHelper.IsOS(WindowsOS.Win11OrLater)) return;

        const uint DWMWA_COLOR_DEFAULT = 0xFFFFFFFF;
        var bg = DWMWA_COLOR_DEFAULT;
        var text = DWMWA_COLOR_DEFAULT;

        if (bgColor != null) bg = bgColor.Value.ToCOLORREF();
        if (textColor != null) text = textColor.Value.ToCOLORREF();

        unsafe
        {
            _ = PInvoke.DwmSetWindowAttribute(new HWND(wndHandle),
                DWMWINDOWATTRIBUTE.DWMWA_CAPTION_COLOR,
                &bg, sizeof(uint));

            _ = PInvoke.DwmSetWindowAttribute(new HWND(wndHandle),
                DWMWINDOWATTRIBUTE.DWMWA_TEXT_COLOR,
                &text, sizeof(uint));
        }
    }



    /// <summary>
    /// Sets windows backdrop.
    /// </summary>
    /// <returns>Returns <c>true</c> if succeeded, else <c>false</c>.</returns>
    public static bool SetWindowBackdrop(IntPtr wndHandle, DWM_SYSTEMBACKDROP_TYPE type = DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE)
    {
        if (!BHelper.IsOS(WindowsOS.Win11OrLater)) return false;

        HRESULT result;
        unsafe
        {
            if (BHelper.IsOS(WindowsOS.Win11_22H2_OrLater))
            {
                var attr = DWMWINDOWATTRIBUTE_UNDOCUMENTED.DWMWA_SYSTEMBACKDROP_TYPE;

                result = PInvoke.DwmSetWindowAttribute(new HWND(wndHandle),
                   (DWMWINDOWATTRIBUTE)attr,
                   &type, sizeof(uint));
            }
            else
            {
                var attr = DWMWINDOWATTRIBUTE_UNDOCUMENTED.DWMWA_MICA_EFFECT;
                var preference = 1;

                result = PInvoke.DwmSetWindowAttribute(new HWND(wndHandle),
                   (DWMWINDOWATTRIBUTE)attr,
                   &preference, sizeof(uint));
            }
        }

        return result.Succeeded;
    }


    /// <summary>
    /// Check if the composition is enabled.
    /// </summary>
    public static bool IsCompositionEnabled()
    {
        if (Environment.OSVersion.Version.Major < 6)
            return false;

        var result = PInvoke.DwmIsCompositionEnabled(out var compositionEnabled);

        return result.Succeeded && compositionEnabled > 0;
    }


    /// <summary>
    /// Extends the window frame to the client area.
    /// </summary>
    /// <returns>Returns <c>true</c> if succeeded, else <c>false</c>.</returns>
    public static bool SetWindowFrame(IntPtr wndHandle, Padding? margin = null)
    {
        if (!IsCompositionEnabled()) return false;

        margin ??= new Padding(0);
        var mg = new MARGINS
        {
            cyTopHeight = margin.Value.Top,
            cxLeftWidth = margin.Value.Left,

            cxRightWidth = margin.Value.Right,
            cyBottomHeight = margin.Value.Bottom,
        };


        var result = PInvoke.DwmExtendFrameIntoClientArea(new HWND(wndHandle), mg);

        return result.Succeeded;
    }


    /// <summary>
    /// Sets the control's black background transparent.
    /// <para>
    /// <c>Note**: </c> The control's <c>BackColor</c> must be set to <see cref="Color.Black"/>.</para>
    /// </summary>
    public static void SetTransparentBlackBackground(Control control, Rectangle rect)
    {
        using var g = control.CreateGraphics();
        SetTransparentBlackBackground(g, rect);
    }


    /// <summary>
    /// Sets the control's black background transparent.
    /// <para>
    /// <c>Note**: </c> The control's <c>BackColor</c> must be set to <see cref="Color.Black"/>.</para>
    /// </summary>
    public static void SetTransparentBlackBackground(Graphics g, Rectangle destRect)
    {
        var destDc = g.GetHdc();
        var createdHdc = PInvoke.CreateCompatibleDC(new HDC(destDc));
        var memoryDc = new HDC(createdHdc);
        var bitmapOld = new HGDIOBJ(IntPtr.Zero);
        var bitmapGdiObj = new HGDIOBJ(IntPtr.Zero);

        var dib = new BITMAPINFO();
        dib.bmiHeader.biHeight = -(destRect.Height);
        dib.bmiHeader.biWidth = destRect.Width;
        dib.bmiHeader.biPlanes = 1;
        dib.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
        dib.bmiHeader.biBitCount = 32;
        dib.bmiHeader.biCompression = 0; // BI_RGB

        if (!(PInvoke.SaveDC(memoryDc) == 0))
        {
            var bitmapPtr = CreateDIBSection(memoryDc, ref dib, (int)DIB_USAGE.DIB_RGB_COLORS, 0, IntPtr.Zero, 0);
            bitmapGdiObj = new HGDIOBJ(bitmapPtr);

            if (!bitmapGdiObj.IsNull)
            {
                bitmapOld = PInvoke.SelectObject(memoryDc, bitmapGdiObj);

                PInvoke.BitBlt(new HDC(destDc), destRect.Left, destRect.Top, destRect.Width, destRect.Height, memoryDc, 0, 0, ROP_CODE.SRCCOPY);
            }

            // Remember to clean up
            _ = PInvoke.SelectObject(memoryDc, bitmapOld);

            _ = PInvoke.DeleteObject(bitmapGdiObj);
            _ = PInvoke.ReleaseDC(new HWND(memoryDc.Value), memoryDc); // (IntPtr, -1)
            _ = PInvoke.DeleteDC(createdHdc);
        }

        g.ReleaseHdc();
    }


    /// <summary>
    /// Checks if this is a touch device.
    /// </summary>
    public static bool IsTouchDevice()
    {
        var numberOfTouches = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_MAXIMUMTOUCHES);

        return numberOfTouches > 0;
    }


    /// <summary>
    /// Makes control resizable. This should be called in <c>MouseDown</c> event.
    /// </summary>
    public static void SetResizerOnMouseDown(IntPtr handle, ResizerType type)
    {
        if (type == ResizerType.None) return;

        const int WM_NCLBUTTONDOWN = 0x00A1;
        var typePtr = new IntPtr((int)type);

        // Release current mouse capture
        _ = PInvoke.ReleaseCapture();

        // Tell the OS that you want to drag the window.
        _ = PInvoke.SendMessage(new HWND(handle), WM_NCLBUTTONDOWN,
            new WPARAM((uint)typePtr), new LPARAM());
    }


    /// <summary>
    /// Sets window top most.
    /// </summary>
    public static void SetWindowTopMost(IntPtr handle, bool enable)
    {
        var topMost = enable ? new HWND(new IntPtr(-1)) : new HWND(new IntPtr(-2));
        _ = PInvoke.SetWindowPos(new HWND(handle), topMost, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
    }
}


/// <summary>
/// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow
/// </summary>
public enum SHOW_WINDOW_CMD : uint
{
    SW_HIDE = 0,
    /// <summary>
    /// Activates and displays a window. If the window is minimized or maximized,
    /// the system restores it to its original size and position. An application
    /// should specify this flag when displaying the window for the first time.
    /// </summary>
    SW_SHOWNORMAL = 1,
    /// <summary>
    /// Activates the window and displays it as a minimized window.
    /// </summary>
    SW_SHOWMINIMIZED = 2,
    /// <summary>
    /// Activates the window and displays it as a maximized window.
    /// </summary>
    SW_SHOWMAXIMIZED = 3,
    /// <summary>
    /// Displays a window in its most recent size and position. This value is similar
    /// to SW_SHOWNORMAL, except that the window is not activated.
    /// </summary>
    SW_SHOWNOACTIVATE = 4,
    /// <summary>
    /// Activates the window and displays it in its current size and position.
    /// </summary>
    SW_SHOW = 5,
    /// <summary>
    /// Minimizes the specified window and activates the next top-level window in
    /// the Z order.
    /// </summary>
    SW_MINIMIZE = 6,
    /// <summary>
    /// Displays the window as a minimized window. This value is similar to
    /// SW_SHOWMINIMIZED, except the window is not activated.
    /// </summary>
    SW_SHOWMINNOACTIVE = 7,
    /// <summary>
    /// Displays the window in its current size and position. This value is similar
    /// to SW_SHOW, except that the window is not activated.
    /// </summary>
    SW_SHOWNA = 8,
    /// <summary>
    /// Activates and displays the window. If the window is minimized or maximized,
    /// the system restores it to its original size and position.
    /// An application should specify this flag when restoring a minimized window.
    /// </summary>
    SW_RESTORE = 9,
    /// <summary>
    /// Sets the show state based on the SW_ value specified in the STARTUPINFO structure
    /// passed to the CreateProcess function by the program that started the application.
    /// </summary>
    SW_SHOWDEFAULT = 10,
    /// <summary>
    /// Minimizes a window, even if the thread that owns the window is not responding.
    /// This flag should only be used when minimizing windows from a different thread.
    /// </summary>
    SW_FORCEMINIMIZE = 11,
}


/// <summary>
/// The DWM_WINDOW_CORNER_PREFERENCE enum for DwmSetWindowAttribute's third parameter, 
/// which tells the function what value of the enum to set.
/// </summary>
public enum WindowCorner
{
    // DWMWCP_DEFAULT
    Default = 0,

    /// <summary>
    /// DWMWCP_DONOTROUND
    /// </summary>
    DoNotRound = 1,

    /// <summary>
    /// DWMWCP_ROUND
    /// </summary>
    Round = 2,

    /// <summary>
    /// DWMWCP_ROUNDSMALL
    /// </summary>
    RoundSmall = 3,
}

internal enum DWMWINDOWATTRIBUTE_UNDOCUMENTED
{
    DWMWA_SYSTEMBACKDROP_TYPE = 38,
    DWMWA_MICA_EFFECT = 1029,
}


public enum DWM_SYSTEMBACKDROP_TYPE
{
    /// <summary>
    /// Let OS decides.
    /// </summary>
    DWMSBT_AUTO = 0,

    /// <summary>
    /// No effect.
    /// </summary>
    DWMSBT_NONE = 1,

    /// <summary>
    /// Mica effect.
    /// </summary>
    DWMSBT_MAINWINDOW = 2,

    /// <summary>
    /// Acrylic effect.
    /// </summary>
    DWMSBT_TRANSIENTWINDOW = 3,

    /// <summary>
    /// Draw the backdrop material effect corresponding to a window with a tabbed title bar.
    /// </summary>
    DWMSBT_TABBEDWINDOW = 4,
}


public enum ResizerType : int
{
    None = -1,

    HTTOP = 12,
    HTTOPLEFT = 13,
    HTTOPRIGHT = 14,

    HTBOTTOM = 15,
    HTBOTTOMLEFT = 16,
    HTBOTTOMRIGHT = 17,

    HTLEFT = 10,
    HTRIGHT = 11,
}
