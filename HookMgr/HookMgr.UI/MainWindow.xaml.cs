﻿using System.Collections.ObjectModel;
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
    private ApiDefinition? _selectedApi;
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
        
        LogPathTextBlock.Text = $"日志路径: {_loggerService.LogFilePath}";
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
                    // 忽略无法访问的进程
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
            CallCountTextBlock.Text = $"捕获调用数: {_capturedCalls.Count}";
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
            // 应用过滤器
            if (!_filterService.ShouldCapture(call))
                continue;
            
            // 应用搜索
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

    private void ProcessComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProcessComboBox.SelectedItem is ProcessItem selectedProcess)
        {
            _selectedProcessId = selectedProcess.Id;
            StatusTextBlock.Text = $"状态: 已选择进程 {selectedProcess.DisplayName}";
        }
    }

    private void RefreshProcessButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshProcessList();
    }

    private void StartHookButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProcessId == 0)
        {
            MessageBox.Show("请先选择一个进程", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            _hookService.StartHooking(_selectedProcessId);
            
            StartHookButton.IsEnabled = false;
            StopHookButton.IsEnabled = true;
            ProcessComboBox.IsEnabled = false;
            RefreshProcessButton.IsEnabled = false;
            
            HookStatusTextBlock.Text = "钩取状态: 正在进行";
            StatusTextBlock.Text = $"状态: 正在钩取进程 PID: {_selectedProcessId}";
            
            _loggerService.LogMessage($"开始钩取进程 {_selectedProcessId}", LogLevel.Info);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动钩取失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _loggerService.LogMessage($"启动钩取失败: {ex.Message}", LogLevel.Error);
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
            
            HookStatusTextBlock.Text = "钩取状态: 已停止";
            StatusTextBlock.Text = "状态: 未连接";
            
            _loggerService.LogMessage("停止钩取", LogLevel.Info);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"停止钩取失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _loggerService.LogMessage($"停止钩取失败: {ex.Message}", LogLevel.Error);
        }
    }

    private void SimulateCallButton_Click(object sender, RoutedEventArgs e)
    {
        // 模拟一个API调用
        var index = _random.Next(_testApis.Length);
        var apiName = _testApis[index];
        var parameters = _testParameters[index];
        var returnValue = _testReturnValues[index];
        
        _hookService.SimulateApiCall(apiName, parameters, returnValue);
    }

    private void ApiDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ApiDataGrid.SelectedItem is ApiDefinition selectedApi)
        {
            _selectedApi = selectedApi;
            
            // 自动填充重定向设置
            RedirectApiNameTextBox.Text = selectedApi.Name;
        }
    }

    private void EnableApiButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedApi != null)
        {
            _hookService.EnableApiHook(_selectedApi.Name);
            _selectedApi.IsEnabled = true;
            
            // 刷新列表
            var index = _apiDefinitions.IndexOf(_selectedApi);
            _apiDefinitions.RemoveAt(index);
            _apiDefinitions.Insert(index, _selectedApi);
        }
    }

    private void DisableApiButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedApi != null)
        {
            _hookService.DisableApiHook(_selectedApi.Name);
            _selectedApi.IsEnabled = false;
            
            // 刷新列表
            var index = _apiDefinitions.IndexOf(_selectedApi);
            _apiDefinitions.RemoveAt(index);
            _apiDefinitions.Insert(index, _selectedApi);
        }
    }

    private void CallsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CallsDataGrid.SelectedItem is ApiCallInfo selectedCall)
        {
            _selectedCall = selectedCall;
            
            // 更新详细信息
            DetailTimestampTextBox.Text = selectedCall.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            DetailApiNameTextBox.Text = selectedCall.ApiName;
            DetailModuleNameTextBox.Text = selectedCall.ModuleName;
            DetailProcessIdTextBox.Text = selectedCall.ProcessId.ToString();
            DetailProcessNameTextBox.Text = selectedCall.ProcessName;
            DetailParametersTextBox.Text = selectedCall.Parameters;
            DetailOriginalReturnValueTextBox.Text = selectedCall.OriginalReturnValue;
            DetailModifiedReturnValueTextBox.Text = selectedCall.ModifiedReturnValue;
            DetailRedirectTargetTextBox.Text = selectedCall.RedirectTarget;
            DetailCallStackTextBox.Text = selectedCall.CallStack;
        }
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
            CallCountTextBlock.Text = "捕获调用数: 0";
            _loggerService.LogMessage("清除了调用列表", LogLevel.Info);
        }
    }

    private void ApplyIncludeFilter_Click(object sender, RoutedEventArgs e)
    {
        var filters = IncludeFilterTextBox.Text
            .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrEmpty(f));
        
        _filterService.ClearFilters();
        
        foreach (var filter in filters)
        {
            _filterService.AddIncludeFilter(filter);
        }
        
        MessageBox.Show("包含过滤已应用", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        _loggerService.LogMessage("应用了包含过滤", LogLevel.Info);
    }

    private void ApplyExcludeFilter_Click(object sender, RoutedEventArgs e)
    {
        var filters = ExcludeFilterTextBox.Text
            .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrEmpty(f));
        
        foreach (var filter in filters)
        {
            _filterService.AddExcludeFilter(filter);
        }
        
        MessageBox.Show("排除过滤已应用", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        _loggerService.LogMessage("应用了排除过滤", LogLevel.Info);
    }

    private void SetRedirect_Click(object sender, RoutedEventArgs e)
    {
        var apiName = RedirectApiNameTextBox.Text.Trim();
        var target = RedirectTargetTextBox.Text.Trim();
        
        if (string.IsNullOrEmpty(apiName))
        {
            MessageBox.Show("请输入API名称", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (string.IsNullOrEmpty(target))
        {
            MessageBox.Show("请输入目标函数", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        _hookService.SetRedirect(apiName, target);
        MessageBox.Show($"已设置重定向: {apiName} -> {target}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RemoveRedirect_Click(object sender, RoutedEventArgs e)
    {
        var apiName = RedirectApiNameTextBox.Text.Trim();
        
        if (string.IsNullOrEmpty(apiName))
        {
            MessageBox.Show("请输入API名称", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        _hookService.RemoveRedirect(apiName);
        MessageBox.Show($"已移除重定向: {apiName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ClearAllFilters_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("确定要清除所有过滤和重定向设置吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            _filterService.ClearFilters();
            IncludeFilterTextBox.Clear();
            ExcludeFilterTextBox.Clear();
            RedirectApiNameTextBox.Clear();
            RedirectTargetTextBox.Clear();
            
            UpdateFilteredCalls();
            
            MessageBox.Show("所有过滤和重定向设置已清除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            _loggerService.LogMessage("清除了所有过滤和重定向设置", LogLevel.Info);
        }
    }

    private void ApplyModifyReturnValue_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCall == null)
        {
            MessageBox.Show("请先选择一个调用记录", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var newValue = DetailModifiedReturnValueTextBox.Text.Trim();
        
        if (string.IsNullOrEmpty(newValue))
        {
            MessageBox.Show("请输入修改后的值", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        _hookService.ModifyReturnValue(_selectedCall.Id, newValue);
        _selectedCall.ModifiedReturnValue = newValue;
        _selectedCall.IsModified = true;
        
        // 刷新列表
        var index = _filteredCalls.IndexOf(_selectedCall);
        if (index >= 0)
        {
            _filteredCalls.RemoveAt(index);
            _filteredCalls.Insert(index, _selectedCall);
        }
        
        MessageBox.Show("返回值已修改", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        _loggerService.LogMessage($"修改了调用 {_selectedCall.Id} 的返回值为: {newValue}", LogLevel.Info);
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
