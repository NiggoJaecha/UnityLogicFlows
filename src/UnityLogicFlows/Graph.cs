﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace LogicFlows
{
    public class LogicFlowGraph : LogicFlowBox
    {
        public KeyCode KeySelectTree { get; set; } = KeyCode.T;
        public KeyCode KeySelectNetwork { get; set; } = KeyCode.N;
        public KeyCode KeyNodeDisable { get; set; } = KeyCode.D;
        public KeyCode KeyNodeDelete { get; set; } = KeyCode.Delete;


        private Dictionary<int, LogicFlowNode> nodes = new Dictionary<int, LogicFlowNode>();

        public static Color backgroundColor = Color.white;
        public static Color headerColor = Color.gray;
        public static Color selectionColor = LogicFlowNode.selectedColor;

        private bool draggingWindow = false;
        private bool hoverExpand = false;
        private bool draggingExpand = false;
        internal bool anyInputHovered = false;
        internal bool anyNodeHovered = false;
        internal int? dragConnectionSourceIndex = null;

        // selection
        internal Vector2? selectionStart;
        internal List<int> selectedNodes = new List<int>();

        public bool eatingInput { get; internal set; } = false;
        public bool mouseOver { get; internal set; } = false;

        public bool isLoading = false;

        public List<LogicFlowNode> getAllNodes()
        {
            return nodes.Values.ToList();
        }

        public LogicFlowGraph(Rect rect)
        {
            this.setBackingRect(rect);
        }

        public LogicFlowNode getNodeAt(int key)
        {
            if (!nodes.ContainsKey(key)) return null;
            return nodes[key];
        }

        public void setUIScaleModifier(float modifier)
        {
            UIScaleModifier = modifier;
        }

        public float getUIScale()
        {
            return UIScale;
        }

        /// <summary>
        /// Should only be called from the constructor of LogicFlowNode
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        internal int AddNode(LogicFlowNode node)
        {
            int key;
            
            key = new System.Random().Next(int.MinValue, 0);
            while (nodes.ContainsKey(key))
            {
                key = new System.Random().Next(int.MinValue, 0);
            }

            nodes.Add(key, node);
            return key;
        }

        internal int AddNode(int key, LogicFlowNode node)
        {
            nodes.Add(key, node);
            return key;
        }

        public void RemoveNode(int index)
        {
            nodes.Remove(index);
        }

        public void ForceUpdate()
        {
            foreach (LogicFlowNode node in nodes.Values)
            {
                if (node != null && node is LogicFlowOutput n)
                {
                    n.forceUpdate();
                }
            }
        }

        /// <summary>
        /// should be called per frame when the flow is active but not displaying its UI
        /// </summary>
        public void backgroundUpdate()
        {
            foreach (LogicFlowNode node in nodes.Values)
            {
                if (node != null && node is LogicFlowOutput)
                {
                    node.getValue(); //evaluate flow
                }
            }
        }

        /// <summary>
        /// should be called from within OnGUI whenever the flow is displaying its UI
        /// </summary>
        public void ongui()
        {
            foreach (LogicFlowNode node in nodes.Values)
            {
                if (node != null) node.drawLabel();
            }

            // draw tooltip after drawing all the labes otherwise the tooltip will be hidden by the lables of nodes later in the list
            foreach (LogicFlowNode node in nodes.Values)
            {
                if (node != null) node.drawTooltip();
            }

            if (eatingInput) Input.ResetInputAxes();
        }

        /// <summary>
        /// Selects all nodes in the node tree of a node, or all trees of all selected nodes
        /// </summary>
        /// <param name="treeRoot"></param>
        public void selectTree(int? treeRoot = null)
        {
            List<int> roots = new List<int>();
            if (treeRoot.HasValue) roots.Add(treeRoot.Value);
            else roots = selectedNodes;
            if (roots.Count > 0)
            {
                List<int> newSelects = new List<int>();
                foreach(int index in roots)
                {
                    if (!newSelects.Contains(index))
                    {
                        newSelects.AddRange(getNodeAt(index).getInputTree().Where(i => !newSelects.Contains(i)));
                    }
                }
                roots.AddRange(newSelects);
                selectedNodes = roots;
            }
        }

        /// <summary>
        /// Selects all nodes in the nodenetwork of a node, or all networks of all selected nodes
        /// Network does not expand through input nodes.
        /// </summary>
        /// <param name="networkStart"></param>
        public void selectNetwork(int? networkStart = null)
        {
            List<int> roots = new List<int>();
            if (networkStart.HasValue) roots.Add(networkStart.Value);
            else roots = selectedNodes;
            if (roots.Count > 0)
            {
                List<int> newSelects = new List<int>();
                foreach(int index in roots)
                {
                    if (!newSelects.Contains(index))
                    {
                        newSelects.AddRange(getNodeAt(index).getNodeNetwork().Where(i => !newSelects.Contains(i)));
                    }
                }
                roots.AddRange(newSelects);
                selectedNodes = roots;
            }
        }

        /// <summary>
        /// should be called from within Update() whenever the flow is displaying its UI
        /// </summary>
        public void update()
        {
            Rect headerRect = new Rect(B.x, B.y + (UIScale * 10), rect.width, UIScale * 25);
            Rect eatInputArea = new Rect(A.x - (UIScale * 10), A.y - (UIScale * 10), rect.width + (UIScale * 20), rect.height + (UIScale * 50));

            bool mouseOverHeader = headerRect.Contains(Input.mousePosition);
            mouseOver = rect.Contains(Input.mousePosition);

            if (eatInputArea.Contains(Input.mousePosition))
            {
                eatingInput = true;
            }
            else eatingInput = false;

            anyNodeHovered = false;

            Event current = Event.current;


            if (draggingWindow)
            {
                if (!delta.HasValue) return;
                Vector2 m = (Vector2)Input.mousePosition - delta.Value;
                setPositionUI(m);
            }

            // drag connection
            bool hoveringAny = false;
            foreach (LogicFlowNode node in nodes.Values.Reverse())
            {
                if (node == null) continue;
                node.update();
                if (node.mouseOver) anyNodeHovered = true;
                if (node.outputHovered && (current.type == EventType.MouseDown || current.type == EventType.MouseDrag) && !LogicFlowNode.draggingAnyNode && !selectionStart.HasValue)
                {
                    dragConnectionSourceIndex = node.index;
                }
                if (dragConnectionSourceIndex.HasValue && node.inputHovered.HasValue && node.index != dragConnectionSourceIndex && !getNodeAt(dragConnectionSourceIndex.Value).getInputTree().Contains(node.index))
                {
                    if (current.type == EventType.MouseUp)
                    {
                        node.SetInput(dragConnectionSourceIndex.Value);
                    }
                    hoveringAny = true;
                }
            }
            if (hoveringAny) anyInputHovered = true;
            else anyInputHovered = false;


            // dragging expand
            if (new Rect(D.x - 10, D.y - 10, 20, 20).Contains(Input.mousePosition)
                && !anyNodeHovered
                && !selectionStart.HasValue
                && !dragConnectionSourceIndex.HasValue
                && !selectionStart.HasValue)
            {
                hoverExpand = true;
                if (current.type == EventType.MouseDrag && current.button == 0) draggingExpand = true;
            }
            else hoverExpand = false;

            if (draggingExpand)
            {
                setSizeUI(new Vector2(Input.mousePosition.x - B.x, B.y - Input.mousePosition.y));
                setPositionUI(new Vector2(B.x, Input.mousePosition.y));
            }

            // clear selection
            if (current.modifiers != EventModifiers.Shift
                && current.button == 0
                && current.type == EventType.MouseDown
                && mouseOver
                && !mouseOverHeader
                && !draggingWindow
                && !dragConnectionSourceIndex.HasValue
                && !anyNodeHovered
                ) selectedNodes.Clear();

            //selection
            if (current.type == EventType.MouseDown)
            {
                foreach(LogicFlowNode node in nodes.Values)
                {
                    if (node.mouseOver && !selectedNodes.Contains(node.index))
                    {
                        if (current.modifiers != EventModifiers.Shift)
                        {
                            selectedNodes.Clear();
                        }
                        selectedNodes.Add(node.index);
                    }
                }
            }
            if (dragConnectionSourceIndex.HasValue) selectionStart = null;
            if (!LogicFlowNode.draggingAnyNode
                && current.button == 0
                && !mouseOverHeader
                && !draggingWindow
                && !dragConnectionSourceIndex.HasValue
                && !draggingExpand)
            {
                if (!selectionStart.HasValue 
                    && (current.type == EventType.MouseDown || current.type == EventType.MouseDrag) 
                    && mouseOver)
                {
                    selectionStart = Input.mousePosition;
                }
                if (current.type == EventType.MouseUp && selectionStart.HasValue)
                {
                    if (current.modifiers != EventModifiers.Shift) selectedNodes.Clear();
                    Rect selectionRect = RectFromVectors(selectionStart.Value - A, (Vector2)Input.mousePosition - A);
                    foreach(LogicFlowNode node in nodes.Values.Reverse())
                    {
                        if (node == null) continue;
                        if (selectionRect.Contains(node.A) || selectionRect.Contains(node.B) || selectionRect.Contains(node.C) || selectionRect.Contains(node.D))
                        {
                            if (!selectedNodes.Contains(node.index)) selectedNodes.Add(node.index);
                        }
                    }
                }
            }

            // autoselector
            if (mouseOver && current.keyCode == KeySelectTree && current.modifiers == EventModifiers.Control && current.type == EventType.KeyDown)
            {
                selectTree();
            }
            if (mouseOver && current.keyCode == KeySelectNetwork && current.modifiers == EventModifiers.Control && current.type == EventType.KeyDown)
            {
                selectNetwork();
            }


            // drag window around
            if (mouseOverHeader
                && (current.type == EventType.MouseDown || current.type == EventType.MouseDrag)
                && current.button == 0
                && !anyNodeHovered)
            {
                if (!delta.HasValue) delta = (Vector2)Input.mousePosition - rect.position;
                draggingWindow = true;
            }

            // reset stuff on mouseUP
            if (current.type == EventType.MouseUp)
            {
                draggingWindow = false;
                dragConnectionSourceIndex = null;
                delta = null;
                draggingExpand = false;
                selectionStart = null;
            }
        }

        /// <summary>
        /// should be called from within OnPostRender whenever the flow is displaying its UI
        /// has to be enclosed by GL.PushMatrix() and GL.EndMatrix()
        /// </summary>
        public void draw()
        {
            // draw header
            GL.Begin(GL.QUADS);
            GL.Color(headerColor);
            GL.Vertex(translateToGL(B + new Vector2(0, 10) * UIScale));
            GL.Vertex(translateToGL(B + new Vector2(0, 35) * UIScale));
            GL.Vertex(translateToGL(C + new Vector2(0, 35) * UIScale));
            GL.Vertex(translateToGL(C + new Vector2(0, 10) * UIScale));
            GL.End();

            // draw background
            GL.Begin(GL.QUADS);
            GL.Color(backgroundColor);
            GL.Vertex(translateToGL(A));
            GL.Vertex(translateToGL(B));
            GL.Vertex(translateToGL(C));
            GL.Vertex(translateToGL(D));
            GL.End();

            foreach (LogicFlowNode node in nodes.Values)
            {
                if (node != null) node.draw();
            }

            if (dragConnectionSourceIndex.HasValue)
            {
                GL.Begin(GL.QUADS);
                GL.Color(anyInputHovered ? LogicFlowNode.connectorHoverColor : LogicFlowNode.lineColor);
                GL_DrawLineWithWidth(getNodeAt(dragConnectionSourceIndex.Value).outputScreenPos + A, Input.mousePosition, 2);
                GL.End();
            }

            if (selectionStart.HasValue)
            {
                Vector2 v1 = selectionStart.Value;
                Vector2 v2 = new Vector2(selectionStart.Value.x, Input.mousePosition.y);
                Vector2 v3 = Input.mousePosition;
                Vector2 v4 = new Vector2(Input.mousePosition.x, selectionStart.Value.y);
                GL.Begin(GL.QUADS);
                GL.Color(selectionColor);
                GL_DrawLineWithWidth(v1, v2, 3);
                GL_DrawLineWithWidth(v2, v3, 3);
                GL_DrawLineWithWidth(v3, v4, 3);
                GL_DrawLineWithWidth(v4, v1, 3);
                GL.End();
            }

            if (hoverExpand || draggingExpand)
            {
                GL.Begin(GL.TRIANGLE_STRIP);
                GL.Color(headerColor);
                GL.Vertex(translateToGL(D + new Vector2(-10, 0)));
                GL.Vertex(translateToGL(D + new Vector2(10, -10)));
                GL.Vertex(translateToGL(D));
                GL.Vertex(translateToGL(D + new Vector2(0, 10)));
                GL.End();
            }

        }

        protected override Rect initRect()
        {
            return new Rect(new Vector2(10,10), new Vector2(600,300) * UIScale);
        }
    }
}
