﻿// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Media;
using System;
using System.Diagnostics;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Used within the template of a <see cref="T:Avalonia.Controls.DataGrid" /> to specify the 
    /// location in the control's visual tree where the rows are to be added.
    /// </summary>
    public sealed class DataGridRowsPresenter : Panel
    {
        internal DataGrid OwningGrid
        {
            get;
            set;
        }
        
        /// <summary>
        /// Arranges the content of the <see cref="T:Avalonia.Controls.Primitives.DataGridRowsPresenter" />.
        /// </summary>
        /// <returns>
        /// The actual size used by the <see cref="T:Avalonia.Controls.Primitives.DataGridRowsPresenter" />.
        /// </returns>
        /// <param name="finalSize">
        /// The final area within the parent that this element should use to arrange itself and its children.
        /// </param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (finalSize.Height == 0 || OwningGrid == null)
            {
                return base.ArrangeOverride(finalSize);
            }

            OwningGrid.OnFillerColumnWidthNeeded(finalSize.Width);

            double rowDesiredWidth = OwningGrid.ColumnsInternal.VisibleEdgedColumnsWidth + OwningGrid.ColumnsInternal.FillerColumn.FillerWidth;
            double topEdge = -OwningGrid.NegVerticalOffset;
            foreach (Control element in OwningGrid.DisplayData.GetScrollingElements())
            {
                if (element is DataGridRow row)
                {
                    Debug.Assert(row.Index != -1); // A displayed row should always have its index

                    // Visibility for all filler cells needs to be set in one place.  Setting it individually in
                    // each CellsPresenter causes an NxN layout cycle (see DevDiv Bugs 211557)
                    row.EnsureFillerVisibility();
                    row.Arrange(new Rect(-OwningGrid.HorizontalOffset, topEdge, rowDesiredWidth, element.DesiredSize.Height));
                }
                else if (element is DataGridRowGroupHeader groupHeader)
                {
                    double leftEdge = (OwningGrid.AreRowGroupHeadersFrozen) ? 0 : -OwningGrid.HorizontalOffset;
                    groupHeader.Arrange(new Rect(leftEdge, topEdge, rowDesiredWidth - leftEdge, element.DesiredSize.Height));
                }

                topEdge += element.DesiredSize.Height;
            }

            double finalHeight = Math.Max(topEdge + OwningGrid.NegVerticalOffset, finalSize.Height);

            // Clip the RowsPresenter so rows cannot overlap other elements in certain styling scenarios
            var rg = new RectangleGeometry
            {
                Rect = new Rect(0, 0, finalSize.Width, finalHeight)
            };
            Clip = rg;

            return new Size(finalSize.Width, finalHeight);
        }

        /// <summary>
        /// Measures the children of a <see cref="T:Avalonia.Controls.Primitives.DataGridRowsPresenter" /> to 
        /// prepare for arranging them during the <see cref="M:System.Windows.FrameworkElement.ArrangeOverride(System.Windows.Size)" /> pass.
        /// </summary>
        /// <param name="availableSize">
        /// The available size that this element can give to child elements. Indicates an upper limit that child elements should not exceed.
        /// </param>
        /// <returns>
        /// The size that the <see cref="T:Avalonia.Controls.Primitives.DataGridRowsPresenter" /> determines it needs during layout, based on its calculations of child object allocated sizes.
        /// </returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (availableSize.Height == 0 || OwningGrid == null)
            {
                return base.MeasureOverride(availableSize);
            }

            // If the Width of our RowsPresenter changed then we need to invalidate our rows
            bool invalidateRows = (!OwningGrid.RowsPresenterAvailableSize.HasValue || availableSize.Width != OwningGrid.RowsPresenterAvailableSize.Value.Width)
                                  && !double.IsInfinity(availableSize.Width);

            // The DataGrid uses the RowsPresenter available size in order to autogrow
            // and calculate the scrollbars
            OwningGrid.RowsPresenterAvailableSize = availableSize;

            OwningGrid.OnRowsMeasure();

            double totalHeight = -OwningGrid.NegVerticalOffset;
            double totalCellsWidth = OwningGrid.ColumnsInternal.VisibleEdgedColumnsWidth;

            double headerWidth = 0;
            foreach (Control element in OwningGrid.DisplayData.GetScrollingElements())
            {
                DataGridRow row = element as DataGridRow;
                if (row != null)
                {
                    if (invalidateRows)
                    {
                        row.InvalidateMeasure();
                    }
                }

                element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                if (row != null && row.HeaderCell != null)
                {
                    headerWidth = Math.Max(headerWidth, row.HeaderCell.DesiredSize.Width);
                }
                else if (element is DataGridRowGroupHeader groupHeader && groupHeader.HeaderCell != null)
                {
                    headerWidth = Math.Max(headerWidth, groupHeader.HeaderCell.DesiredSize.Width);
                }

                totalHeight += element.DesiredSize.Height;
            }

            OwningGrid.RowHeadersDesiredWidth = headerWidth;
            // Could be positive infinity depending on the DataGrid's bounds
            OwningGrid.AvailableSlotElementRoom = availableSize.Height - totalHeight;

            totalHeight = Math.Max(0, totalHeight);

            return new Size(totalCellsWidth + headerWidth, totalHeight);
        }

        // / <summary>
        // / Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        // / </summary>
        //Automation
        //protected override AutomationPeer OnCreateAutomationPeer()
        //{
        //    return new DataGridRowsPresenterAutomationPeer(this);
        //}
        
#if DEBUG
        internal void PrintChildren()
        {
            foreach (Control element in Children)
            {
                if (element is DataGridRow row)
                {
                    Debug.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Slot: {0} Row: {1} Visibility: {2} ", row.Slot, row.Index, row.IsVisible));
                }
                else if (element is DataGridRowGroupHeader groupHeader)
                {
                    Debug.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Slot: {0} GroupHeader: {1} Visibility: {2}", groupHeader.RowGroupInfo.Slot, groupHeader.RowGroupInfo.CollectionViewGroup.Key, groupHeader.IsVisible));
                }
            }
        }
#endif
    }
}