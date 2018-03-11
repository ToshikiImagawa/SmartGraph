using System.Collections.Generic;
using UnityEngine;

namespace SmartGraph
{
    public class AreaGraphDrawer : GraphDrawerBase
    {
        [SerializeField] private DrawingType _drawingType;
        [SerializeField] private bool _isReverse;
        
        private Vector2 _drawPoint;

        /// <summary>
        /// 描画方法
        /// </summary>
        public DrawingType Drawing
        {
            get { return _drawingType; }
            set
            {
                _drawingType = value;
                OnUpdateViewPointPositions();
            }
        }

        /// <summary>
        /// 反転
        /// </summary>
        public bool IsReverse
        {
            get { return _isReverse; }
            set
            {
                _isReverse = value;
                OnUpdateViewPointPositions();
            }
        }

        protected override void OnUpdateViewPointPositions()
        {
            _drawPoint = IsReverse ? new Vector2(
                    - IntervalStartPosition.x + RectTransform.rect.width,
                    - IntervalStartPosition.y + RectTransform.rect.height
                ) : - IntervalStartPosition;

            if (ThreadPool.Instance == null)
            {
                ThreadPool.InitInstance();
            }
            ThreadPool.QueueUserWorkItem(waitcallback, new WorkerClass(DoHeavyProcess));
        }

        private void DoHeavyProcess()
        {
            lock (VertexHelperLock)
            {
                SetViewUiVerticesGroup(
                    CreateUiVertexsGroup(ViewPointPositions, color, Drawing, IsReverse, _drawPoint)
                );
            }
        }

        private static IEnumerable<UIVertex[]> CreateUiVertexsGroup(IList<Vector2> points, Color color, DrawingType drawingType, bool isReverse, Vector2 startPosition)
        {
            var vertex = new UIVertex { color = color };
            var queVertexs = new List<UIVertex[]>();
            for (var i = 0; i < points.Count - 1; i++)
            {
                var vertexs = new List<UIVertex>();
                // Pointを定義
                var point1 = points[i + 0];
                var point2 = points[i + 1];

                switch (drawingType)
                {
                    case DrawingType.Vertical:
                        if (isReverse)
                        {
                            vertex.position = point1;
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = new Vector2(point2.x, startPosition.y);
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = new Vector2(point1.x, startPosition.y);
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);
                            queVertexs.Add(vertexs.ToArray());
                        }
                        else
                        {
                            vertex.position = new Vector2(point1.x, startPosition.y);
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = new Vector2(point2.x, startPosition.y);
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = point1;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);
                            queVertexs.Add(vertexs.ToArray());
                        }
                        break;
                    case DrawingType.Horizontal:
                        if (isReverse)
                        {
                            vertex.position = point1;
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = new Vector2(startPosition.x, point1.y);
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = new Vector2(startPosition.x, point2.y);
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = point2;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);
                            queVertexs.Add(vertexs.ToArray());
                        }
                        else
                        {
                            vertex.position = new Vector2(startPosition.x, point1.y);
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = point1;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = new Vector2(startPosition.x, point2.y);
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);
                            queVertexs.Add(vertexs.ToArray());
                        }
                        break;
                }


            }
            return queVertexs.ToArray();
        }

        public enum DrawingType
        {
            Vertical,
            Horizontal
        }
    }
}