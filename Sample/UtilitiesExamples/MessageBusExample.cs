using System;
using System.Threading.Tasks;
using QwQ.Avalonia.Utilities.MessageBus;

namespace Sample.UtilitiesExamples;

/// <summary>
/// 示例消息类型
/// </summary>
public class TextMessage(string content)
{
    public string Content { get; } = content;
}

/// <summary>
/// 数值变化消息
/// </summary>
public class ValueChangedMessage(int oldValue, int newValue)
{
    public int OldValue { get; } = oldValue;
    public int NewValue { get; } = newValue;
}

/// <summary>
/// 消息发送者示例
/// </summary>
public class MessageSender
{
    private int _currentValue;

    /// <summary>
    /// 发送文本消息
    /// </summary>
    public void SendTextMessage(string text)
    {
        // 创建并发送消息
        MessageBus.CreateMessage(new TextMessage(text)).FromSender(this).Publish();

        Console.WriteLine($"[发送者] 已发送文本消息: {text}");
    }

    /// <summary>
    /// 发送高优先级文本消息
    /// </summary>
    public void SendHighPriorityMessage(string text)
    {
        // 创建高优先级消息
        MessageBus
            .CreateMessage(new TextMessage(text))
            /*.FromSender(this)*/
            .SetPriority(9) // 设置高优先级
            .Publish();

        Console.WriteLine($"[发送者] 已发送高优先级消息: {text}");
    }

    /// <summary>
    /// 发送一次性消息
    /// </summary>
    public void SendOneTimeMessage(string text)
    {
        // 创建一次性消息
        MessageBus
            .CreateMessage(new TextMessage(text))
            .FromSender(this)
            .SetAsOneTime() // 设置为一次性消息
            .Publish();

        Console.WriteLine($"[发送者] 已发送一次性消息: {text}");
    }

    /// <summary>
    /// 发送特定接收者消息
    /// </summary>
    public void SendTargetedMessage(string text)
    {
        // 创建特定接收者消息
        MessageBus
            .CreateMessage(new TextMessage(text))
            .FromSender(this)
            .AddReceivers<SpecificReceiver>() // 只发送给特定接收者
            .Publish();

        Console.WriteLine($"[发送者] 已发送特定接收者消息: {text}");
    }

    /// <summary>
    /// 更新值并发送变化消息
    /// </summary>
    public async Task UpdateValue(int newValue)
    {
        int oldValue = _currentValue;
        _currentValue = newValue;

        // 创建并发送值变化消息
        await MessageBus
            .CreateMessage(new ValueChangedMessage(oldValue, newValue))
            .FromSender(this)
            .WaitForCompletion() // 等待所有接收者处理完成
            .PublishAsync();

        Console.WriteLine($"[发送者] 值已从 {oldValue} 更新为 {newValue}");
    }
}

/// <summary>
/// 通用消息接收者示例
/// </summary>
public class MessageReceiver
{
    public MessageReceiver()
    {
        // 订阅文本消息
        MessageBus
            .ReceiveMessage<TextMessage>(this)
            .WithHandler(
                (message, sender) =>
                {
                    Console.WriteLine(
                        $"[接收者] 收到来自 {sender.GetType().Name} 的消息: {message.Content}"
                    );
                }
            )
            .Subscribe();

        // 订阅值变化消息
        MessageBus
            .ReceiveMessage<ValueChangedMessage>(this)
            .WithHandler(
                (message, _) =>
                {
                    Console.WriteLine($"[接收者] 值从 {message.OldValue} 变为 {message.NewValue}");
                }
            )
            .Subscribe();
    }

    /// <summary>
    /// 取消所有订阅
    /// </summary>
    public void Unsubscribe()
    {
        MessageBus.ReceiveMessage<TextMessage>(this).Unsubscribe();
        MessageBus.ReceiveMessage<ValueChangedMessage>(this).Unsubscribe();
        Console.WriteLine("[接收者] 已取消所有订阅");
    }
}

/// <summary>
/// 特定消息接收者示例
/// </summary>
public class SpecificReceiver
{
    public SpecificReceiver()
    {
        // 订阅文本消息，并设置过滤条件
        MessageBus
            .ReceiveMessage<TextMessage>(this)
            .WithFilter((message, _) => message.Content.Contains("重要")) // 只接收包含"重要"的消息
            .WithHandler(
                (message, _) =>
                {
                    Console.WriteLine($"[特定接收者] 收到重要消息: {message.Content}");
                }
            )
            .Subscribe();
    }

    /// <summary>
    /// 取消所有订阅
    /// </summary>
    public void Unsubscribe()
    {
        MessageBus.ReceiveMessage<TextMessage>(this).Unsubscribe();
        Console.WriteLine("[特定接收者] 已取消所有订阅");
    }
}

/// <summary>
/// 消息总线使用示例
/// </summary>
public static class MessageBusExample
{
    public static async Task RunExample()
    {
        Console.WriteLine("===== 消息总线示例 =====\n");

        // 创建发送者和接收者
        var sender = new MessageSender();
        var receiver = new MessageReceiver();
        var specificReceiver = new SpecificReceiver();

        // 发送普通消息
        sender.SendTextMessage("这是一条普通消息");

        // 发送高优先级消息
        sender.SendHighPriorityMessage("这是一条高优先级消息");

        // 发送包含特定关键词的消息
        sender.SendTextMessage("这是一条包含重要信息的消息");

        // 发送特定接收者消息
        sender.SendTargetedMessage("这条消息只发给特定接收者");

        // 发送一次性消息
        sender.SendOneTimeMessage("这是一条一次性消息");
        sender.SendOneTimeMessage("这条一次性消息不会被接收，因为订阅已失效");

        // 更新值并等待处理完成
        await Task.Run(() => sender.UpdateValue(100));

        // 取消订阅
        receiver.Unsubscribe();

        // 取消订阅后发送消息
        sender.SendTextMessage("这条消息不会被普通接收者接收，因为已取消订阅");

        specificReceiver.Unsubscribe();

        Console.WriteLine("\n===== 示例结束 =====");
    }
}
