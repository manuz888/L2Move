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
        // TODO: doesn't work on macos
        if ((e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta)) && 
             e.Key == Key.V)
        {
            if (this.Clipboard != null)
            {
                var text = await this.Clipboard.GetTextAsync();
            }
        }
    }
    
    private void OnDragOver(object? sender, DragEventArgs eventArgs)
    {
        eventArgs.Handled = true;
        
        // To prevent the drag
        eventArgs.DragEffects = DragDropEffects.None;

        if (!Helpers.GetLocalPathFromDragEvent(eventArgs, out var localPath))
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
        if (!Helpers.GetLocalPathFromDragEvent(eventArgs, out var localPath))
        {
            return;
        }

        var prefix = string.Empty;
        if (File.Exists(localPath))
        {
            prefix = "File:";
            
            _sourcePathList = [localPath];
        }
        else if (Directory.Exists(localPath))
        {
            prefix = "Directory:";
            
            Helpers.GetFilesFromPathByExtension(".adg", localPath, out _sourcePathList);
        }
        else
        {
            // TODO: manage error
        }

        this.ProcessButton.IsEnabled = true;
        this.DropBoxBlock.Text = $"{prefix} {Path.GetFileName(localPath)}";
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