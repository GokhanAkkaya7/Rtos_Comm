using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rtos_Comm.application.PipeConnection
{
    public class PipeClient
    {
        private NamedPipeClientStream _pipeClient;
        private readonly string _pipeName;

        public bool IsConnected => _pipeClient != null && _pipeClient.IsConnected;

        public PipeClient(string pipeName)
        {
            _pipeName = pipeName;
        }

        public async Task<bool> ConnectAsync(int timeoutMs = 3000)
        {
            if (IsConnected) 
                return true;

            try
            {
                _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                await _pipeClient.ConnectAsync(timeoutMs);
                return IsConnected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PipeClient] Connection failed: {ex.Message}");
                Disconnect(); 
                return false;
            }
        }

        public async Task<string> ReceiveMessageAsync()
        {
            if (!IsConnected)
            {
                return null;
            }

            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead = await _pipeClient.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    return null;
                }

                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received message: {receivedMessage}");
                return receivedMessage;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
            catch (IOException ex)
            {
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public async Task<bool> SendMessageAsync(string sent_message)
        {
            if (!IsConnected)
            {
                Console.WriteLine("Not connected.");
                return false;
            }

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(sent_message);
                await _pipeClient.WriteAsync(buffer, 0, buffer.Length);
                //await _pipeClient.FlushAsync();
                return true;
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Pipe is closed or disconnected.");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IO error while sending message: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while sending message: {ex.Message}");
            }

            return false;
        }
        public void Disconnect()
        {
            _pipeClient?.Close(); 
            _pipeClient?.Dispose();
            _pipeClient = null;
        }
    }
}
