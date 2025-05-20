using System.Net.Http;
using System.Text;
using RestSharp;
using RestSharp.Portable.WebRequest;
using RestSharp.Portable;
using System.Text.Json;
using System.Windows;

namespace GeminiChatBotWPF
{
    public partial class MainWindow : Window
    {
        // Security tip: Store API keys in environment variables or config files  
        private const string ApiKey = "AIzaSyAccETqJW5XKtIwk2w-SE3L9RocvtrM4Sk"; // Replace with secure storage  
        private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

        private readonly List<object> chatHistory = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string userInput = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(userInput)) return;

            ChatBox.AppendText($"\nYou: {userInput}\n");
            InputBox.Clear();

            chatHistory.Add(new
            {
                role = "user",
                parts = new[] { new { text = userInput } }
            });

            var requestBody = new { contents = chatHistory };

            using var httpClient = new HttpClient();
            var requestContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{Endpoint}?key={ApiKey}", requestContent);

            if (response.IsSuccessStatusCode)
            {
                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var reply = json.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                ChatBox.AppendText($"Gemini: {reply}\n");

                chatHistory.Add(new
                {
                    role = "model",
                    parts = new[] { new { text = reply } }
                });
            }
            else
            {
                ChatBox.AppendText($"Error: {await response.Content.ReadAsStringAsync()}\n");
            }
        }
    }
}
