using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ResimGösterici
{
    public class VirtualizingTilePanel : VirtualizingPanel, IScrollInfo
    {
        public double Columns
        {
            get { return (double)GetValue(ColumnsProperty); }
            set
            {
                SetValue(ColumnsProperty, value);
            }
        }

        public static readonly DependencyProperty ColumnsProperty
            = DependencyProperty.RegisterAttached(nameof(Columns), typeof(double), typeof(VirtualizingTilePanel), new FrameworkPropertyMetadata(Double.NaN, OnItemsSourceChanged));


        public static readonly DependencyProperty ItemWidthProperty
           = DependencyProperty.RegisterAttached(nameof(ItemWidth), typeof(double), typeof(VirtualizingTilePanel), new FrameworkPropertyMetadata(150d, OnItemsSourceChanged));

        public double ItemWidth
        {
            get
            {
                if (Double.IsNaN(Columns))
                {
                    return (double)GetValue(ItemWidthProperty);
                }
                else
                {
                    return Math.Floor(ViewportWidth / Columns);
                }
            }
            set { SetValue(ItemWidthProperty, value); }
        }

        public static readonly DependencyProperty ItemHeightModifierProperty
            = DependencyProperty.RegisterAttached(nameof(ItemHeightModifier), typeof(double), typeof(VirtualizingTilePanel), new FrameworkPropertyMetadata(1.5, OnItemsSourceChanged));

        public double ItemHeightModifier
        {
            get { return (double)GetValue(ItemHeightModifierProperty); }
            set { SetValue(ItemHeightModifierProperty, value); }
        }

        public double ItemHeight
        {
            get => ItemWidth * ItemHeightModifier;
        }

        private static void OnItemsSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var panel = obj as VirtualizingTilePanel;
            if (panel._itemsControl == null)
            {
                return;
            }

            panel.InvalidateMeasure();
            panel.ScrollOwner?.InvalidateScrollInfo();

            if (panel.currentlyVisible != null)
            {
                var index = panel.GeneratorContainer.IndexFromContainer(panel.currentlyVisible);
                if (index >= 0)
                {
                    panel.MakeVisible(panel.currentlyVisible, new Rect(new Size(panel.ItemWidth, panel.ItemHeight)));
                }
                else
                {
                    panel.SetVerticalOffset(0);
                }
            }
        }

        private IRecyclingItemContainerGenerator Generator;

        private ItemContainerGenerator GeneratorContainer
        {
            get => (ItemContainerGenerator)Generator;
        }

        public ItemsControl _itemsControl;

        public VirtualizingTilePanel()
        {

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                Dispatcher.BeginInvoke((Action)delegate
                {
                    _itemsControl = ItemsControl.GetItemsOwner(this);
                    Generator = (IRecyclingItemContainerGenerator)ItemContainerGenerator;
                    InvalidateMeasure();
                });
            }

            RenderTransform = _trans;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_itemsControl == null)
            {
                if (availableSize.Width == double.PositiveInfinity || availableSize.Height == double.PositiveInfinity)
                {
                    return Size.Empty;
                }
                else
                {
                    return availableSize;
                }
            }

            UpdateScrollInfo(availableSize);

            GetVisibleRange(out var firstVisibleItemIndex, out var lastVisibleItemIndex);
            if (lastVisibleItemIndex < 0)
            {
                return availableSize;
            }

            UIElementCollection children = InternalChildren;

            CleanUpItems(firstVisibleItemIndex, lastVisibleItemIndex);

            GeneratorPosition startPos = Generator.GeneratorPositionFromIndex(firstVisibleItemIndex);

            int childIndex = (startPos.Offset == 0) ? startPos.Index : startPos.Index + 1;

            using (Generator.StartAt(startPos, GeneratorDirection.Forward, true))
            {
                for (int itemIndex = firstVisibleItemIndex; itemIndex <= lastVisibleItemIndex; ++itemIndex, ++childIndex)
                {
                    UIElement child = Generator.GenerateNext(out var newlyRealized) as UIElement;

                    if (newlyRealized)
                    {
                        if (childIndex >= children.Count)
                        {
                            AddInternalChild(child);
                        }
                        else
                        {
                            InsertInternalChild(childIndex, child);
                        }
                        Generator.PrepareItemContainer(child);
                    }
                    else if (!InternalChildren.Contains(child))
                    {
                        InsertInternalChild(childIndex, child);
                        ItemContainerGenerator.PrepareItemContainer(child);
                    }

                    child.Measure(GetInitialChildSize(child));
                }
            }

            return availableSize;
        }

        private void CleanUpItems(int minDesiredGenerated, int maxDesiredGenerated)
        {
            for (int i = Children.Count - 1; i >= 0; i--)
            {
                GeneratorPosition childGeneratorPosition = new GeneratorPosition(i, 0);
                int iIndex = ItemContainerGenerator.IndexFromGeneratorPosition(childGeneratorPosition);
                if ((iIndex < minDesiredGenerated || iIndex > maxDesiredGenerated) && iIndex > 0)
                {
                    Generator.Recycle(childGeneratorPosition, 1);
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                UIElement child = Children[i];

                int itemIndex = Generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));

                ArrangeChild(itemIndex, child, finalSize);
            }

            UpdateScrollInfo(finalSize);
            return finalSize;
        }

        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Move:
                    RemoveInternalChildRange(args.OldPosition.Index, args.ItemUICount);
                    break;
            }
        }

        #region Layout specific code

        private void GetVisibleRange(out int firstVisibleItemIndex, out int lastVisibleItemIndex)
        {
            int itemCount = _itemsControl.HasItems ? _itemsControl.Items.Count : 0;
            if (itemCount == 0)
            {
                firstVisibleItemIndex = -1;
                lastVisibleItemIndex = -1;
                return;
            }

            int childrenPerRow = CalculateChildrenPerRow(_extent);
            var rows = 0;
            double totalHeight = 0;
            while (_offset.Y > totalHeight + ItemHeight)
            {
                totalHeight += ItemHeight;
                rows++;
            }

            firstVisibleItemIndex = (int)((rows == 0 ? rows : rows) * childrenPerRow);
            var newRows = (int)Math.Ceiling(_viewport.Height / ItemHeight) + 1;
            lastVisibleItemIndex = firstVisibleItemIndex + (newRows * childrenPerRow);
            if (lastVisibleItemIndex >= itemCount)
            {
                lastVisibleItemIndex = itemCount - 1;
            }
        }

        private Size GetInitialChildSize(UIElement child) => new Size(ItemWidth, ItemHeight);

        public IContainItemStorage GetItemStorageProvider() => _itemsControl as IContainItemStorage;

        private int GetItemRow(int itemIndex, int itemPerRow)
        {
            int column = itemIndex % itemPerRow;
            return itemIndex < column ? 0 : (int)Math.Floor(itemIndex / (double)itemPerRow);
        }

        private void ArrangeChild(int itemIndex, UIElement child, Size finalSize)
        {
            int childrenPerRow = CalculateChildrenPerRow(finalSize);
            int column = itemIndex % childrenPerRow;
            int row = GetItemRow(itemIndex, childrenPerRow);
            var targetRect = new Rect(
                column * ItemWidth,
                GetTotalHeightForRow(row),
                ItemWidth,
                ItemHeight);

            child.Arrange(targetRect);
            _ = GeneratorContainer.ItemFromContainer(child);
        }

        private int CalculateChildrenPerRow(Size availableSize)
        {
            if (!Double.IsNaN(Columns))
            {
                return Convert.ToInt32(Columns);
            }

            return availableSize.Width == Double.PositiveInfinity
                ? Children.Count
                : Math.Max(1, (int)Math.Floor(availableSize.Width / ItemWidth));
        }

        #endregion

        #region IScrollInfo implementation

        private double GetTotalHeightForRow(int row) => (ItemHeight * row);

        private double GetTotalHeight(Size availableSize)
        {
            int itemCount = _itemsControl.HasItems ? _itemsControl.Items.Count : 0;
            var perRow = CalculateChildrenPerRow(availableSize);
            var rows = Math.Ceiling(itemCount / (double)perRow);

            double totalHeight = 0;
            for (var i = 0; i < rows; i++)
            {
                totalHeight += ItemHeight;
            }

            return totalHeight;
        }

        private void UpdateScrollInfo(Size availableSize)
        {
            if (_itemsControl == null)
            {
                return;
            }

            _ = _itemsControl.HasItems ? _itemsControl.Items.Count : 0;
            var perRow = CalculateChildrenPerRow(availableSize);
            var totalHeight = GetTotalHeight(availableSize);

            if (_offset.Y > totalHeight)
            {
                _offset.Y = 0;
                _trans.Y = 0;
            }

            Size extent = new Size(perRow * ItemWidth, totalHeight);

            if (extent != _extent)
            {
                _extent = extent;
                ScrollOwner?.InvalidateScrollInfo();
            }

            if (availableSize != _viewport)
            {
                _viewport = availableSize;
                ScrollOwner?.InvalidateScrollInfo();
            }
        }

        public ScrollViewer ScrollOwner { get; set; }

        public bool CanHorizontallyScroll { get; set; } = false;

        public bool CanVerticallyScroll
        {
            get { return _canVScroll; }
            set { _canVScroll = value; }
        }

        public double HorizontalOffset
        {
            get { return _offset.X; }
        }

        public double VerticalOffset
        {
            get { return _offset.Y; }
        }

        public double ExtentHeight
        {
            get { return _extent.Height; }
        }

        public double ExtentWidth
        {
            get { return _extent.Width; }
        }

        public double ViewportHeight
        {
            get { return _viewport.Height; }
        }

        public double ViewportWidth
        {
            get { return _viewport.Width; }
        }

        public void LineUp() => SetVerticalOffset(VerticalOffset - ItemHeight);

        public void LineDown() => SetVerticalOffset(VerticalOffset + ItemHeight);

        public void PageUp() => SetVerticalOffset(VerticalOffset - _viewport.Height);

        public void PageDown() => SetVerticalOffset(VerticalOffset + _viewport.Height);

        public void MouseWheelUp() => SetVerticalOffset(VerticalOffset - ItemHeight);

        public void MouseWheelDown() => SetVerticalOffset(VerticalOffset + ItemHeight);

        public void LineLeft()
        {

        }

        public void LineRight()
        {

        }

        private Visual currentlyVisible;

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            var index = GeneratorContainer.IndexFromContainer(visual);
            if (index < 0)
            {
                return rectangle;
            }

            currentlyVisible = visual;
            var perRow = CalculateChildrenPerRow(_extent);
            var row = GetItemRow(index, perRow);
            var offset = GetTotalHeightForRow(row);
            var offsetSize = offset + ItemHeight;
            var offsetBottom = _offset.Y + _viewport.Height;
            if (offset > _offset.Y && offsetSize < offsetBottom)
            {
                return rectangle;
            }
            else if (offset > _offset.Y && (offsetBottom - offset < ItemHeight))
            {
                offset = _offset.Y + (ItemHeight - (offsetBottom - offset));
            }
            else if (Math.Floor((offsetBottom - offset)) == Math.Floor(ItemHeight))
            {
                return rectangle;
            }

            _offset.Y = offset;
            _trans.Y = -offset;
            InvalidateMeasure();
            return rectangle;
        }

        public void MouseWheelLeft()
        {

        }

        public void MouseWheelRight()
        {

        }

        public void PageLeft()
        {

        }

        public void PageRight()
        {

        }

        public void SetHorizontalOffset(double offset)
        {

        }

        public void SetVerticalOffset(double offset)
        {
            if (offset < 0 || _viewport.Height >= _extent.Height)
            {
                offset = 0;
            }
            else
            {
                if (offset + _viewport.Height >= _extent.Height)
                {
                    offset = _extent.Height - _viewport.Height;
                }
            }

            _offset.Y = offset;

            ScrollOwner?.InvalidateScrollInfo();

            _trans.Y = -offset;
            InvalidateMeasure();
        }

        private TranslateTransform _trans = new TranslateTransform();
        private bool _canVScroll = false;
        private Size _extent = new Size(0, 0);
        private Size _viewport = new Size(0, 0);
        private Point _offset;

        #endregion

    }
}