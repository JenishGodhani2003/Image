using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace OCR.Controllers
{
    public class HomeController : Controller
    {

        // GET: Image/Index
        public ActionResult Index()
        {
            return View();
        }

        // POST: Image/Upload
        [HttpPost]
        public async Task<ActionResult> Upload(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                // Check if the file is an image
                if (file.ContentType.StartsWith("image"))
                {
                    using (var stream = file.InputStream)
                    {
                        byte[] imageData;
                        using (var memoryStream = new MemoryStream())
                        {
                            await stream.CopyToAsync(memoryStream);
                            imageData = memoryStream.ToArray();
                        }
                        var text = await PerformOCR(imageData);
                        ViewBag.Text = text;
                    }
                }
                else
                {
                    ViewBag.Error = "Uploaded file is not an image.";
                }
            }
            else
            {
                ViewBag.Error = "No file uploaded.";
            }

            return View("Index");
        }

        private async Task<string> PerformOCR(byte[] imageData)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var formData = new MultipartFormDataContent();
                    formData.Add(new ByteArrayContent(imageData), "image", "image.png");
                    formData.Add(new StringContent("K82419917288957"), "apikey");
                    formData.Add(new StringContent("true"), "isOverlayRequired"); // Enable overlay to get text details
                    formData.Add(new StringContent("eng"), "language"); // Set the language to English

                    var response = await client.PostAsync("https://api.ocr.space/parse/image", formData);
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    dynamic result = JObject.Parse(responseContent);

                    if (result.OCRExitCode == 1)
                    {
                        // Extract both horizontal and vertical text
                        string combinedText = ExtractText(result);
                        return combinedText;
                    }
                    else
                    {
                        return "Error: " + result.ErrorMessage[0];
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error processing image: " + ex.Message;
            }
        }

        private string ExtractText(dynamic result)
        {
            string combinedText = string.Empty;
            foreach (var parsedResult in result.ParsedResults)
            {
                var textOverlay = parsedResult.TextOverlay;
                if (textOverlay != null)
                {
                    foreach (var line in textOverlay.Lines)
                    {
                        foreach (var word in line.Words)
                        {
                            combinedText += word.WordText + " ";
                        }
                        combinedText += "\n";
                    }

                }
            }
            return combinedText.Trim();
        }  // cloan   conflict2
    }
}