using System;

using DACarter.ClientServer;

namespace PopNService {

    class ServiceStarter {

        [STAThread]
        static void Main(string[] args) {

            if (args.Length > 0 && args[0].ToLower() == "-noservice") {
                // when running with this param from debugger, 
                //  do not start service, just call start method.
				Console.WriteLine("Starting PopNService as Console Mode program - not a service.");
                PopNService service = new PopNService();
				// DAC removed following because PublicOnStart is called from CreateComm called by POPNForm_Load
                // -- so basically in -noService mode, this program does nothing;
                //    PopNService is created by PopN
                //service.PublicOnStart();
                System.Threading.Thread.Sleep(20000);
                // Put a breakpoint on the following line to always catch
                // your service when it has finished its work
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
                //System.Threading.Thread.Sleep(2000);
            }
            else {
                // install and start the service
                //
                PopNService.ServiceOfficialName = "POPNService";
                PopNService.ServiceDescription = "NOAA/ESRL/PSD2 PopN Main Service.";
                PopNService.ServiceStartType = System.ServiceProcess.ServiceStartMode.Automatic;
                PopNService.AssemblyName = System.Reflection.Assembly.GetExecutingAssembly().FullName;
                ServiceHelper<PopNService> helper = new ServiceHelper<PopNService>(args);
                //MessageBox.Show("ServiceHelper.Run()...");
                helper.Run();
            }

            return;
        }
    }
}
