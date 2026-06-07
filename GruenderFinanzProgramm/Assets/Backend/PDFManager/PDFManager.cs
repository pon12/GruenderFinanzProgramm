using System.Collections.Generic;

public static class PDFManager
{
    public static List<UserPDFDocument> GetPDFsForUser(int userId, DataBase db)
    {
        return db.getPDFDocumentsByUser(userId);
    }

    public static UserPDFDocument GetPDFById(int pdfId, int userId, DataBase db)
    {
        return db.getUserPDFDocumentById(pdfId, userId);
    }
}