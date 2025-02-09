using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;

using LiveToMoveUI.Core;

namespace LiveToMoveUI.Views;

public partial class MainWindow : Window
{
    private string _dropBoxLabelText;
    private List<string> _sourcePathList;
    
    public MainWindow()
    {
        InitializeComponent();

        _dropBoxLabelText = this.DropBoxBlock?.Text ?? string.Empty;

        this.ResultBlock.Opacity = 0;
        
        this.ProcessButton.IsEnabled = false;
        this.ProcessButton.Click += this.OnProcessClicked;
        
        this.AddHandler(DragDrop.DropEvent, this.OnDrop);
        this.AddHandler(DragDrop.DragOverEvent, this.OnDragOver);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects &= DragDropEffects.Copy;
    }

    private async void OnProcessClicked(object? sender, RoutedEventArgs e)
    {
        var processText = this.ProcessButton.Content;
        this.IsEnabled = false;

        var cts = new CancellationTokenSource();
        _ = this.AnimateButtonText(this.ProcessButton, "Processing", cts.Token);
        
        try
        {
            DrumRack.Process(_sourcePathList);
        }
        finally
        {
            await cts.CancelAsync();

            this.ProcessButton.Content = processText;
            this.IsEnabled = true;
            
            this.ResultBlock.Opacity = 1;
        }
    }
    
    private void OnDrop(object? sender, DragEventArgs e)
    {
        e.DragEffects &= DragDropEffects.Copy;

        if (e.Data.Contains(DataFormats.Files))
        {
            var file = e.Data.GetFiles()?.FirstOrDefault();
            if (file == null)
            {
                return;
            }

            var localPath = file.Path?.LocalPath;
            if (string.IsNullOrEmpty(localPath))
            {
                return;
            }

            if (File.Exists(localPath))
            {
                _sourcePathList = [localPath];
            }
            else if (Directory.Exists(localPath))
            {
                _sourcePathList = Directory.GetFiles(localPath, "*.adg")?.ToList();
                if (_sourcePathList == null || _sourcePathList.Count == 0)
                {
                    return;
                }
            }

            this.ProcessButton.IsEnabled = true;
            this.DropBoxBlock.Text = file.Name;
        }
    }
    
    private async Task AnimateButtonText(Button button, string text, CancellationToken token)
    {
        var baseText = text;
        string[] states = [".", "..", "..."];
        var index = 0;

        while (!token.IsCancellationRequested)
        {
            button.Content = baseText + states[index];
            index = (index + 1) % states.Length;
            
            await Task.Delay(250, token); 
        }
    }
}