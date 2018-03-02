/* 
 * Author:  Johanan Round
 */

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using EditorWindowFullscreen;

/// <summary>
/// Get information about a system display. Recognize multiple displays and their positions on the screen.
/// Multidisplay support for Windows, Mac and Linux. (NOTE: Currently only Windows is supported by this class.)
/// </summary>
public partial class SystemDisplay
{

    public string Name { get; private set; }

    public bool AttachedToDesktop { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool HasMainWindow { get; private set; }

    public Rect Bounds { get; private set; }
    public Rect PhysicalBounds { get; private set; }
    public Rect WorkArea { get; private set; }

    public int PixelWidth { get; private set; }
    public int PixelHeight { get; private set; }

#if UNITY_STANDALONE_WIN
    private class NativeDisplay : WindowsDisplay { }
#elif UNITY_STANDALONE_OSX
    private class NativeDisplay : MacOSDisplay { }
#elif UNITY_STANDALONE_LINUX
    private class NativeDisplay : LinuxDisplay { }
#else
    private class NativeDisplay {public static List<SystemDisplay> GetAllDisplays() {return null;}} //Fallback to single screen on any other platform
#endif

    public SystemDisplay() {
        this.Name = "";
    }

    ///Get all the displays which are attached to the desktop
    public static SystemDisplay[] GetAllDisplays()
    {
        return GetAllDisplays(false);
    }
    ///Get all the displays, and choose whether to include monitors not attached to the desktop
    public static SystemDisplay[] GetAllDisplays(bool IncludeMonitorsNotAttachedToDesktop)
    {
        var allDisplays = NativeDisplay.GetAllDisplays();

        if (!IncludeMonitorsNotAttachedToDesktop)
        {
            //Remove displays not attached to the desktop
            allDisplays.RemoveAll(display => !display.AttachedToDesktop);
        }

        if (allDisplays == null || allDisplays.Count == 0)
        {
            /*Failed to find the displays, so add the primary Screen as a display*/
            var display = new SystemDisplay();
            display.Bounds = new Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height);
            display.PhysicalBounds = display.Bounds;
            display.PixelWidth = (int)display.PhysicalBounds.width;
            display.PixelHeight = (int)display.PhysicalBounds.height;
            display.WorkArea = display.Bounds;
            display.AttachedToDesktop = true;
            display.IsPrimary = true;
            allDisplays.Add(display);
        }

        return allDisplays.ToArray();
    }

    /// Get the system display which contains the specified (x, y) position. Returns null if none of the displays contain the point.
    public static SystemDisplay ContainingPoint(int x, int y)
    {
        var allDisplays = GetAllDisplays();
        return allDisplays == null ? null : allDisplays.ContainingPoint(x, y);
    }

    /// Get the system display containing or closest to the specified (x, y) position.
    public static SystemDisplay ClosestToPoint(int x, int y)
    {
        var allDisplays = GetAllDisplays();
        return allDisplays == null ? null : allDisplays.ClosestToPoint(x, y);
    }
    /// Get the system display containing or closest to the specified point.
    public static SystemDisplay ClosestToPoint(Vector2 point)
    {
        var allDisplays = GetAllDisplays();
        return allDisplays == null ? null : allDisplays.ClosestToPoint(point);
    }

    /// Get the system display which has the main window
    public static SystemDisplay WithMainWindow()
    {
        var allDisplays = GetAllDisplays();
        return allDisplays == null ? null : allDisplays.WithMainWindow();
    }

    /// <summary> Makes a window with the specified title fullscreen on a system display. (This method currently only supports Windows OS) </summary>
    public static void MakeWindowCoverTaskBar(string windowTitle, SystemDisplay display) {
        MethodInfo makeWindowCoverTaskBar = null;
        try
        {
            makeWindowCoverTaskBar = typeof(NativeDisplay).BaseType.GetMethod("MakeWindowCoverTaskBar", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(SystemDisplay) }, null);
        }
        catch { }

        //If the OS Native Display class has the method, call that, otherwise throw an exception.
        if (makeWindowCoverTaskBar != null)
        {
            makeWindowCoverTaskBar.Invoke(null, new object[] { windowTitle, display });
        }
        else
        {
            throw new MissingMethodException("This method is not implemented for the current Operating System.");
        }
    }

    /// <summary>
    /// Obsolete. Use editorWindow.IsFullscreenOnDisplay(display) method.
    /// </summary>
    public static bool EditorWindowIsFullscreenOnDisplay(UnityEditor.EditorWindow editorWin, SystemDisplay display)
    {
        string windowTitle = editorWin.GetWindowTitle();
        MethodInfo windowIsFullscreenOnDisplay = null;
        try
        {
            windowIsFullscreenOnDisplay = typeof(NativeDisplay).BaseType.GetMethod("WindowIsFullscreenOnDisplay", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(SystemDisplay) }, null);
        } catch { }

        bool winIsFullscreen = false;

        //If the OS Native Display class has the method, call that, otherwise use a fallback.
        if (windowIsFullscreenOnDisplay != null)
        {
            winIsFullscreen = (bool)windowIsFullscreenOnDisplay.Invoke(null, new object[] { windowTitle, display});
        } else
        {
            winIsFullscreen = editorWin.position.Contains(display.Bounds) && editorWin.position.width == display.Bounds.width;
        }

        return winIsFullscreen;
    }
    /// Converts a logical point to a physical point
    public static Vector2 LogicalToPhysicalPoint (Vector2 logicalPoint)
    {
        Vector2 physPoint;
        MethodInfo getPhysicalPoint = null;
        try
        {
            getPhysicalPoint = typeof(NativeDisplay).BaseType.GetMethod("GetPhysicalPoint", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Vector2) }, null);
        }
        catch {
            Debug.Log("error finding physical point.");
        }

        //If the OS Native Display class has the method, call that, otherwise use a fallback.
        if (getPhysicalPoint != null)
        {
            physPoint = (Vector2)getPhysicalPoint.Invoke(null, new object[] { logicalPoint });
        }
        else
        {
            physPoint = logicalPoint;
        }

        return physPoint;
    }
}

public static class SystemDisplayExtensions
{
    /// Returns true if the display contains the specified logical point (logical point differs from physical point when there is display scaling)
    public static bool ContainsPoint(this SystemDisplay display, Vector2 logicalPoint)
    {
        return (display.Bounds.Contains(logicalPoint));
    }
    public static bool ContainsPoint(this SystemDisplay display, Vector2 point, bool physicalPoint)
    {
        if (physicalPoint)
        {
            return display.PhysicalBounds.Contains(point);
        }
        else
        {
            return (display.Bounds.Contains(point));
        }
    }

    /// Get the system display within the array which contains the specified (x, y) position. 
    public static SystemDisplay ContainingPoint(this SystemDisplay[] displayList, int x, int y)
    {
        var physicalPoint = new Vector2(x, y);
        return displayList.ContainingPoint(physicalPoint);
    }

    /// Get the system display within the array which contains the specified point. Returns null if none of the displays contain the point.
    public static SystemDisplay ContainingPoint(this SystemDisplay[] displayList, Vector2 logicalPoint)
    {
        return displayList.ContainingPoint(logicalPoint, false);
    }
    public static SystemDisplay ContainingPoint(this SystemDisplay[] displayList, Vector2 point, bool physicalPoint)
    {
        foreach (SystemDisplay display in displayList)
        {
            if (display.ContainsPoint(point, physicalPoint)) return display;
        }
        return null;
    }

    /// Get the system display within the array which is containing or closest to the specified (x, y) position.
    public static SystemDisplay ClosestToPoint(this SystemDisplay[] displayList, int x, int y)
    {
        return ClosestToPoint(displayList, new Vector2(x, y));
    }

    /// Get the system display within the array which is containing or closest to the specified point.
    public static SystemDisplay ClosestToPoint(this SystemDisplay[] displayList, Vector2 logicalPoint)
    {
        return displayList.ClosestToPoint(logicalPoint, false);
    }
    public static SystemDisplay ClosestToPoint(this SystemDisplay[] displayList, Vector2 point, bool physicalPoint)
    {
        float closestDistance = 0;
        SystemDisplay closestDisplay = null;

        foreach (SystemDisplay display in displayList)
        {
            if (display.ContainsPoint(point, physicalPoint)) return display;

            var dist = physicalPoint ? display.PhysicalBounds.DistanceToPoint(point) : display.Bounds.DistanceToPoint(point);
            if (dist < closestDistance || closestDisplay == null)
            {
                closestDistance = dist;
                closestDisplay = display;
            }
        }

        return closestDisplay;
    }

    /// Get the system display within the array which has the main window
    public static SystemDisplay WithMainWindow(this SystemDisplay[] displayList)
    {
        foreach (SystemDisplay display in displayList)
        {
            if (display.HasMainWindow) return display;
        }
        return null;
    }

    /// <summary>
    /// Linux-specific methods of SystemDisplay
    /// </summary>
    private class LinuxDisplay
    {
        public static List<SystemDisplay> GetAllDisplays()
        {
            return null;
        }
    }

    /// <summary>
    /// Mac-specific methods of SystemDisplay
    /// </summary>
    private class MacOSDisplay
    {
        public static List<SystemDisplay> GetAllDisplays()
        {
            return null;
        }
    }
}


