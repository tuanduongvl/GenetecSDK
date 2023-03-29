using Genetec.Sdk;
using Genetec.Sdk.Entities;
using Genetec.Sdk.Queries;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace testCCTV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Property for the Camera selected index.
        public static readonly DependencyProperty CamerasSelectedIndexProperty = DependencyProperty.Register(
                            "CamerasSelectedIndex", typeof(int), typeof(MainWindow), new PropertyMetadata(-1));

        // Property for the Group button to control the Ptz Camera.
        public static readonly DependencyProperty IsPtzCameraSelectedProperty = DependencyProperty.Register(
                            "IsPtzCameraSelected", typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

        // Property for the connection to the sdk engine.
        public static readonly DependencyProperty IsSdkEngineConnectedProperty = DependencyProperty.Register(
                            "IsSdkEngineConnected", typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

        // Property for the message about the user locking the camera.
        public static readonly DependencyProperty PtzLockedMessageProperty = DependencyProperty.Register(
                            "PtzLockedMessage", typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));


        private Genetec.Sdk.Media.Ptz.PtzCoordinatesManager m_pcm;
        public ObservableCollection<Camera> CamerasItems { get; private set; }
        public int CamerasSelectedIndex
        {
            get => (int)GetValue(CamerasSelectedIndexProperty);
            set => SetValue(CamerasSelectedIndexProperty, value);
        }

        public bool IsPtzCameraSelected
        {
            get => (bool)GetValue(IsPtzCameraSelectedProperty);
            set => SetValue(IsPtzCameraSelectedProperty, value);
        }

        public bool IsSdkEngineConnected
        {
            get => (bool)GetValue(IsSdkEngineConnectedProperty);
            set => SetValue(IsSdkEngineConnectedProperty, value);
        }

        private readonly Engine m_sdkEngine;
        public MainWindow()
        {
            InitializeComponent();
            CamerasItems = new ObservableCollection<Camera>();
            m_sdkEngine = new Engine();
            m_sdkEngine.LoginManager.LoggedOn += M_sdkEngine_LoggedOn;
            m_sdkEngine.LoginManager.RequestDirectoryCertificateValidation += M_sdkEngine_RequestDirectoryCertificateValidation;
            m_sdkEngine.LoginManager.LogonFailed += LoginManager_LogonFailed;
            player.StreamingConnectionStatusChanged += Player_StreamingConnectionStatusChanged;
            player2.StreamingConnectionStatusChanged += Player2_StreamingConnectionStatusChanged;
            
        }

        private void Player2_StreamingConnectionStatusChanged(object sender, Genetec.Sdk.Media.StreamingConnectionStatusChangedEventArgs e)
        {
            txtStatus.Text += "\nPlayer 2 " + player2.StreamingConnectionStatus.ToString();
        }

        private void Player_StreamingConnectionStatusChanged(object sender, Genetec.Sdk.Media.StreamingConnectionStatusChangedEventArgs e)
        {
            txtStatus.Text += "\nPlayer 1 " + player.StreamingConnectionStatus.ToString();
        }

        private void LoginManager_LogonFailed(object sender, LogonFailedEventArgs e)
        {
            MessageBox.Show(e.FormattedErrorMessage);
        }

        private void M_sdkEngine_RequestDirectoryCertificateValidation(object sender, Genetec.Sdk.EventsArgs.DirectoryCertificateValidationEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("The identity of the Directory server cannot be verified. \n" +
                            "The certificate is not from a trusted certifying authority. \n" +
                            "Do you trust this server?", "Secure Communication", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                e.AcceptDirectory = true;
            }
        }

        private void M_sdkEngine_LoggedOn(object sender, LoggedOnEventArgs e)
        {

            //player.Background = new SolidColorBrush(Colors.Black);
            IsSdkEngineConnected = m_sdkEngine.LoginManager.IsConnected;
            txtStatus.Text += "\n Login succeeded";
            player.HardwareAccelerationEnabled = true;
                        player2.HardwareAccelerationEnabled = true;
            var query = m_sdkEngine.ReportManager.CreateReportQuery(ReportType.EntityConfiguration) as EntityConfigurationQuery;
            query.EntityTypeFilter.Add(EntityType.Camera);

            QueryCompletedEventArgs result = query.Query();
            if (result.Success)
            {
                foreach (DataRow dr in result.Data.Rows)
                {
                    Camera camera = m_sdkEngine.GetEntity((Guid)dr[0]) as Camera;
                    if ((camera != null))// &&  (camera.IsOnline) && (!camera.IsGhostCamera) && (!camera.IsSequence))
                    {                        
                        listCam.Items.Add(camera);
                        listCam2.Items.Add(camera);
                    }
                }
            }
            else
            {
                txtStatus.Text += "The query has failed";
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {

            txtStatus.Text += "\n Login started";
            m_sdkEngine.LoginManager.BeginLogOn("192.168.1.14", "admin", "!Password01");
                }

        private void listCam_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            player.Stop();
            player.Initialize(m_sdkEngine, ((Camera)listCam.SelectedItem).Guid);
            player.PlayLive();

            txtStatus.Text += "\n Player started";

            m_pcm = new Genetec.Sdk.Media.Ptz.PtzCoordinatesManager();
            m_pcm.Initialize(m_sdkEngine, ((Camera)listCam.SelectedItem).Guid);

        }

        private void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            //player.PlayLive();
            //player.PlayArchive(DateTime.Now - TimeSpan.FromMinutes(10));
            txtStatus.Text += "\n" + player.StreamingConnectionStatus.ToString();
        }

        private void txtStatus_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void listCam2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            player2.Stop();
            player2.Initialize(m_sdkEngine, ((Camera)listCam2.SelectedItem).Guid);
            player2.PlayLive();
        }

        private void player_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            m_pcm.ControlPtz(PtzCommandType.StartZoom, (e.Delta > 0) ? 0 : 1, 70);
            m_pcm.ControlPtz(PtzCommandType.StopZoom, 0, 0);
        }

        private void player_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            m_pcm.ControlPtz(PtzCommandType.StartPanTilt, -20, 0);
            m_pcm.ControlPtz(PtzCommandType.StopZoom, 0, 0);
        }

        private void player_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

            m_pcm.ControlPtz(PtzCommandType.StartPanTilt, 20, 0);
        }

        private void player_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            m_pcm.ControlPtz(PtzCommandType.StopPanTilt, 0, 0);
        }
    }
}
