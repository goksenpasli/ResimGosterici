using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ResimGösterici
{
    public class VirtualizingUniformSizeWrapPanel : VirtualizingPanel, IScrollInfo
    {
        #region ItemSize

        #region ItemWidth

        public static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register(
                "ItemWidth",
                typeof(double),
                typeof(VirtualizingUniformSizeWrapPanel),
                new FrameworkPropertyMetadata(
                    double.NaN,
                    FrameworkPropertyMetadataOptions.AffectsMeasure
                ),
                new ValidateValueCallback(IsWidthHeightValid)
            );

        [TypeConverter(typeof(LengthConverter))]
        public double ItemWidth
        {
            get { return (double)GetValue(ItemWidthProperty); }
            set { SetValue(ItemWidthProperty, value); }
        }

        #endregion

        #region ItemHeight

        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register(
                "ItemHeight",
                typeof(double),
                typeof(VirtualizingUniformSizeWrapPanel),
                new FrameworkPropertyMetadata(
                    double.NaN,
                    FrameworkPropertyMetadataOptions.AffectsMeasure
                ),
                new ValidateValueCallback(IsWidthHeightValid)
            );

        [TypeConverter(typeof(LengthConverter))]
        public double ItemHeight
        {
            get { return (double)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        #endregion

        #region IsWidthHeightValid
        private static bool IsWidthHeightValid(object value)
        {
            double d = (double)value;
            return double.IsNaN(d) || ((d >= 0) && !double.IsPositiveInfinity(d));
        }
        #endregion

        #endregion

        #region Orientation

        public static readonly DependencyProperty OrientationProperty =
            WrapPanel.OrientationProperty.AddOwner(
                typeof(VirtualizingUniformSizeWrapPanel),
                new FrameworkPropertyMetadata(
                    Orientation.Horizontal,
                    FrameworkPropertyMetadataOptions.AffectsMeasure,
                    new PropertyChangedCallback(OnOrientationChanged)
                )
            );

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            VirtualizingUniformSizeWrapPanel panel = d as VirtualizingUniformSizeWrapPanel;
            panel.offset = default;
            panel.InvalidateMeasure();
        }

        #endregion

        #region MeasureOverride, ArrangeOverride

        private Size PrevViewPortSize { set; get; }

        private readonly ConcurrentDictionary<int, Rect> containerLayouts = new();

        private double GetLogicalWidth(Size size)
        {
            bool isHorizontal = Orientation == Orientation.Horizontal;
            return isHorizontal ? size.Width : size.Height;
        }

        private double GetLogicalHeight(Size size)
        {
            bool isHorizontal = Orientation == Orientation.Horizontal;
            return isHorizontal ? size.Height : size.Width;
        }

        private double GetLogicalX(Point point)
        {
            bool isHorizontal = Orientation == Orientation.Horizontal;
            return isHorizontal ? point.X : point.Y;
        }

        private double GetLogicalY(Point point)
        {
            bool isHorizontal = Orientation == Orientation.Horizontal;
            return isHorizontal ? point.Y : point.X;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            bool isHorizontal = Orientation == Orientation.Horizontal;

            Size maxSize = default(Size);
            if (GetLogicalWidth(availableSize) < 1) return maxSize;

            bool isChaigeViewPortSize = PrevViewPortSize != availableSize;
            if (isChaigeViewPortSize)
            {
                Console.WriteLine("ViewPoirt:" + availableSize);
                containerLayouts.Clear();
            }

            int prevColumCount = Math.Max(1, Convert.ToInt32(GetLogicalWidth(PrevViewPortSize)) / Convert.ToInt32(GetLogicalWidth(prevSize)));

            int vy = Convert.ToInt32(offset.Y) / Convert.ToInt32(prevSize.Height);
            int vx = Convert.ToInt32(GetLogicalX(offset)) / Convert.ToInt32(prevSize.Width);
            int first = vx + vy * prevColumCount;

            double vey = (Convert.ToInt32(GetLogicalY(offset)) + GetLogicalHeight(PrevViewPortSize)) / Convert.ToInt32(GetLogicalHeight(prevSize)) + 2;
            double vex = (Convert.ToInt32(GetLogicalX(offset)) + GetLogicalWidth(PrevViewPortSize)) / Convert.ToInt32(GetLogicalWidth(prevSize)) + 1;
            int end = Convert.ToInt32(vex + vey * prevColumCount) + 1;

            int childrenCount = 0;
            ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
            if (itemsControl != null)
            {
                childrenCount = itemsControl.Items.Count;
            }
            if (childrenCount < 1) return maxSize;

            if (isChaigeViewPortSize)
            {
                if (first < 1)
                {
                    first = 0;
                    end = 1000;
                }
                if (end >= childrenCount)
                {
                    end = childrenCount;
                    first = end - 1000;
                }
            }

            int firstIndex = Math.Max(0, first);
            int endIndex = Math.Min(end, childrenCount);
            Console.WriteLine("Index:{0},{1}", first, end);

            int columCount = Math.Max(1, Convert.ToInt32(GetLogicalWidth(availableSize)) / Convert.ToInt32(GetLogicalWidth(prevSize)));

            Point adjustViewPortOffset = offset;
            if (isChaigeViewPortSize)
            {
                double pvy = GetLogicalY(adjustViewPortOffset);

                pvy = pvy * prevColumCount / columCount;
                if (isHorizontal)
                {
                    adjustViewPortOffset.Y = pvy;
                }
                else
                {
                    adjustViewPortOffset.X = pvy;
                }
            }

            bool continium = false;

            Size childSize = prevSize;

            int endCheckIndex = endIndex;
            for (int i = firstIndex; i < endCheckIndex; i++)
            {
                int x = (int)((i % columCount) * GetLogicalWidth(childSize));
                int y = (int)((i / columCount) * GetLogicalHeight(childSize));

                Rect itemRect = new(isHorizontal ? x : y, isHorizontal ? y : x, childSize.Width, childSize.Height);
                Rect viewportRect = new(adjustViewPortOffset, availableSize);
                if (itemRect.IntersectsWith(viewportRect))
                {
                    containerLayouts[i] = new Rect(isHorizontal ? x : y, isHorizontal ? y : x, childSize.Width, childSize.Height);

                    if (!continium)
                    {
                        firstIndex = i;
                    }
                    //endIndex = i;

                    continium = true;
                    continue;
                }

                if (continium) break;
            }

            using (ChildGenerator generator = new(this, firstIndex, endIndex))
            {
                const int limitTime = 250;

                Stopwatch stopWatch = new();

                for (int i = firstIndex; i < endIndex; i++)
                {
                    {
                        UIElement child = generator.GetChild(i);
                        if (child == null)
                        {
                            stopWatch.Start();
                            child = generator.GetOrCreateChild(i);
                        }

                        stopWatch.Stop();
                        if (stopWatch.ElapsedTicks > limitTime)
                        {
                            Dispatcher.BeginInvoke((Action)(() => InvalidateMeasure()));
                            break;
                        }

                        continium = true;
                        continue;
                    }

                }
                generator.CleanupChildren();
            }

            int row = childrenCount / columCount + ((childrenCount % columCount == 0) ? 0 : 1);

            if (isHorizontal)
            {
                maxSize = new Size(availableSize.Width, Math.Max(row * childSize.Height, availableSize.Height));
            }
            else
            {
                maxSize = new Size(Math.Max(row * childSize.Width, availableSize.Width), availableSize.Height);
            }

            PrevViewPortSize = availableSize;

            extent = maxSize;
            viewport = availableSize;

            SetVerticalOffset(adjustViewPortOffset.Y);

            ScrollOwner?.InvalidateScrollInfo();

            return maxSize;
        }

        #region ChildGenerator
        private class ChildGenerator : IDisposable
        {
            #region fields

            private readonly VirtualizingUniformSizeWrapPanel owner;

            private readonly IItemContainerGenerator generator;

            private IDisposable generatorTracker;

            private int firstGeneratedIndex;

            private int lastGeneratedIndex;

            private int currentGenerateIndex;

            #endregion

            #region _ctor

            public ChildGenerator(VirtualizingUniformSizeWrapPanel owner)
            {
                this.owner = owner;

                int childrenCount = owner.InternalChildren.Count;
                generator = owner.ItemContainerGenerator;

                int first = int.MaxValue;
                int last = -1;
                for (int i = 0; i < childrenCount; i++)
                {
                    GeneratorPosition childPos = new(i, 0);
                    int index = generator.IndexFromGeneratorPosition(childPos);

                    first = Math.Min(first, index);
                    last = Math.Max(last, index);
                }

                if (first < int.MaxValue)
                    firstGeneratedIndex = first;
                else
                    firstGeneratedIndex = -1;

                if (last >= 0) lastGeneratedIndex = last;

                Console.WriteLine("F{0}:L{1}", firstGeneratedIndex, lastGeneratedIndex);
            }

            public ChildGenerator(VirtualizingUniformSizeWrapPanel owner, int firstIndex, int endIndex)
            {
                this.owner = owner;
                _ = owner.InternalChildren.Count;
                generator = owner.ItemContainerGenerator;

                firstGeneratedIndex = firstIndex;
                lastGeneratedIndex = endIndex;

                Console.WriteLine("F{0}:L{1}", firstGeneratedIndex, lastGeneratedIndex);
            }

            ~ChildGenerator()
            {
                Dispose();
            }

            public void Dispose()
            {
                generatorTracker?.Dispose();
            }

            #endregion

            #region GetOrCreateChild

            private void BeginGenerate(int index)
            {
                if (firstGeneratedIndex < 0)
                {
                    firstGeneratedIndex = index;
                }
                else
                {
                    firstGeneratedIndex = Math.Min(firstGeneratedIndex, index);
                }

                GeneratorPosition startPos = generator.GeneratorPositionFromIndex(index);
                currentGenerateIndex = (startPos.Offset == 0) ? startPos.Index : startPos.Index + 1;
                generatorTracker = generator.StartAt(startPos, GeneratorDirection.Forward, true);
            }

            public UIElement GetChild(int index)
            {
                if (generator == null)
                    return owner.InternalChildren[index];

                if (generator is ItemContainerGenerator itemGenerator)
                {
                    DependencyObject item = itemGenerator.ContainerFromIndex(index);
                    if (item != null)
                        return item as UIElement;
                }
                return null;
            }

            public UIElement GetOrCreateChild(int index)
            {
                if (generator == null)
                    return owner.InternalChildren[index];

                if (generator is ItemContainerGenerator itemGenerator)
                {
                    DependencyObject item = itemGenerator.ContainerFromIndex(index);
                    if (item != null)
                        return item as UIElement;
                }

                if (generatorTracker == null)
                    BeginGenerate(index);

                UIElement child = generator.GenerateNext(out bool newlyRealized) as UIElement;
                if (newlyRealized)
                {
                    if (currentGenerateIndex >= owner.InternalChildren.Count)
                        owner.AddInternalChild(child);
                    else
                        owner.InsertInternalChild(currentGenerateIndex, child);

                    generator.PrepareItemContainer(child);
                }

                lastGeneratedIndex = index;
                currentGenerateIndex++;

                return child;
            }

            #endregion

            #region CleanupChildren
            public void CleanupChildren()
            {
                if (generator == null)
                    return;

                UIElementCollection children = owner.InternalChildren;

                for (int i = children.Count - 1; i >= 0; i--)
                {
                    GeneratorPosition childPos = new(i, 0);
                    int index = generator.IndexFromGeneratorPosition(childPos);
                    if (index < firstGeneratedIndex || index > lastGeneratedIndex)
                    {
                        generator.Remove(childPos, 1);
                        owner.RemoveInternalChildRange(i, 1);
                    }
                }
            }
            #endregion
        }
        #endregion

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement child in InternalChildren)
            {
                int index = (ItemContainerGenerator is ItemContainerGenerator gen) ? gen.IndexFromContainer(child) : InternalChildren.IndexOf(child);
                if (containerLayouts.ContainsKey(index))
                {
                    Rect layout = containerLayouts[index];
                    layout.Offset(offset.X * -1, offset.Y * -1);

                    child.Arrange(layout);
                }
            }

            return finalSize;      
        }

        #endregion

        #region ContainerSizeForIndex

        private Size prevSize = new(125, 125);

        #endregion

        #region OnItemsChanged
        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                    break;
            }
        }
        #endregion

        #region IScrollInfo Members

        #region Extent

        private Size extent;

        public double ExtentHeight
        {
            get { return extent.Height; }
        }

        public double ExtentWidth
        {
            get { return extent.Width; }
        }

        #endregion Extent

        #region Viewport

        private Size viewport;

        public double ViewportHeight
        {
            get { return viewport.Height; }
        }

        public double ViewportWidth
        {
            get { return viewport.Width; }
        }

        #endregion

        #region Offset

        private Point offset;

        public double HorizontalOffset
        {
            get { return offset.X; }
        }

        public double VerticalOffset
        {
            get { return offset.Y; }
        }

        #endregion

        #region ScrollOwner
        public ScrollViewer ScrollOwner { get; set; }
        #endregion

        #region CanHorizontallyScroll
        public bool CanHorizontallyScroll { get; set; }
        #endregion

        #region CanVerticallyScroll
        public bool CanVerticallyScroll { get; set; }
        #endregion

        #region LineUp
        public void LineUp()
        {
            SetVerticalOffset(VerticalOffset - SystemParameters.ScrollHeight);
        }
        #endregion

        #region LineDown
        public void LineDown()
        {
            SetVerticalOffset(VerticalOffset + SystemParameters.ScrollHeight);
        }
        #endregion

        #region LineLeft
        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset - SystemParameters.ScrollWidth);
        }
        #endregion

        #region LineRight
        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset + SystemParameters.ScrollWidth);
        }
        #endregion

        #region PageUp
        public void PageUp()
        {
            SetVerticalOffset(VerticalOffset - viewport.Height);
        }
        #endregion

        #region PageDown
        public void PageDown()
        {
            SetVerticalOffset(VerticalOffset + viewport.Height);
        }
        #endregion

        #region PageLeft
        public void PageLeft()
        {
            SetHorizontalOffset(HorizontalOffset - viewport.Width);
        }
        #endregion

        #region PageRight
        public void PageRight()
        {
            SetHorizontalOffset(HorizontalOffset + viewport.Width);
        }
        #endregion

        #region MouseWheelUp
        public void MouseWheelUp()
        {
            SetVerticalOffset(VerticalOffset - SystemParameters.ScrollHeight * SystemParameters.WheelScrollLines);
        }
        #endregion

        #region MouseWheelDown
        public void MouseWheelDown()
        {
            SetVerticalOffset(VerticalOffset + SystemParameters.ScrollHeight * SystemParameters.WheelScrollLines);
        }
        #endregion

        #region MouseWheelLeft
        public void MouseWheelLeft()
        {
            SetHorizontalOffset(HorizontalOffset - SystemParameters.ScrollWidth * SystemParameters.WheelScrollLines);
        }
        #endregion

        #region MouseWheelRight
        public void MouseWheelRight()
        {
            SetHorizontalOffset(HorizontalOffset + SystemParameters.ScrollWidth * SystemParameters.WheelScrollLines);
        }
        #endregion

        #region MakeVisible
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            int idx = InternalChildren.IndexOf(visual as UIElement);

            if (ItemContainerGenerator is IItemContainerGenerator generator)
            {
                GeneratorPosition pos = new(idx, 0);
                idx = generator.IndexFromGeneratorPosition(pos);
            }

            if (idx < 0)
                return Rect.Empty;

            if (!containerLayouts.ContainsKey(idx))
                return Rect.Empty;

            Rect layout = containerLayouts[idx];

            if (HorizontalOffset + ViewportWidth < layout.X + layout.Width)
                SetHorizontalOffset(layout.X + layout.Width - ViewportWidth);
            if (layout.X < HorizontalOffset)
                SetHorizontalOffset(layout.X);

            if (VerticalOffset + ViewportHeight < layout.Y + layout.Height)
                SetVerticalOffset(layout.Y + layout.Height - ViewportHeight);
            if (layout.Y < VerticalOffset)
                SetVerticalOffset(layout.Y);

            layout.Width = Math.Min(ViewportWidth, layout.Width);
            layout.Height = Math.Min(ViewportHeight, layout.Height);

            return layout;
        }
        #endregion

        #region SetHorizontalOffset
        public void SetHorizontalOffset(double offset)
        {
            if (offset < 0 || ViewportWidth >= ExtentWidth)
            {
                offset = 0;
            }
            else
            {
                if (offset + ViewportWidth >= ExtentWidth)
                    offset = ExtentWidth - ViewportWidth;
            }

            this.offset.X = offset;

            ScrollOwner?.InvalidateScrollInfo();

            InvalidateMeasure();
        }
        #endregion

        #region SetVerticalOffset
        public void SetVerticalOffset(double offset)
        {
            if (offset < 0 || ViewportHeight >= ExtentHeight)
            {
                offset = 0;
            }
            else
            {
                if (offset + ViewportHeight >= ExtentHeight)
                    offset = ExtentHeight - ViewportHeight;
            }

            this.offset.Y = offset;

            ScrollOwner?.InvalidateScrollInfo();

            InvalidateMeasure();
        }
        #endregion

        #endregion
    }
}