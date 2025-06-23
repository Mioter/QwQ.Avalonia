# AlphabeticalScrollViewer 控件示例

## 功能特性

AlphabeticalScrollViewer 是一个支持按字母顺序分组的滚动视图控件，具有以下特性：

### 核心功能
- **MVVM 支持**：通过 `ItemsSource` 和 `ItemTemplate` 进行数据绑定
- **字母分组**：通过 `LetterSelector` 委托自定义分组逻辑，支持AOT
- **字母索引导航**：右侧字母导航栏支持快速跳转到对应分组
- **粘性标题**：滚动时当前分组标题会粘在顶部
- **数字分组**：以非A-Z字母开头的项目会自动归类到 `#` 分组
- **收藏功能**：支持收藏项目，收藏项会显示在列表最前面

### 字母导航栏
- **收藏按钮 (♥)**：位于导航栏顶部，点击可跳转到收藏区域
- **A-Z 字母**：26个英文字母按钮，支持快速导航
- **# 按钮**：位于导航栏底部，包含所有非A-Z字母开头的项目

### 交互功能
- **点击导航**：点击字母按钮可快速跳转到对应分组
- **拖拽导航**：在字母导航栏上拖拽可连续滚动
- **鼠标滚轮**：在字母导航栏上使用鼠标滚轮可滚动内容
- **收藏管理**：支持添加/移除收藏项目

## 使用方法

### 基本用法

```xml
<control:AlphabeticalScrollViewer 
    x:Name="AlphabeticalScrollViewer"
    ItemsSource="{Binding Contacts}">
    <control:AlphabeticalScrollViewer.ItemTemplate>
        <DataTemplate>
            <!-- 自定义项目模板 -->
        </DataTemplate>
    </control:AlphabeticalScrollViewer.ItemTemplate>
</control:AlphabeticalScrollViewer>
```

### 设置分组逻辑（代码中设置 LetterSelector）

```csharp
// 在页面/控件的代码中设置
AlphabeticalScrollViewer.LetterSelector = obj => {
    var contact = obj as ContactItem;
    if (contact != null && !string.IsNullOrEmpty(contact.Name))
    {
        char c = char.ToUpper(contact.Name[0]);
        return (c >= 'A' && c <= 'Z') ? c : '#';
    }
    return '#';
};
```
> 注意：LetterSelector 只能在代码中设置，不能通过XAML绑定。

### 带收藏功能

```xml
<control:AlphabeticalScrollViewer 
    x:Name="AlphabeticalScrollViewer"
    ItemsSource="{Binding Contacts}"
    Favorites="{Binding Favorites}">
    <!-- 项目模板 -->
</control:AlphabeticalScrollViewer>
```

## 属性说明

| 属性               | 类型                                       | 说明            |
|------------------|------------------------------------------|---------------|
| `ItemsSource`    | `IEnumerable`                            | 数据源集合         |
| `LetterSelector` | `Func<object, char>`                     | 用于分组的委托（代码设置） |
| `Favorites`      | `IEnumerable`                            | 收藏项目集合        |
| `ItemTemplate`   | `IDataTemplate`                          | 项目模板          |
| `GroupedItems`   | `IReadOnlyList<LetterGroup>`             | 分组后的项目（只读）    |
| `Alphabet`       | `IReadOnlyList<AlphabetLetterViewModel>` | 字母导航栏数据（只读）   |

## 示例功能

本示例展示了以下功能：

### 1. 基本联系人管理
- 添加新联系人
- 删除现有联系人
- 动态分组更新

### 2. 收藏功能
- 添加联系人到收藏
- 从收藏中移除联系人
- 收藏项目显示在列表顶部

### 3. 非字母分组测试
- 包含以数字、特殊字符、中文等开头的联系人，这些项目会自动归类到 `#` 分组

### 4. 交互体验
- 点击字母导航栏快速跳转
- 滚动时粘性标题显示当前分组

## 数据模型

```csharp
public class ContactItem
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}
```

## 技术实现

### 分组逻辑
- 通过 LetterSelector 委托自定义分组逻辑，AOT友好
- 只有A-Z字母字符按字母分组，其他全部归类到#分组
- 收藏项显示在列表最前面

### 收藏功能
- 监听 `Favorites` 集合变化
- 收藏项显示在列表最前面
- 支持动态添加/移除收藏

### 导航功能
- 精确计算分组位置
- 平滑滚动到目标分组
- 支持收藏区域快速跳转

### 样式统一化
- 所有导航按钮保持一致的外观

### 分组逻辑优化
- 除A-Z字母外的所有首字符都归类到#分组，包括数字、特殊字符、中文字符等

## 使用建议

1. **性能优化**：对于大量数据，建议使用虚拟化容器
2. **样式定制**：可以通过修改控件模板来自定义外观
3. **数据绑定**：确保数据模型实现 `INotifyPropertyChanged` 接口
4. **收藏管理**：建议使用 `ObservableCollection` 作为收藏集合以支持实时更新 