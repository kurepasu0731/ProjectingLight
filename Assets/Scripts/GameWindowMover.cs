// Win32Apiを使用しているためWindowsのみサポート

using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

//Version 0.21 | twitter:@izm update for DK2 
//Version 0.2 | s.b.Newsom Edition

//Source from http://answers.unity3d.com/questions/179775/game-window-size-from-editor-window-in-editor-mode.html
//Modified by seieibob for use at the Virtual Environment and Multimodal Interaction Lab at the University of Maine.
//Use however you'd like!

//Modified by sbNewsom. Like it is said above, use as you like! If you're interested in my work, check out:
//http://www.sbnewsom.com

/// <summary>
/// Displays a popup window that undocks, repositions and resizes the game window according to
/// what is specified by the user in the popup. Offsets are applied to ensure screen borders are not shown.
/// </summary>
public class GameWindowMover : EditorWindow
{
    //The size of the toolbar above the game view, excluding the OS border.
    private int tabHeight = 22;

    private bool toggle = true;

    //Get the size of the window borders. Changes depending on the OS.
#if UNITY_STANDALONE_WIN
    //Windows settings
    private int osBorderWidth = 5;
#elif UNITY_STANDALONE_OSX
	//Mac settings (untested)
	private int osBorderWidth = 0; //OSX windows are borderless.
#else
	//Linux / other platform; sizes change depending on the variant you're running
	private int osBorderWidth = 5;
#endif
    //default setting 
    private static Vector2 _gameSize = new Vector2(0, 0); // 初期値を(0, 0)に変更
    private static Vector2 _gamePosition = new Vector2(0, 0);

    //Desired window resolution
    public Vector2 gameSize = new Vector2(_gameSize.x, _gameSize.y);

    //Desired window position
    public Vector2 gamePosition = new Vector2(_gamePosition.x, _gamePosition.y);

    //Tells the script to use the default resolution specified in the player settings.
    //private bool usePlayerSettingsResolution = false;

    //For those that duplicate screen
    private bool useDesktopResolution = false;

    // ---------------------------------------------------------------------------------------------------
    // ▼こっから▼
    // ---------------------------------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    struct RectApi
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public int width
        {
            get
            {
                return right - left;
            }
        }
        public int height
        {
            get
            {
                return bottom - top;
            }
        }
    }

    // メインモニター以外のモニターの位置およびサイズ(解像度)のリスト
    private List<RectApi> screenInfo;
    private List<string> screenName;
    private List<int> screenNumber;
//    int number = 1;

    delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RectApi pRect, int dwData);
    [DllImport("user32")]
    static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lpRect, MonitorEnumProc callback, int dwData);
    // https://msdn.microsoft.com/ja-jp/library/cc428502.aspx
    

    void GetDisplays()
    {
        if (!EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, enumCallBack, 0))
            Debug.Log("An error occured while enumerating monitors");
    }

    bool enumCallBack(IntPtr hMonitor, IntPtr hdc, ref RectApi prect, int d)
    {
        //if (!(prect.left == 0 && prect.top == 0) &&
        //    ((prect.width == 1920 && prect.height == 1080) ||
        //    (prect.width == 1080 && prect.height == 1920) ||
        //    (prect.width == 1280 && prect.height == 800))
        //)
        //{
        //    // (left, top)が(0, 0)以外(つまりメインモニター以外)でかつサイズがDK2(1920, 1080)または(1080, 1920)またはDK1(1280, 800)の場合、モニター情報をリストに追加
        //    screenInfo.Add(prect);
        //}

        screenInfo.Add(prect);
        screenName.Add("Monitor:" + screenInfo.Count);
        screenNumber.Add(screenInfo.Count);
        
        return true;
    }

    void Awake()
    {
        SetDefault(false);
    }

    void SetDefault(bool flg)
    {
        ResetDesplay();

        // メインモニターの次に見つかったものをデフォルト値としてセット
        if (screenInfo.Count > 1)
        {
            if (flg || gameSize.x == 0)
            {
                gameSize = new Vector2(screenInfo[1].width, screenInfo[1].height);
                gamePosition = new Vector2(screenInfo[1].left, screenInfo[1].top);
            }
        }
    }

    void SetDesplay(int num)
    {

        ResetDesplay();

        if(num < screenInfo.Count)
        {
            
            gameSize = new Vector2(screenInfo[num].width, screenInfo[num].height);
            gamePosition = new Vector2(screenInfo[num].left, screenInfo[num].top);
        }

    }

    void ResetDesplay()
    {
        screenInfo = new List<RectApi>();
        screenName = new List<string>();
        screenNumber = new List<int>();
        GetDisplays();
    }

    // ---------------------------------------------------------------------------------------------------
    // ▲ここまで追加▲
    // ---------------------------------------------------------------------------------------------------

    //Shows the popup
    [MenuItem("Window/Game Sub Display Mode")]
    static void OpenPopup()
    {
        GameWindowMover window = (GameWindowMover)(EditorWindow.GetWindow(typeof(GameWindowMover)));
        //Set popup window properties
        Vector2 popupSize = new Vector2(300, 160);
        //When minSize and maxSize are the same, no OS border is applied to the window.
        window.minSize = popupSize;
        window.maxSize = popupSize;
        window.titleContent = new GUIContent("SubDisplayMode");
        window.ShowPopup();
    }

    //Returns the current game view as an EditorWindow object.
    public static EditorWindow GetMainGameView()
    {
        //Creates a game window. Only works if there isn't one already.
        EditorApplication.ExecuteMenuItem("Window/Game");

        System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        System.Reflection.MethodInfo GetMainGameView = T.GetMethod("GetMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        System.Object Res = GetMainGameView.Invoke(null, null);
        return (EditorWindow)Res;
    }

    void OnGUI()
    {

        EditorGUILayout.Space();

        if (useDesktopResolution)
        {
            gameSize = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
        }
        gameSize = EditorGUILayout.Vector2Field("Sub Display Size:", gameSize);
        gamePosition = EditorGUILayout.Vector2Field("Sub Display Position:", gamePosition);

        EditorGUILayout.Space();

        ResetDesplay();

//        string[] names = screenName.ToArray();
//        int[] numbers = screenNumber.ToArray();
//        EditorGUILayout.IntPopup("Set Sub Display Number:", number, names, numbers);

//        SetDesplay( number - 1 );

        if (GUILayout.Button("Reset"))
        {
            // デフォルト値をセットするように変更
            SetDefault(true);
        }
        GUILayout.Label("Full Screen Mode is now activated. ");

        GUILayout.Label("Don't close this panel to keep script in effect.");

    }

    void Update()
    {
        if (Application.isPlaying)
        {
            MoveGameWindow();
            toggle = true;
        }
        else
        {
            if (toggle)
            {
                CloseGameWindow();
                toggle = false;
            }
        }
    }

    void MoveGameWindow()
    {
        EditorWindow gameView = GetMainGameView();
        gameView.titleContent = new GUIContent("Game (Sub Desplay)");
        //When minSize and maxSize are the same, no OS border is applied to the window.
        gameView.minSize = new Vector2(gameSize.x, gameSize.y + tabHeight - osBorderWidth);
        gameView.maxSize = gameView.minSize;
        Rect newPos = new Rect(gamePosition.x, gamePosition.y - tabHeight, gameSize.x, gameSize.y + tabHeight - osBorderWidth);
        gameView.position = newPos;
        gameView.ShowPopup();
    }

    void CloseGameWindow()
    {
        EditorWindow gameView = GetMainGameView();
        gameView.Close();
    }
}
