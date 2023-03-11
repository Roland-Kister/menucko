using System.Threading.Tasks;
using AngleSharp.Dom;

namespace Menucko.Util.Html;

public interface IHtmlUtil
{
    public Task<string> FetchDocument(string url);

    public Task<IDocument> ParseDocument(string rawHtml);
}