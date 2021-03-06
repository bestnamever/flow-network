﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flow_Network
{
    public partial class Main : Form
    {
        enum ActiveToolType
        {
            Pump,
            Splitter,
            Merger,
            AdjustableSplitter,
            Select,
            Delete,
            Sink,
            Pipe,
            None
        }

        public static List<Element> AllElements = new List<Element>();
        public static List<ConnectionZone.Path> AllPaths = new List<ConnectionZone.Path>();

        private ActiveToolType ActiveTool = ActiveToolType.None;
       
        private PictureBox iconBelowCursor;

        private Element PathStart;
        private Element PathEnd;

        private Element dragElement;
        private PictureBox oldDragElementPosition;
        private Point dragStart;

        private Point mousePosition = new Point(0, 0);

        PictureBox currentActiveToolPbox;

        public Main()
        {
            InitializeComponent();

            oldDragElementPosition = new PictureBox();
            oldDragElementPosition.Height = 32;
            oldDragElementPosition.SizeMode = PictureBoxSizeMode.StretchImage;
            oldDragElementPosition.Width = 32;
            oldDragElementPosition.BorderStyle = BorderStyle.FixedSingle;
            oldDragElementPosition.Visible = false;

            plDraw.Controls.Add(oldDragElementPosition);

            plDraw.Paint += plDraw_DrawPaths;

            Resources.PumpIcon = this.pictureBox2.Image;
            Resources.SinkIcon = this.pictureBox3.Image;
            Resources.MergerIcon = this.pictureBox6.Image;
            Resources.SplitterIcon = this.pictureBox4.Image;
            Resources.AdjSplitterIcon = this.pictureBox5.Image;
            iconBelowCursor = new PictureBox();
            iconBelowCursor.Width = 16;
            iconBelowCursor.Height = 16;
            iconBelowCursor.BackColor = Color.AliceBlue;
            iconBelowCursor.Visible = false;
            Controls.Add(iconBelowCursor);

            plDraw.MouseMove += plDraw_HandleDynamicIcon;
            plDraw.MouseMove += plDraw_MoveDragElement;
            plDraw.MouseDown += plDraw_HandleStartDrag;
            plDraw.MouseUp += plDraw_HandleStopDrag;

            plDraw.Click += plDraw_HandleClick;

            UndoStack.OnUndoAltered += (numberLeft, lastAction) =>
            {
                numberActionsToUndoLbl.Text = numberLeft.ToString();
                if (lastAction == null)
                    lastActionToUndoLbl.Text = "";
                else
                    lastActionToUndoLbl.Text = lastAction.ToString();

                if (lastAction is UndoableActions.RemoveConnection || lastAction is UndoableActions.AddConnection)
                    plDraw.Invalidate();
            };

            UndoStack.OnRedoAltered += (numberLeft, lastAction) =>
            {
                numberActionsRedone.Text = numberLeft.ToString();
                if (lastAction == null)
                    lastActionUndone.Text = "";
                else
                    lastActionUndone.Text = lastAction.ToString();

                if (lastAction is UndoableActions.RemoveConnection || lastAction is UndoableActions.AddConnection)
                    plDraw.Invalidate();
            };
        }

        protected void pboxToolClick(object sender, EventArgs e)
        {
            PictureBox clickedPbox = (PictureBox)sender;
            if (clickedPbox == null)
                return;
            if (currentActiveToolPbox != null)
            {
                currentActiveToolPbox.BackColor = Color.AliceBlue;
            }
            currentActiveToolPbox = clickedPbox;
            if (currentActiveToolPbox == pictureBox1)
                ActiveTool = ActiveToolType.Select;
            else if (currentActiveToolPbox == pictureBox2)
                ActiveTool = ActiveToolType.Pump;
            else if (currentActiveToolPbox == pictureBox3)
                ActiveTool = ActiveToolType.Sink;
            else if (currentActiveToolPbox == pictureBox4)
                ActiveTool = ActiveToolType.Splitter;
            else if (currentActiveToolPbox == pictureBox5)
                ActiveTool = ActiveToolType.AdjustableSplitter;
            else if (currentActiveToolPbox == pictureBox6)
                ActiveTool = ActiveToolType.Merger;
            else if (currentActiveToolPbox == pictureBox7)
                ActiveTool = ActiveToolType.Pipe;
            else if (currentActiveToolPbox == pictureBox8)
                ActiveTool = ActiveToolType.Delete;
            else
                ActiveTool = ActiveToolType.None;
            clickedPbox.BackColor = Color.Gold;
        }

        void plDraw_HandleClick(object sender, EventArgs e)
        {
            if (((MouseEventArgs)e).Button == MouseButtons.Right)
            {
                HandleRightClick();
                return;
            }

            switch (ActiveTool)
            {
                case ActiveToolType.Pump:
                case ActiveToolType.Sink:
                case ActiveToolType.Splitter:
                case ActiveToolType.AdjustableSplitter:
                case ActiveToolType.Merger:
                    HandleCreateElementToolClick(); return;
                case ActiveToolType.Pipe: HandleConnectionToolClick(); return;
                case ActiveToolType.Delete: HandleDeleteToolClick(); return;
                case ActiveToolType.Select: HandleSelectToolClick(); return;
                case ActiveToolType.None: return;
                default: throw new ArgumentException("Unknown tool " + ActiveTool);
            }
        }
        #region tools
        void HandleSelectToolClick()
        {

        }

        void HandleDeleteToolClick()
        {
            Element e = FindCollisionUnder(mousePosition);
            if(e!=null)
                RemoveElement(e);
        }

        void HandleCreateElementToolClick()
        {
            if (HasCollision(mousePosition))
            {
                return;
            }

            Element elementToAdd = null;

            if (ActiveTool == ActiveToolType.Pump)
            {
                elementToAdd = new Pump();
            }
            else if (ActiveTool == ActiveToolType.Sink)
            {
                elementToAdd = new Sink();
            }
            else if (ActiveTool == ActiveToolType.Splitter)
            {
                elementToAdd = new Splitter();
            }
            else if (ActiveTool == ActiveToolType.AdjustableSplitter)
            {
                elementToAdd = new AdjustableSplitter();
            }
            else if (ActiveTool == ActiveToolType.Merger)
            {
                elementToAdd = new Merger();
            }
            if (elementToAdd != null)
            {
                AddElement(elementToAdd, mousePosition);
            }
            else
                throw new ArgumentException("Unknown element " + ActiveTool);
        }

        void HandleConnectionToolClick()
        {
            Element hovered = FindCollisionUnder(mousePosition);
            if (hovered == null) return;

            if (PathStart == null) PathStart = hovered;
            else PathEnd = hovered;

            if (PathStart != null && PathEnd != null)
            {
                ConnectionZone.Path result = new ConnectionZone.Path(new ConnectionZone(new Point(), PathStart),
                    new ConnectionZone(new Point(), PathEnd));

                result.OnCreated += () =>
                {
                    AllPaths.Add(result);
                    plDraw.Invalidate();
                };

                result.OnAdjusted += () =>
                {
                    plDraw.Invalidate();
                };

                result.Adjust();

                UndoStack.AddAction(new UndoableActions.AddConnection(result));

                PathStart = null;
                PathEnd = null;
            }

        }
        #endregion
        #region drag
        void plDraw_HandleStopDrag(object sender, MouseEventArgs e)
        {
            if (HasCollision(mousePosition))
            {
                RevertDrag();
            }
            oldDragElementPosition.Visible = false;
            dragElement = null;
            plDraw.Cursor = Cursors.Arrow;
        }

        void plDraw_HandleStartDrag(object sender, MouseEventArgs e)
        {
            if (ActiveTool == ActiveToolType.Select)
            {
                if (dragElement == null)
                {
                    dragElement = FindCollisionUnder(mousePosition);
                    if (dragElement != null)
                    {
                        dragStart = dragElement.PictureBox.Location;
                        oldDragElementPosition.Visible = true;
                        oldDragElementPosition.Location = dragStart;
                        oldDragElementPosition.Image = dragElement.PictureBox.Image;
                    }
                }
                else
                {
                    dragElement.PictureBox.Location = mousePosition;
                }
            }
        }

        void plDraw_MoveDragElement(object sender, MouseEventArgs e)
        {
            if (dragElement == null) return;
            if (dragElement.PictureBox.Location != e.Location)
            {
                dragElement.PictureBox.Location = e.Location;
                RefreshConnections();
            }
        }

        private void RevertDrag()
        {
            if (dragElement == null) return;

            dragElement.PictureBox.Location = dragStart;
            oldDragElementPosition.Visible = false;
            dragElement = null;

            RefreshConnections();

        }

        #endregion
        void plDraw_HandleDynamicIcon(object sender, MouseEventArgs evnt)
        {
            if (ActiveTool == ActiveToolType.Select)
            {
                Element e = FindCollisionUnder(mousePosition);
                if (e != null || dragElement != null)
                {
                    this.Cursor = Cursors.SizeAll;
                }
                else
                    this.Cursor = Cursors.Arrow;
            }

            mousePosition = evnt.Location;
            if (ActiveTool == ActiveToolType.None)
            {
                iconBelowCursor.Visible = false;
                iconBelowCursor.BackColor = Color.Bisque;
            }
            else
            {
                iconBelowCursor.Visible = true;
                Point point = evnt.Location;
                point.Offset(plDraw.Location);
                point.Offset(16, 16);
                this.iconBelowCursor.Location = point;
                if (HasCollision(mousePosition))
                {
                    iconBelowCursor.BackColor = Color.Red;
                }
                else iconBelowCursor.BackColor = Color.Green;
                iconBelowCursor.BringToFront();
            }
        }

        void plDraw_DrawPaths(object sender, PaintEventArgs e)
        {
            foreach (ConnectionZone.Path path in new List<ConnectionZone.Path>(AllPaths))
            {
                Point previous = path.From;
                foreach (Point point in path.PathPoints)
                {
                    e.Graphics.DrawLine(Pens.Black, previous, point);
                    previous = point;
                }
            }
        }

        #region AddElement Remove

        void RemoveElement(Element e)
        {
            AllElements.Remove(e);
            plDraw.Controls.Remove(e.PictureBox);
            UndoStack.AddAction(new UndoableActions.RemoveElement(e, plDraw));

            RefreshConnections(e);
        }

        void AddElement(Element e, Point position)
        {
            e.PictureBox.Enabled = false;

            e.X = position.X;
            e.Y = position.Y;

            this.plDraw.Controls.Add(e.PictureBox);
            AllElements.Add(e);

            UndoStack.AddAction(new UndoableActions.AddElement(e));

            RefreshConnections(e);
        }

        private void RefreshConnections(Element e = null)
        {
            if (e == null)
                foreach (Element item in AllElements)
                    item.RefreshConnections();
            else
                foreach (Element item in AllElements)
                    if (item == e)
                        continue;
                    else
                        item.RefreshConnections();

            plDraw.Invalidate();
        }

        void AddElement<T>(Point position) where T : Element
        {
            Element e = Activator.CreateInstance<T>();
            AddElement(e, position);
        }
        #endregion

        #region rightClick
        [Flags]
        enum RightClickOptions
        {
            Sink,
            Pump,
            Splitter,
            Merger,
            Adjustable,
            Remove,
            Cancel
        }

        Panel rightClickPanel;
        Point rightClickMousePosition = new Point();

        private void HandleRightClick()
        {
            if (ActiveTool == ActiveToolType.Select)
            {
                RevertDrag();
                return;
            }

            RightClickOptions options = ~RightClickOptions.Remove;
            rightClickMousePosition = mousePosition;

            if (HasCollision(rightClickMousePosition))
            {
                Element e = FindCollisionUnder(rightClickMousePosition);
                if (e != null)
                    options = RightClickOptions.Remove;
            }

            if (rightClickPanel == null)
            {
                rightClickPanel = new Panel();
                plDraw.Controls.Add(rightClickPanel);

                rightClickPanel.Width = 100;
                rightClickPanel.Height = 140;

                rightClickPanel.AddButton("Remove", 0, (x, y) =>
                {
                    Element e = FindCollisionUnder(rightClickMousePosition);
                    if (e == null) return;
                    else
                    {
                        RemoveElement(e);
                    }
                }).Name = "remove";
                rightClickPanel.AddButton("Add Pump", 20, (x, y) => { AddElement<Pump>(rightClickMousePosition); });
                rightClickPanel.AddButton("Add Sink", 40, (x, y) => { AddElement<Sink>(rightClickMousePosition); });
                rightClickPanel.AddButton("Add Splitter", 60, (x, y) => { AddElement<Splitter>(rightClickMousePosition); });
                rightClickPanel.AddButton("Add Adjustable", 80, (x, y) => { AddElement<AdjustableSplitter>(rightClickMousePosition); });
                rightClickPanel.AddButton("Add Merger", 100, (x, y) => { AddElement<Merger>(rightClickMousePosition); });
                rightClickPanel.AddButton("Cancel", 120).Name = "cancel";

                foreach (var item in rightClickPanel.Controls)
                {
                    if (item is Button)
                    {
                        (item as Button).Click += (x, y) => rightClickPanel.Visible = false;
                    }
                }
            }
            foreach (var item in rightClickPanel.Controls)
            {
                if (item is Button)
                {
                    (item as Button).Enabled = true;
                }
            }
            if (!options.HasFlag(RightClickOptions.Remove))
                rightClickPanel.Controls.Find("remove", false)[0].Enabled = false;
            else
            {
                foreach (var item in rightClickPanel.Controls)
                {
                    if (item is Button)
                    {
                        (item as Button).Enabled = false;
                    }
                }
                rightClickPanel.Controls.Find("remove", false)[0].Enabled = true;
            }

            rightClickPanel.Controls.Find("cancel", false)[0].Enabled = true;

            rightClickPanel.Location = rightClickMousePosition;
            rightClickPanel.Visible = true;
            rightClickPanel.BringToFront();
            rightClickPanel.BackColor = Color.FromArgb(255, 157, 157, 157);
        }
        #endregion
        #region collision,element detection
        private Element FindCollisionUnder(Point mousePosition)
        {
            return AllElements.FirstOrDefault(q =>
                {
                    if (q == dragElement) return false;

                    if (q.X <= mousePosition.X && q.X + q.Width >= mousePosition.X)
                        if (q.Y <= mousePosition.Y && q.Y + q.Height >= mousePosition.Y)
                            return true;
                    return false;
                });
        }

        private Element FindCollisionElement(Point mousePosition)
        {
            return AllElements.FirstOrDefault(q =>
            {
                if (q == dragElement) return false;
                Point position = mousePosition;

                if (q.X - q.PictureBox.Width <= position.X && q.X + q.PictureBox.Width >= position.X)
                    if (q.Y - q.PictureBox.Height <= position.Y && q.Y + q.PictureBox.Height >= position.Y)
                        return true;
                return false;
            });
        }

        private bool HasCollision(Point mousePosition)
        {
            return FindCollisionElement(mousePosition) != null;
        }
        #endregion

        private void undoButton_Click(object sender, EventArgs e)
        {
            UndoStack.Undo();
        }

        private void redoButton_Click(object sender, EventArgs e)
        {
            UndoStack.Redo();
        }

        private void btnNew_Click(object sender, EventArgs e)
        {

        }

        private void btnLoad_Click(object sender, EventArgs e)
        {

        }

        private void btnSave_Click(object sender, EventArgs e)
        {

        }
    }
}

static class E
{
    public static Button AddButton(this Panel panel, string text, int top, EventHandler onClick = null)
    {
        Button button = new Button();
        button.Text = text;
        button.Width = panel.Width;
        button.Height = 20;
        button.Top = top;
        if(onClick != null)
            button.Click += onClick;

        panel.Controls.Add(button);

        return button;
    }
}

