using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace AltinKaynakScraper
{
    class Program
    {
        private readonly string url;

        public Program()
        {
            this.url = "http://www.altinkaynak.com/Doviz/Kur/Guncel";
        }


        static void Main(string[] args)
        {
            try
            {
                Program p = new Program();
                p.Run();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }

        private void Run()
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new Exception("Url can not be empty");
            }

            var content = Download();

            HtmlDocument doc = new HtmlDocument();
            //doc.OptionAutoCloseOnEnd = true;
            doc.Load(content.Result);

            HtmlNode node = doc.DocumentNode.SelectSingleNode("//*[@id=\"cphMain_cphSubContent_dLastTime\"]");
            if (node == null)
            {
                throw new Exception("Last update time node not found or empty");
            }

            var lastUpdateDate = node.SelectSingleNode("//*[@class=\"date\"]");

            if (lastUpdateDate == null)
            {
                throw new Exception("Date node not found or empty");
            }

            var lastUpdateTime = node.SelectSingleNode("//*[@class=\"time\"]");

            if (lastUpdateTime == null)
            {
                throw new Exception("Time node not found or empty");
            }

            var date = lastUpdateDate.InnerText.Split(".");
            if (date.Length != 3)
            {
                throw new Exception("Date format not as expected");
            }

            var normalizedDate = string.Format("{0}-{1}-{2}", date[2], date[1].Length == 1 ? "0"+date[1] : date[1] , date[0]);
            Console.WriteLine($"Normalized Date: {normalizedDate}");

            var dateTime = normalizedDate + " " + lastUpdateTime.InnerText;

            DateTime oDate = DateTime.Parse(dateTime);

            Console.WriteLine($"Date: {oDate.ToString()}");

            HtmlNode currencyNode = doc.DocumentNode.SelectSingleNode("//table[@class=\"table\"]");

            if (currencyNode == null)
            {
                throw new Exception("Currency table not found");
            }

            var currencyListBody = currencyNode.SelectSingleNode("//tbody");
            if(currencyListBody == null)
            {
                throw new Exception("Body tag of currency list not found");
            }

            var currencyLists = currencyListBody.SelectNodes("//tr[@data-flag]");

            if (currencyLists == null)
            {
                throw new Exception("Currency body is empty or changed");
            }

            foreach (var currencyList in currencyLists)
            {
                var currencyName = currencyList.Attributes["data-flag"];

                var currencyBuy = currencyList.SelectSingleNode("//td[@id=\"td" + currencyName.Value.Trim() + "Buy\"]").InnerText;
                var currencySell = currencyList.SelectSingleNode("//td[@id=\"td" + currencyName.Value.Trim() + "Sell\"]").InnerText;

                int pos = currencyBuy.IndexOf(currencySell, StringComparison.CurrentCulture);
                if (pos >= 0)
                {
                    currencyBuy = currencyBuy.Remove(pos, currencySell.Length);
                }

                //currencyBuy.Replace(currencySell, "");

                Console.WriteLine($"{currencyName.Value.Trim()} Buy: {currencyBuy} Sell: {currencySell}");
            }

        }

        private async Task<Stream> Download()
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);

            if(!response.IsSuccessStatusCode)
            {
                throw new Exception($"Status code is not ok: {response.StatusCode}");
            }

            Stream stream = await response.Content.ReadAsStreamAsync();

            return stream;
        }
    }
}
