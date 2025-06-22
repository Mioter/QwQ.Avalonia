# AlphabeticalScrollViewer 控件示例

## 功能特性

AlphabeticalScrollViewer 是一个支持按字母顺序分组的滚动视图控件，具有以下特性：

### 核心功能
- **MVVM 支持**：通过 `ItemsSource` 和 `ItemTemplate` 进行数据绑定
- **字母分组**：自动按指定属性（通过 `LetterMemberPath`）的首字母进行分组
- **字母索引导航**：右侧字母导航栏支持快速跳转到对应分组
- **粘性标题**：滚动时当前分组标题会粘在顶部
- **数字分组**：以数字开头的项目会自动归类到 `#` 分组
- **收藏功能**：支持收藏项目，收藏项会显示在列表最前面

### 字母导航栏
- **收藏按钮 (♥)**：位于导航栏顶部，点击可跳转到收藏区域
- **A-Z 字母**：26个英文字母按钮，支持快速导航
- **# 按钮**：位于导航栏底部，包含所有以数字开头的项目

### 交互功能
- **点击导航**：点击字母按钮可快速跳转到对应分组
- **拖拽导航**：在字母导航栏上拖拽可连续滚动
- **鼠标滚轮**：在字母导航栏上使用鼠标滚轮可滚动内容
- **收藏管理**：支持添加/移除收藏项目

## 使用方法

### 基本用法

```xml
<control:AlphabeticalScrollViewer 
    ItemsSource="{Binding Contacts}"
    LetterMemberPath="Name">
    <control:AlphabeticalScrollViewer.ItemTemplate>
        <DataTemplate>
            <!-- 自定义项目模板 -->
        </DataTemplate>
    </control:AlphabeticalScrollViewer.ItemTemplate>
</control:AlphabeticalScrollViewer>
```

### 带收藏功能

```xml
<control:AlphabeticalScrollViewer 
    ItemsSource="{Binding Contacts}"
    Favorites="{Binding Favorites}"
    LetterMemberPath="Name">
    <!-- 项目模板 -->
</control:AlphabeticalScrollViewer>
```

## 属性说明

| 属性 | 类型 | 说明 |
|------|------|------|
| `ItemsSource` | `IEnumerable` | 数据源集合 |
| `LetterMemberPath` | `string` | 用于分组的属性路径 |
| `Favorites` | `IEnumerable` | 收藏项目集合 |
| `ItemTemplate` | `IDataTemplate` | 项目模板 |
| `GroupedItems` | `IReadOnlyList<LetterGroup>` | 分组后的项目（只读） |
| `Alphabet` | `IReadOnlyList<AlphabetLetterViewModel>` | 字母导航栏数据（只读） |

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

### 3. 数字分组测试
- 包含以数字开头的联系人（如 "123 Company", "2Fast Delivery"）
- 这些项目会自动归类到 `#` 分组

### 4. 非字母分组测试
- 包含各种非字母开头的联系人：
  - 数字开头：`7-Eleven`, `99 Cents Store`
  - 特殊字符：`@Tech Support`, `&More Services`, `!Important Contact`, `?Help Desk`
  - 中文字符：`中文联系人`
  - 日文字符：`日本語名`
  - 韩文字符：`한국어 이름`
- 所有这些非A-Z字母开头的项目都会归类到 `#` 分组

### 5. 交互体验
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
- 使用反射获取指定属性的值
- 只有A-Z字母字符按字母分组
- 所有其他字符（数字、特殊字符、中文字符等）都归类到 `#` 分组
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
- **问题**：收藏按钮（♥）和#按钮使用了特殊样式，与其他字母按钮外观不一致
- **修复**：移除特殊按钮的样式定义，让所有导航按钮保持一致的外观

### 分组逻辑优化
- **问题**：只有数字字符归类到#分组，其他特殊字符和中文字符仍然按原字符分组
- **修复**：修改分组逻辑，使除了A-Z字母外的所有首字符都归类到#分组，包括数字、特殊字符、中文字符等

## 最近修复

### 数字分组问题
- **问题**：以数字开头的项目没有正确归类到 `#` 分组，而是单独显示为数字分组
- **修复**：在 `GetLetterFromItem` 方法中添加数字检测逻辑，确保数字字符返回 `#`

### 收藏数据绑定问题
- **问题**：收藏功能的数据绑定有问题，添加收藏后界面不立即更新
- **修复**：添加对 `Favorites` 集合变化的监听，确保收藏操作后立即更新界面

### #分组排序问题
- **问题**：`#` 分组显示在收藏分组下方，而不是在所有字母分组下方
- **修复**：修改分组排序逻辑，使用 `OrderBy(g => g.Key == '#' ? 'Z' + 1 : g.Key)` 确保 `#` 分组排在所有字母之后

## 使用建议

1. **性能优化**：对于大量数据，建议使用虚拟化容器
2. **样式定制**：可以通过修改控件模板来自定义外观
3. **数据绑定**：确保数据模型实现 `INotifyPropertyChanged` 接口
4. **收藏管理**：建议使用 `ObservableCollection` 作为收藏集合以支持实时更新 