using System;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using System.Collections.Generic;
using iText.Kernel.Font;
using iText.IO.Font.Constants;

namespace FacePass.Management.Services
{
    public class ReportService
    {
        public void GenerateAttendanceReport(string filePath, string studentName, string rollNo, List<AttendanceRecord> records)
        {
            using (var writer = new PdfWriter(filePath))
            {
                using (var pdf = new PdfDocument(writer))
                {
                    var document = new Document(pdf);
                    
                    var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    var italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

                    // Title
                    document.Add(new Paragraph("FacePass Attendance Report")
                        .SetFont(boldFont)
                        .SetFontSize(24)
                        .SetFontColor(ColorConstants.DARK_GRAY)
                        .SetTextAlignment(TextAlignment.CENTER));

                    document.Add(new Paragraph($"Student: {studentName} ({rollNo})")
                        .SetFontSize(14)
                        .SetMarginTop(20));

                    document.Add(new Paragraph($"Date Generated: {DateTime.Now:f}")
                        .SetFont(italicFont)
                        .SetFontSize(10)
                        .SetMarginBottom(20));

                    // Table
                    var table = new Table(4).UseAllAvailableWidth();
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Date & Time").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Course").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Method").SetFont(boldFont)));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Status").SetFont(boldFont)));

                    foreach (var record in records)
                    {
                        table.AddCell(record.Timestamp.ToString("g"));
                        table.AddCell(record.CourseName);
                        table.AddCell(record.Method);
                        
                        var statusCell = new Cell().Add(new Paragraph(record.Status));
                        if (record.Status == "suspicious") statusCell.SetFontColor(ColorConstants.RED);
                        else if (record.Status == "present") statusCell.SetFontColor(ColorConstants.GREEN);
                        
                        table.AddCell(statusCell);
                    }

                    document.Add(table);
                    document.Close();
                }
            }
        }
    }

    public class AttendanceRecord
    {
        public DateTime Timestamp { get; set; }
        public string CourseName { get; set; } = "";
        public string Method { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
