using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace YARG.Server {
	public partial class Host : MonoBehaviour {
		private List<Thread> threads = new();
		private int connectionCount = 0;
		private TcpListener server;

		private void Start() {
			server = new TcpListener(IPAddress.Any, 6145);
			server.Start();

			Log("Opened server on localhost:6145.");

			server.BeginAcceptTcpClient(AcceptTcpClient, server);

			Log("Waiting for clients...");
		}

		private void AcceptTcpClient(IAsyncResult result) {
			TcpClient client = server.EndAcceptTcpClient(result);

			if (connectionCount >= 5) {
				Log("<color=red>Max connection count reached (5).</color>");
				return;
			}

			// Create a thread for this connection
			var thread = new Thread(() => ServerThread(client));
			thread.Start();
			threads.Add(thread);
			connectionCount++;

			server.BeginAcceptTcpClient(AcceptTcpClient, server);
		}

		private void ServerThread(TcpClient client) {
			Log("<color=green>Client accepted!</color>");

			var stream = client.GetStream();
			while (true) {
				if (stream.DataAvailable) {
					// Get data from client
					byte[] bytes = new byte[1024];
					int size = stream.Read(bytes, 0, bytes.Length);

					// Get request
					var str = System.Text.Encoding.ASCII.GetString(bytes, 0, size);
					Log($"Received: `{str}`.");

					// Do something
					if (str == "End") {
						break;
					} else if (str == "ReqCache") {
						SendFile(stream, Game.CACHE_FILE);
					}
				} else {
					// Prevent CPU burn
					Thread.Sleep(100);
				}
			}

			Log("<color=yellow>Client disconnected.</color>");
			connectionCount--;
		}

		private void SendFile(NetworkStream stream, FileInfo file) {
			Log($"Sending file `{file.FullName}`...");
			using var fs = file.OpenRead();

			// Send file size
			stream.Write(BitConverter.GetBytes(fs.Length));

			// Send file itself
			fs.CopyTo(stream);
		}

		private void OnDestroy() {
			foreach (var thread in threads) {
				thread.Abort();
			}
			server.Stop();
		}
	}
}