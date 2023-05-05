using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace View
{
    public class GraphManager : MonoBehaviour
    {
        public static GraphManager Instance { get; private set; }
        private Dictionary<int, INode> nodes = new();

        private void Awake()
        {
            Instance = this;
            foreach (INode node in GetComponentsInChildren<INode>())
            {
                nodes.Add(node.Id, node);
            }
        }
        public INode GetNode(int id)
        {
            return nodes[id];
        }
    }
}