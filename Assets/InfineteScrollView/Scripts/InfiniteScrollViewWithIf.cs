using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace InfiniteScrollView
{
    [RequireComponent(typeof(ScrollRect))]
    public class InfiniteScrollViewWithIf : MonoBehaviour, IDisposable
    {
        /// <summary>
        /// prefab同士のSpace
        /// </summary>
        [SerializeField] private float _spacing;

        /// <summary>
        /// スクロール方向
        /// </summary>
        [SerializeField] private Direction _direction;

        /// <summary>
        /// topのマージン
        /// </summary>
        [SerializeField] private float _topMargin;

        /// <summary>
        /// bottomのマージン
        /// </summary>
        [SerializeField] private float _bottomMargin;

        /// <summary>
        /// アイテムのベース
        /// </summary>
        [SerializeField] private GameObject _itemGO;

        [SerializeField] private RectTransform _contentRectTransform;

        /// <summary>
        /// InstantiateしたPrefabの数
        /// </summary>
        private int _prefabCount => _views.Count;

        private ScrollRect _scrollRect;
        private bool _isInitialized;
        private int _dataLength;
        private float _itemSize;
        private int _maxVisibleItemCount;
        private int _maxPrefabCount;
        private float _viewPortAreaSize;
        private Vector2 _bufferScrollBarPosition;
        private readonly List<IInfiniteScrollItem> _views = new();
        private readonly Dictionary<int, IDisposable> _disposables = new();
        private readonly Subject<IInfiniteScrollItem> _updateItemSubject = new();
        private readonly Subject<Edge> _reachedEdge = new();
        private readonly Subject<int> _clickedItem = new();

        public IObservable<IInfiniteScrollItem> OnUpdateItemEvent => _updateItemSubject;
        public IObservable<Edge> OnReachedEdge => _reachedEdge;
        public IObservable<int> OnClickedItem => _clickedItem;

        public void Setup(
            int dataLength,
            Action<IInfiniteScrollItem> instantiateCompleted
        )
        {
            _dataLength = dataLength;
            if(!_isInitialized) Initialize();
            var instantiateViewCount = _dataLength >= _maxPrefabCount ? _maxPrefabCount - _prefabCount : _dataLength - _prefabCount;
        
            //Prefabを任意数生成
            for (var i = 0; i < instantiateViewCount; i++)
            {
                var view =　InstantiateItemView();
            }
            
            InitializePrefabs();
            
            if(_isInitialized) Render();
            else
                for (var i = 0; i < _prefabCount; i++)
                {
                    var view = _views[i];
                    if (i < _maxVisibleItemCount && view.DataIndex >= 0 && view.DataIndex < _dataLength)
                    {
                        instantiateCompleted.Invoke(view);
                        view.Activate();
                    }
                    else view.Deactivate();
                }
            
            ResizeScrollArea(_dataLength);
        }
        
        /// <summary>
        /// 選択したIndexを上辺合わせするように描画
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void RedrawTopWithSelectedIndex(int index)
        {
            if (index < 0 || index >= _dataLength) throw new ArgumentException();

            //下辺合わせにした時に描画できるアイテム最大数を求める
            var itemCount = (int)((_viewPortAreaSize - _bottomMargin + _spacing) / (_itemSize + _spacing));
            var isValidIndex = index < _dataLength - itemCount;
            //上辺合わせにできないので、下辺合わせ
            if (!isValidIndex)
            {
                RedrawBottomWithSelectedIndex(_dataLength - 1);
                return;
            }

            var dataIndex = index;
            foreach (var view in _views)
            {
                view.RectTransform.anchoredPosition = CalcPositionWithIndex(dataIndex);
                view.UpdateDataIndex(dataIndex);
                dataIndex++;
            }
            
            Render();
            
            _contentRectTransform.anchoredPosition = _direction switch
            {
                Direction.Vertical => new Vector2(0, _topMargin + (_itemSize + _spacing) * index),
                Direction.Horizontal => new Vector2(_topMargin + (_itemSize + _spacing) * index, 0),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
       
        /// <summary>
        /// 選択したIndexを下辺合わせするように描画
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void RedrawCenterWithSelectedIndex(int index)
        {
            if (index < 0 || index >= _dataLength) throw new ArgumentException();
            
            var itemCount = (int)(((_viewPortAreaSize - _topMargin + _spacing) / (_itemSize + _spacing))/2 + 1);
            var isValidIndex = index > itemCount - 1 || index < _dataLength - itemCount;
            if (!isValidIndex)
            {
                RedrawTopWithSelectedIndex(0);
                return;
            }

            var topIndex = index - itemCount + 1;
            foreach (var view in _views)
            {
                view.RectTransform.anchoredPosition = CalcPositionWithIndex(topIndex);
                view.UpdateDataIndex(topIndex);
                topIndex++;
            }
            
            Render();

            _contentRectTransform.anchoredPosition = _direction switch
            {
                Direction.Vertical => new Vector2(0, _topMargin + (_itemSize + _spacing) * index - (_viewPortAreaSize - _itemSize) / 2),
                Direction.Horizontal => new Vector2(_topMargin + (_itemSize + _spacing) * index - (_viewPortAreaSize - _itemSize) / 2, 0),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        /// 選択したIndexを中心合わせするように描画
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void RedrawBottomWithSelectedIndex(int index)
        {
            if (index < 0 || index >= _dataLength) throw new ArgumentException();
            
            //上辺合わせにした時に美ょグアできるアイテム最大数を求める
            var itemCount = (int)((_viewPortAreaSize - _topMargin + _spacing) / (_itemSize + _spacing));
            var isValidIndex = index > itemCount - 1;
            if (!isValidIndex)
            {
                RedrawTopWithSelectedIndex(0);
                return;
            }

            var topIndex = index - itemCount - 1;
            foreach (var view in _views)
            {
                view.RectTransform.anchoredPosition = CalcPositionWithIndex(topIndex);
                view.UpdateDataIndex(topIndex);
                topIndex++;
            }
            
            Render();

            _contentRectTransform.anchoredPosition = _direction switch
            {
                Direction.Vertical => new Vector2(0, _topMargin + (_itemSize + _spacing) * index - (_viewPortAreaSize -_itemSize)),
                Direction.Horizontal => new Vector2(_topMargin + (_itemSize + _spacing) * index - (_viewPortAreaSize - _itemSize), 0),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        /// <summary>
        /// Prefab全ての描画処理・表示、非表示をする
        /// </summary>
        public void Render()
        {
            for (var i = 0; i < _prefabCount; i++)
            {
                var view = _views[i];
                if (i < _maxVisibleItemCount && view.DataIndex >= 0 && view.DataIndex < _dataLength)
                {
                    _updateItemSubject.OnNext(view);
                    view.Activate();
                }
                else view.Deactivate();
            }
        }
        
        public void ResizeItem(int dataLength)
        {
            if (dataLength > _dataLength && _prefabCount < _maxPrefabCount)
            {
                var diff = _maxPrefabCount - _prefabCount;
                for (var i = 0; i < diff; i++)
                {
                    var lastView = _views.Last();
                    var view = InstantiateItemView();
                    SetBottomPosition(view.RectTransform, lastView.RectTransform);
                    view.UpdateDataIndex(lastView.DataIndex + 1);
                }
            }
            
            _dataLength = dataLength;

            Render();

            ResizeScrollArea(_dataLength);
        }
        
        /// <summary>
        /// 初期配置・データにリセット
        /// </summary>
        public void ResetItem()
        {
            InitializePrefabs();
            Render();
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables.Values) disposable.Dispose();
        }
        
        private void Initialize()
        {
            _scrollRect = GetComponent<ScrollRect>();
            var item = _itemGO.GetComponent<IInfiniteScrollItem>() ?? throw new InvalidOperationException("Interfaceを継承してください");
            switch (_direction)
            {
                case Direction.Horizontal:
                    _itemSize = item.Width;
                    _viewPortAreaSize = _scrollRect.viewport.rect.width;
                    break;
                case Direction.Vertical:
                    _itemSize = item.Height;
                    _viewPortAreaSize = _scrollRect.viewport.rect.height;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            _maxVisibleItemCount = (int)Math.Ceiling(_viewPortAreaSize / (_itemSize + _spacing)) + 2;
            _maxPrefabCount = _maxVisibleItemCount + 5;
            
            _scrollRect.onValueChanged.AddListener(UpdateItem);
        }
        
        private IInfiniteScrollItem InstantiateItemView()
        {
            if (_prefabCount > _maxPrefabCount) throw new InvalidOperationException("最大生成数を超えています");
            
            var go = Instantiate(_itemGO, _contentRectTransform);
            var view = go.GetComponent<IInfiniteScrollItem>() ?? throw new InvalidOperationException("interfaceを継承していません");
            
            view.RectTransform.anchorMin = new Vector2(0.5f, 1);
            view.RectTransform.anchorMax = new Vector2(0.5f, 1);
            view.RectTransform.pivot = _direction switch
            {
                Direction.Horizontal => new Vector2(0, 0.5f),
                Direction.Vertical => new Vector2(0.5f, 1),
                _ => throw new ArgumentOutOfRangeException(),
            };
            _disposables.Add(go.GetInstanceID(), view.OnClickedButton().Subscribe(x => _clickedItem.OnNext(view.DataIndex)));
            _views.Add(view);

            return view;
        }
        
        /// <summary>
        /// ScrollArea, Prefab, DataIndexを初期化
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void InitializePrefabs()
        {
            for (var i = 0; i < _prefabCount; i++)
            {
                var view = _views[i];
                var rect = view.RectTransform;
                switch (_direction)
                {
                    case Direction.Horizontal:
                        rect.pivot = new Vector2(0, 0.5f);
                        var anchorX = i * (_itemSize + _spacing) + _topMargin;
                        rect.anchoredPosition =
                            new Vector2(anchorX, 0);
                        break;
                    case Direction.Vertical:
                        rect.pivot = new Vector2(0.5f, 1);
                        var anchorY = -i * (_itemSize + _spacing) - _topMargin;
                        rect.anchoredPosition =
                            new Vector2(0, anchorY);
                        break;
                    default: throw new InvalidOperationException("Unknown Type");
                }
                
                view.UpdateDataIndex(i);
            }
            
            //ScrollRectを初期位置に更新
            _contentRectTransform.anchoredPosition = Vector2.zero;
        }
        
        /// <summary>
        /// Prefabを一番下のPositionに移動させる
        /// </summary>
        /// <param name="targetRectTransform"></param>
        /// <param name="currentBottomRectTransform"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void SetBottomPosition(RectTransform targetRectTransform, RectTransform currentBottomRectTransform)
        {
            var anchoredPosition = currentBottomRectTransform.anchoredPosition;
            targetRectTransform.anchoredPosition = _direction switch
            {
                Direction.Horizontal => new Vector2(
                    anchoredPosition.x - (_itemSize + _spacing),
                    0
                ),
                Direction.Vertical => new Vector2(
                    0,
                    anchoredPosition.y - (_itemSize + _spacing)
                ),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        /// Prefabを一番上のPositionに移動させる
        /// </summary>
        /// <param name="targetRectTransform"></param>
        /// <param name="currentBottomRectTransform"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void SetTopPosition(RectTransform targetRectTransform, RectTransform currentBottomRectTransform)
        {
            var anchoredPosition = currentBottomRectTransform.anchoredPosition;
            targetRectTransform.anchoredPosition = _direction switch
            {
                Direction.Horizontal => new Vector2(
                    anchoredPosition.x + (_itemSize + _spacing),
                    0
                ),
                Direction.Vertical => new Vector2(
                    0,
                    anchoredPosition.y + (_itemSize + _spacing)
                ),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        /// <summary>
        /// PrefabのIndexから適切なPositionを返す
        /// 上辺起点
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private Vector2 CalcPositionWithIndex(int index) =>
            _direction switch
            {
                Direction.Vertical => new Vector2(0, -(_topMargin + (_itemSize + _spacing) * index)),
                Direction.Horizontal => new Vector2(-(_topMargin + (_itemSize + _spacing) * index), 0),
                _ => throw new ArgumentOutOfRangeException()
            };

        /// <summary>
        /// ScrollViewのサイズを調整する
        /// </summary>
        /// <param name="dataLength"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void ResizeScrollArea(int dataLength)
        {
            var contentAreaSize = dataLength * (_itemSize + _spacing) + _topMargin + _bottomMargin - _spacing;
            var sizeDelta = _scrollRect.content.sizeDelta;
            
            _scrollRect.content.sizeDelta = _direction switch
            {
                Direction.Horizontal => new Vector2(
                    contentAreaSize,
                    sizeDelta.y
                ),
                Direction.Vertical => new Vector2(
                    sizeDelta.x, 
                    contentAreaSize
                ),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
        
        private void UpdateItem(Vector2 scrollBar)
        {
            if(_prefabCount == 0) return;

            var topIndex = GetTopItemIndex();
            var bottomIndex = GetBottomItemIndex();
            
            var isChanged = false;
            
            //後方向
            var isBack = _direction switch
            {
                Direction.Vertical => scrollBar.y < _bufferScrollBarPosition.y,
                Direction.Horizontal => scrollBar.x < _bufferScrollBarPosition.x,
                _ => throw new ArgumentException(),
            };
            if (isBack && bottomIndex <= _dataLength)
            {
                var overFlowViews = _views.Where(x => x.DataIndex < topIndex).OrderBy(x => x.DataIndex).ToArray();
                foreach (var view in overFlowViews)
                {
                    var lastView = _views.OrderByDescending(x => x.DataIndex).First();
                    SetBottomPosition(view.RectTransform, lastView.RectTransform);
                    view.UpdateDataIndex(lastView.DataIndex + 1);
                    isChanged = true;
                }
            }
            
            //前方向
            var isFront = _direction switch
            {
                Direction.Vertical => scrollBar.y > _bufferScrollBarPosition.y,
                Direction.Horizontal => scrollBar.x > _bufferScrollBarPosition.x,
                _ => throw new ArgumentException(),
            };
            if (isFront && topIndex >= 0 && _views[0].DataIndex > topIndex)
            {
                var diff = _views[0].DataIndex - topIndex;
                for (int i = 0; i < diff; i++)
                {
                    var topDataIndex = _views[0].DataIndex;
                    var targetView = _views.Last();
                    SetTopPosition(targetView.RectTransform, _views[0].RectTransform);
                    targetView.UpdateDataIndex(topDataIndex - 1);
                    isChanged = true;
                }
            }

            if (isChanged)
            {
                _views.Sort((a, b) => a.DataIndex - b.DataIndex);
                
                //描画更新
                var updateViews = _views.Where(x => !x.IsUpdated && x.DataIndex >= 0 && x.DataIndex < _dataLength);
                foreach (var view in updateViews) _updateItemSubject.OnNext(view);
            
                //非表示
                for (int i = 0; i < _prefabCount; i++)
                {
                    var view = _views[i];
                    if (i < _maxVisibleItemCount && view.DataIndex >= 0 && view.DataIndex < _dataLength) view.Activate();
                    else view.Deactivate();
                }
            }
            
            DetectEdge(scrollBar);
            _bufferScrollBarPosition = scrollBar;
        }
        
        /// <summary>
        /// 末端に位置がきたことのイベントを発火する
        /// </summary>
        /// <param name="scrollBar"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void DetectEdge(Vector2 scrollBar)
        {
            switch (_direction)
            {
                case Direction.Horizontal:
                    switch (scrollBar.x)
                    {
                        case <= 0f:
                            _reachedEdge.OnNext(Edge.Front);
                            break;
                        case >= 1f:
                            _reachedEdge.OnNext(Edge.Back);
                            break;
                    }
                    break;
                case Direction.Vertical:
                    switch (scrollBar.y)
                    {
                        case >= 1f:
                            _reachedEdge.OnNext(Edge.Front);
                            break;
                        case <= 0f:
                            _reachedEdge.OnNext(Edge.Back);
                            break;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        /// <summary>
        /// ViewAreaの場所を計算して上辺と被っているPrefabの表示されるべきDataIndexを返す
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private int GetTopItemIndex()
        {
            Vector3 localPosition;
            return _direction switch
            {
                Direction.Horizontal => (localPosition = _scrollRect.content.localPosition).x < 0f
                    ? 0 
                    : (int)System.Math.Truncate((localPosition.x - _topMargin) / (_itemSize + _spacing)),
                Direction.Vertical => (localPosition = _scrollRect.content.localPosition).y < 0f 
                    ? 0 
                    : (int)System.Math.Truncate((localPosition.y - _topMargin) / (_itemSize + _spacing)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        /// ViewAreaの場所を計算して下辺と被っているPrefabの表示されるべきDataIndexを返す
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private int GetBottomItemIndex()
        {
            Vector3 localPosition;
            return _direction switch
            {
                Direction.Horizontal => (localPosition = _scrollRect.content.localPosition).x < 0f
                    ? 0
                    : (int)Math.Truncate((localPosition.x + _viewPortAreaSize + _topMargin) / (_itemSize + _spacing)),
                Direction.Vertical => (localPosition = _scrollRect.content.localPosition).y < 0f 
                    ? 0 
                    : (int)Math.Truncate((localPosition.y + _viewPortAreaSize - _topMargin) / (_itemSize + _spacing)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
