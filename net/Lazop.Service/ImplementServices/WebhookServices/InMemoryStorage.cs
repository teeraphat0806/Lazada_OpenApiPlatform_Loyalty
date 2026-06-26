using System.Collections.Concurrent;
using System.Collections.Generic;
using Lazop.Domain.Models;

namespace Lazop.Service.ImplementServices.WebhookServices
{
    public static class InMemoryStorage
    {
        public static ConcurrentList<LazadaMessage> LazadaMessages { get; } = new ConcurrentList<LazadaMessage>();
        public static ConcurrentDictionary<string, LazadaOrder> LazadaOrders { get; } = new ConcurrentDictionary<string, LazadaOrder>();
        public static ConcurrentDictionary<string, LazadaReverseOrder> LazadaReverseOrders { get; } = new ConcurrentDictionary<string, LazadaReverseOrder>();
    }

    public class ConcurrentList<T>
    {
        private readonly List<T> _list = new List<T>();
        private readonly object _lock = new object();

        public void Add(T item)
        {
            lock (_lock)
            {
                _list.Add(item);
            }
        }

        public List<T> ToList()
        {
            lock (_lock)
            {
                return new List<T>(_list);
            }
        }
    }
}
