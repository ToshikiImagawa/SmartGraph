using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartGraph
{
    public class LineGraphDrawer : GraphDrawerBase
    {
        [SerializeField] private float _width = 5f;
        [SerializeField] private CornerType _corner;
        [SerializeField] private CapType _cap;

        /// <summary>
        /// 角
        /// </summary>
        public CornerType Corner
        {
            get { return _corner; }
            set
            {
                _corner = value;
                OnUpdateViewPointPositions();
            }
        }

        /// <summary>
        /// 端
        /// </summary>
        public CapType Cap
        {
            get { return _cap; }
            set
            {
                _cap = value;
                OnUpdateViewPointPositions();
            }
        }

        /// <summary>
        /// 幅
        /// </summary>
        public float Width
        {
            get { return Mathf.Abs(_width); }
            set
            {
                _width = value;
                OnUpdateViewPointPositions();
            }
        }

        // 線を太らせる方向を計算する
        private static Vector2 CalcNormal(Vector2? prev, Vector2 current, Vector2? next)
        {
            var dir = Vector2.zero;
            if (prev.HasValue) dir += (prev.Value - current).normalized;
            if (next.HasValue) dir += (current - next.Value).normalized;
            dir = new Vector2(-dir.y, dir.x).normalized;
            return dir;
        }

        private static IEnumerable<UIVertex[]> CreateUiVertexsGroup(IList<Vector2> points, float width,
            Color color, CapType cap, CornerType corner)
        {
            var vertex = new UIVertex {color = color};

            var queVertexs = new List<UIVertex[]>();
            for (var i = 0; i < points.Count - 1; i++)
            {
                var vertexs = new List<UIVertex>();
                // Pointを定義
                Vector2? point0;
                var point1 = points[i + 0];
                var point2 = points[i + 1];
                Vector2? point3;

                if (i - 1 < 0) point0 = null;
                else point0 = points[i - 1];

                if (i + 2 > points.Count - 1) point3 = null;
                else point3 = points[i + 2];

                var defaultNormal = (point1 - point2).normalized;
                var defaultCalcNormal = new Vector2(-defaultNormal.y, defaultNormal.x);
                {
                    var normal2 = CalcNormal(point1, point2, point3);
                    var normal1 = CalcNormal(point0, point1, point2);

                    var innerProduct1 = Vector2.Dot(defaultCalcNormal, normal1);
                    var innerProduct2 = Vector2.Dot(defaultCalcNormal, normal2);

                    var upPosition1 = defaultNormal - (normal1 + defaultCalcNormal).normalized;
                    var downPosition1 = defaultNormal + (normal1 + defaultCalcNormal).normalized;
                    var upPosition2 = defaultNormal - (normal2 + defaultCalcNormal).normalized;
                    var downPosition2 = defaultNormal + (normal2 + defaultCalcNormal).normalized;

                    var isUp1 = upPosition1.magnitude >= downPosition1.magnitude;
                    var isUp2 = upPosition2.magnitude >= downPosition2.magnitude;
                    
                    var width1 = width / innerProduct1;
                    var width2 = width / innerProduct2;

                    if (!point0.HasValue)
                    {
                        AddCap(point1, point2, defaultNormal, defaultCalcNormal, width, false, cap, vertex,
                            queVertexs);
                    }
                    else
                    {
                        var upPosition = -defaultNormal - (normal1 + defaultCalcNormal).normalized;
                        var downPosition = -defaultNormal + (normal1 + defaultCalcNormal).normalized;
                        var clockwise = upPosition.magnitude >= downPosition.magnitude;
                        AddCorner(point1, point2, defaultNormal, defaultCalcNormal, normal1, width, false, clockwise, corner,
                            vertex,
                            queVertexs);
                    }

                    switch (corner)
                    {
                        case CornerType.Miter:
                        {
                            vertex.position = point1 - normal1 * width1;
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2 - normal2 * width2;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2 + normal2 * width2;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = point1 + normal1 * width1;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);
                            queVertexs.Add(vertexs.ToArray());
                            vertexs.Clear();
                            break;
                        }
                        case CornerType.Round:
                        case CornerType.Bevel:
                        {
                            vertex.position = isUp1 ? point1 - defaultCalcNormal * width : point1 - normal1 * width1;
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = isUp2 ? point2 - normal2 * width2 : point2 - defaultCalcNormal * width;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = isUp2 ? point2 + defaultCalcNormal * width : point2 + normal2 * width2;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = isUp1 ? point1 + normal1 * width1 : point1 + defaultCalcNormal * width;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);
                            queVertexs.Add(vertexs.ToArray());
                            vertexs.Clear();
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException(corner.GetType().Name, corner, null);
                    }

                    if (!point3.HasValue)
                    {
                        AddCap(point1, point2, defaultNormal, defaultCalcNormal, width, true, cap, vertex,
                            queVertexs);
                    }
                    else
                    {
                        var upPosition = -defaultNormal - (normal2 + defaultCalcNormal).normalized;
                        var downPosition = -defaultNormal + (normal2 + defaultCalcNormal).normalized;
                        var clockwise = upPosition.magnitude >= downPosition.magnitude;
                        AddCorner(point1, point2, defaultNormal, defaultCalcNormal, normal2, width, true, clockwise, corner,
                            vertex,
                            queVertexs);
                    }
                }
            }
            return queVertexs.ToArray();
        }

        /// <summary>
        /// 端に追加する
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="defaultNormal"></param>
        /// <param name="defaultCalcNormal"></param>
        /// <param name="width"></param>
        /// <param name="isEnd"></param>
        /// <param name="cap"></param>
        /// <param name="vertex"></param>
        /// <param name="queVertexs"></param>
        private static void AddCap(Vector2 point1, Vector2 point2, Vector2 defaultNormal, Vector2 defaultCalcNormal,
            float width, bool isEnd, CapType cap, UIVertex vertex,
            ICollection<UIVertex[]> queVertexs)
        {
            var vertexs = new List<UIVertex>();
            if (!isEnd)
            {
                switch (cap)
                {
                    case CapType.Butt:
                        break;
                    case CapType.Round:
                    {
                        var upCorner = RotationVector2(defaultNormal, -Mathf.PI / 4);
                        var downCorner = RotationVector2(defaultNormal, Mathf.PI / 4);
                        {
                            vertex.position = point1 + downCorner * width;
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = point1 + defaultCalcNormal * width;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = point1;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = point1 + defaultNormal * width;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);

                            queVertexs.Add(vertexs.ToArray());
                            vertexs.Clear();
                        }
                        {
                            vertex.position = point1 + defaultNormal * width;
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = point1;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = point1 - defaultCalcNormal * width;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = point1 + upCorner * width;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);
                            queVertexs.Add(vertexs.ToArray());
                            vertexs.Clear();
                        }
                        break;
                    }
                    case CapType.Square:
                    {
                        vertex.position = point1 + defaultNormal * width + defaultCalcNormal * width;
                        vertex.uv0 = new Vector2(0, 0);
                        vertexs.Add(vertex);

                        vertex.position = point1 + defaultCalcNormal * width;
                        vertex.uv0 = new Vector2(1, 0);
                        vertexs.Add(vertex);

                        vertex.position = point1 - defaultCalcNormal * width;
                        vertex.uv0 = new Vector2(1, 1);
                        vertexs.Add(vertex);

                        vertex.position = point1 + defaultNormal * width - defaultCalcNormal * width;
                        vertex.uv0 = new Vector2(0, 1);
                        vertexs.Add(vertex);

                        queVertexs.Add(vertexs.ToArray());
                        vertexs.Clear();
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(cap.GetType().Name, cap, null);
                }
            }
            else
            {
                switch (cap)
                {
                    case CapType.Butt:
                        break;
                    case CapType.Round:
                    {
                        var upCorner = RotationVector2(-defaultNormal, Mathf.PI / 4);
                        var downCorner = RotationVector2(-defaultNormal, -Mathf.PI / 4);
                        {
                            vertex.position = point2 + defaultCalcNormal * width;
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2 + downCorner * width;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2 - defaultNormal * width;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = point2;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);
                            queVertexs.Add(vertexs.ToArray());
                            vertexs.Clear();
                        }
                        {
                            vertex.position = point2;
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2 - defaultNormal * width;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2 + upCorner * width;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = point2 - defaultCalcNormal * width;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);
                            queVertexs.Add(vertexs.ToArray());
                            vertexs.Clear();
                        }
                        break;
                    }
                    case CapType.Square:
                    {
                        vertex.position = point2 + defaultCalcNormal * width;
                        vertex.uv0 = new Vector2(0, 0);
                        vertexs.Add(vertex);

                        vertex.position = point2 - defaultNormal * width + defaultCalcNormal * width;
                        vertex.uv0 = new Vector2(1, 0);
                        vertexs.Add(vertex);

                        vertex.position = point2 - defaultNormal * width - defaultCalcNormal * width;
                        vertex.uv0 = new Vector2(1, 1);
                        vertexs.Add(vertex);

                        vertex.position = point2 - defaultCalcNormal * width;
                        vertex.uv0 = new Vector2(0, 1);
                        vertexs.Add(vertex);

                        queVertexs.Add(vertexs.ToArray());
                        vertexs.Clear();
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(cap.GetType().Name, cap, null);
                }
            }
        }

        /// <summary>
        /// 角に追加する
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="defaultNormal"></param>
        /// <param name="defaultCalcNormal"></param>
        /// <param name="normal"></param>
        /// <param name="width"></param>
        /// <param name="isEnd"></param>
        /// <param name="corner"></param>
        /// <param name="vertex"></param>
        /// <param name="queVertexs"></param>
        private static void AddCorner(Vector2 point1, Vector2 point2, Vector2 defaultNormal,
            Vector2 defaultCalcNormal, Vector2 normal,
            float width, bool isEnd,bool clockwise, CornerType corner, UIVertex vertex,
            ICollection<UIVertex[]> queVertexs)
        {
            var vertexs = new List<UIVertex>();
            if (isEnd)
            {
                switch (corner)
                {
                    case CornerType.Miter:
                        break;
                    case CornerType.Round:
                    {
                        var upPosition = -defaultNormal - (normal + defaultCalcNormal).normalized;
                        var downPosition = -defaultNormal + (normal + defaultCalcNormal).normalized;
                            
                        var innerProduct = Vector2.Dot(defaultCalcNormal, normal);
                        if (upPosition.magnitude >= downPosition.magnitude)
                        {
                            vertex.position = point2 + normal * width / innerProduct;
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2 - normal * width;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2 - (normal + defaultCalcNormal).normalized * width;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = point2 - defaultCalcNormal * width;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);
                            queVertexs.Add(vertexs.ToArray());
                            vertexs.Clear();
                        }
                        else
                        {
                            vertex.position = point2 - normal * width / innerProduct;
                                vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2 + defaultCalcNormal * width;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2 + (normal + defaultCalcNormal).normalized * width;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = point2 + normal * width;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);

                            queVertexs.Add(vertexs.ToArray());
                            vertexs.Clear();
                        }
                        break;
                    }
                    case CornerType.Bevel:
                    {
                        var innerProduct = Vector2.Dot(defaultCalcNormal, normal);

                        var upPosition = -defaultNormal - (normal + defaultCalcNormal).normalized;
                        var downPosition = -defaultNormal + (normal + defaultCalcNormal).normalized;

                        if (upPosition.magnitude >= downPosition.magnitude)
                        {
                            vertex.position = point2 + normal * width / innerProduct;
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2 - normal * width * innerProduct;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2 - defaultCalcNormal * width;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = point2 + normal * width / innerProduct;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);
                            queVertexs.Add(vertexs.ToArray());
                            vertexs.Clear();
                        }
                        else
                        {
                            vertex.position = point2 - normal * width / innerProduct; ;
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2 + defaultCalcNormal * width;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = point2 + normal * width * innerProduct;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = point2 - normal * width / innerProduct; ;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);

                            queVertexs.Add(vertexs.ToArray());
                            vertexs.Clear();
                        }
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(corner.GetType().Name, corner, null);
                }
            }
            else
            {
                switch (corner)
                {
                    case CornerType.Miter:
                        break;
                    case CornerType.Round:
                    {
                        var upPosition = defaultNormal - (normal + defaultCalcNormal).normalized;
                        var downPosition = defaultNormal + (normal + defaultCalcNormal).normalized;
                        var innerProduct = Vector2.Dot(defaultCalcNormal, normal);
                        if (upPosition.magnitude >= downPosition.magnitude)
                        {
                            vertex.position = point1 + normal * width / innerProduct;
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = point1 - defaultCalcNormal * width;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = point1 - (normal + defaultCalcNormal).normalized * width;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = point1 - normal * width;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);
                            queVertexs.Add(vertexs.ToArray());
                            vertexs.Clear();
                        }
                        else
                        {
                            vertex.position = point1 - normal * width / innerProduct;
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = point1 + normal * width;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = point1 + (normal + defaultCalcNormal).normalized * width;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = point1 + defaultCalcNormal * width;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);
                            queVertexs.Add(vertexs.ToArray());
                            vertexs.Clear();
                        }
                        break;
                    }
                    case CornerType.Bevel:
                    {
                        var innerProduct = Vector2.Dot(defaultCalcNormal, normal);

                        var upPosition = defaultNormal - (normal + defaultCalcNormal).normalized;
                        var downPosition = defaultNormal + (normal + defaultCalcNormal).normalized;
                        if (upPosition.magnitude >= downPosition.magnitude)
                        {
                            vertex.position = point1 + normal * width / innerProduct; ;
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = point1 - defaultCalcNormal * width;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = point1 - normal * width * innerProduct;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = point1 + normal * width / innerProduct; ;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);
                            queVertexs.Add(vertexs.ToArray());
                            vertexs.Clear();
                        }
                        else
                        {
                            vertex.position = point1 - normal * width / innerProduct; ;
                            vertex.uv0 = new Vector2(0, 0);
                            vertexs.Add(vertex);

                            vertex.position = point1 + normal * width * innerProduct;
                            vertex.uv0 = new Vector2(1, 0);
                            vertexs.Add(vertex);

                            vertex.position = point1 + defaultCalcNormal * width;
                            vertex.uv0 = new Vector2(1, 1);
                            vertexs.Add(vertex);

                            vertex.position = point1 - normal * width / innerProduct; ;
                            vertex.uv0 = new Vector2(0, 1);
                            vertexs.Add(vertex);
                            queVertexs.Add(vertexs.ToArray());
                            vertexs.Clear();
                        }
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(corner.GetType().Name, corner, null);
                }
            }
        }
        
        protected override void OnUpdateViewPointPositions()
        {
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
                SetViewUiVerticesGroup(CreateUiVertexsGroup(ViewPointPositions, Width, color, Cap, Corner));
            }
        }

        private static Vector2 RotationVector2(Vector2 source, float radian)
        {
            // (x,y) * Matrix([[cos(π),-sin(π)],[sin(π),cos(π)]])
            return new Vector2(
                Mathf.Cos(radian) * source.x - Mathf.Sin(radian) * source.y,
                Mathf.Sin(radian) * source.x + Mathf.Cos(radian) * source.y
            );
        }

        public enum CornerType
        {
            Miter = 0,
            Round = 1,
            Bevel = 2
        }

        public enum CapType
        {
            Butt = 0,
            Round = 1,
            Square = 2
        }
    }
}