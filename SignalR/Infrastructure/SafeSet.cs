﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SignalR.Infrastructure
{
	internal class SafeSet<T>
	{
		private readonly ConcurrentDictionary<T, object> _items;

		public SafeSet()
		{
			_items = new ConcurrentDictionary<T, object>();
		}

		public SafeSet(IEqualityComparer<T> comparer)
		{
			_items = new ConcurrentDictionary<T, object>(comparer);
		}

		public SafeSet(IEnumerable<T> items)
		{
			_items = new ConcurrentDictionary<T, object>(items.Select(x => new KeyValuePair<T, object>(x, null)));
		}

		public IEnumerable<T> GetSnapshot()
		{
			return _items.Select(x => x.Key);
		}

		public void Add(T item)
		{
			_items.TryAdd(item, null);
		}

		public void Remove(T item)
		{
			object _;
			_items.TryRemove(item, out _);
		}
	}
}
