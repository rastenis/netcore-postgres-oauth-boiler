using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using netcore_postgres_oauth_boiler.Models;

namespace netcore_postgres_oauth_boiler.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // The original method is just 'return View()', this here is the data demo
            if (HttpContext.Session.GetString("user") == null)
            {
                return View();
            }

            // Strongly typed data pass to View
            var sampleData = new SampleDataListModel();
            sampleData.SampleList.Add(new SampleDataModel("Burger shop", "438 Durham Ave."));
            sampleData.SampleList.Add(new SampleDataModel("Hairdresser", "9489 Warren St."));
            sampleData.SampleList.Add(new SampleDataModel("Grocery store", "807 NW. Newport Ave."));

            // Weakly typed data pass to View
            var l = new string[] { "Milk","Eggs","Flour"};
            ViewData["data"] = String.Join(",", l);
            ViewData["dataTitle"] = "Shopping List:";

            return View(sampleData);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public new IActionResult NotFound()
        {
            return View();
        }
    }
}
