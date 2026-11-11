using System.ComponentModel;

namespace Sample.Features.TreeView;

public class FileNode : INotifyPropertyChanged
{
    public required string Name { get; init; }
    public required string Icon { get; init; }
    public bool IsFolder { get; init; }
    public bool IsLocked { get; init; }
    public bool LazyLoad { get; init; }
    public List<FileNode>? Children { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public void Notify(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public static IReadOnlyList<FileNode> SampleData() => new[]
    {
        new FileNode
        {
            Name = "Documents", Icon = "📁", IsFolder = true,
            Children = new()
            {
                new FileNode
                {
                    Name = "Projects", Icon = "📁", IsFolder = true,
                    Children = new()
                    {
                        new FileNode { Name = "design.sketch", Icon = "🎨" },
                        new FileNode { Name = "spec.md", Icon = "📄" },
                        new FileNode
                        {
                            Name = "src", Icon = "📁", IsFolder = true,
                            Children = new()
                            {
                                new FileNode { Name = "Program.cs", Icon = "💾" },
                                new FileNode { Name = "App.xaml", Icon = "🖼️" }
                            }
                        }
                    }
                },
                new FileNode { Name = "readme.txt", Icon = "📄" },
                new FileNode { Name = "secret.key", Icon = "🔒", IsLocked = true }
            }
        },
        new FileNode
        {
            Name = "Downloads", Icon = "📁", IsFolder = true,
            Children = new()
            {
                new FileNode { Name = "installer.dmg", Icon = "💿" },
                new FileNode { Name = "photo.png", Icon = "🖼️" }
            }
        },
        new FileNode
        {
            Name = "Cloud (lazy)", Icon = "☁️", IsFolder = true, LazyLoad = true
        },
        new FileNode { Name = "todo.txt", Icon = "📄" }
    };
}
