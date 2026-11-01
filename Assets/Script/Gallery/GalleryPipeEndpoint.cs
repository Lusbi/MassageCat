using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;

namespace MassageCat.Gallery
{
    internal sealed class GalleryPipeEndpoint : IDisposable
    {
        private readonly bool m_isServer;
        private readonly ConcurrentQueue<GalleryEnvelope> m_receivedMessages = new ConcurrentQueue<GalleryEnvelope>();
        private readonly ConcurrentQueue<string> m_outgoingMessages = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<bool> m_connectionChanges = new ConcurrentQueue<bool>();
        private readonly AutoResetEvent m_writerSignal = new AutoResetEvent(false);
        private readonly object m_pipeLock = new object();

        private Thread m_connectionThread;
        private PipeStream m_activePipe;
        private volatile bool m_running;
        private volatile bool m_connected;
        private int m_disposed;

        internal bool IsConnected => m_connected;

        internal GalleryPipeEndpoint(bool isServer)
        {
            m_isServer = isServer;
        }

        internal void Start()
        {
            if (m_running)
                return;

            m_running = true;
            m_connectionThread = new Thread(ConnectionLoop)
            {
                IsBackground = true,
                Name = m_isServer ? "MassageCat Gallery Pipe Server" : "MassageCat Gallery Pipe Client",
            };
            m_connectionThread.Start();
        }

        internal void Send<T>(string type, T payload)
        {
            if (m_running == false)
                return;

            GalleryEnvelope envelope = new GalleryEnvelope
            {
                type = type,
                payload = payload == null ? string.Empty : JsonConvert.SerializeObject(payload),
            };
            m_outgoingMessages.Enqueue(JsonConvert.SerializeObject(envelope));
            m_writerSignal.Set();
        }

        internal bool TryDequeueMessage(out GalleryEnvelope envelope)
        {
            return m_receivedMessages.TryDequeue(out envelope);
        }

        internal bool TryDequeueConnectionChange(out bool connected)
        {
            return m_connectionChanges.TryDequeue(out connected);
        }

        internal static bool TryReadPayload<T>(GalleryEnvelope envelope, out T payload) where T : class
        {
            payload = default;
            if (envelope == null || string.IsNullOrEmpty(envelope.payload))
                return false;

            try
            {
                payload = JsonConvert.DeserializeObject<T>(envelope.payload);
                return payload != null;
            }
            catch (JsonException exception)
            {
                Debug.LogWarning($"圖鑑 IPC 訊息格式錯誤：{exception.Message}");
                return false;
            }
        }

        private void ConnectionLoop()
        {
            while (m_running)
            {
                PipeStream pipe = null;
                try
                {
                    pipe = m_isServer ? CreateServerPipe() : CreateClientPipe();
                    if (pipe == null || m_running == false)
                    {
                        pipe?.Dispose();
                        continue;
                    }

                    lock (m_pipeLock)
                    {
                        m_activePipe = pipe;
                    }

                    SetConnected(true);
                    RunConnected(pipe);
                }
                catch (Exception exception) when (exception is IOException
                                                   || exception is TimeoutException
                                                   || exception is ObjectDisposedException)
                {
                    if (m_running)
                        Thread.Sleep(250);
                }
                finally
                {
                    SetConnected(false);
                    lock (m_pipeLock)
                    {
                        if (ReferenceEquals(m_activePipe, pipe))
                            m_activePipe = null;
                    }
                    pipe?.Dispose();
                }

                if (m_running && m_isServer == false)
                    Thread.Sleep(750);
            }
        }

        private NamedPipeServerStream CreateServerPipe()
        {
            NamedPipeServerStream pipe = new NamedPipeServerStream(
                GalleryProtocol.PipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);
            lock (m_pipeLock)
            {
                m_activePipe = pipe;
            }

            try
            {
                pipe.WaitForConnection();
                return pipe;
            }
            catch
            {
                lock (m_pipeLock)
                {
                    if (ReferenceEquals(m_activePipe, pipe))
                        m_activePipe = null;
                }
                pipe.Dispose();
                throw;
            }
        }

        private static NamedPipeClientStream CreateClientPipe()
        {
            NamedPipeClientStream pipe = new NamedPipeClientStream(
                ".",
                GalleryProtocol.PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);
            pipe.Connect(1000);
            return pipe;
        }

        private void RunConnected(PipeStream pipe)
        {
            using StreamReader reader = new StreamReader(pipe);
            using StreamWriter writer = new StreamWriter(pipe) { AutoFlush = true };

            Thread writerThread = new Thread(() => WriterLoop(writer, pipe))
            {
                IsBackground = true,
                Name = m_isServer ? "MassageCat Gallery Pipe Server Writer" : "MassageCat Gallery Pipe Client Writer",
            };
            writerThread.Start();

            try
            {
                while (m_running && pipe.IsConnected)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                        break;

                    GalleryEnvelope envelope = JsonConvert.DeserializeObject<GalleryEnvelope>(line);
                    if (envelope == null || envelope.protocolVersion != GalleryProtocol.Version)
                        continue;

                    m_receivedMessages.Enqueue(envelope);
                }
            }
            finally
            {
                CloseActivePipe();
                m_writerSignal.Set();
                writerThread.Join(500);
            }
        }

        private void WriterLoop(StreamWriter writer, PipeStream pipe)
        {
            try
            {
                while (m_running && pipe.IsConnected)
                {
                    if (m_outgoingMessages.TryDequeue(out string message))
                    {
                        writer.WriteLine(message);
                        continue;
                    }

                    m_writerSignal.WaitOne(250);
                }
            }
            catch (Exception exception) when (exception is IOException || exception is ObjectDisposedException)
            {
                CloseActivePipe();
            }
        }

        private void SetConnected(bool connected)
        {
            if (m_connected == connected)
                return;

            m_connected = connected;
            m_connectionChanges.Enqueue(connected);
        }

        private void CloseActivePipe()
        {
            lock (m_pipeLock)
            {
                try
                {
                    m_activePipe?.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref m_disposed, 1) != 0)
                return;

            m_running = false;
            m_writerSignal.Set();
            CloseActivePipe();
            if (m_connectionThread != null && m_connectionThread.IsAlive)
                m_connectionThread.Join(1000);
            m_writerSignal.Dispose();
        }
    }
}
