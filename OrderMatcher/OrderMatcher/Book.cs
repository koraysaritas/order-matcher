﻿using OrderMatcher.Types;
using System.Collections.Generic;
namespace OrderMatcher
{
    public class Book
    {
        private readonly Dictionary<OrderId, Order> _currentOrders;
        private readonly Side<QuantityTrackingPriceLevel> _bids;
        private readonly Side<QuantityTrackingPriceLevel> _asks;
        private readonly Side<PriceLevel> _stopBids;
        private readonly Side<PriceLevel> _stopAsks;

        private ulong _sequence;

        public IEnumerable<KeyValuePair<OrderId, Order>> CurrentOrders => _currentOrders;
        public IEnumerable<QuantityTrackingPriceLevel> BidSide => _bids.PriceLevels;
        public IEnumerable<QuantityTrackingPriceLevel> AskSide => _asks.PriceLevels;
        internal int AskPriceLevelCount => _asks.PriceLevelCount;
        internal int BidPriceLevelCount => _bids.PriceLevelCount;
        public IEnumerable<PriceLevel> StopBidSide => _stopBids.PriceLevels;
        public IEnumerable<PriceLevel> StopAskSide => _stopAsks.PriceLevels;
        public Price? BestBidPrice => _bids.BestPriceLevel?.Price;
        public Price? BestAskPrice => _asks.BestPriceLevel?.Price;
        public Quantity? BestBidQuantity => _bids.BestPriceLevel?.Quantity;
        public Quantity? BestAskQuantity => _asks.BestPriceLevel?.Quantity;
        public Price? BestStopBidPrice => _stopBids.BestPriceLevel?.Price;
        public Price? BestStopAskPrice => _stopAsks.BestPriceLevel?.Price;

        public Book()
        {
            var _priceComparerAscending = new PriceComparerAscending();
            var _priceComparerDescending = new PriceComparerDescending();

            _currentOrders = new Dictionary<OrderId, Order>();
            _bids = new Side<QuantityTrackingPriceLevel>(_priceComparerDescending, new PriceLevelComparerDescending<QuantityTrackingPriceLevel>());
            _asks = new Side<QuantityTrackingPriceLevel>(_priceComparerAscending, new PriceLevelComparerAscending<QuantityTrackingPriceLevel>());
            _stopBids = new Side<PriceLevel>(_priceComparerAscending, new PriceLevelComparerAscending<PriceLevel>());
            _stopAsks = new Side<PriceLevel>(_priceComparerDescending, new PriceLevelComparerDescending<PriceLevel>());
            _sequence = 0;
        }

        internal void RemoveOrder(Order order)
        {
            if (order.IsBuy)
            {
                bool removed = _bids.RemoveOrder(order, order.Price);
                if (!removed && order.IsStop)
                    _stopBids.RemoveOrder(order, order.StopPrice);
            }
            else
            {
                bool removed = _asks.RemoveOrder(order, order.Price);
                if (!removed && order.IsStop)
                    _stopAsks.RemoveOrder(order, order.StopPrice);
            }

            _currentOrders.Remove(order.OrderId);
        }

        internal void AddStopOrder(Order order)
        {
            order.Sequence = ++_sequence;
            var side = order.IsBuy ? _stopBids : _stopAsks;
            side.AddOrder(order, order.StopPrice);
            _currentOrders.Add(order.OrderId, order);
        }

        internal void AddOrderOpenBook(Order order)
        {
            order.Sequence = ++_sequence;
            var side = order.IsBuy ? _bids : _asks;
            side.AddOrder(order, order.Price);
            _currentOrders.Add(order.OrderId, order);
        }

        internal List<PriceLevel> RemoveStopAsks(Price price)
        {
            return RemoveFromCurrentOrders(_stopAsks.RemovePriceLevelTill(price));
        }

        internal List<PriceLevel> RemoveStopBids(Price price)
        {
            return RemoveFromCurrentOrders(_stopBids.RemovePriceLevelTill(price));
        }

        private List<PriceLevel> RemoveFromCurrentOrders(List<PriceLevel> priceLevels)
        {
            foreach (var priceLevel in priceLevels)
            {
                foreach (var order in priceLevel)
                {
                    _currentOrders.Remove(order.OrderId);
                }
            }
            return priceLevels;
        }

        internal bool TryGetOrder(OrderId orderId, out Order order)
        {
            return _currentOrders.TryGetValue(orderId, out order);
        }

        internal bool FillOrder(Order order, Quantity quantity)
        {
            var side = order.IsBuy ? _bids : _asks;
            if (side.FillOrder(order, quantity))
            {
                _currentOrders.Remove(order.OrderId);
                return true;
            }
            return false;
        }

        internal Order? GetBestBuyOrderToMatch(bool isBuy)
        {
            return isBuy ? _bids.BestPriceLevel?.First : _asks.BestPriceLevel?.First;
        }

        internal bool CheckCanFillOrder(bool isBuy, Quantity requestedQuantity, Price limitPrice)
        {
            var side = isBuy ? _asks : _bids;
            return side.CheckCanBeFilled(requestedQuantity, limitPrice);
        }

        internal bool CheckCanFillMarketOrderAmount(bool isBuy, Quantity orderAmount)
        {
            var side = isBuy ? _asks : _bids;
            return side.CheckMarketOrderAmountCanBeFilled(orderAmount);
        }
    }
}
