﻿using PathOfExile;
using PathOfExile.Model;
using PoECurrency = PathOfExile.Model.Currency;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using PublicStashExample.Example.Trade;
using Currency = PublicStashExample.Example.Trade.Currency;
using LiteDB;

namespace TestePoE
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        public static Dictionary<String, int> GetAllCurrencyAndAddThemUp(PublicStash ps)
        {
            var dict = new Dictionary<String, int>();

            foreach (var stash in ps.stashes)
            {
                foreach (var item in stash.items)
                {
                    switch (item)
                    {
                        case PoECurrency currency when item.GetType() == typeof(PoECurrency):
                            dict.TryGetValue(currency.typeLine, out var current);
                            current += currency.stackSize;
                            dict[currency.typeLine] = current;

                            break;
                    }
                }
            }

            return dict;
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }


        public class CurrencyDb
        {
            public int Id { get; set; }
            public string Buy { get; set; }
            public string Sell { get; set; }
            public decimal Average { get; set; }
        }

        private void RunAnalytics()
        {
            var iteration = 0;
            var id = PublicStashAPI.GetLatestStashIdAsync().Result;
            var publicStash = PublicStashAPI.GetAsync(id).Result;
            var nextChangeId = publicStash.next_change_id;
            var cachedChangeId = "";
            var trader = new PoeTrader();

            for (; ; )
            {
                if (cachedChangeId != nextChangeId)
                {
                    IEnumerable<Price> teste = trader.GetCurrencyListings(publicStash);
                    List<Price> target = teste.ToList();

                    foreach (Currency val in Enum.GetValues(typeof(Currency)))
                    {
                        foreach (Currency val2 in Enum.GetValues(typeof(Currency)))
                        {

                            using (var db = new LiteDatabase(@"MyData.db"))
                            {

                                if (val != val2)
                                {
                                    var value = getAveragePrice(target, val2, val);
                                    var currencyCollection = db.GetCollection<CurrencyDb>("currency");

                                    var newCurrency = new CurrencyDb
                                    {
                                        Buy = val2.Value(),
                                        Sell = val.Value(),
                                        Average = value
                                    };

                                    var Exist = currencyCollection.Exists(x => x.Buy == val2.Value() && x.Sell == val.Value());
                                    if (value > 0)
                                    {
                                        if (Exist)
                                        {
                                            currencyCollection.Update(newCurrency);
                                        }
                                        else
                                        {
                                            currencyCollection.Insert(newCurrency);

                                        }
                                    }
                                }

                            }

                            /*/using (StreamWriter sw = File.AppendText(Application.StartupPath + "/err.txt"))
                            {
                                if (val != val2)
                                {
                                    var value = getAveragePrice(target, val, val2);
                                    if (value > 0)
                                    {
                                        sw.WriteLine("Compra 1 " + val.Value() + " por " + value + " " + val2.Value());
                                    }
                                }
                            }*/
                        }
                    }
                    iteration++;
                    cachedChangeId = nextChangeId;
                }
                else
                {
                    publicStash = PublicStashAPI.GetAsync(nextChangeId).Result;
                    nextChangeId = publicStash.next_change_id;
                    Thread.Sleep(5000);
                }
            }
        }

        private void RunTrader()
        {
            // ReSharper disable once NotAccessedVariable
            var iteration = 0;

            var id = PublicStashAPI.GetLatestStashIdAsync().Result;
            var publicStash = PublicStashAPI.GetAsync(id).Result;
            var nextChangeId = publicStash.next_change_id;
            var cachedChangeId = "";
            var trader = new PoeTrader();

            for (; ; )
            {
                if (cachedChangeId != nextChangeId)
                {
                    IEnumerable<Price> teste = trader.GetCurrencyListings(publicStash);
                    List<Price> target = teste.ToList();
                    iteration++;

                    for (int index = 0; index < target.Count; index++)
                    {
                        if (target[index].Seller.League == "Delve")
                        {
                            if (target[index].Selling.Currency != Currency.MISSING_TYPE && target[index].Buying.Currency != Currency.MISSING_TYPE)
                            {
                                using (var db = new LiteDatabase("MyData.db"))
                                {
                                    var orders = db.GetCollection<CurrencyDb>("currency");
                                    if (target[index].PricePerUnit > 0)
                                    {
                                        var query = orders.Find(x => x.Buy == target[index].Buying.Currency.Value() && x.Sell == target[index].Selling.Currency.Value());
                                        foreach (var order in query)
                                        {
                                            if (target[index].PricePerUnit > order.Average)
                                            {
                                                richTextBox1.Invoke((MethodInvoker)delegate
                                                {
                                                    richTextBox1.AppendText(target[index].Seller.LastKnownCharacter + ", Buy: " + target[index].Buying.Currency.Value() + ", Sell: " + target[index].Selling.Currency.Value() + ", Value: " + target[index].PricePerUnit + Environment.NewLine);
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    cachedChangeId = nextChangeId;
                }
                else
                {
                    publicStash = PublicStashAPI.GetAsync(nextChangeId).Result;
                    nextChangeId = publicStash.next_change_id;
                    Thread.Sleep(5000);
                }
            }
        }

        public static decimal Sum(params decimal[] value)
        {
            decimal result = 0;

            for (int i = 0; i < value.Length; i++)
            {
                result += value[i];
            }

            return result;
        }

        public static decimal Average(params decimal[] value)
        {
            decimal sum = Sum(value);
            decimal result = (decimal)sum / value.Length;
            return result;
        }

        private static decimal getAveragePrice(List<Price> Prices, Currency currencyBuying, Currency currencySelling)
        {
            List<decimal> AveragePrice = new List<decimal>();
            var target = Prices;
            decimal lastPrice = 0;
            for (int index = 0; index < target.Count; index++)
            {
                if (target[index].Seller.League == "Delve")
                {
                    if (target[index].Selling.Currency != Currency.MISSING_TYPE && target[index].Buying.Currency != Currency.MISSING_TYPE && target[index].Selling.Currency == currencySelling && target[index].Buying.Currency == currencyBuying)
                    {
                        AveragePrice.Add(target[index].PricePerUnit);
                        lastPrice = target[index].PricePerUnit;
                    }
                }
            }
            decimal[] AveragePriceArray = AveragePrice.ToArray();
            if (AveragePriceArray.Length > 0)
            {
                AveragePrice.RemoveAll(it => it > (Average(AveragePriceArray)));
                AveragePriceArray = AveragePrice.ToArray();
                return Average(AveragePriceArray);
            }
            else
            {
                return 0;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            backgroundWorker2.RunWorkerAsync();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            RunAnalytics();
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            RunTrader();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            using (var db = new LiteDatabase("MyData.db"))
            {
                var orders = db.GetCollection<CurrencyDb>("currency");
                var query = orders.Find(x => x.Buy == Currency.CHAOS_ORB.Value());
                string teste = "";
                foreach (var order in query)
                {
                    teste = teste + order.Buy + " " + order.Sell + " " + order.Average + Environment.NewLine;
                }
                richTextBox2.Invoke((MethodInvoker)delegate
                {
                    richTextBox2.Text = teste;
                });
            }
        }

    }
}