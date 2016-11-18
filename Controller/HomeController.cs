using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MonteCarlo_Shradha.Models;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Web.Script.Serialization;
using System.Web.UI;

namespace MonteCarlo_Shradha.Controllers
{
    public class HomeController : Controller
    {
        //New db context

        public ActionResult Index()
        {
            return View();
        }
        
        //Practice with anonymous types, greatly reduces db query time
        //List<string> allProducts = db.[table name].Where(m => (m.PurchaseDate.Year == DateTime.Today.Year && m.PurchaseDate.Month == (DateTime.Today.Month - 1))).GroupBy(p => p.SellerSKU).Select(s => new { Key = s.Key, Value = s.Count() }).Where(m => (m.Value > 0)).Select(d => d.Key).ToList(); //Use anonymous dictionary type in select to grab more than one value from group, convert from dictionary to list of NRCodes

        [HttpPost]
        public ActionResult Forecast(string accountCode = "", string accountNumber = "", string nrCode = "", int numberOfPrevMonths = 12, string backtestingCustomBoundPercentages = "", int numberOfForecastMonths = 12, string forecastingCustomBoundPercentages = "") //Set default values for optional parameters
        {
            //Set preliminary values
            int trials = 5000;
            string customBoundPercentages = backtestingCustomBoundPercentages + forecastingCustomBoundPercentages;
            if (customBoundPercentages == "") //Arbitrary percentages in case custom ones aren't inputted: 10% variance
            {
                customBoundPercentages = "0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010, 0010";
            }

            //Initialize account variables & properties so accessible later
            string accurateAccountCode = "";
            string accurateAccountNumber = "";
            List<string> brandProducts = new List<string>();
            string brandName = "";

            //List all products to run a Monte Carlo simulation for
            Dictionary<string, List<int>> productsToForecast = new Dictionary<string, List<int>>();
            DateTime beginningOfCurrentMonth = DateTime.Now.ToStartOfMonth(); //ToStartOfMonth helper from Common
            DateTime monthBeforeBacktesting = DateTime.Now.AddMonths(-(numberOfPrevMonths + 1)).ToStartOfMonth(); //Cut off month for backtesting

            if (nrCode == "") //Forecasting for brand account
            {
                //Remove extra spaces from accountCode & accountNumber
                accurateAccountCode = Regex.Replace(accountCode, @"\s+", "");
                accurateAccountNumber = Regex.Replace(accountNumber, @"\s+", "");

                //Query all active products under brand account & get brand name
                brandProducts = //SalesForce call to list all NRCodes for active products in an account
                brandName = //SalesForce call to get brand/account name

                //Only forecast for brand products that have at least 13 months or (numberOfPreviousMonths + 1 month) of previous sales
                for (var i = 0; i < brandProducts.Count; i ++)
                {
                    string NRCode = brandProducts[i];

                    List<int> saleSummaries = db.[table name]
                        .Where(m => m.SellerSKU == NRCode && m.LatestPurchaseDate >= monthBeforeBacktesting && m.LatestPurchaseDate < beginningOfCurrentMonth)
                        .OrderBy(m => m.SummaryKey)
                        .Select(m => m.QuantityShipped)
                        .ToList();

                    //Find if enough summaries are available
                    if (saleSummaries.Count == (numberOfPrevMonths + 1))
                    {
                        //Confirm that all sales values are > 0, some products counted 0 sales for a month instead of omitting entire month summary
                        List<bool> enoughSales = new List<bool>();
                        for (var s = 0; s < saleSummaries.Count; s ++)
                        {
                            if (saleSummaries[s] > 0)
                            {
                                enoughSales.Add(true);
                            }
                        }

                        if (enoughSales.Count == (numberOfPrevMonths + 1))
                        {
                            productsToForecast.Add(NRCode, saleSummaries);
                        }
                        else
                        {
                            return View("Error"); //Temp error page
                        }
                    }
                    else
                    {
                        return View("Error");
                    }
                }
            }
            else //Forecasting for individual product
            {
                //Remove extra spaces from NR code
                string accurateNRCode = Regex.Replace(nrCode, @"\s+", "");

                List<int> saleSummaries = db.[table name]
                    .Where(m => m.SellerSKU == accurateNRCode && m.LatestPurchaseDate >= monthBeforeBacktesting && m.LatestPurchaseDate < beginningOfCurrentMonth)
                    .OrderBy(m => m.SummaryKey)
                    .Select(m => m.QuantityShipped)
                    .ToList();

                if (saleSummaries.Count == (numberOfPrevMonths + 1))
                {
                    List<bool> enoughSales = new List<bool>();
                    for (var s = 0; s < saleSummaries.Count; s++)
                    {
                        if (saleSummaries[s] > 0)
                        {
                            enoughSales.Add(true);
                        }
                    }

                    if (enoughSales.Count == (numberOfPrevMonths + 1))
                    {
                        productsToForecast.Add(accurateNRCode, saleSummaries);
                    }
                    else
                    {
                        return View("Error"); //Temp error page
                    }
                }
                else
                {
                    return View("Error");
                }
            }

            //Remove empty spaces & non-alphanumerical values from custom bound percentages to get string of numbers
            string separatedCustomBounds = Regex.Replace(customBoundPercentages, "[^0-9a-zA-Z]+", ""); //Note: Regex gets rid of horizontal dashed (-) so adjust if anticipating -% inputs

            //Instantiate custom lower & upper percentage lists for numerical values
            List<int> lowerCustomPercentages = new List<int>();
            List<int> upperCustomPercentages = new List<int>();

            //Loop through each char in separated custom bounds string to compile percentages
            for (var i = 0; i < separatedCustomBounds.Length; i += 4) //Lower bounds
            {
                string bound = separatedCustomBounds[i].ToString() + separatedCustomBounds[i + 1].ToString(); //Two sequential chars create one percentage(ex. string = "101520", 1st % = 10)
                lowerCustomPercentages.Add(Convert.ToInt32(bound));
            }
            for (var i = 2; i <separatedCustomBounds.Length; i += 4) //Upper bounds
            {
                string bound = separatedCustomBounds[i].ToString() + separatedCustomBounds[i + 1].ToString();
                upperCustomPercentages.Add(Convert.ToInt32(bound));
            }

            //Instantiate months lists
            List<string> monthNames = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "June", "July", "Aug", "Sep", "Oct", "Nov", "Dec" }; //string list of month names for creating full date
            List<string> months = new List<string>(); //Full date list (month name + year)

            //Create full month name & year list for forecast
            //Add previous month names
            for (var i = (DateTime.Today.Month - numberOfPrevMonths); i < DateTime.Today.Month; i++)
            {
                if (i <= -12 && i > -24) //If previous months are from 2 years ago
                {
                    string fullMonth = monthNames[i + 23] + " " + DateTime.Today.AddYears(-2).Year;
                    months.Add(fullMonth);
                }
                else if (i <= 0 && i > -12) //If previous months are in last year
                {
                    string fullMonth = monthNames[i + 11] + " " + DateTime.Today.AddYears(-1).Year;
                    months.Add(fullMonth);
                }
                else if (i > 0) //If all previous months are in current year
                {
                    string fullMonth = monthNames[i - 1] + " " + DateTime.Today.Year;
                    months.Add(fullMonth);
                }
            }

            //Add forecast month names
            for (var i = DateTime.Today.Month; i < (DateTime.Today.Month + numberOfForecastMonths); i++)
            {
                if (i > 12) //If forecast months are in next year
                {
                    string fullMonth = monthNames[i - 13] + " " + DateTime.Today.AddYears(1).Year;
                    months.Add(fullMonth);
                }
                else //If forecast months are in current year
                {
                    string fullMonth = monthNames[i - 1] + " " + DateTime.Today.Year;
                    months.Add(fullMonth);
                }
            }

            //Instantiate new model properties for assumptions
            Dictionary<string, int> initialLowerPercentages = new Dictionary<string, int>() { };
            Dictionary<string, int> initialUpperPercentages = new Dictionary<string, int>() { };

            //Add initial lower & upper bounds to corresponding months & to dictionaries
            for (var i = 0; i < (numberOfPrevMonths + numberOfForecastMonths); i ++)
            {
                initialLowerPercentages.Add(months[i], lowerCustomPercentages[i]);
                initialUpperPercentages.Add(months[i], upperCustomPercentages[i]);
            }

            //Instantiate new account model properties
            List<Product> productsList = new List<Product>(); //List of all product results for an account forecast

            //Instantiate total % differences lists (used to calculate average % differences for entire account)
            List<int> lowerTotalPercDifferences = new List<int>();
            List<int> meanTotalPercDifferences = new List<int>();
            List<int> upperTotalPercDifferences = new List<int>();

            //Loop through all products to forecast (even when only forecasting a single product)
            for (var ii = 0; ii < productsToForecast.Count; ii ++)
            {
                var currentProduct = productsToForecast.ElementAt(ii); //Note: ElementAt() allows retrieval of dictionary values
                string NRCode = currentProduct.Key; //Note: Can't directly reference list i value in DB query

                //Query db for product name
                string productName = db.[table name].FirstOrDefault(m => (m.SellerSKU == NRCode)).Title;

                //Set last month's product sales from previous db call
                List<int> pastMonthlyTotals = currentProduct.Value.ToList();
                int monthBeforeBacktestingSales = pastMonthlyTotals[0]; //Starting number for back testing
                pastMonthlyTotals.Remove(pastMonthlyTotals[0]); //Extra total sale not needed anymore

                //Set monthly total for future forecasting - last month's total sales
                int lastMonthSales = pastMonthlyTotals[pastMonthlyTotals.Count - 1];

                //Initialize predicted sales, lower & upper bounds lists
                List<int> predictedMonthlyMeans = new List<int>();
                List<int> lowerBounds = new List<int>();
                List<int> upperBounds = new List<int>();
                List<int> ninetyFiveUpper = new List<int>();
                List<int> ninetyFiveLower = new List<int>();

                //Monte Carlo method for predicted sales
                for (var i = 0; i < (pastMonthlyTotals.Count + numberOfForecastMonths); i ++)
                {
                    int startingNumber = monthBeforeBacktestingSales; //Set backtesting starting sales total

                    if (i == pastMonthlyTotals.Count) //Once all previous months are backtested, set new startingNumber for future forecasts
                    {
                        startingNumber = lastMonthSales;
                    }
                    else if (i > 0) //All other months draw from previous predicted total sale, not previous actual sale like when starting backtesting or forecasting
                    {
                        startingNumber = predictedMonthlyMeans[i - 1];
                    }

                    //Initialize random object, total sales & sales list of random normal trials
                    Random rnd = new Random();
                    int cumulativeTotalSales = 0;
                    List<int> allSales = new List<int>();

                    //Loop through trials
                    for (var j = 0; j < trials; j ++)
                    {
                        //Create two random numbers to create random normal
                        double rand1 = rnd.NextDouble();
                        double rand2 = rnd.NextDouble();

                        //Box-Mueller transform to create a random normal w/ mean = 0 & stDev = 1
                        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(rand1)) * Math.Sin(2.0 * Math.PI * rand2);
                        double randNormal = 0;

                        //Set random normal w/mean & custom percentages
                        int lowerLimit = (int)((1 - (lowerCustomPercentages[i] / 100.0)) * startingNumber);
                        int upperLimit = (int)((1 + (upperCustomPercentages[i] / 100.0)) * startingNumber);

                        //Find random trial sales using random normal
                        if (randStdNormal < 0)
                        {
                            randNormal = startingNumber + ((startingNumber - lowerLimit) * randStdNormal);
                        }
                        else
                        {
                            randNormal = startingNumber + ((upperLimit - startingNumber) * randStdNormal);
                        }

                        //Add random normally selected sale to cumulative total (how mean is calculated) & sales list (how bounds are derived)
                        int trialSales = Convert.ToInt32(randNormal);
                        cumulativeTotalSales += trialSales;
                        allSales.Add(trialSales);
                    }
                    //Add predicted monthly means based on custom percentages
                    predictedMonthlyMeans.Add(cumulativeTotalSales / trials);

                    //Sort sales list in ascending order to easily get first & third quartile for 25% bounds
                    allSales.Sort();

                    //Convert trials to JSON string
                    string iterationsJson = JsonConvert.SerializeObject(allSales);

                    //Find 25% bounds & add to lists
                    double trialsDouble = Convert.ToDouble(trials);
                    lowerBounds.Add(allSales[Convert.ToInt32(trialsDouble * 0.25)]); //First quartile
                    upperBounds.Add(allSales[Convert.ToInt32(trialsDouble * 0.75)]); //Third quartile

                    //Find & add 95% bounds to lists
                    ninetyFiveLower.Add(allSales[trials / 40]);
                    ninetyFiveUpper.Add(allSales[(39 * trials) / 40]);
                }
                //Calculate sales difference for each month (actual - predicted)
                List<int> salesDifference = new List<int>();
                List<int> lowerDifference = new List<int>(); 
                List<int> upperDifference = new List<int>();

                for (var i = 0; i < pastMonthlyTotals.Count; i++)
                {
                    salesDifference.Add(pastMonthlyTotals[i] - predictedMonthlyMeans[i]);
                    lowerDifference.Add(pastMonthlyTotals[i] - ninetyFiveLower[i]); //Upper & lower based on 95% bounds
                    upperDifference.Add(pastMonthlyTotals[i] - ninetyFiveUpper[i]);
                }

                //Calculate % difference for each month ((actual - predicted)/actual) * 100
                List<int> percDifference = new List<int>();
                List<int> lowerPercDifference = new List<int>();
                List<int> upperPercDifference = new List<int>();

                for (var i = 0; i < salesDifference.Count(); i++)
                {
                    double difference = (Convert.ToDouble(salesDifference[i]) / Convert.ToDouble(pastMonthlyTotals[i])) * 100;
                    percDifference.Add(Convert.ToInt32(difference));

                    double lDifference = (Convert.ToDouble(lowerDifference[i]) / Convert.ToDouble(pastMonthlyTotals[i])) * 100;
                    lowerPercDifference.Add(Convert.ToInt32(lDifference)); //Upper & lower based on 95% bounds

                    double uDifference = (Convert.ToDouble(upperDifference[i]) / Convert.ToDouble(pastMonthlyTotals[i])) * 100;
                    upperPercDifference.Add(Convert.ToInt32(uDifference));
                }

                //Instantiate new product model properties
                Dictionary<string, int> actualMonthlySales = new Dictionary<string, int>() { };
                Dictionary<string, int> predictedMonthlySales = new Dictionary<string, int>() { };
                Dictionary<string, int> meanSalesDifference = new Dictionary<string, int>() { };
                Dictionary<string, int> meanPercentageDifference = new Dictionary<string, int>() { };
                Dictionary<string, int> monthlyLowerBounds = new Dictionary<string, int>() { };
                Dictionary<string, int> monthlyUpperBounds = new Dictionary<string, int>() { };

                //Sales difference & % differences for 95% bounds
                Dictionary<string, int> ninetyFiveLowerBounds = new Dictionary<string, int>() { };
                Dictionary<string, int> lowerSalesDifference = new Dictionary<string, int>() { };
                Dictionary<string, int> lowerPercentageDifference = new Dictionary<string, int>() { };

                Dictionary<string, int> ninetyFiveUpperBounds = new Dictionary<string, int>() { };
                Dictionary<string, int> upperSalesDifference = new Dictionary<string, int>() { };
                Dictionary<string, int> upperPercentageDifference = new Dictionary<string, int>() { };

                //Dictionary of all total % differences
                Dictionary<string, int> totalPercentageDifferences = new Dictionary<string, int>() { };

                //Add actual monthly sales & sales/% differences to corresponding dictionaries (only calculated for backtested months)
                for (var i = 0; i < pastMonthlyTotals.Count; i ++)
                {
                    actualMonthlySales.Add(months[i], pastMonthlyTotals[i]);
                    meanSalesDifference.Add(months[i], salesDifference[i]);
                    meanPercentageDifference.Add(months[i], percDifference[i]);
                    lowerSalesDifference.Add(months[i], lowerDifference[i]);
                    lowerPercentageDifference.Add(months[i], lowerPercDifference[i]);
                    upperSalesDifference.Add(months[i], upperDifference[i]);
                    upperPercentageDifference.Add(months[i], upperPercDifference[i]);
                }

                //Add predicted monthly sales & monthly bounds to model dictionaries (calculated for all months - previous & future)
                for (var i = 0; i < (pastMonthlyTotals.Count + numberOfForecastMonths); i++)
                {
                    predictedMonthlySales.Add(months[i], predictedMonthlyMeans[i]);

                    monthlyLowerBounds.Add(months[i], lowerBounds[i]);
                    monthlyUpperBounds.Add(months[i], upperBounds[i]);

                    ninetyFiveUpperBounds.Add(months[i], ninetyFiveUpper[i]);
                    ninetyFiveLowerBounds.Add(months[i], ninetyFiveLower[i]);
                }

                //Generate entire product's % sales difference
                double meanTotalDifference = 0;
                double lowerTotalDifference = 0;
                double upperTotalDifference = 0;

                for (var i = 0; i < salesDifference.Count; i ++) //Total sales difference in units, not %
                {
                    meanTotalDifference += salesDifference[i];
                    lowerTotalDifference += lowerDifference[i];
                    upperTotalDifference += upperDifference[i];
                }

                double pastTotal = 0;
                for (var i = 1; i < pastMonthlyTotals.Count; i ++) //Total actual sales for all previous months
                {
                    pastTotal += pastMonthlyTotals[i];
                }
                //(sum of (actual - predicted))/(sum of actual)
                int meanAvPercDifference = Convert.ToInt32((meanTotalDifference / pastTotal) * 100);
                int lowerAvPercDifference = Convert.ToInt32((lowerTotalDifference / pastTotal) * 100);
                int upperAvPercDifference = Convert.ToInt32((upperTotalDifference / pastTotal) * 100);

                //Add total % differences to model dictionary
                totalPercentageDifferences.Add("(Mean) Average % Difference", meanAvPercDifference);
                totalPercentageDifferences.Add("(Lower) Average % Difference", lowerAvPercDifference);
                totalPercentageDifferences.Add("(Upper) Average % Difference", upperAvPercDifference);

                if (nrCode == "") //Account forecast
                {
                    //Set derived values to new product model
                    Product newProduct = new Product { nrCode = NRCode, productName = productName, monthBeforeBacktestingSales = monthBeforeBacktestingSales, lastMonthSales = lastMonthSales, actualMonthlySales = actualMonthlySales, predictedMonthlySales = predictedMonthlySales, meanSalesDifference = meanSalesDifference, meanPercentageDifference = meanPercentageDifference, monthlyLowerBounds = monthlyLowerBounds, monthlyUpperBounds = monthlyUpperBounds, ninetyFiveLowerBounds = ninetyFiveLowerBounds, lowerSalesDifference = lowerSalesDifference, lowerPercentageDifference = lowerPercentageDifference, ninetyFiveUpperBounds = ninetyFiveUpperBounds, upperSalesDifference = upperSalesDifference, upperPercentageDifference = upperPercentageDifference, totalPercentageDifferences = totalPercentageDifferences };

                    productsList.Add(newProduct); //Add new product to account products list

                    //Add total % differences to list for average calcs
                    lowerTotalPercDifferences.Add(lowerAvPercDifference);
                    meanTotalPercDifferences.Add(meanAvPercDifference);
                    upperTotalPercDifferences.Add(upperAvPercDifference);

                    //Convert product model to JSON string
                    string productJson = JsonConvert.SerializeObject(newProduct);
                }
                else //Individual product forecast (end program)
                {
                    //Set derived values to new forecasted product model
                    //ForecastedProduct model is different from Product model as it holds the assumptions from the query/forecast
                    ForecastedProduct newForecastedProduct = new ForecastedProduct { nrCode = NRCode, productName = productName, iterations = trials, numberOfPrevMonths = numberOfPrevMonths, numberOfForecastMonths = numberOfForecastMonths, initialLowerPercentages = initialLowerPercentages, initialUpperPercentages = initialUpperPercentages, monthBeforeBacktestingSales = monthBeforeBacktestingSales, lastMonthSales = lastMonthSales, actualMonthlySales = actualMonthlySales, predictedMonthlySales = predictedMonthlySales, meanSalesDifference = meanSalesDifference, meanPercentageDifference = meanPercentageDifference, monthlyLowerBounds = monthlyLowerBounds, monthlyUpperBounds = monthlyUpperBounds, ninetyFiveLowerBounds = ninetyFiveLowerBounds, lowerSalesDifference = lowerSalesDifference, lowerPercentageDifference = lowerPercentageDifference, ninetyFiveUpperBounds = ninetyFiveUpperBounds, upperSalesDifference = upperSalesDifference, upperPercentageDifference = upperPercentageDifference, totalPercentageDifferences = totalPercentageDifferences };

                    //Convert product model to JSON string
                    string forecastedProductJson = JsonConvert.SerializeObject(newForecastedProduct);
                    newForecastedProduct.productJson = forecastedProductJson; //Add JSON string to model

                    return View("ProductForecast", newForecastedProduct); //Pass forecasted product model to respective view
                }
            }
            //Calc total average % difference between all products in brand
            int totalMeanPercDifference = 0;
            int totalLowerPercDifference = 0;
            int totalUpperPercDifference = 0;

            for (var i = 0; i < meanTotalPercDifferences.Count; i ++)
            {
                totalMeanPercDifference += (int)Math.Abs(meanTotalPercDifferences[i]);
                totalLowerPercDifference += (int)Math.Abs(lowerTotalPercDifferences[i]);
                totalUpperPercDifference += (int)Math.Abs(upperTotalPercDifferences[i]);
            }
            //(sum of % differences)/(number of products)
            totalMeanPercDifference = (totalMeanPercDifference / productsToForecast.Count);
            totalLowerPercDifference = (totalLowerPercDifference / productsToForecast.Count);
            totalUpperPercDifference = (totalUpperPercDifference / productsToForecast.Count);

            //Set derived values to new account model
            ForecastedAccount newForecastedAccount = new ForecastedAccount { accountCode = accurateAccountCode, accountNumber = accurateAccountNumber, brandName = brandName, iterations = trials, numberOfPrevMonths = numberOfPrevMonths, numberOfForecastMonths = numberOfForecastMonths, initialLowerPercentages = initialLowerPercentages, initialUpperPercentages = initialUpperPercentages, numberOfProducts = productsList.Count, productsList = productsList, totalMeanPercDifference = totalMeanPercDifference, totalLowerPercDifference = totalLowerPercDifference, totalUpperPercDifference = totalUpperPercDifference };

            //Convert account model to JSON string
            string forecastedAccountJson = JsonConvert.SerializeObject(newForecastedAccount);
            newForecastedAccount.accountJson = forecastedAccountJson; //Add JSON string to model

            return View("AccountForecast", newForecastedAccount); //Pass forecasted account model to respective view
        }
    }
}
