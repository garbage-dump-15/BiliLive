using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;

namespace TrayLive
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		private TaskbarIcon _taskbarIcon;

		private HttpListener _httpListener;

		private static bool _exit;

		private sealed class ExitCommand : ICommand
		{
			public bool CanExecute(object parameter)
			{
				return true;
			}

			public void Execute(object parameter)
			{
				_exit = true;
				Current.Shutdown();
				Thread.CurrentThread.Abort();
			}

			public event EventHandler CanExecuteChanged;
		}

		private void InitMenu()
		{
			var menu = new ContextMenu();
			menu.Items.Add(new MenuItem
			{
				Header = "退出程序",
				Command = new ExitCommand()
			});
			_taskbarIcon.ContextMenu = menu;
		}

		private void InitApplication(object sender, StartupEventArgs startupEventArgs)
		{
			_taskbarIcon = (TaskbarIcon) FindResource("MyNotifyIcon");
			if (_taskbarIcon != null) _taskbarIcon.Icon = Icon.ExtractAssociatedIcon("favicon.ico");

			InitMenu();
			StartServer();
		}

		private void StartServer()
		{
			_httpListener = new HttpListener {AuthenticationSchemes = AuthenticationSchemes.Anonymous};
			_httpListener.Prefixes.Add("http://localhost:7046/");
			_httpListener.Start();

			new Thread(new ThreadStart(delegate
			{
				while (!_exit)
				{
					try
					{
						var httpListenerContext = _httpListener.GetContext();
						var path = httpListenerContext.Request.Url.LocalPath;
						httpListenerContext.Response.StatusCode = 200;
						var writer = new StreamWriter(httpListenerContext.Response.OutputStream);
						switch (path)
						{
							case "/":
							case "/index.html":
							{
								httpListenerContext.Response.ContentType = "text/html";
								var text = File.ReadAllText("index.html");
								var sess = httpListenerContext.Request.QueryString["type"] ?? "online";
								text = text.Replace("do_not_change_this", sess);
								writer.WriteAsync(text).GetAwaiter().OnCompleted(delegate { writer.Dispose(); });
								break;
							}
							case "/refresh":
								httpListenerContext.Response.ContentType = "text/json";
								var uid = httpListenerContext.Request.QueryString["uid"];
								var sessVar = httpListenerContext.Request.QueryString["sess"] ?? "online";
								if (uint.TryParse(uid, out var id))
								{
									new DataService(id, sessVar).ReadLiveAsync(delegate(uint data)
									{
										writer.WriteAsync("{\"online\": " + data + "}").GetAwaiter()
											.OnCompleted(delegate { writer.Dispose(); });
									});
									break;
								}

								writer.WriteAsync("{\"error\": \"params error\"}").GetAwaiter()
									.OnCompleted(delegate { writer.Dispose(); });

								break;
						}
					}
					catch (Exception)
					{
						// ignored
					}
				}
			})).Start();
		}
	}
}