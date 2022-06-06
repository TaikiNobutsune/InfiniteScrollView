using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace InfiniteScrollView
{
    public class SampleItemWithIfView : MonoBehaviour, IInfiniteScrollItem
    {
        [SerializeField] private Text _text;
        [SerializeField] private Button _button;
        
        public RectTransform RectTransform => (RectTransform)transform;
        public float Height => RectTransform.rect.height;
        public float Width => RectTransform.rect.width;
        public bool IsUpdated { get; private set; }
        public IObservable<int> OnClickedButton() => _button.OnClickAsObservable().Select(_ => DataIndex);

        public int DataIndex { get; private set; }
        
        public void UpdateItem<T>(T data)
        {
            _text.text = (data as SampleData)?.Number.ToString() ?? throw new InvalidCastException();
            IsUpdated = true;
        }

        public void UpdateDataIndex(int dataIndex)
        {
            DataIndex = dataIndex;
            IsUpdated = false;
        }

        public void Deactivate() => gameObject.SetActive(false);

        public void Activate() => gameObject.SetActive(true);
    }
}
