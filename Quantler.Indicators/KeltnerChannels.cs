#region License Header

/*
* QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
* Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
* Modifications Copyright 2018 Quantler B.V.
*
*/

#endregion License Header

using Quantler.Data.Bars;

namespace Quantler.Indicators
{
    /// <summary>
    /// This indicator creates a moving average (middle band) with an upper band and lower band
    /// fixed at k average true range multiples away from the middle band.
    /// </summary>
    public class KeltnerChannels : BarIndicator
    {
        #region Private Fields

        private readonly decimal _k;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the KeltnerChannels class
        /// </summary>
        /// <param name="period">The period of the average true range and moving average (middle band)</param>
        /// <param name="k">The number of multiplies specifying the distance between the middle band and upper or lower bands</param>
        /// <param name="movingAverageType">The type of moving average to be used</param>
        public KeltnerChannels(int period, decimal k, MovingAverageType movingAverageType = MovingAverageType.Simple)
            : this(string.Format("KC({0},{1})", period, k), period, k, movingAverageType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the KeltnerChannels class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the average true range and moving average (middle band)</param>
        /// <param name="k">The number of multiples specifying the distance between the middle band and upper or lower bands</param>
        /// <param name="movingAverageType">The type of moving average to be used</param>
        public KeltnerChannels(string name, int period, decimal k, MovingAverageType movingAverageType = MovingAverageType.Simple)
            : base(name)
        {
            _k = k;

            //Initialize ATR and SMA
            AverageTrueRange = new AverageTrueRange(name + "_AverageTrueRange", period, MovingAverageType.Simple);
            MiddleBand = movingAverageType.AsIndicator(name + "_MiddleBand", period);

            //Compute Lower Band
            LowerBand = new FunctionalIndicator<DataPointBar>(name + "_LowerBand",
                input => ComputeLowerBand(),
                lowerBand => MiddleBand.IsReady,
                () => MiddleBand.Reset()
                );

            //Compute Upper Band
            UpperBand = new FunctionalIndicator<DataPointBar>(name + "_UpperBand",
                input => ComputeUpperBand(),
                upperBand => MiddleBand.IsReady,
                () => MiddleBand.Reset()
                );
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Gets the average true range
        /// </summary>
        public IndicatorBase<DataPointBar> AverageTrueRange
        {
            get;
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady =>
            MiddleBand.IsReady && UpperBand.IsReady && LowerBand.IsReady && AverageTrueRange.IsReady;

        /// <summary>
        /// Gets the lower band of the channel
        /// </summary>
        public IndicatorBase<DataPointBar> LowerBand
        {
            get;
        }

        /// <summary>
        /// Gets the middle band of the channel
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> MiddleBand
        {
            get;
        }

        /// <summary>
        /// Gets the upper band of the channel
        /// </summary>
        public IndicatorBase<DataPointBar> UpperBand
        {
            get;
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            AverageTrueRange.Reset();
            MiddleBand.Reset();
            UpperBand.Reset();
            LowerBand.Reset();
            base.Reset();
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input">The TradeBar to this indicator on this time step</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(DataPointBar input)
        {
            AverageTrueRange.Update(input);

            var typicalPrice = (input.High + input.Low + input.Close) / 3m;
            MiddleBand.Update(input.Occured, input.TimeZone, typicalPrice);

            // poke the upper/lower bands, they actually don't use the input, they compute
            // based on the ATR and the middle band
            LowerBand.Update(input);
            UpperBand.Update(input);
            return MiddleBand;
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Calculates the lower band
        /// </summary>
        private decimal ComputeLowerBand() =>
            MiddleBand.IsReady ? MiddleBand - AverageTrueRange * _k : new decimal(0.0);

        /// <summary>
        /// Calculates the upper band
        /// </summary>
        private decimal ComputeUpperBand() =>
            MiddleBand.IsReady ? MiddleBand + AverageTrueRange * _k : new decimal(0.0);

        #endregion Private Methods
    }
}