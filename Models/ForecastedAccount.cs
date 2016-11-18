using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MonteCarlo_Shradha.Models
{
    public class ForecastedAccount
    {
        //Pre-set values
        public string forecastDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt");
        public int iterations { get; set; }
        
        //Inputs & assumptions
        public string accountCode { get; set; }
        public string accountNumber { get; set; }
        public int numberOfPrevMonths { get; set; }
        public int numberOfForecastMonths { get; set; }
        public Dictionary<string, int> initialLowerPercentages { get; set; }
        public Dictionary<string, int> initialUpperPercentages { get; set; }

        //Results
        public string brandName { get; set; }
        public int numberOfProducts { get; set; }
        public List<Product> productsList { get; set; }
        public int totalMeanPercDifference { get; set; }
        public int totalLowerPercDifference { get; set; }
        public int totalUpperPercDifference { get; set; }

        //JSON string
        public string accountJson { get; set; }
    }
}
