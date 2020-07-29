using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;

namespace BarcodeApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IMemoryCache _cache;

        public IndexModel(ILogger<IndexModel> logger,IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        //properties and variables
        [BindProperty(SupportsGet =true)]
        public string upc { get; set; }

        [BindProperty(SupportsGet = true)]
        public int addAmount { get; set; }

        [BindProperty(SupportsGet = true)]
        public string pickedSKU { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool displaySKU { get; set; }

        [BindProperty(SupportsGet = true)]
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> skuList { get; set; }

        [BindProperty(SupportsGet = true)]
        public string displayMsg { get; set; }


        private string uniqueFileID ="";
        public void OnGet()
        {
            if(string.IsNullOrWhiteSpace(upc))
                upc = "";
            if (string.IsNullOrWhiteSpace(pickedSKU))
                pickedSKU = "";
            if (_cache.Get<int>("addAmount")==0)
            {
                _cache.Set<int>("addAmount", 1);
                addAmount = 1;
            }
            else
            {
                addAmount = _cache.Get<int>("addAmount");
            }

            uniqueFileID = _cache.Get<string>("session");

            displaySKU = false;

            if (_cache.Get<bool>("displaySKU") == true)
            {
                displaySKU = true;
            }

            if (string.IsNullOrWhiteSpace(uniqueFileID))
                uniqueFileID = "";

            if (string.IsNullOrWhiteSpace(_cache.Get<string>("myMsg")))
                displayMsg = "Welcome, scan a UPC to begin.";
            else
                displayMsg = _cache.Get<string>("myMsg");

            if (_cache.Get<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>>("skuList")!=null)
            {
                skuList = _cache.Get<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>>("skuList");
            }
            else
            {
                getSKUList();
            }

        }

        public IActionResult OnPost()
        {
            if(ModelState.IsValid == false)
            {
                return Page(); //error
            }
            if(addAmount!=0)
                _cache.Set<int>("addAmount", addAmount);
            addCount();
            return RedirectToPage("/Index",upc);
        }

        public IActionResult OnPostEdit()
        {
            uniqueFileID = _cache.Get<string>("session");
            if (string.IsNullOrWhiteSpace(uniqueFileID))
            {
                _cache.Set<string>("myMsg", "Nothing to save.");
            }
            else
            {
                Dictionary<string,int> itemCountDic = new Dictionary<string, int>();

                using (TextFieldParser parser = new TextFieldParser(System.AppDomain.CurrentDomain.BaseDirectory + uniqueFileID))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.HasFieldsEnclosedInQuotes = true;
                    parser.SetDelimiters(",");
                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        if (itemCountDic.ContainsKey(fields[0]))
                        {
                            int currentValue;
                            itemCountDic.TryGetValue(fields[0], out currentValue);
                            itemCountDic[fields[0]] = currentValue + Int32.Parse(fields[3]);
                        }
                        else
                        {
                            itemCountDic.Add(fields[0], Int32.Parse(fields[3]));
                        }
                    }
                }
                //clear the file
                System.IO.File.WriteAllText(System.AppDomain.CurrentDomain.BaseDirectory + "COMPILED" + uniqueFileID, string.Empty);
                foreach (var item in itemCountDic)
                {
                    string lineItem = item.Key.Replace("\"", "\"\"") +","+ item.Value+"\n";
                    System.IO.File.AppendAllText(System.AppDomain.CurrentDomain.BaseDirectory + "COMPILED"+uniqueFileID, lineItem);

                }
                _cache.Set<string>("myMsg", "Compiled!");
            }
            return RedirectToPage("/Index");
        }
        public IActionResult OnPostDelete()
        {
            uniqueFileID = _cache.Get<string>("session");
            if (string.IsNullOrWhiteSpace(uniqueFileID))
            {
                _cache.Set<string>("myMsg", "Nothing to undo.");
            }
            else
            {
                var lines = System.IO.File.ReadAllLines(System.AppDomain.CurrentDomain.BaseDirectory + uniqueFileID);
                if (lines.Length > 0)
                {
                    string lastLine = lines[lines.Length - 1].Split(",")[0];
                    _cache.Set<string>("myMsg", lastLine+" removed.");
                    System.IO.File.WriteAllLines(System.AppDomain.CurrentDomain.BaseDirectory + uniqueFileID, lines.Take(lines.Length - 1).ToArray());
                }
                else
                {
                    _cache.Set<string>("myMsg", "Nothing to undo.");
                }
            }
            return RedirectToPage("/Index");
        }

        public void addCount()
        {
            addAmount = _cache.Get<int>("addAmount");
            if (_cache.Get<bool>("displaySKU") == true)
            {
                _cache.Set<bool>("displaySKU", false);
                displaySKU = true;

            }
            //find the upc in the upc listing
            //StreamReader sr = new StreamReader(System.AppDomain.CurrentDomain.BaseDirectory + "upcs.csv");
            bool match = false;
            string sku = "";
            string title = "";
            using (TextFieldParser parser = new TextFieldParser(System.AppDomain.CurrentDomain.BaseDirectory + "upcs.csv"))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.HasFieldsEnclosedInQuotes = true;
                parser.SetDelimiters(",");
                bool firstLine = true;
                while (!parser.EndOfData &&!match)
                {
                    //Processing row
                    string[] fields = parser.ReadFields();

                    if (!firstLine)
                    {
                        if(fields[2].Equals(upc)&&displaySKU==false&&upc.Length>1)
                        {
                            match = true;
                            sku = fields[0];
                            title = fields[1];
                            break;
                        }
                        else
                        {
                            if (displaySKU && pickedSKU.Length > 1)
                            {
                                pickedSKU = pickedSKU.Split("---(")[0];
                                if (fields[0].Equals(pickedSKU))
                                {
                                    match = true;
                                    sku = pickedSKU;
                                    title = fields[1];
                                    upc = fields[2];
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        firstLine = false;

                        continue;
                    }
                    
                }
            }

            if (match)
            {
                uniqueFileID = _cache.Get<string>("session");
                string lineItem = "\""+sku.Replace("\"","\"\"") + "\",\"" + title.Replace("\"", "\"\"") + "\"," + upc + "," + addAmount;
                _cache.Set<string>("myMsg", "("+addAmount+") " +title+" was added to the count.");
                _cache.Set<int>("addAmount", 1);
                if (string.IsNullOrWhiteSpace(uniqueFileID))
                {
                    string timeStamp = "file_" + DateTime.Now.ToString("MMMddHHmmss") + Environment.UserName + ".csv";
                    uniqueFileID = timeStamp;
                    _cache.Set<string>("session", uniqueFileID);
                    System.IO.File.WriteAllText(System.AppDomain.CurrentDomain.BaseDirectory + uniqueFileID, lineItem);

                }
                else
                {
                    lineItem = "\n" + lineItem;
                    System.IO.File.AppendAllText(System.AppDomain.CurrentDomain.BaseDirectory + uniqueFileID, lineItem);
                }

                if(displaySKU && _cache.Get<string>("failedUPC")!="N/A")
                {
                    lineItem = _cache.Get<string>("failedUPC") +","+ "\"" + sku.Replace("\"", "\"\"") + "\""+ "\n";
                    System.IO.File.AppendAllText(System.AppDomain.CurrentDomain.BaseDirectory + "foundUPCs.csv", lineItem);
                }
            }
            else
            {
                _cache.Set<bool>("displaySKU", true);
                _cache.Set<string>("failedUPC", upc);
                _cache.Set<string>("myMsg", "No match!");
            }
        }


        public void getSKUList()
        {
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> skuList = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
            using (TextFieldParser parser = new TextFieldParser(System.AppDomain.CurrentDomain.BaseDirectory + "upcs.csv"))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.HasFieldsEnclosedInQuotes = true;
                parser.SetDelimiters(",");
                bool firstLine = true;
                int counter = 0;
                while (!parser.EndOfData)
                {
                    //Processing row
                    string[] fields = parser.ReadFields();

                    if (!firstLine)
                    {
                        skuList.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(fields[0]+"---( "+fields[1] + " )", counter.ToString()));
                        counter++;
                    }
                    else
                    {
                        firstLine = false;

                        continue;
                    }

                }
            }
            _cache.Set<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>>("skuList", skuList);
        }
        public void complete()
        {

        }
    }
}
