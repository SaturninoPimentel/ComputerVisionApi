using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ComputerVisionAPI
{
    internal class Program
    {
        private static string key = "Key";
        private static string ComputerVisionUri = "https://theregion.api.cognitive.microsoft.com/vision/v1.0/";

        private static void Main(string[] args)
        {
            string path = @"thepath\IMG_20180617_185830_998.jpg";
            string pathPrintedText = @"thepath\IMG_20180711_000930.jpg";
            
            SendToCognitiveServiceWithoutSdkAsync(path);
            RecognizePrintedTextWithSdkAsync(pathPrintedText, true);
            GenerateThumbnailAsync(path, 250, 250, true);
            Console.ReadLine();
        }

        private static async void RecognizePrintedTextWithSdkAsync(string imagePath,
            bool detectOrientation)
        {
            ServiceClientCredentials serviceClientCredentials = new ApiKeyServiceClientCredentials(key);

            Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ComputerVisionAPI computerVisionApi =
                new Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ComputerVisionAPI(
                    serviceClientCredentials)
                {
                    AzureRegion = AzureRegions.Southcentralus
                };
            HttpOperationResponse<OcrResult> result;
            using (FileStream fileReader = new FileStream(imagePath, FileMode.Open))
            {
                result = await computerVisionApi.RecognizePrintedTextInStreamWithHttpMessagesAsync(detectOrientation, fileReader);
            }
            foreach (OcrRegion region in result.Body.Regions)
            {
                foreach (OcrLine line in region.Lines)
                {
                    foreach (OcrWord word in line.Words)
                    {
                        Console.WriteLine(word.Text);
                    }
                }
            }
        }

        private static async void GenerateThumbnailAsync(string imagePath, int width, int height, bool smart)
        {
            byte[] thumbnail = await GetThumbnailAsync(imagePath, width, height, smart);

            string thumbnailPath = $"{Path.GetDirectoryName(imagePath)}\\{Guid.NewGuid()}.jpg";

            using (BinaryWriter bw = new BinaryWriter(new FileStream(thumbnailPath, 
                FileMode.OpenOrCreate, 
                FileAccess.Write)))
            {
                bw.Write(thumbnail);
            }
        }

        private static async Task<byte[]> GetThumbnailAsync(string imagePath, int width, int height, bool smart)
        {
            string prefix = "generateThumbnail";
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-key", key);

            string requestParameters = $"width={width}&height={height}&smartCropping={true}";
            string uri = $"{ComputerVisionUri}{prefix}?{requestParameters}";

            byte[] byteData = GetImageAsByteArrays(imagePath);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                HttpResponseMessage responseMessage = await httpClient.PostAsync(uri, content);

                return await responseMessage.Content.ReadAsByteArrayAsync();
            }
        }

        private static async void SendToCognitiveServiceWithoutSdkAsync(string imagePath)
        {
            string prefix = "analyze";
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);

            string requestParameters = "visualFeatures=Categories,Description&language=es";
            string uri = $"{ComputerVisionUri}{prefix}?{requestParameters}";

            HttpResponseMessage response;
            byte[] byteData = GetImageAsByteArrays(imagePath);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await httpClient.PostAsync(uri, content);
            }
            string computerVisionResponse = await response.Content.ReadAsStringAsync();
            dynamic jsonObject = JsonConvert.DeserializeObject(computerVisionResponse);
            Console.WriteLine(JsonConvert.SerializeObject(jsonObject, Formatting.Indented));
        }

        private static byte[] GetImageAsByteArrays(string imagePath)
        {
            FileStream fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }
    }
}