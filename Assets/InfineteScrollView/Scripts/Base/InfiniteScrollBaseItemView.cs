using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace InfiniteScrollView
{
    [RequireComponent(typeof(Button))]
    public abstract class InfiniteScrollBaseItemView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        public RectTransform RectTransform => (RectTransform)transform;
        public float Height => RectTransform.rect.height;
        public float Width => RectTransform.rect.width;
        public abstract bool IsUpdated { get; protected set; }
        public IObservable<Unit> OnClickedButton => _button.OnClickAsObservable();

        /// <summary>
        /// 表示しているデータの配列のIndex
        /// データを切り替える際に必ず更新
        /// </summary>
        public int DataIndex { get; private set; }

        /// <summary>
        /// Itemの表示更新
        /// </summary>
        public abstract void UpdateItem<T>(T data);

        public void UpdateDataIndex(int dataIndex)
        {
            DataIndex = dataIndex;
            IsUpdated = false;
        }

        public void Deactivate() => gameObject.SetActive(false);
        public void Activate() => gameObject.SetActive(true);
    }
}
