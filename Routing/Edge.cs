﻿namespace Routing
{
    public class Edge<T>
    {
        public Vertex SourceVertex { get; set; }
        public Vertex TargetVertex { get; set; }
        public float Cost { get; set; }
        public bool IsReverse { get; set; }
        public T DataItem { get; set; }

        /// <summary>
        /// Is set to true if this is an edge created due to an overloaded source or target node.
        /// </summary>
        public bool IsOverload { get; set; }

        public Edge<T> Clone()
        {
            return new Edge<T>()
            {
                SourceVertex = SourceVertex,
                TargetVertex = TargetVertex,
                Cost = Cost,
                IsReverse = IsReverse,
                DataItem = DataItem
            };
        }

        public override string ToString()
        {
            return DataItem.ToString();
        }
    }
}