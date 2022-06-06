using System;
using UnityEngine;

namespace InfiniteScrollView
{
    public interface IInfiniteScrollItem
    {
        public RectTransform RectTransform { get; }
        public float Height { get; }
        public float Width { get; }
        public bool IsUpdated { get; }
        public IObservable<int> OnClickedButton();
        public int DataIndex { get; }
        public void UpdateItem<T>(T data);
        public void UpdateDataIndex(int dataIndex);
        public void Deactivate();
        public void Activate();
    }
}
