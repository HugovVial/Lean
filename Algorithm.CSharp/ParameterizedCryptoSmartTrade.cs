using System;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Orders;
using QuantConnect.Algorithm.Framework.Alphas;

using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Parameters;
using QuantConnect.Interfaces;
using System.Drawing;

namespace QuantConnect.Algorithm.CSharp
{
    public class ParameterizedCryptoSmartTrade : QCAlgorithm
    {
        [Parameter("bollinger-periode")]
        public int BollingerPeriod =20;

        [Parameter("bollinger-k")]
         public decimal BollingerK = 2;

         [Parameter("averagedirectionnal-period")]
         public int AverageDirectionnalPeriod = 25;

         [Parameter("k-period")]
        public int KPeriod = 3;
        [Parameter("stoch-period")] 
        public int StochPeriod = 14;
        [Parameter("d-period")]
        public int DPeriod = 3;

        private Symbol _btcusd;

        private string _chartName = "Trade Plot";
        private string _priceSeriesName = "Price";
        private string _portfolioValueSeriesName = "PortFolioValue";
        private string _ADX = "ADX";
        private string _bollingerUpperSeriesName = "UpperBollinger";
        private string _bollingerLowerSeriesName = "LowerBollinger";

          

        public AverageDirectionalIndex adx;
        private BollingerBands _bollingerBands;
        private Stochastic _stoch;
        public override void Initialize()
        {
            this.InitPeriod();

            this.SetWarmUp(TimeSpan.FromDays(365));

            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);

            SetCash(10000); // capital
            var btcSecurity = AddCrypto("BTCUSD", Resolution.Daily);
            _btcusd = btcSecurity.Symbol;

           _bollingerBands = BB(_btcusd, BollingerPeriod, BollingerK, MovingAverageType.Simple, Resolution.Daily);
            adx = ADX(_btcusd, AverageDirectionnalPeriod, Resolution.Daily);
            _stoch = STO(_btcusd, StochPeriod, KPeriod, DPeriod, Resolution.Daily);

            var stockPlot = new Chart(_chartName);
            var assetPrice = new Series(_priceSeriesName, SeriesType.Line, "$", Color.Blue);
            var portFolioValue = new Series(_portfolioValueSeriesName, SeriesType.Line, "$", Color.Green);
            var upperBollingerSeries = new Series(_bollingerUpperSeriesName, SeriesType.Line, "$", Color.Gray);
            var lowerBollingerSeries = new Series(_bollingerLowerSeriesName, SeriesType.Line, "$", Color.Gray);
            var ADXPlot = new Series(_ADX, SeriesType.Line, "$", Color.Pink);
          


            stockPlot.AddSeries(assetPrice);
            stockPlot.AddSeries(portFolioValue);
            stockPlot.AddSeries(upperBollingerSeries);
            stockPlot.AddSeries(lowerBollingerSeries);
            stockPlot.AddSeries(ADXPlot);
             AddChart(stockPlot);

            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromDays(1)), DoPlots);
        }

        private void DoPlots()
        {
            Plot(_chartName, _priceSeriesName, Securities[_btcusd].Price);
            Plot(_chartName, _portfolioValueSeriesName, Portfolio.TotalPortfolioValue);
           Plot(_chartName, _bollingerUpperSeriesName, _bollingerBands.UpperBand);
             Plot(_chartName, _ADX, adx);
            Plot(_chartName, _bollingerLowerSeriesName, _bollingerBands.LowerBand);
        
        }

        public override void OnData(Slice data)
        {
            if (this.IsWarmingUp || !_bollingerBands.IsReady || !_stoch.IsReady)
                return;

            var holdings = Portfolio[_btcusd].Quantity;
            var currentPrice = data[_btcusd].Close;

            if (adx >= 20 && currentPrice > _bollingerBands.UpperBand && _stoch.StochK > 80)
            {
                if (!Portfolio.Invested)
                {
                    SetHoldings(_btcusd, 1);
                }
            }
            else if (adx < 15 && currentPrice < _bollingerBands.LowerBand && _stoch.StochK < 20)
            {
                if (Portfolio.Invested)
                {
                    Liquidate(_btcusd);
                }
            }
        }

        private void InitPeriod()
        {
            SetStartDate(2021, 1, 1); // Start date
            SetEndDate(2023, 10, 20); // End date
        }
    }
}
