using UnityEngine;

public static class SubwayUI
{
    // ── Signature Subway Surfers Mobile Color Palette ────────────────────────
    public static readonly Color GreenFace = new Color(0.16f, 0.82f, 0.38f);
    public static readonly Color GreenBevel = new Color(0.09f, 0.54f, 0.23f);

    public static readonly Color YellowFace = new Color(1.0f, 0.78f, 0.08f);
    public static readonly Color YellowBevel = new Color(0.84f, 0.56f, 0.02f);

    public static readonly Color BlueFace = new Color(0.14f, 0.65f, 0.95f);
    public static readonly Color BlueBevel = new Color(0.08f, 0.42f, 0.66f);

    public static readonly Color RedFace = new Color(0.96f, 0.26f, 0.28f);
    public static readonly Color RedBevel = new Color(0.66f, 0.12f, 0.14f);

    public static readonly Color PurpleFace = new Color(0.62f, 0.32f, 0.88f);
    public static readonly Color PurpleBevel = new Color(0.42f, 0.18f, 0.64f);

    public static readonly Color DarkCard = new Color(0.08f, 0.11f, 0.17f, 0.96f);
    public static readonly Color InnerCard = new Color(0.05f, 0.07f, 0.12f, 0.88f);

    public static readonly Color GoldBorder = new Color(1.0f, 0.84f, 0.15f, 0.95f);
    public static readonly Color CyanBorder = new Color(0.15f, 0.85f, 0.95f, 0.95f);
    public static readonly Color RedBorder = new Color(0.95f, 0.25f, 0.25f, 0.95f);
    public static readonly Color GrayBorder = new Color(0.35f, 0.42f, 0.52f, 0.65f);

    private static Texture2D _whiteTex;
    public static Texture2D WhiteTex
    {
        get
        {
            if (_whiteTex == null)
            {
                _whiteTex = new Texture2D(1, 1);
                _whiteTex.SetPixel(0, 0, Color.white);
                _whiteTex.Apply();
            }
            return _whiteTex;
        }
    }

    public static void DrawRect(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, WhiteTex);
        GUI.color = old;
    }

    /// <summary>
    /// Draws a card container with rounded feel, colored border, and dark slate glass interior.
    /// </summary>
    public static void DrawCard(Rect rect, Color bgColor, Color borderColor, float border = 3f)
    {
        // Outer border
        DrawRect(rect, borderColor);
        // Interior
        Rect inner = new Rect(rect.x + border, rect.y + border, rect.width - (border * 2f), rect.height - (border * 2f));
        DrawRect(inner, bgColor);
        // Top inner glossy sheen
        DrawRect(new Rect(inner.x, inner.y, inner.width, Mathf.Min(8f, inner.height * 0.12f)), new Color(1f, 1f, 1f, 0.06f));
    }

    /// <summary>
    /// Draws chunky 3D extruded comic typography characteristic of Subway Surfers.
    /// </summary>
    public static void DrawExtrudedText(Rect rect, string text, int fontSize, Color faceColor, Color shadowColor, int depth = 3, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = anchor,
            clipping = TextClipping.Overflow
        };

        // Draw multiple dark extruded shadow passes
        style.normal.textColor = shadowColor;
        for (int i = 1; i <= depth; i++)
        {
            GUI.Label(new Rect(rect.x, rect.y + i, rect.width, rect.height), text, style);
        }

        // Draw foreground text
        style.normal.textColor = faceColor;
        GUI.Label(rect, text, style);
    }

    /// <summary>
    /// Draws a tactile 3D candy button with a physical bevel bottom that depresses when tapped.
    /// </summary>
    public static bool DrawChunkyButton(Rect rect, string text, Color faceCol, Color bevelCol, Color textCol, int fontSize, int iconSize = 0)
    {
        Event e = Event.current;
        bool isHover = rect.Contains(e.mousePosition);
        bool isDown = isHover && (Input.GetMouseButton(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase != TouchPhase.Ended));

        float bevel = Mathf.Clamp(rect.height * 0.12f, 4f, 7f);
        float pressOffset = isDown ? bevel - 2f : 0f;

        // 1. Dark bottom bevel
        DrawRect(new Rect(rect.x, rect.y + bevel, rect.width, rect.height - bevel), bevelCol);

        // 2. Button face
        Color curFace = isHover ? Color.Lerp(faceCol, Color.white, 0.10f) : faceCol;
        Rect faceRect = new Rect(rect.x, rect.y + pressOffset, rect.width, rect.height - bevel);
        DrawRect(faceRect, curFace);

        // 3. Top subtle highlight sheen
        DrawRect(new Rect(faceRect.x + 3f, faceRect.y + 1f, faceRect.width - 6f, 2f), new Color(1f, 1f, 1f, 0.32f));

        // 4. Label with 3D drop shadow
        DrawExtrudedText(faceRect, text, fontSize, textCol, new Color(0f, 0f, 0f, 0.75f), 2);

        // Transparent hit-test
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    /// <summary>
    /// Draws a Subway Surfers pill-shaped badge for Currency, High Score, or Multipliers.
    /// </summary>
    public static void DrawPillBadge(Rect rect, string title, string value, Color accentBorder, Color valueColor, int fontSize)
    {
        // Container
        DrawCard(rect, DarkCard, accentBorder, 2f);

        // Text formatting
        GUIStyle tStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp(fontSize - 3, 11, 15),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        tStyle.normal.textColor = Color.gray;

        GUIStyle vStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        vStyle.normal.textColor = valueColor;

        float pad = 10f;
        if (!string.IsNullOrEmpty(title))
        {
            GUI.Label(new Rect(rect.x + pad, rect.y, rect.width * 0.45f, rect.height), title, tStyle);
            GUI.Label(new Rect(rect.x + (rect.width * 0.40f), rect.y, rect.width * 0.60f - pad, rect.height), value, vStyle);
        }
        else
        {
            vStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(rect.x, rect.y, rect.width, rect.height), value, vStyle);
        }
    }

    /// <summary>
    /// Draws an animated progress bar for powerups/shield duration.
    /// </summary>
    public static void DrawProgressBar(Rect rect, float progress01, Color fillColor, Color bgColor, Color borderColor)
    {
        DrawCard(rect, bgColor, borderColor, 1.5f);
        float fillW = Mathf.Clamp01(progress01) * (rect.width - 4f);
        if (fillW > 0f)
        {
            Rect fillRect = new Rect(rect.x + 2f, rect.y + 2f, fillW, rect.height - 4f);
            DrawRect(fillRect, fillColor);
            // Top highlight
            DrawRect(new Rect(fillRect.x, fillRect.y, fillRect.width, fillRect.height * 0.45f), new Color(1f, 1f, 1f, 0.25f));
        }
    }
}
