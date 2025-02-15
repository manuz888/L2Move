using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Interactivity;

using LiveToMoveUI.Core;
using LiveToMoveUI.Core.Json;
using LiveToMoveUI.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LiveToMoveUI.Views;

public partial class MainWindow : Window
{
    private const string DRUM_RACK_LIVE_EXTENSION = ".adg";
    private const string TARGET_DIRECTORY = "Processed";
    private const string REPORT_FILE_NAME = "report.txt";
    
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
            this.HandlePathToProcess(stringPath[0], checkExtension: true);
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

        if (Helpers.ContainsFilesWithExtension(localPath, DRUM_RACK_LIVE_EXTENSION))
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
        
        this.HandlePathToProcess(path);
    }
    
    private void HandlePathToProcess(string path, bool checkExtension = false)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        if (checkExtension && !Helpers.ContainsFilesWithExtension(path, DRUM_RACK_LIVE_EXTENSION))
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
            
            Helpers.GetFilesFromPathByExtension(DRUM_RACK_LIVE_EXTENSION, path, out _sourcePathList);
        }
        else
        {
            // Invalid path
            return;
        }
        
        this.ResultBlock.Opacity = 0;
        this.ProcessButton.IsEnabled = true;
        
        this.DropBoxBlock.Text = $"{prefix}\n{Path.GetFileName(path)}";
    }

    private async void OnProcessClicked(object? sender, RoutedEventArgs e)
    {
        // Recheck for safe reason
        if (_sourcePathList == null || _sourcePathList.Count <= 0)
        {
            // TODO: add a message error?
            
            return;
        }
        
        var processButtonLabel = this.ProcessButton.Content;
        var animatedCts = new CancellationTokenSource();
        _ = this.AnimateButtonText(this.ProcessButton, "Processing", animatedCts.Token);
        
        var targetPath = Path.Combine(Path.GetDirectoryName(_sourcePathList[0]), TARGET_DIRECTORY);
        var result = DrumRackProcessor.Process(_sourcePathList, targetPath);

        // If the source are multiple, so the report will be generated
        if (_sourcePathList.Count > 1)
        {
            ReportGenerator.Generate(result, Path.Combine(targetPath, REPORT_FILE_NAME));
        }

        var preset = MovePresetGenerator.GenerateDrumRack(result[0].FileName, result[0].SamplePathList);
        
        // !!! Only for debug purpose !!!
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new CustomContractResolver(),
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };
        var t = JsonConvert.SerializeObject(preset, settings);
        // !!! Only for debug purpose !!!
        
        if (this.PresetBundleCheckbox.IsChecked ?? false)
        {
            // TODO: ...
        }
        
        var successCount = result.Count(_ => _.Value == ProcessingResult.ValueEnum.Ok);
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