using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using AutoGen;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;



class Program
{

	public static async Task Main(string[] args)
	{

		if (args.Length == 0)
		{
			Console.WriteLine("Usage: UrlToEmailTemplate <URL>");
			return;
		}
		var analyzer = new WebpageAnalyzer();
		await analyzer.AnalyzeWebpage(args[0]);
	}
}

public class WebpageAnalyzer
{
	private static readonly HttpClient httpClient = new HttpClient();
	private static readonly HtmlDocument htmlDoc = new HtmlDocument();
	private static string htmlContent = "";
	private static string logoUrl = "";
	private static List<string> primaryColors = new List<string>();
	private static string primaryColor = "";
	private static string title = "";

	//openAIKey
	private static string openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

	public async Task AnalyzeWebpage(string url)
	{
		if (string.IsNullOrEmpty(openAIKey))
		{
			Console.WriteLine("Please set the OPENAI_API_KEY environment variable.");
			return;
		}

		await FetchHtmlContent(url);
		await GetLogoUrl();
		await FindPrimaryColors();


		Console.WriteLine();
	}

	async Task<string> FetchHtmlContent(string url)
	{
		using var httpClient = new HttpClient();
		return await httpClient.GetStringAsync(url);
	}

	public async Task<string> GetLogoUrl()
	{
		var doc = new HtmlDocument();
		doc.LoadHtml(htmlContent);

		// Find the logo based on common attributes like 'logo' in class or id
		var logoNode = doc.DocumentNode
			.SelectSingleNode("//img[contains(@class, 'logo') or contains(@id, 'logo')]");

		logoUrl = logoNode?.Attributes["src"]?.Value;
		return await Task.FromResult(logoUrl ?? "");
	}

	private async Task<List<string>> FindPrimaryColors()
	{
		var imageStream = await httpClient.GetStreamAsync(logoUrl);
		using (var image = Image.Load<Rgba32>(imageStream))
		{
			// Resize image to reduce processing time
			image.Mutate(x => x.Resize(new ResizeOptions
			{
				Size = new Size(100, 100),
				Mode = ResizeMode.Max
			}));

			var colorDict = new Dictionary<Rgba32, int>();

			// Count the occurrences of each color in the image
			for (int y = 0; y < image.Height; y++)
			{
				for (int x = 0; x < image.Width; x++)
				{
					var pixelColor = image[x, y];
					if (colorDict.ContainsKey(pixelColor))
					{
						colorDict[pixelColor]++;
					}
					else
					{
						colorDict[pixelColor] = 1;
					}
				}
			}

			// Sort colors by frequency
			var sortedColors = colorDict.OrderByDescending(c => c.Value).Take(5).Select(c => c.Key).ToList();

			// Convert colors to hex strings
			primaryColors = sortedColors.Select(c => $"#{c.R:X2}{c.G:X2}{c.B:X2}").ToList();


			return primaryColors;
		}
	}
}

