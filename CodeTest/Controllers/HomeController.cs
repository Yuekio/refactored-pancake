using System;
using System.Linq;
using System.Web.Mvc;
using Quote.Contracts;
using Quote.Models;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace PruebaIngreso.Controllers
{
    public class HomeController : Controller
    {
        private readonly IQuoteEngine quote;

        public HomeController(IQuoteEngine quote)
        {
            this.quote = quote;
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Test()
        {
            var request = new TourQuoteRequest
            {
                adults = 1,
                ArrivalDate = DateTime.Now.AddDays(1),
                DepartingDate = DateTime.Now.AddDays(2),
                getAllRates = true,
                GetQuotes = true,
                RetrieveOptions = new TourQuoteRequestOptions
                {
                    GetContracts = true,
                    GetCalculatedQuote = true,
                },
                TourCode = "E-U10-PRVPARKTRF",
                Language = Language.Spanish
            };

            try
            {
                var result = this.quote.Quote(request);
                var tour = result.Tours.FirstOrDefault() ?? new Tour();
                ViewBag.Message = "Test 1 Correcto";
                if (tour != null)
                    return View(tour);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return View(new Tour());
        }

        [Route("Home/Test2")]
        public ActionResult Test2()
        {
            ViewBag.Message = "Test 2 Correcto";
            return View();
        }

        public class MarginResponse
        {
            public decimal Margin { get; set; }
        }

        public static async Task<string> GetMargin(string code)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpClient client = new HttpClient();
            string url = String.Format("https://refactored-pancake.free.beeceptor.com/margin/{0}", code);
            string margin = "{ \"margin\": 0.0 }";

            try
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                if (response.StatusCode == HttpStatusCode.OK)
                    margin = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return margin;
        }

        public async Task<ActionResult> Test3()
        {
            string code = Uri.EscapeDataString("E-U10-DSCVCOVE 404");
            var json = await GetMargin(code);
            return Content(json, "application/json");
        }

        public class TourQuoteDecorator : IMarginProvider
        {
            public readonly TourQuote _tourQuote;

            public TourQuoteDecorator(TourQuote tourQuote)
            {
                _tourQuote = tourQuote;
            }
            public decimal margin => GetMargin(_tourQuote.ContractService.ServiceCode);

            public decimal GetMargin(string code)
            {
                var marginJson = "";
                Task.Run(async () =>
                {
                    marginJson = await HomeController.GetMargin(code);
                }).GetAwaiter().GetResult();

                var marginData = JsonConvert.DeserializeObject<MarginResponse>(marginJson);
                return marginData?.Margin ?? 0.0m;
            }
        }

        public async Task<ActionResult> Test4()
        {
            var request = new TourQuoteRequest
            {
                adults = 1,
                ArrivalDate = DateTime.Now.AddDays(1),
                DepartingDate = DateTime.Now.AddDays(2),
                getAllRates = true,
                GetQuotes = true,
                RetrieveOptions = new TourQuoteRequestOptions
                {
                    GetContracts = true,
                    GetCalculatedQuote = true,
                },
                Language = Language.Spanish
            };

            var result = this.quote.Quote(request);
            var decoratedQuotes = new List<TourQuoteDecorator>();

            foreach (var quote in result.TourQuotes)
            {
                var decorator = new TourQuoteDecorator(quote);
                decoratedQuotes.Add(decorator);
            }

            return View(decoratedQuotes);
        }
    }
}