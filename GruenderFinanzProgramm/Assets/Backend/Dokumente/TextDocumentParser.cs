using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ParsedTextDocument
{
    public string documentType;
    public string title;
    public string plainText;
    public List<string> bodyLines = new List<string>();
}

public static class TextDocumentParser
{
    public static ParsedTextDocument ParseTextDocument(string filePath)
    {
        ParsedTextDocument parsedDocument = new ParsedTextDocument
        {
            documentType = TextDocumentService.TYPE_STANDARD,
            title = "",
            plainText = ""
        };

        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("[TextDocumentParser] filePath ist leer.");
            return parsedDocument;
        }

        if (!File.Exists(filePath))
        {
            Debug.LogError("[TextDocumentParser] Datei nicht gefunden: " + filePath);
            return parsedDocument;
        }

        string[] lines = File.ReadAllLines(filePath);

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (IsMetaLine(trimmedLine))
            {
                ParseMetaLine(trimmedLine, parsedDocument);
                continue;
            }

            parsedDocument.bodyLines.Add(line);
        }

        parsedDocument.plainText = string.Join(Environment.NewLine, parsedDocument.bodyLines);

        return parsedDocument;
    }

    public static string ReadPlainText(string filePath)
    {
        ParsedTextDocument parsedDocument = ParseTextDocument(filePath);
        return parsedDocument.plainText;
    }

    private static bool IsMetaLine(string line)
    {
        return line.StartsWith("[") && line.EndsWith("]");
    }

    private static void ParseMetaLine(string line, ParsedTextDocument parsedDocument)
    {
        if (line.StartsWith("[DOCTYPE "))
        {
            string type = line
                .Replace("[DOCTYPE ", "")
                .Replace("]", "")
                .Trim();

            parsedDocument.documentType = TextDocumentService.NormalizeDocumentType(type);
            return;
        }

        if (line.StartsWith("[TITLE "))
        {
            string title = line
                .Replace("[TITLE ", "")
                .Replace("]", "")
                .Trim();

            parsedDocument.title = title;
        }
    }
}