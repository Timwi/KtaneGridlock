﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Gridlock;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Gridlock
/// Created by Elias5891, implemented by Timwi
/// </summary>
public class GridlockModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMSelectable MainSelectable;
    public Material[] SquareColors;
    public KMSelectable NextButton;

    public TextMesh PageNumberText;
    public TextMesh TotalPagesText;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    [Flags]
    private enum Symbol
    {
        // Icons
        NoIcon = 0,

        Triangle = 1,
        Diamond = 2,
        Hexagon = 3,
        Star = 4,

        ArrowN = 5,
        ArrowNW = 6,
        ArrowW = 7,
        ArrowSW = 8,
        ArrowS = 9,
        ArrowSE = 10,
        ArrowE = 11,
        ArrowNE = 12,

        IconMask = 15,

        // Colors
        Blank = 0 << 4,
        Green = 1 << 4,
        Yellow = 2 << 4,
        Red = 3 << 4,
        Blue = 4 << 4,
        ColorMask = 15 << 4,
    }

    private Symbol[][] _pages;
    private Texture2D[] _textures;
    private MeshRenderer[] _squares;
    private MeshRenderer[] _symbols;
    private int _curPage;
    private int _solution;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        var numPages = Rnd.Range(5, 11);
        _pages = new Symbol[numPages][];
        for (int i = 0; i < numPages; i++)
        {
            _pages[i] = new Symbol[16];
            for (int j = 0; j < 16; j++)
            {
                switch (Rnd.Range(0, 3))
                {
                    // Colored symbol (avoid star in Page 0)
                    case 0: _pages[i][j] = (Symbol) (Rnd.Range(1, i == 0 ? 4 : 5)) | (Symbol) (Rnd.Range(1, 5) << 4); break;
                    // Arrow
                    case 1: _pages[i][j] = (Symbol) (Rnd.Range(5, 13)); break;
                    // Blank
                    default: _pages[i][j] = Symbol.NoIcon; break;
                }
            }
        }

        // Place a single star in Page 0
        var firstPageStarPosition = Rnd.Range(0, 16);
        _pages[0][firstPageStarPosition] = Symbol.Star | (Symbol) (Rnd.Range(1, 5) << 4);

        // Create the textures for all the symbols
        _textures = new Texture2D[Data.RawPngs.Length];
        for (int i = 0; i < Data.RawPngs.Length; i++)
        {
            _textures[i] = new Texture2D(2, 2);
            _textures[i].LoadImage(Data.RawPngs[i]);
        }

        // Find the objects on which we need to set the colors and symbols
        _squares = new MeshRenderer[16];
        _symbols = new MeshRenderer[16];
        for (int i = 0; i < 16; i++)
        {
            _squares[i] = MainSelectable.Children[i].GetComponent<MeshRenderer>();
            _squares[i].material = SquareColors[0];
            _symbols[i] = _squares[i].transform.FindChild("Symbol").GetComponent<MeshRenderer>();
            _symbols[i].gameObject.SetActive(false);
        }

        // Calculate the solution
        var colorMapX = new Dictionary<Symbol, int[]>
        {
            { Symbol.Blue, new[] { -1, -1, 0, -1, 0, 0, -1, -1, 1, 1, 1, 1, 1, 0, 1, -1 } },
            { Symbol.Green, new[] { 1, 0, 1, -1, -1, -1, 0, -1, 0, 0, -1, -1, 1, 1, 1, 1 } },
            { Symbol.Yellow, new[] { -1, 0, 0, -1, 1, 1, 1, 1, -1, 1, 0, 1, -1, -1, -1, 0 } },
            { Symbol.Red, new[] { 0, -1, -1, -1, -1, -1, 0, 0, 1, 1, 1, 1, 1, -1, 1, 0 } }
        };
        var colorMapY = new Dictionary<Symbol, int[]>
        {
            { Symbol.Blue, new[] { 0, 1, -1, -1, -1, 1, -1, 0, 1, 0, -1, 1, 0, 1, -1, 1 } },
            { Symbol.Green, new[] { 0, 1, -1, 1, 0, 1, -1, -1, -1, 1, -1, 0, 1, 0, -1, 1 } },
            { Symbol.Yellow, new[] { 0, -1, 1, -1, 1, 1, 0, -1, 1, 0, 1, -1, -1, 0, 1, -1 } },
            { Symbol.Red, new[] { -1, -1, 0, 1, -1, 0, -1, 1, -1, 1, 1, 0, -1, 1, 0, 1 } }
        };
        var symbolColorMapX = new Dictionary<Symbol, int>
        {
            { Symbol.Triangle | Symbol.Blue, -1 },
            { Symbol.Triangle | Symbol.Green, 1 },
            { Symbol.Triangle | Symbol.Yellow, 0 },
            { Symbol.Triangle | Symbol.Red, 1 },
            { Symbol.Diamond | Symbol.Blue, -1 },
            { Symbol.Diamond | Symbol.Green, 0 },
            { Symbol.Diamond | Symbol.Yellow, -1 },
            { Symbol.Diamond | Symbol.Red, -1 },
            { Symbol.Hexagon | Symbol.Blue, 0 },
            { Symbol.Hexagon | Symbol.Green, -1 },
            { Symbol.Hexagon | Symbol.Yellow, 0 },
            { Symbol.Hexagon | Symbol.Red, -1 },
            { Symbol.Star | Symbol.Blue, 1 },
            { Symbol.Star | Symbol.Green, 1 },
            { Symbol.Star | Symbol.Yellow, 1 },
            { Symbol.Star | Symbol.Red, 1 }
        };
        var symbolColorMapY = new Dictionary<Symbol, int>
        {
            { Symbol.Triangle | Symbol.Blue, 1 },
            { Symbol.Triangle | Symbol.Green, -1 },
            { Symbol.Triangle | Symbol.Yellow, 1 },
            { Symbol.Triangle | Symbol.Red, 0 },
            { Symbol.Diamond | Symbol.Blue, 1 },
            { Symbol.Diamond | Symbol.Green, -1 },
            { Symbol.Diamond | Symbol.Yellow, 0 },
            { Symbol.Diamond | Symbol.Red, -1 },
            { Symbol.Hexagon | Symbol.Blue, -1 },
            { Symbol.Hexagon | Symbol.Green, -1 },
            { Symbol.Hexagon | Symbol.Yellow, 1 },
            { Symbol.Hexagon | Symbol.Red, 0 },
            { Symbol.Star | Symbol.Blue, -1 },
            { Symbol.Star | Symbol.Green, 1 },
            { Symbol.Star | Symbol.Yellow, 1 },
            { Symbol.Star | Symbol.Red, 0 },
        };

        var taken = new bool[16];
        var curPos = firstPageStarPosition;
        var curPage = 0;
        taken[curPos] = true;
        Symbol lastColor = 0;
        Debug.LogFormat(@"[Gridlock #{0}] Starting at {1} on page {2}.", _moduleId, coord(curPos), curPage + 1);
        while (true)
        {
            var xDir = 0;
            var yDir = 0;
            var symbol = _pages[curPage][curPos];
            var loggingText = "";
            var nextPage = false;

            switch (symbol & Symbol.IconMask)
            {
                case Symbol.NoIcon:
                    xDir = colorMapX[lastColor][curPos];
                    yDir = colorMapY[lastColor][curPos];
                    loggingText = string.Format("blank. Last color was {0}", lastColor);
                    break;

                case Symbol.Triangle:
                case Symbol.Diamond:
                case Symbol.Hexagon:
                case Symbol.Star:
                    xDir = symbolColorMapX[symbol];
                    yDir = symbolColorMapY[symbol];
                    lastColor = symbol & Symbol.ColorMask;
                    nextPage = true;
                    loggingText = string.Format("a {0} {1}", lastColor, symbol & Symbol.IconMask);
                    break;

                default:
                    loggingText = string.Format("an arrow pointing {0}", (symbol & Symbol.IconMask).ToString().Substring(5));
                    switch (symbol & Symbol.IconMask)
                    {
                        case Symbol.ArrowN: yDir = -1; break;
                        case Symbol.ArrowNW: yDir = -1; xDir = -1; break;
                        case Symbol.ArrowW: xDir = -1; break;
                        case Symbol.ArrowSW: yDir = 1; xDir = -1; break;
                        case Symbol.ArrowS: yDir = 1; break;
                        case Symbol.ArrowSE: yDir = 1; xDir = 1; break;
                        case Symbol.ArrowE: xDir = 1; break;
                        case Symbol.ArrowNE: yDir = -1; xDir = 1; break;
                    }
                    break;
            }

            Debug.LogFormat(@"[Gridlock #{0}] {1} on page {2} is {3}. Moving {4}.", _moduleId, coord(curPos), curPage + 1, loggingText, dir(xDir, yDir) + (nextPage ? string.Format(" and switching to page {0}", (curPage + 1) % _pages.Length + 1) : ""));

            if (nextPage)
                curPage = (curPage + 1) % _pages.Length;

            var newPos = curPos;
            do
                newPos = (newPos % 4 + xDir + 4) % 4 + 4 * ((newPos / 4 + yDir + 4) % 4);
            while (newPos != curPos && taken[newPos]);

            if (newPos == curPos)
                break;

            curPos = newPos;
            taken[newPos] = true;
        }

        Debug.LogFormat(@"[Gridlock #{0}] Gridlock occurred at {1}.", _moduleId, coord(curPos));
        _solution = curPos;

        for (int i = 0; i < 16; i++)
            MainSelectable.Children[i].OnInteract = GetSquareClickHandler(i);
        NextButton.OnInteract = delegate { _curPage = (_curPage + 1) % _pages.Length; SetPage(); return false; };

        _curPage = 0;
        SetPage();
    }

    private static string[] _directions = "north-west|north|north-east|west||east|south-west|south|south-east".Split('|');
    private static string dir(int xDir, int yDir)
    {
        return _directions[xDir + 1 + 3 * (yDir + 1)];
    }
    private static string coord(int pos)
    {
        return "" + (char) ('A' + (pos % 4)) + (char) ('1' + (pos / 4));
    }

    private KMSelectable.OnInteractHandler GetSquareClickHandler(int i)
    {
        return delegate
        {
            if (i == _solution)
            {
                Debug.LogFormat(@"[Gridlock #{0}] Pressed {1}: correct. Module solved.", _moduleId, coord(i));
                Module.HandlePass();
            }
            else
            {
                Debug.LogFormat(@"[Gridlock #{0}] Pressed {1}: wrong.", _moduleId, coord(i));
                Module.HandleStrike();
            }
            return false;
        };
    }

    private void SetPage()
    {
        for (int i = 0; i < 16; i++)
        {
            var symbol = _pages[_curPage][i];
            var textureIx = (int) (symbol & Symbol.IconMask) - 1;
            if (textureIx == -1)
                _symbols[i].gameObject.SetActive(false);
            else
            {
                _symbols[i].gameObject.SetActive(true);
                _symbols[i].material.mainTexture = _textures[textureIx];
            }

            _squares[i].material = SquareColors[(int) (symbol & Symbol.ColorMask) >> 4];
        }
        PageNumberText.text = (_curPage + 1).ToString();
        TotalPagesText.text = _pages.Length.ToString();
    }

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToLowerInvariant();
        var m = Regex.Match(command, @"^press (next|[a-d][1-4])$");

        if (!m.Success)
            yield break;

        yield return null;
        if (m.Groups[1].Value == "next")
        {
            _curPage = (_curPage + 1) % _pages.Length;
            SetPage();
        }
        else
            MainSelectable.Children[(m.Groups[1].Value[0] - 'a') + 4 * (m.Groups[1].Value[1] - '1')].OnInteract();
    }
}
