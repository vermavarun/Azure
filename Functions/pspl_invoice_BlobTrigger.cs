// https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/formrecognizer/Azure.AI.FormRecognizer/samples/V3.1/Sample3_RecognizeReceipts.md
// https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/formrecognizer/Azure.AI.FormRecognizer/samples/V3.1/Sample1_RecognizeFormContent.md

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.AI.FormRecognizer.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace PSPL.Invoice
{
    public class pspl_invoice_BlobTrigger
    {
        [FunctionName("pspl_invoice_BlobTrigger")]
        public async Task Run([BlobTrigger("dev/{name}", Connection = "psplstorage_STORAGE")] Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            await AnalyzeInvoice(myBlob,log);
            await AnalyzeReport(myBlob, log);
        }

        private async Task AnalyzeReport(Stream myBlog, ILogger log)
        {

            string endpoint = Environment.GetEnvironmentVariable("formRecognizerEndPoint");
            string apiKey = Environment.GetEnvironmentVariable("formRecognizerKey");
            var credential = new AzureKeyCredential(apiKey);
            var client = new DocumentAnalysisClient(new Uri(endpoint), credential);

            using var stream = myBlog;

            AnalyzeDocumentOperation operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", stream);
            AnalyzeResult result = operation.Value;

            foreach (DocumentPage page in result.Pages)
            {
               log.LogInformation($"Document Page {page.PageNumber} has {page.Lines.Count} line(s), {page.Words.Count} word(s),");
               log.LogInformation($"and {page.SelectionMarks.Count} selection mark(s).");

                for (int i = 0; i < page.Lines.Count; i++)
                {
                    DocumentLine line = page.Lines[i];
                   log.LogInformation($"  Line {i} has content: '{line.Content}'.");

                   log.LogInformation($"    Its bounding polygon (points ordered clockwise):");

                    for (int j = 0; j < line.BoundingPolygon.Count; j++)
                    {
                       log.LogInformation($"      Point {j} => X: {line.BoundingPolygon[j].X}, Y: {line.BoundingPolygon[j].Y}");
                    }
                }

                for (int i = 0; i < page.SelectionMarks.Count; i++)
                {
                    DocumentSelectionMark selectionMark = page.SelectionMarks[i];

                   log.LogInformation($"  Selection Mark {i} is {selectionMark.State}.");
                   log.LogInformation($"    Its bounding polygon (points ordered clockwise):");

                    for (int j = 0; j < selectionMark.BoundingPolygon.Count; j++)
                    {
                       log.LogInformation($"      Point {j} => X: {selectionMark.BoundingPolygon[j].X}, Y: {selectionMark.BoundingPolygon[j].Y}");
                    }
                }
            }

           log.LogInformation("Paragraphs:");

            foreach (DocumentParagraph paragraph in result.Paragraphs)
            {
               log.LogInformation($"  Paragraph content: {paragraph.Content}");

                if (paragraph.Role != null)
                {
                   log.LogInformation($"    Role: {paragraph.Role}");
                }
            }

            foreach (DocumentStyle style in result.Styles)
            {
                // Check the style and style confidence to see if text is handwritten.
                // Note that value '0.8' is used as an example.

                bool isHandwritten = style.IsHandwritten.HasValue && style.IsHandwritten == true;

                if (isHandwritten && style.Confidence > 0.8)
                {
                   log.LogInformation($"Handwritten content found:");

                    foreach (DocumentSpan span in style.Spans)
                    {
                       log.LogInformation($"  Content: {result.Content.Substring(span.Index, span.Length)}");
                    }
                }
            }

           log.LogInformation("The following tables were extracted:");

            for (int i = 0; i < result.Tables.Count; i++)
            {
                DocumentTable table = result.Tables[i];
               log.LogInformation($"  Table {i} has {table.RowCount} rows and {table.ColumnCount} columns.");

                foreach (DocumentTableCell cell in table.Cells)
                {
                   log.LogInformation($"    Cell ({cell.RowIndex}, {cell.ColumnIndex}) has kind '{cell.Kind}' and content: '{cell.Content}'.");
                }
            }

        }

        private async Task AnalyzeInvoice(Stream myBlob, ILogger log)
        {
            string endpoint = Environment.GetEnvironmentVariable("formRecognizerEndPoint");
            string apiKey = Environment.GetEnvironmentVariable("formRecognizerKey");
            var credential = new AzureKeyCredential(apiKey);
            var client = new FormRecognizerClient(new Uri(endpoint), credential);

            using var stream = myBlob;
            var options = new RecognizeReceiptsOptions() { Locale = "en-US" };

            RecognizeReceiptsOperation operation = await client.StartRecognizeReceiptsAsync(stream, options);
            Response<RecognizedFormCollection> operationResponse = await operation.WaitForCompletionAsync();
            RecognizedFormCollection receipts = operationResponse.Value;

            // To see the list of the supported fields returned by service and its corresponding types, consult:
            // https://aka.ms/formrecognizer/receiptfields

            foreach (RecognizedForm receipt in receipts)
            {
                if (receipt.Fields.TryGetValue("MerchantName", out FormField merchantNameField))
                {
                    if (merchantNameField.Value.ValueType == FieldValueType.String)
                    {
                        string merchantName = merchantNameField.Value.AsString();

                        log.LogInformation($"Merchant Name: '{merchantName}', with confidence {merchantNameField.Confidence}");
                    }
                }

                if (receipt.Fields.TryGetValue("TransactionDate", out FormField transactionDateField))
                {
                    if (transactionDateField.Value.ValueType == FieldValueType.Date)
                    {
                        DateTime transactionDate = transactionDateField.Value.AsDate();

                        log.LogInformation($"Transaction Date: '{transactionDate}', with confidence {transactionDateField.Confidence}");
                    }
                }

                if (receipt.Fields.TryGetValue("Items", out FormField itemsField))
                {
                    if (itemsField.Value.ValueType == FieldValueType.List)
                    {
                        foreach (FormField itemField in itemsField.Value.AsList())
                        {
                            log.LogInformation("Item:");

                            if (itemField.Value.ValueType == FieldValueType.Dictionary)
                            {
                                IReadOnlyDictionary<string, FormField> itemFields = itemField.Value.AsDictionary();

                                if (itemFields.TryGetValue("Name", out FormField itemNameField))
                                {
                                    if (itemNameField.Value.ValueType == FieldValueType.String)
                                    {
                                        string itemName = itemNameField.Value.AsString();

                                        log.LogInformation($"  Name: '{itemName}', with confidence {itemNameField.Confidence}");
                                    }
                                }

                                if (itemFields.TryGetValue("TotalPrice", out FormField itemTotalPriceField))
                                {
                                    if (itemTotalPriceField.Value.ValueType == FieldValueType.Float)
                                    {
                                        float itemTotalPrice = itemTotalPriceField.Value.AsFloat();

                                        log.LogInformation($"  Total Price: '{itemTotalPrice}', with confidence {itemTotalPriceField.Confidence}");
                                    }
                                }
                            }
                        }
                    }
                }

                if (receipt.Fields.TryGetValue("Total", out FormField totalField))
                {
                    if (totalField.Value.ValueType == FieldValueType.Float)
                    {
                        float total = totalField.Value.AsFloat();

                        log.LogInformation($"Total: '{total}', with confidence '{totalField.Confidence}'");
                    }
                }
            }


        }

    }
}
