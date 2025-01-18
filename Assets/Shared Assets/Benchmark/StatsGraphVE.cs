using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Mathematics;
using Unity.Burst;

namespace Benchmarking
{
    public class StatsGraphVE : VisualElement
    {
        private List<float> _dataset = new List<float>();

        private float _width = 0f;
        private Vertex[] _vertices = null;
        private ushort[] _triangles = null;
        private int _pointsCounts = 0;
        private bool _isDirty = false;
        public bool isDirty => _isDirty;

        private Color32 k_graphColor = new Color(1, 1, 1, 0.5f);

        public StatsGraphVE()
        {
            generateVisualContent = GenerateGraphMesh;
            AddToClassList("TimingsGraph");
        }

        private void GenerateGraphMesh(MeshGenerationContext mgc)
        {
            if (_width != contentRect.width)
                GenerateVertices();


            MeshWriteData mwd = mgc.Allocate(_vertices.Length, _triangles.Length);
            mwd.SetAllVertices(_vertices);
            mwd.SetAllIndices(_triangles);

            _isDirty = false;
        }

        private void GenerateVertices()
        {
            _width = contentRect.width;

            _pointsCounts = Mathf.CeilToInt(_width);
            _pointsCounts = Mathf.Max(_pointsCounts, 2);

            if (_vertices != null)
            {
                Array.Resize(ref _vertices, _pointsCounts * 2);
                Array.Resize(ref _triangles, (_pointsCounts - 1) * 6);
            }
            else
            {
                _vertices = new Vertex[_pointsCounts * 2];
                _triangles = new ushort[(_pointsCounts - 1) * 6];
            }

            float x;
            float xOffset = _width * 1.0f / (_pointsCounts - 1);
            int t;
            int i2;

            for (int i = 0; i < _pointsCounts; i++)
            {
                x = i * xOffset;
                i2 = i * 2;
                t = i * 6;

                _vertices[i2] = new Vertex()
                {
                    position = new Vector3(x, 0.5f * contentRect.height, Vertex.nearZ), // top
                    tint = k_graphColor
                };
                _vertices[i2 + 1] = new Vertex()
                {
                    position = new Vector3(x, contentRect.height, Vertex.nearZ), // bottom
                    tint = k_graphColor
                };

                if (i < (_pointsCounts - 1))
                {
                    _triangles[t] = (ushort)(i2);
                    _triangles[t + 1] = (ushort)(i2 + 2);
                    _triangles[t + 2] = (ushort)(i2 + 1);

                    _triangles[t + 3] = (ushort)(i2 + 1);
                    _triangles[t + 4] = (ushort)(i2 + 2);
                    _triangles[t + 5] = (ushort)(i2 + 3);
                }
            }

            SetData();
        }

        public void SetData(List<float> data = null, bool refresh = false)
        {
            if (data != null)
            {
                _dataset.Clear();
                _dataset.AddRange(data);
            }

            if (_dataset == null || _dataset.Count < 2 )
                    return;

            for (int i = 0; i < _pointsCounts; i++)
            {
                float t = i * 1.0f / (_pointsCounts - 1);

                int start = Mathf.FloorToInt(Mathf.Max(0, t - 0.5f / _pointsCounts) * _dataset.Count);
                int end = Mathf.CeilToInt(Mathf.Min(1, t - 0.5f / _pointsCounts) * _dataset.Count);

                float m = 0f;
                for (int j = start; j < end; j++)
                {
                    m = Mathf.Max(m, _dataset[j]);
                }

                _vertices[i * 2].position.y = contentRect.height * (1.0f - m);
            }

            if (!refresh)
                return;

            MarkDirtyRepaint();
            _isDirty = true;
        }
    }
}