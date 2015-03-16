using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Bing.Speech;

namespace Kinect2SampleSpeech
{

    public sealed partial class MainPage : Page
    {
        private SpeechRecognizer speechRec;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply credentials from the Windows Azure Data Marketplace.
            var credentials = new SpeechAuthorizationParameters();
            credentials.ClientId = "YOUR CLIENT ID";
            credentials.ClientSecret = "YOUR CLIENT SECRET";

            // Initialize the speech recognizer.
            speechRec = new SpeechRecognizer("en-US", credentials);

            // Add speech recognition event handlers.
            speechRec.AudioCaptureStateChanged += speechRec_AudioCaptureStateChanged;
            speechRec.AudioLevelChanged += speechRec_AudioLevelChanged;
            speechRec.RecognizerResultReceived += speechRec_RecognizerResultReceived;
        }

        void speechRec_RecognizerResultReceived(SpeechRecognizer sender, SpeechRecognitionResultReceivedEventArgs args)
        {
            if (args.Text == null) return;

            IntermediateResultsTextBlock.Text = "IntermediateResults: " + args.Text;

            if (args.Text.ToLower().Contains("cancel"))
                speechRec.RequestCancelOperation();
            else if (args.Text.ToLower().Contains("stop"))
                speechRec.StopListeningAndProcessAudio();
        }

        void speechRec_AudioLevelChanged(SpeechRecognizer sender, SpeechRecognitionAudioLevelChangedEventArgs args)
        {
            VolumeTextBlock.Text = "Volume:  "+ args.AudioLevel;
        }

        void speechRec_AudioCaptureStateChanged(SpeechRecognizer sender, SpeechRecognitionAudioCaptureStateChangedEventArgs args)
        {
            CaptureStateTextBlock.Text = "Capture State: " + Enum.GetName(typeof(SpeechRecognizerAudioCaptureState), args.State);
        }

        async private void StartRecButton_Click(object sender, RoutedEventArgs e)
        {
            // Prevent concurrent calls to an async method
            StartRecButton.IsEnabled = false;

            // Reset all the text
            VolumeTextBlock.Text = "";
            CaptureStateTextBlock.Text = "";
            IntermediateResultsTextBlock.Text = "";
            ConfidenceTextBlock.Text = "";
            FinalResultTextBlock.Text = "";
            AlternatesTextBlock.Text = "";

            // Use a try block because RecognizeSpeechToTextAsync depends on a web service which can throw exceptions.
            try
            {
                // Start speech recognition and await the result
                // As this is occuring, the RecognizerResultReceived will fire as the user is speaking.
                var result = await speechRec.RecognizeSpeechToTextAsync();

                // Show the TextConfidence.
                ConfidenceTextBlock.Text = "Confidence: " + Enum.GetName(typeof(SpeechRecognitionConfidence), result.TextConfidence);

                // Display the text.
                if (result.Text != null)
                {
                    FinalResultTextBlock.Text = result.Text;
                }

                // Fill a string with the alternate results.
                var alternates = result.GetAlternates(5);
                if (alternates.Count > 1)
                {
                    string s = "";
                    for (int i = 1; i < alternates.Count; i++)
                    {
                        s += "\n" + alternates[i].Text  ;
                    }
                    AlternatesTextBlock.Text = "Alternates: " + s;
                }
            }
            catch (Exception ex)
            {
                // If there's an exception, show it instead of the Final Result.
                if (ex.GetType() != typeof(OperationCanceledException))
                {
                    FinalResultTextBlock.Text = string.Format("{0}: {1}",
                                ex.GetType().ToString(), ex.Message);
                }
            }

            // Finished recording, allow recording again.
            StartRecButton.IsEnabled = true;
        }
    }
}
