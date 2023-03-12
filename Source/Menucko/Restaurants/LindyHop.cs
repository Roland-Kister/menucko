using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Tesseract;

namespace Menucko
{
    public static class LindyHop
    {
        [FunctionName(nameof(LindyHop))]
        public static async Task<IActionResult> GetMenu(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "lindy-hop")] HttpRequest req, ExecutionContext context)
        {
            var assetsPath = Path.Combine(context.FunctionAppDirectory, "Assets");

            var builder = new StringBuilder();

            var directoryContent = Directory.GetFiles(assetsPath);

            foreach (var file in directoryContent)
            {
                // log.LogInformation(file);
            }

            using (var engine = new TesseractEngine(assetsPath, "slk", EngineMode.Default))
            {
                using (var img = Pix.LoadFromFile(Path.Combine(assetsPath, "lindy_hop.jpg")))
                {
                    using (var page = engine.Process(img))
                    {
                        var text = page.GetText();
                        Console.WriteLine("Mean confidence: {0}", page.GetMeanConfidence());

                        Console.WriteLine("Text (GetText): \r\n{0}", text);
                        Console.WriteLine("Text (iterator):");
                        using (var iter = page.GetIterator())
                        {
                            iter.Begin();

                            do
                            {
                                do
                                {
                                    do
                                    {
                                        do
                                        {
                                            if (iter.IsAtBeginningOf(PageIteratorLevel.Block))
                                            {
                                                builder.Append("block</br>");
                                                Console.WriteLine("<BLOCK>");
                                            }

                                            var line = iter.GetText(PageIteratorLevel.Word);
                                            builder.Append(line);
                                            Console.Write(line);
                                            Console.Write(" ");

                                            if (iter.IsAtFinalOf(PageIteratorLevel.TextLine, PageIteratorLevel.Word))
                                            {
                                                builder.Append("</br>");
                                                Console.WriteLine();
                                            }
                                        } while (iter.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));

                                        if (iter.IsAtFinalOf(PageIteratorLevel.Para, PageIteratorLevel.TextLine))
                                        {
                                            builder.Append("</br>");
                                            Console.WriteLine();
                                        }
                                    } while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                                } while (iter.Next(PageIteratorLevel.Block, PageIteratorLevel.Para));
                            } while (iter.Next(PageIteratorLevel.Block));
                        }
                    }
                }
            }

            // log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? builder.ToString()
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
