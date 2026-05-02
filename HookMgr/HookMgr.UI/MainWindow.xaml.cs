using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using HookMgr.Core.Models;
using HookMgr.Core.Services;

namespace HookMgr.UI;

public partial class MainWindow : Window
{
    private readonly ILoggerService _loggerService;
    private readonly IFilterService _filterService;
    private readonly IHookService _hookService;
    
    private readonly ObservableCollection<ApiDefinition> _apiDefinitions;
    private readonly ObservableCollection<ApiCallInfo> _capturedCalls;
    private readonly ObservableCollection<ApiCallInfo> _filteredCalls;
    
    private int _selectedProcessId;
    private ApiCallInfo? _selectedCall;
    
    private readonly Random _random = new();
    private readonly string[] _testApis = { "CreateFileW", "ReadFile", "WriteFile", "RegOpenKeyExW", "MessageBoxW", "VirtualAlloc" };
    private readonly string[] _testParameters = {
        "fileName: \"C:\\\\test.txt\", desiredAccess: 0x80000000",
        "hFile: 0x12345678, buffer: 0xABCDEF00, numberOfBytesToRead: 1024",
        "hFile: 0x12345678, buffer: 0xABCDEF00, numberOfBytesToWrite: 512",
        "hKey: 0x80000002, subKey: \"Software\\\\Test\", ulOptions: 0, samDesired: 0x20019",
        "hWnd: 0x00000000, text: \"Hello World\", caption: \"Test\", type: 0x00000040",
        "lpAddress: 0x00000000, dwSize: 4096, flAllocationType: 0x00001000, flProtect: 0x04"
    };
    private readonly string[] _testReturnValues = { "0x12345678", "1", "0", "0x00000000", "1", "0xABCDEF00" };

    public MainWindow()
    {
        InitializeComponent();
        
        _loggerService = new FileLoggerService();
        _filterService = new FilterService();
        _hookService = new HookService(_loggerService, _filterService);
        
        _apiDefinitions = new ObservableCollection<ApiDefinition>(_hookService.GetAvailableApis());
        _capturedCalls = new ObservableCollection<ApiCallInfo>();
        _filteredCalls = new ObservableCollection<ApiCallInfo>();
        
        ApiDataGrid.ItemsSource = _apiDefinitions;
        CallsDataGrid.ItemsSource = _filteredCalls;
        
        _hookService.ApiCallCaptured += HookService_ApiCallCaptured;
        _filterService.FilterChanged += FilterService_FilterChanged;
        
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        
        LogPathTextBlock.Text = " | 日志路径: " + _loggerService.LogFilePath;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshProcessList();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_hookService.IsHooking)
        {
            _hookService.StopHooking();
        }
    }

    private void RefreshProcessList()
    {
        ProcessComboBox.Items.Clear();
        
        try
        {
            var processes = Process.GetProcesses()
                .OrderBy(p => p.ProcessName)
                .ToList();
            
            foreach (var process in processes)
            {
                try
                {
                    ProcessComboBox.Items.Add(new ProcessItem
                    {
                        Id = process.Id,
                        Name = process.ProcessName,
                        DisplayName = $"{process.ProcessName} (PID: {process.Id})"
                    });
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"获取进程列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void HookService_ApiCallCaptured(object? sender, ApiCallInfo e)
    {
        Dispatcher.Invoke(() =>
        {
            _capturedCalls.Insert(0, e);
            UpdateFilteredCalls();
            CallCountTextBlock.Text = " | 捕获调用数: " + _capturedCalls.Count;
        });
    }

    private void FilterService_FilterChanged(object? sender, FilterChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateFilteredCalls();
        });
    }

    private void UpdateFilteredCalls()
    {
        var searchText = SearchTextBox.Text.ToLower();
        
        _filteredCalls.Clear();
        
        foreach (var call in _capturedCalls)
        {
            if (!_filterService.ShouldCapture(call))
                continue;
            
            if (!string.IsNullOrEmpty(searchText))
            {
                if (!call.ApiName.ToLower().Contains(searchText) &&
                    !call.ModuleName.ToLower().Contains(searchText) &&
                    !call.Parameters.ToLower().Contains(searchText))
                {
                    continue;
                }
            }
            
            _filteredCalls.Add(call);
        }
    }

    private void RefreshProcessButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshProcessList();
    }

    private void StartHookButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProcessComboBox.SelectedItem == null)
        {
            MessageBox.Show("请先选择一个进程", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var selectedProcess = (ProcessItem)ProcessComboBox.SelectedItem;
        _selectedProcessId = selectedProcess.Id;
        
        try
        {
            _hookService.StartHooking(_selectedProcessId);
            
            StartHookButton.IsEnabled = false;
            StopHookButton.IsEnabled = true;
            ProcessComboBox.IsEnabled = false;
            RefreshProcessButton.IsEnabled = false;
            
            HookStatusTextBlock.Text = " | 钩取状态: 正在进行";
            StatusTextBlock.Text = "状态: 正在钩取进程 PID: " + _selectedProcessId;
            
            _loggerService.LogMessage("开始钩取进程 " + _selectedProcessId, LogLevel.Info);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动钩取失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _loggerService.LogMessage("启动钩取失败: " + ex.Message, LogLevel.Error);
        }
    }

    private void StopHookButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _hookService.StopHooking();
            
            StartHookButton.IsEnabled = true;
            StopHookButton.IsEnabled = false;
            ProcessComboBox.IsEnabled = true;
            RefreshProcessButton.IsEnabled = true;
            
            HookStatusTextBlock.Text = " | 钩取状态: 已停止";
            StatusTextBlock.Text = "状态: 未连接";
            
            _loggerService.LogMessage("停止钩取", LogLevel.Info);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"停止钩取失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _loggerService.LogMessage("停止钩取失败: " + ex.Message, LogLevel.Error);
        }
    }

    private void SimulateCallButton_Click(object sender, RoutedEventArgs e)
    {
        var index = _random.Next(_testApis.Length);
        var apiName = _testApis[index];
        var parameters = _testParameters[index];
        var returnValue = _testReturnValues[index];
        
        _hookService.SimulateApiCall(apiName, parameters, returnValue);
    }

    private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateFilteredCalls();
    }

    private void ClearCallsList_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("确定要清除所有捕获的调用列表吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            _capturedCalls.Clear();
            _filteredCalls.Clear();
            CallCountTextBlock.Text = " | 捕获调用数: 0";
            _loggerService.LogMessage("清除了调用列表", LogLevel.Info);
        }
    }

    private void CallsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CallsDataGrid.SelectedItem is ApiCallInfo selectedCall)
        {
            _selectedCall = selectedCall;
            
            DetailApiNameTextBlock.Text = "API名称: " + selectedCall.ApiName;
            DetailParametersTextBlock.Text = "参数: " + selectedCall.Parameters;
            DetailOriginalReturnTextBlock.Text = "原始返回值: " + selectedCall.OriginalReturnValue;
            ModifiedReturnValueTextBox.Text = selectedCall.ModifiedReturnValue;
        }
    }

    private void ApplyModifyReturnValue_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCall == null)
        {
            MessageBox.Show("请先选择一个调用记录", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var newValue = ModifiedReturnValueTextBox.Text.Trim();
        
        if (string.IsNullOrEmpty(newValue))
        {
            MessageBox.Show("请输入修改后的值", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        _hookService.ModifyReturnValue(_selectedCall.Id, newValue);
        _selectedCall.ModifiedReturnValue = newValue;
        _selectedCall.IsModified = true;
        
        var index = _filteredCalls.IndexOf(_selectedCall);
        if (index >= 0)
        {
            _filteredCalls.RemoveAt(index);
            _filteredCalls.Insert(index, _selectedCall);
        }
        
        MessageBox.Show("返回值已修改", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        _loggerService.LogMessage("修改了调用 " + _selectedCall.Id + " 的返回值为: " + newValue, LogLevel.Info);
    }
}

public class ProcessItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    
    public override string ToString()
    {
        return DisplayName;
    }
}
