using Microsoft.WindowsAzure.Storage.Table;
using System.Collections;
using System.Collections.Generic;

namespace CryptoDistributor
{
    public class TableQueryResult<T> : IList<T>
    {
        private readonly IList<T> result;

        public TableQueryResult(IList<T> result, TableContinuationToken continuationToken)
        {
            this.result = result;
            this.ContinuationToken = continuationToken;
        }

        public T this[int index] { get => this.result[index]; set => this.result[index] = value; }

        public int Count => this.result.Count;

        public bool IsReadOnly => this.result.IsReadOnly;

        public TableContinuationToken ContinuationToken { get; }

        public void Add(T item) => this.result.Add(item);

        public void Clear() => this.result.Clear();

        public bool Contains(T item) => this.result.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => this.result.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => this.result.GetEnumerator();

        public int IndexOf(T item) => this.result.IndexOf(item);

        public void Insert(int index, T item) => this.result.Insert(index, item);

        public bool Remove(T item) => this.result.Remove(item);

        public void RemoveAt(int index) => this.result.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => this.result.GetEnumerator();
    }
}
