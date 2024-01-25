namespace CadRevealComposer.Operations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

internal class IntervalKdTree<T>
    where T : notnull
{
    private readonly Node _rootNode;
    private readonly int _divisionThreshold;

    public IntervalKdTree(Vector3 min, Vector3 max, int divisionThreshold)
    {
        _divisionThreshold = divisionThreshold;
        _rootNode = new Node(this, 0, min, max);
    }

    public void Put(Vector3 min, Vector3 max, T value)
    {
        _rootNode.AddBox(new Box(value, min, max));
    }

    public IEnumerable<T> GetValues(Vector3 min, Vector3 max)
    {
        return _rootNode.GetValues(new Cube(min, max));
    }

    /// <summary>
    /// Node is extend axis aligned 3d cube which is the node implementation of IntervalKDTree.
    /// </summary>
    private record Node : Cube
    {
        private readonly int _depth;
        private float _divisionBoundary;

        [MemberNotNullWhen(true, nameof(_lowChild), nameof(_highChild))]
        private bool HasChildren() => _lowChild is not null && _highChild is not null;

        private Node? _lowChild;
        private Node? _highChild;

        private readonly IntervalKdTree<T> _tree;
        private List<Box> _boxes;

        public Node(IntervalKdTree<T> tree, int depth, Vector3 min, Vector3 max)
            : base(min, max)
        {
            _tree = tree;
            _depth = depth;
            _boxes = new List<Box>();
        }

        public void AddBox(Box box)
        {
            if (HasChildren())
            {
                if (box.IsBelow(_depth, _divisionBoundary))
                {
                    _lowChild.AddBox(box);
                    return;
                }
                if (box.IsAbove(_depth, _divisionBoundary))
                {
                    _highChild.AddBox(box);
                    return;
                }
            }

            _boxes.Add(box);

            // Divide to children if threshold has been exceeded
            if (!HasChildren() && _boxes.Count > _tree._divisionThreshold)
            {
                Divide();
            }
        }

        public IEnumerable<T> GetValues(Cube cube)
        {
            foreach (var box in _boxes)
            {
                if (cube.Intersects(box))
                {
                    yield return box.Value;
                }
            }

            if (!HasChildren())
            {
                yield break;
            }

            if (cube.IsBelow(_depth, _divisionBoundary))
            {
                foreach (var value in _lowChild.GetValues(cube))
                {
                    yield return value;
                }
            }
            else if (cube.IsAbove(_depth, _divisionBoundary))
            {
                foreach (var value in _highChild.GetValues(cube))
                {
                    yield return value;
                }
            }
            else
            {
                foreach (var value in _lowChild.GetValues(cube))
                {
                    yield return value;
                }
                foreach (var value in _highChild.GetValues(cube))
                {
                    yield return value;
                }
            }
        }

        private void Divide()
        {
            if (HasChildren())
            {
                throw new Exception("Already has children.");
            }

            int dimension = _depth % 3;

            _divisionBoundary = dimension switch
            {
                0 => (Max.X + Min.X) / 2,
                1 => (Max.Y + Min.Y) / 2,
                _ => (Max.Z + Min.Z) / 2
            };

            _lowChild = dimension switch
            {
                0 => new Node(_tree, _depth + 1, Min, new Vector3(_divisionBoundary, Max.Y, Max.Z)),
                1 => new Node(_tree, _depth + 1, Min, new Vector3(Max.X, _divisionBoundary, Max.Z)),
                _ => new Node(_tree, _depth + 1, Min, new Vector3(Max.X, Max.Y, _divisionBoundary))
            };

            _highChild = dimension switch
            {
                0 => new Node(_tree, _depth + 1, new Vector3(_divisionBoundary, Min.Y, Min.Z), Max),
                1 => new Node(_tree, _depth + 1, new Vector3(Min.X, _divisionBoundary, Min.Z), Max),
                _ => new Node(_tree, _depth + 1, new Vector3(Min.X, Min.Y, _divisionBoundary), Max)
            };

            IList<Box> oldBoxList = _boxes;
            _boxes = new List<Box>();
            foreach (Box cube in oldBoxList)
            {
                AddBox(cube);
            }
        }
    }

    /// <summary>
    /// Box is axis aligned 3d cube which can hold value. Acts as capsule in IntervalKDTree data structure.
    /// </summary>
    private record Box(T Value, Vector3 Min, Vector3 Max) : Cube(Min, Max);

    /// <summary>
    /// Axis aligned 3d cube implementation with math functions.
    /// </summary>
    private record Cube(Vector3 Min, Vector3 Max)
    {
        public bool IsBelow(int depth, double boundary)
        {
            int dimension = depth % 3;
            return dimension switch
            {
                0 => Max.X < boundary,
                1 => Max.Y < boundary,
                _ => Max.Z < boundary
            };
        }

        public bool IsAbove(int depth, double boundary)
        {
            int dimension = depth % 3;
            return dimension switch
            {
                0 => boundary <= Min.X,
                1 => boundary <= Min.Y,
                _ => boundary <= Min.Z
            };
        }

        public bool Intersects(Cube cube)
        {
            // positive if overlaps
            var diff = Vector3.Min(Max, cube.Max) - Vector3.Max(Min, cube.Min);

            return diff.X > 0f && diff.Y > 0f && diff.Z > 0f;
        }
    }
}
