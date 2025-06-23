# UniversalGroupScrollViewer 控件文档

## 主要特性
- 支持任意类型的分组Key（如字符串、数字、枚举等）
- 通过 `GroupKeySelector` 委托自定义分组方式
- 通过 `GroupKeyDisplaySelector` 委托自定义分组导航栏显示文本
- 分组导航栏根据实际分组动态生成，支持高亮当前分组
- 分组标题吸顶、推出效果
- 支持自定义 `ItemTemplate`
- 完全AOT友好，无反射

## 用法示例

### XAML
```xml
<control:UniversalGroupScrollViewer x:Name="UniversalGroupScrollViewer"
                                    ItemsSource="{Binding Contacts}">
    <control:UniversalGroupScrollViewer.ItemTemplate>
        <DataTemplate>
            <!-- 自定义项目模板 -->
        </DataTemplate>
    </control:UniversalGroupScrollViewer.ItemTemplate>
</control:UniversalGroupScrollViewer>
```

### 代码设置分组委托
```csharp
// 以部门分组为例
UniversalGroupScrollViewer.GroupKeySelector = obj =>
{
    var contact = obj as ContactItem;
    return contact?.Department ?? "";
};
UniversalGroupScrollViewer.GroupKeyDisplaySelector = key => key?.ToString() ?? "";
```
> 注意：GroupKeySelector/GroupKeyDisplaySelector 只能在代码中设置，不能通过XAML绑定。

## 属性说明
| 属性                        | 类型                         | 说明              |
|---------------------------|----------------------------|-----------------|
| `ItemsSource`             | `IEnumerable`              | 数据源集合           |
| `GroupKeySelector`        | `Func<object, object>`     | 分组Key选择器（代码设置）  |
| `GroupKeyDisplaySelector` | `Func<object, string>`     | 分组显示文本选择器（代码设置） |
| `GroupKeyComparer`        | `IComparer<object>`        | 分组排序器（可选）       |
| `ItemTemplate`            | `IDataTemplate`            | 项目模板            |
| `GroupedItems`            | `IReadOnlyList<GroupInfo>` | 分组后的项目（只读）      |
| `NavigationGroups`        | `IReadOnlyList<GroupInfo>` | 导航栏分组（只读）       |

## 典型场景
- 按部门、类型、标签、日期等任意字段分组
- 商品、联系人、文件、消息等列表的分组展示
- 支持分组导航、吸顶分组头

## MVVM注意事项
- 分组委托（GroupKeySelector/GroupKeyDisplaySelector）**只能在代码中设置**，不能XAML绑定
- 建议在页面/控件的构造函数或Loaded事件中设置
- 可将分组逻辑写为ViewModel的静态方法，便于复用

## 扩展建议
- 可自定义分组头模板、导航栏按钮模板
- 可扩展分组排序、分组统计、分组展开/收起等高级功能

## 数据模型示例
```csharp
public class ContactItem
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}
```

## 示例：按部门分组联系人
```csharp
UniversalGroupScrollViewer.GroupKeySelector = obj =>
{
    var contact = obj as ContactItem;
    return contact?.Department ?? "";
};
UniversalGroupScrollViewer.GroupKeyDisplaySelector = key => key?.ToString() ?? "";
```

---
如需更多分组场景或自定义需求，欢迎随时提问！ 