using System.Windows;
using System.Windows.Input;

namespace RevitAKSOpen {
    /// <summary>
    /// Interaction logic for FormMsg.xaml
    /// </summary>
    public partial class FormMsgWPF : Window {
        string _purpose;
        //DispatcherTimer timeOut = new DispatcherTimer();
        public FormMsgWPF() {
            InitializeComponent();
            this.Top = Properties.Settings.Default.AKSOpen_Top;
            this.Left = Properties.Settings.Default.AKSOpen_Left;
            //timeOut.Tick += new EventHandler(timeOut_Tick);
            //timeOut.Interval = new TimeSpan(0, 0, 3);
            //timeOut.Start();
        }

        //private void timeOut_Tick(object sender, EventArgs e) {
        //    timeOut.Stop();
        //    Hide();
        //}

        public void SetMsg(string _msg, string purpose, string _bot = "") {
            _purpose = purpose;
            this.MsgTextBlockMainMsg.Text = _msg;
            this.MsgLabelTop.Content = purpose;
            if (_bot != "") {
                this.MsgLabelBot.Content = _bot;
            }
        }
     
        public void DragWindow(object sender, MouseButtonEventArgs args) {
            // Watch out. Fatal error if not primary button!
            if (args.LeftButton == MouseButtonState.Pressed) { DragMove(); } 
        }
    }
}
