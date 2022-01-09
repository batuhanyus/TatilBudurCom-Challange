using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Challenge1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting...");
            SalesDataHelper salesData = new SalesDataHelper();
            Console.WriteLine("Getting data...");
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var items = salesData.GetStreamingSales(SAMPLE_SIZE);

            var summaryItems = Challenge1Solution(items);

            SaveToDatabase(summaryItems);

            stopwatch.Stop();
            Console.WriteLine($"Processing sales data completed. Total time: {stopwatch.Elapsed.TotalSeconds} seconds.");
            Console.ReadKey();
        }

        private const int SAMPLE_SIZE = 10000000;
        private static IEnumerable<SummarizedSalesData> Challenge1Solution(IEnumerable<SalesData> items)
        {
            //*****README*****
            //Greetings.
            //Code itself is pretty simple and self explanatory.
            //Main logic revolves around "theCode".
            //"theCode" is an unique-ish identifier. It groups sales data on date,brand,company,store and product basis.
            //Thus, it allows to aggregate volume and price data in a group.
            //theCode holds group data in a numeric way with 9 digits. Structure of digits as follows:
            // YEAR'SLASTDIGIT-WEEK-WEEK-BRANDCODESINDEX-COMPANYID-STORECODESINDEX-STORECODESINDEX-PRODUCTID-PRODUCTID
            //Solution is in single-threaded format, unsuprisingly multi-threaded approach didn't yield results.
            //This is because of the data-access-heavy nature of the solution. More CPU time doesn't help.
            //One way to improve the processing time is using arrays instead of dictionaries and lists.
            //However, doing so increased RAM usage tremondously and I choose to go with dictionaries.
            //*****README*****

            Dictionary<int, SummarizedSalesData> summaryHolder = new();

            List<string> brandCodes = new List<string>();
            List<string> storeCodes = new List<string>();

            //To parse data's week data we can in-built parsing method.
            CultureInfo ci = new CultureInfo("en-UK");
            Calendar calendar = ci.Calendar;
            CalendarWeekRule calendarWeekRule = ci.DateTimeFormat.CalendarWeekRule;
            DayOfWeek firstDayOfWeek = ci.DateTimeFormat.FirstDayOfWeek;

            foreach (var salesData in items)
            {
                //Parse the SalesDate to requested format.
                DateTime dt = DateTime.ParseExact(salesData.SalesDate, "yyyy-MM-dd", ci);
                int weekNumber = dt.Year * 100; //Ex: 202100
                weekNumber += calendar.GetWeekOfYear(dt, calendarWeekRule, firstDayOfWeek);

                //Check if brand and store codes have their theCode parts. If not, create.
                if (!brandCodes.Contains(salesData.BrandId))
                    brandCodes.Add(salesData.BrandId);
                if (!storeCodes.Contains(salesData.StoreId))
                    storeCodes.Add(salesData.StoreId);

                //Create the "theCode".
                int theCode = 0;
                theCode += (dt.Year - 2020) * 100000000;
                theCode += calendar.GetWeekOfYear(dt, calendarWeekRule, firstDayOfWeek) * 1000000;
                theCode += brandCodes.IndexOf(salesData.BrandId) * 100000;
                theCode += salesData.CompanyId * 10000;
                theCode += storeCodes.IndexOf(salesData.StoreId) * 100;
                theCode += salesData.ProductId;

                //Check if we have an entry for theCode, if not create.
                if (!summaryHolder.ContainsKey(theCode))
                {
                    SummarizedSalesData ssd = new();

                    //As we lack a ctor, let's do it in the old way.
                    ssd.ProductId = salesData.ProductId;
                    ssd.CompanyId = salesData.CompanyId;
                    ssd.BrandId = salesData.BrandId;
                    ssd.StoreId = salesData.StoreId;
                    ssd.WeekNumber = weekNumber;

                    summaryHolder.Add(theCode, ssd);
                }

                //Now as we have the required record in the dictionary, we can update volumes and prices.
                summaryHolder[theCode].TotalVolume += salesData.Volume;
                summaryHolder[theCode].TotalPrice += salesData.Price;
            }

            //Our work is done. Now return the correct data.
            return summaryHolder.Select(a => a.Value).ToList();
        }
        private static void SaveToDatabase(IEnumerable<SummarizedSalesData> items)
        {
            // Assumed database fast insert code is implemented here.
            Console.WriteLine("Writing records to the database...");
            int recordCounter = 0;
            double totalVolume = 0;
            decimal totalPrice = 0;
            foreach (var item in items)
            {
                recordCounter++;
                totalVolume += item.TotalVolume;
                totalPrice += item.TotalPrice;
            }
            Console.WriteLine($"Records saved to the database. Total record count: {recordCounter}, Total Volume: {totalVolume}, Total Price: {totalPrice}");
        }
    }

    public sealed class SalesDataHelper
    {
        public SalesDataHelper()
        {
            _rnd = new Random();
            InitArray(ref _brands, ref _brandCodes, 10, "Brand Text", true);
            InitArray(ref _companies, ref _companyCodes, 5, "Company Name", true);
            InitArray(ref _stores, ref _storeCodes, 100, "Store Name", true);
            InitArray(ref _prodcuts, ref _prodcuts, 50, "Product description", false);
        }

        private Random _rnd;
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public IEnumerable<SalesData> GetStreamingSales(long maxCount)
        {
            long counter = 0;
            while (counter++ < maxCount)
            {
                int brandIndex = _rnd.Next(0, _brands.Length);
                int companyIndex = _rnd.Next(0, _companies.Length);
                int storeIndex = _rnd.Next(0, _stores.Length);
                int productIndex = _rnd.Next(0, _prodcuts.Length);
                var item = new SalesData()
                {
                    Id = counter,
                    BrandId = _brandCodes[brandIndex],
                    BrandName = _brands[brandIndex],
                    CompanyId = companyIndex,
                    CompanyName = _companies[companyIndex],
                    ProductId = productIndex,
                    ProductName = _prodcuts[productIndex],
                    StoreId = _storeCodes[storeIndex],
                    StoreName = _stores[storeIndex],
                    Price = (decimal)(_rnd.NextDouble() * 1000),
                    Volume = Math.Round(_rnd.NextDouble() * 1000, 2),
                    SalesDate = DateTime.Today.AddDays(-1 * _rnd.Next(1, 60)).ToString("yyyy-MM-dd"),
                    OtherData = new byte[10 * 1024] // 10KB
                };
                _rnd.NextBytes(item.OtherData);
                yield return item;
            }
        }

        private void InitArray(ref string[] array, ref string[] idArray, int itemCount, string prefix, bool fillIdArray)
        {
            array = new string[itemCount];
            if (fillIdArray)
                idArray = new string[itemCount];
            for (int i = 0; i < itemCount; i++)
            {
                array[i] = $"{prefix} - {i.ToString()}";
                if (fillIdArray)
                {
                    string tmpCode;
                    do
                    {
                        tmpCode = new string(Enumerable.Repeat(chars, 5)
                          .Select(s => s[_rnd.Next(s.Length)]).ToArray());
                    } while (idArray.Contains(tmpCode));
                    idArray[i] = tmpCode;
                }
            }
        }


        private string[] _brands;
        private string[] _brandCodes;
        private string[] _companies;
        private string[] _companyCodes;
        private string[] _stores;
        private string[] _storeCodes;
        private string[] _prodcuts;
    }

    public sealed class SalesData
    {
        public long Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string StoreId { get; set; }
        public string StoreName { get; set; }
        public string BrandId { get; set; }
        public string BrandName { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string SalesDate { get; set; }
        public double Volume { get; set; }
        public decimal Price { get; set; }
        public byte[] OtherData { get; set; }
    }

    public sealed class SummarizedSalesData
    {
        public int ProductId { get; set; }
        public int CompanyId { get; set; }
        public string StoreId { get; set; }
        public string BrandId { get; set; }
        public int WeekNumber { get; set; }
        public double TotalVolume { get; set; }
        public decimal TotalPrice { get; set; }
    }
}