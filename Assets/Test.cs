using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;
using UnityEngine.UI;

namespace InfiniteScrollView
{
    public class Test : MonoBehaviour
    {
        [SerializeField] private InfiniteScrollView _infiniteScrollView;
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

        void Start()
        {
            var data = new List<SampleData>();
            
            for (int i = 0; i < 100; i++)
                data.Add(new SampleData(i));
            
            _infiniteScrollView.Setup(data.Count, view =>
            {
                view.UpdateItem(data[view.DataIndex]);
            });

            _infiniteScrollView.OnUpdateItemEvent.Subscribe(x =>
            {
                x.UpdateItem(data[x.DataIndex]);
            });

            _infiniteScrollView.OnReachedEdge.Subscribe(x => Debug.Log(x));

            _infiniteScrollView.OnClickedItem.Subscribe(x => Debug.Log($"Index {x}"));

            _addDataButton.OnClickAsObservable().Subscribe(_ =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var lastNumber = data.Last().Number;
                    data.Add(new SampleData(lastNumber + 1));
                }

                _infiniteScrollView.ResizeItem(data.Count);
            });

            _removeDataButton.OnClickAsObservable().Subscribe(_ =>
            {
                var removeIndex = int.Parse(_inputField.text);
                var index = data.FindIndex(x => x.Number == removeIndex);
                if (index >= 0) data.RemoveAt(index);
                _infiniteScrollView.ResizeItem(data.Count);
            });

            _resetButton.OnClickAsObservable().Subscribe(_ =>
            {
                _infiniteScrollView.ResetItem();
            });

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
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
