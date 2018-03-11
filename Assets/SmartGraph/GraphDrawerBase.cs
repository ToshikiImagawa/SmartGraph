using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace SmartGraph
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(CanvasRenderer), typeof(RectTransform))]
    public abstract class GraphDrawerBase : MaskableGraphic, IGraphDrawer
    {
        [SerializeField] private Vector2[] _points = Enumerable.Empty<Vector2>().ToArray();
        [SerializeField] private float _maxY = 100f;
        [SerializeField] private float _maxX = 100f;
        [SerializeField] private Vector2 _startPosition;

        private Vector2[] _viewPointPositions = Enumerable.Empty<Vector2>().ToArray();
        private Vector2[] _filterPointPositions = Enumerable.Empty<Vector2>().ToArray();
        private UIVertex[][] _viewUiVerticesGroup = Enumerable.Empty<UIVertex[]>().ToArray();

        protected WaitCallback waitcallback = new WaitCallback(ThreadFunc);
        protected readonly object VertexHelperLock = new object();
        protected readonly object PointsLock = new object();

        private Rect _rect = Rect.zero;
        private RectTransform _rectTransform;

        /// <summary>
        /// ポイント一覧
        /// </summary>
        public Vector2[] Points
        {
            get { return _points.ToArray(); }
        }

        /// <summary>
        /// Y軸最大値
        /// </summary>
        public float MaxY
        {
            get { return _maxY; }
            set
            {
                UpdateFilterPointPositions();
                _maxY = value;
            }
        }

        /// <summary>
        /// X軸最大値
        /// </summary>
        public float MaxX
        {
            get { return _maxX; }
            set
            {
                UpdateFilterPointPositions();
                _maxX = value;
            }
        }

        /// <summary>
        /// 表示開始位置
        /// </summary>
        public Vector2 StartPosition
        {
            get { return _startPosition; }
            set
            {
                UpdateViewPointPositions();
                _startPosition = value;
            }
        }

        /// <summary>
        /// 表示位置一覧
        /// </summary>
        protected Vector2[] ViewPointPositions
        {
            get { return _viewPointPositions; }
            private set { _viewPointPositions = value; }
        }

        protected Vector2 IntervalStartPosition
        {
            get
            {
                return new Vector2(
                    RectTransform.rect.width * RectTransform.pivot.x,
                    RectTransform.rect.height * RectTransform.pivot.y
                );
            }
        }
        protected RectTransform RectTransform
        {
            get { return _rectTransform ?? (_rectTransform = GetComponent<RectTransform>()); }
        }
        private float IntervalX
        {
            get { return RectTransform.rect.width / MaxX; }
        }
        private float IntervalY
        {
            get { return RectTransform.rect.height / MaxY; }
        }

        /// <summary>
        /// 点設定
        /// </summary>
        /// <param name="points"></param>
        public void SetPoints(IEnumerable<Vector2> points)
        {
            lock (PointsLock)
            {
                _points = points.ToArray();
            }
            UpdateFilterPointPositions();
        }

        /// <summary>
        /// 点追加
        /// </summary>
        /// <param name="point"></param>
        public void AddPoint(Vector2 point)
        {
            var points = _points.ToList();
            points.Add(point);
            lock (PointsLock)
            {
                _points = points.ToArray();
            }
            UpdateFilterPointPositions();
        }

        /// <summary>
        /// ViewPointPositionsの更新されたときに呼ばれるイベント
        /// </summary>
        protected abstract void OnUpdateViewPointPositions();

        /// <summary>
        /// ViewUiVerticesGroupを設定する
        /// </summary>
        /// <param name="vertices"></param>
        protected void SetViewUiVerticesGroup(IEnumerable<UIVertex[]> vertices)
        {
            lock (VertexHelperLock)
            {
                _viewUiVerticesGroup = vertices.ToArray();
            }
        }

        #region Base

        protected sealed override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            lock (VertexHelperLock)
            {
                foreach (var uiVertices in _viewUiVerticesGroup)
                {
                    vh.AddUIVertexQuad(uiVertices);
                }
            }
        }

        protected sealed override void Awake()
        {
            UpdateFilterPointPositions();
        }

        protected void Update()
        {
            if (_rect != RectTransform.rect)
            {
                _rect = RectTransform.rect;
                UpdateViewPointPositions();
            }
            if (_points == null) return;
            UpdateGeometry();
        }

#if UNITY_EDITOR
        protected sealed override void OnValidate()
        {
            UpdateFilterPointPositions();
        }
#endif

        #endregion

        private Vector2 ConvertPositionRect(Vector2 position)
        {
            return new Vector2((position.x - StartPosition.x) * IntervalX, (position.y - StartPosition.y) * IntervalY) - IntervalStartPosition;
        }

        private Vector2[] FilterViewPosition(IList<Vector2> positions)
        {
            if (positions == null) return null;
            if (positions.Count < 2) return positions.ToArray();
            var list = new List<Vector2> { positions.First() };
            for (var i = 1; i < positions.Count - 1; i++)
            {
                var pre = positions[i - 1];
                var now = positions[i];
                var next = positions[i + 1];

                if ((next - now).y * (now - pre).y <= 0 ||
                    IsInsideViewArea(pre) || IsInsideViewArea(now) || IsInsideViewArea(next))
                {
                    list.Add(now);
                }
            }
            list.Add(positions.Last());
            return list.ToArray();
        }

        private void UpdateFilterPointPositions()
        {
            _filterPointPositions = FilterViewPosition(_points);
            UpdateViewPointPositions();
        }

        private void UpdateViewPointPositions()
        {
            ViewPointPositions = _filterPointPositions.Select(ConvertPositionRect).ToArray();
            OnUpdateViewPointPositions();
        }

        private bool IsInsideViewArea(Vector2 position)
        {
            return position.x >= 0 + StartPosition.x &&
                   position.y >= 0 + StartPosition.y &&
                   position.x <= MaxX + StartPosition.x &&
                   position.y <= MaxY + StartPosition.y;
        }

        private static void ThreadFunc(System.Object obj)
        {
            var worker = obj as WorkerClass;
            if (worker != null) worker.Invoke();
        }

        protected class WorkerClass
        {
            private System.Action _worker;

            public WorkerClass(System.Action worker)
            {
                _worker = worker;
            }

            public void Invoke()
            {
                if (_worker != null && _worker.Target != null) _worker.Invoke();
            }
        }
    }

    public interface IGraphDrawer
    {
        /// <summary>
        /// ポイント一覧
        /// </summary>
        Vector2[] Points { get; }

        /// <summary>
        /// Y軸最大値
        /// </summary>
        float MaxY { get; set; }

        /// <summary>
        /// X軸最大値
        /// </summary>
        float MaxX { get; set; }

        /// <summary>
        /// 表示開始位置
        /// </summary>
        Vector2 StartPosition { get; set; }

        /// <summary>
        /// 点設定
        /// </summary>
        /// <param name="points"></param>
        void SetPoints(IEnumerable<Vector2> points);

        /// <summary>
        /// 点追加
        /// </summary>
        /// <param name="point"></param>
        void AddPoint(Vector2 point);
    }
}