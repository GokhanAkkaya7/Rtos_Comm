// FILE: Program.cs (Updated with Global Exception Handling)

using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Rtos_Comm
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // --- ADD GLOBAL EXCEPTION HANDLING ---
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            // --- END OF ADDITION ---

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        // --- ADD THESE TWO METHODS ---

        // This handles exceptions from the UI thread (e.g., button clicks, form loading)
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ShowExceptionDetails(e.Exception);
        }

        // This handles exceptions from non-UI threads (e.g., background tasks)
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowExceptionDetails(e.ExceptionObject as Exception);
        }

        // Helper method to display and log the exception
        private static void ShowExceptionDetails(Exception ex)
        {
            if (ex == null) return;

            string errorMessage = $"An unhandled exception occurred:\n\n" +
                                  $"Message: {ex.Message}\n\n" +
                                  $"Stack Trace:\n{ex.StackTrace}";

            // Log the error to a file
            try
            {
                // We use a different log file to avoid conflicts
                string errorLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fatal_error_log.txt");
                File.WriteAllText(errorLogPath, errorMessage);
            }
            catch { /* If logging fails, we can't do much more */ }

            // Show the error to the user
            MessageBox.Show(errorMessage, "Fatal Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            // It's often best to exit the application after a fatal error
            Environment.Exit(1);
        }
    }
}