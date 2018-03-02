/* 
 * Author:  Johanan Round
 * Package: Editor Window Fullscreen
 * License: Unity Asset Store EULA (Editor extension asset. Requires 1 license per machine.)
 */

using UnityEditor;
using UnityEngine;

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.InteropServices;


namespace EditorWindowFullscreen
{
    /// <summary>
    /// Monitors the current input while in the editor
    /// </summary>
    public sealed class EditorInput : EditorWindow
    {

        private static EditorInput inputWin;

        private EditorInput() { }

        //List of keys currently down
        private static List<KeyCode> KeysDown;

        /// <summary> The Alt key is currently down. </summary>
        public static bool Alt { get; private set; }
        /// <summary> The Control key is currently down. </summary>
        public static bool Ctrl { get; private set; }
        /// <summary> The Shift key is currently down. </summary>
        public static bool Shift { get; private set; }

        /// <summary> This event handler is called on editor keyboard events. </summary>
        public delegate void EventHandler();
        public static event EventHandler KeyEventHandler;

        /// <summary> Last editor key Event. </summary>
        private static Event _LastKeyEvent;
        public static Event LastKeyEvent
        {
            get
            {
                return _LastKeyEvent;
            }
            private set
            {
                _LastKeyEvent = value;
            }
        }

        /// <summary> Current absolute mouse position. </summary>
        private static Vector2 _MousePosition;
        public static Vector2 MousePosition
        {
            get
            {
#if UNITY_STANDALONE_WIN
                _MousePosition = GetMousePosition();
#endif
                return _MousePosition;
            }
            private set
            {
                _MousePosition = value;
            }
        }

        /// <summary> Returns true if the specified key is down. </summary>
        public static bool GetKey(KeyCode keyCode)
        {
            if (KeysDown.Contains(keyCode)) return true;
            else return false;
        }


        private static void SetKeyDown(KeyCode keyCode, bool keyIsDown)
        {
            if (keyIsDown && !KeysDown.Contains(keyCode))
            {
                KeysDown.Add(keyCode);
            }
            else
            {
                KeysDown.Clear();
            }
        }

        public static KeyCode[] GetKeysDown()
        {
            return KeysDown.ToArray();
        }

        public static string GetKeysDownString(List<KeyCode> keysDownList)
        {
            return GetKeysDownString(keysDownList, EventModifiers.None);
        }
        public static string GetKeysDownString(List<KeyCode> keysDownList, EventModifiers modifiers)
        {
            var keysDown = new List<KeyCode>(keysDownList);
            string keysDownString = "";

            if (modifiers.MatchesFlag(EventModifiers.Command))
            {
                keysDownString += "Cmd+";
                keysDown.RemoveAll(kc => kc == KeyCode.LeftCommand || kc == KeyCode.RightCommand);
            }
            if (modifiers.MatchesFlag(EventModifiers.Control))
            {
                keysDownString += "Ctrl+";
                keysDown.RemoveAll(kc => kc == KeyCode.LeftControl || kc == KeyCode.RightControl);
            }
            if (modifiers.MatchesFlag(EventModifiers.Alt))
            {
                keysDownString += "Alt+";
                keysDown.RemoveAll(kc => kc == KeyCode.LeftAlt || kc == KeyCode.RightAlt);
            }
            if (modifiers.MatchesFlag(EventModifiers.Shift))
            {
                keysDownString += "Shift+";
                keysDown.RemoveAll(kc => kc == KeyCode.LeftShift || kc == KeyCode.RightShift);
            }
            if (modifiers.MatchesFlag(EventModifiers.CapsLock))
            {
                keysDownString += "Caps+";
                keysDown.RemoveAll(kc => kc == KeyCode.CapsLock);
            }

            for (int i = 0; i < keysDown.Count; i++)
            {
                if (keysDown[i] == KeyCode.LeftApple || keysDown[i] == KeyCode.RightApple)
                {
                    keysDownString += "Apple+";
                    keysDown[i] = KeyCode.None;
                }
                if (keysDown[i] == KeyCode.LeftCommand || keysDown[i] == KeyCode.RightCommand)
                {
                    keysDownString += "Cmd+";
                    keysDown[i] = KeyCode.None;
                }
                if (keysDown[i] == KeyCode.LeftControl || keysDown[i] == KeyCode.RightControl)
                {
                    keysDownString += "Ctrl+";
                    keysDown[i] = KeyCode.None;
                }
                if (keysDown[i] == KeyCode.LeftAlt || keysDown[i] == KeyCode.RightAlt)
                {
                    keysDownString += "Alt+";
                    keysDown[i] = KeyCode.None;
                }
                if (keysDown[i] == KeyCode.LeftShift || keysDown[i] == KeyCode.RightShift)
                {
                    keysDownString += "Shift+";
                    keysDown[i] = KeyCode.None;
                }
            }

            foreach (KeyCode key in keysDown)
            {
                if (key != KeyCode.None)
                {
                    if (key == KeyCode.Space || key == KeyCode.PageUp || key == KeyCode.PageDown)
                        keysDownString += key.ToString() + "+";
                    else
                        keysDownString += key.ToKeyString() + "+";
                }
            }

            //Remove the last +
            if (keysDownString.Length > 1)
                keysDownString = keysDownString.Substring(0, keysDownString.Length - 1);

            return keysDownString;
        }
        public static string GetKeysDownString()
        {
            return GetKeysDownString(KeysDown, LastKeyEvent.modifiers);
        }

        /// <summary>
        /// Convert a keyCode to a "Keys Down" string.
        /// </summary>
        public static string GetKeysDownString(KeyCode keyCode)
        {
            var keyList = new List<KeyCode>();
            keyList.Add(keyCode);
            return GetKeysDownString(keyList);
        }
        /// <summary>
        /// Convert a keyCode and modifiers to a "Keys Down" string.
        /// </summary>
        public static string GetKeysDownString(KeyCode keyCode, EventModifiers modifiers)
        {
            var keyList = new List<KeyCode>();
            keyList.Add(keyCode);
            return GetKeysDownString(keyList, modifiers);
        }
        /// <summary>
        /// Convert a keyCode and modifiers to a "Keys Down" string meant for Menu Items.
        /// </summary>
        public static string GetKeyMenuItemString(KeyCode keyCode, EventModifiers modifiers)
        {
            string keysDownString = "";

            if (modifiers.MatchesFlag(EventModifiers.Command))
            {
                keysDownString += "%";
            }
            if (modifiers.MatchesFlag(EventModifiers.Control))
            {
                keysDownString += "%";
            }
            if (modifiers.MatchesFlag(EventModifiers.Shift))
            {
                keysDownString += "#";
            }
            if (modifiers.MatchesFlag(EventModifiers.Alt))
            {
                keysDownString += "&";
            }

            keysDownString += keyCode.ToKeyString();

            return keysDownString;
        }

        private static FieldInfo globalEventHandlerFieldInfo;
        private static EditorApplication.CallbackFunction globalEventHandler
        {
            get { return (EditorApplication.CallbackFunction)globalEventHandlerFieldInfo.GetValue(null); }
            set { globalEventHandlerFieldInfo.SetValue(null, value); }
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            KeysDown = new List<KeyCode>();

            EditorApplication.update += RefreshEvent;

            try
            {
                globalEventHandlerFieldInfo = typeof(EditorApplication).GetField("globalEventHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                globalEventHandler += GlobalEvent;
            }
            catch (Exception e)
            {
                Debug.LogError("Could not find the global event handler. " + e.ToString());
            }

        }

        private static void GlobalEvent()
        {
            var currentEvent = Event.current;

            if (currentEvent != null)
            {
                UpdateInputState(currentEvent);
                if (currentEvent.type == EventType.KeyDown || currentEvent.type == EventType.KeyUp)
                {
                    _LastKeyEvent = currentEvent;

                    SetKeyDown(currentEvent.keyCode, currentEvent.type == EventType.KeyDown);

                    if (KeyEventHandler != null)
                    {
                        KeyEventHandler.Invoke();
                    }


                }
            }
        }

        private static void RefreshEvent()
        {
            if (inputWin == null)
            {
                InitializeInputWin();
            }

            inputWin.Repaint();
        }

        private static void InitializeInputWin()
        {
            //Close existing input wins except for one
            var allInputWins = (EditorInput[])Resources.FindObjectsOfTypeAll(typeof(EditorInput));
            for (int i=0; i<allInputWins.Length; i++)
            {
                if (i == allInputWins.Length - 1)
                {
                    //Keep the last one
                    inputWin = allInputWins[i];
                }
                else
                {
                    allInputWins[i].Close();
                }
            }

            if (inputWin == null)
            {
                //If couldn't find an existing one, create a hidden EditorWindow to receive an editor event
                Rect winPos = new Rect(-5000, -5000, 1, 1);
                inputWin = CreateInstance<EditorInput>();
                inputWin.SetWindowTitle("EditorInputWin", true);

                inputWin.minSize = winPos.size;
                inputWin.maxSize = winPos.size;
                inputWin.position = winPos;

                inputWin.ShowWithMode(EditorWindowExtensions.ShowMode.NoShadow);
                inputWin.SetSaveToLayout(false);
                EditorMainWindow.Focus();
            }

#if UNITY_STANDALONE_WIN && (UNITY_5_0 || UNITY_5_1)
            inputWin.DisableKeyboardCapture();
            inputWin.InitKeyboardCapture();
#endif
        }

        void OnGUI()
        {
            UpdateInputState(Event.current);
        }

        void OnDisable()
        {
#if UNITY_STANDALONE_WIN && (UNITY_5_0 || UNITY_5_1)
            this.DisableKeyboardCapture();
#endif
        }

        private static void UpdateInputState(Event e)
        {
#if UNITY_STANDALONE_WIN
            //Don't update mouse position. It is fetched on demand.
#else
            //Update mouse position periodically.
             _MousePosition = GetMousePosition(e);
#endif

            Alt = e.alt;
            Ctrl = e.control;
            if (e.shift != Shift)
            {
                /*Store shift state as Left Shift KeyCode, because the global event handler doesn't pickup Shift events*/
                Shift = e.shift;
                SetKeyDown(KeyCode.LeftShift, e.shift);
            }
        }

#if UNITY_STANDALONE_WIN
        private static Vector2 GetMousePosition()
        {
            POINT cursorPos;
            GetCursorPos(out cursorPos);
            return new Vector2(cursorPos.X, cursorPos.Y);
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

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

#if (UNITY_5_0 || UNITY_5_1)
        [StructLayout(LayoutKind.Sequential)]
        public class KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        public enum WindowsMessage : uint
        {
            WM_KEYDOWN = 0x0100,
            WM_KEYUP = 0x0101,
            WM_SYSKEYDOWN = 0x0104,
            WM_SYSKEYUP = 0x0105
        }

        private delegate IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static KeyboardProc KeyHandler;

        private IntPtr kbHookHandle;

        [DllImport("user32.dll")]
        private static extern short GetKeyState(EnumExtensions.VirtualKey virtualKey);

        private static bool GetKeyDown(EnumExtensions.VirtualKey virtualKey)
        { 
            return (GetKeyState(virtualKey) & (0x1 << 15)) != 0;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardProc lpfn, IntPtr hMod, int dwThreadId);

        [DllImport("user32.dll")]
        private static extern int UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        public void InitKeyboardCapture()
        {
            KeyHandler = null;
            KeyHandler += KeyCallback;
            kbHookHandle = SetWindowsHookEx(2, KeyHandler, IntPtr.Zero, (int)GetCurrentThreadId());
        }

        public void DisableKeyboardCapture()
        {
            if ((int)kbHookHandle != 0)
                UnhookWindowsHookEx(kbHookHandle);
        }

        private static IntPtr KeyCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                bool keyDownEvent = ((uint)lParam & (0x1 << 31)) == 0;
                if (keyDownEvent)
                {
                    var virtualKey = (EnumExtensions.VirtualKey)wParam;
                    var keyCode = virtualKey.ToKeyCode();
                    var modifiers = EventModifiers.None;

                    if (virtualKey != EnumExtensions.VirtualKey.Control && GetKeyDown(EnumExtensions.VirtualKey.Control)) modifiers = modifiers | EventModifiers.Control;
                    if (virtualKey != EnumExtensions.VirtualKey.Shift && GetKeyDown(EnumExtensions.VirtualKey.Shift)) modifiers = modifiers | EventModifiers.Shift;
                    if (virtualKey != EnumExtensions.VirtualKey.Menu && GetKeyDown(EnumExtensions.VirtualKey.Menu)) modifiers = modifiers | EventModifiers.Alt;
                    if (virtualKey != EnumExtensions.VirtualKey.CapsLock && GetKeyDown(EnumExtensions.VirtualKey.CapsLock)) modifiers = modifiers | EventModifiers.CapsLock;
                    
                    if (EditorFullscreenController.TriggerFullscreenHotkey(keyCode, modifiers)) return (IntPtr)1;
                }
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
#endif
#else
        private static Vector2 GetMousePosition(Event e)
        {
            return GUIUtility.GUIToScreenPoint(e.mousePosition);
        }
#endif

    }

    public static class EnumExtensions
    {
        public static bool MatchesFlag(this EventModifiers modifiers, EventModifiers flag)
        {
            return (modifiers & flag) == flag;
        }

        public static string ToKeyString(this KeyCode keyCode)
        {
            string keyString = keyCode.ToString();

            switch (keyCode)
            {
                case KeyCode.BackQuote:
                    keyString = "`";
                    break;
                case KeyCode.Backslash:
                    keyString = "\\";
                    break;
                case KeyCode.Comma:
                    keyString = ",";
                    break;
                case KeyCode.Equals:
                    keyString = "=";
                    break;
                case KeyCode.Minus:
                    keyString = "-";
                    break;
                case KeyCode.PageDown:
                    keyString = "PGDN";
                    break;
                case KeyCode.PageUp:
                    keyString = "PGUP";
                    break;
                case KeyCode.Period:
                    keyString = ".";
                    break;
                case KeyCode.Plus:
                    keyString = "+";
                    break;
                case KeyCode.Quote:
                    keyString = "'";
                    break;
                case KeyCode.RightBracket:
                    keyString = "]";
                    break;
                case KeyCode.LeftBracket:
                    keyString = "[";
                    break;
                case KeyCode.Semicolon:
                    keyString = ";";
                    break;
                case KeyCode.Slash:
                    keyString = "/";
                    break;
                case KeyCode.Space:
                    keyString = " ";
                    break;
                case KeyCode.Underscore:
                    keyString = "_";
                    break;
                default:
                    keyString = keyString.Replace("Alpha", "");
                    keyString = keyString.Replace("Arrow", "");
                    keyString = keyString.ToUpper();
                    break;
            }

            return keyString;
        }

        public enum VirtualKey : ushort
        {
            LeftButton = 0x01,
            RightButton = 0x02,
            Cancel = 0x03,
            MiddleButton = 0x04,
            ExtraButton1 = 0x05,
            ExtraButton2 = 0x06,
            Back = 0x08,
            Tab = 0x09,
            Clear = 0x0C,
            Return = 0x0D,
            Shift = 0x10,
            Control = 0x11,
            Menu = 0x12,
            Pause = 0x13,
            CapsLock = 0x14,
            Kana = 0x15,
            Hangeul = 0x15,
            Hangul = 0x15,
            Junja = 0x17,
            Final = 0x18,
            Hanja = 0x19,
            Kanji = 0x19,
            Escape = 0x1B,
            Convert = 0x1C,
            NonConvert = 0x1D,
            Accept = 0x1E,
            ModeChange = 0x1F,
            Space = 0x20,
            Prior = 0x21,
            Next = 0x22,
            End = 0x23,
            Home = 0x24,
            Left = 0x25,
            Up = 0x26,
            Right = 0x27,
            Down = 0x28,
            Select = 0x29,
            Print = 0x2A,
            Execute = 0x2B,
            Snapshot = 0x2C,
            Insert = 0x2D,
            Delete = 0x2E,
            Help = 0x2F,
            N0 = 0x30,
            N1 = 0x31,
            N2 = 0x32,
            N3 = 0x33,
            N4 = 0x34,
            N5 = 0x35,
            N6 = 0x36,
            N7 = 0x37,
            N8 = 0x38,
            N9 = 0x39,
            A = 0x41,
            B = 0x42,
            C = 0x43,
            D = 0x44,
            E = 0x45,
            F = 0x46,
            G = 0x47,
            H = 0x48,
            I = 0x49,
            J = 0x4A,
            K = 0x4B,
            L = 0x4C,
            M = 0x4D,
            N = 0x4E,
            O = 0x4F,
            P = 0x50,
            Q = 0x51,
            R = 0x52,
            S = 0x53,
            T = 0x54,
            U = 0x55,
            V = 0x56,
            W = 0x57,
            X = 0x58,
            Y = 0x59,
            Z = 0x5A,
            LeftWindows = 0x5B,
            RightWindows = 0x5C,
            Application = 0x5D,
            Sleep = 0x5F,
            Numpad0 = 0x60,
            Numpad1 = 0x61,
            Numpad2 = 0x62,
            Numpad3 = 0x63,
            Numpad4 = 0x64,
            Numpad5 = 0x65,
            Numpad6 = 0x66,
            Numpad7 = 0x67,
            Numpad8 = 0x68,
            Numpad9 = 0x69,
            Multiply = 0x6A,
            Add = 0x6B,
            Separator = 0x6C,
            Subtract = 0x6D,
            Decimal = 0x6E,
            Divide = 0x6F,
            F1 = 0x70,
            F2 = 0x71,
            F3 = 0x72,
            F4 = 0x73,
            F5 = 0x74,
            F6 = 0x75,
            F7 = 0x76,
            F8 = 0x77,
            F9 = 0x78,
            F10 = 0x79,
            F11 = 0x7A,
            F12 = 0x7B,
            F13 = 0x7C,
            F14 = 0x7D,
            F15 = 0x7E,
            F16 = 0x7F,
            F17 = 0x80,
            F18 = 0x81,
            F19 = 0x82,
            F20 = 0x83,
            F21 = 0x84,
            F22 = 0x85,
            F23 = 0x86,
            F24 = 0x87,
            NumLock = 0x90,
            ScrollLock = 0x91,
            NEC_Equal = 0x92,
            LeftShift = 0xA0,
            RightShift = 0xA1,
            LeftControl = 0xA2,
            RightControl = 0xA3,
            LeftMenu = 0xA4,
            RightMenu = 0xA5,
            BrowserBack = 0xA6,
            BrowserForward = 0xA7,
            BrowserRefresh = 0xA8,
            BrowserStop = 0xA9,
            BrowserSearch = 0xAA,
            BrowserFavorites = 0xAB,
            BrowserHome = 0xAC,
            VolumeMute = 0xAD,
            VolumeDown = 0xAE,
            VolumeUp = 0xAF,
            MediaNextTrack = 0xB0,
            MediaPrevTrack = 0xB1,
            MediaStop = 0xB2,
            MediaPlayPause = 0xB3,
            LaunchMail = 0xB4,
            LaunchMediaSelect = 0xB5,
            LaunchApplication1 = 0xB6,
            LaunchApplication2 = 0xB7,
            OEM1 = 0xBA,
            OEMPlus = 0xBB,
            OEMComma = 0xBC,
            OEMMinus = 0xBD,
            OEMPeriod = 0xBE,
            OEM2 = 0xBF,
            OEM3 = 0xC0,
            OEM4 = 0xDB,
            OEM5 = 0xDC,
            OEM6 = 0xDD,
            OEM7 = 0xDE,
            OEM8 = 0xDF,
            OEMAX = 0xE1,
            OEM102 = 0xE2,
            ICOHelp = 0xE3,
            ICO00 = 0xE4,
            ProcessKey = 0xE5,
            ICOClear = 0xE6,
            Packet = 0xE7,
            OEMReset = 0xE9,
            OEMJump = 0xEA,
            OEMPA1 = 0xEB,
            OEMPA2 = 0xEC,
            OEMPA3 = 0xED,
            OEMWSCtrl = 0xEE,
            OEMCUSel = 0xEF,
            OEMATTN = 0xF0,
            OEMFinish = 0xF1,
            OEMCopy = 0xF2,
            OEMAuto = 0xF3,
            OEMENLW = 0xF4,
            OEMBackTab = 0xF5,
            ATTN = 0xF6,
            CRSel = 0xF7,
            EXSel = 0xF8,
            EREOF = 0xF9,
            Play = 0xFA,
            Zoom = 0xFB,
            Noname = 0xFC,
            PA1 = 0xFD,
            OEMClear = 0xFE
        }

        public static KeyCode ToKeyCode(this VirtualKey virtualKey)
        {
            KeyCode keyCode = KeyCode.None;

            if (RangeConvert(virtualKey, VirtualKey.F1, VirtualKey.F15, KeyCode.F1, out keyCode)) return keyCode;
            if (RangeConvert(virtualKey, VirtualKey.A, VirtualKey.Z, KeyCode.A, out keyCode)) return keyCode;
            if (RangeConvert(virtualKey, VirtualKey.N0, VirtualKey.N9, KeyCode.Alpha0, out keyCode)) return keyCode;
            if (RangeConvert(virtualKey, VirtualKey.Numpad0, VirtualKey.Numpad9, KeyCode.Keypad0, out keyCode)) return keyCode;

            switch (virtualKey)
            {
                case VirtualKey.Control:
                case VirtualKey.LeftControl:
                case VirtualKey.OEMWSCtrl:
                    keyCode = KeyCode.LeftControl;
                    break;
                case VirtualKey.RightControl:
                    keyCode = KeyCode.RightControl;
                    break;
                case VirtualKey.Menu:
                case VirtualKey.LeftMenu:
                    keyCode = KeyCode.LeftAlt;
                    break;
                case VirtualKey.RightMenu:
                    keyCode = KeyCode.RightAlt;
                    break;
                case VirtualKey.Shift:
                case VirtualKey.LeftShift:
                    keyCode = KeyCode.LeftShift;
                    break;
                case VirtualKey.RightShift:
                    keyCode = KeyCode.RightShift;
                    break;
                case VirtualKey.CapsLock:
                    keyCode = KeyCode.CapsLock;
                    break;
                case VirtualKey.Tab:
                    keyCode = KeyCode.Tab;
                    break;
                case VirtualKey.OEM3:
                    keyCode = KeyCode.BackQuote;
                    break;
                case VirtualKey.Escape:
                    keyCode = KeyCode.Escape;
                    break;
                case VirtualKey.Pause:
                    keyCode = KeyCode.Pause;
                    break;
                case VirtualKey.Delete:
                    keyCode = KeyCode.Delete;
                    break;
                case VirtualKey.Home:
                    keyCode = KeyCode.Home;
                    break;
                case VirtualKey.Prior:
                    keyCode = KeyCode.PageUp;
                    break;
                case VirtualKey.Next:
                    keyCode = KeyCode.PageDown;
                    break;
                case VirtualKey.End:
                    keyCode = KeyCode.End;
                    break;
                case VirtualKey.OEMMinus:
                    keyCode = KeyCode.Minus;
                    break;
                case VirtualKey.OEMPlus:
                    keyCode = KeyCode.Plus;
                    break;
                case VirtualKey.Back:
                    keyCode = KeyCode.Backspace;
                    break;
                case VirtualKey.OEM1:
                    keyCode = KeyCode.Semicolon;
                    break;
                case VirtualKey.OEMComma:
                    keyCode = KeyCode.Comma;
                    break;
                case VirtualKey.OEMPeriod:
                    keyCode = KeyCode.Period;
                    break;
                case VirtualKey.OEM2:
                    keyCode = KeyCode.Slash;
                    break;
                case VirtualKey.OEM4:
                    keyCode = KeyCode.LeftBracket;
                    break;
                case VirtualKey.OEM5:
                    keyCode = KeyCode.Backslash;
                    break;
                case VirtualKey.OEM6:
                    keyCode = KeyCode.RightBracket;
                    break;
                case VirtualKey.OEM7:
                    keyCode = KeyCode.Quote;
                    break;
                case VirtualKey.Divide:
                    keyCode = KeyCode.KeypadDivide;
                    break;
                case VirtualKey.Multiply:
                    keyCode = KeyCode.KeypadMultiply;
                    break;
                case VirtualKey.Subtract:
                    keyCode = KeyCode.KeypadMinus;
                    break;
                case VirtualKey.Add:
                    keyCode = KeyCode.KeypadPlus;
                    break;
                case VirtualKey.Return:
                    keyCode = KeyCode.Return;
                    break;
                case VirtualKey.Insert:
                    keyCode = KeyCode.Insert;
                    break;
                case VirtualKey.Space:
                    keyCode = KeyCode.Space;
                    break;
            }
            return keyCode;
        }

        private static bool RangeConvert(VirtualKey virtualKey, VirtualKey rangeStart, VirtualKey rangeEnd, KeyCode keyCodeRangeStart, out KeyCode keyCode)
        {
            if (virtualKey >= rangeStart && virtualKey <= rangeEnd)
            {
                int offset = virtualKey - rangeStart;
                keyCode = keyCodeRangeStart + offset;
                return true;
            }
            else
            {
                keyCode = KeyCode.None;
                return false;
            }
        }
    }
}