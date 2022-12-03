using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core
{
    // Runtime data class of a level/piece/atom
    public class GridInfo
    {
        private const float c_slopeIsInfinite = 99991337f;
        public struct RuntimeAtom { public int decorID; }
        public Dictionary<Int2, RuntimeAtom> Data { get; } = new Dictionary<Int2, RuntimeAtom>();
        private List<bool> _existingIndexes = new List<bool>();

        /// <summary>
        /// Initialize the specified atoms into GridInfo.
        /// </summary>
        /// <param name="atoms">Atoms.</param>
        public void Init(Piece.Atom[] atoms)
        {
            if (atoms == null) return;
            if (atoms.Length == 0) return;
            RuntimeAtom ra = new RuntimeAtom();
            for (int k = 0; k < atoms.Length; k++)
            {
                for (int i = atoms[k].rectangle.xMin; i < atoms[k].rectangle.xMax; i++)
                {
                    for (int j = atoms[k].rectangle.yMin; j < atoms[k].rectangle.yMax; j++)
                    {
                        int did = atoms[k].decorationID;
                        ra.decorID = did;
                        Data[new Int2(i, j)] = ra;
                        while (_existingIndexes.Count <= did)
                        {
                            _existingIndexes.Add(false);
                        }
                        _existingIndexes[did] = true;
                    }
                }
            }
        }
        /// <summary>
        /// Gets the existing decoration indexes inside the GridInfo
        /// </summary>
        /// <returns>The existing indexes.</returns>
        public IEnumerable<int> GetExistingIndexes()
        {
            for (int i = 0; i < _existingIndexes.Count; i++)
            {
                if (_existingIndexes[i])
                    yield return i;
            }
        }
        /// <summary>
        /// Gets the atoms.
        /// </summary>
        /// <returns>Rectangle optimized atoms.</returns>
        public Piece.Atom[] GetOptimizedAtoms()
        {
            var sCoordinates = new Int2[Data.Count];
            var sDecorations = new int[Data.Count];
            int counter = 0;
            foreach (var d in Data)
            {
                sCoordinates[counter] = d.Key;
                sDecorations[counter++] = d.Value.decorID;
            }
            Array.Sort(sCoordinates, sDecorations);
            bool emptyCheck1 = CompactOnXAxis(ref sCoordinates, ref sDecorations,
                out List<Piece.RectInt> _tempRectList, out List<int> _tempDecorList);
            bool emptyCheck2 = CompactOnYAxis(ref _tempRectList, ref _tempDecorList,
                out List<Piece.RectInt> _finalRects, out List<int> _finalDecors);
            Piece.Atom[] atoms;
            if (emptyCheck1 && emptyCheck2)
            {
                atoms = new Piece.Atom[_finalRects.Count];
                for (int i = 0; i < _finalRects.Count; i++)
                {
                    atoms[i].rectangle = _finalRects[i];
                    atoms[i].decorationID = _finalDecors[i];
                }
            }
            else
            {
                atoms = new Piece.Atom[0];
            }
            return atoms;
        }
        /// <summary>
        /// Deletes a rectangle for the given corner points from the grid.
        /// </summary>
        /// <remarks>Handles given corners well, without assumption of which corner is positioned where. Do not worry.</remarks>
        /// <param name="corner1">Corner1.</param>
        /// <param name="corner2">Corner2.</param>
        /// <param name="offset">Offset.</param>
        public void DeleteRectangle(Vector3 corner1, Vector3 corner2, Vector3 offset = default)
        {
            Vector3 _delta = corner1 - corner2;
            if (_delta.x < 0 && _delta.y < 0)
            {
                for (int y = Mathf.RoundToInt(corner1.y); y <= corner2.y; y++)
                {
                    for (int x = Mathf.RoundToInt(corner1.x); x <= corner2.x; x++)
                    {
                        Delete(x, y, offset);
                    }
                }
            }
            else if (_delta.x > 0 && _delta.y < 0)
            {
                for (int y = Mathf.RoundToInt(corner1.y); y <= corner2.y; y++)
                {
                    for (int x = Mathf.RoundToInt(corner2.x); x <= corner1.x; x++)
                    {
                        Delete(x, y, offset);
                    }
                }
            }
            else if (_delta.x > 0 && _delta.y > 0)
            {
                for (int y = Mathf.RoundToInt(corner2.y); y <= corner1.y; y++)
                {
                    for (int x = Mathf.RoundToInt(corner2.x); x <= corner1.x; x++)
                    {
                        Delete(x, y, offset);
                    }
                }
            }
            else if (_delta.x < 0 && _delta.y > 0)
            {
                for (int y = Mathf.RoundToInt(corner2.y); y <= corner1.y; y++)
                {
                    for (int x = Mathf.RoundToInt(corner1.x); x <= corner2.x; x++)
                    {
                        Delete(x, y, offset);
                    }
                }
            }
        }
        /// <summary>
        /// Inserts a rectangle for the given corner points from the grid.
        /// </summary>
        /// <remarks>Handles given corners well, without assumption of which corner is positioned where. Do not worry.</remarks>
        /// <param name="corner1">Corner1.</param>
        /// <param name="corner2">Corner2.</param>
        /// <param name="decorID">Decor identifier.</param>
        /// <param name="offset">Offset.</param>
        public void InsertRectangle(Vector3 corner1, Vector3 corner2, in int decorID, Vector3 offset = default)
        {
            Vector3 _delta = corner1 - corner2;
            if (_delta.x < 0 && _delta.y < 0)
            {
                for (int y = Mathf.RoundToInt(corner1.y); y <= corner2.y; y++)
                {
                    for (int x = Mathf.RoundToInt(corner1.x); x <= corner2.x; x++)
                    {
                        AddToGrid(x, y, in decorID, offset);
                    }
                }
            }
            else if (_delta.x > 0 && _delta.y < 0)
            {
                for (int y = Mathf.RoundToInt(corner1.y); y <= corner2.y; y++)
                {
                    for (int x = Mathf.RoundToInt(corner2.x); x <= corner1.x; x++)
                    {
                        AddToGrid(x, y, in decorID, offset);
                    }
                }
            }
            else if (_delta.x > 0 && _delta.y > 0)
            {
                for (int y = Mathf.RoundToInt(corner2.y); y <= corner1.y; y++)
                {
                    for (int x = Mathf.RoundToInt(corner2.x); x <= corner1.x; x++)
                    {
                        AddToGrid(x, y, in decorID, offset);
                    }
                }
            }
            else if (_delta.x < 0 && _delta.y > 0)
            {
                for (int y = Mathf.RoundToInt(corner2.y); y <= corner1.y; y++)
                {
                    for (int x = Mathf.RoundToInt(corner1.x); x <= corner2.x; x++)
                    {
                        AddToGrid(x, y, in decorID, offset);
                    }
                }
            }
        }
        /// <summary>
        /// Draws a line of atoms into the grid for the given start/end points.
        /// </summary>
        /// <remarks>Handles given points well, without assumption of which point is positioned where. Do not worry.</remarks>
        /// <param name="startPoint">Start point.</param>
        /// <param name="endPoint">End point.</param>
        /// <param name="decorID">Decor identifier.</param>
        /// <param name="offset">Offset.</param>
        public void InsertLine(Vector3 startPoint, Vector3 endPoint, in int decorID, Vector3 offset = default)
        {
            Vector3 _deltaMouse = startPoint - endPoint;
            var _slope = GetSlope(startPoint, endPoint);
            if (_slope == c_slopeIsInfinite) // formula of line x = n
            {
                if (_deltaMouse.y < 0)
                {
                    for (int y = Mathf.RoundToInt(startPoint.y); y <= endPoint.y - startPoint.y; y++)
                    {
                        AddToGrid(_deltaMouse.x, y, in decorID, offset);
                    }
                }
                else
                {
                    for (int y = Mathf.RoundToInt(endPoint.y); y <= _deltaMouse.y; y--)
                    {
                        AddToGrid(_deltaMouse.x, y, in decorID, offset);
                    }
                }
            }
            else
            {
                switch (_slope > 1 || _slope < -1)
                {
                    case true:  // eğim 45 dereceden büyük
                        if (_deltaMouse.y < 0)  // başlangıc kordinatnın y'si bitişten küçük
                        {
                            float x;
                            for (int y = Mathf.RoundToInt(startPoint.y); y <= endPoint.y; y++)
                            {
                                x = ((y - startPoint.y) / _slope) + startPoint.x;
                                AddToGrid(x, y, in decorID, offset);
                            }
                        }
                        else  // başlangıc kordinatinın y'si bitişten büyük
                        {
                            float x;
                            for (int y = Mathf.RoundToInt(endPoint.y); y <= startPoint.y; y++)
                            {
                                x = ((y - startPoint.y) / _slope) + startPoint.x;
                                AddToGrid(x, y, in decorID, offset);
                            }
                        }
                        break;
                    case false: // eğim 45 dereceden küçük
                        if (_deltaMouse.x < 0) // başlangıc kordinatının x'si bitişten küçük
                        {
                            float y;
                            for (int x = Mathf.RoundToInt(startPoint.x); x <= endPoint.x; x++)
                            {
                                y = (_slope * (x - startPoint.x)) + startPoint.y;
                                AddToGrid(x, y, in decorID, offset);
                            }
                        }  // başlangıc kordinatnın x'si bitişten büyük
                        else
                        {
                            float y;
                            for (int x = Mathf.RoundToInt(endPoint.x); x <= startPoint.x; x++)
                            {
                                y = (_slope * (x - startPoint.x)) + startPoint.y;
                                AddToGrid(x, y, in decorID, offset);
                            }
                        }
                        break;
                }
            }
        }
        /// <summary>
        /// Adds to grid.
        /// </summary>
        /// <returns><c>true</c>, if data was added to grid (free fill), <c>false</c> replacement or already existing same.</returns>
        /// <param name="coordinate">Coordinate.</param>
        /// <param name="decorID">Decor identifier.</param>
        public bool AddToGrid(Int2 coordinate, in int decorID)
        {
            bool flag = true;
            if (Data.TryGetValue(coordinate, out RuntimeAtom old))
            {
                if (old.decorID != decorID)
                {
                    Data.Remove(coordinate);
                    flag = false;
                }
                else
                {
                    return flag;
                }
            }
            Data.Add(coordinate, new RuntimeAtom
            {
                decorID = decorID
            });
            return flag;
        }
        /// <summary>
        /// Delete the specified coordinate from GridInfo.
        /// </summary>
        /// <param name="coordinate">Coordinate.</param>
        public void Delete(Int2 coordinate)
        {
            Data.Remove(coordinate);
        }
        /// <summary>
        /// Clears this instance of GridInfo.
        /// </summary>
        public void Clear()
        {
            Data.Clear();
            _existingIndexes.Clear();
        }
        #region Private Methods
        private bool CompactOnXAxis(ref GridInfo.Int2[] i_ria, ref int[] i_da, out List<Piece.RectInt> o_ril, out List<int> o_dl)
        {
            o_ril = new List<Piece.RectInt>();        // Output rectint list
            o_dl = new List<int>();                     // Output decor list
            int i_length = i_ria.Length;
            if (i_length == 0) return false;
            if (i_da.Length == 0) return false;
            int count = 1;
            var par = new Piece.RectInt();       // Potantial atom rectangle
                                                 // Edge case : First data processing.
            SetNewPotantialAtomRect(ref par, ref i_ria[0]);
            // Common case.
            while (count < i_length)
            {
                if (i_ria[count - 1].x + 1 == i_ria[count].x &&
                     i_ria[count - 1].y == i_ria[count].y)      // Candidate
                {
                    if (i_da[count - 1] == i_da[count])
                    {
                        par.xMax += 1;        // Consecutive atom discovered
                    }
                    else
                    {
                        AddCandidate(o_ril, o_dl, ref par, ref i_da[count - 1]);
                        SetNewPotantialAtomRect(ref par, ref i_ria[count]);
                    }
                }
                // Not candidate
                else
                {
                    AddCandidate(o_ril, o_dl, ref par, ref i_da[count - 1]);
                    SetNewPotantialAtomRect(ref par, ref i_ria[count]);
                }
                count++;
            }
            // Edge case : Last data processing for X axis compaction.
            if (count == i_length)
            {
                AddCandidate(o_ril, o_dl, ref par, ref i_da[count - 1]);
            }
            return true;
        }
        private bool CompactOnYAxis(ref List<Piece.RectInt> i_ril, ref List<int> i_dl, out List<Piece.RectInt> o_ril, out List<int> o_dl)
        {
            o_ril = new List<Piece.RectInt>();        // Output Rectint list
            o_dl = new List<int>();                     // Output decor list
            if (i_ril.Count == 0) return false;
            if (i_dl.Count == 0) return false;
            var par = new Piece.RectInt();            // Potantial atom rect
            int _ir = 0;                          // Rect Index (outer loop)
            int _tir = 1;                    // Temp Rect Index (inner loop)
            while (_ir < i_ril.Count)
            {
                par = i_ril[_ir];
                while (_tir < i_ril.Count)
                {
                    if (par.xMax == i_ril[_tir].xMax &&
                        par.xMin == i_ril[_tir].xMin &&
                        par.yMax == i_ril[_tir].yMin)           // Candidate
                    {
                        if (i_dl[_ir] == i_dl[_tir])
                        {
                            // Consecutive atom discovered
                            i_ril.RemoveAt(_tir);
                            i_dl.RemoveAt(_tir);
                            par.size = new Vector2Int(par.size.x, par.size.y + 1);
                            continue;
                        }
                    }
                    _tir++;
                }
                AddCandidate(o_ril, o_dl, ref par, i_dl[_ir]);
                _ir++;
                _tir = _ir + 1;
            }
            return true;
        }
        private void SetNewPotantialAtomRect(ref Piece.RectInt par, ref GridInfo.Int2 int2)
        {
            par.y = int2.y;
            par.x = int2.x;
            par.size = Vector2Int.one;
        }
        private void AddCandidate(List<Piece.RectInt> ril, List<int> il, Piece.RectInt par, int i)
        {
            ril.Add(new Piece.RectInt(par.position, par.size));
            il.Add(i);
        }
        private void AddCandidate(List<Piece.RectInt> ril, List<int> il, ref Piece.RectInt par, ref int i)
        {
            ril.Add(new Piece.RectInt(par.position, par.size));
            il.Add(i);
        }
        private void AddCandidate(List<Piece.RectInt> ril, List<int> il, ref Piece.RectInt par, int i)
        {
            ril.Add(new Piece.RectInt(par.position, par.size));
            il.Add(i);
        }
        private float GetSlope(Vector3 @this, Vector3 other)
        {
            if (other.x == @this.x) return c_slopeIsInfinite;
            return (@other.y - @this.y) / (other.x - @this.x);
        }
        private void AddToGrid(float x, float y, in int _selectedDecorID, Vector2 offset = default)
        {
            var k = new Int2(
                Mathf.RoundToInt(x) - Mathf.RoundToInt(offset.x),
                Mathf.RoundToInt(y) - Mathf.RoundToInt(offset.y));
            AddToGrid(k, in _selectedDecorID);
        }
        private void Delete(float x, float y, Vector3 offset)
        {
            var k = new GridInfo.Int2(
                Mathf.RoundToInt(x) - Mathf.RoundToInt(offset.x),
                Mathf.RoundToInt(y) - Mathf.RoundToInt(offset.y));
            Delete(k);
        }
#endregion
        public struct Int2 : IComparable
        {
            public int x;
            public int y;

            public Int2(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
            public override int GetHashCode()
            {
                return x + (y << 16);
            }

            public override bool Equals(object obj) {
                var other = (Int2)obj;
                return other.x == x && other.y == y;
            }

            public int CompareTo(object obj)
            {
                if (!(obj is Int2))
                {
                    throw new ArgumentException("Compared Object is not of Int2");
                }
                Int2 otherInt2 = (Int2)obj;
                if (this.y < otherInt2.y)
                {
                    return -1;
                }
                if (this.y == otherInt2.y)
                {
                    if (this.x < otherInt2.x)
                    {
                        return -1;
                    }
                    if (this.x == otherInt2.x)
                        return 0;
                    if (this.x > otherInt2.x)
                        return 1;
                }
                if (this.y > otherInt2.y)
                {
                    return 1;
                }
                return 0;
            }
        }
    }
}
