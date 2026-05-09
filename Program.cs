using System.ServiceProcess;

namespace FileChecksum
{
    /// <summary>
    /// Entry point for the File Checksum Windows service.
    /// Instantiates and runs the service via the Windows Service Control Manager.
    /// </summary>
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// Starts the Windows service(s).
        /// </summary>
        [System.STAThread]
        static void Main()
        {
            ServiceBase[] servicesToRun;
            servicesToRun = new ServiceBase[]
            {
                new FileChecksumService()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
