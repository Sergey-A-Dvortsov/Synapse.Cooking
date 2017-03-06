using Ecng.Serialization;
using Ecng.Xaml;
using StockSharp.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace Synapse.MoexLoader
{
    public class BaseViewModel : BaseLogReceiver, INotifyPropertyChanged
    {

        #region INotifyPropertyChanged releases

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }

    public class MainViewModel : BaseViewModel
    {
        private Loader _loader;
        public MainViewModel()
        {
            _loader = Loader.GetInstance();
            StartCommand = new DelegateCommand(OnStart, CanStart);
        }

        #region properties

        private double _progressValue;
        public double ProgressValue
        {
            get { return _progressValue; }

            set
            {
                _progressValue = value;
                NotifyPropertyChanged();
            }
        }
        public eProcessState State
        {
            get { return _loader.State; }
            set
            {
                _loader.State = value;
                NotifyPropertyChanged();
            }
        }

        private GeneralViewModel _generalViewModel;
        public GeneralViewModel GeneralViewModel
        {
            get
            {
                if (_generalViewModel == null)
                    GeneralViewModel = new GeneralViewModel();
                return _generalViewModel;
            }
            private set
            {
                _generalViewModel = value;
                NotifyPropertyChanged();
            } 
        }

        private InterestViewModel _interestViewModel;
        public InterestViewModel InterestViewModel
        {
            get
            {
                if (_interestViewModel == null)
                    InterestViewModel = new InterestViewModel();
                return _interestViewModel;
            }

            private set
            {
                _interestViewModel = value;
                NotifyPropertyChanged();
            }
        }

        private IndexesViewModel _indexesViewModel;
        public IndexesViewModel IndexesViewModel
        {
            get
            {
                if (_indexesViewModel == null)
                    IndexesViewModel = new IndexesViewModel();

                return _indexesViewModel;
            }

            private set
            {
                _indexesViewModel = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        public DelegateCommand StartCommand { private set; get; }

        private void OnStart(object obj)
        {
            if (State == eProcessState.Stopped)
                _loader.Start();
            else if (State == eProcessState.Started)
                _loader.Stop();

            NotifyPropertyChanged("State");
        }
        private bool CanStart(object obj)
        {
            return !string.IsNullOrWhiteSpace(_loader.DataPath) && (_loader.Sources.Any(s => s.OnOff));
        }

        public void OnLoaded(object sender, RoutedEventArgs e)
        {
            SettingsStorage storage = null;
            if (File.Exists("settings.xml"))
                storage = new XmlSerializer<SettingsStorage>().Deserialize("settings.xml");
            _loader.Load(storage);
        }

        public void OnClosing(object sender, CancelEventArgs e)
        {
            var storage = _loader.Save();
            new XmlSerializer<SettingsStorage>().Serialize(storage, "settings.xml");
        }

    }

    public class GeneralViewModel : BaseViewModel
    {
        private Loader _loader;
        public GeneralViewModel()
        {
            _loader = Loader.GetInstance();
            DataPathCommand = new DelegateCommand(SetDataPath, CanSetDataPath);
            _loader.SettingsLoaded += () => NotifyPropertyChanged("DataPath");
        }
        public string DataPath
        {
            get { return _loader.DataPath; }
            set
            {
                _loader.DataPath = value;
                NotifyPropertyChanged();
            }
        }

        public DelegateCommand DataPathCommand { private set; get; }

        private void SetDataPath(object obj)
        {
            var fbd = new FolderBrowserDialog();
            if (!string.IsNullOrWhiteSpace(DataPath))
                fbd.SelectedPath = DataPath;
            fbd.ShowNewFolderButton = true;
            fbd.Description = "Выбирете корневую папку хранилища данных";

            if (fbd.ShowDialog() == DialogResult.OK)
                DataPath = fbd.SelectedPath;

        }

        private bool CanSetDataPath(object obj)
        {
            return true;
        }

    }

    public abstract class SourceViewModel : BaseViewModel
    {
        protected Loader _loader;
        protected BaseSourceLoader _settings;
        public SourceViewModel()
        {
            _loader = Loader.GetInstance();
        }

        public bool OnOff
        {
            get { return _settings.OnOff; }
            set
            {
                _settings.OnOff = value;
                NotifyPropertyChanged();
            }
        }

        private eProcessState _state = eProcessState.Stopped;
        public eProcessState State
        {
            get { return _state; }
            set
            {
                _state = value;
                NotifyPropertyChanged();
            }
        }

        public decimal ProgressBarValue
        {
            get { return _settings.ProgressBarValue; }
            set { var t = value; }
        }

        public DateTime StartDate
        {
            get { return _settings.StartDate; }
            set
            {
                _settings.StartDate = value;
                NotifyPropertyChanged();
            }
        }

        public DateTime EndDate
        {
            get { return _settings.EndDate; }
            set
            {
                _settings.EndDate = value;
                NotifyPropertyChanged();
            }
        }

        public DateTime LoadTo
        {
            get { return _settings.LoadTo; }
            set
            {
                _settings.LoadTo = value;
                NotifyPropertyChanged();
            }
        }

        public void Notify()
        {
            NotifyPropertyChanged("OnOff");
            NotifyPropertyChanged("StartDate");
            NotifyPropertyChanged("EndDate");
            NotifyPropertyChanged("LoadTo");
            NotifyPropertyChanged("ProgressBarValue");
        }

    }

    public class InterestViewModel : SourceViewModel
    {
        public InterestViewModel() : base()
        {
            _settings = _loader.Sources[0];
            _loader.SettingsLoaded += () =>
            {
                Notify();
                NotifyPropertyChanged("ContractTypes");
                NotifyPropertyChanged("SeparateOptionsFile");
            };
        }

        public eContractType ContractTypes
        {
            get { return ((InterestSourceLoader)_settings).ContractTypes; }
            set { ((InterestSourceLoader)_settings).ContractTypes = value; }
        }

        public bool SeparateOptionsFile
        {
            get { return ((InterestSourceLoader)_settings).SeparateOptionsFile; }
            set { ((InterestSourceLoader)_settings).SeparateOptionsFile = value; }
        }
    }

    public class IndexesViewModel : SourceViewModel
    {
        public IndexesViewModel() : base()
        {
            _settings = _loader.Sources[1];
        }

    }

}