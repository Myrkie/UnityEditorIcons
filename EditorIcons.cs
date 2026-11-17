#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class EditorIcons : EditorWindow
{
    [MenuItem("Tools/Editor Icons %e", priority = -1001)]
    public static void EditorIconsOpen()
    {
#if UNITY_2018
        var w = GetWindow<EditorIcons>("Editor Icons");
#else
        var w = CreateWindow<EditorIcons>("Editor Icons");
#endif
        w.ShowUtility();
        w.minSize = new Vector2(320, 450);
    }

    static bool viewBigIcons = true;

    Vector2 scroll;

    int buttonSize = 70;

    string search = "";

    void SearchGUI()
    {
        using( new GUILayout.HorizontalScope())
        {
            if (isWide) GUILayout.Space(10);

#if UNITY_2018
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarTextField);
#else
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
#endif
            if ( GUILayout.Button(EditorGUIUtility.IconContent("winbtn_mac_close_h"), //SVN_DeletedLocal
                EditorStyles.toolbarButton, 
                GUILayout.Width(22))
            ) search = "";
        }
    }

    bool isWide => Screen.width > 550;

    bool doSearch => ! string.IsNullOrWhiteSpace(search) && search != "";

    GUIContent GetIcon( string icon_name )
    {
        GUIContent valid = null;
        Debug.unityLogger.logEnabled = false;
        if (!string.IsNullOrEmpty(icon_name)) valid = EditorGUIUtility.IconContent(icon_name);
        Debug.unityLogger.logEnabled = true;
        return valid?.image == null ? null : valid;
    }

    void SaveIcon( string icon_name )
    {
        Texture2D tex = EditorGUIUtility.IconContent(icon_name).image as Texture2D;

        if (tex != null)
        {
            string path = EditorUtility.SaveFilePanel(
                "Save icon", "", icon_name, "png");

            if (string.IsNullOrEmpty(path)) return;
            try
            {
                Texture2D outTex = ConvertToReadableTexture(tex);

                if (outTex == null)
                    throw new Exception("Could not convert texture to a readable format.");

                byte[] pngBytes = outTex.EncodeToPNG();

                if (pngBytes == null || pngBytes.Length == 0)
                    throw new Exception("PNG encoding returned no data.");

                File.WriteAllBytes(path, pngBytes);
                DestroyImmediate(outTex);
            }
            catch (Exception e)
            {
                Debug.LogError("Cannot save the icon : " + e.Message);
            }
        }
        else
        {
            Debug.LogError("Cannot save the icon : null texture error!");
        }
    }

    Texture2D ConvertToReadableTexture(Texture2D tex)
    {
        RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);

        RenderTexture previous = RenderTexture.active;

        try
        {
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;

            Texture2D readableTex = new Texture2D(
                tex.width, tex.height, TextureFormat.RGBA32, false);

            readableTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

            readableTex.Apply();

            return readableTex;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    void SaveAllIcons()
    {
        var folderpath = EditorUtility.SaveFolderPanel("", "", "");
        try
        {
            if (string.IsNullOrWhiteSpace(folderpath))
                return;

            foreach (string icon in iconNames)
            {
                var split = icon.Split('/').Last();
                var invalidChars = Path.GetInvalidFileNameChars();
                var safeFileName = new string(split.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());

                Texture2D tex = EditorGUIUtility.IconContent(icon).image as Texture2D;

                if (tex == null) continue;

                var path = Path.Combine(folderpath, $"{safeFileName}.png");

                if (File.Exists(path))
                {
                    Debug.Log($"file already written with name aborting write: {path}");
                }
                else
                {
                    Texture2D outTex = ConvertToReadableTexture(tex);

                    if (outTex == null)
                        throw new Exception("Could not convert texture to a readable format: " + icon);

                    byte[] pngBytes = outTex.EncodeToPNG();

                    if (pngBytes == null || pngBytes.Length == 0)
                    {
                        DestroyImmediate(outTex);
                        throw new Exception("PNG encoding returned no data: " + icon);
                    }

                    File.WriteAllBytes(path, pngBytes);
                    DestroyImmediate(outTex);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Cannot save the icons: " + e.Message);
        }
    }

    private void OnEnable()
    {
        HashSet<string> discoveredIcons = new HashSet<string>(StringComparer.Ordinal);

        FieldInfo iconField = typeof(EditorGUIUtility).GetField("s_IconGUIContents", BindingFlags.Static | BindingFlags.NonPublic);

        if (iconField != null)
        {
            IDictionary iconDictionary = iconField.GetValue(null) as IDictionary;

            if (iconDictionary != null)
            {
                foreach (DictionaryEntry entry in iconDictionary)
                {
                    string iconName = entry.Key as string;

                    if (!string.IsNullOrEmpty(iconName))
                        discoveredIcons.Add(iconName);
                }
            }
        }

        //List<string> found = new List<string>();
        //var skip_flag = HideFlags.HideInInspector | HideFlags.HideAndDontSave;
        //int unique_to_resources = 0, skipped_empty_str = 0, skipped_flags = 0,
        //    skipped_not_persistent = 0, skipped_nulls = 0;

        foreach (Texture2D x in Resources.FindObjectsOfTypeAll<Texture2D>())
        {
            //if (string.IsNullOrEmpty(x.name)) skipped_empty_str++;                                      // skipped 10 empty
            //if (!EditorUtility.IsPersistent(x)) skipped_not_persistent++;                               // skipped 39 none persistent
            //if (x.hideFlags != HideFlags.HideAndDontSave && x.hideFlags != skip_flag) skipped_flags++;  // skipped 27 icons

            GUIContent icoContent = GetIcon(x.name);
            if (icoContent == null) continue; // skipped 14 icons

            discoveredIcons.Add(x.name);

            //found.Add( x.name );
        }

        //Debug.Log($"Resources skipped nulls={skipped_nulls} empty={skipped_empty_str} flags={skipped_flags}" );
        //Debug.Log("Resources skipped_not_persistent=" + skipped_not_persistent);

        iconNames = discoveredIcons
            .Where(x => GetIcon(x) != null)
            .OrderBy(x => x, StringComparer.Ordinal)  // light and dark icon order is not stable, sort 2 times
            // ReSharper disable once MultipleOrderBy
            .OrderBy(x =>     // not ThenBy
                x.StartsWith("d_", StringComparison.Ordinal) ? x[2..] : x, StringComparer.Ordinal)
            .ToList();

        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    private void OnGUI()
    {
        var ppp = EditorGUIUtility.pixelsPerPoint;

        InitIcons();

        if (!isWide) SearchGUI();

        using ( new GUILayout.HorizontalScope( EditorStyles.toolbar ) )
        {
            if(GUILayout.Button("Save all icons to folder...",EditorStyles.miniButton)) SaveAllIcons();
            GUILayout.Label("Select what icons to show", GUILayout.Width( 160 ));
            viewBigIcons = GUILayout.SelectionGrid(
              viewBigIcons ? 1 : 0, new string[] { "Small", "Big" }, 
              2 , EditorStyles.toolbarButton) == 1;

            if (isWide) SearchGUI();
        }

        if (isWide) GUILayout.Space(3);

        using (var scope = new GUILayout.ScrollViewScope(scroll))
        {
            GUILayout.Space(10);

            scroll = scope.scrollPosition;

            buttonSize = viewBigIcons ? 70 : 40;

            // scrollbar_width = ~ 12.5
            var render_width = (Screen.width / ppp - 13f);
            var gridW = Mathf.FloorToInt( render_width / buttonSize );
            var margin_left = ( render_width - buttonSize * gridW ) / 2;

            int row = 0, index = 0;

            List<GUIContent> iconList;

            if( doSearch ) iconList = iconContentListAll.Where( x => x.tooltip.ToLower()
                .Contains( search.ToLower() ) ).ToList();
            else iconList = viewBigIcons ? iconContentListBig : iconContentListSmall;

            while ( index < iconList.Count )
            {
                using( new GUILayout.HorizontalScope() )
                {
                    GUILayout.Space(margin_left);

                    for( var i = 0; i < gridW; ++ i )
                    {
                        int k = i + row * gridW;

                        var icon = iconList[ k ];

                        if (GUILayout.Button(icon,
                            iconButtonStyle,
                            GUILayout.Width(buttonSize),
                            GUILayout.Height(buttonSize))) {
                            EditorGUI.FocusTextInControl("");
                            iconSelected = icon;
                        }

                        index ++ ;

                        if( index == iconList.Count ) break;
                    }
                }

                row ++ ;
            }

            GUILayout.Space(10);
        }


        if (iconSelected == null) return;

        GUILayout.FlexibleSpace();

        using (new GUILayout.HorizontalScope( EditorStyles.helpBox, GUILayout.MaxHeight(viewBigIcons ? 140 : 120 ) ) )
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(128)))
            {
                GUILayout.Button(iconSelected,
                    iconPreviewBlack,
                    GUILayout.Width(128), GUILayout.Height(viewBigIcons ? 128 : 128/*40*/));

                GUILayout.Button(iconSelected,
                    iconPreviewWhite,
                    GUILayout.Width(128), GUILayout.Height(viewBigIcons ? 128 : 128/*40*/));

                GUILayout.FlexibleSpace();
            }

            using (new GUILayout.VerticalScope())
            {
                var s = $"Size: {iconSelected.image.width}x{iconSelected.image.height}";
                s += "\nIs Pro Skin Icon: " + ( iconSelected.tooltip.IndexOf("d_") == 0 ? "Yes" : "No" );
                s += $"\nTotal {iconContentListAll.Count} icons";

                EditorGUILayout.HelpBox(s, MessageType.None);
                GUILayout.Space(5);
                EditorGUILayout.TextField("EditorGUIUtility.IconContent(\"" + iconSelected.tooltip + "\")");
                GUILayout.Space(5);

                if (GUILayout.Button("Copy to clipboard", EditorStyles.miniButton))
                    EditorGUIUtility.systemCopyBuffer = iconSelected.tooltip;
                if(GUILayout.Button("Save icon to file ...",EditorStyles.miniButton))
                    SaveIcon(iconSelected.tooltip);


                // USS for Unity Editor
                var ussName = iconSelected.tooltip;
                int isDark = 0;
                if (iconNames.Contains("d_" + ussName))
                {
                    isDark = 1;
                }
                else if (ussName.StartsWith("d_", StringComparison.Ordinal))
                {
                    if (iconNames.Contains(ussName.Substring(2)))
                    {
                        ussName = ussName.Substring(2);
                        isDark = 1;
                    }
                }
                var iconFileName = ussName;
                // remove redundant 'icon'
                if (ussName.EndsWith("icon", StringComparison.OrdinalIgnoreCase))
                    ussName = ussName[..^4];
                if (ussName.StartsWith("icon", StringComparison.OrdinalIgnoreCase))
                    ussName = ussName[4..];
                var trimChars = new char[] { '-', '_', ' ', '.' };
                foreach (var trim in trimChars)
                {
                    int pos;
                    while (true)
                    {
                        pos = ussName.IndexOf(trim + "icon", StringComparison.OrdinalIgnoreCase);
                        if (pos < 0)
                            break;

                        ussName = ussName[..pos] + ussName[(pos + 5)..];
                    }
                }
                ussName = ussName.Trim(trimChars);

                ussName = ".editor--"
                    + ussName.Replace(' ', '-').Replace('.', '-').Replace('@', '-').ToLowerInvariant() + "--icon";
                // always export both light and dark
                var uss = string.Empty;
                while (isDark-- >= 0)
                {
                    uss += $@"{ussName} {{
    background-image: resource('{iconFileName}');
    -unity-background-scale-mode: scale-to-fit;
}}
"
                        ;
                    iconFileName = "d_" + iconFileName;
                    ussName += "-dark";
                }
                EditorGUILayout.TextField(uss, GUILayout.ExpandHeight(true));
            }

            if (GUILayout.Button( "X", GUILayout.ExpandHeight(true)))
            {
                iconSelected = null;
            }

        }
    }

    static GUIContent iconSelected;
    static List<GUIContent> iconContentListAll;
    static List<GUIContent> iconContentListSmall;
    static List<GUIContent> iconContentListBig;
    static GUIStyle iconButtonStyle = null;
    static GUIStyle iconPreviewBlack = null;
    static GUIStyle iconPreviewWhite = null;

    static List<string> iconNames = new List<string>();

    void AllTheTextures( ref GUIStyle s, Texture2D t )
    {
        s.hover.background = s.onHover.background = s.focused.background = s.onFocused.background = s.active.background = s.onActive.background = s.normal.background = s.onNormal.background = t;
        s.hover.scaledBackgrounds = s.onHover.scaledBackgrounds = s.focused.scaledBackgrounds = s.onFocused.scaledBackgrounds = s.active.scaledBackgrounds = s.onActive.scaledBackgrounds = s.normal.scaledBackgrounds = s.onNormal.scaledBackgrounds = new Texture2D[] { t };
    }

    Texture2D Texture2DPixel( Color c )
    {
        Texture2D t = new Texture2D(1,1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    void InitIcons()
    {
        if( iconContentListSmall != null ) return;

        iconButtonStyle = new GUIStyle( EditorStyles.miniButton )
        {
            margin = new RectOffset(0,0,0,0),
            fixedHeight = 0
        };

        iconPreviewBlack = new GUIStyle(iconButtonStyle);
        AllTheTextures( ref iconPreviewBlack, Texture2DPixel(new Color(0.15f,0.15f,0.15f)));

        iconPreviewWhite = new GUIStyle(iconButtonStyle);
        AllTheTextures( ref iconPreviewWhite, Texture2DPixel(new Color(0.85f, 0.85f, 0.85f)));

        iconContentListSmall = new List<GUIContent>();
        iconContentListBig = new List<GUIContent>();
        iconContentListAll = new List<GUIContent>();

        foreach (var t in iconNames)
        {
            GUIContent ico = GetIcon(t);

            if( ico == null )
                continue;

            ico.tooltip = t;

            iconContentListAll.Add(ico);

            if ( !( ico.image.width <= 36 || ico.image.height <= 36 ) )
                iconContentListBig.Add(ico);
            else iconContentListSmall.Add(ico);
        }
    }
}
#endif