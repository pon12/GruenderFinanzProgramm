using System;
using System.IO;
using UnityEngine;
using iTextSharp.text;
using iTextSharp.text.pdf;

public static class TextDocumentPdfExporter
{
    public static UserPDFDocument ExportTextDocumentToPdf(
        TextDocumentMeta textDocument,
        int userId,
        DataBase db,
        string category = "Textdokumente"
    )
    {
        if (textDocument == null)
        {
            Debug.LogError("[TextDocumentPdfExporter] TextDocumentMeta ist null.");
            return null;
        }

        if (db == null)
        {
            Debug.LogError("[TextDocumentPdfExporter] Datenbank ist null.");
            return null;
        }

        if (string.IsNullOrEmpty(textDocument.filePath) || !File.Exists(textDocument.filePath))
        {
            Debug.LogError("[TextDocumentPdfExporter] Textdatei nicht gefunden: " + textDocument.filePath);
            return null;
        }

        ParsedTextDocument parsedDocument =
            TextDocumentParser.ParseTextDocument(textDocument.filePath);

        string pdfFolder = PDFUserFolderSetup.GetUserCategoryFolder(userId, category);

        string safeTitle = MakeSafeFileName(
            string.IsNullOrEmpty(textDocument.title)
                ? "Textdokument"
                : textDocument.title
        );

        string pdfFileName =
            safeTitle + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf";

        string pdfPath = Path.Combine(pdfFolder, pdfFileName);

        try
        {
            iTextSharp.text.Document pdfDocument =
                new iTextSharp.text.Document(PageSize.A4, 50, 50, 50, 50);

            using (FileStream fileStream = new FileStream(pdfPath, FileMode.Create))
            {
                PdfWriter.GetInstance(pdfDocument, fileStream);

                pdfDocument.AddAuthor("Ventoriq");
                pdfDocument.AddTitle(textDocument.title);

                pdfDocument.Open();

                iTextSharp.text.Font titleFont =
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);

                iTextSharp.text.Font metaFont =
                    FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9);

                iTextSharp.text.Font normalFont =
                    FontFactory.GetFont(FontFactory.HELVETICA, 12);

                string title = string.IsNullOrEmpty(parsedDocument.title)
                    ? textDocument.title
                    : parsedDocument.title;

                if (!string.IsNullOrEmpty(title))
                {
                    iTextSharp.text.Paragraph titleParagraph =
                        new iTextSharp.text.Paragraph(title, titleFont);

                    titleParagraph.SpacingAfter = 12f;
                    pdfDocument.Add(titleParagraph);
                }

                iTextSharp.text.Paragraph metaParagraph =
                    new iTextSharp.text.Paragraph(
                        "Dokumenttyp: " + parsedDocument.documentType,
                        metaFont
                    );

                metaParagraph.SpacingAfter = 16f;
                pdfDocument.Add(metaParagraph);

                string plainText = parsedDocument.plainText;

                if (string.IsNullOrWhiteSpace(plainText))
                {
                    plainText = "Dieses Dokument enthält keinen Textinhalt.";
                }

                string[] paragraphs = plainText.Split(
                    new string[] { Environment.NewLine + Environment.NewLine },
                    StringSplitOptions.None
                );

                foreach (string paragraphText in paragraphs)
                {
                    iTextSharp.text.Paragraph paragraph =
                        new iTextSharp.text.Paragraph(paragraphText, normalFont);

                    paragraph.SpacingAfter = 8f;
                    pdfDocument.Add(paragraph);
                }

                pdfDocument.Close();
            }

            UserPDFDocument pdfMeta = new UserPDFDocument
            {
                userId = userId,
                originalFileName = pdfFileName,
                storedFileName = pdfFileName,
                filePath = pdfPath,
                category = category,
                uploadedAt = DateTime.Now
            };

            db.createUserPDFDocument(pdfMeta);

            Debug.Log("[TextDocumentPdfExporter] PDF erfolgreich erstellt: " + pdfPath);

            return pdfMeta;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[TextDocumentPdfExporter] Fehler beim PDF-Export: " + exception.Message
            );

            return null;
        }
    }

    private static string MakeSafeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "Textdokument";
        }

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return fileName.Trim();
    }
}