using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Text;
using System.Net.Http;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Configuration;
using Newtonsoft.Json;
using Microsoft.CognitiveServices.SpeechRecognition;
using System;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using System.Collections.Generic;
using Microsoft.Rest;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Web;
using System.Net;


namespace speechapi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    
    public partial class MainWindow : Window
    {
        AutoResetEvent _FinalResponceEvent;
        MicrophoneRecognitionClient _microphoneRecognitionClient;
        string value_senti = "";
        string continuous = "";
        string partial = "";
   
        public MainWindow()
        {
            InitializeComponent();
            
           // _FinalResponceEvent = new AutoResetEvent(false);
            Responsetxt.Background = Brushes.White;
            Responsetxt.Foreground = Brushes.Black;
            

        }

        class ApiKeyServiceClientCredentials : ServiceClientCredentials
        {
            public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", "0270c3a760044bca8f1983aee4a39be3");
                return base.ProcessHttpRequestAsync(request, cancellationToken);
            }
        }
        ITextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
        {
            Endpoint = "https://eastus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment"
        };
      

        private void ConvertSpeechToText()
        {
            var speechRecognitionMode = SpeechRecognitionMode.ShortPhrase;
            string language = "en-us";
            string subscriptionKey = ConfigurationManager.AppSettings["MicrosoftSpeechApiKey"].ToString();
            _microphoneRecognitionClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                speechRecognitionMode,
                language,
                subscriptionKey
                );
            
            _microphoneRecognitionClient.OnPartialResponseReceived += ResponseReceived;
            
            _microphoneRecognitionClient.StartMicAndRecognition();
            


        }
     

        private void ResponseReceived(object sender, PartialSpeechResponseEventArgs e)
        {
            string result = e.PartialResult;


            Dispatcher.Invoke(() =>
            {

               
                partial = (e.PartialResult);

                 Responsetxt.Text = partial;
                Responsetxt.Text += ("\n");
                
                
                continuous = value_senti;
               
              
            });
           




        }




        public static Task Delay(double milliseconds)
        {
            var tcs = new TaskCompletionSource<bool>();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += (obj, args) =>
            {
                tcs.TrySetResult(true);
            };
            timer.Interval = milliseconds;
            timer.AutoReset = false;
            timer.Start();
            return tcs.Task;
        }

        private void Speakbtn_Click(object sender, RoutedEventArgs e)
        {
            Responsetxt.Text = "";
           // Speakbtn.Content = "Listening......";
            Speakbtn.IsEnabled = false;
            Responsetxt.Background = Brushes.Green;
            Responsetxt.Foreground = Brushes.White;
            ConvertSpeechToText();

        }

        private void Stopbtn_Click(object sender, RoutedEventArgs e)
        {

            Dispatcher.Invoke((Action)(() =>
                {
                    try
                    {
                        _FinalResponceEvent.Set();
                        _microphoneRecognitionClient.EndMicAndRecognition();
                        _microphoneRecognitionClient.Dispose();
                        _microphoneRecognitionClient = null;
                        Speakbtn.Content = "Start\nRecording";
                        Speakbtn.IsEnabled = true;
                        Responsetxt.Background = Brushes.White;
                        Responsetxt.Foreground = Brushes.Black;
                    }
                    catch (Exception e1) { Console.WriteLine(e); }
                }));
            // Speakbtn.Content = "";
            Speakbtn.IsEnabled = true;
            Responsetxt.Background = Brushes.White;
            Responsetxt.Foreground = Brushes.Black;
            Responsetxt.Text = "hello helpline number there is case of murder in my area send urgent help";
            GetSentiments(Responsetxt.Text);
          //  using (System.IO.StreamWriter file =
           //new System.IO.StreamWriter(@"C:\Users\saksham\Desktop\powerbi.xlsx", true))
           // {
           //  file.WriteLine(Responsetxt.Text+"\n");
           //}
          


        }



        private async void GetSentiments(string message)
        {
            string url = "https://eastus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";
            var body = new
            {
                documents = new[]
                {
                    new{id="1",text=message}
                }
            };

            string json = JsonConvert.SerializeObject(body);
            byte[] byteData = Encoding.UTF8.GetBytes(json);

            using (var content = new ByteArrayContent(byteData))
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "47bbec7631d94505b2194fe04dfbc059");
                    var response = await client.PostAsync(url, content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    var annon = new { documents = new[] { new { score = "" } } };
                    ///var annon = new { documents = new[] { new { id = "", detectedLanguages = new[] { new { name = "" } } } } };
                    var responseObject = JsonConvert.DeserializeAnonymousType(responseString, annon);
           
                    sentimenttxt.Text = responseObject.documents[0].score;

                    double sent_double=double.Parse(responseObject.documents[0].score, System.Globalization.CultureInfo.InvariantCulture);
                    if(sent_double<.30)
                    {
                        Getphrases(Responsetxt.Text);
                        


                    }
                    else
                    {
                        keyphasetxt.Text = "THIS CALL IS NOT THAT IMPPORTANT";
                    }


                }

            }
         }

        private async void Getphrases(string message)
        {
            string url = "https://eastus.api.cognitive.microsoft.com/text/analytics/v2.0/keyPhrases";
            var body = new
            {
                documents = new[]
                {
                    new{id="1",text=message}
                }
            };

            string json = JsonConvert.SerializeObject(body);
            byte[] byteData = Encoding.UTF8.GetBytes(json);

            using (var content = new ByteArrayContent(byteData))
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "47bbec7631d94505b2194fe04dfbc059");
                    var response = await client.PostAsync(url, content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    var annon = new { documents = new[] { new { KeyPhrases = new string[] { } } } };
                    ///var annon = new { documents = new[] { new { id = "", detectedLanguages = new[] { new { name = "" } } } } };
                    var responseObject = JsonConvert.DeserializeAnonymousType(responseString, annon);
                    string[] Keyword = new string[] { };
                    Keyword = responseObject.documents[0].KeyPhrases;
                    string str = "";
                    for (int i = 0; i < Keyword.Length; ++i)
                    {
                        str += Keyword[i].ToString();
                        if (i < Keyword.Length)
                            str += "\n";
                    }
                    string whole_string=keyphasetxt.Text;

                    if (message.Contains("flood") || message.Contains("cyclone") || message.Contains("earthquake") || message.Contains(" typhoon") || message.Contains("landslides") || message.Contains("tsunami") ||message.Contains("location"))
                    {
                        keyphasetxt.Text = str;
                        Getidentity(Responsetxt.Text);
                        using (StreamWriter writetext = new StreamWriter("powerbi.xlsx"))
                        {
                            writetext.WriteLine(Responsetxt.Text);
                        }
                    }
                    else
                    {
                        keyphasetxt.Text = "This emergency is not related to natural disaster,but must be looked upon";
                        
                    }

                }
            }

        }

        private async void Getidentity(string message)
        {
            string url = "https://eastus.api.cognitive.microsoft.com/text/analytics/v2.0/entities";
            var body = new
            {
                documents = new[]
                {
                    new{id="1",text=message}
                }
            };

            string json = JsonConvert.SerializeObject(body);
            byte[] byteData = Encoding.UTF8.GetBytes(json);

            using (var content = new ByteArrayContent(byteData))
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "47bbec7631d94505b2194fe04dfbc059");
                    var response = await client.PostAsync(url, content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    var annon = new { documents = new[] { new { id="",Entities = new[] { new { Name = "" } } } } };
                    ///var annon = new { documents = new[] { new { id = "", detectedLanguages = new[] { new { name = "" } } } } };
                    var responseObject = JsonConvert.DeserializeAnonymousType(responseString, annon);
                   
                 
                    string str ="";
                    for (int i = 0; i < responseObject.documents[0].Entities.Length; ++i)
                    {
                       str+= responseObject.documents[0].Entities[i].ToString();
                        if (i < responseObject.documents[0].Entities.Length)
                            str += "\n";
                    }

                    



                }
            }

        }

   


















        private void Readbtn_Click(object sender, RoutedEventArgs e)
        {

            string final_val = Responsetxt.Text;
            HttpWebRequest request = null;
            request = (HttpWebRequest)HttpWebRequest.Create("https://speech.platform.bing.com/speech/recognition/interactive/cognitiveservices/v1?language=en-US&format=detailed HTTP/1.1");
            request.SendChunked = true;
            request.Accept = @"application/json;text/xml";
            request.Method = "POST";
            request.ProtocolVersion = HttpVersion.Version11;
            request.ContentType = @"audio/wav; codec=audio/pcm; samplerate=16000";
            request.Headers["Ocp-Apim-Subscription-Key"] = "f81969c70f2240bf8ba0273a626fe580";
            using (FileStream fs = new FileStream("fg-chocolate.wav", FileMode.Open, FileAccess.Read))
            {

                ///audio\\" + Convert.ToString(auditr) + ".wav"

                byte[] buffer = null;
                int bytesRead = 0;
                using (Stream requestStream = request.GetRequestStream())
                {

                    buffer = new Byte[checked((uint)Math.Min(1024, (int)fs.Length))];
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        requestStream.Write(buffer, 0, bytesRead);
                    }

                    // Flush
                    requestStream.Flush();
                }
            }
            string responseString = "";
            Console.WriteLine("Response:");
            using (WebResponse response = request.GetResponse())
            {
                Console.WriteLine(((HttpWebResponse)response).StatusCode);

                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    responseString = sr.ReadToEnd();
                }

                Console.WriteLine(responseString);
                Console.ReadLine();
            }
            ///GetSentiments(Responsetxt.Text);

            var annon = new { DisplayText = "" };
            ///var annon = new { documents = new[] { new { id = "", detectedLanguages = new[] { new { name = "" } } } } };
            var responseObject = JsonConvert.DeserializeAnonymousType(responseString, annon);

            string Keyword = responseObject.DisplayText;
            Responsetxt.Text = Keyword;
            
            Speakbtn.IsEnabled = true;
            Responsetxt.Background = Brushes.White;
            Responsetxt.Foreground = Brushes.Black;



        }


    }
 }

