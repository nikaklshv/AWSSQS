using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWSSQS.Models;
using System.Text.Encodings.Web;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

var sqs = new AmazonSQSClient("accesskey", "secretkey", RegionEndpoint.EUCentral1);
var queueUrl = "url";

bool isRunning = true;

while (isRunning)
{
    Console.WriteLine("\n=== ГОЛОВНЕ МЕНЮ ===");
    Console.WriteLine("1 - Створити заявку");
    Console.WriteLine("2 - Обробити всі заявки");
    Console.WriteLine("3 - Показати приблизну кількість заявок у черзі");
    Console.WriteLine("0 - Вихід");
    Console.Write("Оберіть дію: ");

    string choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            await CreateRequest(sqs, queueUrl);
            break;
        case "2":
            await ProcessRequests(sqs, queueUrl);
            break;
        case "3":
            await ShowQueueSize(sqs, queueUrl);
            break;
        case "0":
            isRunning = false;
            Console.WriteLine("Вихід з програми...");
            break;
        default:
            Console.WriteLine("Невірний вибір. Спробуйте ще раз.");
            break;
    }
}

// --- МЕТОДИ ДЛЯ ОБРОБКИ ОПЕРАЦІЙ ---

async Task CreateRequest(AmazonSQSClient client, string url)
{
    Console.WriteLine("\n=== Створення заявки в службу підтримки ===");
    Console.Write("Введіть ваше ім'я: ");
    string userName = Console.ReadLine();

    Console.Write("Введіть тему заявки: ");
    string topic = Console.ReadLine();

    Console.Write("Введіть опис проблеми: ");
    string description = Console.ReadLine();

    Console.Write("Введіть пріоритет (Low, Medium, High): ");
    string priority = Console.ReadLine();

    var supportRequest = new SupportRequest
    {
        Id = $"REQ-{Random.Shared.Next(1000, 9999)}",
        UserName = userName,
        Topic = topic,
        Description = description,
        Priority = string.IsNullOrWhiteSpace(priority) ? "Low" : priority,
        CreatedAt = DateTime.UtcNow
    };

    var options = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    string jsonRequest = JsonSerializer.Serialize(supportRequest, options);

    try
    {
        var sendMessageRequest = new SendMessageRequest
        {
            QueueUrl = url,
            MessageBody = jsonRequest
        };

        await client.SendMessageAsync(sendMessageRequest);
        Console.WriteLine($"\n[✓] Заявку № {supportRequest.Id} успішно додано в чергу.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n[Х] Помилка при відправленні в SQS: {ex.Message}");
    }
}

async Task ProcessRequests(AmazonSQSClient client, string url)
{
    Console.WriteLine("\n=== Зчитування заявок з черги ===");
    ReceiveMessageResponse response;
    bool hasMessages = false;

    do
    {
        response = await client.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = url,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 5
        });

        if (response.Messages == null || response.Messages.Count == 0)
        {
            if (!hasMessages)
            {
                Console.WriteLine("Черга порожня. Немає нових заявок для обробки.");
            }
            break;
        }

        hasMessages = true;

        foreach (var message in response.Messages)
        {
            try
            {
                var receivedRequest = JsonSerializer.Deserialize<SupportRequest>(message.Body);

                TimeSpan waitTime = DateTime.UtcNow - receivedRequest.CreatedAt;
                string formattedWaitTime = $"{(int)waitTime.TotalHours} год {waitTime.Minutes} хв {waitTime.Seconds} сек";

                Console.WriteLine("\n-----------------------------");
                Console.WriteLine($"Id: {receivedRequest.Id}");
                Console.WriteLine($"Користувач: {receivedRequest.UserName}");
                Console.WriteLine($"Тема: {receivedRequest.Topic}");
                Console.WriteLine($"Опис: {receivedRequest.Description}");
                Console.WriteLine($"Пріоритет: {receivedRequest.Priority}");
                Console.WriteLine($"Час в черзі: {formattedWaitTime}");

                if (receivedRequest.Priority?.Equals("High", StringComparison.OrdinalIgnoreCase) == true)
                {
                    Console.WriteLine("⚠️ ТЕРМІНОВА ЗАЯВКА ⚠️");
                }
                Console.WriteLine("-----------------------------");

                await client.DeleteMessageAsync(url, message.ReceiptHandle);
                Console.WriteLine($"[✓] Повідомлення {receivedRequest.Id} успішно оброблено та видалено.");
            }
            catch (JsonException)
            {
                Console.WriteLine("\n[Х] Помилка: Не вдалося розпізнати формат повідомлення. Видалення з черги.");
                await client.DeleteMessageAsync(url, message.ReceiptHandle);
            }
        }

    } while (response.Messages != null && response.Messages.Count > 0);

    if (hasMessages) Console.WriteLine("\nОбробку завершено.");
}

async Task ShowQueueSize(AmazonSQSClient client, string url)
{
    try
    {
        var attributesRequest = new GetQueueAttributesRequest
        {
            QueueUrl = url,
            AttributeNames = new List<string> { "ApproximateNumberOfMessages" }
        };

        var response = await client.GetQueueAttributesAsync(attributesRequest);
        string count = response.Attributes["ApproximateNumberOfMessages"];

        Console.WriteLine($"\n[i] Приблизна кількість заявок у черзі: {count}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n[Х] Не вдалося отримати дані черги: {ex.Message}");
    }
}