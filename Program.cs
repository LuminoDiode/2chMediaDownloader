using System;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Net.Cache;
using System.Runtime.InteropServices;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace _2chMediaDownloader
{
	class Program
	{
		static DownloaderUI UI;

		static readonly string[] MediaFileExtensions = "mp4;webm;jpeg;jpg;png;gif;bmp;webp".Split(";");

		static readonly Regex ThreadUriRegex =
			new Regex(@"2ch[.]hk/(.*?)/res/([0-9]*)[.]html");
		static readonly Regex MediaRegex =
			new Regex($"(data-src|src|href)=\"/(.*?)/([0-9]*)[.]({string.Join('|', MediaFileExtensions)})\"", RegexOptions.IgnorePatternWhitespace);

		static readonly DirectoryInfo SaveDirectoryInfo =
			new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\2chDownloads");

		[DllImport("kernel32")] static extern bool AllocConsole();
		[STAThread]
		public static void Main()
		{
			AllocConsole();
			Trace.Listeners.Add(new ConsoleTraceListener());
			Trace.WriteLine("Console initialized and listening Trace class.");

			Trace.WriteLine("Creating UI instance...");
			UI = new DownloaderUI();

			Trace.WriteLine("Binding download button...");
			UI.DownloadButtonClick += DownloadButton_Click;

			Trace.WriteLine("Running UI...");
			Application.Run((Form)UI);
		}

		public static void DownloadButton_Click(object sender, EventArgs ea)
		{
			Trace.WriteLine("Download button clicked.");

			Trace.WriteLine("Disabling UI...");
			((Form)(UI)).Enabled = false;
			WebClient DownloaderWC = new WebClient();
			HtmlDocument CurrentPage = new HtmlDocument();
			IEnumerable<Uri> MediaUris;
			DirectoryInfo CurrentDirectory;

			Trace.WriteLine("Beginning recognizing links and download...");

			foreach (Uri ThreadUriMatch in GetThreadURIsFromText(UI.UrisInput))
			{
				Trace.WriteLine($"Thread uri \"{ThreadUriMatch}\" recognized.");

				try
				{
					CurrentPage.LoadHtml(DownloaderWC.DownloadString(ThreadUriMatch));
				}
				catch (WebException we)
				{
					Trace.WriteLine(
						$"Web error while trying to access thread page at \"{ThreadUriMatch}\": \"{we.Message}\".");
					continue;
				}
				catch (Exception e)
				{
					Trace.WriteLine(
						$"Unexpected error while trying to access thread page at \"{ThreadUriMatch}\": \"{e.Message}\".");
					continue;
				}

				Trace.WriteLine($"Thread page at \"{ThreadUriMatch}\" downloaded.");

				Trace.WriteLine($"Parsing page at \"{ThreadUriMatch}\" to media links...");
				try
				{
					MediaUris = GetMediaURIsFromThreadHtmlPage(CurrentPage);
				}
				catch (Exception e)
				{
					Trace.WriteLine(
						$"Unexpected error while parsing page at \"{ThreadUriMatch}\" to media links: \"{e.Message}\".");
					continue;
				}

				Trace.WriteLine($"Total media links found at \"{ThreadUriMatch}\": {MediaUris.Count()}.");
				Trace.WriteLine(
					$"Media links found at \"{ThreadUriMatch}\": \"{string.Join(';', MediaUris.Select(x => '"' + x.AbsoluteUri + '"'))}\".");

				Trace.WriteLine($"Beginning download files from thread at \"{ThreadUriMatch}\".");

				if (UI.SubFoldersRequired)
					CurrentDirectory = SaveDirectoryInfo.CreateSubdirectory(GetThreadNumberFromURI(ThreadUriMatch));
				else
					CurrentDirectory = SaveDirectoryInfo;

				CurrentDirectory.Create();

				foreach (Uri MediaUri in MediaUris)
				{
					FileInfo CurrentFile = new FileInfo(CurrentDirectory.FullName + GetFileNameFromURI(MediaUri));
					if (CurrentFile.Exists)
					{
						Trace.WriteLine($"File at \"{MediaUri}\" from thread at \"{ThreadUriMatch}\" skipped as existing at \"{CurrentFile.FullName}\".");
					}

					try
					{
						Trace.WriteLine(
							$"Beginning download file at \"{MediaUri}\" from thread at \"{ThreadUriMatch}\" as \"{CurrentFile.FullName}\".");
						DownloaderWC.DownloadFile(MediaUri, CurrentFile.FullName);
					}
					catch (WebException we)
					{
						Trace.WriteLine(
							$"Web error while trying to download file at \"{MediaUri}\" from thread at \"{ThreadUriMatch}\" as \"{CurrentFile.FullName}\": \"{we.Message}\".");
						if (we.InnerException != null) Console.WriteLine(we.InnerException.Message);
						continue;
					}
					catch (Exception e)
					{
						Trace.WriteLine(
							$"Unexpected error while trying to download file at \"{MediaUri}\" from thread at \"{ThreadUriMatch}\" as \"{CurrentFile.FullName}\": \"{e.Message}\".");
					}
					Trace.WriteLine($"File at \"{MediaUri}\" from thread at \"{ThreadUriMatch}\" saved as \"{CurrentFile.FullName}\".");
				}
			}

			Trace.WriteLine("Download process finished.");
			Trace.WriteLine("Enabling UI...");
			((Form)(UI)).Enabled = true;
		}


		static IEnumerable<Uri> GetMediaURIsFromThreadHtmlPage(HtmlDocument ThreadPage)
		{
			return
				MediaRegex.Matches(string.Concat(ThreadPage.DocumentNode.SelectNodes("//a[contains(@class, 'post__image-link')]").Select(x => x.OuterHtml.Split('>')[0])))
					.Select(x => new Uri(@"https://2ch.hk" + x.Value.Split('"')[1]));
		}
		static IEnumerable<Uri> GetThreadURIsFromText(string Text)
		{
			return
				ThreadUriRegex.Matches(Text).Select(x => new Uri(@"https://" + x.Value));
		}
		static string GetThreadNumberFromURI(Uri ThreadUri) => ThreadUri.AbsoluteUri.Split('/').Last().Split('.').First();
		static string GetFileNameFromURI(Uri FileUri) => FileUri.AbsoluteUri.Split('/').Last();
	}


	class DownloaderUI
	{
		Label InfoLabel = new Label { Text = "Inset your 2ch.hk thread URIs here:" };
		RichTextBox URIsTextBox = new RichTextBox();
		CheckBox CreateSubFoldersCheckBox = new CheckBox { Text = "Create sub-folder for each thread", Checked = true };
		Button DownloadButton = new Button { Text = "Start downloading" };
		Form MainForm = new Form() { };

		public DownloaderUI()
		{
			Control[] AllControls = { InfoLabel, URIsTextBox, CreateSubFoldersCheckBox, DownloadButton };
			Controls.ToSameWidth(AllControls, 300);
			Panel AllElementsPanel = Controls.ToVerticalStackPanel(AllControls);
			MainForm.Controls.Add(AllElementsPanel);
			MainForm.ClientSize = new Size(AllElementsPanel.Width, AllElementsPanel.Height);
			MainForm.MinimumSize = MainForm.MaximumSize = MainForm.Size;
		}

		public static explicit operator Form(DownloaderUI dui)
		{
			return dui.MainForm;
		}

		public Form Form => MainForm;
		public string UrisInput => URIsTextBox.Text;
		public bool SubFoldersRequired => CreateSubFoldersCheckBox.Checked;

		public event EventHandler DownloadButtonClick
		{
			add => DownloadButton.Click += value;
			remove => DownloadButton.Click -= value;
		}
	}

	class Controls
	{
		public static Panel ToVerticalStackPanel(IList<Control> Controls, int Space = 0)
		{
			Panel Out = new Panel();
			int CurrentHeight = 0;

			for (int i = 0; i < Controls.Count; i++)
			{
				Controls[i].Location = new Point(0, CurrentHeight);
				Out.Controls.Add(Controls[i]);
				CurrentHeight += Controls[i].Height + Space;
			}

			Out.Height = Controls.Last().Location.Y + Controls.Last().Height;
			Out.Width = Controls.Max(x => x.Width);
			Out.BackColor = Color.Transparent;

			return Out;
		}

		public static void ToSameWidth(IList<Control> Controls, int Width)
		{
			for (int i = 0; i < Controls.Count; i++) Controls[i].Width = Width;
		}
	}
}