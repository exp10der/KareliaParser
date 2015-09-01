using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using HtmlAgilityPack;

internal class Program
{
    private static void Main(string[] args)
    {
        MainAsync().Wait();
    }

    public static async Task MainAsync()
    {
        string html;

        using (var client = new HttpClient())
        {
            html = await client.GetStringAsync("http://market.karelia.pro/section/8/");
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var pageCount =
            int.Parse(HttpUtility.ParseQueryString(
                new Uri(doc.DocumentNode.SelectSingleNode(".//*[@id='paginator']/li[9]/a").GetAttributeValue("href", ""))
                    .Query
                ).Get("page")
                );

        var task = Enumerable.Range(1, pageCount)
            .Select((n, i) => $"http://market.karelia.pro/section/8/?page={i + 1}")
            .Select(
                async n =>
                {
                    using (var clien = new HttpClient())
                    {
                        return await clien.GetStringAsync(n);
                    }
                }).ToList();

        var results = await Task.WhenAll(task);


        var q = results.SelectMany(n =>
        {
            var innerdoc = new HtmlDocument();
            innerdoc.LoadHtml(n);

            var foo =
                innerdoc.DocumentNode.SelectNodes(".//*[@id='alist']/li")
                    .Select(x => new
                    {
                        link = x.SelectSingleNode("//div[@class='name']/a").Attributes["href"].Value,
                        title = x.SelectSingleNode("//div[@class='name']/a/span[@class='title']").InnerText,
                        price = x.SelectSingleNode("//div[@class='price']/strong/span").InnerText
                    });

            return foo;
        }).ToList();

        new XDocument(new XDeclaration("1.0", null, null),
            new XElement("root",
                q.Select(
                    n =>
                        new XElement("item", new XElement(nameof(n.link), n.link),
                            new XElement(nameof(n.price), n.price), new XElement(nameof(n.title), n.title))))).Save(
                                "result.xml");
    }
}