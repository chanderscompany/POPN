using System;
using System.ServiceProcess;
using System.Configuration.Install;
using System.ComponentModel;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

using Microsoft.Win32;

using DACarter.ClientServer;
using DACarter.Utilities;
using DACarter.PopUtilities;
using POPCommunication;

namespace PopNService {

	[RunInstaller(true)]
	public class ProjectInstaller : Installer {

		public ProjectInstaller() {
			DacProjectInstaller dacInstaller = new DacProjectInstaller();
            dacInstaller.Install(typeof(PopNService), Installers);
		}
        
        protected override void OnCommitted(IDictionary savedState) {
            // make sure to notify all subscribers to the event
            base.OnCommitted(savedState);

            // The following code sets the flag to allow desktop interaction for the service
            /*
            using (RegistryKey ckey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\POPNService", true)) {
                if (ckey != null) {
                    if (ckey.GetValue("Type") != null) {
                        ckey.SetValue("Type", (((int)ckey.GetValue("Type")) | 256));
                    }
                }
            }
            */
        }

    }

    public class PopNService : DacServiceBase {

        //public System.Diagnostics.EventLog EventLogPopNService;
        private PopNDwellWorker _dwellWorker;
        //public string Version;

        private BackgroundWorker _worker = null;
        //private POPCommServerHost _host;
		//private POPCommServer<IStatusUpdateHandler> _commServer;
        private POPCommunicator communicator;

		private bool _startedAsService;

        private POPNEventLogWriter _eventLogWriter;

		public PopNService() {
            //MessageBox.Show("PopNService constructor");
            //TextFile.WriteLineToFile("DebugStatus2.txt", "PopNService constructor " + DateTime.Now.ToString(), true);
            _startedAsService = false;
            _eventLogWriter = new POPNEventLogWriter();
            InitializeComponent();
            CanStop = true;
            CanShutdown = true;
        }

	    protected override void OnStart(string[] args) {
            //TextFile.WriteLineToFile("DebugStatus2.txt", "In OnStart() " + DateTime.Now.ToString(), true);
			_startedAsService = true;
            _eventLogWriter.WriteEntry("Service OnStart()", 1);
            //Thread.Sleep(10000);
            //TextFile.WriteLineToFile("DebugStatus2.txt", "CanStop =  " + CanStop.ToString(), true);
            //TextFile.WriteLineToFile("DebugStatus2.txt", "CanShutdown =  " + CanShutdown.ToString(), true);
            PublicOnStart();
        }

        protected override void OnShutdown() {
            //TextFile.WriteLineToFile("DebugStatus2.txt", "In OnShutdown1 " + DateTime.Now.ToString(), true);
            _worker.CancelAsync();
            Thread.Sleep(3000);
            //TextFile.WriteLineToFile("DebugStatus2.txt", "In OnShutdown2 " + DateTime.Now.ToString(), true);
            base.OnShutdown();
        }

        private void InitializeComponent() {
            //this.EventLogPopNService = new System.Diagnostics.EventLog();
            //((System.ComponentModel.ISupportInitialize)(this.eventLogPopNService)).BeginInit();
            //((System.ComponentModel.ISupportInitialize)(this.eventLogPopNService)).EndInit();
        }

        /// <summary>
        /// stopping the service
        /// </summary>
        protected override void OnStop() {
            //TextFile.WriteLineToFile("DebugStatus2.txt", "In OnStop " + DateTime.Now.ToString(), true);
            string logfolder = PopNStateFile.GetLogFolder();
            DacLogger.WriteEntry("PopN Service OnStop()", logfolder);
            //Cancel = true;
            PublicOnStop();
        }

        public void PublicOnStop() {
            _eventLogWriter.WriteEntry("Service OnStop()", 500);
            _worker.CancelAsync();
            string logfolder = PopNStateFile.GetLogFolder();
            DacLogger.WriteEntry("Cancelling worker thread...", logfolder);
            Thread.Sleep(1000);
        }

        public void PublicOnStart(bool runningAsService = true) {
            // call this directly to debug OnStart()

            //TextFile.WriteLineToFile("DebugStatus2.txt", "In OnPublicStart() " + DateTime.Now.ToString(), true);
            _eventLogWriter.WriteEntry("Service PublicOnStart()", 10);
            //TextFile.WriteLineToFile("DebugStatus2.txt", "after writeEntry " + DateTime.Now.ToString(), true);
            // get last logfolder
            string logfolder = PopNStateFile.GetLogFolder();
            DacLogger.WriteEntry("PopN Service OnStart()", logfolder);
            //Console.WriteLine("PopNService: PublicOnStart() method");


            // create communicator object to communicate with client (UI control panel)
            communicator = new POPCommunicator(POPCommunicator.POPCommType.Server);
            //eventLogPopNService.WriteEntry("After new POPCommunicator.", System.Diagnostics.EventLogEntryType.Information, 20);
            //EventLog.WriteEntry("After new PopCommunicator", System.Diagnostics.EventLogEntryType.Information, 111);
            //_commServer = communicator.TheCommServer;
            //Console.WriteLine("PopNService: communicator created.");
            Thread.Sleep(1000);
            communicator.UpdateStatus(new PopStatusMessage("PopNService: communicator created"));

			_dwellWorker = new PopNDwellWorker();
			_dwellWorker.Communicator = communicator;

            // create worker thread to do POPN work
            _worker = new BackgroundWorker();
            _worker.DoWork += _dwellWorker.DoWork;
            _worker.WorkerReportsProgress = true;

            _worker.WorkerSupportsCancellation = true;
            _worker.ProgressChanged += _dwellWorker.ProgressChanged;
            _worker.RunWorkerCompleted += _dwellWorker.WorkerCompleted;

            _dwellWorker.WorkerThread = _worker;
            _dwellWorker.EventLogWriter = _eventLogWriter;
            _dwellWorker.RunningAsService = runningAsService;

			_worker.RunWorkerAsync();
            //eventLogPopNService.WriteEntry("After RunWorkerAsync().", System.Diagnostics.EventLogEntryType.Information, 50);
            Thread.Sleep(1000);

			communicator.CommandReceived += _dwellWorker.CommandReceived;
            communicator.ParamsRequested += _dwellWorker.ParamsRequested;
            communicator.PowerMeterRequested += _dwellWorker.PowerMeterRequested;
            communicator.NumDaqDevicesRequested += _dwellWorker.NumDaqDevicesRequested;
            communicator.SamplesRequested += _dwellWorker.SamplesRequested;
            communicator.AScanRequested += _dwellWorker.AScanRequested;
            communicator.SpectrumRequested += _dwellWorker.SpectrumRequested;
            communicator.CrossCorrRequested += _dwellWorker.CrossCorrRequested;
            communicator.CrossCorrRatioRequested += _dwellWorker.CrossCorrRatioRequested;
            communicator.CrossCorrProfileRequested += _dwellWorker.CrossCorrProfileRequested;
            communicator.DopplerTSRequested += _dwellWorker.DopplerTSRequested;
            communicator.CltrWvltRequested += _dwellWorker.CltrWvltRequested;
            communicator.MomentsRequested += _dwellWorker.MomentsRequested;

			if (_startedAsService) {
                string message = "PopNService started as a service.";
				communicator.UpdateStatus(new PopStatusMessage(message));
                DacLogger.WriteEntry(message, logfolder);
            }
			else {
				communicator.UpdateStatus(new PopStatusMessage("PopNService started as a console mode program."));
                DacLogger.WriteEntry("PopNService code run not as service.", logfolder);
            }
            //eventLogPopNService.WriteEntry("Leaving PublicOnStart().", System.Diagnostics.EventLogEntryType.Information, 100);

		}

	}  // end class PopNService

    public class POPNEventLogWriter {
        private bool _enabled;
        private System.Diagnostics.EventLog _eventLogPOPNService;
        string SourceName = "POPN";

        public POPNEventLogWriter() {
            _enabled = true;
            try {
                System.Diagnostics.EventLog.Delete("POPNService Log");
                System.Diagnostics.EventLog.Delete("POPN-Service Log");
                System.Diagnostics.EventLog.Delete("PopN Service Log");
                if (!System.Diagnostics.EventLog.SourceExists(SourceName)) {
                    System.Diagnostics.EventLog.CreateEventSource(SourceName, "POPN Log");
                    //this.WriteEntry("Initial Entry", 0);
                }
                _eventLogPOPNService = new System.Diagnostics.EventLog();
                _eventLogPOPNService.Source = SourceName;
                _eventLogPOPNService.ModifyOverflowPolicy(System.Diagnostics.OverflowAction.OverwriteAsNeeded, 100);
                string log = _eventLogPOPNService.Log;
                string version = Application.ProductVersion;
                _eventLogPOPNService.WriteEntry("PopNService Constructor " + version, System.Diagnostics.EventLogEntryType.Information, 0);
            }
            catch (Exception e1){
                // probably here due to not having administrator rights
                _enabled = false;
            }
        }

        public void WriteEntry(string entry, int id) {
            if (_enabled) {
                try {
                    _eventLogPOPNService.WriteEntry(entry, System.Diagnostics.EventLogEntryType.Information, id);
                }
                catch {
                    _enabled = false;
                }
            }
        }
    }


}  // end namespace
