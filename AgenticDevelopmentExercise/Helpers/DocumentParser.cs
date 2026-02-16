using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AgenticDevelopmentExercise.Helpers
{
    internal class DocumentParser
    {
        /// <summary>
        /// Parses a PDF file and extracts all text content
        /// </summary>
        public async Task<string> ParsePdfAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var sb = new StringBuilder();

                    using (var reader = new PdfReader(filePath))
                    using (var pdfDoc = new PdfDocument(reader))
                    {
                        int totalPages = pdfDoc.GetNumberOfPages();
                        sb.AppendLine($"[PDF DOCUMENT: {Path.GetFileName(filePath)}]");
                        sb.AppendLine($"Total Pages: {totalPages}");
                        sb.AppendLine(new string('=', 60));
                        sb.AppendLine();

                        for (int page = 1; page <= totalPages; page++)
                        {
                            var pdfPage = pdfDoc.GetPage(page);

                            // Extract text using location-based strategy for better formatting
                            var strategy = new LocationTextExtractionStrategy();
                            string pageText = PdfTextExtractor.GetTextFromPage(pdfPage, strategy);

                            sb.AppendLine($"--- Page {page}/{totalPages} ---");
                            sb.AppendLine(pageText.Trim());
                            sb.AppendLine();
                        }

                        // Add metadata if available
                        var info = pdfDoc.GetDocumentInfo();
                        if (info != null)
                        {
                            sb.AppendLine(new string('=', 60));
                            sb.AppendLine("[Document Metadata]");

                            if (!string.IsNullOrEmpty(info.GetTitle()))
                                sb.AppendLine($"Title: {info.GetTitle()}");
                            if (!string.IsNullOrEmpty(info.GetAuthor()))
                                sb.AppendLine($"Author: {info.GetAuthor()}");
                            if (!string.IsNullOrEmpty(info.GetSubject()))
                                sb.AppendLine($"Subject: {info.GetSubject()}");
                        }
                    }

                    return sb.ToString();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to parse PDF '{Path.GetFileName(filePath)}': {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// Parses a Word document (DOCX) and extracts all text content
        /// </summary>
        public async Task<string> ParseWordDocumentAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var sb = new StringBuilder();

                    using (var doc = WordprocessingDocument.Open(filePath, false))
                    {
                        sb.AppendLine($"[WORD DOCUMENT: {Path.GetFileName(filePath)}]");
                        sb.AppendLine(new string('=', 60));
                        sb.AppendLine();

                        var body = doc.MainDocumentPart?.Document.Body;
                        if (body == null)
                        {
                            return "[Empty document]";
                        }

                        // Extract paragraphs
                        var paragraphs = body.Descendants<Paragraph>();
                        int paragraphCount = 0;

                        foreach (var paragraph in paragraphs)
                        {
                            string text = paragraph.InnerText;

                            // Skip empty paragraphs
                            if (string.IsNullOrWhiteSpace(text))
                            {
                                sb.AppendLine();
                                continue;
                            }

                            // Check if it's a heading (simple heuristic)
                            var paragraphProperties = paragraph.ParagraphProperties;
                            var style = paragraphProperties?.ParagraphStyleId?.Val?.Value;

                            if (style != null && style.StartsWith("Heading"))
                            {
                                sb.AppendLine();
                                sb.AppendLine($"## {text}");
                                sb.AppendLine();
                            }
                            else
                            {
                                sb.AppendLine(text);
                            }

                            paragraphCount++;
                        }

                        // Extract tables if any
                        var tables = body.Descendants<Table>();
                        if (tables.Any())
                        {
                            sb.AppendLine();
                            sb.AppendLine(new string('-', 60));
                            sb.AppendLine("[TABLES]");
                            sb.AppendLine();

                            int tableNum = 1;
                            foreach (var table in tables)
                            {
                                sb.AppendLine($"Table {tableNum}:");

                                foreach (var row in table.Descendants<TableRow>())
                                {
                                    var cells = row.Descendants<TableCell>();
                                    var cellTexts = cells.Select(c => c.InnerText.Trim()).ToArray();
                                    sb.AppendLine("| " + string.Join(" | ", cellTexts) + " |");
                                }

                                sb.AppendLine();
                                tableNum++;
                            }
                        }

                        sb.AppendLine(new string('=', 60));
                        sb.AppendLine($"[Total paragraphs: {paragraphCount}]");
                    }

                    return sb.ToString();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to parse Word document '{Path.GetFileName(filePath)}': {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// Parses a CSV file and formats it as a readable table
        /// </summary>
        public async Task<string> ParseCsvAsync(string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[CSV DOCUMENT: {Path.GetFileName(filePath)}]");
                sb.AppendLine(new string('=', 60));
                sb.AppendLine();

                var lines = await File.ReadAllLinesAsync(filePath);

                if (lines.Length == 0)
                {
                    return "[Empty CSV file]";
                }

                // Assume first line is header
                sb.AppendLine("Headers:");
                sb.AppendLine(lines[0]);
                sb.AppendLine();
                sb.AppendLine("Data:");

                for (int i = 1; i < Math.Min(lines.Length, 101); i++) // Limit to first 100 rows
                {
                    sb.AppendLine(lines[i]);
                }

                if (lines.Length > 101)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[... {lines.Length - 101} more rows]");
                }

                sb.AppendLine();
                sb.AppendLine($"[Total rows: {lines.Length - 1}]");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse CSV '{Path.GetFileName(filePath)}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses a JSON file and formats it
        /// </summary>
        public async Task<string> ParseJsonAsync(string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[JSON DOCUMENT: {Path.GetFileName(filePath)}]");
                sb.AppendLine(new string('=', 60));
                sb.AppendLine();

                string jsonContent = await File.ReadAllTextAsync(filePath);

                // Try to pretty-print JSON
                try
                {
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    string prettyJson = System.Text.Json.JsonSerializer.Serialize(
                        jsonDoc.RootElement,
                        options
                    );
                    sb.AppendLine(prettyJson);
                }
                catch
                {
                    // If parsing fails, just return raw content
                    sb.AppendLine(jsonContent);
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse JSON '{Path.GetFileName(filePath)}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Determines if a file is a supported document type
        /// </summary>
        public bool IsSupportedDocument(string filePath)
        {
            string[] supportedExtensions =
            {
                ".txt", ".md", ".log",
                ".pdf",
                ".docx", ".doc",
                ".csv",
                ".json",
                ".xml"
            };

            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return supportedExtensions.Contains(extension);
        }

        /// <summary>
        /// Gets a human-readable description of the document type
        /// </summary>
        public string GetDocumentTypeDescription(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".txt" => "Plain Text Document",
                ".md" => "Markdown Document",
                ".log" => "Log File",
                ".pdf" => "PDF Document",
                ".docx" or ".doc" => "Microsoft Word Document",
                ".csv" => "Comma-Separated Values (CSV)",
                ".json" => "JSON Data File",
                ".xml" => "XML Document",
                _ => "Unknown Document Type"
            };
        }
    }
}