﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2011 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using pwiz.Common.DataBinding.Clustering;
using pwiz.Common.DataBinding.Internal;
using pwiz.Common.DataBinding.Layout;

namespace pwiz.Common.DataBinding.Controls
{
    public class BindingListSource : BindingSource
    {
        public BindingListSource(IContainer container) : this(new EventTaskScheduler())
        {
            container.Add(this);
        }
        public BindingListSource() : this((EventTaskScheduler) null)
        {
        }

        public BindingListSource(CancellationToken cancellationToken) : this()
        {
            QueryLock = new QueryLock(cancellationToken);
        }

        private BindingListSource(EventTaskScheduler taskScheduler)
        {
            base.DataSource = BindingListView = new BindingListView(taskScheduler);
            BindingListView.UnhandledExceptionEvent += BindingListViewOnUnhandledException;
            BindingListView.AllRowsChanged += BindingListViewOnAllRowsChanged;
            
            ColumnFormats = new ColumnFormats();
        }

        private void BindingListViewOnAllRowsChanged(object sender, EventArgs eventArgs)
        {
            var handler = AllRowsChanged;
            if (null != handler)
            {
                handler(this, eventArgs);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                BindingListView.Dispose();
            }
            base.Dispose(disposing);
        }

        internal BindingListView BindingListView { get; private set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IRowSource RowSource
        {
            get { return BindingListView.RowSource; }
            set { SetView(ViewInfo, value); }
        }

        [Browsable(false)]
        public new object DataSource
        {
            get { return base.DataSource; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public QueryLock QueryLock 
        { 
            get { return BindingListView.QueryLock; } 
            set { BindingListView.QueryLock = value; }
        }

        public IViewContext ViewContext { get; private set; }
        public void SetViewContext(IViewContext viewContext, ViewInfo viewInfo)
        {
            ViewLayout viewLayout = null;
            if (null != viewInfo && viewContext != null && viewInfo.ViewGroup != null)
            {
                var viewLayoutList = viewContext.GetViewLayoutList(viewInfo.ViewGroup.Id.ViewName(viewInfo.Name));
                if (null != viewLayoutList)
                {
                    viewLayout = viewLayoutList.FindLayout(viewLayoutList.DefaultLayoutName);
                }
            }
            SetViewContext(viewContext, viewInfo, viewLayout);
        }

        public void SetViewContext(IViewContext viewContext, ViewInfo viewInfo, ViewLayout viewLayout)
        {
            ViewContext = viewContext;
            if (null == viewInfo)
            {
                BindingListView.ViewInfo = null;
            }
            else
            {
                IRowSource rowSource = null;
                bool viewChanged = true;
                if (null != ViewInfo)
                {
                    if (ViewInfo.RowSourceName == viewInfo.RowSourceName)
                    {
                        rowSource = RowSource;
                        if (ViewInfo.Name == viewInfo.Name)
                        {
                            viewChanged = false;
                        }
                    }
                }
                rowSource = rowSource ?? viewContext.GetRowSource(viewInfo);
                BindingListView.SetViewAndRows(viewInfo, rowSource);
                if (viewChanged)
                {
                    BindingListView.ClearTransformStack();
                }

                if (viewLayout != null)
                {
                    ApplyLayout(viewLayout);
                }
            }
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        public void SetViewContext(IViewContext viewContext, ViewGroup viewGroup, ViewSpecLayout viewSpecLayout)
        {
            var viewInfo = viewContext.GetViewInfo(viewGroup, viewSpecLayout.ViewSpec);
            SetViewContext(viewContext, viewInfo, viewSpecLayout.DefaultViewLayout);
        }

        public void SetView(ViewInfo viewInfo, IRowSource rows)
        {
            BindingListView.SetViewAndRows(viewInfo, rows);
        }
        public void SetViewContext(IViewContext viewContext)
        {
            ViewInfo viewInfo = BindingListView.ViewInfo;
            if (viewInfo == null && viewContext != null)
            {
                viewInfo = viewContext.GetViewInfo(
                    ViewGroup.BUILT_IN,
                    viewContext.GetViewSpecList(ViewGroup.BUILT_IN.Id).ViewSpecs.FirstOrDefault());
            }
            SetViewContext(viewContext, viewInfo);
        }
        public ViewInfo ViewInfo
        {
            get { return BindingListView.ViewInfo; }
        }
        public ViewSpec ViewSpec
        {
            get { return ViewInfo.ViewSpec; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RowFilter RowFilter
        {
            get { return BindingListView.RowFilter; }
            set { BindingListView.RowFilter = value; }
        }

        private void BindingListViewOnUnhandledException(object sender, BindingManagerDataErrorEventArgs args)
        {
            OnDataError(args);
        }

        public bool IsComplete
        {
            get
            {
                return !BindingListView.IsRequerying;
            }
        }

        /// <summary>
        /// Event which fires when all rows of data have potentially changed.
        /// We try to avoid firing a ListChangeType.Reset event because that resets the
        /// current cell in the DataGridView to the beginning of the row.
        /// </summary>
        public event EventHandler AllRowsChanged;

        public ColumnFormats ColumnFormats
        {
            get; private set;
        }

        public ItemProperties ItemProperties { get { return BindingListView.ItemProperties; } }

        public DataPropertyDescriptor FindDataProperty(string dataPropertyName)
        {
            int index = BindingListView.ItemProperties.IndexOfName(dataPropertyName);
            if (index < 0)
            {
                return null;
            }

            return ItemProperties[index];
        }

        public ReportResults ReportResults
        {
            get { return BindingListView.ReportResults; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ClusteringSpec ClusteringSpec
        {
            get { return BindingListView.ClusteringSpec; }
            set
            {
                BindingListView.ClusteringSpec = value;
            }
        }

        public void ApplyLayout(ViewLayout viewLayout)
        {
            BindingListView.TransformStack = new TransformStack(viewLayout.RowTransforms, 0);
            foreach (var format in viewLayout.ColumnFormats)
            {
                ColumnFormats.SetFormat(format.Item1, format.Item2);
            }
            ClusteringSpec = viewLayout.ClusterSpec;
        }

        public INewRowHandler NewRowHandler
        {
            get { return BindingListView.NewRowHandler; }
            set { BindingListView.NewRowHandler = value; }
        }

        protected override void OnListChanged(ListChangedEventArgs e)
        {
            base.OnListChanged(e);
            if (BindingListView != null && CurrencyManager != null)
            {
                var newRowPos = BindingListView.NewRowPos;
                if (newRowPos.HasValue)
                {
                    CurrencyManager.Position = newRowPos.Value;
                }
            }
        }

        public bool ValidateRow(int rowIndex, out bool cancelRowEdit)
        {
            bool result= BindingListView.ValidateRow(rowIndex, out cancelRowEdit);
            if (cancelRowEdit)
            {
                ((ICancelAddNew)this).CancelNew(rowIndex);
            }
            return result;
        }

        public IEnumerable<ColumnDescriptor> FindColumnDescriptorsWithType<T>()
        {
            var propertyPaths = new HashSet<PropertyPath>();
            foreach (var dataPropertyDescriptor in ItemProperties)
            {
                var columnPropertyDescriptor = dataPropertyDescriptor as ColumnPropertyDescriptor;
                if (columnPropertyDescriptor == null)
                {
                    continue;
                }

                var columnDescriptor = columnPropertyDescriptor.DisplayColumn.ColumnDescriptor;
                while (columnDescriptor != null)
                {
                    if (!propertyPaths.Add(columnDescriptor.PropertyPath))
                    {
                        break;
                    }

                    if (typeof(T).IsAssignableFrom(columnDescriptor.PropertyType))
                    {
                        yield return columnDescriptor;
                    }

                    columnDescriptor = columnDescriptor.Parent;
                }
            }
        }
    }
}
