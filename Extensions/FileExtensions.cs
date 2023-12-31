
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;

namespace achappey.ChatGPTeams.Extensions
{
    public static class FileExtensions
    {

        public static List<string> ConvertPdfToLines(this byte[] pdfBytes, int maxWordsPerLine = 1000)
        {
            using var stream = new MemoryStream(pdfBytes);
            using var pdfReader = new PdfReader(stream);
            using var pdfDocument = new PdfDocument(pdfReader);

            var paragraphs = new List<string>();

            for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
            {
                var page = pdfDocument.GetPage(pageNum);
                var strategy = new LocationTextExtractionStrategy();

                var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);

                var regex = new Regex(@"(?<=\r\n)(?=[A-Z0-9])");
                var rawParagraphs = regex.Split(pageText);

                foreach (var rawParagraph in rawParagraphs)
                {
                    string paragraph = rawParagraph.Trim();

                    if (!string.IsNullOrWhiteSpace(paragraph))
                    {
                        var words = paragraph.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                        var line = new StringBuilder();
                        int wordCount = 0;

                        foreach (var word in words)
                        {
                            line.Append(word + " ");
                            wordCount++;

                            if (wordCount >= maxWordsPerLine)
                            {
                                paragraphs.Add(line.ToString().Trim());
                                line.Clear();
                                wordCount = 0;
                            }
                        }

                        // Add remaining words if any
                        if (line.Length > 0)
                        {
                            paragraphs.Add(line.ToString().Trim());
                        }
                    }
                }
            }

            return paragraphs;
        }


        public static List<string> ConvertPptxToLines(this byte[] pptxBytes)
        {
            using var stream = new MemoryStream(pptxBytes);
            using var presentationDoc = PresentationDocument.Open(stream, false);

            var paragraphs = new List<string>();

            PresentationPart presentationPart = presentationDoc.PresentationPart;
            if (presentationPart != null)
            {
                foreach (SlideId slideId in presentationPart.Presentation.SlideIdList)
                {
                    SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId);

                    foreach (Shape shape in slidePart.Slide.Descendants<Shape>())
                    {
                        foreach (A.Paragraph paragraph in shape.Descendants<A.Paragraph>())
                        {
                            string paragraphText = string.Join(" ", paragraph.Descendants<A.Text>().Select(t => t.Text)).Trim();

                            if (!string.IsNullOrEmpty(paragraphText))
                            {
                                paragraphs.Add(paragraphText);
                            }
                        }
                    }
                }
            }

            return paragraphs;
        }

        public static List<string> ConvertDocxToLines(this byte[] docxBytes)
        {
            var result = new List<string>();

            using (var memoryStream = new MemoryStream(docxBytes))
            {
                using (var wordDocument = WordprocessingDocument.Open(memoryStream, false))
                {
                    var body = wordDocument.MainDocumentPart.Document.Body;
                    var paragraphs = body.Descendants<W.Paragraph>();


                    foreach (var paragraph in paragraphs)
                    {
                        var sb = new StringBuilder();
                        var runs = paragraph.Elements<W.Run>();

                        foreach (var run in runs)
                        {
                            var textElements = run.Elements<W.Text>();
                            var text = string.Join(string.Empty, textElements.Select(t => t.Text));
                            sb.Append(text);
                        }

                        if (!string.IsNullOrEmpty(sb.ToString().Trim()))
                        {
                            result.Add(sb.ToString());
                        }

                    }

                    return result;
                }
            }
        }

        public static List<string> ConvertTxtToList(this byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes))
            {
                using (var streamReader = new StreamReader(memoryStream, Encoding.UTF8))
                {
                    List<string> lines = new List<string>();

                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        string trimmedLine = line.Trim();

                        if (!string.IsNullOrEmpty(trimmedLine))
                        {
                            lines.Add(trimmedLine);
                        }

                    }

                    return lines;
                }
            }
        }

        public static List<string> ConvertHtmlToList(this byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes))
            {
                using (var streamReader = new StreamReader(memoryStream, Encoding.UTF8))
                {
                    string htmlContent = streamReader.ReadToEnd();

                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(htmlContent);

                    return htmlDocument.ExtractTextFromHtmlParagraphs();
                }
            }
        }

        public static List<Tuple<byte[], string, DateTime?>> ExtractAttachments(this byte[] msgBytes)
        {
            using var stream = new MemoryStream(msgBytes);
            using var message = new MsgReader.Outlook.Storage.Message(stream);

            var attachments = new List<Tuple<byte[], string, DateTime?>>();

            foreach (MsgReader.Outlook.Storage.Attachment attachment in message.Attachments)
            {
                // Make a copy of the attachment data.
                byte[] dataCopy = new List<byte>(attachment.Data).ToArray();

                // Create a tuple containing the data, filename, and last modified time.
                var tuple = new Tuple<byte[], string, DateTime?>(dataCopy, attachment.FileName, attachment.LastModificationTime);

                attachments.Add(tuple);

                // Dispose of the Attachment object.
                attachment.Dispose();
            }

            return attachments;
        }

        public static List<string> ConvertMsgToLines(this byte[] msgBytes)
        {
            using var stream = new MemoryStream(msgBytes);
            using var message = new MsgReader.Outlook.Storage.Message(stream);

            var paragraphs = new List<string>
            {
                "Subject: " + message.Subject,
                "Sender: " + message.Sender,
                "Recipients: " + string.Join(",", message.Recipients.Select(a => a.DisplayName)),
                "SentOn: " + message.SentOn?.ToString()
            };

            // Extract the body of the message.
            string body = message.BodyText;

            // Split the body into lines.
            var lines = body.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();

                if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    paragraphs.Add(trimmedLine);
                }
            }

            return paragraphs;
        }

        public static List<string> ExtractTextFromHtmlParagraphs(this HtmlDocument htmlDoc)
        {
            var paragraphs = htmlDoc.DocumentNode.SelectNodes("//p");
            var pageParagraphs = new List<string>();

            if (paragraphs is not null)
            {
                foreach (var paragraph in paragraphs)
                {
                    var htmlContent = paragraph.InnerHtml.Trim();  // Voor de inhoud binnen <p> tags

                    if (!string.IsNullOrEmpty(htmlContent))
                    {
                        pageParagraphs.Add(htmlContent);
                    }
                }
            }

            return pageParagraphs;
        }


        public static async Task<List<string>> ConvertPageToList(this HttpClient httpClient, string url)
        {
            // Download the HTML content of the provided URL.
            var htmlContent = await httpClient.GetStringAsync(url);

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(htmlContent);

            return htmlDocument.ExtractTextFromHtmlParagraphs();

        }

        public static List<string> ConvertCsvToList(this byte[] csvBytes)
        {
            var records = new List<string>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                MissingFieldFound = null
            };
            using (var memoryStream = new MemoryStream(csvBytes))
            using (var reader = new StreamReader(memoryStream))
            using (var csv = new CsvReader(reader, config))
            {
                var recordsList = csv.GetRecords<dynamic>();

                foreach (var record in recordsList)
                {
                    var recordDictionary = record as IDictionary<string, object>;
                    var formattedRecord = new List<string>();

                    foreach (var entry in recordDictionary)
                    {
                        formattedRecord.Add($"{entry.Key}:{entry.Value.ToString().Trim()}");
                    }

                    records.Add(string.Join(";", formattedRecord));
                }
            }

            return records;
        }
    }
}