using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MonteCarlo_Shradha.Models
{
    public class ForecastedProduct
    {
        //Model used when forecasting an individual product

        //Pre-set values
        public string forecastDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt");
        public int iterations { get; set; }

        //Inputs & assumptions
        public string nrCode { get; set; }
        public int numberOfPrevMonths { get; set; }
        public int numberOfForecastMonths { get; set; }
        public Dictionary<string, int> initialLowerPercentages { get; set; }
        public Dictionary<string, int> initialUpperPercentages { get; set; }

        //Results
        public string productName { get; set; }
        public int monthBeforeBacktestingSales { get; set; }
        public int lastMonthSales { get; set; }
        public Dictionary<string, int> actualMonthlySales { get; set; }
        public Dictionary<string, int> predictedMonthlySales { get; set; }
        public Dictionary<string, int> meanSalesDifference { get; set; }
        public Dictionary<string, int> meanPercentageDifference { get; set; }
        public Dictionary<string, int> monthlyLowerBounds { get; set; }
        public Dictionary<string, int> monthlyUpperBounds { get; set; }
        public Dictionary<string, int> ninetyFiveLowerBounds { get; set; }
        public Dictionary<string, int> lowerSalesDifference { get; set; } //Sales & % Differences set for 95% bounds
        public Dictionary<string, int> lowerPercentageDifference { get; set; }
        public Dictionary<string, int> ninetyFiveUpperBounds { get; set; }
        public Dictionary<string, int> upperSalesDifference { get; set; }
        public Dictionary<string, int> upperPercentageDifference { get; set; }
        public Dictionary<string, int> totalPercentageDifferences { get; set; } //Mean, upper & lower bounds % differences

        //JSON string
        public string productJson { get; set; }
    }
}
