// Ignore Spelling: Unschedule

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

        protected void NotifyOfTransaction(TradeTransaction transaction)
        {
            _notifyAction.Invoke(new Notification()
            {
                NotificationType = NotificationTypes.Transaction,
                TransactionValuesByColumns = new Dictionary<string, string>()
                {
                    { nameof(TradeTransaction.TimeStamp), transaction.TimeStamp.ToString() },
                    { nameof(TradeTransaction.TransactionType), transaction.TransactionType.ToString() },
                    { nameof(TradeTransaction.Quantity), transaction.Quantity.ToString() },
                    { nameof(TradeTransaction.Price), transaction.Price.ToString() },
                    { nameof(TradeTransaction.Total), transaction.Price.ToString() },
                    { nameof(TradeTransaction.AvailableUSD), transaction.AvailableUSD.ToString() },
                    { nameof(TradeTransaction.LockedUSD), transaction.LockedUSD.ToString() },
                    { nameof(TradeTransaction.AvailableBTC), transaction.AvailableBTC.ToString() },
                    { nameof(TradeTransaction.LockedBTC), transaction.LockedBTC.ToString() }
                }
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
