using System; 
using DeviceKit.Events; 
 
namespace DeviceKit.Events; 
 
internal interface IDeviceEventSource 
{ 
    event EventHandler<DeviceDriverEvent>? EventOccurred; 
} 
