using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer;
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
        }

        private async Task AnalyzeInvoice(Stream myBlob,ILogger log)
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
