namespace AbilityKit.Battle.SearchTarget
{
    internal sealed class SearchHitBuffer
    {
        private SearchHit[] _items;

        public SearchHit[] Items => _items;

        public void EnsureCapacity(int capacity)
        {
            if (capacity <= 0)
            {
                _items = null;
                return;
            }

            if (_items == null || _items.Length < capacity)
            {
                _items = new SearchHit[capacity];
            }
        }

        public void Reset()
        {
        }
    }
}
