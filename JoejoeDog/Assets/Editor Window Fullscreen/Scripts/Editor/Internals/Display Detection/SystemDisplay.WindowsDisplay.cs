/* 
 * Author:  Johanan Round
 */

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using EditorWindowFullscreen;

public partial class SystemDisplay
{
    /// <summary>
    /// Windows-specific methods of SystemDisplay
    /// </summary>
    private class WindowsDisplay
    {

        public static List<SystemDisplay> GetAllDisplays()
        {
            List<SystemDisplay> allDisplays = new List<SystemDisplay>();

            IntPtr hMainWindowMonitor = IntPtr.Zero;
            try
            {
                IntPtr mainWindowHandle = GetProcessMainWindow();
                if (mainWindowHandle != IntPtr.Zero)
                {
                    var mainWindowMonitorInfoEx = MonitorInfoEx.CreateWithDefaults();
                    hMainWindowMonitor = MonitorFromWindow(mainWindowHandle, MONITOR_DEFAULTTONEAREST);
                    GetMonitorInfo(hMainWindowMonitor, ref mainWindowMonitorInfoEx);
                }
            }
            catch { }

            var deviceDisplayMonitorCount = new Dictionary<string, uint>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData)
                {
                    try
                    {
                        //Get the monitor info
                        var monitorInfoEx = MonitorInfoEx.CreateWithDefaults();
                        GetMonitorInfo(hMonitor, ref monitorInfoEx);

                        //Get the associated display device
                        bool mirroringDriver = false;
                        bool attachedToDesktop = false;
                        string deviceName = monitorInfoEx.DeviceName;

                        if (!deviceDisplayMonitorCount.ContainsKey(deviceName)) deviceDisplayMonitorCount.Add(deviceName, 0);
                        deviceDisplayMonitorCount[deviceName] += 1;

                        var displayDevice = Display_Device.CreateWithDefaults();
                        int displayMonitor = 0;
                        for (uint id = 0; EnumDisplayDevices(deviceName, id, ref displayDevice, 0); id++)
                        {
                            attachedToDesktop = ((displayDevice.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) == DisplayDeviceStateFlags.AttachedToDesktop);

                            if (attachedToDesktop)
                            {
                                displayMonitor++;
                                if (displayMonitor == deviceDisplayMonitorCount[deviceName])
                                {
                                    mirroringDriver = ((displayDevice.StateFlags & DisplayDeviceStateFlags.MirroringDriver) == DisplayDeviceStateFlags.MirroringDriver);
                                    break; //Found the display device which matches the monitor
                                }
                            }

                            displayDevice.Size = Marshal.SizeOf(displayDevice);
                        }

                        //Skip the monitor if it's a pseudo monitor
                        if (mirroringDriver) return true;

                        //Store the monitor info in a SystemDisplay object
                        var display = new SystemDisplay();
                        display.Name = displayDevice.DeviceString;
                        display.AttachedToDesktop = attachedToDesktop; //Should always be true within EnumDisplayMonitors
                        display.IsPrimary = monitorInfoEx.Flags == (uint)1;
                        display.HasMainWindow = (hMonitor == hMainWindowMonitor);
                        display.Bounds = RectFromRectStruct(lprcMonitor);
                        display.WorkArea = RectFromRectStruct(monitorInfoEx.WorkAreaBounds);

                        var devMode = new DEVMODE();
                        EnumDisplaySettings(monitorInfoEx.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode);
                        display.PixelWidth = devMode.dmPelsWidth;
                        display.PixelHeight = devMode.dmPelsHeight;

                        //Add the SystemDisplay to allDisplays
                        allDisplays.Add(display);

                    }
                    catch (Exception e) { Debug.Log(e); }

                    return true; //Continue the enumeration
                }, IntPtr.Zero);

            //Calculate physical bounds
            foreach (var display in allDisplays)
            {
                Rect physicalBounds = display.Bounds;
                physicalBounds.width = display.PixelWidth;
                physicalBounds.height = display.PixelHeight;
                Vector2 displayTopLeft = new Vector2(display.Bounds.xMin, display.Bounds.yMin);
                
                var displayTopLeftPhysical = GetPhysicalPoint(GetProcessMainWindow(), displayTopLeft);
                physicalBounds.x = displayTopLeftPhysical.x;
                physicalBounds.y = displayTopLeftPhysical.y;
                display.PhysicalBounds = physicalBounds;
            }

            return allDisplays;
        }

        private static Rect RectFromRectStruct(RectStruct rectStruct)
        {
            return new Rect(rectStruct.Left, rectStruct.Top, rectStruct.Right - rectStruct.Left, rectStruct.Bottom - rectStruct.Top);
        }

        /// Makes sure a window covers the taskbar when it is fullscreen
        internal static void MakeWindowCoverTaskBar(string windowTitle, SystemDisplay display)
        {
            IntPtr windowHandle = GetProcessWindow(null, windowTitle, true);
            int existingExStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);

            SetWindowLong(windowHandle, GWL_EXSTYLE,
                         existingExStyle & (int)(WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE));

            SetWindowPos(windowHandle, IntPtr.Zero, (int)display.Bounds.x, (int)display.Bounds.y, (int)display.Bounds.width, (int)display.Bounds.height,
                         SWP.NOZORDER | SWP.FRAMECHANGED | SWP.NOACTIVATE);
        }

        internal static bool WindowIsFullscreenOnDisplay(string windowTitle, SystemDisplay display)
        {
            IntPtr windowHandle = GetProcessWindow(null, windowTitle, true);

            Rect winPhysBounds = GetWindowPhysicalBounds(windowHandle);
            Rect displayPhysBounds = display.PhysicalBounds;

            float padding = 1;
            winPhysBounds.xMin -= padding;
            winPhysBounds.xMax += padding;
            winPhysBounds.yMin -= padding;
            winPhysBounds.yMax += padding;

            return winPhysBounds.Contains(displayPhysBounds);
        }

        internal static Rect GetWindowBounds(IntPtr windowHandle)
        {
            RectStruct winRectStruct = new RectStruct();
            GetWindowRect(windowHandle, ref winRectStruct);
            Rect winRect = RectFromRectStruct(winRectStruct);
            return winRect;
        }

        internal static Rect GetWindowPhysicalBounds(IntPtr windowHandle)
        {
            RectStruct winRect = new RectStruct();
            GetWindowRect(windowHandle, ref winRect);

            POINT winTopLeft = GetPhysicalPoint(windowHandle, new POINT(winRect.Left, winRect.Top));
            POINT win100 = GetPhysicalPoint(windowHandle, new POINT(winRect.Left + 100, winRect.Top));
            float scalingFactor = (win100.X - winTopLeft.X) / 100f;
            Rect winPhysicalBounds = new Rect(winTopLeft.X, winTopLeft.Y, Mathf.CeilToInt((winRect.Right - winRect.Left) * scalingFactor), Mathf.CeilToInt((winRect.Bottom - winRect.Top) * scalingFactor));

            return winPhysicalBounds;
        }

        internal static Vector2 GetPhysicalPoint(Vector2 logicalPoint)
        {
            IntPtr mainWindowHandle = GetProcessMainWindow();
            RectStruct winRect = new RectStruct();
            GetWindowRect(mainWindowHandle, ref winRect);
            POINT winTopLeft = GetPhysicalPoint(mainWindowHandle, new POINT(winRect.Left, winRect.Top));
            POINT win100 = GetPhysicalPoint(mainWindowHandle, new POINT(winRect.Left + 100, winRect.Top));
            float scalingFactor = (win100.X - winTopLeft.X) / 100f;

            Vector2 physicalPoint = logicalPoint * scalingFactor;
            physicalPoint.x = Mathf.RoundToInt(physicalPoint.x);
            physicalPoint.y = Mathf.RoundToInt(physicalPoint.y);
            return physicalPoint;
        }
        internal static Vector2 GetPhysicalPoint(IntPtr windowHandle, Vector2 logicalPoint)
        {
            POINT physPoint = GetPhysicalPoint(windowHandle, new POINT((int)logicalPoint.x, (int)logicalPoint.y));
            return new Vector2(physPoint.X, physPoint.Y);
        }
        internal static POINT GetPhysicalPoint(IntPtr windowHandle, POINT logicalPoint)
        {
            POINT physicalPoint = new POINT(logicalPoint.X, logicalPoint.Y);
            try
            {
                LogicalToPhysicalPointForPerMonitorDPI(windowHandle, ref physicalPoint);
            }
            catch
            {
                try
                {
                    LogicalToPhysicalPoint(windowHandle, ref physicalPoint);
                }
                catch
                {
                    //Point remains logical
                }
            }
            return physicalPoint;
        }

        /**** Windows API Calls ****/

        private const int CCHDEVICENAME = 32; //Size of device name string

        delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref Display_Device lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);
        const int ENUM_CURRENT_SETTINGS = -1;
        const int ENUM_REGISTRY_SETTINGS = -2;

        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
        const int MONITOR_DEFAULTTONULL = 0;
        const int MONITOR_DEFAULTTOPRIMARY = 1;
        const int MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref RectStruct rect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool LogicalToPhysicalPointForPerMonitorDPI(IntPtr hWnd, ref POINT lpPoint); //Win 8.1-10
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool LogicalToPhysicalPoint(IntPtr hWnd, ref POINT lpPoint); //Win Vista-8

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        private delegate bool EnumWindow(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool EnumWindows(EnumWindow lpEnumFunc, IntPtr lParam);
        private static IntPtr GetProcessMainWindow()
        {
            return GetProcessWindow("UnityContainerWndClass", "Unity", false);
        }
        private static IntPtr GetProcessWindow(string withClassName, string withTitleMatching, bool fullTitleMatch)
        {
            IntPtr hMainWindow = IntPtr.Zero;
            int processID = System.Diagnostics.Process.GetCurrentProcess().Id;
            EnumWindows(
                delegate (IntPtr windowHandle, IntPtr lParam)
                {
                    uint winProcessID;
                    GetWindowThreadProcessId(windowHandle, out winProcessID);

                    if (winProcessID == processID)
                    {
                        StringBuilder className = new StringBuilder(256);

                        GetClassName(windowHandle, className, className.Capacity);

                        if (withClassName == null || className.ToString() == withClassName)
                        {
                            StringBuilder titleText = new StringBuilder(1024);
                            GetWindowText(windowHandle, titleText, titleText.MaxCapacity);

                            if (withTitleMatching != null && ((!fullTitleMatch && titleText.ToString().StartsWith(withTitleMatching)) || titleText.ToString() == withTitleMatching))
                            {
                                hMainWindow = windowHandle;
                                return false;
                            }
                        }
                    }
                    return true;
                }, IntPtr.Zero);

            return hMainWindow;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RectStruct
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct MonitorInfoEx
        {
            public int Size; //Must be set to the byte size of this structure (Use Marshal.SizeOf()), so that GetMonitorInfo knows which struct is being passed.
            public RectStruct MonitorBounds; //Monitor bounds on the virtual screen
            public RectStruct WorkAreaBounds; //Work Area of the monitor (The bounds which a window maximizes to)
            public uint Flags; //If this value is 1, the monitor is the primary display
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string DeviceName; //The device name of the monitor

            public static MonitorInfoEx CreateWithDefaults()
            {
                var mi = new MonitorInfoEx();
                mi.DeviceName = String.Empty;
                mi.Size = Marshal.SizeOf(mi);
                return mi;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct Display_Device
        {
            [MarshalAs(UnmanagedType.U4)]
            public int Size;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;

            public static Display_Device CreateWithDefaults()
            {
                var dd = new Display_Device();
                dd.Size = Marshal.SizeOf(dd);
                return dd;
            }
        }

        [Flags()]
        public enum DisplayDeviceStateFlags : int
        {
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            PrimaryDevice = 0x4,
            MirroringDriver = 0x8, //Represents a pseudo device used to mirror application drawing for remoting or other purposes. An invisible pseudo monitor is associated with this device.
            VGACompatible = 0x10,
            Removable = 0x20,
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        //Constants for use with SetWindowPos
        #pragma warning disable 0414
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        static readonly IntPtr HWND_TOP = new IntPtr(0);
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        #pragma warning restore 0414

        /// <summary>
        /// Window handles (HWND) used for hWndInsertAfter
        /// </summary>
        public static class HWND
        {
            public static IntPtr
            NoTopMost = new IntPtr(-2),
            TopMost = new IntPtr(-1),
            Top = new IntPtr(0),
            Bottom = new IntPtr(1);
        }

        /// <summary>
        /// SetWindowPos Flags
        /// </summary>
        public struct SWP
        {
            public static readonly uint
            NOSIZE = 0x0001,
            NOMOVE = 0x0002,
            NOZORDER = 0x0004,
            NOREDRAW = 0x0008,
            NOACTIVATE = 0x0010,
            DRAWFRAME = 0x0020,
            FRAMECHANGED = 0x0020,
            SHOWWINDOW = 0x0040,
            HIDEWINDOW = 0x0080,
            NOCOPYBITS = 0x0100,
            NOOWNERZORDER = 0x0200,
            NOREPOSITION = 0x0200,
            NOSENDCHANGING = 0x0400,
            DEFERERASE = 0x2000,
            ASYNCWINDOWPOS = 0x4000;
        }

        const int GWL_HWNDPARENT = (-8);
        const int GWL_ID = (-12);
        const int GWL_STYLE = (-16);
        const int GWL_EXSTYLE = (-20);

        // Window Styles 
        const UInt32 WS_OVERLAPPED = 0;
        const UInt32 WS_POPUP = 0x80000000;
        const UInt32 WS_CHILD = 0x40000000;
        const UInt32 WS_MINIMIZE = 0x20000000;
        const UInt32 WS_VISIBLE = 0x10000000;
        const UInt32 WS_DISABLED = 0x8000000;
        const UInt32 WS_CLIPSIBLINGS = 0x4000000;
        const UInt32 WS_CLIPCHILDREN = 0x2000000;
        const UInt32 WS_MAXIMIZE = 0x1000000;
        const UInt32 WS_CAPTION = 0xC00000; //WS_BORDER or WS_DLGFRAME  
        const UInt32 WS_BORDER = 0x800000;
        const UInt32 WS_DLGFRAME = 0x400000;
        const UInt32 WS_VSCROLL = 0x200000;
        const UInt32 WS_HSCROLL = 0x100000;
        const UInt32 WS_SYSMENU = 0x80000;
        const UInt32 WS_THICKFRAME = 0x40000;
        const UInt32 WS_GROUP = 0x20000;
        const UInt32 WS_TABSTOP = 0x10000;
        const UInt32 WS_MINIMIZEBOX = 0x20000;
        const UInt32 WS_MAXIMIZEBOX = 0x10000;
        const UInt32 WS_TILED = WS_OVERLAPPED;
        const UInt32 WS_ICONIC = WS_MINIMIZE;
        const UInt32 WS_SIZEBOX = WS_THICKFRAME;

        // Extended Window Styles 
        const UInt32 WS_EX_DLGMODALFRAME = 0x0001;
        const UInt32 WS_EX_NOPARENTNOTIFY = 0x0004;
        const UInt32 WS_EX_TOPMOST = 0x0008;
        const UInt32 WS_EX_ACCEPTFILES = 0x0010;
        const UInt32 WS_EX_TRANSPARENT = 0x0020;
        const UInt32 WS_EX_MDICHILD = 0x0040;
        const UInt32 WS_EX_TOOLWINDOW = 0x0080;
        const UInt32 WS_EX_WINDOWEDGE = 0x0100;
        const UInt32 WS_EX_CLIENTEDGE = 0x0200;
        const UInt32 WS_EX_CONTEXTHELP = 0x0400;
        const UInt32 WS_EX_RIGHT = 0x1000;
        const UInt32 WS_EX_LEFT = 0x0000;
        const UInt32 WS_EX_RTLREADING = 0x2000;
        const UInt32 WS_EX_LTRREADING = 0x0000;
        const UInt32 WS_EX_LEFTSCROLLBAR = 0x4000;
        const UInt32 WS_EX_RIGHTSCROLLBAR = 0x0000;
        const UInt32 WS_EX_CONTROLPARENT = 0x10000;
        const UInt32 WS_EX_STATICEDGE = 0x20000;
        const UInt32 WS_EX_APPWINDOW = 0x40000;
        const UInt32 WS_EX_OVERLAPPEDWINDOW = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE);
        const UInt32 WS_EX_PALETTEWINDOW = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST);
        const UInt32 WS_EX_LAYERED = 0x00080000;
        const UInt32 WS_EX_NOINHERITLAYOUT = 0x00100000;
        const UInt32 WS_EX_LAYOUTRTL = 0x00400000;
        const UInt32 WS_EX_COMPOSITED = 0x02000000;
        const UInt32 WS_EX_NOACTIVATE = 0x08000000;
    }
}