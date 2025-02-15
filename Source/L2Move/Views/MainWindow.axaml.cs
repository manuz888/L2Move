using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Interactivity;

using L2Move.Core;
using L2Move.Models;

namespace L2Move.Views;

public partial class MainWindow : Window
{
    #region Constants

    private const string DRUM_RACK_LIVE_EXTENSION = ".adg";
    private const string TARGET_DIRECTORY = "Processed";
    private const string REPORT_FILE_NAME = "report.txt";

    private const string RESULT_STRING = "Result";
    private static readonly string RESULT_OK_STRING = $"{RESULT_STRING}: Ok";
    private static readonly string RESULT_WARNING_STRING = $"{RESULT_STRING}: Ok but see report";
    private static readonly string RESULT_ERROR_STRING = $"{RESULT_STRING}: Error (see report)";

    private const string PROCESSING_STRING = "Processing";

    private const string PREFIX_FOR_SOURCE = "> ";
    private const string DIRECTORY_STRING = "Directory";
    private const string FILE_STRING = "File";

    #endregion
    
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
        
        var prefix = PREFIX_FOR_SOURCE;
        if (File.Exists(path))
        {
            prefix += FILE_STRING;
            
            _sourcePathList = [path];
        }
        else if (Directory.Exists(path))
        {
            prefix += DIRECTORY_STRING;
            
            Helpers.GetFilesFromPathByExtension(DRUM_RACK_LIVE_EXTENSION, path, out _sourcePathList);
        }
        else
        {
            // Invalid path
            return;
        }
        
        this.ResultBlock.Opacity = 0;
        this.ProcessButton.IsEnabled = true;
        
        this.DropBoxBlock.Text = $"{prefix}:\n{Path.GetFileName(path)}";
    }

    private async void OnProcessClicked(object? sender, RoutedEventArgs e)
    {
        // Recheck for safe reason
        if (_sourcePathList == null || _sourcePathList.Count <= 0)
        {
            this.ManageResult(isError: true);
            
            return;
        }
        
        var sourceDirectory = Path.GetDirectoryName(_sourcePathList[0]);
        if (string.IsNullOrEmpty(sourceDirectory) || !Directory.Exists(sourceDirectory))
        {
            this.ManageResult(isError: true);
            
            return;
        }
        
        this.IsEnabled = false;
        
        var animatedCts = new CancellationTokenSource();

        var processButtonLabel = this.ProcessButton.Content;
        _ = this.AnimateButtonText(this.ProcessButton, PROCESSING_STRING, animatedCts.Token);
        
        var targetPath = Path.Combine(sourceDirectory, TARGET_DIRECTORY);
        var processingResultList = await Task.Run(() => DrumRackProcessor.Process(_sourcePathList, targetPath));

        // If the source are multiple, so the report will be generated
        if (_sourcePathList.Count > 1)
        {
            ReportGenerator.Generate(processingResultList, Path.Combine(targetPath, REPORT_FILE_NAME));
        }

        var processingOkList = processingResultList.Where(_ => _.Value == ProcessingResult.ValueEnum.Ok);
        if (processingOkList.Count() == _sourcePathList.Count)
        {
            this.ManageResult(isOk: true);
        }
        else if (!processingOkList.Any())
        {
            this.ManageResult(isError: true);
        }
        else
        {
            this.ManageResult(isWarning: true);
        }

        if (this.PresetBundleCheckbox.IsChecked ?? false)
        {
            foreach (var processingOk in processingOkList)
            {
                var presetName = Path.GetFileNameWithoutExtension(processingOk.FileName);
                var samplePathList = processingOk.SamplePathList;
                
                // TODO: feedback error to user
                await Task.Run(() => MovePresetManager.GenerateDrumRack(presetName, samplePathList, targetPath));
            }
        }
        
        await animatedCts.CancelAsync();
        this.ProcessButton.Content = processButtonLabel;
        
        this.IsEnabled = true;
        this.ResultBlock.Opacity = 1;
    }

    private void ManageResult(bool isOk = false, bool isWarning = false, bool isError = false)
    {
        if (isOk)
        {
            this.ResultBlockLabel.Text = RESULT_OK_STRING;
            this.ResultBlock.Background = Brushes.LightGreen;
        }
        else if (isWarning)
        {
            this.ResultBlockLabel.Text = RESULT_WARNING_STRING;
            this.ResultBlock.Background = Brushes.LightYellow;
        }
        else
        {
            this.ResultBlockLabel.Text = RESULT_ERROR_STRING;
            this.ResultBlock.Background = Brushes.LightCoral;
        }
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