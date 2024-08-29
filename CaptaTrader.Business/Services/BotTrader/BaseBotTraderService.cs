using CaptaTrader.Business.Services;
using Quartz;
using Quartz.Impl;

namespace CaptaTrader.Business.Models
{
    internal class BaseBotTraderService : IBotTraderService
    {
        public event EventHandler<TradeEventArgs>? TradeCompleted;

        internal int MinSecondsBetweenTrades { get; set; } = 6;
        internal PriceInterval? PriceIntervalMonth { get; set; }
        internal PriceInterval? PriceIntervalDay { get; set; }
        internal decimal? AccountBalance { get; set; }
        internal decimal? AssetBalance { get; set; }

        protected readonly IScheduler _scheduler;

        internal BaseBotTraderService()
        {
            _scheduler = Task.Run(() => new StdSchedulerFactory().GetScheduler()).Result;
            _scheduler.Context.Put(this.GetType().Name, this);
        }

        protected async Task UnscheduleJobsAsync()
        {
            await _scheduler.Clear();
            await _scheduler.Standby();
        }

        protected virtual async Task ScheduleJobsAsync()
        {
            await Task.Run(() => throw new NotImplementedException());
        }

        protected virtual void OnTradeCompleted(TradeEventArgs tradeEventArgs)
        {
            TradeCompleted?.Invoke(this, tradeEventArgs);
        }

        #region Virtual Interface Methods
        public virtual Task<bool> InitializeAsync()
        {
            throw new NotImplementedException();
        }

        public virtual Task ShutdownAsync()
        {
            throw new NotImplementedException();
        }

        public virtual Task<bool> UpdateBalancesAsync()
        {
            throw new NotImplementedException();
        }

        public virtual Task<bool> UpdatePriceIntervalsAsync()
        {
            throw new NotImplementedException();
        }

        public virtual Task<bool> AttemptToInitiateTradeAsync()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
