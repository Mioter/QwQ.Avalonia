using System.Collections.ObjectModel;

namespace Test.App.Models;

public class MainWindowModel
{
    // 数据模型集合
    public ObservableCollection<object> PageModels { get; } =
    [
        new Test1PageModel(),
        new Test2PageModel(),
    ];
}
