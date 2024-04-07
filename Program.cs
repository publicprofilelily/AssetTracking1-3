
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AssetTracking
{
    public abstract class Asset
    {
        public string Type { get; }
        public string Brand { get; }
        public string Model { get; }
        public string Office { get; }
        public DateTime PurchaseDate { get; }
        public double PriceInUSD { get; }
        public string Currency { get; set; }

        protected Asset(string type, string brand, string model, string office, DateTime purchaseDate, double priceInUSD)
        {
            Type = type;
            Brand = brand;
            Model = model;
            Office = office;
            PurchaseDate = purchaseDate;
            PriceInUSD = priceInUSD;
            Currency = SetCurrencyByOffice(); // Sets currency based on office location
        }

        public abstract string SetCurrencyByOffice();
        public abstract double ConvertCurrency();

        // Gets column data based on index for LINQ operations
        public string GetColumnData(int index)
        {
            return index switch
            {
                0 => Type,
                1 => Brand,
                2 => Model,
                3 => Office,
                4 => PurchaseDate.ToString("MM/dd/yyyy"),
                5 => PriceInUSD.ToString("N0", CultureInfo.InvariantCulture),
                6 => Currency,
                7 => ConvertCurrency().ToString("N0", CultureInfo.InvariantCulture),
                _ => throw new IndexOutOfRangeException() // If in the future developments it adds more headers, this throw ensures that the developer changes it here too
            };
        }

        // Format output based on dynamic column widths
        public string GetFormattedOutput(int[] columnWidths)
        {
            CultureInfo ci = CultureInfo.InvariantCulture;
            var localPriceToday = ConvertCurrency().ToString("N0", ci);

            return $"{Type.PadRight(columnWidths[0])}  " +
                   $"{Brand.PadRight(columnWidths[1])}  " +
                   $"{Model.PadRight(columnWidths[2])}  " +
                   $"{Office.PadRight(columnWidths[3])}  " +
                   $"{PurchaseDate.ToString("MM/dd/yyyy", ci).PadRight(columnWidths[4])}  " +
                   $"{PriceInUSD.ToString("N0").PadRight(columnWidths[5])}  " +
                   $"{Currency.PadRight(columnWidths[6])}  " +
                   $"{localPriceToday.PadRight(columnWidths[7])}";
        }

        // Generates a CSV string for file output
        public string ToCsvString()
        {
            var localPriceToday = ConvertCurrency().ToString("N0", CultureInfo.InvariantCulture);
            return $"{Type},{" ",10},{Brand},{Model},{Office},{PurchaseDate:MM/dd/yyyy},{PriceInUSD:N0},{Currency},{localPriceToday}";
        }
    }

    public class Computer : Asset
    {
        public Computer(string brand, string model, string office, DateTime purchaseDate, double priceInUSD)
            : base("Computer", brand, model, office, purchaseDate, priceInUSD)
        {
        }

        public override string SetCurrencyByOffice()
        {
            return Office switch
            {
                "Europe" => "EUR",
                "Spain" => "EUR",  // Ensuring Spain uses EUR
                "Sweden" => "SEK", // Adding SEK for Sweden
                "USA" => "USD",
                _ => "USD"  // Default currency
            };
        }

        public override double ConvertCurrency()
        {
            double conversionRate = Currency switch
            {
                "EUR" => 0.92,
                "SEK" => 10.63,
                _ => 1.0 // USD and any other unspecified currency
            };
            return PriceInUSD * conversionRate;
        }
    }

    public class Phone : Asset
    {
        public Phone(string brand, string model, string office, DateTime purchaseDate, double priceInUSD)
            : base("Phone", brand, model, office, purchaseDate, priceInUSD)
        {
        }

        public override string SetCurrencyByOffice()
        {
            return Office switch
            {
                "Europe" => "EUR",
                "Sweden" => "SEK",
                "USA" => "USD",
                "Spain" => "EUR",  // Ensuring Spain uses EUR
                _ => "USD"  // Default currency
            };
        }

        public override double ConvertCurrency()
        {
            double conversionRate = Currency switch
            {
                "EUR" => 0.92,
                "SEK" => 10.63,
                _ => 1.0 // Default rate for USD and any other unspecified currency
            };
            return PriceInUSD * conversionRate;
        }
    }

    class Program
    {
        static List<Asset> assets = new List<Asset>();

        static void Main(string[] args)
        {
            InputAssets();
            SaveAssetsToFile("assets.csv");
            DisplayAssetsTable();
        }

        static void InputAssets()
        {
            bool adding = true;
            while (adding)
            {
                Console.WriteLine("Add a new asset (Computer/Phone) or type 'done' to finish:");
                string type = Console.ReadLine().ToLower();
                if (type == "done") break;

                Console.WriteLine("Enter brand:");
                string brand = Console.ReadLine();
                Console.WriteLine("Enter model:");
                string model = Console.ReadLine();
                Console.WriteLine("Enter office location:");
                string office = Console.ReadLine();
                Console.WriteLine("Enter purchase date (MM/dd/yyyy):");
                DateTime purchaseDate;
                while (!DateTime.TryParseExact(Console.ReadLine(), "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out purchaseDate))
                {
                    Console.WriteLine("Invalid date format. Please enter the purchase date (MM/dd/yyyy):");
                }
                Console.WriteLine("Enter price in USD:");
                double priceInUSD;
                while (!double.TryParse(Console.ReadLine(), out priceInUSD))
                {
                    Console.WriteLine("Invalid input. Please enter the price in USD:");
                }

                Asset asset = type == "computer" ? new Computer(brand, model, office, purchaseDate, priceInUSD)
                                                 : new Phone(brand, model, office, purchaseDate, priceInUSD);
                assets.Add(asset);
            }
        }




        static void SaveAssetsToFile(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("Type,Brand,Model,Office,Purchase Date,Price in USD,Currency,Local Price");
                    foreach (Asset asset in assets)
                    {
                        writer.WriteLine(asset.ToCsvString());
                    }
                }
                Console.WriteLine("Assets saved to " + filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while saving the file: " + ex.Message);
            }
        }

        static void DisplayAssetsTable()
        {
            // Headers
            string[] headers = { "Type", "Brand", "Model", "Office", "Purchase Date", "Price in USD", "Currency", "Local price today" };

            // Calculate max length of each column (including headers)
            int[] columnWidths = headers
                .Select((header, index) => assets
                    .Select(asset => asset.GetColumnData(index).Length)
                    .Prepend(header.Length)
                    .Max())
                .ToArray();

            // Generate the header line
            string headerLine = GenerateLine(headers, columnWidths);
            string underline = GenerateUnderline(columnWidths);

            Console.WriteLine(headerLine);
            Console.WriteLine(underline);

            // Print the rows
            var sortedAssets = assets.OrderBy(a => a.Office).ThenBy(a => a.PurchaseDate);
            foreach (var asset in sortedAssets)
            {
                TimeSpan timeToEOL = asset.PurchaseDate.AddYears(3) - DateTime.Now;

                // Apply color coding based on the time to end of life (EOL)
                if (timeToEOL.TotalDays <= 90)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else if (timeToEOL.TotalDays <= 180)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                }

                // Print the asset row
                Console.WriteLine(asset.GetFormattedOutput(columnWidths));

                Console.ResetColor();
            }



            static string GenerateLine(string[] headers, int[] columnWidths)
            {
                return string.Join("  ", headers.Select((header, index) => header.PadRight(columnWidths[index])));
            }

            static string GenerateUnderline(int[] columnWidths)
            {
                return string.Join("  ", columnWidths.Select(width => new string('-', width)));
            }
        }
    }
}