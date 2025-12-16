using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceController.Core.Abstractions;
using DeviceController.Devices.Scanner;
using DeviceController.Services;
using System.Windows;

namespace DeviceController.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private DeviceViewModel? _selectedDevice;
        private readonly IDecodeEventBus? _decodeBus;
        private readonly System.Windows.Threading.Dispatcher _dispatcher;

        public MainViewModel(IDeviceRegistry deviceRegistry, IDecodeEventBus? decodeBus = null)
        {
            Devices = new ObservableCollection<DeviceViewModel>(deviceRegistry.Devices.Select(d => new DeviceViewModel(d)));
            SelectedDevice = Devices.FirstOrDefault();
            _decodeBus = decodeBus;
            _dispatcher = Application.Current.Dispatcher;
            Decodes = new ObservableCollection<string>();

            if (_decodeBus != null)
            {
                _decodeBus.DecodeReceived += OnDecodeReceived;
            }
        }

        public ObservableCollection<DeviceViewModel> Devices { get; }

        public DeviceViewModel? SelectedDevice
        {
            get => _selectedDevice;
            set => SetProperty(ref _selectedDevice, value);
        }

        public ObservableCollection<string> Decodes { get; }

        private void OnDecodeReceived(object? sender, ScannerDecodeData e)
        {
            void Add() => Decodes.Insert(0, $"{DateTime.Now:HH:mm:ss} [{e.BarcodeType:X2}] {e.Payload}");
            if (_dispatcher.CheckAccess())
            {
                Add();
            }
            else
            {
                _dispatcher.Invoke(Add);
            }
        }
    }
}
