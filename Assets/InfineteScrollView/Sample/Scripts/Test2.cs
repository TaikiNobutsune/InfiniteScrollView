using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace InfiniteScrollView
{
    public class Test2 : MonoBehaviour
    {
        [SerializeField] private InfiniteScrollViewWithIf _infiniteScrollView;
        [SerializeField] private Button _addDataButton;
        [SerializeField] private InputField _inputField;
        [SerializeField] private Button _removeDataButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private InputField _inputField1;
        [SerializeField] private Button _adjustTopButton;
        [SerializeField] private InputField _inputField2;
        [SerializeField] private Button _adjustCenterButton;
        [SerializeField] private InputField _inputField3;
        [SerializeField] private Button _adjustBottomButton;
        [SerializeField] private Button _switchData;

        private readonly List<SampleData> _incrementNumberData = new();
        private readonly List<SampleData> _minusNumberData = new();

        private List<SampleData> _data => _flag switch
        {
            true => _incrementNumberData,
            false => _minusNumberData,
        };
        private bool _flag = true;

        void Start()
        {
            for (int i = 0; i < 100; i++)
            {
                _incrementNumberData.Add(new SampleData(i));
                if (i < 10) _minusNumberData.Add(new SampleData(-i));
            }

            _infiniteScrollView.Setup(_data.Count);

            _infiniteScrollView.OnUpdateItemEvent.Subscribe(x => { x.UpdateItem(_data[x.DataIndex]); });

            _infiniteScrollView.OnReachedEdge.Subscribe(x => Debug.Log(x));

            _infiniteScrollView.OnClickedItem.Subscribe(x => Debug.Log($"Index {x}"));

            _addDataButton.OnClickAsObservable().Subscribe(_ =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var lastNumber = _data.Last().Number;
                    _data.Add(new SampleData(lastNumber + 1));
                }

                _infiniteScrollView.ResizeData(_data.Count);
            });

            _removeDataButton.OnClickAsObservable().Subscribe(_ =>
            {
                var removeIndex = int.Parse(_inputField.text);
                var index = _data.FindIndex(x => x.Number == removeIndex);
                if (index >= 0) _data.RemoveAt(index);
                _infiniteScrollView.ResizeData(_data.Count);
            });

            _resetButton.OnClickAsObservable().Subscribe(_ => { _infiniteScrollView.ReLayout(); });

            _adjustTopButton.OnClickAsObservable().Subscribe(_ =>
            {
                var index = int.Parse(_inputField1.text);
                _infiniteScrollView.RedrawTopWithSelectedIndex(index);
            });

            _adjustCenterButton.OnClickAsObservable().Subscribe(_ =>
            {
                var index = int.Parse(_inputField2.text);
                _infiniteScrollView.RedrawCenterWithSelectedIndex(index);
            });

            _adjustBottomButton.OnClickAsObservable().Subscribe(_ =>
            {
                var index = int.Parse(_inputField3.text);
                _infiniteScrollView.RedrawBottomWithSelectedIndex(index);
            });

            _switchData.OnClickAsObservable().Subscribe(_ =>
            {
                _flag = !_flag;
                _infiniteScrollView.Setup(_data.Count);
            });
        }
    }
}
