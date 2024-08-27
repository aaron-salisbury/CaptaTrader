using CaptaTrader.Business.Entities;
using CaptaTrader.Business.Services;
using Quartz;
using Quartz.Impl;
using static CaptaTrader.Business.Models.Notification;

namespace CaptaTrader.Business.Models
{
    public class BaseBotTraderService : IBotTraderService
    {
        public int MinSecondsBetweenTrades { get; set; } = 6;
        public PriceInterval? PriceIntervalMonth { get; set; }
        public PriceInterval? PriceIntervalDay { get; set; }
        public decimal? AccountBalance { get; set; }
        public decimal? AssetBalance { get; set; }

        protected readonly Action<Notification> _notifyAction;
        protected readonly IScheduler _scheduler;

        public BaseBotTraderService(Action<Notification> notifyAction)
        {
            _notifyAction = notifyAction;

            _scheduler = Task.Run(() => new StdSchedulerFactory().GetScheduler()).Result;
            _scheduler.Context.Put(this.GetType().Name, this);
        }

        protected void NotifyOfTradeOrder(TradeOrder order)
        {
            _notifyAction.Invoke(new Notification()
            {
                NotificationType = NotificationTypes.Transaction,
                TradeOrder = order
            });
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
