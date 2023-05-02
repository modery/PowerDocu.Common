using System;
using System.Collections.Generic;
using System.Text;

namespace PowerDocu.Common
{
    public class ActionNode
    {
        public string Name;
        public string Description;
        public string Expression;
        public Expression actionExpression;
        public Expression actionInput;
        public string Type;
        public string Inputs;
        public string Connection;
        public List<Expression> actionInputs = new List<Expression>();
        public List<ActionNode> Neighbours = new List<ActionNode>();
        public List<ActionNode> Subactions = new List<ActionNode>();
        public List<ActionNode> Elseactions = new List<ActionNode>();
        public Dictionary<ActionNode, string[]> nodeRunAfterConditions = new Dictionary<ActionNode, string[]>();
        //list of children that are called as part of a switch
        public Dictionary<ActionNode, string> switchRelationship = new Dictionary<ActionNode, string>();
        public int Order;
        public ActionNode parent;

        public ActionNode(string name)
        {
            this.Name = name;
        }

        public bool AddNeighbour(ActionNode neighbour)
        {
            if (Neighbours.Contains(neighbour))
            {
                return false;
            }
            else
            {
                Neighbours.Add(neighbour);
                return true;
            }
        }
        public bool AddSubaction(ActionNode subaction)
        {
            if (Subactions.Contains(subaction))
            {
                return false;
            }
            else
            {
                Subactions.Add(subaction);
                subaction.parent = this;
                return true;
            }
        }
        public bool AddElseaction(ActionNode elseaction)
        {
            if (Elseactions.Contains(elseaction))
            {
                return false;
            }
            else
            {
                Elseactions.Add(elseaction);
                elseaction.parent = this;
                return true;
            }
        }
        public override string ToString()
        {
            return Name;
        }
    }

    public class ActionGraph
    {
        private readonly List<ActionNode> myActionNodes = new List<ActionNode>();
        //root nodes are nodes that run right after the trigger. Usually there is one, but occasionally there are more (parallel branches)
        private List<ActionNode> rootNodes = new List<ActionNode>();

        public ActionGraph()
        {
        }

        public int Count
        {
            get
            {
                return myActionNodes.Count;
            }
        }
        public IList<ActionNode> ActionNodes
        {
            get
            {
                return myActionNodes.AsReadOnly();
            }
        }
        public bool AddNode(string value)
        {
            if (Find(value) != null)
            {
                return false;
            }
            else
            {
                myActionNodes.Add(new ActionNode(value));
                return true;
            }
        }

        public bool hasRoot()
        {
            return rootNodes.Count > 0;
        }

        public bool AddEdge(ActionNode gn1, ActionNode gn2, string[] runAfterConditions)
        {
            if (gn1 == null && gn2 == null)
            {
                return false;
            }
            else if (gn1.Neighbours.Contains(gn2))
            {
                return false;
            }
            else
            {
                gn1.AddNeighbour(gn2);
                gn1.nodeRunAfterConditions.Add(gn2, runAfterConditions);
                return true;
            }
        }

        public ActionNode Find(string value)
        {
            foreach (ActionNode item in myActionNodes)
            {
                if (item.Name.Equals(value))
                {
                    return item;
                }
            }
            return null;
        }

        public ActionNode FindOrCreate(string value)
        {
            ActionNode item = Find(value);
            if (item == null)
            {
                item = new ActionNode(value);
                myActionNodes.Add(item);
            }
            return item;
        }

        public override string ToString()
        {
            StringBuilder nodeString = new StringBuilder();
            for (int i = 0; i < Count; i++)
            {
                nodeString.Append(myActionNodes[i].ToString());
                if (i < Count - 1)
                {
                    nodeString.Append("\n");
                }
            }
            return nodeString.ToString();
        }

        public List<ActionNode> getPrecedingNeighbours(ActionNode currentNode)
        {
            List<ActionNode> precedingNeighbours = new List<ActionNode>();
            foreach (ActionNode node in myActionNodes)
            {
                if (node.Neighbours.Contains(currentNode))
                {
                    precedingNeighbours.Add(node);
                }
            }
            return precedingNeighbours;
        }

        public List<ActionNode> getRootNodes()
        {
            return rootNodes;
        }

        public void addRootNode(ActionNode root)
        {
            rootNodes.Add(root);
        }
    }
}
