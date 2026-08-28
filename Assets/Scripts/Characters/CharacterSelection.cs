using System;
using UnityEngine;

public enum CharacterChoice { Girl = 0, Boy = 1 }

public static class CharacterSelection
{
    private const string PlayerPrefsKey = "SelectedCharacter";

    // PlayerPrefs is stored per-product machine-wide (the Windows registry key is shared by
    // every running instance of the same build), so two local test instances on one PC would
    // otherwise both read/write the same saved choice. This lets a launched build override it
    // per-process via e.g. "Game.exe -character=Boy", without touching the real saved PlayerPrefs.
    private static readonly CharacterChoice? commandLineOverride = ParseCommandLineOverride();

    // Unity Multiplayer Play Mode runs the Main Editor and every Virtual Player as separate
    // processes that all share the same company+product identity, so - like the local-build case
    // above - they'd otherwise all read/write the same "SelectedCharacter" registry value and
    // stomp each other's Wardrobe pick. MPPM tags each Virtual Player process with "-vpId=<N>" on
    // its command line (absent for the Main Editor and for real standalone builds), so folding
    // that into the key namespaces each instance's saved choice separately.
    private static readonly string mppmKeySuffix = ParseMppmKeySuffix();

    public static CharacterChoice Local
    {
        get => commandLineOverride ?? (CharacterChoice)PlayerPrefs.GetInt(PlayerPrefsKey + mppmKeySuffix, (int)CharacterChoice.Girl);
        set
        {
            PlayerPrefs.SetInt(PlayerPrefsKey + mppmKeySuffix, (int)value);
            PlayerPrefs.Save();
        }
    }

    private static CharacterChoice? ParseCommandLineOverride()
    {
        foreach (string arg in Environment.GetCommandLineArgs())
        {
            if (arg.Equals("-character=Boy", StringComparison.OrdinalIgnoreCase)) return CharacterChoice.Boy;
            if (arg.Equals("-character=Girl", StringComparison.OrdinalIgnoreCase)) return CharacterChoice.Girl;
        }
        return null;
    }

    private static string ParseMppmKeySuffix()
    {
        const string vpIdArgPrefix = "-vpId=";
        foreach (string arg in Environment.GetCommandLineArgs())
        {
            if (arg.StartsWith(vpIdArgPrefix, StringComparison.OrdinalIgnoreCase))
                return "_vp" + arg.Substring(vpIdArgPrefix.Length);
        }
        return string.Empty;
    }
}
