using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Graph {
    public class GraphDataManager {
        private static GraphDataManager _instance;
        private static float[] s_vec16 = new float[16];
        public static GraphDataManager Instance {
            get {
                if (_instance == null) {
                    _instance = new GraphDataManager();
                }
                return _instance;
            }
        }


        private Dictionary<string, GraphData> _graphs = new Dictionary<string, GraphData>();
        public Dictionary<string, GraphData> Graphs => _graphs;

        public GraphData this[string graphName] {
            get {
                return GetOrCreateData(graphName);
            }
        }
        public GraphData GetOrCreateData(string graphName, int columnCount = 0) {
            if (!_graphs.TryGetValue(graphName, out var g)) {
                g = new GraphData(columnCount);
                _graphs.Add(graphName, g);
            }
            return g;
        }

#if UNITY_EDITOR
        public void AddData(string graphName, float[] row) {
            var g = this[graphName];
            g.AddRow(row);
        }
        public void AddData(string graphName, float value) {
            var g = GetOrCreateData(graphName, 1);
            s_vec16[0] = value;
            g.AddRow(s_vec16);
        }
        public void AddData(string graphName, float value0, float value1) {
            var g = GetOrCreateData(graphName, 2);
            s_vec16[0] = value0;
            s_vec16[1] = value1;
            g.AddRow(s_vec16);
        }
        public void AddData(string graphName, float value0, float value1, float value2) {
            var g = GetOrCreateData(graphName, 3);
            s_vec16[0] = value0;
            s_vec16[1] = value1;
            s_vec16[2] = value2;
            g.AddRow(s_vec16);
        }
#endif
    }

    [Serializable]
    public class GraphData {
        [SerializeField] private ExposedList<float> data = new ExposedList<float>();
        [SerializeField] private ColumnInfo[] columnInfos;

        public int ColumnCount {
            get {
                return columnInfos == null ? 0 : columnInfos.Length;
            }
            private set {
                if (columnInfos != null && columnInfos.Length > 0) {
                    throw new Exception("Cannot set column count more than 1 time.");
                }
                if(value > 0) {
                    columnInfos = new ColumnInfo[value];
                    for(int i = 0; i < columnInfos.Length; i++) {
                        columnInfos[i] = ColumnInfo.New();
                    }
                }
            }
        }

        public int RowCount {
            get {
                return data.Count / ColumnCount;
            }
        }
        public float DataLength {
            get => data.Count;
        }
        public int HorizontalColumnIndex {
            get {
                if(ColumnCount == 1) {
                    return -1;
                }
                return 1;
            }
        }
        public GraphData() {

        }
        public GraphData(int columnCount) {
            this.ColumnCount = columnCount;
        }

        public ArrayIndexer<ColumnInfo> ColumnInfos => new ArrayIndexer<ColumnInfo>(this.columnInfos);


        /// <summary>
        /// Add specified row data to graph data. Size of specified row data must be bigger or equal to <see cref="ColumnCount"/>.
        /// </summary>
        /// <param name="rowData">Specified row data.</param>
        public void AddRow(float[] rowData) {
#if UNITY_EDITOR
            if (rowData.Length < this.ColumnCount) {
                ThrowRowDataMismathcException(nameof(rowData));
            }
            if (ColumnCount == 0) {
                if (rowData.Length == 0) {
                    throw new Exception("Size of " + nameof(rowData) + " must be bigger than 0.");
                }
                ColumnCount = rowData.Length;
                
            }

            for (int i = 0; i < ColumnCount; i++) {
                float value = rowData[i];
                data.Add(value);
                columnInfos[i].ValueAdded(value);
            }
#endif
        }
        public int FindClosestIndex(int columnIndex, float value) {
            ref var info = ref this.columnInfos[columnIndex];
            if (info.Ordered) {
                // binary search
                int rowCount = this.RowCount;
                int columnCount = this.ColumnCount;
                int min = 0;
                int max = rowCount - 1;
                while(min <= max) {
                    int mid = (min + max) / 2;
                    if(value == data.array[mid * columnCount + columnIndex]) {
                        return mid + 1;
                    }
                    else if (value < data.array[mid * columnCount + columnIndex]) {
                        max = mid - 1;
                    }
                    else {
                        min = mid + 1;
                    }
                }
                return min;
            }
            else {
                int rowCount = this.RowCount;
                int columnCount = this.ColumnCount;
                int i = 0;
                int index = columnIndex;
                float closestValue = float.PositiveInfinity;
                int closestIndex = -1;

                for (; i < rowCount; i++, index += columnCount) {

                    float arrayValue = this.data.array[index];
                    float dif = arrayValue - value;
                    float distanceSqr = dif * dif;
                    if(distanceSqr < closestValue) {
                        closestIndex = i;
                        closestValue = distanceSqr;
                    }
                }
                return closestIndex;
            }

        }
        /// <summary>
        /// Get data of specified row to specified row data. Size of specified row data must be bigger or equal to <see cref="ColumnCount"/>.
        /// </summary>
        /// <param name="rowIndex">Specified row.</param>
        /// <param name="rowData">Specified row data.</param>
        public void GetRow(int rowIndex, float[] rowData) {
            if (rowData.Length < this.ColumnCount) {
                ThrowRowDataMismathcException(nameof(rowData));
            }
            int index = rowIndex * ColumnCount;
            if (index >= data.Count) {
                throw new IndexOutOfRangeException();
            }
            for (int i = 0; i < ColumnCount; i++, index++) {
                rowData[i] = data.array[index];
            }
        }
        public float GetData(int rowIndex, int columnIndex) {
            var index = columnIndex + rowIndex * ColumnCount;
            if (index >= data.Count) {
                throw new IndexOutOfRangeException();
            }
            return data.array[index];
        }
        private void ThrowRowDataMismathcException(string rowDataFieldName) {
            throw new Exception("Number of entry in the specified " + rowDataFieldName + " has to be equal to " + nameof(ColumnCount));
        }

    }
    public struct ColumnInfo {
        public static ColumnInfo New() {
            ColumnInfo ci;
            ci._ordered = true;
            ci.lastAddedValue = float.NegativeInfinity;
            return ci;
        }
        public bool Ordered { get => _ordered; private set => _ordered = value; }

        [SerializeField] private float lastAddedValue;
        private bool _ordered;
        public void ValueAdded(float value) {
            if(value < lastAddedValue) {
                _ordered = false;
            }
            lastAddedValue = value;
        }
    }
}