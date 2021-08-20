using Microsoft.EntityFrameworkCore;
using Mop.Hierarchy.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mop.Hierarchy.Functions
{
    public sealed class DatabaseReader : IDisposable
    {
        private readonly HierarchyContext hierarchyContext;
        public DatabaseReader()
        {
            hierarchyContext = new HierarchyContext();
        }

        public void Dispose()
        {
            hierarchyContext?.Dispose();
        }

        internal int GetNodeCount()
        {
            return hierarchyContext.Nodes.Count();
        }

        internal IEnumerable<Node> GetLeafNodes(uint nodeId)
        {
            var node = hierarchyContext.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null)
                return Enumerable.Empty<Node>();

            return hierarchyContext.Nodes.Where(n => n.Parent.Id == node.Id);
        }

        internal IEnumerable<Node> GetNodesBySql(string sql)
        {
            return hierarchyContext.Nodes.FromSqlRaw(sql);
        }
    }
}
