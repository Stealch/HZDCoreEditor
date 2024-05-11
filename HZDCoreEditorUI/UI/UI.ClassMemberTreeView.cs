namespace HZDCoreEditorUI.UI;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using BrightIdeasSoftware;
using HZDCoreEditorUI.Util;

/// <summary>
/// A TreeListView that displays the members of a class.
/// </summary>
public class ClassMemberTreeView : TreeListView
{
    // List to hold child nodes
    private readonly List<TreeDataNode> _children = new List<TreeDataNode>();

    // Array to hold default columns
    private readonly OLVColumn[] _defaultColumns;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassMemberTreeView"/> class.
    /// </summary>
    public ClassMemberTreeView()
    {
        // Set handlers for expanding and getting children
        CanExpandGetter = CanExpandGetterHandler;
        ChildrenGetter = ChildrenGetterHandler;

        // Set handlers for cell editing and right-clicking
        CellEditStarting += CellEditStartingHandler;
        CellEditFinishing += CellEditFinishingHandler;
        CellEditFinished += CellEditFinishedHandler;
        CellRightClick += CellRightClickHandler;

        // Set handler for sorting
        BeforeSorting += BeforeSortingHandler;

        // Initialize default columns
        _defaultColumns = new OLVColumn[4];

        // Initialize name column
        _defaultColumns[0] = new OLVColumn("Name", nameof(TreeDataNode.Name))
        {
            Width = 300,
            IsEditable = false,
        };

        // Initialize value column
        _defaultColumns[1] = new OLVColumn("Value", nameof(TreeDataNode.Value))
        {
            Width = 500,
        };

        // Initialize category column
        _defaultColumns[2] = new OLVColumn("Category", nameof(TreeDataNode.Category))
        {
            Width = 100,
            IsEditable = false,
        };

        // Initialize type column
        _defaultColumns[3] = new OLVColumn("Type", nameof(TreeDataNode.TypeName))
        {
            Width = 200,
            IsEditable = false,
        };

        // Create columns
        CreateColumns();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassMemberTreeView"/> class with a base object.
    /// </summary>
    /// <param name="baseObject">The base object to build the tree from.</param>
    public ClassMemberTreeView(object baseObject)
        : this()
    {
        // Rebuild tree from base object
        RebuildTreeFromObject(baseObject);
    }

    /// <summary>
    /// Rebuilds the tree from the provided object.
    /// </summary>
    /// <param name="baseObject">The object to build the tree from.</param>
    public void RebuildTreeFromObject(object baseObject)
    {
        // Clear the existing children
        _children.Clear();

        // Prepare each root node: class member variables act as children
        var objectType = baseObject.GetType();

        // If the object is not of type object, return
        if (Type.GetTypeCode(objectType) != TypeCode.Object)
            return;

        // Get all the fields of the object, including public and non-public instance fields
        var fields = objectType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // For each field, create a new TreeDataNode and add it to the children list
        foreach (var field in fields)
            _children.Add(TreeDataNode.CreateNode(baseObject, new FieldOrProperty(field)));

        // Rebuild the view roots
        RebuildViewRoots(true);
    }

    /// <summary>
    /// Rebuilds the view roots.
    /// </summary>
    /// <param name="forceResort">A boolean indicating whether the objects in the TreeListView should be sorted. If true, the objects are sorted based on the primary sort column and order.</param>
    private void RebuildViewRoots(bool forceResort)
    {
        // A full reset needs to be performed due to bugs in OLV
        var oldState = SaveState();
        Reset();
        CreateColumns();
        RestoreState(oldState);

        if (forceResort)
            SortObjects(PrimarySortColumn.AspectName, PrimarySortOrder);

        Roots = _children;
    }

    /// <summary>
    /// Creates the columns.
    /// </summary>
    private void CreateColumns()
    {
        AllColumns.AddRange(_defaultColumns);
        PrimarySortColumn = _defaultColumns[2];
        PrimarySortOrder = SortOrder.Descending;

        RebuildColumns();
    }

    /// <summary>
    /// Sorts the objects.
    /// </summary>
    /// <param name="aspectName">The name of the aspect to sort by.</param>
    /// <param name="order">The order parameter of type OrderType.</param>
    /// <returns>The return value of type ResultType.</returns>
    private bool SortObjects(string aspectName, SortOrder order)
    {
        Func<TreeDataNode, TreeDataNode, int> compareFunc = null;
        static int CompareNames(TreeDataNode x, TreeDataNode y) => string.Compare(x.Name, y.Name);

        switch (aspectName)
        {
            case nameof(TreeDataNode.Name):
                compareFunc = CompareNames;
                break;

            case nameof(TreeDataNode.Value):
                return false;

            case nameof(TreeDataNode.Category):
                compareFunc = (x, y) =>
                {
                    if (string.IsNullOrEmpty(x.Category))
                        return 1;

                    if (string.IsNullOrEmpty(y.Category))
                        return -1;

                    return string.Compare(x.Category, y.Category);
                };
                break;

            case nameof(TreeDataNode.TypeName):
                compareFunc = (x, y) =>
                {
                    return string.Compare(x.TypeName, y.TypeName);
                };
                break;

            default:
                return false;
        }

        // Only sort the top level objects. The rest don't matter.
        _children.Sort((x, y) =>
        {
            int result = compareFunc(x, y);

                // Reverse the order if ascending
            if (order == SortOrder.Ascending)
            {
                result = result switch
                {
                    -1 => 1,
                    0 => 0,
                    1 => -1,
                    _ => result,
                };
            }

            // Use the member name as a fallback so we have a stable sort order
            if (result == 0)
                result = CompareNames(x, y);

            return result;
        });

        return true;
    }

    /// <summary>
    /// Determines if the object can be expanded.
    /// </summary>
    /// <param name="model">An object of type TreeDataNode.</param>
    /// <returns>True if the object has children, false otherwise.</returns>
    private bool CanExpandGetterHandler(object model)
    {
        return (model as TreeDataNode).HasChildren;
    }

    /// <summary>
    /// Gets the children.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    private IEnumerable<TreeDataNode> ChildrenGetterHandler(object model)
    {
        return (model as TreeDataNode).Children;
    }

    /// <summary>
    /// Handles the CellEditStarting event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CellEditStartingHandler(object sender, CellEditEventArgs e)
    {
        var node = e.RowObject as TreeDataNode;

        if (!node.IsEditable)
        {
            e.Cancel = true;
            return;
        }

        var control = node.CreateEditControl(e.CellBounds);

        if (control != null)
        {
            // Don't auto dispose: the control will be destroyed before CellEditFinishedHandler runs
            e.AutoDispose = false;
            e.Control = control;
        }
    }

    /// <summary>
    /// Handles the CellEditFinishing event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CellEditFinishingHandler(object sender, CellEditEventArgs e)
    {
        // This has to be canceled or CellEditFinishedHandler will fire twice for each edit
        e.Cancel = true;
    }

    /// <summary>
    /// Handles the CellEditFinished event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CellEditFinishedHandler(object sender, CellEditEventArgs e)
    {
        var node = e.RowObject as TreeDataNode;

        if (node.FinishEditControl(e.Control, () => { RefreshObjects(_children); }))
            RefreshItem(e.ListViewItem);
    }

    /// <summary>
    /// Handles the CellRightClick event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CellRightClickHandler(object sender, CellRightClickEventArgs e)
    {
        var contextMenu = new ContextMenuStrip();
        contextMenu.SuspendLayout();

        if (e.Model is TreeDataNode node)
            node.CreateContextMenuItems(contextMenu, () => { RefreshObjects(_children); });

        if (contextMenu.Items.Count > 0)
            contextMenu.Items.Add(new ToolStripSeparator());

        var expandAllItem = new ToolStripMenuItem();
        expandAllItem.Text = "Expand All Rows";
        expandAllItem.Click += (o, e) => ExpandAll();
        contextMenu.Items.Add(expandAllItem);

        var collapseAllItem = new ToolStripMenuItem();
        collapseAllItem.Text = "Collapse All Rows";
        collapseAllItem.Click += (o, e) => CollapseAll();
        contextMenu.Items.Add(collapseAllItem);

        contextMenu.ResumeLayout();
        e.MenuStrip = contextMenu;
    }

    /// <summary>
    /// Handles the BeforeSorting event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BeforeSortingHandler(object sender, BeforeSortingEventArgs e)
    {
        var treeView = sender as ClassMemberTreeView;
        bool sortResult = treeView.SortObjects(e.ColumnToSort.AspectName, e.SortOrder);

        if (!sortResult)
            e.Canceled = true;

        if (!e.Canceled)
            treeView.RebuildViewRoots(false);

        e.Handled = true;
    }
}
