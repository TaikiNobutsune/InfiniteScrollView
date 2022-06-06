using System;
using TMPro.EditorUtilities;
using UnityEngine;
using UnityEngine.UI;

namespace InfiniteScrollView
{
    public class SampleItemView : InfiniteScrollBaseItemView
    {
        [SerializeField] private Text _text;

        public override bool IsUpdated { get; protected set; }

        public override void UpdateItem<T>(T data)
        {
            _text.text = (data as SampleData)?.Number.ToString() ?? throw new InvalidCastException();
            IsUpdated = true;
        }
    }

    public class SampleData
    {
        public int Number;

        public SampleData(int number)
        {
            Number = number;
        }
    }
}
