using UnityEngine;
using System;
using System.Diagnostics;

public static class CopyPaste
{
    public static void CopyToClipboard(string text)
    {
        GUIUtility.systemCopyBuffer = text;

        switch (Application.platform)
        {
            case RuntimePlatform.WindowsPlayer:
                CopyToClipboardWindows(text);
                break;

            case RuntimePlatform.LinuxPlayer:
                CopyToClipboardLinux(text);
                break;
        }

        UnityEngine.Debug.Log("Text in Zwischenablage kopiert: " + text);
    }

    private static void CopyToClipboardWindows(string text)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c echo {text} | clip",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Windows Clipboard Fehler: " + e.Message);
        }
    }

    private static void CopyToClipboardLinux(string text)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"echo '{text}' | xclip -selection clipboard\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process.Start(psi);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Linux Clipboard Fehler: " + e.Message);
        }
    }

    public static void CopyPasskey(string passkey)
    {
        CopyToClipboard(passkey);
    }

    public static void CopyRecoveryKey(string recoveryKey)
    {
        CopyToClipboard(recoveryKey);
    }
}