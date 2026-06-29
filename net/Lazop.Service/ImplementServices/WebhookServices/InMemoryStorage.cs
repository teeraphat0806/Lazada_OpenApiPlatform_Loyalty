using Lazop.Domain.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lazop.Service.ImplementServices.WebhookServices
{
    public static class InMemoryStorage
    {
        public static ConcurrentList<LazadaMessage> LazadaMessages { get; } = new ConcurrentList<LazadaMessage>();
        public static ConcurrentDictionary<string, LazadaOrder> LazadaOrders { get; } = new ConcurrentDictionary<string, LazadaOrder>();
        public static ConcurrentDictionary<string, LazadaReverseOrder> LazadaReverseOrders { get; } = new ConcurrentDictionary<string, LazadaReverseOrder>();
        public static ConcurrentDictionary<string, LazadaAccessToken> LazadaAccessTokens { get; } = new ConcurrentDictionary<string, LazadaAccessToken>();
        public static ConcurrentDictionary<string, LazadaSeller> LazadaSellers { get; } = new ConcurrentDictionary<string, LazadaSeller>();
        public static ConcurrentDictionary<long, LazadaProduct> LazadaProducts { get; } = new ConcurrentDictionary<long, LazadaProduct>();
        public static ConcurrentDictionary<long, LazadaProductSku> LazadaProductSkus { get; } = new ConcurrentDictionary<long, LazadaProductSku>();
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
