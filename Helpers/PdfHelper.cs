using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.IO;
using System.Web.Hosting;

namespace UmbiloRentals.Helpers
{
    public static class PdfHelper
    {
        public static string GenerateAllocationLetter(
            string applicantName,
            string roomNumber,
            decimal monthlyRent)
        {
            // Create folder if it doesn't exist
            string folder = HostingEnvironment.MapPath("~/AllocationLetters");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // Unique file name
            string fileName = $"Allocation_{Guid.NewGuid()}.pdf";
            string fullPath = Path.Combine(folder, fileName);

            // Create document
            Document document = new Document(PageSize.A4, 40, 40, 40, 40);
            PdfWriter.GetInstance(document, new FileStream(fullPath, FileMode.Create));

            document.Open();

            // ---------- Fonts ----------
            Font title = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 24, new BaseColor(37, 99, 235));
            Font heading = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 15, new BaseColor(23, 52, 92));
            Font body = FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.BLACK);
            Font small = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GRAY);

            // ---------- Header ----------
            PdfPTable header = new PdfPTable(2);
            header.WidthPercentage = 100;
            header.SetWidths(new float[] { 70, 30 });
            header.DefaultCell.Border = Rectangle.NO_BORDER;

            PdfPCell left = new PdfPCell();
            left.Border = Rectangle.NO_BORDER;
            left.AddElement(new Paragraph("UMBILO RENTALS", title));
            left.AddElement(new Paragraph("Official Room Allocation Letter", heading));

            PdfPCell right = new PdfPCell(new Phrase(DateTime.Now.ToString("dd MMM yyyy"), body));
            right.Border = Rectangle.NO_BORDER;
            right.HorizontalAlignment = Element.ALIGN_RIGHT;
            right.VerticalAlignment = Element.ALIGN_MIDDLE;

            header.AddCell(left);
            header.AddCell(right);

            document.Add(header);
            document.Add(new Paragraph(" "));

            // Divider
            PdfPTable divider = new PdfPTable(1);
            divider.WidthPercentage = 100;

            PdfPCell line = new PdfPCell();
            line.FixedHeight = 2;
            line.BackgroundColor = new BaseColor(37, 99, 235);
            line.Border = Rectangle.NO_BORDER;

            divider.AddCell(line);
            document.Add(divider);

            document.Add(new Paragraph(" "));
            document.Add(new Paragraph($"Dear {applicantName},", heading));
            document.Add(new Paragraph(" "));

            document.Add(new Paragraph(
                "Congratulations! We are pleased to inform you that your accommodation application has been approved. We look forward to welcoming you to Umbilo Rentals.",
                body));

            document.Add(new Paragraph(" "));

            // ---------- Allocation Card ----------
            PdfPTable card = new PdfPTable(2);
            card.WidthPercentage = 100;
            card.SpacingBefore = 10;
            card.SpacingAfter = 15;
            card.SetWidths(new float[] { 40, 60 });

            BaseColor softBlue = new BaseColor(234, 244, 255);

            void AddRow(string label, string value)
            {
                PdfPCell c1 = new PdfPCell(new Phrase(label, heading));
                c1.BackgroundColor = softBlue;
                c1.Padding = 10;
                c1.BorderColor = new BaseColor(220, 230, 240);

                PdfPCell c2 = new PdfPCell(new Phrase(value, body));
                c2.Padding = 10;
                c2.BorderColor = new BaseColor(220, 230, 240);

                card.AddCell(c1);
                card.AddCell(c2);
            }

            AddRow("Applicant", applicantName);
            AddRow("Allocated Room", roomNumber);
            AddRow("Monthly Rent", $"R {monthlyRent:N2}");
            AddRow("Status", "Approved");
            AddRow("Reference", $"UR-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}");

            document.Add(card);

            // ---------- Next Steps ----------
            document.Add(new Paragraph("Next Steps", heading));
            document.Add(new Paragraph(" ", body));

            var list = new iTextSharp.text.List(iTextSharp.text.List.UNORDERED);

            list.Add(new ListItem("Bring a valid ID when collecting your keys.", body));
            list.Add(new ListItem("Submit proof of payment before moving in.", body));
            list.Add(new ListItem("Contact the office if you need assistance.", body));

            document.Add(list);

            document.Add(new Paragraph(" "));
            document.Add(new Paragraph("Thank you for choosing Umbilo Rentals.", heading));
            document.Add(new Paragraph(" "));
            document.Add(new Paragraph("Management", heading));
            document.Add(new Paragraph("Umbilo Rentals", body));
            document.Add(new Paragraph("info@umbilorentals.co.za", small));
            document.Add(new Paragraph("Umbilo, Durban", small));

            document.Close();

            return "~/AllocationLetters/" + fileName;
        }
    }
}