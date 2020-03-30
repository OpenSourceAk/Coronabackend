using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace CoronaVirus.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
       
        private IMemoryCache _memoryCache;

        public StatisticsController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        [HttpGet]
        public Statistic Get ()
        {

            return GetStatistic();
        }

        private Statistic GetStatistic()
        {
            Statistic statistic = _memoryCache.Get<Statistic>("statistic"); 
            if (statistic == null || statistic.LastUpdatedUTC < DateTime.UtcNow.AddHours(-2) )
            {
                string confirmed = "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_covid19_confirmed_global.csv";
                string deaths = "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_covid19_deaths_global.csv";
                string healed = "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_19-covid-Recovered.csv";

                statistic = new Statistic();
                statistic.Confirmed = GetDayItems(confirmed);
                statistic.Deaths = GetDayItems(deaths);
                statistic.Healed = new[] { new DayItem { Count = 0, Date = "" } };
                _memoryCache.Set<Statistic>("statistic", statistic);
                return statistic;
            }

            return statistic;
        }

        private IEnumerable<DayItem> GetDayItems (string fileUrl )
        {
            using (WebClient webclient = new WebClient())
            {
                var fileText = webclient.DownloadString(fileUrl);
                using (CsvHelper.CsvReader csvReader = new CsvReader(new StreamReader(GenerateStreamFromString(fileText)), new CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture)))
                {

                    while (csvReader.Read())
                    {
                        var record = csvReader.GetRecord<dynamic>();
                        if (csvReader.GetField(1) == "Iraq")
                        {
                            foreach (var property in (record as IDictionary<string, object>).Skip(36))
                            {
                        yield     return    (new DayItem { Date = property.Key, Count = int.Parse(property.Value.ToString()) });
                            }
                        }
                    }
                }
            }
        }
        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
   


    public class Statistic
    {
        public Statistic()
        {
            Confirmed = new List<DayItem>();
            Deaths = new List<DayItem>();
            Healed = new List<DayItem>();
            LastUpdatedUTC = DateTime.UtcNow;
        }
        public IEnumerable<DayItem> Confirmed { get; set; }
        public IEnumerable<DayItem> Deaths { get; set; }
        public IEnumerable<DayItem> Healed { get; set; }
        public DateTime LastUpdatedUTC { get; }
    }

    public class DayItem
    {
        public string Date { get; set; }
        public int Count  { get; set; }

    }
}