using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

using LiveToMoveUI.Core;

namespace LiveToMoveUI.Views;

public partial class MainWindow : Window
{
    private List<string> _sourcePathList;
    
    public MainWindow()
    {
        InitializeComponent();

        this.ResultBlock.Opacity = 0;
        
        this.ProcessButton.IsEnabled = false;
        this.ProcessButton.Click += this.OnProcessClicked;
    
        this.AddHandler(KeyDownEvent, this.OnKeyDown);
        
        this.AddHandler(DragDrop.DragOverEvent, this.OnDragOver);
        this.AddHandler(DragDrop.DropEvent, this.OnDrop);
    }
    
    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (this.Clipboard == null)
        {
            return;
        }
        
        var isPasteCommand = (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta)) && 
                              e.Key == Key.V;
        if (!isPasteCommand)
        {
            return;
        }

        var path = await this.Clipboard.GetDataAsync(DataFormats.FileNames);

        if (path is string[] { Length: > 0 } stringPath)
        {
            this.HandlePath(stringPath[0]);
        }
    }
    
    private void OnDragOver(object? sender, DragEventArgs eventArgs)
    {
        eventArgs.Handled = true;
        
        // To prevent the drag
        eventArgs.DragEffects = DragDropEffects.None;

        if (!Helpers.GetPathFromDragEvent(eventArgs, out var localPath))
        {
            return;
        }
        
        var extension = Path.GetExtension(localPath);
        if (extension == ".adg" || (Directory.Exists(localPath) && Helpers.GetFilesFromPathByExtension(".adg", localPath, out _)))
        {
            eventArgs.DragEffects = DragDropEffects.Copy;
        }
    }
    
    private void OnDrop(object? sender, DragEventArgs eventArgs)
    {
        if (!Helpers.GetPathFromDragEvent(eventArgs, out var path))
        {
            return;
        }

        this.HandlePath(path);
    }

    private async void OnProcessClicked(object? sender, RoutedEventArgs e)
    {
        var processButtonLabel = this.ProcessButton.Content;
        var animatedCts = new CancellationTokenSource();
        _ = this.AnimateButtonText(this.ProcessButton, "Processing", animatedCts.Token);
        
        var result = DrumRackProcessor.Process(_sourcePathList);
        
        var successCount = 0;
        foreach (var sourcePath in _sourcePathList)
        {
            successCount += result[sourcePath] == DrumRackProcessor.ProcessingResult.Ok ? 1 : 0;
        }

        if (successCount == _sourcePathList.Count)
        {
            this.ResultBlockLabel.Text = "Result: OK";
            this.ResultBlock.Background = Brushes.LightGreen;
        }
        else if (successCount == 0)
        {
            this.ResultBlockLabel.Text = "Result: Error (see report)";
            this.ResultBlock.Background = Brushes.LightCoral;
        }
        else
        {
            this.ResultBlockLabel.Text = "Result: OK but see report";
            this.ResultBlock.Background = Brushes.LightYellow;
        }
        
        await animatedCts.CancelAsync();
        this.ProcessButton.Content = processButtonLabel;
        
        this.IsEnabled = true;
        this.ResultBlock.Opacity = 1;
    }

    private void HandlePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        
        var prefix = ">";
        if (File.Exists(path))
        {
            prefix += "File:";
            
            _sourcePathList = [path];
        }
        else if (Directory.Exists(path))
        {
            prefix += "Directory:";
            
            Helpers.GetFilesFromPathByExtension(".adg", path, out _sourcePathList);
        }
        else
        {
            // Invalid path
            return;
        }
        
        this.ProcessButton.IsEnabled = true;
        this.DropBoxBlock.Text = $"{prefix}\n{Path.GetFileName(path)}";
    }
    
    private async Task AnimateButtonText(Button button, string text, CancellationToken token)
    {
        string[] animatedStates = [".", "..", "..."];
        
        var index = 0;
        while (!token.IsCancellationRequested)
        {
            button.Content = text + animatedStates[index];
            index = (index + 1) % animatedStates.Length;
            
            await Task.Delay(250, token); 
        }
    }
}