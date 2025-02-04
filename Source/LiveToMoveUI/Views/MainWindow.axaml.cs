using System.Linq;

using Avalonia.Input;
using Avalonia.Controls;

namespace LiveToMoveUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects &= DragDropEffects.Copy;
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        e.DragEffects &= DragDropEffects.Copy;

        if (e.Data.Contains(DataFormats.Files))
        {
            // DropState.Text = e.Data.GetFiles()?.FirstOrDefault()?.Path.ToString();
        }
    }
}