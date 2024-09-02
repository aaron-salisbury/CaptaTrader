using CaptaTrader.Business.Services;
using Quartz;
using Quartz.Impl;

namespace CaptaTrader.Business.Models
{
    public class BaseBotTraderService : IBotTraderService
    {
        public Portfolio RunningPortfolio { get; set; } = new();

        internal int MinSecondsBetweenTrades { get; set; } = 6;
        internal PriceInterval? PriceIntervalMonth { get; set; }
        internal PriceInterval? PriceIntervalDay { get; set; }

        protected readonly IScheduler _scheduler;

        public BaseBotTraderService()
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
