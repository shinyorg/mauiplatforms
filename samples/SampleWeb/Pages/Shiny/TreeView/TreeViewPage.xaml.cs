using Shiny;
using Shiny.Maui.Controls.Tree;

namespace Sample.Features.TreeView;

public partial class TreeViewPage : ContentPage
{
    readonly List<FileNode> data;

    public TreeViewPage()
    {
        InitializeComponent();
        data = FileNode.SampleData().ToList();

        Tree.ItemsSource = data;
        Tree.ChildrenSelector = item => (item is FileNode { LazyLoad: false } f) ? f.Children : null;
        Tree.HasChildrenSelector = item => item is FileNode f && f.IsFolder;
        Tree.CanSelectSelector = item => item is FileNode f && !f.IsLocked;
        Tree.CanExpandSelector = item => item is FileNode f && f.IsFolder;
        Tree.ChildrenLoader = LoadCloudChildrenAsync;
    }

    async Task<IEnumerable<object>> LoadCloudChildrenAsync(object parent)
    {
        // Only the "Cloud" branch is lazy. Other folders use the sync selector.
        if (parent is FileNode { LazyLoad: true } folder)
        {
            await Task.Delay(900); // simulate network

            // "shared" is a second lazy level so the demo shows nested loading, but it
            // returns leaves — an endlessly generated branch would never finish expanding.
            if (folder.Name == "shared")
            {
                return new object[]
                {
                    new FileNode { Name = "team-notes.md", Icon = "📄" },
                    new FileNode { Name = "budget.xlsx", Icon = "📊" }
                };
            }

            return new object[]
            {
                new FileNode { Name = "remote-backup.zip", Icon = "💾" },
                new FileNode { Name = "shared", Icon = "📁", IsFolder = true, LazyLoad = true },
                new FileNode { Name = "presentation.pptx", Icon = "📊" }
            };
        }
        // Fallback for any unexpected branch
        return Array.Empty<object>();
    }

    async void OnExpandAllClicked(object? sender, EventArgs e) => await Tree.ExpandAllAsync();

    void OnCollapseAllClicked(object? sender, EventArgs e) => Tree.CollapseAll();

    void OnRefreshCloudClicked(object? sender, EventArgs e)
    {
        var cloud = data.FirstOrDefault(d => d.LazyLoad);
        if (cloud != null)
            Tree.Refresh(cloud);
    }

    void OnMultiSelectToggled(object? sender, ToggledEventArgs e)
    {
        Tree.SelectionMode = e.Value ? TreeSelectionMode.Multiple : TreeSelectionMode.Single;
        SelectAllButton.IsVisible = e.Value;
        DeselectAllButton.IsVisible = e.Value;
        StatusLabel.Text = $"Selection mode: {Tree.SelectionMode}";
    }

    void OnSelectAllClicked(object? sender, EventArgs e)
    {
        Tree.SelectAll();
        StatusLabel.Text = $"{Tree.SelectedItems?.Count ?? 0} checked";
    }

    void OnDeselectAllClicked(object? sender, EventArgs e)
    {
        Tree.DeselectAll();
        StatusLabel.Text = "Selection cleared";
    }

    void OnItemSelected(object? sender, TreeItemEventArgs e)
    {
        if (e.Item is not FileNode f)
            return;

        StatusLabel.Text = Tree.SelectionMode == TreeSelectionMode.Multiple
            ? $"{Tree.SelectedItems?.Count ?? 0} checked (last: {f.Name})"
            : $"Selected: {f.Name}";
    }

    void OnItemExpanded(object? sender, TreeItemEventArgs e)
    {
        if (e.Item is FileNode f)
            StatusLabel.Text = $"Expanded: {f.Name}";
    }

    void OnItemCollapsed(object? sender, TreeItemEventArgs e)
    {
        if (e.Item is FileNode f)
            StatusLabel.Text = $"Collapsed: {f.Name}";
    }

    void OnLoadFailed(object? sender, TreeLoadFailedEventArgs e)
    {
        StatusLabel.Text = $"Load failed: {e.Exception.Message}";
    }

    void OnItemDropped(object? sender, TreeItemDroppedEventArgs e)
    {
        if (e.SourceItem is not FileNode src || e.TargetItem is not FileNode tgt)
            return;

        // Pop the source from its current parent collection
        var sourceList = FindParentList(src);
        if (sourceList == null)
            return;

        if (e.Position == TreeDropPosition.Into)
        {
            sourceList.Remove(src);
            tgt.Children ??= new();
            tgt.Children.Add(src);
            StatusLabel.Text = $"Moved {src.Name} into {tgt.Name}";
        }
        else
        {
            var targetList = FindParentList(tgt);
            if (targetList == null)
                return;

            sourceList.Remove(src);
            var targetIndex = targetList.IndexOf(tgt);
            var before = e.Position == TreeDropPosition.Above;
            targetList.Insert(before ? targetIndex : targetIndex + 1, src);
            StatusLabel.Text = $"Moved {src.Name} {(before ? "before" : "after")} {tgt.Name}";
        }

        // Re-bind so the tree re-flattens with the new order
        Tree.ItemsSource = null;
        Tree.ItemsSource = data;
    }

    List<FileNode>? FindParentList(FileNode item)
    {
        if (data.Contains(item))
            return data;
        return FindParentListIn(data, item);
    }

    static List<FileNode>? FindParentListIn(List<FileNode> nodes, FileNode item)
    {
        foreach (var n in nodes)
        {
            if (n.Children == null)
                continue;
            if (n.Children.Contains(item))
                return n.Children;
            var nested = FindParentListIn(n.Children, item);
            if (nested != null)
                return nested;
        }
        return null;
    }
}
