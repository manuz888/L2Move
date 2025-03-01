using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.Documents;

using L2Move.Core;
using L2Move.Models;
using L2Move.Helpers;

namespace L2Move.Views;

public partial class MainWindow : Window
{
    #region Constants

    private static readonly string TARGET_PATH = FileHelper.GetDocumentsPath();
    private const string PROCESSED_DIRECTORY = "Processed";
    private const string TARGET_ADG_DIRECTORY = "Adg";
    private const string TARGET_PRESET_DIRECTORY = "Presets";

    private const string DRUM_RACK_LIVE_EXTENSION = ".adg";
    private const string REPORT_FILE_NAME = "report.txt";
    
    private const string RESULT_BLOCK_STRING = "> Result";
    private const string RESULT_BLOCK_OK_STRING = $"{RESULT_BLOCK_STRING}: Ok";
    private const string RESULT_BLOCK_WARNING_STRING = $"{RESULT_BLOCK_STRING}: Ok but see report";
    private const string RESULT_BLOCK_ERROR_STRING = $"{RESULT_BLOCK_STRING}: Error";
    private static readonly List<Inline> RESULT_BLOCK_FOOTER_RUN =
    [
        new LineBreak(),
        new Run() { Text = "(Click to open output folder)", FontSize = 10 }
    ];

    private const string PROCESSING_STRING = "Processing";
    private const string PREFIX_FOR_SOURCE_STRING = "> ";

    #endregion
    
    private List<string> _sourcePathList;
    private string _targetPath;
    
    public MainWindow()
    {
        InitializeComponent();
        
        this.ResultBlock.Opacity = 0;
        this.ResultBlock.PointerPressed += (_, _) => OSHelper.OpenFolderInFinder(_targetPath);
        
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

        if (!FileHelper.GetPathFromDragEvent(eventArgs, out var localPath))
        {
            return;
        }

        if (FileHelper.ContainsFilesWithExtension(localPath, DRUM_RACK_LIVE_EXTENSION))
        {
            eventArgs.DragEffects = DragDropEffects.Copy;
        }
    }
    
    private void OnDrop(object? sender, DragEventArgs eventArgs)
    {
        if (!FileHelper.GetPathFromDragEvent(eventArgs, out var path))
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

        if (checkExtension && !FileHelper.ContainsFilesWithExtension(path, DRUM_RACK_LIVE_EXTENSION))
        {
            return;
        }
        
        var boxLabel = PREFIX_FOR_SOURCE_STRING;
        var fileName = Path.GetFileName(path);
        if (File.Exists(path))
        {
            boxLabel += $"{fileName}";
            
            _sourcePathList = [path];
        }
        else if (Directory.Exists(path))
        {
            boxLabel += $"../{fileName}/";
            
            FileHelper.GetFilesFromPathByExtension(DRUM_RACK_LIVE_EXTENSION, path, out _sourcePathList);
        }
        else
        {
            // Invalid path
            return;
        }
        
        this.DropBoxBlock.Text = boxLabel;
        
        this.ResultBlock.Opacity = 0;
        this.ProcessButton.IsEnabled = true;
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
        _ = GeneralHelper.AnimateButtonText(this.ProcessButton, PROCESSING_STRING, animatedCts.Token);
        
        _targetPath = Path.Combine(TARGET_PATH, $"{PROCESSED_DIRECTORY}_{GeneralHelper.GetDateNow()}");
        
        var targetAdgPath = Path.Combine(_targetPath, TARGET_ADG_DIRECTORY);
        var processResultList = await Task.Run(() => DrumRackAdgProcessor.Process(_sourcePathList, targetAdgPath));
        
        var processOkList = processResultList.Where(_ => _.AdgValue == ProcessResult.Value.Ok);
        if (processOkList.Count() == _sourcePathList.Count)
        {
            this.ManageResult(isOk: true);
        }
        else if (!processOkList.Any())
        {
            this.ManageResult(isError: true);
        }
        else
        {
            this.ManageResult(isWarning: true);
        }

        if (this.PresetBundleCheckbox.IsChecked ?? false)
        {
            var targetPresetPath = Path.Combine(_targetPath, TARGET_PRESET_DIRECTORY);
            foreach (var processingOk in processOkList)
            {
                string presetName;
                
                if (processingOk is SamplesProcessResult samplesProcessResultOk)
                {
                    presetName = Path.GetFileNameWithoutExtension(samplesProcessResultOk.SourceFileName);

                    await this.CreatePresetAsync(presetName,
                                                 samplesProcessResultOk.SampleList,
                                                 targetPresetPath,
                                                 processingOk);

                    continue;
                }

                var multiSamplesProcessResultOk = processingOk as MultiSamplesProcessResult;
                foreach (var multiSamplesTuple in multiSamplesProcessResultOk.MultiSampleList)
                {
                    presetName = Path.GetFileNameWithoutExtension(multiSamplesTuple.Key);
                    
                    await this.CreatePresetAsync(presetName,
                                                 multiSamplesTuple.Value,
                                                 targetPresetPath,
                                                 processingOk);
                }
            }
        }
        
        // If the source are multiple, so the report will be generated
        if (_sourcePathList.Count > 1)
        {
            ReportGenerator.Generate(processResultList, Path.Combine(_targetPath, REPORT_FILE_NAME));
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
            this.ResultTextBlock.Text = RESULT_BLOCK_OK_STRING;
            this.ResultBlock.Background = Brushes.LightGreen;
        }
        else if (isWarning)
        {
            this.ResultTextBlock.Text = RESULT_BLOCK_WARNING_STRING;
            this.ResultBlock.Background = Brushes.LightYellow;
        }
        else
        {
            this.ResultTextBlock.Text = RESULT_BLOCK_ERROR_STRING;
            this.ResultBlock.Background = Brushes.LightCoral;
        }
        
        // Not used 'AddRange' to avoid overwriting the text above
        this.ResultTextBlock.Inlines.Add(RESULT_BLOCK_FOOTER_RUN[0]);
        this.ResultTextBlock.Inlines.Add(RESULT_BLOCK_FOOTER_RUN[1]);
    }

    private async Task CreatePresetAsync(string presetName,
                                         IEnumerable<Sample> sampleList,
                                         string targetPath,
                                         ProcessResult processResult)
    {
        // Ordering based on notes, so on pads
        var samplePathList = sampleList.OrderByDescending(_ => _.ReceivingNote)
                                       .Select(_ => _.Path);
        
        await Task.Run(() => MovePresetManager.GenerateDrumKit(presetName, samplePathList, targetPath, processResult));
    }
}