using System.Threading;
using System.Threading.Tasks;

using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;

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

    private async void OnOkClicked(object? sender, RoutedEventArgs e)
    {
        var processText = processButton.Content;
        this.IsEnabled = false;

        var cts = new CancellationTokenSource();
        _ = this.AnimateButtonText(processButton, "Processing", cts.Token);
        
        try
        {
            // Simula un'operazione lunga
            for (int i = 0; i <= 100; i += 10)
            {
                await Task.Delay(500); // Simula un lavoro lungo
            }
        }
        finally
        {
            await cts.CancelAsync();

            processButton.Content = processText;
            this.IsEnabled = true;
        }
    }
    
    private void Drop(object? sender, DragEventArgs e)
    {
        e.DragEffects &= DragDropEffects.Copy;

        if (e.Data.Contains(DataFormats.Files))
        {
            // DropState.Text = e.Data.GetFiles()?.FirstOrDefault()?.Path.ToString();
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