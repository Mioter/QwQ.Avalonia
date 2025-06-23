using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Sample.Models;

namespace Sample.ViewModels;

public class AlphabeticalScrollViewerPageViewModel : INotifyPropertyChanged
{
    private ObservableCollection<ContactItem> _contacts;
    private ObservableCollection<ContactItem> _favorites;
    private string _newName = string.Empty;
    private string _newPhone = string.Empty;
    private string _newEmail = string.Empty;
    private string _newDepartment = string.Empty;

    public AlphabeticalScrollViewerPageViewModel()
    {
        _contacts = new ObservableCollection<ContactItem>();
        _favorites = new ObservableCollection<ContactItem>();
        InitializeSampleData();
        
        AddContactCommand = new RelayCommand(AddContact, CanAddContact);
        RemoveContactCommand = new RelayCommand<ContactItem>(RemoveContact);
        AddToFavoritesCommand = new RelayCommand<ContactItem>(AddToFavorites);
        RemoveFromFavoritesCommand = new RelayCommand<ContactItem>(RemoveFromFavorites);
    }

    public ObservableCollection<ContactItem> Contacts
    {
        get => _contacts;
        set
        {
            if (_contacts != value)
            {
                _contacts = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<ContactItem> Favorites
    {
        get => _favorites;
        set
        {
            if (_favorites != value)
            {
                _favorites = value;
                OnPropertyChanged();
            }
        }
    }

    public string NewName
    {
        get => _newName;
        set
        {
            if (_newName != value)
            {
                _newName = value;
                OnPropertyChanged();
                (AddContactCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewPhone
    {
        get => _newPhone;
        set
        {
            if (_newPhone != value)
            {
                _newPhone = value;
                OnPropertyChanged();
            }
        }
    }

    public string NewEmail
    {
        get => _newEmail;
        set
        {
            if (_newEmail != value)
            {
                _newEmail = value;
                OnPropertyChanged();
            }
        }
    }

    public string NewDepartment
    {
        get => _newDepartment;
        set
        {
            if (_newDepartment != value)
            {
                _newDepartment = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand AddContactCommand { get; }
    public ICommand RemoveContactCommand { get; }
    public ICommand AddToFavoritesCommand { get; }
    public ICommand RemoveFromFavoritesCommand { get; }

    private void InitializeSampleData()
    {
        var sampleData = new[]
        {
            new ContactItem { Name = "Alice Johnson", Phone = "555-0101", Email = "alice@example.com", Department = "Engineering" },
            new ContactItem { Name = "Bob Smith", Phone = "555-0102", Email = "bob@example.com", Department = "Marketing" },
            new ContactItem { Name = "Charlie Brown", Phone = "555-0103", Email = "charlie@example.com", Department = "Sales" },
            new ContactItem { Name = "David Wilson", Phone = "555-0104", Email = "david@example.com", Department = "Engineering" },
            new ContactItem { Name = "Emma Davis", Phone = "555-0105", Email = "emma@example.com", Department = "HR" },
            new ContactItem { Name = "Frank Miller", Phone = "555-0106", Email = "frank@example.com", Department = "Finance" },
            new ContactItem { Name = "Grace Lee", Phone = "555-0107", Email = "grace@example.com", Department = "Engineering" },
            new ContactItem { Name = "Henry Taylor", Phone = "555-0108", Email = "henry@example.com", Department = "Marketing" },
            new ContactItem { Name = "Ivy Chen", Phone = "555-0109", Email = "ivy@example.com", Department = "Sales" },
            new ContactItem { Name = "Jack Anderson", Phone = "555-0110", Email = "jack@example.com", Department = "Engineering" },
            new ContactItem { Name = "Kate Martinez", Phone = "555-0111", Email = "kate@example.com", Department = "HR" },
            new ContactItem { Name = "Liam O'Connor", Phone = "555-0112", Email = "liam@example.com", Department = "Finance" },
            new ContactItem { Name = "Mia Rodriguez", Phone = "555-0113", Email = "mia@example.com", Department = "Engineering" },
            new ContactItem { Name = "Noah Thompson", Phone = "555-0114", Email = "noah@example.com", Department = "Marketing" },
            new ContactItem { Name = "Olivia Garcia", Phone = "555-0115", Email = "olivia@example.com", Department = "Sales" },
            new ContactItem { Name = "Paul White", Phone = "555-0116", Email = "paul@example.com", Department = "Engineering" },
            new ContactItem { Name = "Quinn Johnson", Phone = "555-0117", Email = "quinn@example.com", Department = "HR" },
            new ContactItem { Name = "Rachel Kim", Phone = "555-0118", Email = "rachel@example.com", Department = "Finance" },
            new ContactItem { Name = "Sam Davis", Phone = "555-0119", Email = "sam@example.com", Department = "Engineering" },
            new ContactItem { Name = "Tina Wang", Phone = "555-0120", Email = "tina@example.com", Department = "Marketing" },
            new ContactItem { Name = "Uma Patel", Phone = "555-0121", Email = "uma@example.com", Department = "Sales" },
            new ContactItem { Name = "Victor Lopez", Phone = "555-0122", Email = "victor@example.com", Department = "Engineering" },
            new ContactItem { Name = "Wendy Clark", Phone = "555-0123", Email = "wendy@example.com", Department = "HR" },
            new ContactItem { Name = "Xavier Hall", Phone = "555-0124", Email = "xavier@example.com", Department = "Finance" },
            new ContactItem { Name = "Yuki Tanaka", Phone = "555-0125", Email = "yuki@example.com", Department = "Engineering" },
            new ContactItem { Name = "Zoe Adams", Phone = "555-0126", Email = "zoe@example.com", Department = "Marketing" },
            // 添加一些以数字开头的联系人，用于测试#分组
            new ContactItem { Name = "123 Company", Phone = "555-9999", Email = "info@123company.com", Department = "Other" },
            new ContactItem { Name = "2Fast Delivery", Phone = "555-8888", Email = "contact@2fast.com", Department = "Other" },
            // 添加更多非字母开头的联系人
            new ContactItem { Name = "7-Eleven", Phone = "555-7777", Email = "info@7eleven.com", Department = "Retail" },
            new ContactItem { Name = "99 Cents Store", Phone = "555-9999", Email = "contact@99cents.com", Department = "Retail" },
            new ContactItem { Name = "@Tech Support", Phone = "555-0000", Email = "support@tech.com", Department = "IT" },
            new ContactItem { Name = "&More Services", Phone = "555-1111", Email = "info@andmore.com", Department = "Services" },
            new ContactItem { Name = "中文联系人", Phone = "555-2222", Email = "chinese@example.com", Department = "International" },
            new ContactItem { Name = "日本語名", Phone = "555-3333", Email = "japanese@example.com", Department = "International" },
            new ContactItem { Name = "한국어 이름", Phone = "555-4444", Email = "korean@example.com", Department = "International" },
            new ContactItem { Name = "!Important Contact", Phone = "555-5555", Email = "important@example.com", Department = "VIP" },
            new ContactItem { Name = "?Help Desk", Phone = "555-6666", Email = "help@example.com", Department = "Support" },
        };

        foreach (var contact in sampleData)
        {
            Contacts.Add(contact);
        }

        // 添加一些收藏联系人
        Favorites.Add(sampleData[0]); // Alice Johnson
        Favorites.Add(sampleData[5]); // Frank Miller
    }

    private void AddContact()
    {
        if (CanAddContact())
        {
            var newContact = new ContactItem
            {
                Name = NewName,
                Phone = NewPhone,
                Email = NewEmail,
                Department = NewDepartment ?? string.Empty,
            };

            Contacts.Add(newContact);

            // 清空输入框
            NewName = string.Empty;
            NewPhone = string.Empty;
            NewEmail = string.Empty;
            NewDepartment = string.Empty;
        }
    }

    private bool CanAddContact()
    {
        return !string.IsNullOrWhiteSpace(NewName);
    }

    private void RemoveContact(ContactItem? contact)
    {
        if (contact != null)
        {
            Contacts.Remove(contact);
            // 如果从收藏中移除
            if (Favorites.Contains(contact))
            {
                Favorites.Remove(contact);
            }
        }
    }

    private void AddToFavorites(ContactItem? contact)
    {
        if (contact != null && !Favorites.Contains(contact))
        {
            Favorites.Add(contact);
        }
    }

    private void RemoveFromFavorites(ContactItem? contact)
    {
        if (contact != null)
        {
            Favorites.Remove(contact);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// 简单的 RelayCommand 实现
public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
{
    private readonly Action<T?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => _execute((T?)parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
} 