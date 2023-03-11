using System;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;

namespace Menucko.Util.Html;

public class HtmlUtil : IHtmlUtil
{
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36";

    private IHttpClientFactory httpClientFactory;
    
    public HtmlUtil(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }
    
    public async Task<string> FetchDocument(string url)
    {
        using var client = httpClientFactory.CreateClient();

        var requestMsg = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
        requestMsg.Headers.Add("User-Agent", UserAgent);

        var responseMsg = await client.SendAsync(requestMsg);

        return await responseMsg.Content.ReadAsStringAsync();
    }
    
    public async Task<IDocument> ParseDocument(string rawHtml)
    {
        var config = Configuration.Default;

        var context = BrowsingContext.New(config);

        return await context.OpenAsync(req => req.Content(rawHtml));
    }
}