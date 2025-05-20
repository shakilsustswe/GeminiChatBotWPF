using System.IO;
using RestSharp.Portable.WebRequest;
using RestSharp.Portable;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace GeminiChatBotWPF
{
    public partial class MainWindow : Window
    {
        private string? _base64Image = null;
        private string? _mimeType = null;

        // Security tip: Store API keys in environment variables or config files  
        private static readonly string ApiKey = GetApiKeyFromConfig();

        private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

        private readonly List<object> chatHistory = new();

        public MainWindow()
        {
            InitializeComponent();
        }
        private static string GetApiKeyFromConfig()
        {
            var json = File.ReadAllText("config.json");
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("GeminiApiKey").GetString()!;
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var userInput = UserInputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(userInput)) return;

            if (!string.IsNullOrEmpty(_base64Image))
                ChatListBox.Items.Add("You (with image): " + userInput);
            else
                ChatListBox.Items.Add("You: " + userInput);

            ChatListBox.ScrollIntoView(ChatListBox.Items[ChatListBox.Items.Count - 1]);
            
            UserInputBox.Text = string.Empty; // Clear input box
            var parts = new List<object>
            {
                new { text = userInput }
            };


            if (!string.IsNullOrEmpty(_base64Image) && !string.IsNullOrEmpty(_mimeType))
            {
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = _mimeType,
                        data = _base64Image
                    }
                });

                _base64Image = null; // clear after sending
                _mimeType = null;
            }
            
            // Add user's message to chat history
            chatHistory.Add(new
            {
                role = "user",
                parts = parts.ToArray()
            });

            var requestBody = new
            {
                contents = chatHistory.ToArray()
            };

            var client = new RestClient($"{Endpoint}?key={ApiKey}");
            var request = new RestRequest();
            request.AddJsonBody(requestBody);
            try
            {
                SendButton.IsEnabled = false;
                UploadImageButton.IsEnabled = false;
                var response = await client.Execute(request);
                if (response.IsSuccess)
                {
                    var json = JsonDocument.Parse(response.Content!);
                    var text = json.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    ChatListBox.Items.Add("Gemini: " + text);
                    // Add Gemini's response to chat history
                    chatHistory.Add(new
                    {
                        role = "model",
                        parts = new[] { new { text = text } }
                    });
                }
                else
                {
                    ChatListBox.Items.Add("Error: " + response.Content);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
            finally
            {
                SendButton.IsEnabled = true;
                UploadImageButton.IsEnabled = true;

            }

            
        }

        private void UploadImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg)|*.png;*.jpg"
            };

            if (dialog.ShowDialog() == true)
            {
                byte[] imageBytes = File.ReadAllBytes(dialog.FileName);
                _base64Image = Convert.ToBase64String(imageBytes);
                _mimeType = Path.GetExtension(dialog.FileName).ToLower() == ".png" ? "image/png" : "image/jpeg";

                MessageBox.Show("Image uploaded successfully.");
            }
        }
    }
}
