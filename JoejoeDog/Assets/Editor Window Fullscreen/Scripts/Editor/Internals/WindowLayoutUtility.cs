/* 
 * Author:  Johanan Round
 * Package: Editor Window Fullscreen
 * License: Unity Asset Store EULA (Editor extension asset. Requires 1 license per machine.)
 */

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

static class WindowLayoutUtility
{
    internal static System.Type windowLayoutType;
    internal static string projectLibraryPath;

    static WindowLayoutUtility ()
    {
        windowLayoutType = System.Type.GetType("UnityEditor.WindowLayout,UnityEditor");
        projectLibraryPath = Directory.GetCurrentDirectory() + "/Library";
    }

    public static void SaveProjectLayout(string layoutFileName)
    {
        MethodInfo SaveWindowLayout = windowLayoutType.GetMethod("SaveWindowLayout", new[] { typeof(string) });
        SaveWindowLayout.Invoke(null, new[] { Path.Combine(projectLibraryPath, layoutFileName) });
    }

    public static void LoadProjectLayout(string layoutFileName)
    {
        MethodInfo LoadWindowLayout;
        LoadWindowLayout = windowLayoutType.GetMethod("LoadWindowLayout", new[] { typeof(string), typeof(bool) });
        LoadWindowLayout.Invoke(null, new object[] { Path.Combine(projectLibraryPath, layoutFileName), false });
    }

}
